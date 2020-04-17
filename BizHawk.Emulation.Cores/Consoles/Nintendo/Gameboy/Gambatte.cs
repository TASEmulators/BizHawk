using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	/// <summary>
	/// a gameboy/gameboy color emulator wrapped around native C++ libgambatte
	/// </summary>
	[Core(
		CoreNames.Gambatte,
		"",
		isPorted: true,
		isReleased: true,
		portedVersion: "SVN 344",
		portedUrl: "http://gambatte.sourceforge.net/",
		singleInstance: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class Gameboy : IEmulator, IVideoProvider, ISoundProvider, ISaveRam, IStatable, IInputPollable, ICodeDataLogger,
		IBoardInfo, IRomInfo, IDebuggable, ISettable<Gameboy.GambatteSettings, Gameboy.GambatteSyncSettings>,
		IGameboyCommon, ICycleTiming, ILinkable
	{
		[CoreConstructor(new[] { "GB", "GBC" })]
		public Gameboy(CoreComm comm, GameInfo game, byte[] file, object settings, object syncSettings, bool deterministic)
		{
			var ser = new BasicServiceProvider(this);
			ser.Register<IDisassemblable>(new GBDisassembler());
			ServiceProvider = ser;
			Tracer = new TraceBuffer
			{
				Header = "Z80: PC, opcode, registers (A, B, C, D, E, F, H, L, LY, SP, CY)"
			};
			ser.Register<ITraceable>(Tracer);
			InitMemoryCallbacks();

			ThrowExceptionForBadRom(file);
			BoardName = MapperName(file);

			DeterministicEmulation = deterministic;

			GambatteState = LibGambatte.gambatte_create();

			if (GambatteState == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_create)}() returned null???");
			}

			Console.WriteLine(game.System);

			try
			{
				_syncSettings = (GambatteSyncSettings)syncSettings ?? new GambatteSyncSettings();

				LibGambatte.LoadFlags flags = 0;

				switch (_syncSettings.ConsoleMode)
				{
					case GambatteSyncSettings.ConsoleModeType.GB:
						flags |= LibGambatte.LoadFlags.FORCE_DMG;
						break;
					case GambatteSyncSettings.ConsoleModeType.GBC:
						break;
					default:
						if (game.System == "GB")
							flags |= LibGambatte.LoadFlags.FORCE_DMG;
						break;
				}

				if (_syncSettings.GBACGB)
				{
					flags |= LibGambatte.LoadFlags.GBA_CGB;
				}

				if (_syncSettings.MulticartCompat)
				{
					flags |= LibGambatte.LoadFlags.MULTICART_COMPAT;
				}

				if (LibGambatte.gambatte_load(GambatteState, file, (uint)file.Length, flags) != 0)
				{
					throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_load)}() returned non-zero (is this not a gb or gbc rom?)");
				}

				byte[] Bios;
				if ((flags & LibGambatte.LoadFlags.FORCE_DMG) == LibGambatte.LoadFlags.FORCE_DMG)
				{
					Bios = comm.CoreFileProvider.GetFirmware("GB", "World", true, "BIOS Not Found, Cannot Load");
					IsCgb = false;
				}
				else
				{
					Bios = comm.CoreFileProvider.GetFirmware("GBC", "World", true, "BIOS Not Found, Cannot Load");
					IsCgb = true;
				}

				if (LibGambatte.gambatte_loadbios(GambatteState, Bios, (uint)Bios.Length) != 0)
				{
					throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_loadbios)}() returned non-zero (bios error)");
				}

				// set real default colors (before anyone mucks with them at all)
				PutSettings((GambatteSettings)settings ?? new GambatteSettings());

				InitSound();

				Frame = 0;
				LagCount = 0;
				IsLagFrame = false;

				InputCallback = new LibGambatte.InputGetter(ControllerCallback);

				LibGambatte.gambatte_setinputgetter(GambatteState, InputCallback);

				InitMemoryDomains();

				RomDetails = $"{game.Name}\r\nSHA1:{file.HashSHA1()}\r\nMD5:{file.HashMD5()}\r\n";

				byte[] buff = new byte[32];
				LibGambatte.gambatte_romtitle(GambatteState, buff);
				string romname = System.Text.Encoding.ASCII.GetString(buff);
				Console.WriteLine("Core reported rom name: {0}", romname);

				if (!DeterministicEmulation && _syncSettings.RealTimeRTC)
				{
					LibGambatte.gambatte_settimemode(GambatteState, false);
				}
				LibGambatte.gambatte_setrtcdivisoroffset(GambatteState, _syncSettings.RTCDivisorOffset);

				_cdCallback = new LibGambatte.CDCallback(CDCallbackProc);

				NewSaveCoreSetBuff();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public string RomDetails { get; }

		/// <summary>
		/// the nominal length of one frame
		/// </summary>
		private const uint TICKSINFRAME = 35112;

		/// <summary>
		/// number of ticks per second
		/// </summary>
		private const uint TICKSPERSECOND = 2097152;

		/// <summary>
		/// keep a copy of the input callback delegate so it doesn't get GCed
		/// </summary>
		private LibGambatte.InputGetter InputCallback;

		/// <summary>
		/// whatever keys are currently depressed
		/// </summary>
		private LibGambatte.Buttons CurrentButtons = 0;

		#region ALL SAVESTATEABLE STATE GOES HERE

		/// <summary>
		/// internal gambatte state
		/// </summary>
		internal IntPtr GambatteState { get; private set; } = IntPtr.Zero;

		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public bool IsCgb { get; set; }

		// all cycle counts are relative to a 2*1024*1024 mhz refclock

		/// <summary>
		/// total cycles actually executed
		/// </summary>
		private ulong _cycleCount = 0;
		private ulong callbackCycleCount = 0;

		/// <summary>
		/// number of extra cycles we overran in the last frame
		/// </summary>
		private uint frameOverflow = 0;
		public long CycleCount => (long)_cycleCount;
		public double ClockRate => TICKSPERSECOND;

		#endregion

		#region controller

		public static readonly ControllerDefinition GbController = new ControllerDefinition
		{
			Name = "Gameboy Controller",
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Power"
			}
		};

		private LibGambatte.Buttons ControllerCallback()
		{
			InputCallbacks.Call();
			IsLagFrame = false;
			return CurrentButtons;
		}

		#endregion

		/// <summary>
		/// true if the emulator is currently emulating CGB
		/// </summary>
		public bool IsCGBMode()
		{
			//return LibGambatte.gambatte_iscgb(GambatteState);
			return IsCgb;
		}

		private InputCallbackSystem _inputCallbacks = new InputCallbackSystem();

		// low priority TODO: due to certain aspects of the core implementation,
		// we don't smartly use the ActiveChanged event here.
		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		/// <summary>
		/// for use in dual core
		/// </summary>
		public void ConnectInputCallbackSystem(InputCallbackSystem ics)
		{
			_inputCallbacks = ics;
		}

		internal void FrameAdvancePrep(IController controller)
		{
			Frame++;

			// update our local copy of the controller data
			CurrentButtons = 0;

			if (controller.IsPressed("Up"))
				CurrentButtons |= LibGambatte.Buttons.UP;
			if (controller.IsPressed("Down"))
				CurrentButtons |= LibGambatte.Buttons.DOWN;
			if (controller.IsPressed("Left"))
				CurrentButtons |= LibGambatte.Buttons.LEFT;
			if (controller.IsPressed("Right"))
				CurrentButtons |= LibGambatte.Buttons.RIGHT;
			if (controller.IsPressed("A"))
				CurrentButtons |= LibGambatte.Buttons.A;
			if (controller.IsPressed("B"))
				CurrentButtons |= LibGambatte.Buttons.B;
			if (controller.IsPressed("Select"))
				CurrentButtons |= LibGambatte.Buttons.SELECT;
			if (controller.IsPressed("Start"))
				CurrentButtons |= LibGambatte.Buttons.START;

			// the controller callback will set this to false if it actually gets called during the frame
			IsLagFrame = true;

			if (controller.IsPressed("Power"))
			{
				LibGambatte.gambatte_reset(GambatteState);
			}

			if (Tracer.Enabled)
			{
				_tracecb = MakeTrace;
			}
			else
			{
				_tracecb = null;
			}

			LibGambatte.gambatte_settracecallback(GambatteState, _tracecb);

			LibGambatte.gambatte_setlayers(GambatteState, (_settings.DisplayBG ? 1 : 0) | (_settings.DisplayOBJ ? 2 : 0) | (_settings.DisplayWindow ? 4 : 0));
		}

		internal void FrameAdvancePost()
		{
			if (IsLagFrame)
			{
				LagCount++;
			}

			endofframecallback?.Invoke(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
		}


		private static string MapperName(byte[] romdata)
		{
			switch (romdata[0x147])
			{
				case 0x00: return "Plain ROM"; // = PLAIN; break;
				case 0x01: return "MBC1 ROM"; // = MBC1; break;
				case 0x02: return "MBC1 ROM+RAM"; // = MBC1; break;
				case 0x03: return "MBC1 ROM+RAM+BATTERY"; // = MBC1; break;
				case 0x05: return "MBC2 ROM"; // = MBC2; break;
				case 0x06: return "MBC2 ROM+BATTERY"; // = MBC2; break;
				case 0x08: return "Plain ROM+RAM"; // = PLAIN; break;
				case 0x09: return "Plain ROM+RAM+BATTERY"; // = PLAIN; break;
				case 0x0F: return "MBC3 ROM+TIMER+BATTERY"; // = MBC3; break;
				case 0x10: return "MBC3 ROM+TIMER+RAM+BATTERY"; // = MBC3; break;
				case 0x11: return "MBC3 ROM"; // = MBC3; break;
				case 0x12: return "MBC3 ROM+RAM"; // = MBC3; break;
				case 0x13: return "MBC3 ROM+RAM+BATTERY"; // = MBC3; break;
				case 0x19: return "MBC5 ROM"; // = MBC5; break;
				case 0x1A: return "MBC5 ROM+RAM"; // = MBC5; break;
				case 0x1B: return "MBC5 ROM+RAM+BATTERY"; // = MBC5; break;
				case 0x1C: return "MBC5 ROM+RUMBLE"; // = MBC5; break;
				case 0x1D: return "MBC5 ROM+RUMBLE+RAM"; // = MBC5; break;
				case 0x1E: return "MBC5 ROM+RUMBLE+RAM+BATTERY"; // = MBC5; break;
				case 0xFF: return "HuC1 ROM+RAM+BATTERY"; // = HUC1; break;
				default: return "UNKNOWN";
			}
		}

		/// <summary>
		/// throw exception with intelligible message on some kinds of bad rom
		/// </summary>
		private static void ThrowExceptionForBadRom(byte[] romdata)
		{
			if (romdata.Length < 0x148)
			{
				throw new ArgumentException("ROM is far too small to be a valid GB\\GBC rom!");
			}

			switch (romdata[0x147])
			{
				case 0x00: break;
				case 0x01: break;
				case 0x02: break;
				case 0x03: break;
				case 0x05: break;
				case 0x06: break;
				case 0x08: break;
				case 0x09: break;

				case 0x0b: throw new UnsupportedGameException("\"MM01\" Mapper not supported!");
				case 0x0c: throw new UnsupportedGameException("\"MM01\" Mapper not supported!");
				case 0x0d: throw new UnsupportedGameException("\"MM01\" Mapper not supported!");

				case 0x0f: break;
				case 0x10: break;
				case 0x11: break;
				case 0x12: break;
				case 0x13: break;

				case 0x15: throw new UnsupportedGameException("\"MBC4\" Mapper not supported!");
				case 0x16: throw new UnsupportedGameException("\"MBC4\" Mapper not supported!");
				case 0x17: throw new UnsupportedGameException("\"MBC4\" Mapper not supported!");

				case 0x19: break;
				case 0x1a: break;
				case 0x1b: break;
				case 0x1c: break; // rumble
				case 0x1d: break; // rumble
				case 0x1e: break; // rumble

				case 0x20: throw new UnsupportedGameException("\"MBC6\" Mapper not supported!");
				case 0x22: throw new UnsupportedGameException("\"MBC7\" Mapper not supported!");

				case 0xfc: throw new UnsupportedGameException("\"Pocket Camera\" Mapper not supported!");
				case 0xfd: throw new UnsupportedGameException("\"Bandai TAMA5\" Mapper not supported!");
				case 0xfe: throw new UnsupportedGameException("\"HuC3\" Mapper not supported!");
				case 0xff: break;
				default: throw new UnsupportedGameException($"Unknown mapper: {romdata[0x147]:x2}");
			}
		}

		#region ppudebug

		public IntPtr _vram = IntPtr.Zero;
		public IntPtr _bgpal = IntPtr.Zero;
		public IntPtr _sppal = IntPtr.Zero;
		public IntPtr _oam = IntPtr.Zero;

		public GPUMemoryAreas GetGPU()
		{		
			int unused = 0;
			if (!LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.vram, ref _vram, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.bgpal, ref _bgpal, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.sppal, ref _sppal, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.oam, ref _oam, ref unused))
			{
				throw new InvalidOperationException("Unexpected error in gambatte_getmemoryarea");
			}
			return new GPUMemoryAreas(_vram, _oam, _sppal, _bgpal);
		}

		/// <summary>
		/// set up callback
		/// </summary>
		/// <param name="line">scanline. -1 = end of frame, -2 = RIGHT NOW</param>
		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			if (GambatteState == IntPtr.Zero)
			{
				return; // not sure how this is being reached.  tried the debugger...
			}

			endofframecallback = null;
			if (callback == null || line == -1 || line == -2)
			{
				scanlinecb = null;
				LibGambatte.gambatte_setscanlinecallback(GambatteState, null, 0);
				if (line == -1)
				{
					endofframecallback = callback;
				}
				else if (line == -2)
				{
					callback(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
				}
			}
			else if (line >= 0 && line <= 153)
			{
				scanlinecb = delegate
				{
					callback(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
				};
				LibGambatte.gambatte_setscanlinecallback(GambatteState, scanlinecb, line);
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(line), "line must be in [0, 153]");
			}
		}

		GambattePrinter printer;

		/// <summary>
		/// set up Printer callback
		/// </summary>
		public void SetPrinterCallback(PrinterCallback callback)
		{
			// Copying SetScanlineCallback for this check, I assume this is still a bug somewhere
			if (GambatteState == IntPtr.Zero)
			{
				return; // not sure how this is being reached.  tried the debugger...
			}

			if (callback != null)
			{
				printer = new GambattePrinter(this, callback);
				LinkConnected = true;
			}
			else
			{
				LinkConnected = false;
				printer.Disconnect();
				printer = null;
			}
		}

		LibGambatte.ScanlineCallback scanlinecb;
		ScanlineCallback endofframecallback;

		#endregion

		#region palette

		/// <summary>
		/// update gambatte core's internal colors
		/// </summary>
		public void ChangeDMGColors(int[] colors)
		{
			for (int i = 0; i < 12; i++)
			{
				LibGambatte.gambatte_setdmgpalettecolor(GambatteState, (LibGambatte.PalType)(i / 4), (uint)i % 4, (uint)colors[i]);
			}
		}

		public void SetCGBColors(GBColors.ColorType type)
		{
			int[] lut = GBColors.GetLut(type);
			LibGambatte.gambatte_setcgbpalette(GambatteState, lut);
		}

		#endregion
	}
}
