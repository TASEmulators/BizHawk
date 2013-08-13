using BizHawk.Emulation.CPUs.M6502;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// an extension of the 6502 processor

	public class MOS6510
	{
		// ------------------------------------

        private MOS6502X cpu;
        private List<GCHandle> disposeList = new List<GCHandle>();
		private bool freezeCpu;
		private bool pinNMILast;
        private LatchedPort port;
		private bool unusedPin0;
		private bool unusedPin1;
		private uint unusedPinTTL0;
		private uint unusedPinTTL1;
		private uint unusedPinTTLCycles;

		public Func<int, byte> PeekMemory;
		public Action<int, byte> PokeMemory;
		public Func<bool> ReadAEC;
		public Func<bool> ReadIRQ;
		public Func<bool> ReadNMI;
		public Func<bool> ReadRDY;
		public Func<ushort, byte> ReadMemory;
        public Func<byte> ReadPort;
		public Action<ushort, byte> WriteMemory;

		// ------------------------------------

		public MOS6510()
		{
            cpu = new MOS6502X();

			// configure cpu r/w
			cpu.DummyReadMemory = Read;
			cpu.ReadMemory = Read;
			cpu.WriteMemory = Write;

			// todo: verify this value (I only know that unconnected bits fade after a number of cycles)
			unusedPinTTLCycles = 40;

            // perform hard reset
            HardReset();
		}

        ~MOS6510()
        {
            foreach (GCHandle handle in disposeList)
            {
                handle.Free();
            }
        }

		public void HardReset()
		{
            // configure CPU defaults
			cpu.Reset();
			cpu.FlagI = true;
			cpu.BCD_Enabled = true;
            if (ReadMemory != null)
			    cpu.PC = (ushort)(ReadMemory(0xFFFC) | (ReadMemory(0xFFFD) << 8));

            // configure data port defaults
            port = new LatchedPort();
            port.Direction = 0x00;
            port.Latch = 0x1F;

            // NMI is high on startup (todo: verify)
            pinNMILast = true;

            // reset unused IO pin TTLs
            unusedPinTTL0 = 0;
            unusedPinTTL1 = 0;
        }

		// ------------------------------------

		public void ExecutePhase1()
		{
		}

		public void ExecutePhase2()
		{
			if (ReadAEC() && !freezeCpu)
			{
				// the 6502 core expects active high
				// so we reverse the polarity here
				bool thisNMI = ReadNMI();
				if (!thisNMI && pinNMILast)
					cpu.NMI = true;
				else
					cpu.NMI = false;
				pinNMILast = thisNMI;

				cpu.IRQ = !ReadIRQ();
				cpu.ExecuteOne();
			}

			// unfreeze cpu if BA is high
			if (ReadRDY()) freezeCpu = false;

			// process unused pin TTL
			if (unusedPinTTL0 == 0)
				unusedPin0 = false;
			else
				unusedPinTTL0--;

			if (unusedPinTTL1 == 0)
				unusedPin1 = false;
			else
				unusedPinTTL1--;
		}

		// ------------------------------------

		public ushort PC
		{
			get
			{
				return cpu.PC;
			}
		}

		public byte Peek(int addr)
		{
			if (addr == 0x0000)
				return port.Direction;
			else if (addr == 0x0001)
				return PortData;
			else
				return PeekMemory(addr);
		}

		public void Poke(int addr, byte val)
		{
			if (addr == 0x0000)
				port.Direction = val;
			else if (addr == 0x0001)
				port.Latch = val;
			else
				PokeMemory(addr, val);
		}

        public byte PortData
        {
            get
            {
                return port.ReadOutput();
            }
            set
            {
                port.Latch = value;
            }
        }

        public byte Read(ushort addr)
		{
			// cpu freezes after first read when RDY is low
			if (!ReadRDY())
				freezeCpu = true;

			if (addr == 0x0000)
				return port.Direction;
			else if (addr == 0x0001)
				return PortData;
			else
				return ReadMemory(addr);
		}

        public void SyncState(Serializer ser)
        {
            cpu.SyncState(ser);
            ser.Sync("freezeCpu", ref freezeCpu);
            ser.Sync("pinNMILast", ref pinNMILast);
            ser.Sync("unusedPin0", ref unusedPin0);
            ser.Sync("unusedPin1", ref unusedPin1);
            ser.Sync("unusedPinTTL0", ref unusedPinTTL0);
            ser.Sync("unusedPinTTL1", ref unusedPinTTL1);
            ser.Sync("unusedPinTTLCycles", ref unusedPinTTLCycles);
        }

        public void Write(ushort addr, byte val)
		{
			if (addr == 0x0000)
				port.Direction = val;
			else if (addr == 0x0001)
				port.Latch = val;
			WriteMemory(addr, val);
		}
	}
}
