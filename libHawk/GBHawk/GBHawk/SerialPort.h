#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBHawk
{
	class SerialPort
	{
	public:

		SerialPort()
		{

		};

		bool* GBC_compat = nullptr;
		bool* FlagI = nullptr;
		uint8_t* REG_FFFF = nullptr;
		uint8_t* REG_FF0F = nullptr;

		bool serial_start;
		bool can_pulse;
		uint8_t serial_control;
		uint8_t serial_data;
		uint8_t going_out;
		uint8_t coming_in;
		uint32_t serial_clock;
		uint32_t serial_bits;
		uint32_t clk_rate;

		uint8_t ReadReg(int addr)
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

		void WriteReg(int addr, uint8_t value)
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
						if (((value & 2) > 0) && GBC_compat[0])
						{
							clk_rate = 16;
						}
						else
						{
							clk_rate = 512;
						}
						serial_clock = clk_rate;
						can_pulse = true;
					}
					else
					{
						clk_rate = -1;
						serial_clock = clk_rate;
						can_pulse = false;
					}
				}
				else if (serial_start)
				{
					if ((value & 1) > 0)
					{
						if (((value & 2) > 0) && GBC_compat[0])
						{
							clk_rate = 16;
						}
						else
						{
							clk_rate = 512;
						}
						serial_clock = clk_rate;
						can_pulse = true;
					}
					else
					{
						clk_rate = -1;
						serial_clock = clk_rate;
						can_pulse = false;
					}
				}

				if (GBC_compat[0])
				{
					serial_control = (uint8_t)(0x7C | (value & 0x83)); // extra CGB bit
				}
				else
				{
					serial_control = (uint8_t)(0x7E | (value & 0x81)); // middle six bits always 1
				}

				break;
			}
		}


		void serial_transfer_tick()
		{
			if (serial_start)
			{
				if (serial_clock > 0) { serial_clock--; }

				if (serial_clock == 0)
				{
					if (serial_bits > 0)
					{
						serial_data = (uint8_t)((serial_data << 1) | coming_in);

						serial_bits--;

						if (serial_bits == 0)
						{
							serial_control &= 0x7F;
							serial_start = false;

							if ((REG_FFFF[0] & 0x8) > 0) { FlagI[0] = true; }
							REG_FF0F[0] |= 0x08;
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

		void Reset()
		{
			serial_control = 0x7E;
			serial_data = 0x00;
			serial_start = false;
			serial_clock = 0;
			serial_bits = 0;
			clk_rate = 16;
			going_out = 0;
			coming_in = 1;
			can_pulse = false;
		}

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			*saver = (uint8_t)(serial_start ? 1 : 0); saver++;
			*saver = (uint8_t)(can_pulse ? 1 : 0); saver++;

			*saver = serial_control; saver++;
			*saver = serial_data; saver++;
			*saver = going_out; saver++;
			*saver = coming_in; saver++;

			*saver = (uint8_t)(serial_clock & 0xFF); saver++; *saver = (uint8_t)((serial_clock >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((serial_clock >> 16) & 0xFF); saver++; *saver = (uint8_t)((serial_clock >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(serial_bits & 0xFF); saver++; *saver = (uint8_t)((serial_bits >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((serial_bits >> 16) & 0xFF); saver++; *saver = (uint8_t)((serial_bits >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(clk_rate & 0xFF); saver++; *saver = (uint8_t)((clk_rate >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((clk_rate >> 16) & 0xFF); saver++; *saver = (uint8_t)((clk_rate >> 24) & 0xFF); saver++;

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			serial_start = *loader == 1; loader++;
			can_pulse = *loader == 1; loader++;

			serial_control = *loader; loader++;
			serial_data = *loader; loader++;
			going_out = *loader; loader++;
			coming_in = *loader; loader++;

			serial_clock = *loader; loader++; serial_clock |= (*loader << 8); loader++;
			serial_clock |= (*loader << 16); loader++; serial_clock |= (*loader << 24); loader++;

			serial_bits = *loader; loader++; serial_bits |= (*loader << 8); loader++;
			serial_bits |= (*loader << 16); loader++; serial_bits |= (*loader << 24); loader++;

			clk_rate = *loader; loader++; clk_rate |= (*loader << 8); loader++;
			clk_rate |= (*loader << 16); loader++; clk_rate |= (*loader << 24); loader++;

			return loader;
		}

		#pragma endregion
	};
}
