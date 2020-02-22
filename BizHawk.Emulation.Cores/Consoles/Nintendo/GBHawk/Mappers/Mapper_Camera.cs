using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// Gameboy Camera Mapper (no camera support yet)
	// 128  kb of RAM
	public class MapperCamera : MapperBase
	{
		public int ROM_bank;
		public int RAM_bank;
		public bool RAM_enable;
		public int ROM_mask;
		public int RAM_mask;

		public override void Reset()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = Core._rom.Length / 0x4000 - 1;

			RAM_mask = 0;

			RAM_mask = Core.cart_RAM.Length / 0x2000 - 1;
			if (Core.cart_RAM.Length == 0x800) { RAM_mask = 0; }
		}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x4000)
			{
				return Core._rom[addr];
			}
			else if (addr < 0x8000)
			{
				return Core._rom[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else
			{
				if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length))
				{
					return Core.cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
				}
				else
				{
					return 0xFF;
				}
			}
		}

		public override void MapCDL(ushort addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed, but is still effected by mode
				SetCDLROM(flags, addr);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else
			{
				if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length))
				{
					SetCDLRAM(flags, (addr - 0xA000) + RAM_bank * 0x2000);
				}
				else
				{
					return;
				}
			}
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMemory(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x8000)
			{
				if (addr < 0x2000)
				{
					RAM_enable = (value & 0xF) == 0xA;
				}
				else if (addr < 0x3000)
				{
					ROM_bank = value;
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x4000)
				{

				}
				else if (addr < 0x5000)
				{
					//registers
				}
				else if (addr < 0x6000)
				{
					ROM_bank &= 0x1F;
					ROM_bank |= ((value & 3) << 5);
					ROM_bank &= ROM_mask;
				}
				else
				{
					RAM_bank = 0;
				}
			}
			else
			{
				if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length))
				{
					Core.cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
				}
			}
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(ROM_bank), ref ROM_bank);
			ser.Sync(nameof(ROM_mask), ref ROM_mask);
			ser.Sync(nameof(RAM_bank), ref RAM_bank);
			ser.Sync(nameof(RAM_mask), ref RAM_mask);
			ser.Sync(nameof(RAM_enable), ref RAM_enable);
		}
	}
}
