using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// an extension of the 6502 processor
	public sealed partial class Chip6510
	{
		// ------------------------------------
		private readonly MOS6502X<CpuLink> _cpu;
		private LatchedPort _port;
		private int _irqDelay;
		private int _nmiDelay;
		public C64 c64;

		private struct CpuLink : IMOS6502XLink
		{
			private readonly Chip6510 _chip;

			public CpuLink(Chip6510 chip)
			{
				_chip = chip;
			}

			public byte DummyReadMemory(ushort address) => unchecked((byte)_chip.Read(address));

			public void OnExecFetch(ushort address) => _chip.c64.ExecFetch(address);

			public byte PeekMemory(ushort address) => unchecked((byte)_chip.Peek(address));

			public byte ReadMemory(ushort address) => unchecked((byte)_chip.Read(address));

			public void WriteMemory(ushort address, byte value) => _chip.Write(address, value);
		}

		public Func<int, int> PeekMemory;
		public Action<int, int> PokeMemory;
		public Func<bool> ReadAec;
		public Func<bool> ReadIrq;
		public Func<bool> ReadNmi;
		public Func<bool> ReadRdy;
		public Func<int> ReadBus;
		public Func<int, int> ReadMemory;
		public Func<int> ReadPort;
		public Action<int, int> WriteMemory;
		public Action<int> WriteMemoryPort;

		public Action DebuggerStep;

		public Chip6510()
		{
			// configure cpu r/w
			_cpu = new MOS6502X<CpuLink>(new CpuLink(this));

			// perform hard reset
			HardReset();
		}

		public string TraceHeader => "6510: PC, machine code, mnemonic, operands, registers (A, X, Y, P, SP), flags (NVTBDIZCR)";

		public Action<TraceInfo> TraceCallback
		{
			get => _cpu.TraceCallback;
			set => _cpu.TraceCallback = value;
		}

		public void HardReset()
		{
			_cpu.NESSoftReset();
			_port = new LatchedPort
			{
				Direction = 0x00,
				Latch = 0xFF
			};
		}

		public void SoftReset()
		{
			_cpu.NESSoftReset();
			_port.Direction = 0x00;
			_port.Latch = 0xFF;
		}

		public void ExecutePhase()
		{
			_irqDelay >>= 1;
			_nmiDelay >>= 1;
			_irqDelay |= ReadIrq() ? 0x0 : 0x2;
			_nmiDelay |= ReadNmi() ? 0x0 : 0x2;
			_cpu.RDY = ReadRdy();
			_cpu.IRQ = (_irqDelay & 1) != 0;
			_cpu.NMI |= (_nmiDelay & 3) == 2;
			_cpu.ExecuteOne();
		}

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
			int ret = 0;
			
			switch (addr)
			{
				case 0x0000:
					ret = _port.Direction;
					break;
				case 0x0001:
					ret = PortData;
					break;
				default:
					if (ReadAec())
						ret = ReadMemory(addr);
					else
						ret = ReadBus();
					break;
			}

			if (c64._memoryCallbacks.HasReads)
			{
				uint flags = (uint)(MemoryCallbackFlags.CPUZero | MemoryCallbackFlags.AccessRead);
				c64._memoryCallbacks.CallMemoryCallbacks((uint)addr, (uint)ret, flags, "System Bus");
			}

			return ret;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Chip6510Cpu");
			_cpu.SyncState(ser);
			ser.EndSection();

			ser.BeginSection(nameof(_port));
			_port.SyncState(ser);
			ser.EndSection();
			
			ser.Sync(nameof(_irqDelay), ref _irqDelay);
			ser.Sync(nameof(_nmiDelay), ref _nmiDelay);
		}

		public void Write(int addr, int val)
		{
			switch (addr)
			{
				case 0x0000:
					_port.Direction = val;
					WriteMemoryPort(addr);
					break;
				case 0x0001:
					_port.Latch = val;
					WriteMemoryPort(addr);
					break;
				default:
					if (ReadAec())
						WriteMemory(addr, val);
					break;
			}

			if (c64._memoryCallbacks.HasWrites)
			{
				uint flags = (uint)(MemoryCallbackFlags.CPUZero | MemoryCallbackFlags.AccessWrite | MemoryCallbackFlags.SizeByte);
				c64._memoryCallbacks.CallMemoryCallbacks((uint)addr, (uint)val, flags, "System Bus");
			}
		}
	}
}
