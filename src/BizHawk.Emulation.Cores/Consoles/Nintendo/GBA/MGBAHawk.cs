using System;
using System.Text;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	[Core(CoreNames.Mgba, "endrift", true, true, "0.8", "https://mgba.io/", false, "GBA")]
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

		[CoreConstructor("GBA")]
		public MGBAHawk(byte[] file, CoreComm comm, SyncSettings syncSettings, Settings settings, bool deterministic, GameInfo game)
		{
			_syncSettings = syncSettings ?? new SyncSettings();
			_settings = settings ?? new Settings();
			DeterministicEmulation = deterministic;

			byte[] bios = comm.CoreFileProvider.GetFirmware("GBA", "Bios", false);
			DeterministicEmulation &= bios != null;

			if (DeterministicEmulation != deterministic)
			{
				throw new InvalidOperationException("A BIOS is required for deterministic recordings!");
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

			Core = LibmGBA.BizCreate(bios, file, file.Length, GetOverrideInfo(game), skipBios);
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

				Tracer = new TraceBuffer
				{
					Header = "ARM7: PC, machine code, mnemonic, operands, registers"
				};
				_tracecb = msg => Tracer.Put(_traceInfo(msg));
				ser.Register(Tracer);
				MemoryCallbacks = new MGBAMemoryCallbackSystem(this);
			}
			catch
			{
				LibmGBA.BizDestroy(Core);
				throw;
			}

			InputCallbacks = new MemoryBasedInputCallbackSystem(this, "System Bus", new[] { 0x4000130u });
		}

		public IEmulatorServiceProvider ServiceProvider { get; }

		public IVGamepadDef ControllerDefinition => GBAController;

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

			return new TraceInfo
			{
				Disassembly = $"{pc:X8}: { machineCode }  { instruction }".PadRight(50),
				RegisterInfo = sb.ToString()
			};
		}

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			Frame++;
			if (controller.IsPressed("Power"))
			{
				LibmGBA.BizReset(Core);

				// BizReset caused memorydomain pointers to change.
				WireMemoryDomainPointers();
			}

			LibmGBA.BizSetTraceCallback(Core, Tracer.Enabled ? _tracecb : null);

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

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => "GBA";

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

		private static LibmGBA.OverrideInfo GetOverrideInfo(GameInfo game)
		{
			if (!game.OptionPresent("mgbaNeedsOverrides"))
			{
				// the gba game db predates the mgba core in bizhawk, but was never used by the mgba core,
				// which had its own handling for overrides
				// to avoid possible regressions, we don't want to be overriding things that we already
				// know work in mgba, so unless this parameter is set, we do nothing
				return null;
			}

			var ret = new LibmGBA.OverrideInfo();
			if (game.OptionPresent("flashSize"))
			{
				switch (game.GetIntValue("flashSize"))
				{
					case 65536:
						ret.Savetype = LibmGBA.SaveType.Flash512;
						break;
					case 131072:
						ret.Savetype = LibmGBA.SaveType.Flash1m;
						break;
					default:
						throw new InvalidOperationException("Unknown flashSize");
				}
			}
			else if (game.OptionPresent("saveType"))
			{
				switch (game.GetIntValue("saveType"))
				{
					// 3 specifies either flash 512 or 1024, but in vba-over.ini, the latter will have a flashSize as well
					case 3:
						ret.Savetype = LibmGBA.SaveType.Flash512;
						break;
					case 4:
						ret.Savetype = LibmGBA.SaveType.Eeprom;
						break;
					default:
						throw new InvalidOperationException("Unknown saveType");
				}
			}

			if (game.GetInt("rtcEnabled", 0) == 1)
			{
				ret.Hardware |= LibmGBA.Hardware.Rtc;
			}

			if (game.GetInt("mirroringEnabled", 0) == 1)
			{
				throw new InvalidOperationException("Don't know what to do with mirroringEnabled!");
			}

			if (game.OptionPresent("idleLoop"))
			{
				ret.IdleLoop = (uint)game.GetHexValue("idleLoop");
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
			.AddAxis("Light Sensor", 0.RangeTo(255), 0);
	}
}
