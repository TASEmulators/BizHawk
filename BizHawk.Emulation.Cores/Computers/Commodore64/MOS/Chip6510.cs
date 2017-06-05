using System;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// an extension of the 6502 processor
	public sealed partial class Chip6510
	{
		// ------------------------------------
		private readonly MOS6502X _cpu;
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

		public string TraceHeader => "6510: PC, machine code, mnemonic, operands, registers (A, X, Y, P, SP), flags (NVTBDIZCR)";

		public Action<TraceInfo> TraceCallback
		{
			get { return _cpu.TraceCallback; }
			set { _cpu.TraceCallback = value; }
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
			return unchecked((byte)Read(addr));
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

		public int A
		{
			get { return _cpu.A; }
			set { _cpu.A = unchecked((byte)value); }
		}

		public int X
		{
			get { return _cpu.X; }
			set { _cpu.X = unchecked((byte)value); }
		}

		public int Y
		{
			get { return _cpu.Y; }
			set { _cpu.Y = unchecked((byte)value); }
		}

		public int S
		{
			get { return _cpu.S; }
			set { _cpu.S = unchecked((byte)value); }
		}

		public bool FlagC => _cpu.FlagC;
		public bool FlagZ => _cpu.FlagZ;
		public bool FlagI => _cpu.FlagI;
		public bool FlagD => _cpu.FlagD;
		public bool FlagB => _cpu.FlagB;
		public bool FlagV => _cpu.FlagV;
		public bool FlagN => _cpu.FlagN;
		public bool FlagT => _cpu.FlagT;

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

		public int PortData => _port.ReadInput(ReadPort());

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
			ser.BeginSection("Chip6510Cpu");
			_cpu.SyncState(ser);
			ser.EndSection();

			ser.Sync("_pinNmiLast", ref _pinNmiLast);

			ser.BeginSection("_port");
			_port.SyncState(ser);
			ser.EndSection();

			ser.Sync("_thisNmi", ref _thisNmi);
			ser.Sync("LagCycles", ref LagCycles);
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
