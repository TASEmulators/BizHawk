using System;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// Audio Emulation
	public class Audio : ISoundProvider
	{
		public GBHawk Core { get; set; }

		public static int[] DUTY_CYCLES = new int[] {0, 0, 0, 0, 0, 0, 0, 1,
													 1, 0, 0, 0, 0, 0, 0, 1,
													 1, 0, 0, 0, 0, 1, 1, 1,
													 0, 1, 1, 1, 1, 1, 1, 0};

		public const int NR10 = 0;
		public const int NR11 = 1;
		public const int NR12 = 2;
		public const int NR13 = 3;
		public const int NR14 = 4;
		public const int NR21 = 5;
		public const int NR22 = 6;
		public const int NR23 = 7;
		public const int NR24 = 8;
		public const int NR30 = 9;
		public const int NR31 = 10;
		public const int NR32 = 11;
		public const int NR33 = 12;
		public const int NR34 = 13;
		public const int NR41 = 14;
		public const int NR42 = 15;
		public const int NR43 = 16;
		public const int NR44 = 17;
		public const int NR50 = 18;
		public const int NR51 = 19;
		public const int NR52 = 20;

		public static int[] unused_bits = new int[] { 0x80, 0x3F, 0x00, 0xFF, 0xBF,
															0x3F, 0x00, 0xFF, 0xBF,
													  0x7F, 0xFF, 0x9F, 0xFF, 0xBF,
															0xFF, 0x00, 0x00, 0xBF,
													  0x00, 0x00, 0x70};

		public byte[] Audio_Regs = new byte[21];

		public byte[] Wave_RAM = new byte [16];

		struct AUD_Object
		{
			// channel controls
			public byte swp_period;
			public bool negate;
			public byte shift;
			public byte duty;
			public byte length;
			public byte st_vol;
			public bool env_add;
			public byte per;
			public int frq;
			public bool trigger;
			public bool len_en;
			public bool DAC_pow;
			public byte vol_code;
			public byte clk_shft;
			public bool wdth_md;
			public byte div_code;

			// channel states
			public byte length_counter;
			public byte volume_state;
			public int frq_shadow;
			public int internal_cntr;
			public byte duty_counter;
			public bool enable;

			// channel non-states
			public int output;
		}

		struct CTRL_Object
		{
			public bool vin_L_en;
			public bool vin_R_en;
			public byte vol_L;
			public byte vol_R;
			public bool sq1_L_en;
			public bool sq2_L_en;
			public bool wave_L_en;
			public bool noise_L_en;
			public bool sq1_R_en;
			public bool sq2_R_en;
			public bool wave_R_en;
			public bool noise_R_en;
			public bool power;
		}

		AUD_Object SQ1, SQ2, WAVE, NOISE;

		CTRL_Object AUD_CTRL;

		public int sequencer_len, sequencer_vol, sequencer_swp, sequencer_tick;

		public byte ReadReg(int addr)
		{
			byte ret = 0;

			switch (addr)
			{		
				case 0xFF10: ret = (byte)(Audio_Regs[NR10] | unused_bits[NR10]);		break; // NR10 (sweep)
				case 0xFF11: ret = (byte)(Audio_Regs[NR11] | unused_bits[NR11]);		break; // NR11 (sound length / wave pattern duty %)
				case 0xFF12: ret = (byte)(Audio_Regs[NR12] | unused_bits[NR12]);		break; // NR12 (envelope)
				case 0xFF13: ret = (byte)(Audio_Regs[NR13] | unused_bits[NR13]);		break; // NR13 (freq low)
				case 0xFF14: ret = (byte)(Audio_Regs[NR14] | unused_bits[NR14]);		break; // NR14 (freq hi)
				case 0xFF16: ret = (byte)(Audio_Regs[NR21] | unused_bits[NR21]);		break; // NR21 (sound length / wave pattern duty %)
				case 0xFF17: ret = (byte)(Audio_Regs[NR22] | unused_bits[NR22]);		break; // NR22 (envelope)
				case 0xFF18: ret = (byte)(Audio_Regs[NR23] | unused_bits[NR23]);		break; // NR23 (freq low)
				case 0xFF19: ret = (byte)(Audio_Regs[NR24] | unused_bits[NR24]);		break; // NR24 (freq hi)
				case 0xFF1A: ret = (byte)(Audio_Regs[NR30] | unused_bits[NR30]);		break; // NR30 (on/off)
				case 0xFF1B: ret = (byte)(Audio_Regs[NR31] | unused_bits[NR31]);		break; // NR31 (length)
				case 0xFF1C: ret = (byte)(Audio_Regs[NR32] | unused_bits[NR32]);		break; // NR32 (level output)
				case 0xFF1D: ret = (byte)(Audio_Regs[NR33] | unused_bits[NR33]);		break; // NR33 (freq low)
				case 0xFF1E: ret = (byte)(Audio_Regs[NR34] | unused_bits[NR34]);		break; // NR34 (freq hi)
				case 0xFF20: ret = (byte)(Audio_Regs[NR41] | unused_bits[NR41]);		break; // NR41 (length)
				case 0xFF21: ret = (byte)(Audio_Regs[NR42] | unused_bits[NR42]);		break; // NR42 (envelope)
				case 0xFF22: ret = (byte)(Audio_Regs[NR43] | unused_bits[NR43]);		break; // NR43 (shift)
				case 0xFF23: ret = (byte)(Audio_Regs[NR44] | unused_bits[NR44]);		break; // NR44 (trigger)
				case 0xFF24: ret = (byte)(Audio_Regs[NR50] | unused_bits[NR50]);		break; // NR50 (ctrl)
				case 0xFF25: ret = (byte)(Audio_Regs[NR51] | unused_bits[NR51]);		break; // NR51 (ctrl)
				case 0xFF26: ret = (byte)(Audio_Regs[NR52] | unused_bits[NR52]);		break; // NR52 (ctrl)

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
					ret = Wave_RAM[addr & 0x0F];
					break;

			}

			return ret;
		}

		public void WriteReg(int addr, byte value)
		{
			// while power is on, everything is writable
			if (AUD_CTRL.power)
			{
				switch (addr)
				{
					case 0xFF10:                                        // NR10 (sweep)
						Audio_Regs[NR10] = value;
						SQ1.swp_period = (byte)((value & 0x70) >> 4);
						SQ1.negate = (value & 8) > 0;
						SQ1.shift = (byte)(value & 7);
						break;
					case 0xFF11:                                        // NR11 (sound length / wave pattern duty %)
						Audio_Regs[NR11] = value;
						SQ1.duty = (byte)((value & 0xC0) >> 6);
						SQ1.length = (byte)(64 - value & 0x3F);
						break;
					case 0xFF12:                                        // NR12 (envelope)
						Audio_Regs[NR12] = value;
						SQ1.st_vol = (byte)((value & 0xF0) >> 4);
						SQ1.env_add = (value & 8) > 0;
						SQ1.per = (byte)(value & 7);
						break;
					case 0xFF13:                                        // NR13 (freq low)
						Audio_Regs[NR13] = value;
						SQ1.frq &= 0x700;
						SQ1.frq |= value;
						break;
					case 0xFF14:                                        // NR14 (freq hi)
						Audio_Regs[NR14] = value;
						SQ1.trigger = (value & 0x80) > 0;
						SQ1.len_en = (value & 0x40) > 0;
						SQ1.frq &= 0xFF;
						SQ1.frq |= (ushort)((value & 7) << 8);
						break;
					case 0xFF16:                                        // NR21 (sound length / wave pattern duty %)		
						Audio_Regs[NR21] = value;
						SQ2.duty = (byte)((value & 0xC0) >> 6);
						SQ2.length = (byte)(64 - value & 0x3F);
						break;
					case 0xFF17:                                        // NR22 (envelope)
						Audio_Regs[NR22] = value;
						SQ2.st_vol = (byte)((value & 0xF0) >> 4);
						SQ2.env_add = (value & 8) > 0;
						SQ2.per = (byte)(value & 7);
						break;
					case 0xFF18:                                        // NR23 (freq low)
						Audio_Regs[NR23] = value;
						SQ2.frq &= 0x700;
						SQ2.frq |= value;
						break;
					case 0xFF19:                                        // NR24 (freq hi)
						Audio_Regs[NR24] = value;
						SQ2.trigger = (value & 0x80) > 0;
						SQ2.len_en = (value & 0x40) > 0;
						SQ2.frq &= 0xFF;
						SQ2.frq |= (ushort)((value & 7) << 8);
						break;
					case 0xFF1A:                                        // NR30 (on/off)
						Audio_Regs[NR30] = value;
						WAVE.DAC_pow = (value & 0x80) > 0;
						break;
					case 0xFF1B:                                        // NR31 (length)
						Audio_Regs[NR31] = value;
						WAVE.length = (byte)(256 - value);
						break;
					case 0xFF1C:                                        // NR32 (level output)
						Audio_Regs[NR32] = value;
						WAVE.vol_code = (byte)((value & 0x60) >> 5);
						break;
					case 0xFF1D:                                        // NR33 (freq low)
						Audio_Regs[NR33] = value;
						WAVE.frq &= 0x700;
						WAVE.frq |= value;
						break;
					case 0xFF1E:                                        // NR34 (freq hi)
						Audio_Regs[NR34] = value;
						WAVE.trigger = (value & 0x80) > 0;
						WAVE.len_en = (value & 0x40) > 0;
						WAVE.frq &= 0xFF;
						WAVE.frq |= (ushort)((value & 7) << 8);
						break;
					case 0xFF20:                                        // NR41 (length)
						Audio_Regs[NR41] = value;
						NOISE.length = (byte)(64 - value & 0x3F);
						break;
					case 0xFF21:                                        // NR42 (envelope)
						Audio_Regs[NR42] = value;
						NOISE.st_vol = (byte)((value & 0xF0) >> 4);
						NOISE.env_add = (value & 8) > 0;
						NOISE.per = (byte)(value & 7);
						break;
					case 0xFF22:                                        // NR43 (shift)
						Audio_Regs[NR43] = value;
						NOISE.clk_shft = (byte)((value & 0xF0) >> 4);
						NOISE.wdth_md = (value & 8) > 0;
						NOISE.div_code = (byte)(value & 7);
						break;
					case 0xFF23:                                        // NR44 (trigger)
						Audio_Regs[NR44] = value;
						WAVE.trigger = (value & 0x80) > 0;
						WAVE.len_en = (value & 0x40) > 0;
						break;
					case 0xFF24:                                        // NR50 (ctrl)
						Audio_Regs[NR50] = value;
						AUD_CTRL.vin_L_en = (value & 0x80) > 0;
						AUD_CTRL.vol_L = (byte)((value & 0x70) >> 4);
						AUD_CTRL.vin_R_en = (value & 8) > 0;
						AUD_CTRL.vol_R = (byte)(value & 7);
						break;
					case 0xFF25:                                        // NR51 (ctrl)
						Audio_Regs[NR51] = value;
						AUD_CTRL.noise_L_en = (value & 0x80) > 0;
						AUD_CTRL.wave_L_en = (value & 0x40) > 0;
						AUD_CTRL.sq2_L_en = (value & 0x20) > 0;
						AUD_CTRL.sq1_L_en = (value & 0x10) > 0;
						AUD_CTRL.noise_R_en = (value & 8) > 0;
						AUD_CTRL.wave_R_en = (value & 4) > 0;
						AUD_CTRL.sq2_R_en = (value & 2) > 0;
						AUD_CTRL.sq1_R_en = (value & 1) > 0;
						break;
					case 0xFF26:                                        // NR52 (ctrl)
						Audio_Regs[NR52] &= 0x7F;
						Audio_Regs[NR52] |= (byte)(value & 0x80);
						AUD_CTRL.power = (value & 0x80) > 0;

						if (!AUD_CTRL.power)
						{
							power_off();
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
			// when power is off, only length counters and waveRAM are effected by writes
			else
			{
				switch (addr)
				{
					case 0xFF11:                                        // NR11 (sound length / wave pattern duty %)
						SQ1.length = (byte)(64 - value & 0x3F);
						break;
					case 0xFF16:                                        // NR21 (sound length / wave pattern duty %)		
						SQ2.length = (byte)(64 - value & 0x3F);
						break;
					case 0xFF1B:                                        // NR31 (length)
						WAVE.length = (byte)(256 - value);
						break;
					case 0xFF20:                                        // NR41 (length)
						NOISE.length = (byte)(64 - value & 0x3F);
						break;
					case 0xFF26:                                        // NR52 (ctrl)
						Audio_Regs[NR52] &= 0x7F;
						Audio_Regs[NR52] |= (byte)(value & 0x80);
						AUD_CTRL.power = (value & 0x80) > 0;
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

		public void tick()
		{
			// calculate square1's output
			if (SQ1.enable)
			{
				SQ1.internal_cntr++;
				if (SQ1.internal_cntr == (2048 - SQ1.frq_shadow) * 4)
				{
					SQ1.internal_cntr = 0;
					SQ1.duty_counter++;
					SQ1.duty_counter &= 7;

					SQ1.output = DUTY_CYCLES[SQ1.duty * 8 + SQ1.duty_counter];
					SQ1.output *= SQ1.volume_state;
				}
			}

			// calculate square2's output
			if (SQ2.enable)
			{
				SQ2.internal_cntr++;
				if (SQ2.internal_cntr == (2048 - SQ2.frq) * 4)
				{
					SQ2.internal_cntr = 0;
					SQ2.duty_counter++;
					SQ2.duty_counter &= 7;

					SQ2.output = DUTY_CYCLES[SQ2.duty * 8 + SQ2.duty_counter];
					SQ2.output *= SQ2.volume_state;
				}
			}

			// calculate wave output

			// calculate noise output





			// frame sequencer ticks at a rate of 512 hz (or every time a 13 bit counter rolls over)
			sequencer_tick++;

			if (sequencer_tick==8192)
			{
				sequencer_tick = 0;

				sequencer_vol++; sequencer_vol &= 0x7;
				sequencer_len++; sequencer_len &= 0x7;
				sequencer_swp++; sequencer_swp &= 0x7;

				// clock the lengths
				if ((sequencer_len == 1) || (sequencer_len == 3) || (sequencer_len == 5) || (sequencer_len == 7))
				{
					if (SQ1.len_en && SQ1.length_counter > 0) { SQ1.length_counter--; if (SQ1.length_counter == 0) { SQ1.enable = false; } }
					if (SQ2.len_en && SQ2.length_counter > 0) { SQ2.length_counter--; if (SQ2.length_counter == 0) { SQ2.enable = false; } }
					if (WAVE.len_en && WAVE.length_counter > 0) { WAVE.length_counter--; if (WAVE.length_counter == 0) { WAVE.enable = false; } }
					if (NOISE.len_en && NOISE.length_counter > 0) { NOISE.length_counter--; if (NOISE.length_counter == 0) { NOISE.enable = false; } }
				}

				// clock the sweep
				if ((sequencer_swp == 3) || (sequencer_swp == 7))
				{
					if (((SQ1.swp_period > 0) || (SQ1.shift > 0)) && (SQ1.swp_period > 0))
					{
						int shadow_frq = SQ1.frq_shadow;
						shadow_frq = shadow_frq >> SQ1.shift;
						if (SQ1.negate) { shadow_frq = -shadow_frq; }
						shadow_frq += SQ1.frq_shadow;

						// disable channel if overflow
						if ((uint) shadow_frq > 2047)
						{
							SQ1.enable = false;
						}
						else
						{
							shadow_frq &= 0x7FF;
							SQ1.frq = shadow_frq;
							SQ1.frq_shadow = shadow_frq;

							// note that we also write back the frequency to the actual register
							Audio_Regs[NR13] = (byte)(SQ1.frq & 0xFF);
							Audio_Regs[NR14] &= 0xF8;
							Audio_Regs[NR14] |= (byte)((SQ1.frq >> 8) & 7);

							// after writing, we repeat the process and do another overflow check
							shadow_frq = SQ1.frq_shadow;
							shadow_frq = shadow_frq >> SQ1.shift;
							if (SQ1.negate) { shadow_frq = -shadow_frq; }
							shadow_frq += SQ1.frq_shadow;

							if ((uint)shadow_frq > 2047)
							{
								SQ1.enable = false;
							}
						}
					}
				}

				// clock the volume envelope
				if (sequencer_vol == 0)
				{
					if (SQ1.per > 0) { if (SQ1.env_add) { SQ1.volume_state++; } else { SQ1.volume_state--; } }
					if (SQ2.per > 0) { if (SQ2.env_add) { SQ2.volume_state++; } else { SQ2.volume_state--; } }
					if (WAVE.per > 0) { if (WAVE.env_add) { WAVE.volume_state++; } else { WAVE.volume_state--; } }
					if (NOISE.per > 0) { if (NOISE.env_add) { NOISE.volume_state++; } else { NOISE.volume_state--; } }
				}
			}
		}

		public void power_off()
		{
			for (int i = 0; i < 20; i++)
			{
				Audio_Regs[i] = 0;
			}

			sequencer_len = 0;
			sequencer_vol = 0;
			sequencer_swp = 0;
			sequencer_tick = 0;
		}
		public void reset()
		{
			Wave_RAM = new byte[16];

			Audio_Regs = new byte[21];

			SQ1 = new AUD_Object();
			SQ2 = new AUD_Object();
			WAVE = new AUD_Object();
			NOISE = new AUD_Object();

			AUD_CTRL = new CTRL_Object();
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("Audio_Regs", ref Audio_Regs, false);
			ser.Sync("Wave_Ram", ref Wave_RAM, false);

			// some aspecta of the channel states are not derived from the Regs
			ser.Sync("SQ1.length_counter", ref SQ1.length_counter);
			ser.Sync("SQ2.length_counter", ref SQ2.length_counter);
			ser.Sync("WAVE.length_counter", ref WAVE.length_counter);
			ser.Sync("NOISE.length_counter", ref NOISE.length_counter);

			// get derived state
			if (ser.IsReader)
			{
				sync_channels();
			}



		}

		public void sync_channels()
		{

			SQ1.swp_period = (byte)((Audio_Regs[NR10] & 0x70) >> 4);
			SQ1.negate = (Audio_Regs[NR10] & 8) > 0;
			SQ1.shift = (byte)(Audio_Regs[NR10] & 7);

			SQ1.duty = (byte)((Audio_Regs[NR11] & 0xC0) >> 6);
			SQ1.length = (byte)(64 - Audio_Regs[NR11] & 0x3F);

			SQ1.st_vol = (byte)((Audio_Regs[NR12] & 0xF0) >> 4);
			SQ1.env_add = (Audio_Regs[NR12] & 8) > 0;
			SQ1.per = (byte)(Audio_Regs[NR12] & 7);

			SQ1.frq &= 0x700;
			SQ1.frq |= Audio_Regs[NR13];

			SQ1.trigger = (Audio_Regs[NR14] & 0x80) > 0;
			SQ1.len_en = (Audio_Regs[NR14] & 0x40) > 0;
			SQ1.frq &= 0xFF;
			SQ1.frq |= (ushort)((Audio_Regs[NR14] & 7) << 8);
		
			SQ2.duty = (byte)((Audio_Regs[NR21] & 0xC0) >> 6);
			SQ2.length = (byte)(64 - Audio_Regs[NR21] & 0x3F);

			SQ2.st_vol = (byte)((Audio_Regs[NR22] & 0xF0) >> 4);
			SQ2.env_add = (Audio_Regs[NR22] & 8) > 0;
			SQ2.per = (byte)(Audio_Regs[NR22] & 7);

			SQ2.frq &= 0x700;
			SQ2.frq |= Audio_Regs[NR23];

			SQ2.trigger = (Audio_Regs[NR24] & 0x80) > 0;
			SQ2.len_en = (Audio_Regs[NR24] & 0x40) > 0;
			SQ2.frq &= 0xFF;
			SQ2.frq |= (ushort)((Audio_Regs[NR24] & 7) << 8);

			WAVE.DAC_pow = (Audio_Regs[NR30] & 0x80) > 0;

			WAVE.length = (byte)(256 - Audio_Regs[NR31]);

			WAVE.vol_code = (byte)((Audio_Regs[NR32] & 0x60) >> 5);

			WAVE.frq &= 0x700;
			WAVE.frq |= Audio_Regs[NR33];

			WAVE.trigger = (Audio_Regs[NR34] & 0x80) > 0;
			WAVE.len_en = (Audio_Regs[NR34] & 0x40) > 0;
			WAVE.frq &= 0xFF;
			WAVE.frq |= (ushort)((Audio_Regs[NR34] & 7) << 8);

			NOISE.length = (byte)(64 - Audio_Regs[NR41] & 0x3F);

			NOISE.st_vol = (byte)((Audio_Regs[NR42] & 0xF0) >> 4);
			NOISE.env_add = (Audio_Regs[NR42] & 8) > 0;
			NOISE.per = (byte)(Audio_Regs[NR42] & 7);

			NOISE.clk_shft = (byte)((Audio_Regs[NR43] & 0xF0) >> 4);
			NOISE.wdth_md = (Audio_Regs[NR43] & 8) > 0;
			NOISE.div_code = (byte)(Audio_Regs[NR43] & 7);

			WAVE.trigger = (Audio_Regs[NR44] & 0x80) > 0;
			WAVE.len_en = (Audio_Regs[NR44] & 0x40) > 0;

			AUD_CTRL.vin_L_en = (Audio_Regs[NR50] & 0x80) > 0;
			AUD_CTRL.vol_L = (byte)((Audio_Regs[NR50] & 0x70) >> 4);
			AUD_CTRL.vin_R_en = (Audio_Regs[NR50] & 8) > 0;
			AUD_CTRL.vol_R = (byte)(Audio_Regs[NR50] & 7);

			AUD_CTRL.noise_L_en = (Audio_Regs[NR51] & 0x80) > 0;
			AUD_CTRL.wave_L_en = (Audio_Regs[NR51] & 0x40) > 0;
			AUD_CTRL.sq2_L_en = (Audio_Regs[NR51] & 0x20) > 0;
			AUD_CTRL.sq1_L_en = (Audio_Regs[NR51] & 0x10) > 0;
			AUD_CTRL.noise_R_en = (Audio_Regs[NR51] & 8) > 0;
			AUD_CTRL.wave_R_en = (Audio_Regs[NR51] & 4) > 0;
			AUD_CTRL.sq2_R_en = (Audio_Regs[NR51] & 2) > 0;
			AUD_CTRL.sq1_R_en = (Audio_Regs[NR51] & 1) > 0;

			AUD_CTRL.power = (Audio_Regs[NR51] & 0x80) > 0;
		}

		#region audio

		public bool CanProvideAsync => false;

		public int _spf;
		public int AudioClocks;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported.");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			short[] ret = new short[_spf * 2];
			nsamp = _spf;
			GetSamples(ret);
			samples = ret;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			AudioClocks = 0;
		}

		// Exposing this as GetSamplesAsync would allow this to provide async sound
		// However, it does nothing special for async sound so I don't see a point
		private void GetSamples(short[] samples)
		{

		}

		#endregion
	}
}