#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace MSXHawk
{
	class AY_3_8910
	{
	public:
		
		#pragma region AY_3_8910

		AY_3_8910()
		{
			Reset();
		}

		bool A_on, B_on, C_on;
		bool A_up, B_up, C_up;
		bool A_noise, B_noise, C_noise;
		bool env_vol_A, env_vol_B, env_vol_C;

		uint8_t env_shape;
		uint8_t port_sel;
		uint8_t vol_A, vol_B, vol_C;
		uint8_t Register[16] = {};
				
		uint32_t psg_clock;
		uint32_t sq_per_A, sq_per_B, sq_per_C;
		uint32_t clock_A, clock_B, clock_C;

		uint32_t env_per;
		uint32_t env_clock;

		int32_t env_E;
		int32_t E_up_down;

		uint32_t noise_clock;
		uint32_t noise_per;
		uint32_t noise = 0x1;

		uint32_t old_sample;

		// non stated if only on frame boundaries
		bool sound_out_A;
		bool sound_out_B;
		bool sound_out_C;

		uint8_t Clock_Divider;

		uint32_t current_sample;
		uint32_t sampleclock;
		uint32_t num_samples;
		uint32_t samples[9000] = {};

		void Reset()
		{
			clock_A = clock_B = clock_C = 0x1000;
			noise_clock = 0x20;
			port_sel = 0;

			for (int i = 0; i < 16; i++)
			{
				Register[i] = 0x0;
			}
			sync_psg_state();
		}

		short Sample()
		{
			return current_sample;
		}

		const uint32_t VolumeTable[16] =
		{
			0x0000, 0x0055, 0x0079, 0x00AB, 0x00F1, 0x0155, 0x01E3, 0x02AA,
			0x03C5, 0x0555, 0x078B, 0x0AAB, 0x0F16, 0x1555, 0x1E2B, 0x2AAA
		};

		uint8_t ReadReg()
		{
			return Register[port_sel];
		}

		void sync_psg_state()
		{
			sq_per_A = (Register[0] & 0xFF) | (((Register[1] & 0xF) << 8));
			if (sq_per_A == 0)
			{
				sq_per_A = 0x1000;
			}

			sq_per_B = (Register[2] & 0xFF) | (((Register[3] & 0xF) << 8));
			if (sq_per_B == 0)
			{
				sq_per_B = 0x1000;
			}

			sq_per_C = (Register[4] & 0xFF) | (((Register[5] & 0xF) << 8));
			if (sq_per_C == 0)
			{
				sq_per_C = 0x1000;
			}

			env_per = (Register[11] & 0xFF) | (((Register[12] & 0xFF) << 8));
			if (env_per == 0)
			{
				env_per = 0x10000;
			}

			env_per *= 2;

			A_on = (Register[7] & 0x1) > 0;
			B_on = (Register[7] & 0x2) > 0;
			C_on = (Register[7] & 0x4) > 0;
			A_noise = (Register[7] & 0x8) > 0;
			B_noise = (Register[7] & 0x10) > 0;
			C_noise = (Register[7] & 0x20) > 0;

			noise_per = Register[6] & 0x1F;
			if (noise_per == 0)
			{
				noise_per = 0x20;
			}

			uint8_t shape_select = Register[13] & 0xF;

			if (shape_select < 4) { env_shape = 0; }			
			else if (shape_select < 8) { env_shape = 1; }			
			else { env_shape = 2 + (shape_select - 8); }
				
			vol_A = Register[8] & 0xF;
			env_vol_A = ((Register[8] >> 4) & 0x1) > 0;

			vol_B = Register[9] & 0xF;
			env_vol_B = ((Register[9] >> 4) & 0x1) > 0;

			vol_C = Register[10] & 0xF;
			env_vol_C = ((Register[10] >> 4) & 0x1) > 0;
		}

		void WriteReg(uint8_t value)
		{
			value &= 0xFF;

			if (port_sel != 0xE) { Register[port_sel] = value; }
			

			sync_psg_state();

			if (port_sel == 13)
			{
				env_clock = env_per;

				if (env_shape == 0 || env_shape == 2 || env_shape == 3 || env_shape == 4 || env_shape == 5)
				{
					env_E = 15;
					E_up_down = -1;
				}
				else
				{
					env_E = 0;
					E_up_down = 1;
				}
			}
		}

		void generate_sound()
		{
			// there are 8 cpu cycles for every psg cycle
			clock_A--;
			clock_B--;
			clock_C--;

			noise_clock--;
			env_clock--;

			// clock noise
			if (noise_clock == 0)
			{
				noise = (noise >> 1) ^ (((noise &0x1) > 0) ? 0x10004 : 0);
				noise_clock = noise_per;
			}

			if (env_clock == 0)
			{
				env_clock = env_per;

				env_E += E_up_down;

				if (env_E == 16 || env_E == -1)
				{
					// we just completed a period of the envelope, determine what to do now based on the envelope shape
					if (env_shape == 0 || env_shape == 1 || env_shape == 3 || env_shape == 9)
					{
						E_up_down = 0;
						env_E = 0;
					}
					else if (env_shape == 5 || env_shape == 7)
					{
						E_up_down = 0;
						env_E = 15;
					}
					else if (env_shape == 4 || env_shape == 8)
					{
						if (env_E == 16)
						{
							env_E = 15;
							E_up_down = -1;
						}
						else
						{
							env_E = 0;
							E_up_down = 1;
						}
					}
					else if (env_shape == 2)
					{
						env_E = 15;
					}
					else
					{
						env_E = 0;
					}
				}
			}

			if (clock_A == 0)
			{
				A_up = !A_up;
				clock_A = sq_per_A;
			}

			if (clock_B == 0)
			{
				B_up = !B_up;
				clock_B = sq_per_B;
			}

			if (clock_C == 0)
			{
				C_up = !C_up;
				clock_C = sq_per_C;
			}

			sound_out_A = (((noise & 0x1) > 0) | A_noise) & (A_on | A_up);
			sound_out_B = (((noise & 0x1) > 0) | B_noise) & (B_on | B_up);
			sound_out_C = (((noise & 0x1) > 0) | C_noise) & (C_on | C_up);

			// now calculate the volume of each channel and add them together
			current_sample = 0;

			if (env_vol_A)
			{
				current_sample = (sound_out_A ? VolumeTable[env_E] : 0);
			}
			else
			{
				current_sample = (sound_out_A ? VolumeTable[vol_A] : 0);
			}

			if (env_vol_B)
			{
				current_sample += (sound_out_B ? VolumeTable[env_E] : 0);
			}
			else
			{
				current_sample += (sound_out_B ? VolumeTable[vol_B] : 0);
			}

			if (env_vol_C)
			{
				current_sample += (sound_out_C ? VolumeTable[env_E] : 0);
			}
			else
			{
				current_sample += (sound_out_C ? VolumeTable[vol_C] : 0);
			}

			if ((current_sample != old_sample) && (num_samples < 4500))
			{
				samples[num_samples * 2] = sampleclock;
				samples[num_samples * 2 + 1] = current_sample - old_sample;
				num_samples++;
				old_sample = current_sample;
			}
		}

		#pragma endregion

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			*saver = (uint8_t)(A_on ? 1 : 0); saver++;
			*saver = (uint8_t)(B_on ? 1 : 0); saver++;
			*saver = (uint8_t)(C_on ? 1 : 0); saver++;
			*saver = (uint8_t)(A_up ? 1 : 0); saver++;
			*saver = (uint8_t)(B_up ? 1 : 0); saver++;
			*saver = (uint8_t)(C_up ? 1 : 0); saver++;
			*saver = (uint8_t)(A_noise ? 1 : 0); saver++;
			*saver = (uint8_t)(B_noise ? 1 : 0); saver++;
			*saver = (uint8_t)(C_noise ? 1 : 0); saver++;
			*saver = (uint8_t)(env_vol_A ? 1 : 0); saver++;
			*saver = (uint8_t)(env_vol_B ? 1 : 0); saver++;
			*saver = (uint8_t)(env_vol_C ? 1 : 0); saver++;

			*saver = env_shape; saver++;
			*saver = port_sel; saver++;
			*saver = vol_A; saver++;
			*saver = vol_B; saver++;
			*saver = vol_C; saver++;

			for (int i = 0; i < 16; i++) { *saver = Register[i]; saver++; }

			*saver = (uint8_t)(psg_clock & 0xFF); saver++; *saver = (uint8_t)((psg_clock >> 8) & 0xFF); saver++; 
			*saver = (uint8_t)((psg_clock >> 16) & 0xFF); saver++; *saver = (uint8_t)((psg_clock >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(sq_per_A & 0xFF); saver++; *saver = (uint8_t)((sq_per_A >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((sq_per_A >> 16) & 0xFF); saver++; *saver = (uint8_t)((sq_per_A >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(sq_per_B & 0xFF); saver++; *saver = (uint8_t)((sq_per_B >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((sq_per_B >> 16) & 0xFF); saver++; *saver = (uint8_t)((sq_per_B >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(sq_per_C & 0xFF); saver++; *saver = (uint8_t)((sq_per_C >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((sq_per_C >> 16) & 0xFF); saver++; *saver = (uint8_t)((sq_per_C >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(clock_A & 0xFF); saver++; *saver = (uint8_t)((clock_A >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((clock_A >> 16) & 0xFF); saver++; *saver = (uint8_t)((clock_A >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(clock_B & 0xFF); saver++; *saver = (uint8_t)((clock_B >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((clock_B >> 16) & 0xFF); saver++; *saver = (uint8_t)((clock_B >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(clock_C & 0xFF); saver++; *saver = (uint8_t)((clock_C >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((clock_C >> 16) & 0xFF); saver++; *saver = (uint8_t)((clock_C >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(env_per & 0xFF); saver++; *saver = (uint8_t)((env_per >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((env_per >> 16) & 0xFF); saver++; *saver = (uint8_t)((env_per >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(env_clock & 0xFF); saver++; *saver = (uint8_t)((env_clock >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((env_clock >> 16) & 0xFF); saver++; *saver = (uint8_t)((env_clock >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(env_E & 0xFF); saver++; *saver = (uint8_t)((env_E >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((env_E >> 16) & 0xFF); saver++; *saver = (uint8_t)((env_E >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(E_up_down & 0xFF); saver++; *saver = (uint8_t)((E_up_down >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((E_up_down >> 16) & 0xFF); saver++; *saver = (uint8_t)((E_up_down >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(noise_clock & 0xFF); saver++; *saver = (uint8_t)((noise_clock >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((noise_clock >> 16) & 0xFF); saver++; *saver = (uint8_t)((noise_clock >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(noise_per & 0xFF); saver++; *saver = (uint8_t)((noise_per >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((noise_per >> 16) & 0xFF); saver++; *saver = (uint8_t)((noise_per >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(noise & 0xFF); saver++; *saver = (uint8_t)((noise >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((noise >> 16) & 0xFF); saver++; *saver = (uint8_t)((noise >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(old_sample & 0xFF); saver++; *saver = (uint8_t)((old_sample >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((old_sample >> 16) & 0xFF); saver++; *saver = (uint8_t)((old_sample >> 24) & 0xFF); saver++;

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			A_on = *loader == 1; loader++;
			B_on = *loader == 1; loader++;
			C_on = *loader == 1; loader++;
			A_up = *loader == 1; loader++;
			B_up = *loader == 1; loader++;
			C_up = *loader == 1; loader++;
			A_noise = *loader == 1; loader++;
			B_noise = *loader == 1; loader++;
			C_noise = *loader == 1; loader++;
			env_vol_A = *loader == 1; loader++;
			env_vol_B = *loader == 1; loader++;
			env_vol_C = *loader == 1; loader++;

			env_shape = *loader; loader++;
			port_sel = *loader; loader++;
			vol_A = *loader; loader++;
			vol_B = *loader; loader++;
			vol_C = *loader; loader++;

			for (int i = 0; i < 16; i++) { Register[i] = *loader; loader++; }

			psg_clock = *loader; loader++; psg_clock |= (*loader << 8); loader++;
			psg_clock |= (*loader << 16); loader++; psg_clock |= (*loader << 24); loader++;

			sq_per_A = *loader; loader++; sq_per_A |= (*loader << 8); loader++;
			sq_per_A |= (*loader << 16); loader++; sq_per_A |= (*loader << 24); loader++;

			sq_per_B = *loader; loader++; sq_per_B |= (*loader << 8); loader++;
			sq_per_B |= (*loader << 16); loader++; sq_per_B |= (*loader << 24); loader++;

			sq_per_C = *loader; loader++; sq_per_C |= (*loader << 8); loader++;
			sq_per_C |= (*loader << 16); loader++; sq_per_C |= (*loader << 24); loader++;

			clock_A = *loader; loader++; clock_A |= (*loader << 8); loader++;
			clock_A |= (*loader << 16); loader++; clock_A |= (*loader << 24); loader++;

			clock_B = *loader; loader++; clock_B |= (*loader << 8); loader++;
			clock_B |= (*loader << 16); loader++; clock_B |= (*loader << 24); loader++;

			clock_C = *loader; loader++; clock_C |= (*loader << 8); loader++;
			clock_C |= (*loader << 16); loader++; clock_C |= (*loader << 24); loader++;

			env_per = *loader; loader++; env_per |= (*loader << 8); loader++;
			env_per |= (*loader << 16); loader++; env_per |= (*loader << 24); loader++;

			env_clock = *loader; loader++; env_clock |= (*loader << 8); loader++;
			env_clock |= (*loader << 16); loader++; env_clock |= (*loader << 24); loader++;

			env_E = *loader; loader++; env_E |= (*loader << 8); loader++;
			env_E |= (*loader << 16); loader++; env_E |= (*loader << 24); loader++;

			E_up_down = *loader; loader++; E_up_down |= (*loader << 8); loader++;
			E_up_down |= (*loader << 16); loader++; E_up_down |= (*loader << 24); loader++;

			noise_clock = *loader; loader++; noise_clock |= (*loader << 8); loader++;
			noise_clock |= (*loader << 16); loader++; noise_clock |= (*loader << 24); loader++;

			noise_per = *loader; loader++; noise_per |= (*loader << 8); loader++;
			noise_per |= (*loader << 16); loader++; noise_per |= (*loader << 24); loader++;

			noise = *loader; loader++; noise |= (*loader << 8); loader++;
			noise |= (*loader << 16); loader++; noise |= (*loader << 24); loader++;

			old_sample = *loader; loader++; old_sample |= (*loader << 8); loader++;
			old_sample |= (*loader << 16); loader++; old_sample |= (*loader << 24); loader++;

			return loader;
		}

		#pragma endregion
	};
}