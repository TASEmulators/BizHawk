using BizHawk.Common;

using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// Sachen Bootleg Mapper
	// NOTE: Normally, locked mode is disabled after 31 rises of A15
	// this occurs when the Boot Rom is loading the nintendo logo into VRAM
	// instead of tracking that in the main memory map where it will just slow things down for no reason
	// we'll clear the 'locked' flag when the last byte of the logo is read
	internal class MapperSachen2 : MapperBase
	{
		public int ROM_bank;
		public bool locked, locked_GBC, finished;
		public int ROM_mask;
		public int ROM_bank_mask;
		public int BASE_ROM_Bank;
		public bool reg_access;
		public ushort addr_last;
		public int counter;

		public override void Reset()
		{
			ROM_bank = 1;
			ROM_mask = Core._rom.Length / 0x4000 - 1;
			BASE_ROM_Bank = 0;
			ROM_bank_mask = 0;
			locked = true;
			locked_GBC = false;
			finished = false;
			reg_access = false;
			addr_last = 0;
			counter = 0;
		}

		public override byte ReadMemoryLow(ushort addr)
		{
			if (addr < 0x4000)
			{
				// header is scrambled
				if ((addr >= 0x100) && (addr < 0x200))
				{
					int temp0 = (addr & 1);
					int temp1 = (addr & 2);
					int temp4 = (addr & 0x10);
					int temp6 = (addr & 0x40);

					temp0 <<= 6;
					temp1 <<= 3;
					temp4 >>= 3;
					temp6 >>= 6;

					addr &= 0x1AC;
					addr |= (ushort)(temp0 | temp1 | temp4 | temp6);
				}

				if (locked_GBC) { addr |= 0x80; }

				return Core._rom[addr + BASE_ROM_Bank * 0x4000];
			}
			else
			{
				int temp_bank = (ROM_bank & ~ROM_bank_mask) | (ROM_bank_mask & BASE_ROM_Bank);
				temp_bank &= ROM_mask;

				return Core._rom[(addr - 0x4000) + temp_bank * 0x4000];
			}
		}

		public override byte ReadMemoryHigh(ushort addr)
		{
			return 0xFF;
		}

		public override void MapCDL(ushort addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				// header is scrambled
				if ((addr >= 0x100) && (addr < 0x200))
				{
					int temp0 = (addr & 1);
					int temp1 = (addr & 2);
					int temp4 = (addr & 0x10);
					int temp6 = (addr & 0x40);

					temp0 <<= 6;
					temp1 <<= 3;
					temp4 >>= 3;
					temp6 >>= 6;

					addr &= 0x1AC;
					addr |= (ushort)(temp0 | temp1 | temp4 | temp6);
				}

				if (locked_GBC) { addr |= 0x80; }

				SetCDLROM(flags, addr + BASE_ROM_Bank * 0x4000);
			}
			else if (addr < 0x8000)
			{
				int temp_bank = (ROM_bank & ~ROM_bank_mask) | (ROM_bank_mask & BASE_ROM_Bank);
				temp_bank &= ROM_mask;

				SetCDLROM(flags, (addr - 0x4000) + temp_bank * 0x4000);
			}
			else
			{
				return;
			}
		}

		public override byte PeekMemoryLow(ushort addr)
		{
			return ReadMemoryLow(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (reg_access)
				{
					BASE_ROM_Bank = value;
				}
			}
			else if (addr < 0x4000)
			{
				ROM_bank = (value > 0) ? (value) : 1;

				if ((value & 0x30) == 0x30)
				{
					reg_access = true;
				}
				else
				{
					reg_access = false;
				}
			}
			else if (addr < 0x6000)
			{
				if (reg_access)
				{
					ROM_bank_mask = value;
				}
			}
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}

		public override void Mapper_Tick()
		{
			if (locked)
			{
				if (((Core.addr_access & 0x8000) == 0) && ((addr_last & 0x8000) > 0) && (Core.addr_access >= 0x100))
				{
					counter++;
				}

				if (Core.addr_access >= 0x100)
				{
					addr_last = Core.addr_access;
				}

				if (counter == 0x30)
				{
					locked = false;
					locked_GBC = true;
					counter = 0;
				}
			}
			else if (locked_GBC)
			{
				if (((Core.addr_access & 0x8000) == 0) && ((addr_last & 0x8000) > 0) && (Core.addr_access >= 0x100))
				{
					counter++;
				}

				if (Core.addr_access >= 0x100)
				{
					addr_last = Core.addr_access;
				}

				if (counter == 0x30)
				{
					locked_GBC = false;
					finished = true;
					Console.WriteLine("Finished");
					Console.WriteLine(Core.cpu.TotalExecutedCycles);
				}

				// The above condition seems to never be reached as described in the mapper notes
				// so for now add this one

				if ((Core.addr_access == 0x133) && (counter == 1))
				{
					locked_GBC = false;
					finished = true;
					Console.WriteLine("Unlocked");
				}
			}
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(ROM_bank), ref ROM_bank);
			ser.Sync(nameof(ROM_mask), ref ROM_mask);
			ser.Sync(nameof(locked), ref locked);
			ser.Sync(nameof(locked_GBC), ref locked_GBC);
			ser.Sync(nameof(finished), ref finished);
			ser.Sync(nameof(ROM_bank_mask), ref ROM_bank_mask);
			ser.Sync(nameof(BASE_ROM_Bank), ref BASE_ROM_Bank);
			ser.Sync(nameof(reg_access), ref reg_access);
			ser.Sync(nameof(addr_last), ref addr_last);
			ser.Sync(nameof(counter), ref counter);
		}
	}
}