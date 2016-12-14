using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	/// <summary>
	/// a gameboy/gameboy color emulator wrapped around native C++ libgambatte
	/// </summary>
	[CoreAttributes(
		"Gambatte",
		"",
		isPorted: true,
		isReleased: true,
		portedVersion: "SVN 344",
		portedUrl: "http://gambatte.sourceforge.net/"
		)]
	[ServiceNotApplicable(typeof(IDriveLight), typeof(IDriveLight))]
	public partial class Gameboy : IEmulator, IVideoProvider, ISoundProvider, ISaveRam, IStatable, IInputPollable, ICodeDataLogger,
		IDebuggable, ISettable<Gameboy.GambatteSettings, Gameboy.GambatteSyncSettings>
	{
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
		LibGambatte.InputGetter InputCallback;

		/// <summary>
		/// whatever keys are currently depressed
		/// </summary>
		LibGambatte.Buttons CurrentButtons = 0;

		#region RTC

		/// <summary>
		/// RTC time when emulation begins.
		/// </summary>
		uint zerotime = 0;

		/// <summary>
		/// if true, RTC will run off of real elapsed time
		/// </summary>
		bool real_rtc_time = false;

		LibGambatte.RTCCallback TimeCallback;

		static long GetUnixNow()
		{
			// because internally the RTC works off of relative time, we don't need to base
			// this off of any particular canonical epoch.
			return DateTime.UtcNow.Ticks / 10000000L - 60000000000L;
		}

		uint GetCurrentTime()
		{
			if (real_rtc_time)
			{
				return (uint)GetUnixNow();
			}
			else
			{
				ulong fn = (ulong)Frame;
				// as we're exactly tracking cpu cycles, this can be pretty accurate
				fn *= 4389;
				fn /= 262144;
				fn += zerotime;
				return (uint)fn;
			}
		}

		uint GetInitialTime()
		{
			if (real_rtc_time)
				return (uint)GetUnixNow();
			else
				// setting the initial boot time to 0 will cause our zerotime
				// to function as an initial offset, which is what we want
				return 0;
		}

		#endregion

		[CoreConstructor("GB", "GBC")]
		public Gameboy(CoreComm comm, GameInfo game, byte[] file, object Settings, object SyncSettings, bool deterministic)
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
			CoreComm = comm;

			comm.VsyncNum = 262144;
			comm.VsyncDen = 4389;
			comm.RomStatusAnnotation = null;
			comm.RomStatusDetails = null;
			comm.NominalWidth = 160;
			comm.NominalHeight = 144;

			ThrowExceptionForBadRom(file);
			BoardName = MapperName(file);

			DeterministicEmulation = deterministic;

			GambatteState = LibGambatte.gambatte_create();

			if (GambatteState == IntPtr.Zero)
				throw new InvalidOperationException("gambatte_create() returned null???");

			try
			{
				this._syncSettings = (GambatteSyncSettings)SyncSettings ?? new GambatteSyncSettings();
				// copy over non-loadflag syncsettings now; they won't take effect if changed later
				zerotime = (uint)this._syncSettings.RTCInitialTime;
				real_rtc_time = DeterministicEmulation ? false : this._syncSettings.RealTimeRTC;

				LibGambatte.LoadFlags flags = 0;

				if (this._syncSettings.ForceDMG)
					flags |= LibGambatte.LoadFlags.FORCE_DMG;
				if (this._syncSettings.GBACGB)
					flags |= LibGambatte.LoadFlags.GBA_CGB;
				if (this._syncSettings.MulticartCompat)
					flags |= LibGambatte.LoadFlags.MULTICART_COMPAT;

				if (LibGambatte.gambatte_load(GambatteState, file, (uint)file.Length, GetCurrentTime(), flags) != 0)
					throw new InvalidOperationException("gambatte_load() returned non-zero (is this not a gb or gbc rom?)");

				// set real default colors (before anyone mucks with them at all)
				PutSettings((GambatteSettings)Settings ?? new GambatteSettings());

				InitSound();

				Frame = 0;
				LagCount = 0;
				IsLagFrame = false;

				InputCallback = new LibGambatte.InputGetter(ControllerCallback);

				LibGambatte.gambatte_setinputgetter(GambatteState, InputCallback);

				InitMemoryDomains();

				CoreComm.RomStatusDetails = string.Format("{0}\r\nSHA1:{1}\r\nMD5:{2}\r\n",
					game.Name,
					file.HashSHA1(),
					file.HashMD5());

				{
					byte[] buff = new byte[32];
					LibGambatte.gambatte_romtitle(GambatteState, buff);
					string romname = System.Text.Encoding.ASCII.GetString(buff);
					Console.WriteLine("Core reported rom name: {0}", romname);
				}

				TimeCallback = new LibGambatte.RTCCallback(GetCurrentTime);
				LibGambatte.gambatte_setrtccallback(GambatteState, TimeCallback);

				CDCallback = new LibGambatte.CDCallback(CDCallbackProc);

				NewSaveCoreSetBuff();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

	
		public IEmulatorServiceProvider ServiceProvider { get; private set; }

        #region ALL SAVESTATEABLE STATE GOES HERE

        /// <summary>
        /// internal gambatte state
        /// </summary>
        internal IntPtr GambatteState = IntPtr.Zero;

        public int Frame { get; set; }
        public int LagCount { get; set; }
        public bool IsLagFrame { get; set; }

        // all cycle counts are relative to a 2*1024*1024 mhz refclock

        /// <summary>
        /// total cycles actually executed
        /// </summary>
        private ulong _cycleCount = 0;

        /// <summary>
        /// number of extra cycles we overran in the last frame
        /// </summary>
        private uint frameOverflow = 0;
        public ulong CycleCount { get { return _cycleCount; } }

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

		public ControllerDefinition ControllerDefinition
		{
			get { return GbController; }
		}

		public IController Controller { get; set; }

		LibGambatte.Buttons ControllerCallback()
		{
			InputCallbacks.Call();
			IsLagFrame = false;
			return CurrentButtons;
		}

		#endregion

		/// <summary>
		/// true if the emulator is currently emulating CGB
		/// </summary>
		/// <returns></returns>
		public bool IsCGBMode()
		{
			return (LibGambatte.gambatte_iscgb(GambatteState));
		}

		private InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
		// low priority TODO: due to certain aspects of the core implementation,
		// we don't smartly use the ActiveChanged event here.
		public IInputCallbackSystem InputCallbacks { get { return _inputCallbacks; } }

		/// <summary>
		/// for use in dual core
		/// </summary>
		/// <param name="ics"></param>
		public void ConnectInputCallbackSystem(InputCallbackSystem ics)
		{
			_inputCallbacks = ics;
		}

		internal void FrameAdvancePrep()
		{
			Frame++;

			// update our local copy of the controller data
			CurrentButtons = 0;

			if (Controller.IsPressed("Up"))
				CurrentButtons |= LibGambatte.Buttons.UP;
			if (Controller.IsPressed("Down"))
				CurrentButtons |= LibGambatte.Buttons.DOWN;
			if (Controller.IsPressed("Left"))
				CurrentButtons |= LibGambatte.Buttons.LEFT;
			if (Controller.IsPressed("Right"))
				CurrentButtons |= LibGambatte.Buttons.RIGHT;
			if (Controller.IsPressed("A"))
				CurrentButtons |= LibGambatte.Buttons.A;
			if (Controller.IsPressed("B"))
				CurrentButtons |= LibGambatte.Buttons.B;
			if (Controller.IsPressed("Select"))
				CurrentButtons |= LibGambatte.Buttons.SELECT;
			if (Controller.IsPressed("Start"))
				CurrentButtons |= LibGambatte.Buttons.START;

			// the controller callback will set this to false if it actually gets called during the frame
			IsLagFrame = true;

			if (Controller.IsPressed("Power"))
				LibGambatte.gambatte_reset(GambatteState, GetCurrentTime());

			if (Tracer.Enabled)
				tracecb = MakeTrace;
			else
				tracecb = null;
			LibGambatte.gambatte_settracecallback(GambatteState, tracecb);

			LibGambatte.gambatte_setlayers(GambatteState, (_settings.DisplayBG ? 1 : 0) | (_settings.DisplayOBJ ? 2 : 0) | (_settings.DisplayWindow ? 4 : 0 ) );
		}

		internal void FrameAdvancePost()
		{
			if (IsLagFrame)
				LagCount++;

			if (endofframecallback != null)
				endofframecallback(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			FrameAdvancePrep();
			if (_syncSettings.EqualLengthFrames)
			{
				while (true)
				{
					// target number of samples to emit: length of 1 frame minus whatever overflow
					uint samplesEmitted = TICKSINFRAME - frameOverflow;
					System.Diagnostics.Debug.Assert(samplesEmitted * 2 <= soundbuff.Length);
					if (LibGambatte.gambatte_runfor(GambatteState, soundbuff, ref samplesEmitted) > 0)
						LibGambatte.gambatte_blitto(GambatteState, VideoBuffer, 160);

					// account for actual number of samples emitted
					_cycleCount += (ulong)samplesEmitted;
					frameOverflow += samplesEmitted;

					if (rendersound && !Muted)
					{
						ProcessSound((int)samplesEmitted);
					}

					if (frameOverflow >= TICKSINFRAME)
					{
						frameOverflow -= TICKSINFRAME;
						break;
					}
				}
			}
			else
			{
				// target number of samples to emit: always 59.7fps
				// runfor() always ends after creating a video frame, so sync-up is guaranteed
				// when the display has been off, some frames can be markedly shorter than expected
				uint samplesEmitted = TICKSINFRAME;
				if (LibGambatte.gambatte_runfor(GambatteState, soundbuff, ref samplesEmitted) > 0)
					LibGambatte.gambatte_blitto(GambatteState, VideoBuffer, 160);

				_cycleCount += (ulong)samplesEmitted;
				frameOverflow = 0;
				if (rendersound && !Muted)
				{
					ProcessSound((int)samplesEmitted);
				}
			}

			if (rendersound && !Muted)
				ProcessSoundEnd();

			FrameAdvancePost();
		}

		static string MapperName(byte[] romdata)
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
		/// <param name="romdata"></param>
		static void ThrowExceptionForBadRom(byte[] romdata)
		{
			if (romdata.Length < 0x148)
				throw new ArgumentException("ROM is far too small to be a valid GB\\GBC rom!");

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
				default: throw new UnsupportedGameException(string.Format("Unknown mapper: {0:x2}", romdata[0x147]));
			}
			return;
		}

		public string SystemId { get { return "GB"; } }

		public string BoardName { get; private set; }

		public bool DeterministicEmulation { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
			// reset frame counters is meant to "re-zero" emulation time wherever it was
			// so these should be reset as well
			_cycleCount = 0;
			frameOverflow = 0;
		}

		public CoreComm CoreComm { get; set; }

		#region ppudebug

		public bool GetGPUMemoryAreas(out IntPtr vram, out IntPtr bgpal, out IntPtr sppal, out IntPtr oam)
		{
			IntPtr _vram = IntPtr.Zero;
			IntPtr _bgpal = IntPtr.Zero;
			IntPtr _sppal = IntPtr.Zero;
			IntPtr _oam = IntPtr.Zero;
			int unused = 0;
			if (!LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.vram, ref _vram, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.bgpal, ref _bgpal, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.sppal, ref _sppal, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.oam, ref _oam, ref unused))
			{
				vram = IntPtr.Zero;
				bgpal = IntPtr.Zero;
				sppal = IntPtr.Zero;
				oam = IntPtr.Zero;
				return false;
			}
			vram = _vram;
			bgpal = _bgpal;
			sppal = _sppal;
			oam = _oam;
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="lcdc">current value of register $ff40 (LCDC)</param>
		public delegate void ScanlineCallback(int lcdc);

		/// <summary>
		/// set up callback
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="line">scanline. -1 = end of frame, -2 = RIGHT NOW</param>
		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			if (GambatteState == IntPtr.Zero)
				// not sure how this is being reached.  tried the debugger...
				return;
			endofframecallback = null;
			if (callback == null || line == -1 || line == -2)
			{
				scanlinecb = null;
				LibGambatte.gambatte_setscanlinecallback(GambatteState, null, 0);
				if (line == -1)
					endofframecallback = callback;
				else if (line == -2)
					callback(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
			}
			else if (line >= 0 && line <= 153)
			{
				scanlinecb = delegate()
				{
					callback(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
				};
				LibGambatte.gambatte_setscanlinecallback(GambatteState, scanlinecb, line);
			}
			else
			{
				throw new ArgumentOutOfRangeException("line", "line must be in [0, 153]");
			}
		}

		LibGambatte.ScanlineCallback scanlinecb;
		ScanlineCallback endofframecallback;

		#endregion

		public void Dispose()
		{
			if (GambatteState != IntPtr.Zero)
			{
				LibGambatte.gambatte_destroy(GambatteState);
				GambatteState = IntPtr.Zero;
			}
			DisposeSound();
		}

		#region palette

		/// <summary>
		/// update gambatte core's internal colors
		/// </summary>
		public void ChangeDMGColors(int[] colors)
		{
			for (int i = 0; i < 12; i++)
				LibGambatte.gambatte_setdmgpalettecolor(GambatteState, (LibGambatte.PalType)(i / 4), (uint)i % 4, (uint)colors[i]);
		}

		public void SetCGBColors(GBColors.ColorType type)
		{
			int[] lut = GBColors.GetLut(type);
			LibGambatte.gambatte_setcgbpalette(GambatteState, lut);
		}

		#endregion
	}
}
