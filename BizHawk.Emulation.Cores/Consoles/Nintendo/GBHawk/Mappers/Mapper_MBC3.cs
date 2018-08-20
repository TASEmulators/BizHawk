using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

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
					if ((Core.cart_RAM != null) && (RAM_bank < 3))
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

					if ((RAM_bank >= 8) && (RAM_bank < 0xC))
					{
						return RTC_regs_latch[RAM_bank - 8];
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
					RAM_enable = ((value & 0xA) == 0xA) ? true : false;
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
					RAM_bank = value & 3;
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
					if ((Core.cart_RAM != null) && (RAM_bank <= 3))
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length)
						{
							Core.cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
						}
					}
					else if ((RAM_bank >= 8) && (RAM_bank < 0xC))
					{
						RTC_regs[RAM_bank - 8] = value;

						halt = (RTC_regs[4] & 0x40) > 0;
					}
				}
			}
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}

		public override void RTC_Get(byte value, int index)
		{
			RTC_regs[index] = value;
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

						RTC_regs[0]++;
						if (RTC_regs[0] > 59)
						{
							RTC_regs[0] = 0;
							RTC_regs[1]++;
							if (RTC_regs[1] > 59)
							{
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
			ser.Sync("ROM_Bank", ref ROM_bank);
			ser.Sync("ROM_Mask", ref ROM_mask);
			ser.Sync("RAM_Bank", ref RAM_bank);
			ser.Sync("RAM_Mask", ref RAM_mask);
			ser.Sync("RAM_enable", ref RAM_enable);
			ser.Sync("halt", ref halt);
			ser.Sync("RTC_regs", ref RTC_regs, false);
			ser.Sync("RTC_regs_latch", ref RTC_regs_latch, false);
			ser.Sync("RTC_regs_latch_wr", ref RTC_regs_latch_wr);
			ser.Sync("RTC_timer", ref RTC_timer);
			ser.Sync("RTC_low_clock", ref RTC_low_clock);
		}
	}
}
