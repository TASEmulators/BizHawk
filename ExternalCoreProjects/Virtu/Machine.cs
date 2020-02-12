namespace Jellyfish.Virtu
{
	public sealed class Machine
	{
		/// <summary>
		/// for deserialization only!!
		/// </summary>
		public Machine() { }

		public Machine(byte[] appleIIe, byte[] diskIIRom)
		{
			Events = new MachineEvents();
			Keyboard = new Keyboard();
			GamePort = new GamePort();
			Cassette = new Cassette();

			Memory = new Memory(appleIIe);
			Cpu = new Cpu(Memory);
			
			Speaker = new Speaker(Events, Cpu);
			Video = new Video(Events, Memory);
			NoSlotClock = new NoSlotClock(Video);

			var emptySlot = new EmptyPeripheralCard(Video);
			Slot1 = emptySlot;
			Slot2 = emptySlot;
			Slot3 = emptySlot;
			Slot4 = emptySlot;
			Slot5 = emptySlot;
			Slot6 = new DiskIIController(Video, diskIIRom);
			Slot7 = emptySlot;

			// Necessary because of tangling dependencies between memory and video classes
			Memory.Initialize(
				Keyboard,
				GamePort,
				Cassette,
				Speaker,
				Video,
				NoSlotClock,
				Slot1,
				Slot2,
				Slot3,
				Slot4,
				Slot5,
				Slot6,
				Slot7);

			Cpu.Reset();
			Memory.Reset();
			Video.Reset();
		}

		public MachineEvents Events { get; set; }
		public Memory Memory { get; private set; }
		public Cpu Cpu { get; private set; }
		public Keyboard Keyboard { get; private set; }
		public GamePort GamePort { get; private set; }
		public Speaker Speaker { get; private set; }
		public Video Video { get; private set; }
		public Cassette Cassette { get; private set; }
		public NoSlotClock NoSlotClock { get; private set; }

		public IPeripheralCard Slot1 { get; private set; }
		public IPeripheralCard Slot2 { get; private set; }
		public IPeripheralCard Slot3 { get; private set; }
		public IPeripheralCard Slot4 { get; private set; }
		public IPeripheralCard Slot5 { get; private set; }
		public DiskIIController Slot6 { get; private set; }
		public IPeripheralCard Slot7 { get; private set; }
	}
}
