﻿using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public class SerialPort
	{
		public GBHawk Core { get; set; }

		public byte serial_control;
		public byte serial_data;
		public int serial_clock;
		public int serial_bits;
		public int clk_rate;
		public byte going_out;
		public byte coming_in;
		public bool can_pulse;
		public bool IRQ_block;

		public byte ReadReg(int addr)
		{
			switch (addr)
			{
				case 0xFF01:
					return serial_data;
				case 0xFF02:
					return serial_control;
			}

			return 0xFF;
		}

		public void WriteReg(int addr, byte value)
		{
			switch (addr)
			{
				case 0xFF01:
					serial_data = value;
					break;

				case 0xFF02:
					if ((value & 0x80) == 0x80)
					{
						if ((value & 0x01) == 0x01)
						{
							if (((value & 2) > 0) && Core.GBC_compat)
							{
								clk_rate = 16;
								serial_clock = 16 - Core.timer.divider_reg % 8 - 1;

								// if the clock rate is changing and it's on a GBA/C, the parity of (cpu.totalexecutedcycles & 512) effects the first bit
								// Not sure exactly how yet
							}
							else
							{
								clk_rate = 512;
								serial_clock = 512 - Core.timer.divider_reg % 256 - 1;

								// there seems to be some clock inverting happening on some transfers
								// not sure of the exact nature of it, here is one method that gives correct result on one test rom but not others
								/*
								if (Core._syncSettings.GBACGB && Core.is_GBC)
								{
									if ((Core.TotalExecutedCycles % 256) > 127)
									{
										serial_clock = (8 - (int)(Core.cpu.TotalExecutedCycles % 8)) + 1;
									}
								}
								*/
							}

							can_pulse = true;
							serial_bits = 8;
						}
						else
						{
							clk_rate = -1;
							can_pulse = false;
							serial_bits = 8;
						}
					}
					else
					{
						serial_bits = 8;
						clk_rate = -1;
						serial_clock = clk_rate;
						can_pulse = false;
					}

					if (Core.GBC_compat)
					{
						serial_control = (byte)(0x7C | (value & 0x83)); // extra CGB bit
					}
					else
					{
						serial_control = (byte)(0x7E | (value & 0x81)); // middle six bits always 1
					}

					break;
			}
		}


		public void serial_transfer_tick()
		{
			IRQ_block = false;

			if (serial_clock > 0)
			{
				serial_clock--;

				if (serial_clock == 0)
				{
					if (serial_bits > 0)
					{
						serial_data = (byte)((serial_data << 1) | coming_in);

						serial_bits--;

						if (serial_bits == 0)
						{
							serial_control &= 0x7F;

							if (Core.REG_FFFF.Bit(3)) { Core.cpu.FlagI = true; }
							Core.REG_FF0F |= 0x08;
							//Console.WriteLine("SIRQ " + Core.cpu.TotalExecutedCycles);
							IRQ_block = true;
						}
						else
						{
							serial_clock = clk_rate;
							if (clk_rate > 0) { can_pulse = true; }
						}
					}
				}
			}
		}

		public void Reset()
		{
			serial_control = 0x7E;
			serial_data = 0x00;
			serial_clock = -1;
			serial_bits = 8;
			clk_rate = -1;
			going_out = 0;
			coming_in = 1;
			can_pulse = false;
			IRQ_block = false;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(serial_control), ref serial_control);
			ser.Sync(nameof(serial_data), ref serial_data);
			ser.Sync(nameof(serial_clock), ref serial_clock);
			ser.Sync(nameof(serial_bits), ref serial_bits);
			ser.Sync(nameof(clk_rate), ref clk_rate);
			ser.Sync(nameof(going_out), ref going_out);
			ser.Sync(nameof(coming_in), ref coming_in);
			ser.Sync(nameof(can_pulse), ref can_pulse);
			ser.Sync(nameof(IRQ_block), ref IRQ_block);
		}
	}
}
