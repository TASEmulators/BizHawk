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
		
		#pragma region PSG

		bool vol_tone;
		bool noise_type;
		bool noise_bit;
		bool A_L, B_L, C_L, noise_L;
		bool A_R, B_R, C_R, noise_R;
		bool A_up, B_up, C_up;

		uint8_t Chan_vol[4];
		uint8_t stereo_panning;
		uint8_t chan_sel;
		uint8_t noise_rate;

		uint16_t Chan_tone[4];
				
		uint32_t psg_clock;
		uint32_t clock_A, clock_B, clock_C;
		uint32_t noise_clock;
		uint32_t noise;

		// only old_sample_L/R is savestated, this only works if savestates are only made at frame boundaries
		// These would need to be included for subframe states
		uint32_t old_sample_L;
		uint32_t old_sample_R;
		uint32_t current_sample_L;
		uint32_t current_sample_R;
		uint32_t sampleclock;
		uint32_t num_samples_L;
		uint32_t num_samples_R;
		uint32_t samples_L[9000] = {};
		uint32_t samples_R[9000] = {};
		uint32_t Clock_Divider;

		const uint8_t LogScale[16] = { 255, 203, 161, 128, 102, 86, 64, 51, 40, 32, 26, 20, 16, 13, 10, 0 };

		SN76489sms()
		{
			Reset();
		}

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

			if ((current_sample_L != old_sample_L) && (num_samples_L < 4500))
			{
				samples_L[num_samples_L * 2] = sampleclock;
				samples_L[num_samples_L * 2 + 1] = current_sample_L - old_sample_L;
				num_samples_L++;
				old_sample_L = current_sample_L;
			}

			if ((current_sample_R != old_sample_R) && (num_samples_R < 4500))
			{
				samples_R[num_samples_R * 2] = sampleclock;
				samples_R[num_samples_R * 2 + 1] = current_sample_R - old_sample_R;
				num_samples_R++;
				old_sample_R = current_sample_R;
			}				
		}

		#pragma endregion

		#pragma region State Save / Load

		void SaveState(uint8_t* saver)
		{
			*saver = (uint8_t)(vol_tone ? 1 : 0); saver++;
			*saver = (uint8_t)(noise_type ? 1 : 0); saver++;
			*saver = (uint8_t)(noise_bit ? 1 : 0); saver++;
			*saver = (uint8_t)(A_L ? 1 : 0); saver++;
			*saver = (uint8_t)(B_L ? 1 : 0); saver++;
			*saver = (uint8_t)(C_L ? 1 : 0); saver++;
			*saver = (uint8_t)(noise_L ? 1 : 0); saver++;
			*saver = (uint8_t)(A_R ? 1 : 0); saver++;
			*saver = (uint8_t)(B_R ? 1 : 0); saver++;
			*saver = (uint8_t)(C_R ? 1 : 0); saver++;
			*saver = (uint8_t)(noise_R ? 1 : 0); saver++;
			*saver = (uint8_t)(A_up ? 1 : 0); saver++;
			*saver = (uint8_t)(B_up ? 1 : 0); saver++;
			*saver = (uint8_t)(C_up ? 1 : 0); saver++;

			*saver = Chan_vol[0]; saver++;
			*saver = Chan_vol[1]; saver++;
			*saver = Chan_vol[2]; saver++;
			*saver = Chan_vol[3]; saver++;

			*saver = stereo_panning; saver++;
			*saver = chan_sel; saver++;
			*saver = noise_rate; saver++;

			*saver = (uint8_t)(Chan_tone[0] & 0xFF); saver++; *saver = (uint8_t)((Chan_tone[0] >> 8) & 0xFF); saver++;
			*saver = (uint8_t)(Chan_tone[1] & 0xFF); saver++; *saver = (uint8_t)((Chan_tone[1] >> 8) & 0xFF); saver++;
			*saver = (uint8_t)(Chan_tone[2] & 0xFF); saver++; *saver = (uint8_t)((Chan_tone[2] >> 8) & 0xFF); saver++;
			*saver = (uint8_t)(Chan_tone[3] & 0xFF); saver++; *saver = (uint8_t)((Chan_tone[3] >> 8) & 0xFF); saver++;

			*saver = (uint8_t)(psg_clock & 0xFF); saver++; *saver = (uint8_t)((psg_clock >> 8) & 0xFF); saver++; 
			*saver = (uint8_t)((psg_clock >> 16) & 0xFF); saver++; *saver = (uint8_t)((psg_clock >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(clock_A & 0xFF); saver++; *saver = (uint8_t)((clock_A >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((clock_A >> 16) & 0xFF); saver++; *saver = (uint8_t)((clock_A >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(clock_B & 0xFF); saver++; *saver = (uint8_t)((clock_B >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((clock_B >> 16) & 0xFF); saver++; *saver = (uint8_t)((clock_B >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(clock_C & 0xFF); saver++; *saver = (uint8_t)((clock_C >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((clock_C >> 16) & 0xFF); saver++; *saver = (uint8_t)((clock_C >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(noise_clock & 0xFF); saver++; *saver = (uint8_t)((noise_clock >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((noise_clock >> 16) & 0xFF); saver++; *saver = (uint8_t)((noise_clock >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(noise & 0xFF); saver++; *saver = (uint8_t)((noise >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((noise >> 16) & 0xFF); saver++; *saver = (uint8_t)((noise >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(old_sample_L & 0xFF); saver++; *saver = (uint8_t)((old_sample_L >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((old_sample_L >> 16) & 0xFF); saver++; *saver = (uint8_t)((old_sample_L >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(old_sample_R & 0xFF); saver++; *saver = (uint8_t)((old_sample_R >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((old_sample_R >> 16) & 0xFF); saver++; *saver = (uint8_t)((old_sample_R >> 24) & 0xFF); saver++;			
		}

		void LoadState(uint8_t* loader)
		{
			vol_tone = *loader == 1; loader++;
			noise_type = *loader == 1; loader++;
			noise_bit = *loader == 1; loader++;
			A_L = *loader == 1; loader++;
			B_L = *loader == 1; loader++;
			C_L = *loader == 1; loader++;
			noise_L = *loader == 1; loader++;
			A_R = *loader == 1; loader++;
			B_R = *loader == 1; loader++;
			C_R = *loader == 1; loader++;
			noise_R = *loader == 1; loader++;
			A_up = *loader == 1; loader++;
			B_up = *loader == 1; loader++;
			C_up = *loader == 1; loader++;

			Chan_vol[0] = *loader; loader++;
			Chan_vol[1] = *loader; loader++;
			Chan_vol[2] = *loader; loader++;
			Chan_vol[3] = *loader; loader++;

			stereo_panning = *loader; loader++;
			chan_sel = *loader; loader++;
			noise_rate = *loader; loader++;

			Chan_tone[0] = *loader; loader++; Chan_tone[0] |= (*loader << 8); loader++;
			Chan_tone[1] = *loader; loader++; Chan_tone[1] |= (*loader << 8); loader++;
			Chan_tone[2] = *loader; loader++; Chan_tone[2] |= (*loader << 8); loader++;
			Chan_tone[3] = *loader; loader++; Chan_tone[3] |= (*loader << 8); loader++;

			psg_clock = *loader; loader++; psg_clock |= (*loader << 8); loader++;
			psg_clock |= (*loader << 16); loader++; psg_clock |= (*loader << 24); loader++;

			clock_A = *loader; loader++; clock_A |= (*loader << 8); loader++;
			clock_A |= (*loader << 16); loader++; clock_A |= (*loader << 24); loader++;

			clock_B = *loader; loader++; clock_B |= (*loader << 8); loader++;
			clock_B |= (*loader << 16); loader++; clock_B |= (*loader << 24); loader++;

			clock_C = *loader; loader++; clock_C |= (*loader << 8); loader++;
			clock_C |= (*loader << 16); loader++; clock_C |= (*loader << 24); loader++;

			noise_clock = *loader; loader++; noise_clock |= (*loader << 8); loader++;
			noise_clock |= (*loader << 16); loader++; noise_clock |= (*loader << 24); loader++;

			noise = *loader; loader++; noise |= (*loader << 8); loader++;
			noise |= (*loader << 16); loader++; noise |= (*loader << 24); loader++;

			old_sample_L = *loader; loader++; old_sample_L |= (*loader << 8); loader++;
			old_sample_L |= (*loader << 16); loader++; old_sample_L |= (*loader << 24); loader++;

			old_sample_R = *loader; loader++; old_sample_R |= (*loader << 8); loader++;
			old_sample_R |= (*loader << 16); loader++; old_sample_R |= (*loader << 24); loader++;
		}

		#pragma endregion
	};
}