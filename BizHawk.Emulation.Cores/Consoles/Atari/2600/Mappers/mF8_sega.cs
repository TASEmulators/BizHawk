using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
    /*
	F8 Sega
	-----

	Apparently some of Sega's games have banks that are physically flipped, so even though this game uses a common mapper, the initial bank that gets pointed to is incorrect.
	*/

    internal class mF8_sega : MapperBase
	{
		private int _bank4K=1;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bank_4k", ref _bank4K);
		}

		public override void HardReset()
		{
			_bank4K = 1;
			base.HardReset();
		}

		private byte ReadMem(ushort addr, bool peek)
		{
			if (!peek)
			{
				Address(addr);
			}

			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			return Core.Rom[(_bank4K << 12) + (addr & 0xFFF)];
		}

		public override byte ReadMemory(ushort addr)
		{
			return ReadMem(addr, false);
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMem(addr, true);
		}

		private void WriteMem(ushort addr, byte value, bool poke)
		{
			if (!poke)
			{
				Address(addr);
			}

			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
			}
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			WriteMem(addr, value, poke: false);
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMem(addr, value, poke: true);
		}

		private void Address(ushort addr)
		{
			if (addr == 0x1FF8)
			{
				_bank4K = 0;
			}
			else if (addr == 0x1FF9)
			{
				_bank4K = 1;
			}
		}
	}
}