using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	/// <summary>
	/// A container class for the individual machine components
	/// </summary>
	public sealed class Components
	{
		public Components(byte[] appleIIe, byte[] diskIIRom)
		{
			Events = new MachineEvents();
			Memory = new Memory(appleIIe);
			Cpu = new Cpu(Memory);
			Video = new Video(Events, Memory);

			NoSlotClock = new NoSlotClock(Video);
			DiskIIController = new DiskIIController(Video, diskIIRom);

			var emptySlot = new EmptyPeripheralCard(Video);

			// Necessary because of tangling dependencies between memory and video classes
			Memory.Initialize(
				new Keyboard(),
				new GamePortComponent(),
				new EmptyCassetteComponent(),
				new Speaker(Events, Cpu),
				Video,
				NoSlotClock,
				emptySlot,
				emptySlot,
				emptySlot,
				emptySlot,
				emptySlot,
				new DiskIIController(Video, diskIIRom),
				emptySlot);

			Cpu.Reset();
			Memory.Reset();
			Video.Reset();
		}

		public MachineEvents Events { get; }
		public Memory Memory { get; }
		public Cpu Cpu { get; }
		public Video Video { get; }

		// Only needed for convenience of savestate syncing, else the memory component needs to do it
		public NoSlotClock NoSlotClock { get; }
		public DiskIIController DiskIIController { get; }
	}
}
