using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// an extension of the 6502 processor

	public sealed class MOS6510
	{
		// ------------------------------------

	    readonly MOS6502X cpu;
	    bool pinNMILast;
		LatchedPort port;
		bool thisNMI;

		public Func<int, int> PeekMemory;
		public Action<int, int> PokeMemory;
		public Func<bool> ReadAEC;
		public Func<bool> ReadIRQ;
		public Func<bool> ReadNMI;
		public Func<bool> ReadRDY;
		public Func<int, int> ReadMemory;
		public Func<int> ReadPort;
		public Action<int, int> WriteMemory;
		public Action<int, int> WriteMemoryPort;

		// ------------------------------------

		public MOS6510()
		{
            // configure cpu r/w
            cpu = new MOS6502X
		    {
		        DummyReadMemory = addr => unchecked((byte)Read(addr)),
		        ReadMemory = addr => unchecked((byte)Read(addr)),
		        WriteMemory = (addr, val) => Write(addr, val)
		    };

		    // perform hard reset
			HardReset();
		}

		public void HardReset()
		{
			// configure CPU defaults
			cpu.Reset();
			cpu.FlagI = true;
			cpu.BCD_Enabled = true;
			if (ReadMemory != null)
				cpu.PC = unchecked((ushort)(ReadMemory(0x0FFFC) | (ReadMemory(0x0FFFD) << 8)));

			// configure data port defaults
		    port = new LatchedPort
		    {
		        Direction = 0x00,
		        Latch = 0xFF
		    };

		    // NMI is high on startup (todo: verify)
			pinNMILast = true;
		}

		// ------------------------------------

		public void ExecutePhase1()
		{
			cpu.IRQ = !ReadIRQ();
		}

		public void ExecutePhase2()
		{
			cpu.RDY = ReadRDY();

			// the 6502 core expects active high
			// so we reverse the polarity here
			thisNMI = ReadNMI();
			if (!thisNMI && pinNMILast)
				cpu.NMI = true;

            if (ReadAEC())
            {
                cpu.ExecuteOne();
                pinNMILast = thisNMI;
            }
            else
            {
                LagCycles++;
            }
		}

        public int LagCycles { get; set; }

	    internal bool AtInstructionStart()
		{
			return cpu.AtInstructionStart();
		}

		// ------------------------------------

		public ushort PC
		{
			get
			{
				return cpu.PC;
			}
			set
			{
				cpu.PC = value;
			}
		}

		public int A
		{
			get { return cpu.A; } set { cpu.A = unchecked((byte)value); }
		}

		public int X
		{
			get { return cpu.X; } set { cpu.X = unchecked((byte)value); }
		}

		public int Y
		{
			get { return cpu.Y; } set { cpu.Y = unchecked((byte)value); }
		}

		public int S
		{
			get { return cpu.S; } set { cpu.S = unchecked((byte)value); }
		}

		public bool FlagC { get { return cpu.FlagC; } }
		public bool FlagZ { get { return cpu.FlagZ; } }
		public bool FlagI { get { return cpu.FlagI; } }
		public bool FlagD { get { return cpu.FlagD; } }
		public bool FlagB { get { return cpu.FlagB; } }
		public bool FlagV { get { return cpu.FlagV; } }
		public bool FlagN { get { return cpu.FlagN; } }
		public bool FlagT { get { return cpu.FlagT; } }

		public int Peek(int addr)
		{
		    switch (addr)
		    {
		        case 0x0000:
		            return port.Direction;
		        case 0x0001:
		            return PortData;
		        default:
		            return PeekMemory(addr);
		    }
		}

	    public void Poke(int addr, int val)
	    {
	        switch (addr)
	        {
	            case 0x0000:
	                port.Direction = val;
	                break;
	            case 0x0001:
	                port.Latch = val;
	                break;
	            default:
	                PokeMemory(addr, val);
	                break;
	        }
	    }

	    public int PortData
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

		public int Read(int addr)
		{
		    switch (addr)
		    {
		        case 0x0000:
		            return port.Direction;
		        case 0x0001:
		            return PortData;
		        default:
		            return ReadMemory(addr);
		    }
		}

	    public void SyncState(Serializer ser)
		{
			cpu.SyncState(ser);
			SaveState.SyncObject(ser, this);
		}

		public void Write(int addr, int val)
		{
		    switch (addr)
		    {
		        case 0x0000:
		            port.Direction = val;
		            WriteMemoryPort(addr, val);
		            break;
		        case 0x0001:
		            port.Latch = val;
		            WriteMemoryPort(addr, val);
		            break;
		        default:
		            WriteMemory(addr, val);
		            break;
		    }
		}
	}
}
