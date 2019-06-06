using System;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public class SerialPort
	{
		public GBHawk Core { get; set; }

		public byte serial_control;
		public byte serial_data;
		public bool serial_start;
		public int serial_clock;
		public int serial_bits;
		public bool clk_internal;
		public int clk_rate;
		public byte going_out;
		public byte coming_in;

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
					if (((value & 0x80) > 0) && !serial_start)
					{
						serial_start = true;
						serial_bits = 8;
						if ((value & 1) > 0)
						{
							clk_internal = true;
							if (((value & 2) > 0) && Core.GBC_compat)
							{
								clk_rate = 256;
							}
							else
							{
								clk_rate = 512;
							}						
							serial_clock = clk_rate;
						}
						else
						{
							clk_internal = false;
							clk_rate = get_external_clock();
							serial_clock = clk_rate;
						}
					}
					else if (serial_start)
					{
						if ((value & 1) > 0)
						{
							clk_internal = true;
							if (((value & 2) > 0) && Core.GBC_compat)
							{
								clk_rate = 256;
							}
							else
							{
								clk_rate = 512;
							}
							serial_clock = clk_rate;
						}
						else
						{
							clk_internal = false;
							clk_rate = get_external_clock();
							serial_clock = clk_rate;
						}
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
			if (serial_start)
			{
				if (serial_clock > 0) { serial_clock--; }

				if (serial_clock == 0)
				{
					if (serial_bits > 0)
					{
						byte temp = get_external_bit();
						serial_data = (byte)((serial_data << 1) | temp);

						serial_bits--;

						if (serial_bits == 0)
						{
							serial_control &= 0x7F;
							serial_start = false;

							if (Core.REG_FFFF.Bit(3)) { Core.cpu.FlagI = true; }
							Core.REG_FF0F |= 0x08;
						}
						else
						{
							serial_clock = clk_rate;
						}
					}
				}
			}
		}

		// call this function to get the clock rate of a connected device
		// if no external device, the clocking doesn't occur
		public int get_external_clock()
		{
			return -1;
		}

		// call this function to get the next bit from the connected device
		// no device connected returns 0xFF
		public byte get_external_bit()
		{
			return coming_in;
		}

		// calling this function buts an external bit on the cable line
		public void send_external_bit(byte bit_send)
		{
			going_out = (byte)(bit_send >> 7);
		}

		public void Reset()
		{
			serial_control = 0x7E;
			serial_start = false;
			serial_data = 0x00;
			going_out = 0;
			coming_in = 1;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(serial_control), ref serial_control);
			ser.Sync(nameof(serial_data), ref serial_data);
			ser.Sync(nameof(serial_start), ref serial_start);
			ser.Sync(nameof(serial_clock), ref serial_clock);
			ser.Sync(nameof(serial_bits), ref serial_bits);
			ser.Sync(nameof(clk_internal), ref clk_internal);
			ser.Sync(nameof(clk_rate), ref clk_rate);
			ser.Sync(nameof(going_out), ref going_out);
			ser.Sync(nameof(coming_in), ref coming_in);
		}
	}
}
