using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// MBC1 with bank switching and RAM
	public class MapperMBC1 : MapperBase
	{
		public int ROM_bank;
		public int RAM_bank;
		public bool RAM_enable;
		public bool sel_mode;
		public int ROM_mask;
		public int RAM_mask;

		public override void Initialize()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			sel_mode = false;
			ROM_mask = Core._rom.Length / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }
			
			RAM_mask = 0;
			if (Core.cart_RAM != null)
			{
				RAM_mask = Core.cart_RAM.Length / 0x2000 - 1;
				if (Core.cart_RAM.Length == 0x800) { RAM_mask = 0; }
			}
	}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed, but is still effected by mode
				if (sel_mode)
				{
					return Core._rom[(ROM_bank & 0x60) * 0x4000 + addr];
				}
				else
				{
					return Core._rom[addr];
				}
			}
			else if (addr < 0x8000)
			{
				return Core._rom[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else
			{
				if (Core.cart_RAM != null)
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
				if (sel_mode)
				{
					SetCDLROM(flags, (ROM_bank & 0x60) * 0x4000 + addr);
				}
				else
				{
					SetCDLROM(flags, addr);
				}
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else
			{
				if (Core.cart_RAM != null)
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
				else if (addr < 0x4000)
				{
					value &= 0x1F;

					// writing zero gets translated to 1
					if (value == 0) { value = 1; }

					ROM_bank &= 0xE0;
					ROM_bank |= value;
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x6000)
				{
					if (sel_mode && Core.cart_RAM != null)
					{
						RAM_bank = value & 3;
						RAM_bank &= RAM_mask;
					}
					else
					{
						ROM_bank &= 0x1F;
						ROM_bank |= ((value & 3) << 5);
						ROM_bank &= ROM_mask;
					}
				}
				else
				{
					sel_mode = (value & 1) > 0;

					if (sel_mode && Core.cart_RAM != null)
					{
						ROM_bank &= 0x1F;
						ROM_bank &= ROM_mask;
					}
					else
					{
						RAM_bank = 0;
					}
				}
			}
			else
			{
				if (Core.cart_RAM != null)
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length))
					{
						Core.cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
					}
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
			ser.Sync(nameof(sel_mode), ref sel_mode);
		}
	}
}
