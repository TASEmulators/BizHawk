using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// Tama 5 mapper used in tamagatchi 3
	public class MapperTAMA5 : MapperBase
	{
		public int ROM_bank;
		public int RAM_bank;
		public int ROM_mask;
		public int RAM_mask;
		public byte[] RTC_regs = new byte[10];
		public int RTC_timer;
		public int RTC_low_clock;
		public bool halt;
		public int RTC_offset;
		public int ctrl;
		public int RAM_addr_low;
		public int RAM_addr_high;
		public int RAM_val_low;
		public int RAM_val_high;
		public byte Chip_return_low;
		public byte Chip_return_high;

		public override void Initialize()
		{
			ROM_bank = 0;
			RAM_bank = 0;
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

			ctrl = 0;
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

				switch (ctrl)
				{
					case 0xA:
						// The game won't proceed unless this value (anded with 3) is 1
						// see bank 0: 0x1A7D to 0x1A89
						return 1;
					case 0xC:
						//Console.WriteLine("read low: " + Chip_return_low);
						return Chip_return_low;
					case 0xD:
						//Console.WriteLine("read high: " + Chip_return_high);
						return Chip_return_high;
				}

				return 0x0;
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

			}
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMemory(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr == 0xA000)
			{
				switch (ctrl)
				{
					case 0:
						ROM_bank &= 0xF0;
						ROM_bank |= (value & 0xF);
						break;
					case 1:
						ROM_bank &= 0x0F;
						ROM_bank |= ((value & 0x1) << 4);
						break;
					case 4:
						RAM_val_low = (value & 0xF);
						break;
					case 5:
						RAM_val_high = (value & 0xF);
						//Core.cart_RAM[(RAM_addr_high << 4) | RAM_addr_low] = (byte)((RAM_val_high << 4) | RAM_val_low);
						break;
					case 6:
						RAM_addr_high = (value & 1);

						switch ((value & 0xE) >> 1)
						{
							case 0:
								// write to RAM
								Core.cart_RAM[(RAM_addr_high << 4) | RAM_addr_low] = (byte)((RAM_val_high << 4) | RAM_val_low);
								break;
							case 1:
								// read from RAM
								Chip_return_high = (byte)(Core.cart_RAM[(RAM_addr_high << 4) | RAM_addr_low] >> 4);
								Chip_return_low = (byte)(Core.cart_RAM[(RAM_addr_high << 4) | RAM_addr_low] & 0xF);
								break;
							case 2:
								// read from RTC registers
								if (RAM_addr_low == 3)
								{
									Chip_return_high = RTC_regs[2];  
									Chip_return_low = RTC_regs[1];
								}
								else if (RAM_addr_low == 6)
								{
									Chip_return_high = RTC_regs[4];
									Chip_return_low = RTC_regs[3];
								}
								else
								{
									Chip_return_high = 1;
									Chip_return_low = 1;
								}
								break;
							case 3:
								// write to RTC registers (probably wrong, not well tested)
								if (RAM_addr_low == 3)
								{
									RTC_regs[2] = (byte)(RAM_val_high & 0xF);
									RTC_regs[1] = (byte)(RAM_val_low & 0xF);
								}
								else if (RAM_addr_low == 6)
								{
									RTC_regs[4] = (byte)(RAM_val_high & 0xF);
									RTC_regs[3] = (byte)(RAM_val_low & 0xF);
								}
								else
								{
									
								}
								break;
							case 4:
								// read from seconds register (time changes are checked when it rolls over)
								Chip_return_low = (byte)(RTC_regs[0] & 0xF);
								break;
						}

						//Console.WriteLine("CTRL: " + (value >> 1) + " RAM_high:" + RAM_addr_high + " RAM_low: " + RAM_addr_low + " val: " + (byte)((RAM_val_high << 4) | RAM_val_low) + " Cpu: " + Core.cpu.TotalExecutedCycles);
						break;
					case 7:
						RAM_addr_low = (value & 0xF);

						//Console.WriteLine(" RAM_low:" + RAM_addr_low + " Cpu: " + Core.cpu.TotalExecutedCycles);
						break;
				}
			}
			else if (addr == 0xA001)
			{
				ctrl = value;
			}
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}

		public override void RTC_Get(int value, int index)
		{
			if (index < 10)
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
							// 1's digit of minutes
							if (RTC_regs[1] > 9)
							{
								RTC_regs[1] = 0;
								RTC_regs[2]++;
								// 10's digit of minutes
								if (RTC_regs[2] > 5)
								{
									RTC_regs[2] = 0;
									RTC_regs[3]++;
									// 1's digit of hours
									if (RTC_regs[3] > 9)
									{
										RTC_regs[3] = 0;
										RTC_regs[4]++;
										// 10's digit of hours
										if (RTC_regs[4] > 2)
										{
											RTC_regs[4] = 0;
											RTC_regs[5]++;
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
			ser.Sync(nameof(halt), ref halt);
			ser.Sync(nameof(RTC_regs), ref RTC_regs, false);
			ser.Sync(nameof(RTC_timer), ref RTC_timer);
			ser.Sync(nameof(RTC_low_clock), ref RTC_low_clock);
			ser.Sync(nameof(RTC_offset), ref RTC_offset);
			ser.Sync(nameof(ctrl), ref ctrl);
			ser.Sync(nameof(RAM_addr_low), ref RAM_addr_low);
			ser.Sync(nameof(RAM_addr_high), ref RAM_addr_high);
			ser.Sync(nameof(RAM_val_low), ref RAM_val_low);
			ser.Sync(nameof(RAM_val_high), ref RAM_val_high);
			ser.Sync(nameof(Chip_return_low), ref Chip_return_low);
			ser.Sync(nameof(Chip_return_high), ref Chip_return_high);
		}
	}
}
