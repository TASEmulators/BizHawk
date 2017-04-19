// TODO - add serializer (?)

// http://wiki.superfamicom.org/snes/show/Backgrounds

// TODO 
// libsnes needs to be modified to support multiple instances - THIS IS NECESSARY - or else loading one game and then another breaks things
// edit - this is a lot of work
// wrap dll code around some kind of library-accessing interface so that it doesnt malfunction if the dll is unavailablecd

using System;
using System.Linq;
using System.Xml;
using System.IO;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	[CoreAttributes(
		"BSNES",
		"byuu",
		isPorted: true,
		isReleased: true,
		portedVersion: "v87",
		portedUrl: "http://byuu.org/"
		)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public unsafe partial class LibsnesCore : IEmulator, IVideoProvider, ISaveRam, IStatable, IInputPollable, IRegionable, ICodeDataLogger,
		IDebuggable, ISettable<LibsnesCore.SnesSettings, LibsnesCore.SnesSyncSettings>
	{
		public LibsnesCore(GameInfo game, byte[] romData, bool deterministicEmulation, byte[] xmlData, CoreComm comm, object Settings, object SyncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			Tracer = new TraceBuffer
			{
				Header = "65816: PC, mnemonic, operands, registers (A, X, Y, S, D, DB, flags (NVMXDIZC), V, H)"
			};
			(ServiceProvider as BasicServiceProvider).Register<ITraceable>(Tracer);

			(ServiceProvider as BasicServiceProvider).Register<IDisassemblable>(new BizHawk.Emulation.Cores.Components.W65816.W65816_DisassemblerService());

			_game = game;
			CoreComm = comm;
			byte[] sgbRomData = null;
			if (game["SGB"])
			{
				if ((romData[0x143] & 0xc0) == 0xc0)
					throw new CGBNotSupportedException();
				sgbRomData = CoreComm.CoreFileProvider.GetFirmware("SNES", "Rom_SGB", true, "SGB Rom is required for SGB emulation.");
				game.FirmwareHash = sgbRomData.HashSHA1();
			}

			_settings = (SnesSettings)Settings ?? new SnesSettings();
			_syncSettings = (SnesSyncSettings)SyncSettings ?? new SnesSyncSettings();

			api = new LibsnesApi(GetDllPath());
			api.ReadHook = ReadHook;
			api.ExecHook = ExecHook;
			api.WriteHook = WriteHook;

			ScanlineHookManager = new MyScanlineHookManager(this);

			_controllerDeck = new LibsnesControllerDeck(
				_syncSettings.LeftPort,
				_syncSettings.RightPort);
			_controllerDeck.NativeInit(api);
			
			api.CMD_init();

			api.QUERY_set_video_refresh(snes_video_refresh);
			api.QUERY_set_input_poll(snes_input_poll);
			api.QUERY_set_input_state(snes_input_state);
			api.QUERY_set_input_notify(snes_input_notify);
			api.QUERY_set_path_request(snes_path_request);

			scanlineStart_cb = new LibsnesApi.snes_scanlineStart_t(snes_scanlineStart);
			tracecb = new LibsnesApi.snes_trace_t(snes_trace);

			soundcb = new LibsnesApi.snes_audio_sample_t(snes_audio_sample);
			api.QUERY_set_audio_sample(soundcb);

			RefreshPalette();

			// start up audio resampler
			InitAudio();
			(ServiceProvider as BasicServiceProvider).Register<ISoundProvider>(resampler);

			//strip header
			if (romData != null)
				if ((romData.Length & 0x7FFF) == 512)
				{
					var newData = new byte[romData.Length - 512];
					Array.Copy(romData, 512, newData, 0, newData.Length);
					romData = newData;
				}

			if (game["SGB"])
			{
				IsSGB = true;
				SystemId = "SNES";
				BoardName = "SGB";

				CurrLoadParams = new LoadParams()
				{
					type = LoadParamType.SuperGameBoy,
					rom_xml = null,
					rom_data = sgbRomData,
					rom_size = (uint)sgbRomData.Length,
					dmg_data = romData,
				};

				if (!LoadCurrent())
					throw new Exception("snes_load_cartridge_normal() failed");
			}
			else
			{
				//we may need to get some information out of the cart, even during the following bootup/load process
				if (xmlData != null)
				{
					romxml = new System.Xml.XmlDocument();
					romxml.Load(new MemoryStream(xmlData));

					//bsnes wont inspect the xml to load the necessary sfc file.
					//so, we have to do that here and pass it in as the romData :/
					if (romxml["cartridge"] != null && romxml["cartridge"]["rom"] != null)
						romData = File.ReadAllBytes(CoreComm.CoreFileProvider.PathSubfile(romxml["cartridge"]["rom"].Attributes["name"].Value));
					else
						throw new Exception("Could not find rom file specification in xml file. Please check the integrity of your xml file");
				}

				SystemId = "SNES";
				CurrLoadParams = new LoadParams()
				{
					type = LoadParamType.Normal,
					xml_data = xmlData,
					rom_data = romData
				};

				if (!LoadCurrent())
					throw new Exception("snes_load_cartridge_normal() failed");
			}

			if (api.Region == LibsnesApi.SNES_REGION.NTSC)
			{
				//similar to what aviout reports from snes9x and seems logical from bsnes first principles. bsnes uses that numerator (ntsc master clockrate) for sure.
				CoreComm.VsyncNum = 21477272;
				CoreComm.VsyncDen = 4 * 341 * 262;
			}
			else
			{
				//http://forums.nesdev.com/viewtopic.php?t=5367&start=19
				CoreComm.VsyncNum = 21281370;
				CoreComm.VsyncDen = 4 * 341 * 312;
			}

			api.CMD_power();

			SetupMemoryDomains(romData, sgbRomData);

			// DeterministicEmulation = deterministicEmulation; // Note we don't respect the value coming in and force it instead
			if (DeterministicEmulation) // save frame-0 savestate now
			{
				MemoryStream ms = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(ms);
				bw.Write(CoreSaveState());
				bw.Write(true); // framezero, so no controller follows and don't frameadvance on load
				// hack: write fake dummy controller info
				bw.Write(new byte[536]);
				bw.Close();
				_savestatebuff = ms.ToArray();
			}
		}

		private GameInfo _game;

		public string CurrentProfile
		{
			get
			{
				// TODO: This logic will only work until Accuracy is ready, would we really want to override the user's choice of Accuracy with Compatibility?
				if (_game.OptionValue("profile") == "Compatibility")
				{
					return "Compatibility";
				}

				return _syncSettings.Profile;
			}
		}

		public bool IsSGB { get; private set; }

		private LibsnesControllerDeck _controllerDeck;

		/// <summary>disable all external callbacks.  the front end should not even know the core is frame advancing</summary>
		bool nocallbacks = false;

		public ITraceable Tracer { get; private set; }

		public class MyScanlineHookManager : ScanlineHookManager
		{
			public MyScanlineHookManager(LibsnesCore core)
			{
				this.core = core;
			}
			LibsnesCore core;

			public override void OnHooksChanged()
			{
				core.OnScanlineHooksChanged();
			}
		}

		bool _disposed = false;

		public MyScanlineHookManager ScanlineHookManager;
		void OnScanlineHooksChanged()
		{
			if (_disposed) return;
			if (ScanlineHookManager.HookCount == 0) api.QUERY_set_scanlineStart(null);
			else api.QUERY_set_scanlineStart(scanlineStart_cb);
		}

		void snes_scanlineStart(int line)
		{
			ScanlineHookManager.HandleScanline(line);
		}

		string snes_path_request(int slot, string hint)
		{
			//every rom requests msu1.rom... why? who knows.
			//also handle msu-1 pcm files here
			bool is_msu1_rom = hint == "msu1.rom";
			bool is_msu1_pcm = Path.GetExtension(hint).ToLower() == ".pcm";
			if (is_msu1_rom || is_msu1_pcm)
			{
				//well, check if we have an msu-1 xml
				if (romxml != null && romxml["cartridge"] != null && romxml["cartridge"]["msu1"] != null)
				{
					var msu1 = romxml["cartridge"]["msu1"];
					if (is_msu1_rom && msu1["rom"].Attributes["name"] != null)
						return CoreComm.CoreFileProvider.PathSubfile(msu1["rom"].Attributes["name"].Value);
					if (is_msu1_pcm)
					{
						//return @"D:\roms\snes\SuperRoadBlaster\SuperRoadBlaster-1.pcm";
						//return "";
						int wantsTrackNumber = int.Parse(hint.Replace("track-", "").Replace(".pcm", ""));
						wantsTrackNumber++;
						string wantsTrackString = wantsTrackNumber.ToString();
						foreach (var child in msu1.ChildNodes.Cast<XmlNode>())
						{
							if (child.Name == "track" && child.Attributes["number"].Value == wantsTrackString)
								return CoreComm.CoreFileProvider.PathSubfile(child.Attributes["name"].Value);
						}
					}
				}

				//not found.. what to do? (every rom will get here when msu1.rom is requested)
				return "";
			}

			// not MSU-1.  ok.

			string firmwareID;

			switch (hint)
			{
				case "cx4.rom": firmwareID = "CX4"; break;
				case "dsp1.rom": firmwareID = "DSP1"; break;
				case "dsp1b.rom": firmwareID = "DSP1b"; break;
				case "dsp2.rom": firmwareID = "DSP2"; break;
				case "dsp3.rom": firmwareID = "DSP3"; break;
				case "dsp4.rom": firmwareID = "DSP4"; break;
				case "st010.rom": firmwareID = "ST010"; break;
				case "st011.rom": firmwareID = "ST011"; break;
				case "st018.rom": firmwareID = "ST018"; break;
				default:
					CoreComm.ShowMessage(string.Format("Unrecognized SNES firmware request \"{0}\".", hint));
					return "";
			}

			//build romfilename
			string test = CoreComm.CoreFileProvider.GetFirmwarePath("SNES", firmwareID, false, "Game may function incorrectly without the requested firmware.");

			//we need to return something to bsnes
			test = test ?? "";

			Console.WriteLine("Served libsnes request for firmware \"{0}\" with \"{1}\"", hint, test);

			//return the path we built
			return test;
		}

		void snes_trace(string msg)
		{
			// TODO: get them out of the core split up and remove this hackery
			string splitStr = "A:";

			var split = msg.Split(new[] {splitStr }, 2, StringSplitOptions.None);

			Tracer.Put(new TraceInfo
			{
				Disassembly = split[0].PadRight(34),
				RegisterInfo = splitStr + split[1]
			});
		}

		public SnesColors.ColorType CurrPalette { get; private set; }

		public void SetPalette(SnesColors.ColorType pal)
		{
			CurrPalette = pal;
			int[] tmp = SnesColors.GetLUT(pal);
			fixed (int* p = &tmp[0])
				api.QUERY_set_color_lut((IntPtr)p);
		}

		public LibsnesApi api;
		System.Xml.XmlDocument romxml;

		string GetDllPath()
		{
			var exename = "libsneshawk-32-" + CurrentProfile.ToLower() + ".dll";

			string dllPath = Path.Combine(CoreComm.CoreFileProvider.DllPath(), exename);

			if (!File.Exists(dllPath))
				throw new InvalidOperationException("Couldn't locate the DLL for SNES emulation for profile: " + CurrentProfile + ". Please make sure you're using a fresh dearchive of a BizHawk distribution.");

			return dllPath;
		}

		void ReadHook(uint addr)
		{
			MemoryCallbacks.CallReads(addr);
			//we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
			//EDIT: for now, theres some IPC re-entrancy problem
			//RefreshMemoryCallbacks();
		}
		void ExecHook(uint addr)
		{
			MemoryCallbacks.CallExecutes(addr);
			//we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
			//EDIT: for now, theres some IPC re-entrancy problem
			//RefreshMemoryCallbacks();
		}
		void WriteHook(uint addr, byte val)
		{
			MemoryCallbacks.CallWrites(addr);
			//we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
			//EDIT: for now, theres some IPC re-entrancy problem
			//RefreshMemoryCallbacks();
		}

		LibsnesApi.snes_scanlineStart_t scanlineStart_cb;
		LibsnesApi.snes_trace_t tracecb;
		LibsnesApi.snes_audio_sample_t soundcb;

		enum LoadParamType
		{
			Normal, SuperGameBoy
		}
		struct LoadParams
		{
			public LoadParamType type;
			public byte[] xml_data;

			public string rom_xml;
			public byte[] rom_data;
			public uint rom_size;
			public byte[] dmg_data;
		}

		LoadParams CurrLoadParams;

		bool LoadCurrent()
		{
			bool result = false;
			if (CurrLoadParams.type == LoadParamType.Normal)
				result = api.CMD_load_cartridge_normal(CurrLoadParams.xml_data, CurrLoadParams.rom_data);
			else result = api.CMD_load_cartridge_super_game_boy(CurrLoadParams.rom_xml, CurrLoadParams.rom_data, CurrLoadParams.rom_size, CurrLoadParams.dmg_data);

			mapper = api.Mapper;

			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="port">0 or 1, corresponding to L and R physical ports on the snes</param>
		/// <param name="device">LibsnesApi.SNES_DEVICE enum index specifying type of device</param>
		/// <param name="index">meaningless for most controllers.  for multitap, 0-3 for which multitap controller</param>
		/// <param name="id">button ID enum; in the case of a regular controller, this corresponds to shift register position</param>
		/// <returns>for regular controllers, one bit D0 of button status.  for other controls, varying ranges depending on id</returns>
		short snes_input_state(int port, int device, int index, int id)
		{
			return _controllerDeck.CoreInputState(Controller, port, device, index, id);
		}

		void snes_input_poll()
		{
			// this doesn't actually correspond to anything in the underlying bsnes;
			// it gets called once per frame with video_refresh() and has nothing to do with anything
		}

		void snes_input_notify(int index)
		{
			// gets called with the following numbers:
			// 4xxx : lag frame related
			// 0: signifies latch bit going to 0.  should be reported as oninputpoll
			// 1: signifies latch bit going to 1.  should be reported as oninputpoll
			if (index >= 0x4000)
				IsLagFrame = false;
		}

		void snes_video_refresh(int* data, int width, int height)
		{
			bool doubleSize = _settings.AlwaysDoubleSize;
			bool lineDouble = doubleSize, dotDouble = doubleSize;

			_videoWidth = width;
			_videoHeight = height;

			int yskip = 1, xskip = 1;

			//if we are in high-res mode, we get double width. so, lets double the height here to keep it square.
			if (width == 512)
			{
				_videoHeight *= 2;
				yskip = 2;

				lineDouble = true;
				//we dont dot double here because the user wanted double res and the game provided double res
				dotDouble = false;
			}
			else if (lineDouble)
			{
				_videoHeight *= 2;
				yskip = 2;
			}

			int srcPitch = 1024;
			int srcStart = 0;

			bool interlaced = (height == 478 || height == 448);
			if (interlaced)
			{
				//from bsnes in interlaced mode we have each field side by side
				//so we will come in with a dimension of 512x448, say
				//but the fields are side by side, so it's actually 1024x224.
				//copy the first scanline from row 0, then the 2nd scanline from row 0 (offset 512)
				//EXAMPLE: yu yu hakushu legal screens
				//EXAMPLE: World Class Service Super Nintendo Tester (double resolution vertically but not horizontally, in character test the stars should shrink)
				lineDouble = false;
				srcPitch = 512;
				yskip = 1;
				_videoHeight = height;
			}

			if (dotDouble)
			{
				_videoWidth *= 2;
				xskip = 2;
			}

			int size = _videoWidth * _videoHeight;
			if (_videoBuffer.Length != size)
				_videoBuffer = new int[size];

			for (int j = 0; j < 2; j++)
			{
				if (j == 1 && !dotDouble) break;
				int xbonus = j;
				for (int i = 0; i < 2; i++)
				{
					//potentially do this twice, if we need to line double
					if (i == 1 && !lineDouble) break;

					int bonus = i * _videoWidth + xbonus;
					for (int y = 0; y < height; y++)
						for (int x = 0; x < width; x++)
						{
							int si = y * srcPitch + x + srcStart;
							int di = y * _videoWidth * yskip + x * xskip + bonus;
							int rgb = data[si];
							_videoBuffer[di] = rgb;
						}
				}
			}
		}

		void RefreshMemoryCallbacks(bool suppress)
		{
			var mcs = MemoryCallbacks;
			api.QUERY_set_state_hook_exec(!suppress && mcs.HasExecutes);
			api.QUERY_set_state_hook_read(!suppress && mcs.HasReads);
			api.QUERY_set_state_hook_write(!suppress && mcs.HasWrites);
		}

		private int _timeFrameCounter;

		//public byte[] snes_get_memory_data_read(LibsnesApi.SNES_MEMORY id)
		//{
		//  var size = (int)api.snes_get_memory_size(id);
		//  if (size == 0) return new byte[0];
		//  var ret = api.snes_get_memory_data(id);
		//  return ret;
		//}

		#region savestates

		/// <summary>
		/// can freeze a copy of a controller input set and serialize\deserialize it
		/// </summary>
		public class SnesSaveController : IController
		{
			// this is all rather general, so perhaps should be moved out of LibsnesCore

			ControllerDefinition def;

			public SnesSaveController()
			{
				this.def = null;
			}

			public SnesSaveController(ControllerDefinition def)
			{
				this.def = def;
			}

			WorkingDictionary<string, float> buttons = new WorkingDictionary<string,float>();

			/// <summary>
			/// invalid until CopyFrom has been called
			/// </summary>
			public ControllerDefinition Definition
			{
				get { return def; }
			}

			public void Serialize(BinaryWriter b)
			{
				b.Write(buttons.Keys.Count);
				foreach (var k in buttons.Keys)
				{
					b.Write(k);
					b.Write(buttons[k]);
				}
			}

			/// <summary>
			/// no checking to see if the deserialized controls match any definition
			/// </summary>
			/// <param name="b"></param>
			public void DeSerialize(BinaryReader b)
			{
				buttons.Clear();
				int numbuttons = b.ReadInt32();
				for (int i = 0; i < numbuttons; i++)
				{
					string k = b.ReadString();
					float v = b.ReadSingle();
					buttons.Add(k, v);
				}
			}
			
			/// <summary>
			/// this controller's definition changes to that of source
			/// </summary>
			/// <param name="source"></param>
			public void CopyFrom(IController source)
			{
				this.def = source.Definition;
				buttons.Clear();
				foreach (var k in def.BoolButtons)
					buttons.Add(k, source.IsPressed(k) ? 1.0f : 0);
				foreach (var k in def.FloatControls)
				{
					if (buttons.Keys.Contains(k))
						throw new Exception("name collision between bool and float lists!");
					buttons.Add(k, source.GetFloat(k));
				}
			}

			public void Clear()
			{
				buttons.Clear();
			}

			public void Set(string button)
			{
				buttons[button] = 1.0f;
			}
			
			public bool this[string button]
			{
				get { return buttons[button] != 0; }
			}

			public bool IsPressed(string button)
			{
				return buttons[button] != 0;
			}

			public float GetFloat(string name)
			{
				return buttons[name];
			}
		}

		#endregion

		// works for WRAM, garbage for anything else
		static int? FakeBusMap(int addr)
		{
			addr &= 0xffffff;
			int bank = addr >> 16;
			if (bank == 0x7e || bank == 0x7f)
				return addr & 0x1ffff;
			bank &= 0x7f;
			int low = addr & 0xffff;
			if (bank < 0x40 && low < 0x2000)
				return low;
			return null;
		}

		private LibsnesApi.SNES_MAPPER? mapper = null;

		// works for ROM, garbage for anything else
		byte FakeBusRead(int addr)
		{
			addr &= 0xffffff;
			int bank = addr >> 16;
			int low = addr & 0xffff;

			if (!mapper.HasValue)
			{
				return 0;
			}

			switch (mapper)
			{
				case LibsnesApi.SNES_MAPPER.LOROM:
					if (low >= 0x8000)
					{
						return api.QUERY_peek(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr);
					}
					break;
				case LibsnesApi.SNES_MAPPER.EXLOROM:
					if ((bank >= 0x40 && bank <= 0x7f) || low >= 0x8000)
					{
						return api.QUERY_peek(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr);
					}
					break;
				case LibsnesApi.SNES_MAPPER.HIROM:
				case LibsnesApi.SNES_MAPPER.EXHIROM:
					if ((bank >= 0x40 && bank <= 0x7f) || bank >= 0xc0 || low >= 0x8000)
					{
						return api.QUERY_peek(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr);
					}
					break;
				case LibsnesApi.SNES_MAPPER.SUPERFXROM:
					if ((bank >= 0x40 && bank <= 0x5f) || (bank >= 0xc0 && bank <= 0xdf) ||
						(low >= 0x8000 && ((bank >= 0x00 && bank <= 0x3f) || (bank >= 0x80 && bank <= 0xbf))))
					{
						return api.QUERY_peek(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr);
					}
					break;
				case LibsnesApi.SNES_MAPPER.SA1ROM:
					if (bank >= 0xc0 || (low >= 0x8000 && ((bank >= 0x00 && bank <= 0x3f) || (bank >= 0x80 && bank <= 0xbf))))
					{
						return api.QUERY_peek(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr);
					}
					break;
				case LibsnesApi.SNES_MAPPER.BSCLOROM:
					if (low >= 0x8000 && ((bank >= 0x00 && bank <= 0x3f) || (bank >= 0x80 && bank <= 0xbf)))
					{
						return api.QUERY_peek(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr);
					}
					break;
				case LibsnesApi.SNES_MAPPER.BSCHIROM:
					if ((bank >= 0x40 && bank <= 0x5f) || (bank >= 0xc0 && bank <= 0xdf) ||
						(low >= 0x8000 && ((bank >= 0x00 && bank <= 0x1f) || (bank >= 0x80 && bank <= 0x9f))))
					{
						return api.QUERY_peek(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr);
					}
					break;
				case LibsnesApi.SNES_MAPPER.BSXROM:
					if ((bank >= 0x40 && bank <= 0x7f) || bank >= 0xc0 ||
						(low >= 0x8000 && ((bank >= 0x00 && bank <= 0x3f) || (bank >= 0x80 && bank <= 0xbf))) ||
						(low >= 0x6000 && low <= 0x7fff && (bank >= 0x20 && bank <= 0x3f)))
					{
						return api.QUERY_peek(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr);
					}
					break;
				case LibsnesApi.SNES_MAPPER.STROM:
					if (low >= 0x8000 && ((bank >= 0x00 && bank <= 0x5f) || (bank >= 0x80 && bank <= 0xdf)))
					{
						return api.QUERY_peek(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr);
					}
					break;
				default:
					throw new InvalidOperationException(string.Format("Unknown mapper: {0}", mapper));
			}

			return 0;
		}

		#region audio stuff

		SpeexResampler resampler;

		void InitAudio()
		{
			resampler = new SpeexResampler(6, 64081, 88200, 32041, 44100);
		}

		void snes_audio_sample(ushort left, ushort right)
		{
			resampler.EnqueueSample((short)left, (short)right);
		}

		#endregion audio stuff

		void RefreshPalette()
		{
			SetPalette((SnesColors.ColorType)Enum.Parse(typeof(SnesColors.ColorType), _settings.Palette, false));

		}
	}

	public class ScanlineHookManager
	{
		public void Register(object tag, Action<int> callback)
		{
			var rr = new RegistrationRecord();
			rr.tag = tag;
			rr.callback = callback;

			Unregister(tag);
			records.Add(rr);
			OnHooksChanged();
		}

		public int HookCount { get { return records.Count; } }

		public virtual void OnHooksChanged() { }

		public void Unregister(object tag)
		{
			records.RemoveAll((r) => r.tag == tag);
		}

		public void HandleScanline(int scanline)
		{
			foreach (var rr in records) rr.callback(scanline);
		}

		List<RegistrationRecord> records = new List<RegistrationRecord>();

		class RegistrationRecord
		{
			public object tag;
			public int scanline = 0;
			public Action<int> callback;
		}
	}
}
