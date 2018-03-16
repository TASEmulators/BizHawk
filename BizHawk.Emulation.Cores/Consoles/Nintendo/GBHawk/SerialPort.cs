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
							clk_rate = 512;
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
							clk_rate = 512;
							serial_clock = clk_rate;
						}
						else
						{
							clk_internal = false;
							clk_rate = get_external_clock();
							serial_clock = clk_rate;
						}
					}

					serial_control = (byte)(0x7E | (value & 0x81)); // middle six bits always 1
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
						send_external_bit((byte)(serial_data & 0x80));

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
			return 1;
		}

		public void send_external_bit(byte bit_send)
		{

		}

		public void Reset()
		{
			serial_control = 0x7E;
			serial_start = false;
			serial_data = 0x00;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("serial_control", ref serial_control);
			ser.Sync("serial_data", ref serial_data);
			ser.Sync("serial_start", ref serial_start);
			ser.Sync("serial_clock", ref serial_clock);
			ser.Sync("serial_bits", ref serial_bits);
			ser.Sync("clk_internal", ref clk_internal);
			ser.Sync("clk_rate", ref clk_rate);
		}
	}
}
