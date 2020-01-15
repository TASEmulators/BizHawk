#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace MSXHawk
{
	class SN76489sms
	{
	public:
		uint32_t current_sample_L;
		uint32_t current_sample_R;

		SN76489sms()
		{
			Reset();
		}

		uint8_t Chan_vol[4];
		uint16_t Chan_tone[4];

		uint32_t chan_sel;
		bool vol_tone;
		bool noise_type;
		uint32_t noise_rate;
		bool noise_bit;

		bool A_L, B_L, C_L, noise_L;
		bool A_R, B_R, C_R, noise_R;

		uint32_t psg_clock;

		uint32_t clock_A, clock_B, clock_C;

		bool A_up, B_up, C_up;

		uint32_t noise_clock;
		uint32_t noise;

		uint8_t stereo_panning;
		const uint8_t LogScale[16] = { 255, 203, 161, 128, 102, 86, 64, 51, 40, 32, 26, 20, 16, 13, 10, 0 };

		void Reset()
		{
			clock_A = clock_B = clock_C = 0x1000;
			noise_clock = 0x10;
			chan_sel = 0;

			// reset the shift register
			noise = 0x40000;

			Chan_vol[0] = 0xF;
			Chan_vol[1] = 0xF;
			Chan_vol[2] = 0xF;
			Chan_vol[3] = 0xF;

			Set_Panning(0xFF);
		}

		void Set_Panning(uint8_t value)
		{
			A_L = (value & 0x10) != 0;
			A_R = (value & 0x01) != 0;
			B_L = (value & 0x20) != 0;
			B_R = (value & 0x02) != 0;
			C_L = (value & 0x40) != 0;
			C_R = (value & 0x04) != 0;
			noise_L = (value & 0x80) != 0;
			noise_R = (value & 0x08) != 0;

			stereo_panning = value;
		}

		/*
		void SyncState(Serializer ser)
		{
			ser.BeginSection("SN76489");

			ser.Sync(nameof(Chan_vol), ref Chan_vol, false);
			ser.Sync(nameof(Chan_tone), ref Chan_tone, false);

			ser.Sync(nameof(chan_sel), ref chan_sel);
			ser.Sync(nameof(vol_tone), ref vol_tone);
			ser.Sync(nameof(noise_type), ref noise_type);
			ser.Sync(nameof(noise_rate), ref noise_rate);

			ser.Sync(nameof(clock_A), ref clock_A);
			ser.Sync(nameof(clock_B), ref clock_B);
			ser.Sync(nameof(clock_C), ref clock_C);
			ser.Sync(nameof(noise_clock), ref noise_clock);
			ser.Sync(nameof(noise_bit), ref noise_bit);

			ser.Sync(nameof(psg_clock), ref psg_clock);

			ser.Sync(nameof(A_up), ref A_up);
			ser.Sync(nameof(B_up), ref B_up);
			ser.Sync(nameof(C_up), ref C_up);
			ser.Sync(nameof(noise), ref noise);

			ser.Sync(nameof(A_L), ref A_L);
			ser.Sync(nameof(B_L), ref B_L);
			ser.Sync(nameof(C_L), ref C_L);
			ser.Sync(nameof(noise_L), ref noise_L);
			ser.Sync(nameof(A_L), ref A_R);
			ser.Sync(nameof(B_L), ref B_R);
			ser.Sync(nameof(C_L), ref C_R);
			ser.Sync(nameof(noise_L), ref noise_R);

			ser.Sync(nameof(current_sample_L), ref current_sample_L);
			ser.Sync(nameof(current_sample_R), ref current_sample_R);
			ser.Sync(nameof(stereo_panning), ref stereo_panning);

			ser.EndSection();
		}
		*/
		uint8_t ReadReg()
		{
			// not used, reading not allowed, just return 0xFF
			return 0xFF;
		}

		void WriteReg(uint8_t value)
		{
			// if bit 7 is set, change the latch, otherwise modify the currently latched register
			if ((value & 0x80) > 0)
			{
				chan_sel = (value >> 5) & 3;
				vol_tone = ((value & 0x10) > 0);

				if (vol_tone)
				{
					Chan_vol[chan_sel] = (uint8_t)(value & 0xF);
				}
				else
				{
					if (chan_sel < 3)
					{
						Chan_tone[chan_sel] &= 0x3F0;
						Chan_tone[chan_sel] |= (uint16_t)(value & 0xF);
					}
					else
					{
						noise_type = ((value & 0x4) > 0);
						noise_rate = value & 3;

						// reset the shift register
						noise = 0x40000;
					}
				}
			}
			else
			{
				if (vol_tone)
				{
					Chan_vol[chan_sel] = (uint8_t)(value & 0xF);
				}
				else
				{
					if (chan_sel < 3)
					{
						Chan_tone[chan_sel] &= 0xF;
						Chan_tone[chan_sel] |= (uint16_t)((value & 0x3F) << 4);
					}
					else
					{
						noise_type = ((value & 0x4) > 0);
						noise_rate = value & 3;

						// reset the shift register
						noise = 0x40000;
					}
				}
			}
		}

		void generate_sound()
		{
			// there are 16 cpu cycles for every psg cycle
			psg_clock++;

			if (psg_clock == 16)
			{
				psg_clock = 0;

				clock_A--;
				clock_B--;
				clock_C--;
				noise_clock--;

				// clock noise
				if (noise_clock == 0)
				{
					noise_bit = ((noise & 1) > 0);
					if (noise_type)
					{
						noise = (((noise & 1) ^ ((noise >> 1) & 1)) << 14) | (noise >> 1);
					}
					else
					{
						noise = ((noise & 1) << 14) | (noise >> 1);
					}

					if (noise_rate == 0)
					{
						noise_clock = 0x10;
					}
					else if (noise_rate == 1)
					{
						noise_clock = 0x20;
					}
					else if (noise_rate == 2)
					{
						noise_clock = 0x40;
					}
					else
					{
						noise_clock = Chan_tone[2] + 1;
					}

					noise_clock *= 2;
				}

				if (clock_A == 0)
				{
					A_up = !A_up;
					clock_A = Chan_tone[0] + 1;
				}

				if (clock_B == 0)
				{
					B_up = !B_up;
					clock_B = Chan_tone[1] + 1;
				}

				if (clock_C == 0)
				{
					C_up = !C_up;
					clock_C = Chan_tone[2] + 1;
				}

				// now calculate the volume of each channel and add them together
				current_sample_L = (A_L ? (A_up ? LogScale[Chan_vol[0]] * 42 : 0) : 0);

				current_sample_L += (B_L ? (B_up ? LogScale[Chan_vol[1]] * 42 : 0) : 0);

				current_sample_L += (C_L ? (C_up ? LogScale[Chan_vol[2]] * 42 : 0) : 0);

				current_sample_L += (noise_L ? (noise_bit ? LogScale[Chan_vol[3]] * 42 : 0) : 0);

				current_sample_R = (A_R ? (A_up ? LogScale[Chan_vol[0]] * 42 : 0) : 0);

				current_sample_R += (B_R ? (B_up ? LogScale[Chan_vol[1]] * 42 : 0) : 0);

				current_sample_R += (C_R ? (C_up ? LogScale[Chan_vol[2]] * 42 : 0) : 0);

				current_sample_R += (noise_R ? (noise_bit ? LogScale[Chan_vol[3]] * 42 : 0) : 0);
			}
		}
	};
}