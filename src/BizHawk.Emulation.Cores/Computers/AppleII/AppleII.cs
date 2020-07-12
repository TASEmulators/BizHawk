using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;
using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	[Core(
		"Virtu",
		"fool",
		isPorted: true,
		isReleased: true)]
	[ServiceNotApplicable(new[] { typeof(IBoardInfo), typeof(IRegionable), typeof(ISaveRam) })]
	public partial class AppleII : IEmulator, ISoundProvider, IVideoProvider, IStatable, IDriveLight
	{
		static AppleII()
		{
			AppleIIController = new ControllerDefinition { Name = "Apple IIe Keyboard" };
			AppleIIController.BoolButtons.AddRange(RealButtons);
			AppleIIController.BoolButtons.AddRange(ExtraButtons);
		}

		[CoreConstructor("AppleII")]
		public AppleII(CoreLoadParameters<Settings, object> lp)
		{
			_romSet = lp.Roms.Select(r => r.RomData).ToList();
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			_tracer = new TraceBuffer
			{
				Header = "6502: PC, opcode, register (A, X, Y, P, SP, Cy), flags (NVTBDIZC)"
			};

			_disk1 = _romSet[0];

			_appleIIRom = lp.Comm.CoreFileProvider.GetFirmware(
				SystemId, "AppleIIe", true, "The Apple IIe BIOS firmware is required");
			_diskIIRom = lp.Comm.CoreFileProvider.GetFirmware(
				SystemId, "DiskII", true, "The DiskII firmware is required");

			_machine = new Components(_appleIIRom, _diskIIRom);
			
			// make a writable memory stream cloned from the rom.
			// for junk.dsk the .dsk is important because it determines the format from that
			InitDisk();

			ser.Register<ITraceable>(_tracer);

			SetCallbacks();

			SetupMemoryDomains();
			PutSettings(lp.Settings ?? new Settings());
		}

		private static readonly ControllerDefinition AppleIIController;

		private readonly List<byte[]> _romSet = new List<byte[]>();
		private readonly ITraceable _tracer;

		private Components _machine;
		private byte[] _disk1;
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
			CurrentDisk = discNum;
			InitDisk();
		}

		private void IncrementDisk()
		{
			CurrentDisk++;
			if (CurrentDisk >= _romSet.Count)
			{
				CurrentDisk = 0;
			}

			InitDisk();
		}

		private void DecrementDisk()
		{
			CurrentDisk--;
			if (CurrentDisk < 0)
			{
				CurrentDisk = _romSet.Count - 1;
			}

			InitDisk();
		}

		private void InitDisk()
		{
			_disk1 = _romSet[CurrentDisk];

			// make a writable memory stream cloned from the rom.
			// for junk.dsk the .dsk is important because it determines the format from that
			_machine.Memory.DiskIIController.Drive1.InsertDisk("junk.dsk", (byte[])_disk1.Clone(), false);
		}

		private static readonly List<string> RealButtons = new List<string>(Keyboard.GetKeyNames()
			.Where(k => k != "Reset"));

		private static readonly List<string> ExtraButtons = new List<string>
		{
			"Previous Disk",
			"Next Disk",
		};

		public bool DriveLightEnabled => true;
		public bool DriveLightOn => _machine.Memory.DiskIIController.DriveLight;

		private bool _nextPressed;
		private bool _prevPressed;

		private void TracerWrapper(string[] content)
		{
			_tracer.Put(new TraceInfo
			{
				Disassembly = content[0],
				RegisterInfo = content[1]
			});
		}

		private void FrameAdv(IController controller, bool render, bool renderSound)
		{
			if (_tracer.Enabled)
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
	}
}
