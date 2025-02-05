using System.Linq;
using System.Xml;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.W65816;

// TODO - add serializer (?)

// http://wiki.superfamicom.org/snes/show/Backgrounds

// TODO
// libsnes needs to be modified to support multiple instances - THIS IS NECESSARY - or else loading one game and then another breaks things
// edit - this is a lot of work
// wrap dll code around some kind of library-accessing interface so that it doesn't malfunction if the dll is unavailable
namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	[PortedCore(CoreNames.Bsnes, "byuu", "v87", "https://github.com/bsnes-emu/bsnes/tree/v087")]
	public unsafe partial class LibsnesCore : IEmulator, IVideoProvider, ISaveRam, IStatable, IInputPollable, IRegionable, ICodeDataLogger,
		IDebuggable, ISettable<LibsnesCore.SnesSettings, LibsnesCore.SnesSyncSettings>, IBSNESForGfxDebugger
	{
		[CoreConstructor(VSystemID.Raw.SGB)]
		[CoreConstructor(VSystemID.Raw.SNES)]
		public LibsnesCore(GameInfo game, byte[] rom, CoreComm comm,
			LibsnesCore.SnesSettings settings, LibsnesCore.SnesSyncSettings syncSettings)
			:this(game, rom, null, null, comm, settings, syncSettings)
		{}

		public LibsnesCore(GameInfo game, byte[] romData, byte[] xmlData, string baseRomPath, CoreComm comm,
			LibsnesCore.SnesSettings settings, LibsnesCore.SnesSyncSettings syncSettings)
		{
			_baseRomPath = baseRomPath;
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			const string TRACE_HEADER = "65816: PC, mnemonic, operands, registers (A, X, Y, S, D, DB, flags (NVMXDIZC), V, H)";
			_tracer = new TraceBuffer(TRACE_HEADER);

			ser.Register<IDisassemblable>(new W65816_DisassemblerService());

			_game = game;
			CoreComm = comm;
			byte[] sgbRomData = null;

			if (game.System == VSystemID.Raw.SGB)
			{
				if ((romData[0x143] & 0xc0) == 0xc0)
				{
					throw new CGBNotSupportedException();
				}

				sgbRomData = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("SNES", "Rom_SGB"), "SGB Rom is required for SGB emulation.");
				game.FirmwareHash = SHA1Checksum.ComputeDigestHex(sgbRomData);
			}

			_settings = settings ?? new SnesSettings();
			_syncSettings = syncSettings ?? new SnesSyncSettings();

			_videocb = snes_video_refresh;
			_inputpollcb = snes_input_poll;
			_inputstatecb = snes_input_state;
			_inputnotifycb = snes_input_notify;
			_scanlineStartCb = snes_scanlineStart;
			_tracecb = snes_trace;
			_soundcb = snes_audio_sample;
			_pathrequestcb = snes_path_request;

			// TODO: pass profile here
			Api = new(PathUtils.DllDirectoryPath, CoreComm, new Delegate[]
			{
				_videocb,
				_inputpollcb,
				_inputstatecb,
				_inputnotifycb,
				_scanlineStartCb,
				_tracecb,
				_soundcb,
				_pathrequestcb
			})
			{
				ReadHook = ReadHook,
				ExecHook = ExecHook,
				WriteHook = WriteHook,
				ReadHook_SMP = ReadHook_SMP,
				ExecHook_SMP = ExecHook_SMP,
				WriteHook_SMP = WriteHook_SMP,
			};

			ScanlineHookManager = new MyScanlineHookManager(this);

			_controllerDeck = new LibsnesControllerDeck(_syncSettings);
			_controllerDeck.NativeInit(Api);

			Api.CMD_init(_syncSettings.RandomizedInitialState);

			Api.QUERY_set_path_request(_pathrequestcb);

			// start up audio resampler
			InitAudio();

			// strip header
			if ((romData?.Length & 0x7FFF) == 512)
			{
				var newData = new byte[romData.Length - 512];
				Array.Copy(romData, 512, newData, 0, newData.Length);
				romData = newData;
			}

			if (game.System == VSystemID.Raw.SGB)
			{
				IsSGB = true;
				SystemId = VSystemID.Raw.SNES;
				ser.Register<IBoardInfo>(new SGBBoardInfo());

				_currLoadParams = new LoadParams
				{
					type = LoadParamType.SuperGameBoy,
					rom_xml = null,
					rom_data = sgbRomData,
					rom_size = (uint)sgbRomData.Length,
					dmg_data = romData,
				};

				if (!LoadCurrent())
				{
					throw new Exception("snes_load_cartridge_normal() failed");
				}
			}
			else
			{
				// we may need to get some information out of the cart, even during the following bootup/load process
				if (xmlData != null)
				{
					_romxml = new XmlDocument();
					_romxml.Load(new MemoryStream(xmlData));

					// bsnes wont inspect the xml to load the necessary sfc file.
					// so, we have to do that here and pass it in as the romData :/
					if (_romxml["cartridge"]?["rom"] != null)
					{
						romData = File.ReadAllBytes(PathSubfile(_romxml["cartridge"]["rom"].Attributes["name"].Value));
					}
					else
					{
						throw new Exception("Could not find rom file specification in xml file. Please check the integrity of your xml file");
					}
				}

				SystemId = VSystemID.Raw.SNES;
				_currLoadParams = new LoadParams
				{
					type = LoadParamType.Normal,
					xml_data = xmlData,
					rom_data = romData
				};

				if (!LoadCurrent())
				{
					throw new Exception("snes_load_cartridge_normal() failed");
				}
			}

			if (_region == LibsnesApi.SNES_REGION.NTSC)
			{
				// similar to what aviout reports from snes9x and seems logical from bsnes first principles. bsnes uses that numerator (ntsc master clockrate) for sure.
				VsyncNumerator = 21477272;
				VsyncDenominator = 4 * 341 * 262;
			}
			else
			{
				// http://forums.nesdev.com/viewtopic.php?t=5367&start=19
				VsyncNumerator = 21281370;
				VsyncDenominator = 4 * 341 * 312;
			}

			Api.CMD_power();

			SetupMemoryDomains(romData, sgbRomData);

			if (CurrentProfile == "Compatibility")
			{
				ser.Register<ITraceable>(_tracer);
			}

			Api.QUERY_set_path_request(null);
			Api.QUERY_set_video_refresh(_videocb);
			Api.QUERY_set_input_poll(_inputpollcb);
			Api.QUERY_set_input_state(_inputstatecb);
			Api.QUERY_set_input_notify(_inputnotifycb);
			Api.QUERY_set_audio_sample(_soundcb);
			Api.Seal();
			RefreshPalette();
		}

		private readonly LibsnesApi.snes_video_refresh_t _videocb;
		private readonly LibsnesApi.snes_input_poll_t _inputpollcb;
		private readonly LibsnesApi.snes_input_state_t _inputstatecb;
		private readonly LibsnesApi.snes_input_notify_t _inputnotifycb;
		private readonly LibsnesApi.snes_path_request_t _pathrequestcb;

		internal CoreComm CoreComm { get; }

		private readonly string _baseRomPath = "";

		private string PathSubfile(string fname) => Path.Combine(_baseRomPath, fname);

		private readonly GameInfo _game;
		private readonly LibsnesControllerDeck _controllerDeck;
		private readonly ITraceable _tracer;
		private readonly XmlDocument _romxml;
		private readonly LibsnesApi.snes_scanlineStart_t _scanlineStartCb;
		private readonly LibsnesApi.snes_trace_t _tracecb;
		private readonly LibsnesApi.snes_audio_sample_t _soundcb;

		private IController _controller;
		private readonly LoadParams _currLoadParams;
		private int _timeFrameCounter;
		private bool _disposed;

		public bool IsSGB { get; }

		private class SGBBoardInfo : IBoardInfo
		{
			public string BoardName => "SGB";
		}

		public string CurrentProfile => "Compatibility"; // We no longer support performance, and accuracy isn't worth the effort so we shall just hardcode this one

		public LibsnesApi Api { get; }

		public SnesColors.ColorType CurrPalette { get; private set; }

		public void SetPalette(SnesColors.ColorType palette)
		{
			var s = GetSettings();
			s.Palette = Enum.GetName(typeof(SnesColors.ColorType), palette);
			PutSettings(s);
		}

		public ISNESGraphicsDecoder CreateGraphicsDecoder()
			=> new SNESGraphicsDecoder(Api, CurrPalette);

		public ScanlineHookManager ScanlineHookManager { get; }

		public class MyScanlineHookManager : ScanlineHookManager
		{
			private readonly LibsnesCore _core;

			public MyScanlineHookManager(LibsnesCore core)
			{
				_core = core;
			}

			protected override void OnHooksChanged()
			{
				_core.OnScanlineHooksChanged();
			}
		}

		private void OnScanlineHooksChanged()
		{
			if (_disposed)
			{
				return;
			}

			Api.QUERY_set_scanlineStart(ScanlineHookManager.HookCount == 0 ? null : _scanlineStartCb);
		}

		private void snes_scanlineStart(int line)
		{
			ScanlineHookManager.HandleScanline(line);
		}

		private string snes_path_request(int slot, string hint)
		{
			// every rom requests msu1.rom... why? who knows.
			// also handle msu-1 pcm files here
			bool isMsu1Rom = hint == "msu1.rom";
			bool isMsu1Pcm = Path.GetExtension(hint).ToLowerInvariant() == ".pcm";
			if (isMsu1Rom || isMsu1Pcm)
			{
				// well, check if we have an msu-1 xml
				if (_romxml?["cartridge"]?["msu1"] != null)
				{
					var msu1 = _romxml["cartridge"]["msu1"];
					if (isMsu1Rom && msu1["rom"]?.Attributes["name"] != null)
					{
						return PathSubfile(msu1["rom"].Attributes["name"].Value);
					}

					if (isMsu1Pcm)
					{
						// return @"D:\roms\snes\SuperRoadBlaster\SuperRoadBlaster-1.pcm";
						// return "";
						int wantsTrackNumber = int.Parse(hint.Replace("track-", "").Replace(".pcm", ""));
						wantsTrackNumber++;
						string wantsTrackString = wantsTrackNumber.ToString();
						foreach (var child in msu1.ChildNodes.Cast<XmlNode>())
						{
							if (child.Name == "track" && child.Attributes["number"].Value == wantsTrackString)
							{
								return PathSubfile(child.Attributes["name"].Value);
							}
						}
					}
				}

				// not found.. what to do? (every rom will get here when msu1.rom is requested)
				return "";
			}

			// not MSU-1.  ok.
			string firmwareId;

			switch (hint)
			{
				case "cx4.rom": firmwareId = "CX4"; break;
				case "dsp1.rom": firmwareId = "DSP1"; break;
				case "dsp1b.rom": firmwareId = "DSP1b"; break;
				case "dsp2.rom": firmwareId = "DSP2"; break;
				case "dsp3.rom": firmwareId = "DSP3"; break;
				case "dsp4.rom": firmwareId = "DSP4"; break;
				case "st010.rom": firmwareId = "ST010"; break;
				case "st011.rom": firmwareId = "ST011"; break;
				case "st018.rom": firmwareId = "ST018"; break;
				default:
					CoreComm.ShowMessage($"Unrecognized SNES firmware request \"{hint}\".");
					return "";
			}

			string ret;
			var data = CoreComm.CoreFileProvider.GetFirmware(new("SNES", firmwareId), "Game may function incorrectly without the requested firmware.");
			if (data != null)
			{
				ret = hint;
				Api.AddReadonlyFile(data, hint);
			}
			else
			{
				ret = "";
			}

			Console.WriteLine("Served libsnes request for firmware \"{0}\"", hint);

			// return the path we built
			return ret;
		}

		private void snes_trace(uint which, string msg)
		{
			// TODO: get them out of the core split up and remove this hackery
			const string splitStr = "A:";

			if (which == (uint)LibsnesApi.eTRACE.CPU)
			{
				var split = msg.Split(new[] { splitStr }, 2, StringSplitOptions.None);
				_tracer.Put(new(disassembly: split[0].PadRight(34), registerInfo: splitStr + split[1]));
			}
			else if (which == (uint)LibsnesApi.eTRACE.SMP)
			{
				int idx = msg.IndexOf("YA:", StringComparison.Ordinal);
				_tracer.Put(new(disassembly: msg.Substring(0, idx).TrimEnd(), registerInfo: msg.Substring(idx)));
			}
			else if (which == (uint)LibsnesApi.eTRACE.GB)
			{
				int idx = msg.IndexOf("AF:", StringComparison.Ordinal);
				_tracer.Put(new(disassembly: msg.Substring(0, idx).TrimEnd(), registerInfo: msg.Substring(idx)));
			}
		}

		private void ReadHook(uint addr)
		{
			if (MemoryCallbacks.HasReads)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessRead;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
				// we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
				// EDIT: for now, theres some IPC re-entrancy problem
				// RefreshMemoryCallbacks();
			}
		}

		private void ExecHook(uint addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessExecute;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
				// we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
				// EDIT: for now, theres some IPC re-entrancy problem
				// RefreshMemoryCallbacks();
			}
		}

		private void WriteHook(uint addr, byte val)
		{
			if (MemoryCallbacks.HasWrites)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessWrite;
				MemoryCallbacks.CallMemoryCallbacks(addr, val, flags, "System Bus");
				// we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
				// EDIT: for now, theres some IPC re-entrancy problem
				// RefreshMemoryCallbacks();
			}
		}

		private void ReadHook_SMP(uint addr)
		{
			if (MemoryCallbacks.HasReads)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessRead;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "SMP");
			}
		}

		private void ExecHook_SMP(uint addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessExecute;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "SMP");
			}
		}

		private void WriteHook_SMP(uint addr, byte val)
		{
			if (MemoryCallbacks.HasWrites)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessWrite;
				MemoryCallbacks.CallMemoryCallbacks(addr, val, flags, "SMP");
			}
		}

		private enum LoadParamType
		{
			Normal, SuperGameBoy
		}

		private struct LoadParams
		{
			public LoadParamType type;
			public byte[] xml_data;

			public string rom_xml;
			public byte[] rom_data;
			public uint rom_size;
			public byte[] dmg_data;
		}

		private bool LoadCurrent()
		{
			bool result = _currLoadParams.type == LoadParamType.Normal
				? Api.CMD_load_cartridge_normal(_currLoadParams.xml_data, _currLoadParams.rom_data)
				: Api.CMD_load_cartridge_super_game_boy(_currLoadParams.rom_xml, _currLoadParams.rom_data, _currLoadParams.rom_size, _currLoadParams.dmg_data);

			_mapper = Api.Mapper;
			_region = Api.Region;

			return result;
		}

		/// <param name="port">0 or 1, corresponding to L and R physical ports on the snes</param>
		/// <param name="device">LibsnesApi.SNES_DEVICE enum index specifying type of device</param>
		/// <param name="index">meaningless for most controllers.  for multitap, 0-3 for which multitap controller</param>
		/// <param name="id">button ID enum; in the case of a regular controller, this corresponds to shift register position</param>
		/// <returns>for regular controllers, one bit D0 of button status.  for other controls, varying ranges depending on id</returns>
		private short snes_input_state(int port, int device, int index, int id)
		{
			return _controllerDeck.CoreInputState(_controller, port, device, index, id);
		}

		private void snes_input_poll()
		{
			// this doesn't actually correspond to anything in the underlying bsnes;
			// it gets called once per frame with video_refresh() and has nothing to do with anything
		}

		private void snes_input_notify(int index)
		{
			// gets called with the following numbers:
			// 4xxx : lag frame related
			// 0: signifies latch bit going to 0.  should be reported as oninputpoll
			// 1: signifies latch bit going to 1.  should be reported as oninputpoll
			if (index >= 0x4000)
			{
				IsLagFrame = false;
			}
		}

		private void snes_video_refresh(int* data, int width, int height)
		{
			bool doubleSize = _settings.AlwaysDoubleSize;
			bool lineDouble = doubleSize, dotDouble = doubleSize;

			_videoWidth = width;
			_videoHeight = height;

			int yskip = 1, xskip = 1;

			// if we are in high-res mode, we get double width. so, lets double the height here to keep it square.
			if (width == 512)
			{
				_videoHeight *= 2;
				yskip = 2;

				lineDouble = true;

				// we don't dot double here because the user wanted double res and the game provided double res
				dotDouble = false;
			}
			else if (lineDouble)
			{
				_videoHeight *= 2;
				yskip = 2;
			}

			int srcPitch = 1024;
			int srcStart = 0;

			bool interlaced = height == 478 || height == 448;
			if (interlaced)
			{
				// from bsnes in interlaced mode we have each field side by side
				// so we will come in with a dimension of 512x448, say
				// but the fields are side by side, so it's actually 1024x224.
				// copy the first scanline from row 0, then the 2nd scanline from row 0 (offset 512)
				// EXAMPLE: yu yu hakushu legal screens
				// EXAMPLE: World Class Service Super Nintendo Tester (double resolution vertically but not horizontally, in character test the stars should shrink)
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

			if (_settings.CropSGBFrame && IsSGB)
			{
				_videoWidth = 160;
				_videoHeight = 144;
			}

			int size = _videoWidth * _videoHeight;
			if (_videoBuffer.Length != size)
			{
				_videoBuffer = new int[size];
			}

			if (_settings.CropSGBFrame && IsSGB)
			{
				int di = 0;
				for (int y = 0; y < 144; y++)
				{
					int si = ((y+39) * srcPitch) + 48;
					for(int x=0;x<160;x++)
						_videoBuffer[di++] = data[si++];
				}
				return;
			}

			for (int j = 0; j < 2; j++)
			{
				if (j == 1 && !dotDouble)
				{
					break;
				}

				int xbonus = j;
				for (int i = 0; i < 2; i++)
				{
					// potentially do this twice, if we need to line double
					if (i == 1 && !lineDouble)
					{
						break;
					}

					int bonus = (i * _videoWidth) + xbonus;
					for (int y = 0; y < height; y++)
					{
						for (int x = 0; x < width; x++)
						{
							int si = (y * srcPitch) + x + srcStart;
							int di = y * _videoWidth * yskip + x * xskip + bonus;
							int rgb = data[si];
							_videoBuffer[di] = rgb;
						}
					}
				}
			}

			VirtualHeight = BufferHeight;
			VirtualWidth = BufferWidth;
			if (VirtualHeight * 2 < VirtualWidth)
				VirtualHeight *= 2;
			if (VirtualHeight > 240)
				VirtualWidth = 512;
			VirtualWidth = (int)Math.Round(VirtualWidth * 1.146);
		}

		private void RefreshMemoryCallbacks(bool suppress)
		{
			var mcs = MemoryCallbacks;
			Api.QUERY_set_state_hook_exec(!suppress && mcs.HasExecutesForScope("System Bus"));
			Api.QUERY_set_state_hook_read(!suppress && mcs.HasReadsForScope("System Bus"));
			Api.QUERY_set_state_hook_write(!suppress && mcs.HasWritesForScope("System Bus"));
		}

		//public byte[] snes_get_memory_data_read(LibsnesApi.SNES_MEMORY id)
		//{
		//  var size = (int)api.snes_get_memory_size(id);
		//  if (size == 0) return new byte[0];
		//  var ret = api.snes_get_memory_data(id);
		//  return ret;
		//}

		private void RefreshPalette()
		{
			CurrPalette = (SnesColors.ColorType)Enum.Parse(typeof(SnesColors.ColorType), _settings.Palette, false);
			int[] tmp = SnesColors.GetLUT(CurrPalette);
			fixed (int* p = &tmp[0])
				Api.QUERY_set_color_lut((IntPtr)p);
		}
	}
}
