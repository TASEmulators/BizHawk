using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	[PortedCore(CoreNames.Mgba, "endrift", "0.11", "https://mgba.io/")]
	[ServiceNotApplicable(typeof(IRegionable))]
	public partial class MGBAHawk
	{
		private static readonly LibmGBA LibmGBA;

		static MGBAHawk()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "libmgba.dll.so" : "mgba.dll", hasLimitedLifetime: false);
			LibmGBA = BizInvoker.GetInvoker<LibmGBA>(resolver, CallingConventionAdapters.Native);
		}

		private IntPtr Core;

		[CoreConstructor(VSystemID.Raw.GBA)]
		public MGBAHawk(CoreLoadParameters<Settings, SyncSettings> lp)
		{
			_syncSettings = lp.SyncSettings ?? new();
			_settings = lp.Settings ?? new();

			var bios = lp.Comm.CoreFileProvider.GetFirmware(new("GBA", "Bios"));
			if (bios is { Length: not 0x4000 }) throw new InvalidOperationException("BIOS must be exactly 16384 bytes!");
			if (lp.DeterministicEmulationRequested && bios is null) throw new MissingFirmwareException("A BIOS is required for deterministic recordings!");
			DeterministicEmulation = lp.DeterministicEmulationRequested
				|| (bios is not null && !_syncSettings.RTCUseRealTime); // in this case, the core is deterministic even though it wasn't asked to be
			var rom = lp.Roms[0].FileData;
			var overrides = GetOverrideInfo(_syncSettings);

			Core = LibmGBA.BizCreate(
				bios,
				rom,
				rom.Length,
				ref overrides,
				skipBios: _syncSettings.SkipBios && !lp.DeterministicEmulationRequested);

			// the core might ignore our request to run the bios intro if the game cannot boot with the bios intro: that's okay

			if (Core == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LibmGBA.BizCreate)}() returned NULL!  Bad BIOS? and/or ROM?");
			}

			try
			{
				CreateMemoryDomains(rom.Length);
				var ser = new BasicServiceProvider(this);
				ser.Register<IDisassemblable>(new ArmV4Disassembler());
				ser.Register<IMemoryDomains>(_memoryDomains);

				ServiceProvider = ser;
				PutSettings(_settings);

				const string TRACE_HEADER = "ARM7: PC, machine code, mnemonic, operands, registers";
				Tracer = new TraceBuffer(TRACE_HEADER);
				_tracecb = MakeTrace;
				ser.Register(Tracer);
				_memoryCallbacks = new(LibmGBA, Core);

				// most things are already handled in the core, this is just for event.oninputpoll
				InputCallback = InputCallbacks.Call;
				LibmGBA.BizSetInputCallback(Core, InputCallback);

				RumbleCallback = SetRumble;
				LibmGBA.BizSetRumbleCallback(Core, RumbleCallback);
			}
			catch
			{
				LibmGBA.BizDestroy(Core);
				throw;
			}
		}

		private static LibmGBA.OverrideInfo GetOverrideInfo(SyncSettings syncSettings)
		{
			var ret = new LibmGBA.OverrideInfo
			{
				Savetype = syncSettings.OverrideSaveType,
				Hardware = LibmGBA.Hardware.None,
				IdleLoop = LibmGBA.OverrideInfo.IDLE_LOOP_NONE,
				VbaBugCompat = syncSettings.OverrideVbaBugCompat,
				DetectPokemonRomHacks = syncSettings.OverridePokemonRomhackDetect
			};

			if (syncSettings.OverrideRtc is SyncSettings.HardwareSelection.Autodetect)
			{
				ret.Hardware |= LibmGBA.Hardware.AutodetectRtc;
			}
			else if (syncSettings.OverrideRtc is SyncSettings.HardwareSelection.True)
			{
				ret.Hardware |= LibmGBA.Hardware.Rtc;
			}

			if (syncSettings.OverrideRumble is SyncSettings.HardwareSelection.Autodetect)
			{
				ret.Hardware |= LibmGBA.Hardware.AutodetectRumble;
			}
			else if (syncSettings.OverrideRumble is SyncSettings.HardwareSelection.True)
			{
				ret.Hardware |= LibmGBA.Hardware.Rumble;
			}

			if (syncSettings.OverrideLightSensor is SyncSettings.HardwareSelection.Autodetect)
			{
				ret.Hardware |= LibmGBA.Hardware.AutodetectLightSensor;
			}
			else if (syncSettings.OverrideLightSensor is SyncSettings.HardwareSelection.True)
			{
				ret.Hardware |= LibmGBA.Hardware.LightSensor;
			}

			if (syncSettings.OverrideGyro is SyncSettings.HardwareSelection.Autodetect)
			{
				ret.Hardware |= LibmGBA.Hardware.AutodetectGyro;
			}
			else if (syncSettings.OverrideGyro is SyncSettings.HardwareSelection.True)
			{
				ret.Hardware |= LibmGBA.Hardware.Gyro;
			}

			if (syncSettings.OverrideTilt is SyncSettings.HardwareSelection.Autodetect)
			{
				ret.Hardware |= LibmGBA.Hardware.AutodetectTilt;
			}
			else if (syncSettings.OverrideTilt is SyncSettings.HardwareSelection.True)
			{
				ret.Hardware |= LibmGBA.Hardware.Tilt;
			}

			if (syncSettings.OverrideGbPlayerDetect)
			{
				ret.Hardware |= LibmGBA.Hardware.GbPlayerDetect;
			}

			return ret;
		}

		private static readonly DateTime _epoch = new(1970, 1, 1);

		private long RTCTime()
		{
			if (!DeterministicEmulation && _syncSettings.RTCUseRealTime)
			{
				return (long)DateTime.Now.Subtract(_epoch).TotalSeconds;
			}

			long baseTime = (long)_syncSettings.RTCInitialTime.Subtract(_epoch).TotalSeconds;
			long increment = Frame * 4389L >> 18;
			return baseTime + increment;
		}
	}
}
