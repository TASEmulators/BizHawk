using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;
using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	[CoreAttributes(
		"Virtu",
		"fool",
		isPorted: true,
		isReleased: true
		)]
	[ServiceNotApplicable(typeof(ISaveRam), typeof(IRegionable))]
	public partial class AppleII : IEmulator, ISoundProvider, IVideoProvider, IStatable, IDriveLight
	{
		public AppleII(CoreComm comm, IEnumerable<GameInfo> gameInfoSet, IEnumerable<byte[]> romSet, Settings settings)
			: this(comm, gameInfoSet.First(), romSet.First(), settings)
		{
			GameInfoSet = gameInfoSet.ToList();
			RomSet = romSet.ToList();
		}

		[CoreConstructor("AppleII")]
		public AppleII(CoreComm comm, GameInfo game, byte[] rom, Settings settings)
		{
			GameInfoSet = new List<GameInfo>();

			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			CoreComm = comm;

			Tracer = new TraceBuffer
			{
				Header = "6502: PC, opcode, register (A, X, Y, P, SP, Cy), flags (NVTBDIZC)"
			};

			MemoryCallbacks = new MemoryCallbackSystem();
			InputCallbacks = new InputCallbackSystem();

			_disk1 = rom;
			RomSet.Add(rom);

			_appleIIRom = comm.CoreFileProvider.GetFirmware(
				SystemId, "AppleIIe", true, "The Apple IIe BIOS firmware is required");
			_diskIIRom = comm.CoreFileProvider.GetFirmware(
				SystemId, "DiskII", true, "The DiskII firmware is required");

			_machine = new Machine(_appleIIRom, _diskIIRom);
			
			_machine.BizInitialize();

			//make a writeable memory stream cloned from the rom.
			//for junk.dsk the .dsk is important because it determines the format from that
			InitDisk();

			ser.Register<ITraceable>(Tracer);

			setCallbacks();

			InitSaveStates();
			SetupMemoryDomains();
			PutSettings(settings ?? new Settings());
		}


		public List<GameInfo> GameInfoSet { get; private set; }
		private readonly List<byte[]> RomSet = new List<byte[]>();

		public int CurrentDisk { get; private set; }
		public int DiskCount { get { return RomSet.Count; } }

		private ITraceable Tracer { get; set; }

		public void SetDisk(int discNum)
		{
			CurrentDisk = discNum;
			InitDisk();
		}

		private void IncrementDisk()
		{
			CurrentDisk++;
			if (CurrentDisk >= RomSet.Count)
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
				CurrentDisk = RomSet.Count - 1;
			}

			InitDisk();
		}


		private void InitDisk()
		{
			_disk1 = RomSet[CurrentDisk];

			//make a writeable memory stream cloned from the rom.
			//for junk.dsk the .dsk is important because it determines the format from that
			_machine.BootDiskII.Drives[0].InsertDisk("junk.dsk", (byte[])_disk1.Clone(), false);
		}

		private Machine _machine;
		private byte[] _disk1;
		private readonly byte[] _appleIIRom;
		private readonly byte[] _diskIIRom;

		private static readonly ControllerDefinition AppleIIController;

		private static readonly List<string> RealButtons = new List<string>(Keyboard.GetKeyNames()
			.Where(k => k != "White Apple") // Hack because these buttons aren't wired up yet
			.Where(k => k != "Black Apple")
			.Where(k => k != "Reset"));

		private static readonly List<string> ExtraButtons = new List<string>
		{
			"Previous Disk",
			"Next Disk",
		};

		static AppleII()
		{
			AppleIIController = new ControllerDefinition { Name = "Apple IIe Keyboard" };
			AppleIIController.BoolButtons.AddRange(RealButtons);
			AppleIIController.BoolButtons.AddRange(ExtraButtons);
		}

		public bool DriveLightEnabled { get { return true; } }
		public bool DriveLightOn { get { return _machine.DriveLight; } }

		private bool _nextPressed = false;
		private bool _prevPressed = false;

		private void TracerWrapper(string[] content)
		{
			Tracer.Put(new TraceInfo
			{
				Disassembly = content[0],
				RegisterInfo = content[1]
			});
		}

		private void FrameAdv(bool render, bool rendersound)
		{
			if (Tracer.Enabled)
			{
				_machine.Cpu.TraceCallback = (s) => TracerWrapper(s);
			}
			else
			{
				_machine.Cpu.TraceCallback = null;
			}

			if (Controller.IsPressed("Next Disk") && !_nextPressed)
			{
				_nextPressed = true;
				IncrementDisk();
			}
			else if (Controller.IsPressed("Previous Disk") && !_prevPressed)
			{
				_prevPressed = true;
				DecrementDisk();
			}

			if (!Controller.IsPressed("Next Disk"))
			{
				_nextPressed = false;
			}

			if (!Controller.IsPressed("Previous Disk"))
			{
				_prevPressed = false;
			}

			_machine.BizFrameAdvance(RealButtons.Where(b => Controller.IsPressed(b)));
			if (IsLagFrame)
			{
				LagCount++;
			}

			Frame++;
		}

		private void setCallbacks()
		{
			_machine.Memory.ReadCallback = MemoryCallbacks.CallReads;
			_machine.Memory.WriteCallback = MemoryCallbacks.CallWrites;
			_machine.Memory.ExecuteCallback = MemoryCallbacks.CallExecutes;
			_machine.Memory.InputCallback = InputCallbacks.Call;
		}

	}
}
