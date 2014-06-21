using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	E0 (Parker Bros)
	-----

	Parker Brothers used this, and it was used on one other game (Tooth Protectors).  It
	uses 8K of ROM and can map 1K sections of it.

	This mapper has 4 1K banks of ROM in the address space.  The address space is broken up
	into the following locations:

	1000-13FF : To select a 1K ROM bank here, access 1FE0-1FE7 (1FE0 = select first 1K, etc)
	1400-17FF : To select a 1K ROM bank, access 1FE8-1FEF
	1800-1BFF : To select a 1K ROM bank, access 1FF0-1FF7
	1C00-1FFF : This is fixed to the last 1K ROM bank of the 8K

	Like F8, F6, etc. accessing one of the locations indicated will perform the switch.

	Example Games:
		Frogger II - Threedeep! (1983) (Parker Bros)
	*/

	internal class mE0 : MapperBase 
	{
		private int _toggle1;
		private int _toggle2;
		private int _toggle3;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("toggle1", ref _toggle1);
			ser.Sync("toggle2", ref _toggle2);
			ser.Sync("toggle3", ref _toggle3);
		}

		public override void HardReset()
		{
			_toggle1 = 0;
			_toggle2 = 0;
			_toggle3 = 0;
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

			if (addr < 0x1400)
			{
				return Core.Rom[(_toggle1 << 10) + (addr & 0x3FF)];
			}

			if (addr < 0x1800)
			{
				return Core.Rom[(_toggle2 << 10) + (addr & 0x3FF)];
			}

			if (addr < 0x1C00)
			{
				return Core.Rom[(_toggle3 << 10) + (addr & 0x3FF)];
			}

			return Core.Rom[(7 * 1024) + (addr & 0x3FF)]; // 7 because final bank is always set to last
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
			switch (addr)
			{
				case 0x1FE0:
					_toggle1 = 0;
					break;
				case 0x1FE1:
					_toggle1 = 1;
					break;
				case 0x1FE2:
					_toggle1 = 2;
					break;
				case 0x1FE3:
					_toggle1 = 3;
					break;
				case 0x1FE4:
					_toggle1 = 4;
					break;
				case 0x1FE5:
					_toggle1 = 5;
					break;
				case 0x1FE6:
					_toggle1 = 6;
					break;
				case 0x1FE7:
					_toggle1 = 7;
					break;

				case 0x1FE8:
					_toggle2 = 0;
					break;
				case 0x1FE9:
					_toggle2 = 1;
					break;
				case 0x1FEA:
					_toggle2 = 2;
					break;
				case 0x1FEB:
					_toggle2 = 3;
					break;
				case 0x1FEC:
					_toggle2 = 4;
					break;
				case 0x1FED:
					_toggle2 = 5;
					break;
				case 0x1FEE:
					_toggle2 = 6;
					break;
				case 0x1FEF:
					_toggle2 = 7;
					break;

				case 0x1FF0:
					_toggle3 = 0;
					break;
				case 0x1FF1:
					_toggle3 = 1;
					break;
				case 0x1FF2:
					_toggle3 = 2;
					break;
				case 0x1FF3:
					_toggle3 = 3;
					break;
				case 0x1FF4:
					_toggle3 = 4;
					break;
				case 0x1FF5:
					_toggle3 = 5;
					break;
				case 0x1FF6:
					_toggle3 = 6;
					break;
				case 0x1FF7:
					_toggle3 = 7;
					break;
			}
		}
	}
}
