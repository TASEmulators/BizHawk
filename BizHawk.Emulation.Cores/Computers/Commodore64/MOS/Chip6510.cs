using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// an extension of the 6502 processor

	public sealed partial class Chip6510
	{
		// ------------------------------------

	    private MOS6502X _cpu;
	    private bool _pinNmiLast;
	    private LatchedPort _port;
	    private bool _thisNmi;

		public Func<int, int> PeekMemory;
		public Action<int, int> PokeMemory;
		public Func<bool> ReadAec;
		public Func<bool> ReadIrq;
		public Func<bool> ReadNmi;
		public Func<bool> ReadRdy;
		public Func<int, int> ReadMemory;
		public Func<int> ReadPort;
		public Action<int, int> WriteMemory;
		public Action<int, int> WriteMemoryPort;

	    public Action DebuggerStep;

		// ------------------------------------

		public Chip6510()
		{
            // configure cpu r/w
            _cpu = new MOS6502X
            {
		        DummyReadMemory = CpuRead,
		        ReadMemory = CpuRead,
		        WriteMemory = CpuWrite,
                PeekMemory = CpuPeek
		    };

		    // perform hard reset
			HardReset();
		}

	    public void SetOverflow()
	    {
	    }

        private byte CpuPeek(ushort addr)
        {
            return unchecked((byte)Peek(addr));
        }

        private byte CpuRead(ushort addr)
	    {
	        return unchecked((byte) Read(addr));
	    }

	    private void CpuWrite(ushort addr, byte val)
	    {
	        Write(addr, val);
	    }

		public void HardReset()
		{
			_cpu.NESSoftReset();
		    _port = new LatchedPort
		    {
		        Direction = 0x00,
		        Latch = 0xFF
		    };
			_pinNmiLast = true;
		}

		// ------------------------------------

		public void ExecutePhase()
		{
            _cpu.RDY = ReadRdy();

            if (ReadAec())
            {
                _cpu.IRQ = !ReadIrq();
                _pinNmiLast = _thisNmi;
                _thisNmi = ReadNmi();
                _cpu.NMI |= _pinNmiLast && !_thisNmi;
                _cpu.ExecuteOne();
            }
            else
            {
                LagCycles++;
            }
		}

	    public int LagCycles;

	    internal bool AtInstructionStart()
		{
			return _cpu.AtInstructionStart();
		}

        // ------------------------------------

        [SaveState.DoNotSave]
        public ushort Pc
		{
			get
			{
				return _cpu.PC;
			}
			set
			{
				_cpu.PC = value;
			}
		}

        [SaveState.DoNotSave]
        public int A
		{
			get { return _cpu.A; } set { _cpu.A = unchecked((byte)value); }
		}

        [SaveState.DoNotSave]
        public int X
		{
			get { return _cpu.X; } set { _cpu.X = unchecked((byte)value); }
		}

        [SaveState.DoNotSave]
        public int Y
		{
			get { return _cpu.Y; } set { _cpu.Y = unchecked((byte)value); }
		}

        [SaveState.DoNotSave]
        public int S
		{
			get { return _cpu.S; } set { _cpu.S = unchecked((byte)value); }
		}

        [SaveState.DoNotSave]
        public bool FlagC { get { return _cpu.FlagC; } }
        [SaveState.DoNotSave]
        public bool FlagZ { get { return _cpu.FlagZ; } }
        [SaveState.DoNotSave]
        public bool FlagI { get { return _cpu.FlagI; } }
        [SaveState.DoNotSave]
        public bool FlagD { get { return _cpu.FlagD; } }
        [SaveState.DoNotSave]
        public bool FlagB { get { return _cpu.FlagB; } }
        [SaveState.DoNotSave]
        public bool FlagV { get { return _cpu.FlagV; } }
        [SaveState.DoNotSave]
        public bool FlagN { get { return _cpu.FlagN; } }
        [SaveState.DoNotSave]
        public bool FlagT { get { return _cpu.FlagT; } }

		public int Peek(int addr)
		{
		    switch (addr)
		    {
		        case 0x0000:
		            return _port.Direction;
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
	                _port.Direction = val;
	                break;
	            case 0x0001:
	                _port.Latch = val;
	                break;
	            default:
	                PokeMemory(addr, val);
	                break;
	        }
	    }

        [SaveState.DoNotSave]
        public int PortData
		{
			get
			{
				return _port.ReadInput(ReadPort());
			}
			set
			{
				_port.Latch = value;
			}
		}

		public int Read(int addr)
		{
		    switch (addr)
		    {
		        case 0x0000:
		            return _port.Direction;
		        case 0x0001:
		            return PortData;
		        default:
		            return ReadMemory(addr);
		    }
		}

	    public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}

		public void Write(int addr, int val)
		{
		    switch (addr)
		    {
		        case 0x0000:
		            _port.Direction = val;
		            WriteMemoryPort(addr, val);
		            break;
		        case 0x0001:
		            _port.Latch = val;
		            WriteMemoryPort(addr, val);
		            break;
		        default:
		            WriteMemory(addr, val);
		            break;
		    }
		}
	}
}
