using System.Collections.Generic;
using System.Linq;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;
using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	[PortedCore(CoreNames.Virtu, "fool")]
	[ServiceNotApplicable(typeof(IBoardInfo), typeof(IRegionable), typeof(ISaveRam))]
	public partial class AppleII : IEmulator, ISoundProvider, IVideoProvider, IStatable, IDriveLight
	{
		static AppleII()
		{
			AppleIIController = new("Apple IIe Keyboard");
			AppleIIController.BoolButtons.AddRange(RealButtons);
			AppleIIController.BoolButtons.AddRange(ExtraButtons);
			AppleIIController.MakeImmutable();
		}

		[CoreConstructor(VSystemID.Raw.AppleII)]
		public AppleII(CoreLoadParameters<Settings, SyncSettings> lp)
		{
			static (byte[], string) GetRomAndExt(IRomAsset romAssert)
			{
				var ext = romAssert.Extension.ToUpperInvariant();
				return ext switch
				{
					".DSK" or ".PO" or ".DO" or ".NIB" => (romAssert.FileData, ext),
					".2MG" => throw new NotSupportedException("Unsupported extension .2mg!"), // TODO: add a way to support this (we have hashes of this format in our db it seems?)
					_ => (romAssert.FileData, ".DSK") // no idea, let's assume it's just a .DSK?
				};
			}
					
			_romSet = lp.Roms.Select(GetRomAndExt).ToList();
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			const string TRACE_HEADER = "6502: PC, opcode, register (A, X, Y, P, SP, Cy), flags (NVTBDIZC)";
			_tracer = new TraceBuffer(TRACE_HEADER);

			_appleIIRom = lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new(SystemId, "AppleIIe"), "The Apple IIe BIOS firmware is required");
			_diskIIRom = lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new(SystemId, "DiskII"), "The DiskII firmware is required");

			_machine = new Components(_appleIIRom, _diskIIRom);

			InitSaveRam();
			InitDisk();

			ser.Register<ITraceable>(_tracer);

			SetCallbacks();

			SetupMemoryDomains();
			PutSettings(lp.Settings ?? new Settings());

			_syncSettings = lp.SyncSettings ?? new SyncSettings();
			DeterministicEmulation = lp.DeterministicEmulationRequested || !_syncSettings.UseRealTime;
			InitializeRtc(!DeterministicEmulation);
		}

		private static readonly ControllerDefinition AppleIIController;

		private readonly List<(byte[] Data, string Extension)> _romSet;
		private readonly ITraceable _tracer;

		private readonly Components _machine;
		private readonly byte[] _appleIIRom;
		private readonly byte[] _diskIIRom;

		private int _currentDisk;

		public int CurrentDisk
		{
			get => _currentDisk;
			set => _currentDisk = value;
		}

		public int DiskCount => _romSet.Count;

		public void SetDisk(int discNum)
		{
			SaveDelta();
			CurrentDisk = discNum;
			InitDisk();
		}

		private void IncrementDisk()
		{
			SaveDelta();
			CurrentDisk++;
			if (CurrentDisk >= _romSet.Count)
			{
				CurrentDisk = 0;
			}

			InitDisk();
		}

		private void DecrementDisk()
		{
			SaveDelta();
			CurrentDisk--;
			if (CurrentDisk < 0)
			{
				CurrentDisk = _romSet.Count - 1;
			}

			InitDisk();
		}

		private void InitDisk()
		{
			// make a writable memory stream cloned from the rom.
			// the extension is important here because it determines the format from that
			_machine.DiskIIController.Drive1.InsertDisk("junk" + _romSet[CurrentDisk].Extension, (byte[])_romSet[CurrentDisk].Data.Clone(), false);
			LoadDelta(false);
		}

		private static readonly List<string> RealButtons = new List<string>(Keyboard.GetKeyNames()
			.Where(k => k != "Reset"));

		private static readonly List<string> ExtraButtons = new List<string>
		{
			"Previous Disk",
			"Next Disk",
		};

		public bool DriveLightEnabled => true;
		public bool DriveLightOn => _machine.DiskIIController.DriveLight;

		public string DriveLightIconDescription => "Disk Drive Activity LED";

		private bool _nextPressed;
		private bool _prevPressed;

		private void TracerWrapper(string[] content)
			=> _tracer.Put(new(disassembly: content[0], registerInfo: content[1]));

		private void FrameAdv(IController controller, bool render, bool renderSound)
		{
			if (_tracer.IsEnabled())
			{
				_machine.Cpu.TraceCallback = TracerWrapper;
			}
			else
			{
				_machine.Cpu.TraceCallback = null;
			}

			if (controller.IsPressed("Next Disk") && !_nextPressed)
			{
				_nextPressed = true;
				IncrementDisk();
			}
			else if (controller.IsPressed("Previous Disk") && !_prevPressed)
			{
				_prevPressed = true;
				DecrementDisk();
			}

			if (!controller.IsPressed("Next Disk"))
			{
				_nextPressed = false;
			}

			if (!controller.IsPressed("Previous Disk"))
			{
				_prevPressed = false;
			}

			AdvanceRtc();

			MachineAdvance(RealButtons.Where(controller.IsPressed));
			if (IsLagFrame)
			{
				LagCount++;
			}

			Frame++;
		}

		private void MachineAdvance(IEnumerable<string> buttons)
		{
			_machine.Memory.Lagged = true;
			_machine.Memory.DiskIIController.DriveLight = false;
			_machine.Memory.Keyboard.SetKeys(buttons);

			// frame begins at vsync.. beginning of vblank
			while (_machine.Video.IsVBlank)
			{
				_machine.Events.HandleEvents(_machine.Cpu.Execute());
			}

			// now, while not vblank, we're in a frame
			while (!_machine.Video.IsVBlank)
			{
				_machine.Events.HandleEvents(_machine.Cpu.Execute());
			}
		}

		private void SetCallbacks()
		{
			_machine.Memory.ReadCallback = (addr) =>
			{
				if (MemoryCallbacks.HasReads)
				{
					uint flags = (uint)MemoryCallbackFlags.AccessRead;
					MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
				}
			};
			_machine.Memory.WriteCallback = (addr) =>
			{
				if (MemoryCallbacks.HasWrites)
				{
					uint flags = (uint)MemoryCallbackFlags.AccessWrite;
					MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
				}
			};
			_machine.Memory.ExecuteCallback = (addr) =>
			{
				if (MemoryCallbacks.HasExecutes)
				{
					uint flags = (uint)MemoryCallbackFlags.AccessExecute;
					MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
				}
			};
			_machine.Memory.InputCallback = InputCallbacks.Call;
		}

		private bool _useRealTime;
		private long _clockTime;
		private int _clockRemainder;
		private const int TicksInSecond = 10000000; // DateTime.Ticks uses 100-nanosecond intervals
		
		private DateTime GetFrontendTime()
		{
			if (_useRealTime && DeterministicEmulation)
				throw new InvalidOperationException();
			
			return _useRealTime
				? DateTime.Now
				: new DateTime(_clockTime * TicksInSecond + (_clockRemainder * TicksInSecond / VsyncNumerator));
		}

		private void InitializeRtc(bool useRealTime)
		{
			_useRealTime = useRealTime;
			_clockTime = _syncSettings.InitialTime.Ticks / TicksInSecond;
			_clockRemainder = (int)(_syncSettings.InitialTime.Ticks % TicksInSecond) * VsyncNumerator / TicksInSecond;
			_machine.NoSlotClock.FrontendTimeCallback = GetFrontendTime;
		}

		private void AdvanceRtc()
		{
			_clockRemainder += VsyncDenominator;
			if (_clockRemainder >= VsyncNumerator)
			{
				_clockRemainder -= VsyncNumerator;
				_clockTime++;
			}
		}
	}
}
