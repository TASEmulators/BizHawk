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
		//private bool freezeCpu;
		private bool pinNMILast;
        private LatchedPort port;

		public Func<int, byte> PeekMemory;
		public Action<int, byte> PokeMemory;
		public Func<bool> ReadAEC;
		public Func<bool> ReadIRQ;
		public Func<bool> ReadNMI;
		public Func<bool> ReadRDY;
		public Func<int, byte> ReadMemory;
        public Func<byte> ReadPort;
		public Action<int, byte> WriteMemory;

		// ------------------------------------

		public MOS6510()
		{
            cpu = new MOS6502X();

			// configure cpu r/w
			cpu.DummyReadMemory = Read;
			cpu.ReadMemory = Read;
			cpu.WriteMemory = Write;

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
			    cpu.PC = (ushort)(ReadMemory(0x0FFFC) | (ReadMemory(0x0FFFD) << 8));

            // configure data port defaults
            port = new LatchedPort();
            port.Direction = 0x00;
            port.Latch = 0xFF;

            // NMI is high on startup (todo: verify)
            pinNMILast = true;
        }

		// ------------------------------------

		public void ExecutePhase1()
		{
		}

		public void ExecutePhase2()
		{
            cpu.RDY = ReadRDY();

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
                return port.ReadInput(ReadPort());
            }
            set
            {
                port.Latch = value;
            }
        }

        public byte Read(ushort addr)
		{
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
            ser.Sync("pinNMILast", ref pinNMILast);
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
