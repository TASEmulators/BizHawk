using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	/// <summary>
	/// A container class for the individual machine components
	/// </summary>
	public sealed class Components
	{
		/// <summary>
		/// for deserialization only!!
		/// </summary>
		public Components() { }

		public Components(byte[] appleIIe, byte[] diskIIRom)
		{
			Events = new MachineEvents();
			Memory = new Memory(appleIIe);
			Cpu = new Cpu(Memory);
			Video = new Video(Events, Memory);

			var emptySlot = new EmptyPeripheralCard(Video);

			// Necessary because of tangling dependencies between memory and video classes
			Memory.Initialize(
				new Keyboard(),
				new GamePort(),
				new EmptyCassetteComponent(),
				new Speaker(Events, Cpu),
				Video,
				new NoSlotClock(Video),
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

		public MachineEvents Events { get; set; }
		public Memory Memory { get; private set; }
		public Cpu Cpu { get; private set; }
		public Video Video { get; private set; }
	}
}
