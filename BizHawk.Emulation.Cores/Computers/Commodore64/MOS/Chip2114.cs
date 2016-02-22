using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// used as Color RAM in C64

	public sealed class Chip2114
	{
	    private int[] _ram = new int[0x400];

		public Chip2114()
		{
			HardReset();
		}

		public void HardReset()
		{
		    for (var i = 0; i < 0x400; i++)
		    {
		        _ram[i] = 0x0;
		    }
		}

		public int Peek(int addr)
		{
			return _ram[addr & 0x3FF];
		}

		public void Poke(int addr, int val)
		{
			_ram[addr & 0x3FF] = val & 0xF;
		}

		public int Read(int addr)
		{
			return _ram[addr & 0x3FF];
		}

		public int ReadInt(int addr)
		{
			return _ram[addr & 0x3FF];
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}

		public void Write(int addr, int val)
		{
			_ram[addr & 0x3FF] = val & 0xF;
		}
	}
}
