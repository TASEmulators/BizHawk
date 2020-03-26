#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBHawk
{
	class GBAudio
	{
	public:
		
		#pragma region GBAudio

		// Core variables
		bool* is_GBC = nullptr;
		bool* double_speed = nullptr;
		uint32_t* timer_div_reg = nullptr;

		uint32_t num_samples_L, num_samples_R;
		int32_t samples_L[9000] = {};
		int32_t samples_R[9000] = {};

		bool DUTY_CYCLES[32] = {false, false, false, false, false, false, false, true,
								true, false, false, false, false, false, false, true,
								true, false, false, false, false, true, true, true,
								false, true, true, true, true, true, true, false};

		uint32_t DIVISOR[8] = { 8, 16, 32, 48, 64, 80, 96, 112 };


		uint32_t NR10 = 0;
		uint32_t NR11 = 1;
		uint32_t NR12 = 2;
		uint32_t NR13 = 3;
		uint32_t NR14 = 4;
		uint32_t NR21 = 5;
		uint32_t NR22 = 6;
		uint32_t NR23 = 7;
		uint32_t NR24 = 8;
		uint32_t NR30 = 9;
		uint32_t NR31 = 10;
		uint32_t NR32 = 11;
		uint32_t NR33 = 12;
		uint32_t NR34 = 13;
		uint32_t NR41 = 14;
		uint32_t NR42 = 15;
		uint32_t NR43 = 16;
		uint32_t NR44 = 17;
		uint32_t NR50 = 18;
		uint32_t NR51 = 19;
		uint32_t NR52 = 20;

		uint32_t unused_bits[21] = { 0x80, 0x3F, 0x00, 0xFF, 0xBF,
									0x3F, 0x00, 0xFF, 0xBF,
									0x7F, 0xFF, 0x9F, 0xFF, 0xBF,
									0xFF, 0x00, 0x00, 0xBF,
									0x00, 0x00, 0x70};

		uint8_t Audio_Regs[21];

		uint8_t Wave_RAM[16];


		// Audio Variables
		// derived
		bool												WAVE_DAC_pow;
		bool																			NOISE_wdth_md;
		bool     SQ1_negate;
		bool     SQ1_trigger,			SQ2_trigger,		WAVE_trigger,				NOISE_trigger;
		bool     SQ1_len_en,			SQ2_len_en,			WAVE_len_en,				NOISE_len_en;
		bool     SQ1_env_add,			SQ2_env_add,									NOISE_env_add;
		uint8_t												WAVE_vol_code;
		uint8_t																			NOISE_clk_shft;
		uint8_t																			NOISE_div_code;
		uint8_t  SQ1_shift;
		uint8_t  SQ1_duty,				SQ2_duty;
		uint8_t  SQ1_st_vol,			SQ2_st_vol,										NOISE_st_vol;
		uint8_t  SQ1_per,				SQ2_per,										NOISE_per;
		uint8_t  SQ1_swp_prd;
		uint32_t SQ1_frq,				SQ2_frq,			WAVE_frq;
		uint32_t SQ1_length,			SQ2_length,			WAVE_length,				NOISE_length;
		// state
		bool												WAVE_can_get;
		bool     SQ1_calc_done;
		bool     SQ1_swp_enable;
		bool     SQ1_vol_done,			SQ2_vol_done,									NOISE_vol_done;
		bool     SQ1_enable,			SQ2_enable,			WAVE_enable,				NOISE_enable;
		uint8_t  SQ1_vol_state,			SQ2_vol_state,									NOISE_vol_state;
		uint8_t  SQ1_duty_cntr,			SQ2_duty_cntr;
		uint8_t												WAVE_wave_cntr;
		uint32_t SQ1_frq_shadow;
		uint32_t SQ1_intl_cntr,			SQ2_intl_cntr,		WAVE_intl_cntr,				NOISE_intl_cntr;
		uint32_t SQ1_vol_per,			SQ2_vol_per,									NOISE_vol_per;
		uint32_t SQ1_intl_swp_cnt;
		uint32_t																		NOISE_LFSR;
		uint32_t SQ1_len_cntr,			SQ2_len_cntr,		WAVE_len_cntr,				NOISE_len_cntr;
		// computed
		uint32_t SQ1_output,			SQ2_output,			WAVE_output,				NOISE_output;

		// Contol Variables
		bool AUD_CTRL_vin_L_en;
		bool AUD_CTRL_vin_R_en;
		bool AUD_CTRL_sq1_L_en;
		bool AUD_CTRL_sq2_L_en;
		bool AUD_CTRL_wave_L_en;
		bool AUD_CTRL_noise_L_en;
		bool AUD_CTRL_sq1_R_en;
		bool AUD_CTRL_sq2_R_en;
		bool AUD_CTRL_wave_R_en;
		bool AUD_CTRL_noise_R_en;
		bool AUD_CTRL_power;
		uint8_t AUD_CTRL_vol_L;
		uint8_t AUD_CTRL_vol_R;

		uint32_t sequencer_len, sequencer_vol, sequencer_swp;
		bool timer_bit_old;

		uint8_t sample;

		uint32_t master_audio_clock;

		uint32_t latched_sample_L, latched_sample_R;

		uint32_t SQ1_bias_gain, SQ2_bias_gain, WAVE_bias_gain, NOISE_bias_gain;

		uint8_t ReadReg(int addr)
		{
			uint8_t ret = 0;

			switch (addr)
			{
			case 0xFF10: ret = (uint8_t)(Audio_Regs[NR10] | unused_bits[NR10]); break; // NR10 (sweep)
			case 0xFF11: ret = (uint8_t)(Audio_Regs[NR11] | unused_bits[NR11]); break; // NR11 (sound length / wave pattern duty %)
			case 0xFF12: ret = (uint8_t)(Audio_Regs[NR12] | unused_bits[NR12]); break; // NR12 (envelope)
			case 0xFF13: ret = (uint8_t)(Audio_Regs[NR13] | unused_bits[NR13]); break; // NR13 (freq low)
			case 0xFF14: ret = (uint8_t)(Audio_Regs[NR14] | unused_bits[NR14]); break; // NR14 (freq hi)
			case 0xFF16: ret = (uint8_t)(Audio_Regs[NR21] | unused_bits[NR21]); break; // NR21 (sound length / wave pattern duty %)
			case 0xFF17: ret = (uint8_t)(Audio_Regs[NR22] | unused_bits[NR22]); break; // NR22 (envelope)
			case 0xFF18: ret = (uint8_t)(Audio_Regs[NR23] | unused_bits[NR23]); break; // NR23 (freq low)
			case 0xFF19: ret = (uint8_t)(Audio_Regs[NR24] | unused_bits[NR24]); break; // NR24 (freq hi)
			case 0xFF1A: ret = (uint8_t)(Audio_Regs[NR30] | unused_bits[NR30]); break; // NR30 (on/off)
			case 0xFF1B: ret = (uint8_t)(Audio_Regs[NR31] | unused_bits[NR31]); break; // NR31 (length)
			case 0xFF1C: ret = (uint8_t)(Audio_Regs[NR32] | unused_bits[NR32]); break; // NR32 (level output)
			case 0xFF1D: ret = (uint8_t)(Audio_Regs[NR33] | unused_bits[NR33]); break; // NR33 (freq low)
			case 0xFF1E: ret = (uint8_t)(Audio_Regs[NR34] | unused_bits[NR34]); break; // NR34 (freq hi)
			case 0xFF20: ret = (uint8_t)(Audio_Regs[NR41] | unused_bits[NR41]); break; // NR41 (length)
			case 0xFF21: ret = (uint8_t)(Audio_Regs[NR42] | unused_bits[NR42]); break; // NR42 (envelope)
			case 0xFF22: ret = (uint8_t)(Audio_Regs[NR43] | unused_bits[NR43]); break; // NR43 (shift)
			case 0xFF23: ret = (uint8_t)(Audio_Regs[NR44] | unused_bits[NR44]); break; // NR44 (trigger)
			case 0xFF24: ret = (uint8_t)(Audio_Regs[NR50] | unused_bits[NR50]); break; // NR50 (ctrl)
			case 0xFF25: ret = (uint8_t)(Audio_Regs[NR51] | unused_bits[NR51]); break; // NR51 (ctrl)
			case 0xFF26: ret = (uint8_t)(Read_NR52() | unused_bits[NR52]); break; // NR52 (ctrl)

			// wave ram table
			case 0xFF30:
			case 0xFF31:
			case 0xFF32:
			case 0xFF33:
			case 0xFF34:
			case 0xFF35:
			case 0xFF36:
			case 0xFF37:
			case 0xFF38:
			case 0xFF39:
			case 0xFF3A:
			case 0xFF3B:
			case 0xFF3C:
			case 0xFF3D:
			case 0xFF3E:
			case 0xFF3F:
				if (WAVE_enable)
				{
					if (WAVE_can_get || is_GBC[0]) { ret = Wave_RAM[WAVE_wave_cntr >> 1]; }
					else { ret = 0xFF; }
				}
				else { ret = Wave_RAM[addr & 0x0F]; }

				break;
			}

			return ret;
		}

		void WriteReg(int addr, uint8_t value)
		{
			// while power is on, everything is writable
			//Console.WriteLine((addr & 0xFF) + " " + value);
			if (AUD_CTRL_power)
			{
				switch (addr)
				{
				case 0xFF10:                                        // NR10 (sweep)
					Audio_Regs[NR10] = value;
					SQ1_swp_prd = (uint8_t)((value & 0x70) >> 4);
					SQ1_negate = (value & 8) > 0;
					SQ1_shift = (uint8_t)(value & 7);

					if (!SQ1_negate && SQ1_calc_done) { SQ1_enable = false; calculate_bias_gain_1(); }
					break;
				case 0xFF11:                                        // NR11 (sound length / wave pattern duty %)
					Audio_Regs[NR11] = value;
					SQ1_duty = (uint8_t)((value & 0xC0) >> 6);
					SQ1_length = (uint32_t)(64 - (value & 0x3F));
					SQ1_len_cntr = SQ1_length;
					break;
				case 0xFF12:                                        // NR12 (envelope)
					SQ1_st_vol = (uint8_t)((value & 0xF0) >> 4);
					SQ1_env_add = (value & 8) > 0;
					SQ1_per = (uint8_t)(value & 7);

					// several glitchy effects happen when writing to NRx2 during audio playing
					if (((Audio_Regs[NR12] & 7) == 0) && !SQ1_vol_done) { SQ1_vol_state++; }
					else if ((Audio_Regs[NR12] & 8) == 0) { SQ1_vol_state += 2; }

					if (((Audio_Regs[NR12] ^ value) & 8) > 0) { SQ1_vol_state = (uint8_t)(0x10 - SQ1_vol_state); }

					SQ1_vol_state &= 0xF;

					if ((value & 0xF8) == 0) { SQ1_enable = SQ1_swp_enable = false; }
					Audio_Regs[NR12] = value;

					calculate_bias_gain_1();
					break;
				case 0xFF13:                                        // NR13 (freq low)
					Audio_Regs[NR13] = value;
					SQ1_frq &= 0x700;
					SQ1_frq |= value;
					break;
				case 0xFF14:                                        // NR14 (freq hi)
					Audio_Regs[NR14] = value;
					SQ1_trigger = (value & 0x80) > 0;
					SQ1_frq &= 0xFF;
					SQ1_frq |= (uint32_t)((value & 7) << 8);

					if (((sequencer_len & 1) > 0))
					{
						if (!SQ1_len_en && ((value & 0x40) > 0) && (SQ1_len_cntr > 0))
						{
							SQ1_len_cntr--;
							if ((SQ1_len_cntr == 0) && !SQ1_trigger) { SQ1_enable = SQ1_swp_enable = false; }
						}
					}

					if (SQ1_trigger)
					{
						SQ1_enable = true;
						SQ1_vol_done = false;
						if (SQ1_len_cntr == 0)
						{
							SQ1_len_cntr = 64;
							if (((value & 0x40) > 0) && ((sequencer_len & 1) > 0)) { SQ1_len_cntr--; }
						}
						SQ1_vol_state = SQ1_st_vol;
						SQ1_vol_per = (SQ1_per > 0) ? SQ1_per : 8;
						SQ1_frq_shadow = SQ1_frq;
						SQ1_intl_cntr = (2048 - SQ1_frq_shadow) * 4;

						SQ1_intl_swp_cnt = SQ1_swp_prd > 0 ? SQ1_swp_prd : 8;
						SQ1_calc_done = false;

						if ((SQ1_shift > 0) || (SQ1_swp_prd > 0))
						{
							SQ1_swp_enable = true;
						}
						else
						{
							SQ1_swp_enable = false;
						}

						if (SQ1_shift > 0)
						{
							int shadow_frq = SQ1_frq_shadow;
							shadow_frq = shadow_frq >> SQ1_shift;
							if (SQ1_negate) { shadow_frq = -shadow_frq; }
							shadow_frq += SQ1_frq_shadow;

							// disable channel if overflow
							if ((uint32_t)shadow_frq > 2047)
							{
								SQ1_enable = SQ1_swp_enable = false;
							}

							// set negate mode flag that disables channel is negate clerar
							if (SQ1_negate) { SQ1_calc_done = true; }
						}

						if ((SQ1_vol_state == 0) && !SQ1_env_add) { SQ1_enable = SQ1_swp_enable = false; }
					}

					calculate_bias_gain_1();
					SQ1_len_en = (value & 0x40) > 0;
					break;
				case 0xFF16:                                        // NR21 (sound length / wave pattern duty %)		
					Audio_Regs[NR21] = value;
					SQ2_duty = (uint8_t)((value & 0xC0) >> 6);
					SQ2_length = (uint32_t)(64 - (value & 0x3F));
					SQ2_len_cntr = SQ2_length;
					break;
				case 0xFF17:                                        // NR22 (envelope)
					SQ2_st_vol = (uint8_t)((value & 0xF0) >> 4);
					SQ2_env_add = (value & 8) > 0;
					SQ2_per = (uint8_t)(value & 7);

					// several glitchy effects happen when writing to NRx2 during audio playing
					if (((Audio_Regs[NR22] & 7) == 0) && !SQ2_vol_done) { SQ2_vol_state++; }
					else if ((Audio_Regs[NR22] & 8) == 0) { SQ2_vol_state += 2; }

					if (((Audio_Regs[NR22] ^ value) & 8) > 0) { SQ2_vol_state = (uint8_t)(0x10 - SQ2_vol_state); }

					SQ2_vol_state &= 0xF;
					if ((value & 0xF8) == 0) { SQ2_enable = false; }
					Audio_Regs[NR22] = value;

					calculate_bias_gain_2();
					break;
				case 0xFF18:                                        // NR23 (freq low)
					Audio_Regs[NR23] = value;
					SQ2_frq &= 0x700;
					SQ2_frq |= value;
					break;
				case 0xFF19:                                        // NR24 (freq hi)
					Audio_Regs[NR24] = value;
					SQ2_trigger = (value & 0x80) > 0;
					SQ2_frq &= 0xFF;
					SQ2_frq |= (uint32_t)((value & 7) << 8);

					if ((sequencer_len & 1) > 0)
					{
						if (!SQ2_len_en && ((value & 0x40) > 0) && (SQ2_len_cntr > 0))
						{
							SQ2_len_cntr--;
							if ((SQ2_len_cntr == 0) && !SQ2_trigger) { SQ2_enable = false; }
						}
					}

					if (SQ2_trigger)
					{
						SQ2_enable = true;
						SQ2_vol_done = false;

						if (SQ2_len_cntr == 0)
						{
							SQ2_len_cntr = 64;
							if (((value & 0x40) > 0) && ((sequencer_len & 1) > 0)) { SQ2_len_cntr--; }
						}
						SQ2_intl_cntr = (2048 - SQ2_frq) * 4;
						SQ2_vol_state = SQ2_st_vol;
						SQ2_vol_per = (SQ2_per > 0) ? SQ2_per : 8;
						if ((SQ2_vol_state == 0) && !SQ2_env_add) { SQ2_enable = false; }
					}
					calculate_bias_gain_2();
					SQ2_len_en = (value & 0x40) > 0;
					break;
				case 0xFF1A:                                        // NR30 (on/off)
					Audio_Regs[NR30] = value;
					WAVE_DAC_pow = (value & 0x80) > 0;
					if (!WAVE_DAC_pow) { WAVE_enable = false; }
					//calculate_bias_gain_w();
					break;
				case 0xFF1B:                                        // NR31 (length)
					Audio_Regs[NR31] = value;
					WAVE_length = (uint32_t)(256 - value);
					WAVE_len_cntr = WAVE_length;
					break;
				case 0xFF1C:                                        // NR32 (level output)
					Audio_Regs[NR32] = value;
					WAVE_vol_code = (uint8_t)((value & 0x60) >> 5);
					break;
				case 0xFF1D:                                        // NR33 (freq low)
					Audio_Regs[NR33] = value;
					WAVE_frq &= 0x700;
					WAVE_frq |= value;
					break;
				case 0xFF1E:                                        // NR34 (freq hi)
					Audio_Regs[NR34] = value;
					WAVE_trigger = (value & 0x80) > 0;
					WAVE_frq &= 0xFF;
					WAVE_frq |= (uint32_t)((value & 7) << 8);

					if ((sequencer_len & 1) > 0)
					{
						if (!WAVE_len_en && ((value & 0x40) > 0) && (WAVE_len_cntr > 0))
						{
							WAVE_len_cntr--;
							if ((WAVE_len_cntr == 0) && !WAVE_trigger) { WAVE_enable = false; }
						}
					}

					if (WAVE_trigger)
					{
						// some corruption occurs if triggering while reading
						if (WAVE_enable && (WAVE_intl_cntr == 2) && !is_GBC[0])
						{
							// we want to use the previous wave cntr value since it was just incremented
							int t_wave_cntr = (WAVE_wave_cntr + 1) & 31;
							if ((t_wave_cntr >> 1) < 4)
							{
								Wave_RAM[0] = Wave_RAM[t_wave_cntr >> 1];
							}
							else
							{
								Wave_RAM[0] = Wave_RAM[(t_wave_cntr >> 3) * 4];
								Wave_RAM[1] = Wave_RAM[(t_wave_cntr >> 3) * 4 + 1];
								Wave_RAM[2] = Wave_RAM[(t_wave_cntr >> 3) * 4 + 2];
								Wave_RAM[3] = Wave_RAM[(t_wave_cntr >> 3) * 4 + 3];
							}
						}

						WAVE_enable = true;

						if (WAVE_len_cntr == 0)
						{
							WAVE_len_cntr = 256;
							if (((value & 0x40) > 0) && ((sequencer_len & 1) > 0)) { WAVE_len_cntr--; }
						}
						WAVE_intl_cntr = (2048 - WAVE_frq) * 2 + 6; // trigger delay for wave channel
						WAVE_wave_cntr = 0;
						if (!WAVE_DAC_pow) { WAVE_enable = false; }
					}

					//calculate_bias_gain_w();
					WAVE_len_en = (value & 0x40) > 0;
					break;
				case 0xFF20:                                        // NR41 (length)
					Audio_Regs[NR41] = value;
					NOISE_length = (uint32_t)(64 - (value & 0x3F));
					NOISE_len_cntr = NOISE_length;
					break;
				case 0xFF21:                                        // NR42 (envelope)
					NOISE_st_vol = (uint8_t)((value & 0xF0) >> 4);
					NOISE_env_add = (value & 8) > 0;
					NOISE_per = (uint8_t)(value & 7);

					// several glitchy effects happen when writing to NRx2 during audio playing
					if (((Audio_Regs[NR42] & 7) == 0) && !NOISE_vol_done) { NOISE_vol_state++; }
					else if ((Audio_Regs[NR42] & 8) == 0) { NOISE_vol_state += 2; }

					if (((Audio_Regs[NR42] ^ value) & 8) > 0) { NOISE_vol_state = (uint8_t)(0x10 - NOISE_vol_state); }

					NOISE_vol_state &= 0xF;
					if ((value & 0xF8) == 0) { NOISE_enable = false; }
					Audio_Regs[NR42] = value;

					calculate_bias_gain_n();
					break;
				case 0xFF22:                                        // NR43 (shift)
					Audio_Regs[NR43] = value;
					NOISE_clk_shft = (uint8_t)((value & 0xF0) >> 4);
					NOISE_wdth_md = (value & 8) > 0;
					NOISE_div_code = (uint8_t)(value & 7);
					break;
				case 0xFF23:                                        // NR44 (trigger)
					Audio_Regs[NR44] = value;
					NOISE_trigger = (value & 0x80) > 0;

					if ((sequencer_len & 1) > 0)
					{
						if (!NOISE_len_en && ((value & 0x40) > 0) && (NOISE_len_cntr > 0))
						{
							NOISE_len_cntr--;
							if ((NOISE_len_cntr == 0) && !NOISE_trigger) { NOISE_enable = false; }
						}
					}

					if (NOISE_trigger)
					{
						NOISE_enable = true;
						NOISE_vol_done = false;

						if (NOISE_len_cntr == 0)
						{
							NOISE_len_cntr = 64;
							if (((value & 0x40) > 0) && ((sequencer_len & 1) > 0)) { NOISE_len_cntr--; }
						}
						NOISE_intl_cntr = (DIVISOR[NOISE_div_code] << NOISE_clk_shft);
						NOISE_vol_state = NOISE_st_vol;
						NOISE_vol_per = (NOISE_per > 0) ? NOISE_per : 8;
						NOISE_LFSR = 0x7FFF;
						if ((NOISE_vol_state == 0) && !NOISE_env_add) { NOISE_enable = false; }
					}

					calculate_bias_gain_n();
					NOISE_len_en = (value & 0x40) > 0;
					break;
				case 0xFF24:                                        // NR50 (ctrl)
					Audio_Regs[NR50] = value;
					AUD_CTRL_vin_L_en = (value & 0x80) > 0;
					AUD_CTRL_vol_L = (uint8_t)((value & 0x70) >> 4);
					AUD_CTRL_vin_R_en = (value & 8) > 0;
					AUD_CTRL_vol_R = (uint8_t)(value & 7);

					calculate_bias_gain_a();
					break;
				case 0xFF25:                                        // NR51 (ctrl)
					Audio_Regs[NR51] = value;
					AUD_CTRL_noise_L_en = (value & 0x80) > 0;
					AUD_CTRL_wave_L_en = (value & 0x40) > 0;
					AUD_CTRL_sq2_L_en = (value & 0x20) > 0;
					AUD_CTRL_sq1_L_en = (value & 0x10) > 0;
					AUD_CTRL_noise_R_en = (value & 8) > 0;
					AUD_CTRL_wave_R_en = (value & 4) > 0;
					AUD_CTRL_sq2_R_en = (value & 2) > 0;
					AUD_CTRL_sq1_R_en = (value & 1) > 0;

					calculate_bias_gain_a();
					break;
				case 0xFF26:                                        // NR52 (ctrl)						
					// NOTE: Make sure to do the power off first since it will call the write_reg function again
					if ((value & 0x80) == 0) { power_off(); }
					AUD_CTRL_power = (value & 0x80) > 0;
					break;

					// wave ram table
				case 0xFF30:
				case 0xFF31:
				case 0xFF32:
				case 0xFF33:
				case 0xFF34:
				case 0xFF35:
				case 0xFF36:
				case 0xFF37:
				case 0xFF38:
				case 0xFF39:
				case 0xFF3A:
				case 0xFF3B:
				case 0xFF3C:
				case 0xFF3D:
				case 0xFF3E:
				case 0xFF3F:
					if (WAVE_enable)
					{
						if (WAVE_can_get || is_GBC[0]) { Wave_RAM[WAVE_wave_cntr >> 1] = value; }
					}
					else
					{
						Wave_RAM[addr & 0xF] = value;
					}

					break;
				}
			}
			// when power is off, only length counters and waveRAM are effected by writes
			// ON GBC, length counters cannot be written to either
			else
			{
				switch (addr)
				{
				case 0xFF11:                                        // NR11 (sound length / wave pattern duty %)
					if (!is_GBC[0])
					{
						SQ1_length = (uint32_t)(64 - (value & 0x3F));
						SQ1_len_cntr = SQ1_length;
					}
					break;
				case 0xFF16:                                        // NR21 (sound length / wave pattern duty %)		
					if (!is_GBC[0])
					{
						SQ2_length = (uint32_t)(64 - (value & 0x3F));
						SQ2_len_cntr = SQ2_length;
					}
					break;
				case 0xFF1B:                                        // NR31 (length)
					if (!is_GBC[0])
					{
						WAVE_length = (uint32_t)(256 - value);
						WAVE_len_cntr = WAVE_length;
					}
					break;
				case 0xFF20:                                        // NR41 (length)
					if (!is_GBC[0])
					{
						NOISE_length = (uint32_t)(64 - (value & 0x3F));
						NOISE_len_cntr = NOISE_length;
					}
					break;
				case 0xFF26:                                        // NR52 (ctrl)
					AUD_CTRL_power = (value & 0x80) > 0;
					if (AUD_CTRL_power)
					{
						sequencer_vol = 0;
						sequencer_len = 0;
						sequencer_swp = 0;
					}
					break;

					// wave ram table
				case 0xFF30:
				case 0xFF31:
				case 0xFF32:
				case 0xFF33:
				case 0xFF34:
				case 0xFF35:
				case 0xFF36:
				case 0xFF37:
				case 0xFF38:
				case 0xFF39:
				case 0xFF3A:
				case 0xFF3B:
				case 0xFF3C:
				case 0xFF3D:
				case 0xFF3E:
				case 0xFF3F:
					Wave_RAM[addr & 0x0F] = value;
					break;
				}
			}
		}

		void tick()
		{
			// calculate square1's output
			if (SQ1_enable)
			{
				SQ1_intl_cntr--;
				if (SQ1_intl_cntr == 0)
				{
					SQ1_intl_cntr = (2048 - SQ1_frq) * 4;
					SQ1_duty_cntr++;
					SQ1_duty_cntr &= 7;

					SQ1_output = DUTY_CYCLES[SQ1_duty * 8 + SQ1_duty_cntr] ? SQ1_vol_state : SQ1_bias_gain;

					// avoid aliasing at high frequenices
					//if (SQ1_frq > 0x7F0) { SQ1_output = 0; }
				}
			}

			// calculate square2's output
			if (SQ2_enable)
			{
				SQ2_intl_cntr--;
				if (SQ2_intl_cntr == 0)
				{
					SQ2_intl_cntr = (2048 - SQ2_frq) * 4;
					SQ2_duty_cntr++;
					SQ2_duty_cntr &= 7;

					SQ2_output = DUTY_CYCLES[SQ2_duty * 8 + SQ2_duty_cntr] ? SQ2_vol_state : SQ2_bias_gain;

					// avoid aliasing at high frequenices
					//if (SQ2_frq > 0x7F0) { SQ2_output = 0; }
				}
			}

			// calculate wave output
			WAVE_can_get = false;
			if (WAVE_enable)
			{
				WAVE_intl_cntr--;

				if (WAVE_intl_cntr == 0)
				{
					WAVE_can_get = true;

					WAVE_intl_cntr = (2048 - WAVE_frq) * 2;

					if ((WAVE_wave_cntr & 1) == 0)
					{
						sample = (uint8_t)(sample >> 4);
					}

					if (WAVE_vol_code == 0)
					{
						sample = (uint8_t)((sample & 0xF) >> 4);
					}
					else if (WAVE_vol_code == 1)
					{
						sample = (uint8_t)(sample & 0xF);
					}
					else if (WAVE_vol_code == 2)
					{
						sample = (uint8_t)((sample & 0xF) >> 1);
					}
					else
					{
						sample = (uint8_t)((sample & 0xF) >> 2);
					}

					WAVE_output = sample;

					// NOTE: The sample buffer is only reloaded after the current sample is played, even if just triggered
					WAVE_wave_cntr++;
					WAVE_wave_cntr &= 0x1F;
					sample = Wave_RAM[WAVE_wave_cntr >> 1];
				}
			}

			// calculate noise output
			if (NOISE_enable)
			{
				NOISE_intl_cntr--;
				if (NOISE_intl_cntr == 0)
				{
					NOISE_intl_cntr = (DIVISOR[NOISE_div_code] << NOISE_clk_shft);
					int bit_lfsr = (NOISE_LFSR & 1) ^ ((NOISE_LFSR & 2) >> 1);

					NOISE_LFSR = (NOISE_LFSR >> 1) & 0x3FFF;
					NOISE_LFSR |= (bit_lfsr << 14);

					if (NOISE_wdth_md)
					{
						NOISE_LFSR = NOISE_LFSR & 0x7FBF;
						NOISE_LFSR |= (bit_lfsr << 6);
					}

					NOISE_output = (NOISE_LFSR & 1) > 0 ? NOISE_bias_gain : NOISE_vol_state;
				}
			}

			// add up components to each channel
			int L_final = 0;
			int R_final = 0;

			if (AUD_CTRL_sq1_L_en) { L_final += SQ1_output; }
			if (AUD_CTRL_sq2_L_en) { L_final += SQ2_output; }
			if (AUD_CTRL_wave_L_en) { L_final += WAVE_output; }
			if (AUD_CTRL_noise_L_en) { L_final += NOISE_output; }

			if (AUD_CTRL_sq1_R_en) { R_final += SQ1_output; }
			if (AUD_CTRL_sq2_R_en) { R_final += SQ2_output; }
			if (AUD_CTRL_wave_R_en) { R_final += WAVE_output; }
			if (AUD_CTRL_noise_R_en) { R_final += NOISE_output; }

			L_final *= (AUD_CTRL_vol_L + 1) * 40;
			R_final *= (AUD_CTRL_vol_R + 1) * 40;

			if (L_final != latched_sample_L)
			{
				samples_L[num_samples_L * 2] = master_audio_clock;
				samples_L[num_samples_L * 2 + 1] = L_final - latched_sample_L;

				num_samples_L++;
				
				latched_sample_L = L_final;
			}

			if (R_final != latched_sample_R)
			{
				samples_R[num_samples_R * 2] = master_audio_clock;
				samples_R[num_samples_R * 2 + 1] = R_final - latched_sample_R;

				num_samples_R++;
				
				latched_sample_R = R_final;
			}

			master_audio_clock++;

			// frame sequencer ticks at a rate of 512 hz (or every time a 13 bit counter rolls over)
			// the sequencer is actually the timer DIV register
			// so if it's constantly written to, these values won't update
			bool check = double_speed[0] ? ((timer_div_reg[0] & 0x2000) > 0) : ((timer_div_reg[0] & 0x1000) > 0);

			if (check && !timer_bit_old)
			{
				sequencer_vol++; sequencer_vol &= 0x7;
				sequencer_len++; sequencer_len &= 0x7;
				sequencer_swp++; sequencer_swp &= 0x7;

				// clock the lengths
				if ((sequencer_len & 1) > 0)
				{
					if (SQ1_len_en && SQ1_len_cntr > 0)
					{
						SQ1_len_cntr--;
						if (SQ1_len_cntr == 0) { SQ1_enable = SQ1_swp_enable = false; calculate_bias_gain_1(); }
					}
					if (SQ2_len_en && SQ2_len_cntr > 0)
					{
						SQ2_len_cntr--;
						if (SQ2_len_cntr == 0) { SQ2_enable = false; calculate_bias_gain_2(); }
					}
					if (WAVE_len_en && WAVE_len_cntr > 0)
					{
						WAVE_len_cntr--;
						if (WAVE_len_cntr == 0) { WAVE_enable = false; calculate_bias_gain_w(); }
					}
					if (NOISE_len_en && NOISE_len_cntr > 0)
					{
						NOISE_len_cntr--;
						if (NOISE_len_cntr == 0) { NOISE_enable = false; calculate_bias_gain_n(); }
					}
				}

				// clock the sweep
				if ((sequencer_swp == 3) || (sequencer_swp == 7))
				{
					SQ1_intl_swp_cnt--;
					if ((SQ1_intl_swp_cnt == 0) && SQ1_swp_enable)
					{
						SQ1_intl_swp_cnt = SQ1_swp_prd > 0 ? SQ1_swp_prd : 8;

						if ((SQ1_swp_prd > 0))
						{
							int shadow_frq = SQ1_frq_shadow;
							shadow_frq = shadow_frq >> SQ1_shift;
							if (SQ1_negate) { shadow_frq = -shadow_frq; }
							shadow_frq += SQ1_frq_shadow;

							// set negate mode flag that disables channel is negate clerar
							if (SQ1_negate) { SQ1_calc_done = true; }

							// disable channel if overflow
							if ((uint32_t)shadow_frq > 2047)
							{
								SQ1_enable = SQ1_swp_enable = false; calculate_bias_gain_1();
							}
							else
							{
								if (SQ1_shift > 0)
								{
									shadow_frq &= 0x7FF;
									SQ1_frq = shadow_frq;
									SQ1_frq_shadow = shadow_frq;

									// note that we also write back the frequency to the actual register
									Audio_Regs[NR13] = (uint8_t)(SQ1_frq & 0xFF);
									Audio_Regs[NR14] &= 0xF8;
									Audio_Regs[NR14] |= (uint8_t)((SQ1_frq >> 8) & 7);

									// after writing, we repeat the process and do another overflow check
									shadow_frq = SQ1_frq_shadow;
									shadow_frq = shadow_frq >> SQ1_shift;
									if (SQ1_negate) { shadow_frq = -shadow_frq; }
									shadow_frq += SQ1_frq_shadow;

									if ((uint32_t)shadow_frq > 2047)
									{
										SQ1_enable = SQ1_swp_enable = false; calculate_bias_gain_1();
									}
								}
							}
						}
					}
				}

				// clock the volume envelope
				if (sequencer_vol == 0)
				{
					if (SQ1_per > 0)
					{
						SQ1_vol_per--;
						if (SQ1_vol_per == 0)
						{
							SQ1_vol_per = (SQ1_per > 0) ? SQ1_per : 8;
							if (!SQ1_vol_done)
							{
								if (SQ1_env_add)
								{
									if (SQ1_vol_state < 15) { SQ1_vol_state++; }
									else { SQ1_vol_done = true; }
								}
								else
								{
									if (SQ1_vol_state >= 1) { SQ1_vol_state--; }
									else { SQ1_vol_done = true; }
								}
							}
						}
					}

					if (SQ2_per > 0)
					{
						SQ2_vol_per--;
						if (SQ2_vol_per == 0)
						{
							SQ2_vol_per = (SQ2_per > 0) ? SQ2_per : 8;
							if (!SQ2_vol_done)
							{
								if (SQ2_env_add)
								{
									if (SQ2_vol_state < 15) { SQ2_vol_state++; }
									else { SQ2_vol_done = true; }
								}
								else
								{
									if (SQ2_vol_state >= 1) { SQ2_vol_state--; }
									else { SQ2_vol_done = true; }
								}
							}
						}
					}

					if (NOISE_per > 0)
					{
						NOISE_vol_per--;
						if (NOISE_vol_per == 0)
						{
							NOISE_vol_per = (NOISE_per > 0) ? NOISE_per : 8;
							if (!NOISE_vol_done)
							{
								if (NOISE_env_add)
								{
									if (NOISE_vol_state < 15) { NOISE_vol_state++; }
									else { NOISE_vol_done = true; }
								}
								else
								{
									if (NOISE_vol_state >= 1) { NOISE_vol_state--; }
									else { NOISE_vol_done = true; }
								}
							}
						}
					}
				}
			}
			timer_bit_old = double_speed[0] ? ((timer_div_reg[0] & 0x2000) > 0) : ((timer_div_reg[0] & 0x1000) > 0);
		}

		void power_off()
		{
			for (int i = 0; i < 0x16; i++)
			{
				WriteReg(0xFF10 + i, 0);
			}

			calculate_bias_gain_a();

			// duty and length are reset
			SQ1_duty_cntr = SQ2_duty_cntr = 0;

			// reset state variables
			SQ1_enable = SQ1_swp_enable = SQ2_enable = WAVE_enable = NOISE_enable = false;

			SQ1_len_en = SQ2_len_en = WAVE_len_en = NOISE_len_en = false;

			SQ1_output = SQ2_output = WAVE_output = NOISE_output = 0;

			// on GBC, lengths are also reset
			if (is_GBC[0])
			{
				SQ1_length = SQ2_length = WAVE_length = NOISE_length = 0;
				SQ1_len_cntr = SQ2_len_cntr = WAVE_len_cntr = NOISE_len_cntr = 0;
			}

			sequencer_len = 0;
			sequencer_vol = 0;
			sequencer_swp = 0;
		}

		void Reset()
		{
			if (is_GBC[0])
			{
				Wave_RAM[0] = 0; Wave_RAM[2] = 0; Wave_RAM[4] = 0; Wave_RAM[6] = 0;
				Wave_RAM[8] = 0; Wave_RAM[10] = 0; Wave_RAM[12] = 0; Wave_RAM[14] = 0;

				Wave_RAM[1] = 0xFF; Wave_RAM[3] = 0xFF; Wave_RAM[5] = 0xFF; Wave_RAM[7] = 0xFF;
				Wave_RAM[9] = 0xFF; Wave_RAM[11] = 0xFF; Wave_RAM[13] = 0xFF; Wave_RAM[15] = 0xFF;
			}
			else
			{
				Wave_RAM[0] = 0x84; Wave_RAM[1] = 0x40; Wave_RAM[2] = 0x43; Wave_RAM[3] = 0xAA;
				Wave_RAM[4] = 0x2D; Wave_RAM[5] = 0x78; Wave_RAM[6] = 0x92; Wave_RAM[7] = 0x3C;

				Wave_RAM[8] = 0x60; Wave_RAM[9] = 0x59; Wave_RAM[10] = 0x59; Wave_RAM[11] = 0xB0;
				Wave_RAM[12] = 0x34; Wave_RAM[13] = 0xB8; Wave_RAM[14] = 0x2E; Wave_RAM[15] = 0xDA;
			}

			for (int i = 0; i < 21; i++) 
			{
				Audio_Regs[i] = 0;
			}

			for (int i = 0; i < 0x16; i++)
			{
				WriteReg(0xFF10 + i, 0);
			}

			calculate_bias_gain_a();

			SQ1_duty_cntr = SQ2_duty_cntr = 0;

			SQ1_enable = SQ1_swp_enable = SQ2_enable = WAVE_enable = NOISE_enable = false;

			SQ1_len_en = SQ2_len_en = WAVE_len_en = NOISE_len_en = false;

			SQ1_output = SQ2_output = WAVE_output = NOISE_output = 0;

			SQ1_length = SQ2_length = WAVE_length = NOISE_length = 0;
			SQ1_len_cntr = SQ2_len_cntr = WAVE_len_cntr = NOISE_len_cntr = 0;

			master_audio_clock = 0;

			sequencer_len = 0;
			sequencer_swp = 0;
			sequencer_vol = 0;

			sample = 0;
		}

		void calculate_bias_gain_a()
		{
			if ((AUD_CTRL_sq1_R_en | AUD_CTRL_sq1_L_en) && ((Audio_Regs[NR12] & 0xF8) > 0))
			{
				SQ1_bias_gain = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 2;
			}
			else
			{
				SQ1_bias_gain = 0;
			}
			if ((AUD_CTRL_sq2_R_en | AUD_CTRL_sq2_L_en) && ((Audio_Regs[NR22] & 0xF8) > 0))
			{
				SQ2_bias_gain = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 2;
			}
			else
			{
				SQ2_bias_gain = 0;
			}
			if ((AUD_CTRL_wave_R_en | AUD_CTRL_wave_L_en) && WAVE_DAC_pow)
			{
				WAVE_bias_gain = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 2;
			}
			else
			{
				WAVE_bias_gain = 0;
			}
			if ((AUD_CTRL_noise_R_en | AUD_CTRL_noise_L_en) && ((Audio_Regs[NR42] & 0xF8) > 0))
			{
				NOISE_bias_gain = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 2;
			}
			else
			{
				NOISE_bias_gain = 0;
			}

			if (!SQ1_enable) { SQ1_output = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 1; }
			if (!SQ2_enable) { SQ2_output = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 1; }
			//if (!WAVE_enable) { WAVE_output = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 1; }
			if (!NOISE_enable) { NOISE_output = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 2; }
		}

		void calculate_bias_gain_1()
		{
			if ((AUD_CTRL_sq1_R_en | AUD_CTRL_sq1_L_en) && ((Audio_Regs[NR12] & 0xF8) > 0))
			{
				SQ1_bias_gain = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 3;
			}
			else
			{
				SQ1_bias_gain = 0;
			}

			if (!SQ1_enable) { SQ1_output = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 1; }
		}

		void calculate_bias_gain_2()
		{
			if ((AUD_CTRL_sq2_R_en | AUD_CTRL_sq2_L_en) && ((Audio_Regs[NR22] & 0xF8) > 0))
			{
				SQ2_bias_gain = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 3;
			}
			else
			{
				SQ2_bias_gain = 0;
			}

			if (!SQ2_enable) { SQ2_output = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 1; }
		}

		void calculate_bias_gain_w()
		{
			if ((AUD_CTRL_wave_R_en | AUD_CTRL_wave_L_en) && WAVE_DAC_pow)
			{
				WAVE_bias_gain = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 3;
			}
			else
			{
				WAVE_bias_gain = 0;
			}

			if (!WAVE_enable) { WAVE_output = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 1; }
		}

		void calculate_bias_gain_n()
		{
			if ((AUD_CTRL_noise_R_en | AUD_CTRL_noise_L_en) && ((Audio_Regs[NR42] & 0xF8) > 0))
			{
				NOISE_bias_gain = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 3;
			}
			else
			{
				NOISE_bias_gain = 0;
			}

			if (!NOISE_enable) { NOISE_output = (AUD_CTRL_vol_R | AUD_CTRL_vol_L) >> 2; }
		}

		uint8_t Read_NR52()
		{
			return (uint8_t)(
				((AUD_CTRL_power ? 1 : 0) << 7) |
				(SQ1_enable ? 1 : 0) |
				((SQ2_enable ? 1 : 0) << 1) |
				((WAVE_enable ? 1 : 0) << 2) |
				((NOISE_enable ? 1 : 0) << 3));
		}

		#pragma endregion


		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			for (int i = 0; i < 21; i++) 
			{
				saver = byte_saver(Audio_Regs[i], saver);
			}

			for (int i = 0; i < 16; i++)
			{
				saver = byte_saver(Wave_RAM[i], saver);
			}

			saver = bool_saver(SQ1_vol_done, saver);
			saver = bool_saver(SQ1_calc_done, saver);
			saver = bool_saver(SQ1_swp_enable, saver);
			saver = bool_saver(SQ1_enable, saver);
			saver = byte_saver(SQ1_vol_state, saver);
			saver = byte_saver(SQ1_duty_cntr, saver);
			saver = int_saver(SQ1_frq_shadow, saver);
			saver = int_saver(SQ1_intl_cntr, saver);
			saver = int_saver(SQ1_vol_per, saver);
			saver = int_saver(SQ1_intl_swp_cnt, saver);
			saver = int_saver(SQ1_len_cntr, saver);
			saver = bool_saver(SQ1_negate, saver);
			saver = bool_saver(SQ1_trigger, saver);
			saver = bool_saver(SQ1_len_en, saver);
			saver = bool_saver(SQ1_env_add, saver);
			saver = byte_saver(SQ1_shift, saver);
			saver = byte_saver(SQ1_duty, saver);
			saver = byte_saver(SQ1_st_vol, saver);
			saver = byte_saver(SQ1_per, saver);
			saver = byte_saver(SQ1_swp_prd, saver);
			saver = int_saver(SQ1_frq, saver);
			saver = int_saver(SQ1_length, saver);
			saver = int_saver(SQ1_output, saver);

			saver = bool_saver(SQ2_vol_done, saver);
			saver = bool_saver(SQ2_enable, saver);
			saver = byte_saver(SQ2_vol_state, saver);
			saver = byte_saver(SQ2_duty_cntr, saver);
			saver = int_saver(SQ2_intl_cntr, saver);
			saver = int_saver(SQ2_vol_per, saver);
			saver = int_saver(SQ2_len_cntr, saver);
			saver = bool_saver(SQ2_trigger, saver);
			saver = bool_saver(SQ2_len_en, saver);
			saver = bool_saver(SQ2_env_add, saver);
			saver = byte_saver(SQ2_duty, saver);
			saver = byte_saver(SQ2_st_vol, saver);
			saver = byte_saver(SQ2_per, saver);
			saver = int_saver(SQ2_frq, saver);
			saver = int_saver(SQ2_length, saver);
			saver = int_saver(SQ2_output, saver);

			saver = bool_saver(WAVE_can_get, saver);
			saver = bool_saver(WAVE_enable, saver);
			saver = byte_saver(WAVE_wave_cntr, saver);
			saver = int_saver(WAVE_intl_cntr, saver);
			saver = int_saver(WAVE_len_cntr, saver);
			saver = bool_saver(WAVE_DAC_pow, saver);
			saver = bool_saver(WAVE_trigger, saver);
			saver = bool_saver(WAVE_len_en, saver);
			saver = byte_saver(WAVE_vol_code, saver);
			saver = int_saver(WAVE_frq, saver);
			saver = int_saver(WAVE_length, saver);
			saver = int_saver(WAVE_output, saver);

			saver = bool_saver(NOISE_vol_done, saver);
			saver = bool_saver(NOISE_enable, saver);
			saver = byte_saver(NOISE_vol_state, saver);
			saver = int_saver(NOISE_intl_cntr, saver);
			saver = int_saver(NOISE_vol_per, saver);
			saver = int_saver(NOISE_LFSR, saver);
			saver = int_saver(NOISE_len_cntr, saver);
			saver = bool_saver(NOISE_wdth_md, saver);
			saver = bool_saver(NOISE_trigger, saver);
			saver = bool_saver(NOISE_len_en, saver);
			saver = bool_saver(NOISE_env_add, saver);
			saver = byte_saver(NOISE_clk_shft, saver);
			saver = byte_saver(NOISE_div_code, saver);
			saver = byte_saver(NOISE_st_vol, saver);
			saver = byte_saver(NOISE_per, saver);
			saver = int_saver(NOISE_length, saver);
			saver = int_saver(NOISE_output, saver);

			saver = int_saver(sequencer_len, saver);
			saver = int_saver(sequencer_vol, saver);
			saver = int_saver(sequencer_swp, saver);
			saver = bool_saver(timer_bit_old, saver);

			saver = int_saver(master_audio_clock, saver);

			saver = byte_saver(sample, saver);
			saver = int_saver(latched_sample_L, saver);
			saver = int_saver(latched_sample_R, saver);
			saver = int_saver(num_samples_L, saver);
			saver = int_saver(num_samples_R, saver);

			saver = bool_saver(AUD_CTRL_vin_L_en, saver);
			saver = bool_saver(AUD_CTRL_vin_R_en, saver);
			saver = bool_saver(AUD_CTRL_sq1_L_en, saver);
			saver = bool_saver(AUD_CTRL_sq2_L_en, saver);
			saver = bool_saver(AUD_CTRL_wave_L_en, saver);
			saver = bool_saver(AUD_CTRL_noise_L_en, saver);
			saver = bool_saver(AUD_CTRL_sq1_R_en, saver);
			saver = bool_saver(AUD_CTRL_sq2_R_en, saver);
			saver = bool_saver(AUD_CTRL_wave_R_en, saver);
			saver = bool_saver(AUD_CTRL_noise_R_en, saver);
			saver = bool_saver(AUD_CTRL_power, saver);
			saver = byte_saver(AUD_CTRL_vol_L, saver);
			saver = byte_saver(AUD_CTRL_vol_R, saver);

			saver = int_saver(SQ1_bias_gain, saver);
			saver = int_saver(SQ2_bias_gain, saver);
			saver = int_saver(WAVE_bias_gain, saver);
			saver = int_saver(NOISE_bias_gain, saver);
			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			for (int i = 0; i < 21; i++)
			{
				loader = byte_loader(&Audio_Regs[i], loader);
			}

			for (int i = 0; i < 16; i++)
			{
				loader = byte_loader(&Wave_RAM[i], loader);
			}

			loader = bool_loader(&SQ1_vol_done, loader);
			loader = bool_loader(&SQ1_calc_done, loader);
			loader = bool_loader(&SQ1_swp_enable, loader);
			loader = bool_loader(&SQ1_enable, loader);
			loader = byte_loader(&SQ1_vol_state, loader);
			loader = byte_loader(&SQ1_duty_cntr, loader);
			loader = int_loader(&SQ1_frq_shadow, loader);
			loader = int_loader(&SQ1_intl_cntr, loader);
			loader = int_loader(&SQ1_vol_per, loader);
			loader = int_loader(&SQ1_intl_swp_cnt, loader);
			loader = int_loader(&SQ1_len_cntr, loader);
			loader = bool_loader(&SQ1_negate, loader);
			loader = bool_loader(&SQ1_trigger, loader);
			loader = bool_loader(&SQ1_len_en, loader);
			loader = bool_loader(&SQ1_env_add, loader);
			loader = byte_loader(&SQ1_shift, loader);
			loader = byte_loader(&SQ1_duty, loader);
			loader = byte_loader(&SQ1_st_vol, loader);
			loader = byte_loader(&SQ1_per, loader);
			loader = byte_loader(&SQ1_swp_prd, loader);
			loader = int_loader(&SQ1_frq, loader);
			loader = int_loader(&SQ1_length, loader);
			loader = int_loader(&SQ1_output, loader);

			loader = bool_loader(&SQ2_vol_done, loader);
			loader = bool_loader(&SQ2_enable, loader);
			loader = byte_loader(&SQ2_vol_state, loader);
			loader = byte_loader(&SQ2_duty_cntr, loader);
			loader = int_loader(&SQ2_intl_cntr, loader);
			loader = int_loader(&SQ2_vol_per, loader);
			loader = int_loader(&SQ2_len_cntr, loader);
			loader = bool_loader(&SQ2_trigger, loader);
			loader = bool_loader(&SQ2_len_en, loader);
			loader = bool_loader(&SQ2_env_add, loader);
			loader = byte_loader(&SQ2_duty, loader);
			loader = byte_loader(&SQ2_st_vol, loader);
			loader = byte_loader(&SQ2_per, loader);
			loader = int_loader(&SQ2_frq, loader);
			loader = int_loader(&SQ2_length, loader);
			loader = int_loader(&SQ2_output, loader);

			loader = bool_loader(&WAVE_can_get, loader);
			loader = bool_loader(&WAVE_enable, loader);
			loader = byte_loader(&WAVE_wave_cntr, loader);
			loader = int_loader(&WAVE_intl_cntr, loader);
			loader = int_loader(&WAVE_len_cntr, loader);
			loader = bool_loader(&WAVE_DAC_pow, loader);
			loader = bool_loader(&WAVE_trigger, loader);
			loader = bool_loader(&WAVE_len_en, loader);
			loader = byte_loader(&WAVE_vol_code, loader);
			loader = int_loader(&WAVE_frq, loader);
			loader = int_loader(&WAVE_length, loader);
			loader = int_loader(&WAVE_output, loader);

			loader = bool_loader(&NOISE_vol_done, loader);
			loader = bool_loader(&NOISE_enable, loader);
			loader = byte_loader(&NOISE_vol_state, loader);
			loader = int_loader(&NOISE_intl_cntr, loader);
			loader = int_loader(&NOISE_vol_per, loader);
			loader = int_loader(&NOISE_LFSR, loader);
			loader = int_loader(&NOISE_len_cntr, loader);
			loader = bool_loader(&NOISE_wdth_md, loader);
			loader = bool_loader(&NOISE_trigger, loader);
			loader = bool_loader(&NOISE_len_en, loader);
			loader = bool_loader(&NOISE_env_add, loader);
			loader = byte_loader(&NOISE_clk_shft, loader);
			loader = byte_loader(&NOISE_div_code, loader);
			loader = byte_loader(&NOISE_st_vol, loader);
			loader = byte_loader(&NOISE_per, loader);
			loader = int_loader(&NOISE_length, loader);
			loader = int_loader(&NOISE_output, loader);

			loader = int_loader(&sequencer_len, loader);
			loader = int_loader(&sequencer_vol, loader);
			loader = int_loader(&sequencer_swp, loader);
			loader = bool_loader(&timer_bit_old, loader);

			loader = int_loader(&master_audio_clock, loader);

			loader = byte_loader(&sample, loader);
			loader = int_loader(&latched_sample_L, loader);
			loader = int_loader(&latched_sample_R, loader);
			loader = int_loader(&num_samples_L, loader);
			loader = int_loader(&num_samples_R, loader);

			loader = bool_loader(&AUD_CTRL_vin_L_en, loader);
			loader = bool_loader(&AUD_CTRL_vin_R_en, loader);
			loader = bool_loader(&AUD_CTRL_sq1_L_en, loader);
			loader = bool_loader(&AUD_CTRL_sq2_L_en, loader);
			loader = bool_loader(&AUD_CTRL_wave_L_en, loader);
			loader = bool_loader(&AUD_CTRL_noise_L_en, loader);
			loader = bool_loader(&AUD_CTRL_sq1_R_en, loader);
			loader = bool_loader(&AUD_CTRL_sq2_R_en, loader);
			loader = bool_loader(&AUD_CTRL_wave_R_en, loader);
			loader = bool_loader(&AUD_CTRL_noise_R_en, loader);
			loader = bool_loader(&AUD_CTRL_power, loader);
			loader = byte_loader(&AUD_CTRL_vol_L, loader);
			loader = byte_loader(&AUD_CTRL_vol_R, loader);

			loader = int_loader(&SQ1_bias_gain, loader);
			loader = int_loader(&SQ2_bias_gain, loader);
			loader = int_loader(&WAVE_bias_gain, loader);
			loader = int_loader(&NOISE_bias_gain, loader);
			return loader;
		}

		uint8_t* bool_saver(bool to_save, uint8_t* saver)
		{
			*saver = (uint8_t)(to_save ? 1 : 0); saver++;

			return saver;
		}

		uint8_t* byte_saver(uint8_t to_save, uint8_t* saver)
		{
			*saver = to_save; saver++;

			return saver;
		}

		uint8_t* int_saver(uint32_t to_save, uint8_t* saver)
		{
			*saver = (uint8_t)(to_save & 0xFF); saver++; *saver = (uint8_t)((to_save >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((to_save >> 16) & 0xFF); saver++; *saver = (uint8_t)((to_save >> 24) & 0xFF); saver++;

			return saver;
		}

		uint8_t* bool_loader(bool* to_load, uint8_t* loader)
		{
			to_load[0] = *to_load == 1; loader++;

			return loader;
		}

		uint8_t* byte_loader(uint8_t* to_load, uint8_t* loader)
		{
			to_load[0] = *loader; loader++;

			return loader;
		}

		uint8_t* int_loader(uint32_t* to_load, uint8_t* loader)
		{
			to_load[0] = *loader; loader++; to_load[0] |= (*loader << 8); loader++;
			to_load[0] |= (*loader << 16); loader++; to_load[0] |= (*loader << 24); loader++;

			return loader;
		}

		#pragma endregion
	};
}