using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	/// <summary>
	/// a gameboy/gameboy color emulator wrapped around native C++ libgambatte
	/// </summary>
	[PortedCore(CoreNames.Gambatte, "", "Gambatte-Speedrun r717+", "https://github.com/pokemon-speedrunning/gambatte-speedrun")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class Gameboy : IInputPollable, IRomInfo, IGameboyCommon, ICycleTiming, ILinkable
	{
		[CoreConstructor(VSystemID.Raw.GB)]
		[CoreConstructor(VSystemID.Raw.GBC)]
		[CoreConstructor(VSystemID.Raw.SGB)]
		public Gameboy(CoreComm comm, GameInfo game, byte[] file, GambatteSettings settings, GambatteSyncSettings syncSettings, bool deterministic)
		{
			var ser = new BasicServiceProvider(this);
			ser.Register<IDisassemblable>(_disassembler);
			ServiceProvider = ser;
			const string TRACE_HEADER = "LR35902: PC, opcode, registers (A, F, B, C, D, E, H, L, LY, SP, CY)";
			Tracer = new TraceBuffer(TRACE_HEADER);
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
				_syncSettings = syncSettings ?? new GambatteSyncSettings();

				LibGambatte.LoadFlags flags = LibGambatte.LoadFlags.READONLY_SAV;

				switch (_syncSettings.ConsoleMode)
				{
					case GambatteSyncSettings.ConsoleModeType.GB:
						break;
					case GambatteSyncSettings.ConsoleModeType.GBC:
						flags |= LibGambatte.LoadFlags.CGB_MODE;
						break;
					case GambatteSyncSettings.ConsoleModeType.GBA:
						flags |= LibGambatte.LoadFlags.CGB_MODE | LibGambatte.LoadFlags.GBA_FLAG;
						break;
					default:
						if (game.System == VSystemID.Raw.GBC)
							flags |= LibGambatte.LoadFlags.CGB_MODE;
						break;
				}

				if (game.System == VSystemID.Raw.SGB)
				{
					flags &= ~(LibGambatte.LoadFlags.CGB_MODE | LibGambatte.LoadFlags.GBA_FLAG);
					flags |= LibGambatte.LoadFlags.SGB_MODE;
					IsSgb = true;
				}

				if (_syncSettings.MulticartCompat)
				{
					flags |= LibGambatte.LoadFlags.MULTICART_COMPAT;
				}

				IsCgb = (flags & LibGambatte.LoadFlags.CGB_MODE) == LibGambatte.LoadFlags.CGB_MODE;
				if (_syncSettings.EnableBIOS)
				{
					FirmwareID fwid = new(
						IsCgb ? "GBC" : "GB",
						IsSgb
							? "SGB2"
							: _syncSettings.ConsoleMode is GambatteSyncSettings.ConsoleModeType.GBA
								? "AGB"
								: "World");
					var bios = comm.CoreFileProvider.GetFirmwareOrThrow(fwid, "BIOS Not Found, Cannot Load.  Change SyncSettings to run without BIOS."); // https://github.com/TASEmulators/BizHawk/issues/2832 tho
					if (LibGambatte.gambatte_loadbiosbuf(GambatteState, bios, (uint)bios.Length) != 0)
					{
						throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_loadbiosbuf)}() returned non-zero (bios error)");
					}
				}
				else
				{
					if (DeterministicEmulation) // throw a warning if a movie is being recorded with the bios disabled
					{
						comm.ShowMessage("Detected disabled BIOS during movie recording. It is recommended to use a BIOS for movie recording. Change Sync Settings to run with a BIOS.");
					}
					flags |= LibGambatte.LoadFlags.NO_BIOS;
				}

				if (LibGambatte.gambatte_loadbuf(GambatteState, file, (uint)file.Length, flags) != 0)
				{
					throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_loadbuf)}() returned non-zero (is this not a gb or gbc rom?)");
				}

				if (IsSgb)
				{
					ResetStallTicks = 128 * (2 << 14);
				}
				else if (_syncSettings.EnableBIOS && (_syncSettings.ConsoleMode is GambatteSyncSettings.ConsoleModeType.GBA))
				{
					ResetStallTicks = 485808; // GBA takes 971616 cycles to switch to CGB mode; CGB CPU is inactive during this time.
				}
				else
				{
					ResetStallTicks = 0;
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

				RomDetails = $"{game.Name}\r\n{SHA1Checksum.ComputePrefixedHex(file)}\r\n{MD5Checksum.ComputePrefixedHex(file)}\r\n";

				byte[] buff = new byte[32];
				LibGambatte.gambatte_romtitle(GambatteState, buff);
				string romname = System.Text.Encoding.ASCII.GetString(buff);
				Console.WriteLine("Core reported rom name: {0}", romname);

				if (!_syncSettings.EnableBIOS && IsCgb && IsCGBDMGMode()) // without a bios, we need to set the palette for cgbdmg ourselves
				{
					int[] cgbDmgColors = new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					if (file[0x14B] == 0x01 || (file[0x14B] == 0x33 && file[0x144] == '0' && file[0x145] == '1')) // Nintendo licencees get special palettes
					{
						cgbDmgColors = ColorsFromTitleHash(file);
					}
					ChangeDMGColors(cgbDmgColors);
				}

				if (!DeterministicEmulation && _syncSettings.RealTimeRTC)
				{
					LibGambatte.gambatte_settimemode(GambatteState, false);
				}

				if (DeterministicEmulation)
				{
					int[] rtcRegs = new int[11];
					rtcRegs[(int)LibGambatte.RtcRegIndices.Dh] = 0;
					if (_syncSettings.InternalRTCOverflow)
					{
						rtcRegs[(int)LibGambatte.RtcRegIndices.Dh] |= 0x80;
					}
					if (_syncSettings.InternalRTCHalt)
					{
						rtcRegs[(int)LibGambatte.RtcRegIndices.Dh] |= 0x40;
					}
					rtcRegs[(int)LibGambatte.RtcRegIndices.Dh] |= _syncSettings.InternalRTCDays >> 8;
					rtcRegs[(int)LibGambatte.RtcRegIndices.Dl] = _syncSettings.InternalRTCDays & 0xFF;
					rtcRegs[(int)LibGambatte.RtcRegIndices.H] = (_syncSettings.InternalRTCHours < 0) ? (_syncSettings.InternalRTCHours + 0x20) : _syncSettings.InternalRTCHours;
					rtcRegs[(int)LibGambatte.RtcRegIndices.M] = (_syncSettings.InternalRTCMinutes < 0) ? (_syncSettings.InternalRTCMinutes + 0x40) : _syncSettings.InternalRTCMinutes;
					rtcRegs[(int)LibGambatte.RtcRegIndices.S] = (_syncSettings.InternalRTCSeconds < 0) ? (_syncSettings.InternalRTCSeconds + 0x40) : _syncSettings.InternalRTCSeconds;
					rtcRegs[(int)LibGambatte.RtcRegIndices.C] = _syncSettings.InternalRTCCycles;
					rtcRegs[(int)LibGambatte.RtcRegIndices.Dh_L] = 0;
					if (_syncSettings.LatchedRTCOverflow)
					{
						rtcRegs[(int)LibGambatte.RtcRegIndices.Dh_L] |= 0x80;
					}
					if (_syncSettings.LatchedRTCHalt)
					{
						rtcRegs[(int)LibGambatte.RtcRegIndices.Dh_L] |= 0x40;
					}
					rtcRegs[(int)LibGambatte.RtcRegIndices.Dh_L] |= _syncSettings.LatchedRTCDays >> 8;
					rtcRegs[(int)LibGambatte.RtcRegIndices.Dl_L] = _syncSettings.LatchedRTCDays & 0xFF;
					rtcRegs[(int)LibGambatte.RtcRegIndices.H_L] = _syncSettings.LatchedRTCHours;
					rtcRegs[(int)LibGambatte.RtcRegIndices.M_L] = _syncSettings.LatchedRTCMinutes;
					rtcRegs[(int)LibGambatte.RtcRegIndices.S_L] = _syncSettings.LatchedRTCSeconds;
					LibGambatte.gambatte_setrtcregs(GambatteState, rtcRegs);
				}

				LibGambatte.gambatte_setrtcdivisoroffset(GambatteState, _syncSettings.RTCDivisorOffset);

				LibGambatte.gambatte_setcartbuspulluptime(GambatteState, (uint)_syncSettings.CartBusPullUpTime);

				_cdCallback = new LibGambatte.CDCallback(CDCallbackProc);

				ControllerDefinition = CreateControllerDefinition(sgb: IsSgb, sub: _syncSettings.FrameLength is GambatteSyncSettings.FrameLengthType.UserDefinedFrames, tilt: false);

				NewSaveCoreSetBuff();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private readonly GBDisassembler _disassembler = new();

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
		/// number of reset stall ticks
		/// </summary>
		private uint ResetStallTicks { get; set; } = 0;

		/// <summary>
		/// keep a copy of the input callback delegate so it doesn't get GCed
		/// </summary>
		private readonly LibGambatte.InputGetter InputCallback;

		/// <summary>
		/// whatever keys are currently depressed
		/// </summary>
		private LibGambatte.Buttons CurrentButtons = 0;

		/// <summary>
		/// internal gambatte state
		/// </summary>
		internal IntPtr GambatteState { get; private set; } = IntPtr.Zero;

		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public bool IsCgb { get; set; }
		public bool IsSgb { get; set; }

		// all cycle counts are relative to a 2*1024*1024 hz refclock

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

		public static ControllerDefinition CreateControllerDefinition(bool sgb, bool sub, bool tilt)
		{
			var ret = new ControllerDefinition((sub ? "Subframe " : "") + "Gameboy Controller" + (tilt ? " + Tilt" : ""));
			if (sub)
			{
				ret.AddAxis("Input Length", 0.RangeTo(35112), 35112);
			}
			if (tilt)
			{
				ret.AddXYPair($"Tilt {{0}}", AxisPairOrientation.RightAndUp, (-90).RangeTo(90), 0);
			}
			if (sgb)
			{
				for (int i = 0; i < 4; i++)
				{
					ret.BoolButtons.AddRange(
						new[] { "Up", "Down", "Left", "Right", "Start", "Select", "B", "A" }
							.Select(s => $"P{i + 1} {s}"));
				}
				ret.BoolButtons.Add("Power");
			}
			else
			{
				ret.BoolButtons.AddRange(new[] { "Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Power" });
			}
			return ret.MakeImmutable();
		}

		private LibGambatte.Buttons ControllerCallback()
		{
			InputCallbacks.Call();
			IsLagFrame = false;
			if (IsSgb)
			{
				int index = LibGambatte.gambatte_getjoypadindex(GambatteState);
				uint b = (uint)CurrentButtons;
				b >>= index * 8;
				b &= 0xFF;
				if ((b & 0x30) == 0x30) // snes software side blocks l+r
				{
					b &= ~0x30u;
				}
				if ((b & 0xC0) == 0xC0) // same for u+d
				{
					b &= ~0xC0u;
				}
				return (LibGambatte.Buttons)b;
			}
			else
			{
				return CurrentButtons;
			}
		}

		/// <summary>
		/// true if the emulator is currently emulating CGB
		/// </summary>
		public bool IsCGBMode()
		{
			//return LibGambatte.gambatte_iscgb(GambatteState);
			return IsCgb;
		}
		
		/// <summary>
		/// true if the emulator is currently emulating CGB in DMG compatibility mode (NOTE: this mode does not take affect until the bootrom unmaps itself)
		/// </summary>
		public bool IsCGBDMGMode()
		{
			return LibGambatte.gambatte_iscgbdmg(GambatteState);
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

		// needs to match the reverse order of Libgambatte's button enum
		private static readonly IReadOnlyList<string> GB_BUTTON_ORDER_IN_BITMASK = new[] { "Down", "Up", "Left", "Right", "Start", "Select", "B", "A" };

		// input callback assumes buttons are ordered from first player in lsbs to last player in msbs
		private static readonly IReadOnlyList<string> SGB_BUTTON_ORDER_IN_BITMASK = new[] {
			"P4 Down", "P4 Up", "P4 Left", "P4 Right", "P4 Start", "P4 Select", "P4 B", "P4 A",
			"P3 Down", "P3 Up", "P3 Left", "P3 Right", "P3 Start", "P3 Select", "P3 B", "P3 A",
			"P2 Down", "P2 Up", "P2 Left", "P2 Right", "P2 Start", "P2 Select", "P2 B", "P2 A",
			"P1 Down", "P1 Up", "P1 Left", "P1 Right", "P1 Start", "P1 Select", "P1 B", "P1 A" };

		internal void FrameAdvancePrep(IController controller)
		{
			// update our local copy of the controller data
			uint b = 0;
			if (IsSgb)
			{
				for (var i = 0; i < 32; i++)
				{
					b <<= 1;
					if (controller.IsPressed(SGB_BUTTON_ORDER_IN_BITMASK[i])) b |= 1;
				}
			}
			else
			{
				for (var i = 0; i < 8; i++)
				{
					b <<= 1;
					if (controller.IsPressed(GB_BUTTON_ORDER_IN_BITMASK[i])) b |= 1;
				}
			}
			CurrentButtons = (LibGambatte.Buttons)b;

			// the controller callback will set this to false if it actually gets called during the frame
			IsLagFrame = true;

			if (controller.IsPressed("Power"))
			{
				LibGambatte.gambatte_reset(GambatteState, ResetStallTicks);
			}

			if (Tracer.IsEnabled())
			{
				_tracecb = MakeTrace;
			}
			else
			{
				_tracecb = null;
			}

			LibGambatte.gambatte_settracecallback(GambatteState, _tracecb);

			LibGambatte.gambatte_setlayers(GambatteState, (_syncSettings.DisplayBG ? 1 : 0) | (_syncSettings.DisplayOBJ ? 2 : 0) | (_syncSettings.DisplayWindow ? 4 : 0));
		}

		internal void FrameAdvancePost()
		{
			if (IsLagFrame)
			{
				LagCount++;
			}

			Frame++;

			endofframecallback?.Invoke(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
		}


		private static string MapperName(byte[] romdata)
		{
			return (romdata[0x147]) switch
			{
				0x00 => "Plain ROM",
				0x01 => "MBC1 ROM",
				0x02 => "MBC1 ROM+RAM",
				0x03 => "MBC1 ROM+RAM+BATTERY",
				0x05 => "MBC2 ROM",
				0x06 => "MBC2 ROM+BATTERY",
				0x08 => "Plain ROM+RAM",
				0x09 => "Plain ROM+RAM+BATTERY",
				0x0B => "MMM01 ROM", // fixme: mmm01's proper header is at the end of the rom!
				0x0C => "MMM01 ROM+RAM",
				0x0D => "MMM01 ROM+BATTERY",
				0x0F => "MBC3 ROM+TIMER+BATTERY",
				0x10 => "MBC3 ROM+TIMER+RAM+BATTERY",
				0x11 => "MBC3 ROM",
				0x12 => "MBC3 ROM+RAM",
				0x13 => "MBC3 ROM+RAM+BATTERY",
				0x19 => "MBC5 ROM",
				0x1A => "MBC5 ROM+RAM",
				0x1B => "MBC5 ROM+RAM+BATTERY",
				0x1C => "MBC5 ROM+RUMBLE",
				0x1D => "MBC5 ROM+RUMBLE+RAM",
				0x1E => "MBC5 ROM+RUMBLE+RAM+BATTERY",
				0xFC => "Pocket Camera ROM+RAM+BATTERY",
				0xFE => "HuC3 ROM+RAM+BATTERY",
				0xFF => "HuC1 ROM+RAM+BATTERY",
				_ => "UNKNOWN",
			};
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

				case 0x0B: break;
				case 0x0C: break;
				case 0x0D: break;

				case 0x0F: break;
				case 0x10: break;
				case 0x11: break;
				case 0x12: break;
				case 0x13: break;

				case 0x19: break;
				case 0x1A: break;
				case 0x1B: break;
				case 0x1C: break; // rumble
				case 0x1D: break; // rumble
				case 0x1E: break; // rumble

				case 0x20: throw new UnsupportedGameException("\"MBC6\" Mapper not supported!");
				case 0x22: throw new UnsupportedGameException("\"MBC7\" Mapper not supported!");

				case 0xFC: break;
				case 0xFD: throw new UnsupportedGameException("\"Bandai TAMA5\" Mapper not supported!");
				case 0xFE: break;
				case 0xFF: break;
				default: throw new UnsupportedGameException($"Unknown mapper: {romdata[0x147]:x2}");
			}
		}

		private static int[] ColorsFromTitleHash(byte[] romdata)
		{
			int titleHash = 0;
			for (int i = 0; i < 16; i++)
			{
				titleHash += romdata[0x134 + i];
			}

			return (titleHash & 0xFF) switch
			{
				0x01 or 0x10 or 0x29 or 0x52 or 0x5D or 0x68 or 0x6D or 0xF6 => new int[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000 },
				0x0C or 0x16 or 0x35 or 0x67 or 0x75 or 0x92 or 0x99 or 0xB7 => new int[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000 },
				0x14 => new int[] { 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x15 or 0xDB => new int[] { 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000 },
				0x17 or 0x8B => new int[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
				0x19 => new int[] { 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x1D => new int[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000 },
				0x34 => new int[] { 0xFFFFFF, 0x7BFF00, 0xB57300, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x36 => new int[] { 0x52DE00, 0xFF8400, 0xFFFF00, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x39 or 0x43 or 0x97 => new int[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
				0x3C => new int[] { 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x3D => new int[] { 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x3E or 0xE0 => new int[] { 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
				0x49 or 0x5C => new int[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000, 0x0000FF, 0xFFFFFF, 0xFFFF7B, 0x0084FF },
				0x4B or 0x90 or 0x9A or 0xBD => new int[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x4E => new int[] { 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFFFF7B, 0x0084FF, 0xFF0000 },
				0x58 => new int[] { 0xFFFFFF, 0xA5A5A5, 0x525252, 0x000000, 0xFFFFFF, 0xA5A5A5, 0x525252, 0x000000, 0xFFFFFF, 0xA5A5A5, 0x525252, 0x000000 },
				0x59 => new int[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
				0x69 or 0xF2 => new int[] { 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
				0x6B => new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
				0x6F => new int[] { 0xFFFFFF, 0xFFCE00, 0x9C6300, 0x000000, 0xFFFFFF, 0xFFCE00, 0x9C6300, 0x000000, 0xFFFFFF, 0xFFCE00, 0x9C6300, 0x000000 },
				0x70 => new int[] { 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x00FF00, 0x318400, 0x004A00, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
				0x71 or 0xFF => new int[] { 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000 },
				0x86 or 0xA8 => new int[] { 0xFFFF9C, 0x94B5FF, 0x639473, 0x003A3A, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x88 => new int[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xA59CFF, 0xFFFF00, 0x006300, 0x000000 },
				0x8C => new int[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000 },
				0x95 => new int[] { 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
				0x9C => new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000 },
				0x9D => new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000 },
				0xA2 or 0xF7 => new int[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
				0xAA => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000 },
				0xC9 => new int[] { 0xFFFFCE, 0x63EFEF, 0x9C8431, 0x5A5A5A, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
				0xCE or 0xD1 or 0xF0 => new int[] { 0x6BFF00, 0xFFFFFF, 0xFF524A, 0x000000, 0xFFFFFF, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000 },
				0xE8 => new int[] { 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF },
				0x0D => (romdata[0x137]) switch
				{
					0x45 => new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000 },
					0x52 => new int[] { 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x18 => (romdata[0x137]) switch
				{
					0x4B => new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x27 => (romdata[0x137]) switch
				{
					0x42 => new int[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000, 0x0000FF, 0xFFFFFF, 0xFFFF7B, 0x0084FF },
					0x4E => new int[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x28 => (romdata[0x137]) switch
				{
					0x41 => new int[] { 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF },
					0x46 => new int[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x46 => (romdata[0x137]) switch
				{
					0x45 => new int[] { 0xB5B5FF, 0xFFFF94, 0xAD5A42, 0x000000, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A },
					0x52 => new int[] { 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFF00, 0xFF0000, 0x630000, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000 },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x61 => (romdata[0x137]) switch
				{
					0x41 => new int[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
					0x45 => new int[] { 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x66 => (romdata[0x137]) switch
				{
					0x45 => new int[] { 0xFFFFFF, 0x7BFF00, 0xB57300, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x6A => (romdata[0x137]) switch
				{
					0x49 => new int[] { 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
					0x4B => new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0xA5 => (romdata[0x137]) switch
				{
					0x41 => new int[] { 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF },
					0x52 => new int[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000 },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0xB3 => (romdata[0x137]) switch
				{
					0x42 => new int[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000, 0x0000FF, 0xFFFFFF, 0xFFFF7B, 0x0084FF },
					0x52 => new int[] { 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
					0x55 => new int[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000 },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0xBF => (romdata[0x137]) switch
				{
					0x20 => new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
					0x43 => new int[] { 0x6BFF00, 0xFFFFFF, 0xFF524A, 0x000000, 0xFFFFFF, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000 },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0xC6 => (romdata[0x137]) switch
				{
					0x41 => new int[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0xD3 => (romdata[0x137]) switch
				{
					0x49 => new int[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
					0x52 => new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000 },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0xF4 => (romdata[0x137]) switch
				{
					0x20 => new int[] { 0xFFFFFF, 0x7BFF00, 0xB57300, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
					0x2D => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
					_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				_ => new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
			};
		}

		public IGPUMemoryAreas LockGPU()
		{
			var _vram = IntPtr.Zero;
			var _bgpal = IntPtr.Zero;
			var _sppal = IntPtr.Zero;
			var _oam = IntPtr.Zero;
			int unused = 0;
			if (!LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.vram, ref _vram, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.bgpal, ref _bgpal, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.sppal, ref _sppal, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.oam, ref _oam, ref unused))
			{
				throw new InvalidOperationException("Unexpected error in gambatte_getmemoryarea");
			}
			return new GPUMemoryAreas
			{
				Vram = _vram,
				Oam = _oam,
				Sppal = _sppal,
				Bgpal = _bgpal,	
			};
		}

		private class GPUMemoryAreas : IGPUMemoryAreas
		{
			public IntPtr Vram { get; init; }

			public IntPtr Oam { get; init; }

			public IntPtr Sppal { get; init; }

			public IntPtr Bgpal { get; init; }

			public void Dispose() {}
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
					callback(LibGambatte.gambatte_cpuread(GambatteState, 0xFF40));
				}
			}
			else if (line >= 0 && line <= 153)
			{
				scanlinecb = () => callback(LibGambatte.gambatte_cpuread(GambatteState, 0xFF40));
				LibGambatte.gambatte_setscanlinecallback(GambatteState, scanlinecb, line);
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(line), "line must be in [0, 153]");
			}
		}

		private GambattePrinter printer;

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
				_linkConnected = true;
			}
			else
			{
				_linkConnected = false;
				if (printer != null) // have no idea how this is ever null???
				{
					printer.Disconnect();
					printer = null;
				}
			}
		}

		private LibGambatte.ScanlineCallback scanlinecb;
		private ScanlineCallback endofframecallback;

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
	}
}
