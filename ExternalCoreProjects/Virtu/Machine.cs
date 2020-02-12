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

			var emptySlot = new EmptyPeripheralCard(Video);
			Slot1 = emptySlot;
			Slot2 = emptySlot;
			Slot3 = emptySlot;
			Slot4 = emptySlot;
			Slot5 = emptySlot;
			Slot6 = new DiskIIController(Video, diskIIRom);
			Slot7 = emptySlot;
		}

		#region API

		public void BizInitialize()
		{
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

		public void CpuExecute()
		{
			Events.HandleEvents(Cpu.Execute());
		}

		public void InsertDisk1(byte[] disk1)
		{
			// make a writable memory stream cloned from the rom.
			// for junk.dsk the .dsk is important because it determines the format from that
			Slot6.Drives[0].InsertDisk("junk.dsk", (byte[])disk1.Clone(), false);
		}

		public Cpu Cpu { get; private set; }
		public Memory Memory { get; private set; }
		public Speaker Speaker { get; private set; }
		public Video Video { get; private set; }
		public bool Lagged { get; set; }

		#endregion

		internal MachineEvents Events { get; set; }

		internal Keyboard Keyboard { get; private set; }
		internal GamePort GamePort { get; private set; }
		internal Cassette Cassette { get; private set; }
		internal NoSlotClock NoSlotClock { get; private set; }

		internal IPeripheralCard Slot1 { get; private set; }
		internal IPeripheralCard Slot2 { get; private set; }
		internal IPeripheralCard Slot3 { get; private set; }
		internal IPeripheralCard Slot4 { get; private set; }
		internal IPeripheralCard Slot5 { get; private set; }
		public DiskIIController Slot6 { get; private set; }
		internal IPeripheralCard Slot7 { get; private set; }
	}
}
