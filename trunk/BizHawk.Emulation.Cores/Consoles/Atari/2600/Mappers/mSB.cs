using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	Cartridge class used for SB "SUPERbanking" 128k-256k bankswitched games.
	There are either 32 or 64 4K banks.
	*/
	internal class mSB : MapperBase
	{
		private int _bank4K;
		private int myStartBank
		{
			get { return (Core.Rom.Length >> 12) - 1; }
		}
		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bank_4k", ref _bank4K);
		}

		public override void HardReset()
		{
			_bank4K = 0;
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
			var temp = addr & (0x17FF + (Core.Rom.Length >> 12));
			if ((temp & 0x1800) == 0x800)
			{
				_bank4K = temp & myStartBank;
			}
		}
	}
}
