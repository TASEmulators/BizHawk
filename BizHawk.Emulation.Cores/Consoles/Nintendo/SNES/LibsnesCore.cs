//TODO - add serializer (?)

//http://wiki.superfamicom.org/snes/show/Backgrounds

//TODO 
//libsnes needs to be modified to support multiple instances - THIS IS NECESSARY - or else loading one game and then another breaks things
// edit - this is a lot of work
//wrap dll code around some kind of library-accessing interface so that it doesnt malfunction if the dll is unavailable

using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
	public unsafe class LibsnesCore : IEmulator, IVideoProvider, ISaveRam, IStatable, IInputPollable, IRegionable, ICodeDataLogger,
		IDebuggable, ISettable<LibsnesCore.SnesSettings, LibsnesCore.SnesSyncSettings>
	{
		public LibsnesCore(GameInfo game, byte[] romData, bool deterministicEmulation, byte[] xmlData, CoreComm comm, object Settings, object SyncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			MemoryCallbacks = new MemoryCallbackSystem();
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

			this.Settings = (SnesSettings)Settings ?? new SnesSettings();
			this.SyncSettings = (SnesSyncSettings)SyncSettings ?? new SnesSyncSettings();

			api = new LibsnesApi(GetDllPath());
			api.ReadHook = ReadHook;
			api.ExecHook = ExecHook;
			api.WriteHook = WriteHook;

			ScanlineHookManager = new MyScanlineHookManager(this);

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
					dmg_xml = null,
					dmg_data = romData,
					dmg_size = (uint)romData.Length
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

			if (api.QUERY_get_region() == LibsnesApi.SNES_REGION.NTSC)
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

			DeterministicEmulation = deterministicEmulation;
			if (DeterministicEmulation) // save frame-0 savestate now
			{
				MemoryStream ms = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(ms);
				bw.Write(CoreSaveState());
				bw.Write(true); // framezero, so no controller follows and don't frameadvance on load
				// hack: write fake dummy controller info
				bw.Write(new byte[536]);
				bw.Close();
				savestatebuff = ms.ToArray();
			}
		}

		CodeDataLog currCdl;

		public void SetCDL(CodeDataLog cdl)
		{
			if(currCdl != null) currCdl.Unpin();
			currCdl = cdl;
			if(currCdl != null) currCdl.Pin();
			
			//set it no matter what. if its null, the cdl will be unhooked from libsnes internally
			api.QUERY_set_cdl(currCdl);
		}

		public void NewCDL(CodeDataLog cdl)
		{
			cdl["CARTROM"] = new byte[MemoryDomains["CARTROM"].Size];

			if (MemoryDomains.Has("CARTRAM"))
				cdl["CARTRAM"] = new byte[MemoryDomains["CARTRAM"].Size];

			cdl["WRAM"] = new byte[MemoryDomains["WRAM"].Size];
			cdl["APURAM"] = new byte[MemoryDomains["APURAM"].Size];

			cdl.SubType = "SNES";
			cdl.SubVer = 0;			
		}

		public void DisassembleCDL(Stream s, CodeDataLog cdl)
		{
			//not supported yet
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

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

				return SyncSettings.Profile;
			}
		}

		public bool IsSGB { get; private set; }

		/// <summary>disable all external callbacks.  the front end should not even know the core is frame advancing</summary>
		bool nocallbacks = false;

		bool disposed = false;
		public void Dispose()
		{
			if (disposed) return;
			disposed = true;

			api.CMD_unload_cartridge();
			api.CMD_term();

			resampler.Dispose();
			api.Dispose();

			if (currCdl != null) currCdl.Unpin();
		}

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			LibsnesApi.CpuRegs regs;
			api.QUERY_peek_cpu_regs(out regs);

			bool fn = (regs.p & 0x80)!=0;
			bool fv = (regs.p & 0x40)!=0;
			bool fm = (regs.p & 0x20)!=0;
			bool fx = (regs.p & 0x10)!=0;
			bool fd = (regs.p & 0x08)!=0;
			bool fi = (regs.p & 0x04)!=0;
			bool fz = (regs.p & 0x02)!=0;
			bool fc = (regs.p & 0x01)!=0;
			
			return new Dictionary<string, RegisterValue>
			{
				{ "PC", regs.pc },
				{ "A", regs.a },
				{ "X", regs.x },
				{ "Y", regs.y },
				{ "Z", regs.z },
				{ "S", regs.s },
				{ "D", regs.d },
				{ "Vector", regs.vector },
				{ "P", regs.p },
				{ "AA", regs.aa },
				{ "RD", regs.rd },
				{ "SP", regs.sp },
				{ "DP", regs.dp },
				{ "DB", regs.db },
				{ "MDR", regs.mdr },
				{ "Flag N", fn },
				{ "Flag V", fv },
				{ "Flag M", fm },
				{ "Flag X", fx },
				{ "Flag D", fd },
				{ "Flag I", fi },
				{ "Flag Z", fz },
				{ "Flag C", fc },
			};
		}

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();

		// TODO: optimize managed to unmanaged using the ActiveChanged event
		public IInputCallbackSystem InputCallbacks { get { return _inputCallbacks; } }

		public ITraceable Tracer { get; private set; }
		public IMemoryCallbackSystem MemoryCallbacks { get; private set; }

		public bool CanStep(StepType type) { return false; }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

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
		public MyScanlineHookManager ScanlineHookManager;
		void OnScanlineHooksChanged()
		{
			if (disposed) return;
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
			api.SPECIAL_Resume();
		}
		void ExecHook(uint addr)
		{
			MemoryCallbacks.CallExecutes(addr);
			//we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
			//EDIT: for now, theres some IPC re-entrancy problem
			//RefreshMemoryCallbacks();
			api.SPECIAL_Resume();
		}
		void WriteHook(uint addr, byte val)
		{
			MemoryCallbacks.CallWrites(addr);
			//we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
			//EDIT: for now, theres some IPC re-entrancy problem
			//RefreshMemoryCallbacks();
			api.SPECIAL_Resume();
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
			public string dmg_xml;
			public byte[] dmg_data;
			public uint dmg_size;
		}

		LoadParams CurrLoadParams;

		bool LoadCurrent()
		{
			bool result = false;
			if (CurrLoadParams.type == LoadParamType.Normal)
				result = api.CMD_load_cartridge_normal(CurrLoadParams.xml_data, CurrLoadParams.rom_data);
			else result = api.CMD_load_cartridge_super_game_boy(CurrLoadParams.rom_xml, CurrLoadParams.rom_data, CurrLoadParams.rom_size, CurrLoadParams.dmg_xml, CurrLoadParams.dmg_data, CurrLoadParams.dmg_size);

			mapper = api.QUERY_get_mapper();

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
		ushort snes_input_state(int port, int device, int index, int id)
		{
			// as this is implemented right now, only P1 and P2 normal controllers work

			// port = 0, oninputpoll = 2: left port was strobed
			// port = 1, oninputpoll = 3: right port was strobed

			// InputCallbacks.Call();
			//Console.WriteLine("{0} {1} {2} {3}", port, device, index, id);

			string key = "P" + (1 + port) + " ";
			if ((LibsnesApi.SNES_DEVICE)device == LibsnesApi.SNES_DEVICE.JOYPAD)
			{
				switch ((LibsnesApi.SNES_DEVICE_ID)id)
				{
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_A: key += "A"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_B: key += "B"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_X: key += "X"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_Y: key += "Y"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_UP: key += "Up"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_DOWN: key += "Down"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_LEFT: key += "Left"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_RIGHT: key += "Right"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_L: key += "L"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_R: key += "R"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_SELECT: key += "Select"; break;
					case LibsnesApi.SNES_DEVICE_ID.JOYPAD_START: key += "Start"; break;
					default: return 0;
				}

				return (ushort)(Controller[key] ? 1 : 0);
			}

			return 0;

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
			bool doubleSize = Settings.AlwaysDoubleSize;
			bool lineDouble = doubleSize, dotDouble = doubleSize;

			vidWidth = width;
			vidHeight = height;

			int yskip = 1, xskip = 1;

			//if we are in high-res mode, we get double width. so, lets double the height here to keep it square.
			if (width == 512)
			{
				vidHeight *= 2;
				yskip = 2;

				lineDouble = true;
				//we dont dot double here because the user wanted double res and the game provided double res
				dotDouble = false;
			}
			else if (lineDouble)
			{
				vidHeight *= 2;
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
				vidHeight = height;
			}

			if (dotDouble)
			{
				vidWidth *= 2;
				xskip = 2;
			}

			int size = vidWidth * vidHeight;
			if (vidBuffer.Length != size)
				vidBuffer = new int[size];

			for (int j = 0; j < 2; j++)
			{
				if (j == 1 && !dotDouble) break;
				int xbonus = j;
				for (int i = 0; i < 2; i++)
				{
					//potentially do this twice, if we need to line double
					if (i == 1 && !lineDouble) break;

					int bonus = i * vidWidth + xbonus;
					for (int y = 0; y < height; y++)
						for (int x = 0; x < width; x++)
						{
							int si = y * srcPitch + x + srcStart;
							int di = y * vidWidth * yskip + x * xskip + bonus;
							int rgb = data[si];
							vidBuffer[di] = rgb;
						}
				}
			}
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			api.MessageCounter = 0;

			if(Settings.UseRingBuffer)
				api.BeginBufferIO();

			/* if the input poll callback is called, it will set this to false
			 * this has to be done before we save the per-frame state in deterministic
			 * mode, because in there, the core actually advances, and might advance
			 * through the point in time where IsLagFrame gets set to false.  makes sense?
			 */

			IsLagFrame = true;

			if (!nocallbacks && Tracer.Enabled)
				api.QUERY_set_trace_callback(tracecb);
			else
				api.QUERY_set_trace_callback(null);

			// for deterministic emulation, save the state we're going to use before frame advance
			// don't do this during nocallbacks though, since it's already been done
			if (!nocallbacks && DeterministicEmulation)
			{
				MemoryStream ms = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(ms);
				bw.Write(CoreSaveState());
				bw.Write(false); // not framezero
				SnesSaveController ssc = new SnesSaveController();
				ssc.CopyFrom(Controller);
				ssc.Serialize(bw);
				bw.Close();
				savestatebuff = ms.ToArray();
			}

			// speedup when sound rendering is not needed
			if (!rendersound)
				api.QUERY_set_audio_sample(null);
			else
				api.QUERY_set_audio_sample(soundcb);

			bool resetSignal = Controller["Reset"];
			if (resetSignal) api.CMD_reset();

			bool powerSignal = Controller["Power"];
			if (powerSignal) api.CMD_power();

			//too many messages
			api.QUERY_set_layer_enable(0, 0, Settings.ShowBG1_0);
			api.QUERY_set_layer_enable(0, 1, Settings.ShowBG1_1);
			api.QUERY_set_layer_enable(1, 0, Settings.ShowBG2_0);
			api.QUERY_set_layer_enable(1, 1, Settings.ShowBG2_1);
			api.QUERY_set_layer_enable(2, 0, Settings.ShowBG3_0);
			api.QUERY_set_layer_enable(2, 1, Settings.ShowBG3_1);
			api.QUERY_set_layer_enable(3, 0, Settings.ShowBG4_0);
			api.QUERY_set_layer_enable(3, 1, Settings.ShowBG4_1);
			api.QUERY_set_layer_enable(4, 0, Settings.ShowOBJ_0);
			api.QUERY_set_layer_enable(4, 1, Settings.ShowOBJ_1);
			api.QUERY_set_layer_enable(4, 2, Settings.ShowOBJ_2);
			api.QUERY_set_layer_enable(4, 3, Settings.ShowOBJ_3);

			RefreshMemoryCallbacks(false);

			//apparently this is one frame?
			timeFrameCounter++;
			api.CMD_run();

			while (api.QUERY_HasMessage)
				Console.WriteLine(api.QUERY_DequeueMessage());

			if (IsLagFrame)
				LagCount++;

			//diagnostics for IPC traffic
			//Console.WriteLine(api.MessageCounter);

			api.EndBufferIO();
		}

		void RefreshMemoryCallbacks(bool suppress)
		{
			var mcs = MemoryCallbacks;
			api.QUERY_set_state_hook_exec(!suppress && mcs.HasExecutes);
			api.QUERY_set_state_hook_read(!suppress && mcs.HasReads);
			api.QUERY_set_state_hook_write(!suppress && mcs.HasWrites);
		}

		public DisplayType Region
		{
			get
			{
				if (api.QUERY_get_region() == LibsnesApi.SNES_REGION.NTSC)
					return DisplayType.NTSC;
				else
					return DisplayType.PAL;
			}
		}

		//video provider
		int IVideoProvider.BackgroundColor { get { return 0; } }
		int[] IVideoProvider.GetVideoBuffer() { return vidBuffer; }
		int IVideoProvider.VirtualWidth { get { return (int)(vidWidth * 1.146); } }
		public int VirtualHeight { get { return vidHeight; } }
		int IVideoProvider.BufferWidth { get { return vidWidth; } }
		int IVideoProvider.BufferHeight { get { return vidHeight; } }

		int[] vidBuffer = new int[256 * 224];
		int vidWidth = 256, vidHeight = 224;

		public ControllerDefinition ControllerDefinition { get { return SNESController; } }
		IController controller;
		public IController Controller
		{
			get { return controller; }
			set { controller = value; }
		}

		public static readonly ControllerDefinition SNESController =
			new ControllerDefinition
			{
				Name = "SNES Controller",
				BoolButtons = {
					"Reset", "Power",
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Select", "P1 Start", "P1 Y", "P1 B", "P1 X", "P1 A", "P1 L", "P1 R",
					"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Select", "P2 Start", "P2 Y", "P2 B", "P2 X", "P2 A", "P2 L", "P2 R",

					// adelikat: disabling these since they aren't hooked up
					// "P3 Up", "P3 Down", "P3 Left", "P3 Right", "P3 Select", "P3 Start", "P3 Y", "P3 B", "P3 X", "P3 A", "P3 L", "P3 R",
					// "P4 Up", "P4 Down", "P4 Left", "P4 Right", "P4 Select", "P4 Start", "P4 Y", "P4 B", "P4 X", "P4 A", "P4 L", "P4 R",
				}
			};

		int timeFrameCounter;
		public int Frame { get { return timeFrameCounter; } set { timeFrameCounter = value; } }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public string SystemId { get; private set; }

		public string BoardName { get; private set; }

		// adelikat: Nasty hack to force new business logic.  Compatibility (and Accuracy when fully supported) will ALWAYS be in deterministic mode,
		// a consequence is a permanent performance hit to the compatibility core
		// Perormance will NEVER be in deterministic mode (and the client side logic will prohibit movie recording on it)
		// feos: Nasty hack to a nasty hack. Allow user disable it with a strong warning.
		public bool DeterministicEmulation
		{
			get
			{
				return Settings.ForceDeterminism &&
				(CurrentProfile == "Compatibility" || CurrentProfile == "Accuracy");
			}
			private set {  /* Do nothing */ }
		}

		public bool SaveRamModified
		{
			get
			{
				return api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM) != 0 || api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.SGB_CARTRAM) != 0;
			}
		}

		public byte[] CloneSaveRam()
		{
			byte* buf = api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM);
			var size = api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM);
			if (buf == null)
			{
				buf = api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.SGB_CARTRAM);
				size = api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.SGB_CARTRAM);
			}
			var ret = new byte[size];
			Marshal.Copy((IntPtr)buf, ret, 0, size);
			return ret;
		}

		//public byte[] snes_get_memory_data_read(LibsnesApi.SNES_MEMORY id)
		//{
		//  var size = (int)api.snes_get_memory_size(id);
		//  if (size == 0) return new byte[0];
		//  var ret = api.snes_get_memory_data(id);
		//  return ret;
		//}

		public void StoreSaveRam(byte[] data)
		{
			byte* buf = api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM);
			var size = api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM);
			if (buf == null)
			{
				buf = api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.SGB_CARTRAM);
				size = api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.SGB_CARTRAM);
			}
			if (size == 0) return;
			if (size != data.Length) throw new InvalidOperationException("Somehow, we got a mismatch between saveram size and what bsnes says the saveram size is");
			Marshal.Copy(data, 0, (IntPtr)buf, size);
		}

		public void ResetCounters()
		{
			timeFrameCounter = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

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
			public ControllerDefinition Type
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
				this.def = source.Type;
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


		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
			writer.WriteLine("Frame {0}", Frame); // we don't parse this, it's only for the client to use
			writer.WriteLine("Profile {0}", CurrentProfile);
		}
		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
			reader.ReadLine(); // Frame #
			var profile = reader.ReadLine().Split(' ')[1];
			ValidateLoadstateProfile(profile);
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			if (!DeterministicEmulation)
				writer.Write(CoreSaveState());
			else
				writer.Write(savestatebuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
			writer.Write(CurrentProfile);

			writer.Flush();
		}
		public void LoadStateBinary(BinaryReader reader)
		{
			int size = api.QUERY_serialize_size();
			byte[] buf = reader.ReadBytes(size);
			CoreLoadState(buf);

			if (DeterministicEmulation) // deserialize controller and fast-foward now
			{
				// reconstruct savestatebuff at the same time to avoid a costly core serialize
				MemoryStream ms = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(ms);
				bw.Write(buf);
				bool framezero = reader.ReadBoolean();
				bw.Write(framezero);
				if (!framezero)
				{
					SnesSaveController ssc = new SnesSaveController(ControllerDefinition);
					ssc.DeSerialize(reader);
					IController tmp = this.Controller;
					this.Controller = ssc;
					nocallbacks = true;
					FrameAdvance(false, false);
					nocallbacks = false;
					this.Controller = tmp;
					ssc.Serialize(bw);
				}
				else // hack: dummy controller info
				{
					bw.Write(reader.ReadBytes(536));
				}
				bw.Close();
				savestatebuff = ms.ToArray();
			}

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			var profile = reader.ReadString();
			ValidateLoadstateProfile(profile);
		}

		void ValidateLoadstateProfile(string profile)
		{
			if (profile != CurrentProfile)
			{
				throw new InvalidOperationException(string.Format("You've attempted to load a savestate made using a different SNES profile ({0}) than your current configuration ({1}). We COULD automatically switch for you, but we havent done that yet. This error is to make sure you know that this isnt going to work right now.", profile, CurrentProfile));
			}
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		public bool BinarySaveStatesPreferred { get { return true; } }

		/// <summary>
		/// handle the unmanaged part of loadstating
		/// </summary>
		void CoreLoadState(byte[] data)
		{
			int size = api.QUERY_serialize_size();
			if (data.Length != size)
				throw new Exception("Libsnes internal savestate size mismatch!");
			api.CMD_init();
			//zero 01-sep-2014 - this approach isn't being used anymore, it's too slow!
			//LoadCurrent(); //need to make sure chip roms are reloaded
			fixed (byte* pbuf = &data[0])
				api.CMD_unserialize(new IntPtr(pbuf), size);
		}


		/// <summary>
		/// handle the unmanaged part of savestating
		/// </summary>
		byte[] CoreSaveState()
		{
			int size = api.QUERY_serialize_size();
			byte[] buf = new byte[size];
			fixed (byte* pbuf = &buf[0])
				api.CMD_serialize(new IntPtr(pbuf), size);
			return buf;
		}

		/// <summary>
		/// most recent internal savestate, for deterministic mode ONLY
		/// </summary>
		byte[] savestatebuff;

		#endregion

		public CoreComm CoreComm { get; private set; }

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

		unsafe void MakeFakeBus()
		{
			int size = api.QUERY_get_memory_size(LibsnesApi.SNES_MEMORY.WRAM);
			if (size != 0x20000)
				throw new InvalidOperationException();

			byte* blockptr = api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.WRAM);

			var md = new MemoryDomainDelegate("System Bus", 0x1000000, MemoryDomain.Endian.Little,
				(addr) =>
				{
					var a = FakeBusMap((int)addr);
					if (a.HasValue)
						return blockptr[a.Value];
					else
						return FakeBusRead((int)addr);
				},
				(addr, val) =>
				{
					var a = FakeBusMap((int)addr);
					if (a.HasValue)
						blockptr[a.Value] = val;
				}, wordSize: 2);
			_memoryDomains.Add(md);
		}


		// ----- Client Debugging API stuff -----
		unsafe MemoryDomain MakeMemoryDomain(string name, LibsnesApi.SNES_MEMORY id, MemoryDomain.Endian endian, int byteSize = 1)
		{
			int size = api.QUERY_get_memory_size(id);
			int mask = size - 1;
			bool pow2 = Util.IsPowerOfTwo(size);

			//if this type of memory isnt available, dont make the memory domain (most commonly save ram)
			if (size == 0)
				return null;

			byte* blockptr = api.QUERY_get_memory_data(id);

			MemoryDomain md;

			if(id == LibsnesApi.SNES_MEMORY.OAM)
			{
				//OAM is actually two differently sized banks of memory which arent truly considered adjacent. 
				//maybe a better way to visualize it is with an empty bus and adjacent banks
				//so, we just throw away everything above its size of 544 bytes
				if (size != 544) throw new InvalidOperationException("oam size isnt 544 bytes.. wtf?");
				md = new MemoryDomainDelegate(name, size, endian,
				   (addr) => (addr < 544) ? blockptr[addr] : (byte)0x00,
					 (addr, value) => { if (addr < 544) blockptr[addr] = value; },
					 byteSize);
			}
			else if(pow2)
				md = new MemoryDomainDelegate(name, size, endian,
						(addr) => blockptr[addr & mask],
						(addr, value) => blockptr[addr & mask] = value, byteSize);
			else
				md = new MemoryDomainDelegate(name, size, endian,
						(addr) => blockptr[addr % size],
						(addr, value) => blockptr[addr % size] = value, byteSize);

			_memoryDomains.Add(md);

			return md;
		}

		void SetupMemoryDomains(byte[] romData, byte[] sgbRomData)
		{
			//lets just do this entirely differently for SGB
			if (IsSGB)
			{
				//NOTE: CGB has 32K of wram, and DMG has 8KB of wram. Not sure how to control this right now.. bsnes might not have any ready way of doign that? I couldnt spot it. 
				//You wouldnt expect a DMG game to access excess wram, but what if it tried to? maybe an oversight in bsnes?
				MakeMemoryDomain("SGB WRAM", LibsnesApi.SNES_MEMORY.SGB_WRAM, MemoryDomain.Endian.Little);

				var romDomain = new MemoryDomainByteArray("SGB CARTROM", MemoryDomain.Endian.Little, romData, true, 1);
				_memoryDomains.Add(romDomain);
		
				//the last 1 byte of this is special.. its an interrupt enable register, instead of ram. weird. maybe its actually ram and just getting specially used?
				MakeMemoryDomain("SGB HRAM", LibsnesApi.SNES_MEMORY.SGB_HRAM, MemoryDomain.Endian.Little);

				MakeMemoryDomain("SGB CARTRAM", LibsnesApi.SNES_MEMORY.SGB_CARTRAM, MemoryDomain.Endian.Little);

				MainMemory = MakeMemoryDomain("WRAM", LibsnesApi.SNES_MEMORY.WRAM, MemoryDomain.Endian.Little);

				var sgbromDomain = new MemoryDomainByteArray("SGB.SFC ROM", MemoryDomain.Endian.Little, sgbRomData, true, 1);
				_memoryDomains.Add(sgbromDomain);
			}
			else
			{
				MainMemory = MakeMemoryDomain("WRAM", LibsnesApi.SNES_MEMORY.WRAM, MemoryDomain.Endian.Little);


				MakeMemoryDomain("CARTROM", LibsnesApi.SNES_MEMORY.CARTRIDGE_ROM, MemoryDomain.Endian.Little, byteSize: 2);
				MakeMemoryDomain("CARTRAM", LibsnesApi.SNES_MEMORY.CARTRIDGE_RAM, MemoryDomain.Endian.Little, byteSize: 2);
				MakeMemoryDomain("VRAM", LibsnesApi.SNES_MEMORY.VRAM, MemoryDomain.Endian.Little, byteSize: 2);
				MakeMemoryDomain("OAM", LibsnesApi.SNES_MEMORY.OAM, MemoryDomain.Endian.Little, byteSize: 2);
				MakeMemoryDomain("CGRAM", LibsnesApi.SNES_MEMORY.CGRAM, MemoryDomain.Endian.Little, byteSize: 2);
				MakeMemoryDomain("APURAM", LibsnesApi.SNES_MEMORY.APURAM, MemoryDomain.Endian.Little, byteSize: 2);

				if (!DeterministicEmulation)
				{
					_memoryDomains.Add(new MemoryDomainDelegate("System Bus", 0x1000000, MemoryDomain.Endian.Little,
						(addr) => api.QUERY_peek(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr),
						(addr, val) => api.QUERY_poke(LibsnesApi.SNES_MEMORY.SYSBUS, (uint)addr, val), wordSize: 2));
				}
				else
				{
					// limited function bus
					MakeFakeBus();
				}
			}

			MemoryDomains = new MemoryDomainList(_memoryDomains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}

		private MemoryDomain MainMemory;
		private List<MemoryDomain> _memoryDomains = new List<MemoryDomain>();
		private IMemoryDomains MemoryDomains;

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

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return resampler; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		#endregion audio stuff

		void RefreshPalette()
		{
			SetPalette((SnesColors.ColorType)Enum.Parse(typeof(SnesColors.ColorType), Settings.Palette, false));

		}

		SnesSettings Settings;
		SnesSyncSettings SyncSettings;

		public SnesSettings GetSettings() { return Settings.Clone(); }
		public SnesSyncSettings GetSyncSettings() { return SyncSettings.Clone(); }
		public bool PutSettings(SnesSettings o)
		{
			bool refreshneeded = o.Palette != Settings.Palette;
			Settings = o;
			if (refreshneeded)
				RefreshPalette();
			return false;
		}
		public bool PutSyncSettings(SnesSyncSettings o)
		{
			bool ret = o.Profile != SyncSettings.Profile;
			SyncSettings = o;
			return ret;
		}

		public class SnesSettings
		{
			public bool ShowBG1_0 = true;
			public bool ShowBG2_0 = true;
			public bool ShowBG3_0 = true;
			public bool ShowBG4_0 = true;
			public bool ShowBG1_1 = true;
			public bool ShowBG2_1 = true;
			public bool ShowBG3_1 = true;
			public bool ShowBG4_1 = true;
			public bool ShowOBJ_0 = true;
			public bool ShowOBJ_1 = true;
			public bool ShowOBJ_2 = true;
			public bool ShowOBJ_3 = true;

			public bool UseRingBuffer = true;
			public bool AlwaysDoubleSize = false;
			public bool ForceDeterminism = true;
			public string Palette = "BizHawk";

			public SnesSettings Clone()
			{
				return (SnesSettings)MemberwiseClone();
			}
		}

		public class SnesSyncSettings
		{
			public string Profile = "Performance"; // "Accuracy", and "Compatibility" are the other choicec, todo: make this an enum

			public SnesSyncSettings Clone()
			{
				return (SnesSyncSettings)MemberwiseClone();
			}
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
