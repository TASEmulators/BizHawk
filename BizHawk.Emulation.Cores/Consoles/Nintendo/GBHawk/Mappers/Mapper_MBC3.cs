using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// MBC3 mapper with Real Time Clock
	public class MapperMBC3 : MapperBase
	{
		public int ROM_bank;
		public int RAM_bank;
		public bool RAM_enable;
		public int ROM_mask;
		public int RAM_mask;
		public byte[] RTC_regs = new byte[5];
		public byte[] RTC_regs_latch = new byte[5];
		public bool RTC_regs_latch_wr;
		public int RTC_timer;
		public int RTC_low_clock;
		public bool halt;
		public int RTC_offset;

		public override void Initialize()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = Core._rom.Length / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }

			RAM_mask = 0;
			if (Core.cart_RAM != null)
			{
				RAM_mask = Core.cart_RAM.Length / 0x2000 - 1;
				if (Core.cart_RAM.Length == 0x800) { RAM_mask = 0; }
			}

			RTC_regs[0] = 0;
			RTC_regs[1] = 0;
			RTC_regs[2] = 0;
			RTC_regs[3] = 0;
			RTC_regs[4] = 0;

			RTC_regs_latch_wr = true;
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
				if (RAM_enable)
				{
					if ((Core.cart_RAM != null) && (RAM_bank <= RAM_mask))
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length)
						{
							return Core.cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
						}
						else
						{
							return 0xFF;
						}
					}

					if ((RAM_bank >= 8) && (RAM_bank <= 0xC))
					{
						//Console.WriteLine("reg: " + (RAM_bank - 8) + " value: " + RTC_regs_latch[RAM_bank - 8] + " cpu: " + Core.cpu.TotalExecutedCycles);
						return RTC_regs_latch[RAM_bank - 8];
					}
					else
					{
						return 0x0;
					}
				}
				else
				{
					return 0x0;
				}
			}
		}

		public override void MapCDL(ushort addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				SetCDLROM(flags, addr);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else
			{
				if (RAM_enable)
				{
					if ((Core.cart_RAM != null) && (RAM_bank <= RAM_mask))
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length)
						{
							SetCDLRAM(flags, (addr - 0xA000) + RAM_bank * 0x2000);
						}
						else
						{
							return;
						}
					}

					if ((RAM_bank >= 8) && (RAM_bank <= 0xC))
					{
						return;
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
					RAM_enable = ((value & 0xA) == 0xA);
				}
				else if (addr < 0x4000)
				{
					value &= 0x7F;

					// writing zero gets translated to 1
					if (value == 0) { value = 1; }

					ROM_bank = value;
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x6000)
				{
					RAM_bank = value;
				}
				else
				{
					if (!RTC_regs_latch_wr && ((value & 1) == 1))
					{
						for (int i = 0; i < 5; i++)
						{
							RTC_regs_latch[i] = RTC_regs[i];
						}
					}

					RTC_regs_latch_wr = (value & 1) > 0;
				}
			}
			else
			{
				if (RAM_enable)
				{
					if ((Core.cart_RAM != null) && (RAM_bank <= RAM_mask))
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length)
						{
							Core.cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
						}
					}
					else if ((RAM_bank >= 8) && (RAM_bank <= 0xC))
					{
						RTC_regs[RAM_bank - 8] = value;

						if ((RAM_bank - 8) == 0) { RTC_low_clock = RTC_timer = 0; }

						halt = (RTC_regs[4] & 0x40) > 0;
					}
				}
			}
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}

		public override void RTC_Get(int value, int index)
		{
			if (index < 5)
			{
				RTC_regs[index] = (byte)value;
			}		
			else
			{
				RTC_offset = value;
			}
		}

		public override void Mapper_Tick()
		{
			if (!halt)
			{
				RTC_timer++;

				if (RTC_timer == 128)
				{
					RTC_timer = 0;

					RTC_low_clock++;

					if (RTC_low_clock == 32768)
					{
						RTC_low_clock = 0;
						RTC_timer = RTC_offset;

						RTC_regs[0]++;

						if (RTC_regs[0] > 59)
						{
							RTC_regs[0] = 0;
							RTC_regs[1]++;
							if (RTC_regs[1] > 59)
							{
								RTC_regs[1] = 0;
								RTC_regs[2]++;
								if (RTC_regs[2] > 23)
								{
									RTC_regs[2] = 0;
									if (RTC_regs[3] < 0xFF)
									{
										RTC_regs[3]++;
									}
									else
									{
										RTC_regs[3] = 0;

										if ((RTC_regs[4] & 1) == 0)
										{
											RTC_regs[4] |= 1;
										}
										else
										{
											RTC_regs[4] &= 0xFE;
											RTC_regs[4] |= 0x80;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(ROM_bank), ref ROM_bank);
			ser.Sync(nameof(ROM_mask), ref ROM_mask);
			ser.Sync(nameof(RAM_bank), ref RAM_bank);
			ser.Sync(nameof(RAM_mask), ref RAM_mask);
			ser.Sync(nameof(RAM_enable), ref RAM_enable);
			ser.Sync(nameof(halt), ref halt);
			ser.Sync(nameof(RTC_regs), ref RTC_regs, false);
			ser.Sync(nameof(RTC_regs_latch), ref RTC_regs_latch, false);
			ser.Sync(nameof(RTC_regs_latch_wr), ref RTC_regs_latch_wr);
			ser.Sync(nameof(RTC_timer), ref RTC_timer);
			ser.Sync(nameof(RTC_low_clock), ref RTC_low_clock);
			ser.Sync(nameof(RTC_offset), ref RTC_offset);
		}
	}
}
