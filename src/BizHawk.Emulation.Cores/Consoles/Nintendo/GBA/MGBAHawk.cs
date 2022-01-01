using System;
using System.Text;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	[PortedCore(CoreNames.Mgba, "endrift", "0.9.1", "https://mgba.io/")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), typeof(IRegionable) })]
	public partial class MGBAHawk : IEmulator, IVideoProvider, ISoundProvider, IGBAGPUViewable,
		ISaveRam, IStatable, IInputPollable, ISettable<MGBAHawk.Settings, MGBAHawk.SyncSettings>,
		IDebuggable
	{
		private static readonly LibmGBA LibmGBA;
		public static LibmGBA ZZHacky => LibmGBA;

		static MGBAHawk()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "libmgba.dll.so" : "mgba.dll", hasLimitedLifetime: false);
			LibmGBA = BizInvoker.GetInvoker<LibmGBA>(resolver, CallingConventionAdapters.Native);
		}

		[CoreConstructor(VSystemID.Raw.GBA)]
		public MGBAHawk(byte[] file, CoreComm comm, SyncSettings syncSettings, Settings settings, bool deterministic, GameInfo game)
		{
			_syncSettings = syncSettings ?? new SyncSettings();
			_settings = settings ?? new Settings();
			DeterministicEmulation = deterministic;

			var bios = comm.CoreFileProvider.GetFirmware(new("GBA", "Bios"));
			DeterministicEmulation &= bios != null;

			if (DeterministicEmulation != deterministic)
			{
				throw new MissingFirmwareException("A BIOS is required for deterministic recordings!");
			}

			if (!DeterministicEmulation && bios != null && !_syncSettings.RTCUseRealTime && !_syncSettings.SkipBios)
			{
				// in these situations, this core is deterministic even though it wasn't asked to be
				DeterministicEmulation = true;
			}

			if (bios != null && bios.Length != 16384)
			{
				throw new InvalidOperationException("BIOS must be exactly 16384 bytes!");
			}

			var skipBios = !DeterministicEmulation && _syncSettings.SkipBios;

			Core = LibmGBA.BizCreate(bios, file, file.Length, GetOverrideInfo(_syncSettings), skipBios);
			if (Core == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LibmGBA.BizCreate)}() returned NULL!  Bad BIOS? and/or ROM?");
			}

			try
			{
				CreateMemoryDomains(file.Length);
				var ser = new BasicServiceProvider(this);
				ser.Register<IDisassemblable>(new ArmV4Disassembler());
				ser.Register<IMemoryDomains>(_memoryDomains);

				ServiceProvider = ser;
				PutSettings(_settings);

				const string TRACE_HEADER = "ARM7: PC, machine code, mnemonic, operands, registers";
				Tracer = new TraceBuffer(TRACE_HEADER);
				_tracecb = msg => Tracer.Put(_traceInfo(msg));
				ser.Register(Tracer);
				MemoryCallbacks = new MGBAMemoryCallbackSystem(this);
			}
			catch
			{
				LibmGBA.BizDestroy(Core);
				throw;
			}

			InputCallback = new LibmGBA.InputCallback(InputCb);
			LibmGBA.BizSetInputCallback(Core, InputCallback);
		}

		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => GBAController;

		private ITraceable Tracer { get; }

		private LibmGBA.TraceCallback _tracecb { get; set; }

		private TraceInfo _traceInfo(string msg)
		{
			var disasm = msg.Split('|')[1];
			var split = disasm.Split(':');
			var machineCode = split[0].PadLeft(8);
			var instruction = split[1].Trim();
			var regs = GetCpuFlagsAndRegisters();
			var wordSize = (regs["CPSR"].Value & 32) == 0 ? 4UL : 2UL;
			var pc = regs["R15"].Value - wordSize * 2;
			var sb = new StringBuilder();

			for (var i = 0; i < RegisterNames.Length; i++)
			{
				sb.Append($" { RegisterNames[i] }:{ regs[RegisterNames[i]].Value:X8}");
			}

			return new(
				disassembly: $"{pc:X8}: { machineCode }  { instruction }".PadRight(50),
				registerInfo: sb.ToString());
		}

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			if (controller.IsPressed("Power"))
			{
				LibmGBA.BizReset(Core);

				// BizReset caused memorydomain pointers to change.
				WireMemoryDomainPointers();
			}

			LibmGBA.BizSetTraceCallback(Core, Tracer.IsEnabled() ? _tracecb : null);

			IsLagFrame = LibmGBA.BizAdvance(
				Core,
				LibmGBA.GetButtons(controller),
				render ? _videobuff : _dummyvideobuff,
				ref _nsamp,
				renderSound ? _soundbuff : _dummysoundbuff,
				RTCTime(),
				(short)controller.AxisValue("Tilt X"),
				(short)controller.AxisValue("Tilt Y"),
				(short)controller.AxisValue("Tilt Z"),
				(byte)(255 - controller.AxisValue("Light Sensor")));

			if (IsLagFrame)
			{
				LagCount++;
			}

			// this should be called in hblank on the appropriate line, but until we implement that, just do it here
			_scanlinecb?.Invoke();

			Frame++;

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => VSystemID.Raw.GBA;

		public bool DeterministicEmulation { get; }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			if (Core != IntPtr.Zero)
			{
				LibmGBA.BizDestroy(Core);
				Core = IntPtr.Zero;
			}
		}

		public GBAGPUMemoryAreas GetMemoryAreas()
		{
			return _gpumem;
		}

		[FeatureNotImplemented]
		public void SetScanlineCallback(Action callback, int scanline)
		{
			_scanlinecb = callback;
		}

		private readonly byte[] _saveScratch = new byte[262144];
		internal IntPtr Core;

		private static LibmGBA.OverrideInfo GetOverrideInfo(SyncSettings syncSettings)
		{
			var ret = new LibmGBA.OverrideInfo
			{
				Savetype = syncSettings.OverrideSaveType,
				Hardware = LibmGBA.Hardware.None
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

			if (syncSettings.OverrideGbPlayerDetect is true)
			{
				ret.Hardware |= LibmGBA.Hardware.GbPlayerDetect;
			}

			return ret;
		}

		private Action _scanlinecb;

		private GBAGPUMemoryAreas _gpumem;

		private long RTCTime()
		{
			if (!DeterministicEmulation && _syncSettings.RTCUseRealTime)
			{
				return (long)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			}

			long baseTime = (long)_syncSettings.RTCInitialTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			long increment = Frame * 4389L >> 18;
			return baseTime + increment;
		}

		public static readonly ControllerDefinition GBAController = new ControllerDefinition("GBA Controller")
		{
			BoolButtons = { "Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "L", "R", "Power" }
		}.AddXYZTriple("Tilt {0}", (-32767).RangeTo(32767), 0)
			.AddAxis("Light Sensor", 0.RangeTo(255), 0)
			.MakeImmutable();
	}
}
