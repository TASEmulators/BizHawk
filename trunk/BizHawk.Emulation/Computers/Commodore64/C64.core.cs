using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BizHawk.Emulation.CPUs.M6502;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class  C64 : IEmulator
	{
        // source
        public Cartridge cart;
        public bool cartInserted;
		public byte[] inputFile;

        // chipset
        public Cia cia1;
        public Cia cia2;
		public MOS6502X cpu;
        public Memory mem;
        public Sid sid;
        public VicII vic;
        public VicSignals vicSignal;

		private void HardReset()
		{
			cpu = new MOS6502X();
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
			cpu.DummyReadMemory = PeekMemory;

            // initialize cia timers
            cia1 = new Cia(Cia.DummyReadPort, Cia.DummyReadPort, Cia.DummyWritePort, Cia.DummyWritePort);
            cia2 = new Cia(Cia.DummyReadPort, Cia.DummyReadPort, Cia.DummyWritePort, Cia.DummyWritePort);

            // initialize vic
            vicSignal = new VicSignals();
            vic = new VicII(vicSignal, VicIIMode.NTSC);

            // initialize sid
            sid = new Sid();

            // initialize memory (this must be done AFTER all other chips are initialized)
            string romPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "C64Kernal");
            mem = new Memory(romPath, vic, sid, cia1, cia2);
            cia2.ReadPortA = mem.CIA2ReadPortA;
            cia2.ReadPortB = mem.CIA2ReadPortB;
            cia2.WritePortA = mem.CIA2WritePortA;
            cia2.WritePortB = mem.CIA2WritePortB;

            // initialize media
            Cartridge cart = new Cartridge(inputFile);
            if (cart.valid)
            {
                mem.ApplyCartridge(cart);
            }

            // initialize cpu (hard reset vector)
            cpu.PC = (ushort)(ReadMemory(0xFFFC) + (ReadMemory(0xFFFD) << 8));
		}

        public byte PeekMemory(ushort addr)
        {
            return mem.Peek(addr);
        }

        public byte PeekMemoryInt(int addr)
        {
            return mem.Peek((ushort)(addr & 0xFFFF));
        }

        public void PokeMemoryInt(int addr, byte val)
        {
            // todo
        }

		public byte ReadMemory(ushort addr)
		{
			return mem.Read(addr);
		}

		public void WriteMemory(ushort addr, byte value)
		{
            mem.Write(addr, value);
		}
	}
}
