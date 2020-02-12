using System.Collections.Generic;

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
			Memory = new Memory(this, appleIIe);
			Cpu = new Cpu(Memory);
			Keyboard = new Keyboard();
			GamePort = new GamePort();
			Speaker = new Speaker(Events, Cpu);
			Video = new Video(Events, Memory);
			NoSlotClock = new NoSlotClock(Video);
			Cassette = new Cassette();

			var emptySlot = new EmptyPeripheralCard(Video);
			Slot1 = emptySlot;
			Slot2 = emptySlot;
			Slot3 = emptySlot;
			Slot4 = emptySlot;
			Slot5 = emptySlot;
			Slot6 = new DiskIIController(Video, diskIIRom);
			Slot7 = emptySlot;

			Memory.Initialize();

			Cpu.Reset();
			Memory.Reset();
			Speaker.Reset();
			Video.Reset();
			Slot6.Reset();
		}

		public void BizFrameAdvance(IEnumerable<string> buttons)
		{
			Lagged = true;
			Slot6.DriveLight = false;

			Keyboard.SetKeys(buttons);

			// frame begins at vsync.. beginning of vblank
			while (Video.IsVBlank)
			{
				Events.HandleEvents(Cpu.Execute());
			}

			// now, while not vblank, we're in a frame
			while (!Video.IsVBlank)
			{
				Events.HandleEvents(Cpu.Execute());
			}
		}

		public bool Lagged { get; set; }

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
