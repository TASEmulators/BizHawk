using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// DRAM for the c64
	// 4164 = 64 kbit
	// 4464 = 256 kbit
	// 4864 = 512 kbit

	// for purposes of simplification we'll just
	// use one 4864, the C64 can use sets of 4164 or
	// 4464 typically

	// memory is striped 00/FF at intervals of 0x40

	public sealed class Chip4864
	{
	    private int[] _ram;

		public Chip4864()
		{
			_ram = new int[0x10000];
			HardReset();
		}

		public void HardReset()
		{
			// stripe the ram
			for (var i = 0; i < 10000; i++)
				_ram[i] = (i & 0x40) != 0 ? 0xFF : 0x00;
		}

		public int Peek(int addr)
		{
			return _ram[addr];
		}

		public void Poke(int addr, int val)
		{
			_ram[addr] = val;
		}

		public int Read(int addr)
		{
			return _ram[addr];
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}

		public void Write(int addr, int val)
		{
			_ram[addr] = val;
		}
	}
}
