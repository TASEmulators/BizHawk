//TODO - so many integers in the square wave output keep us from exactly unbiasing the waveform. also other waves probably. consider improving the unbiasing.
//ALSO - consider whether we should even be doing it: the nonlinear-mixing behaviour probably depends on those biases being there. 
//if we have a better high-pass filter somewhere then we might could cope with the weird biases 
//(mix higher integer precision with the non-linear mixer and then highpass filter befoure outputting s16s)

//http://wiki.nesdev.com/w/index.php/APU_Mixer_Emulation
//http://wiki.nesdev.com/w/index.php/APU
//http://wiki.nesdev.com/w/index.php/APU_Pulse
//sequencer ref: http://wiki.nesdev.com/w/index.php/APU_Frame_Counter

//TODO - refactor length counter to be separate component

using System;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class APU
	{
		public static bool CFG_DECLICK = true;

		public int Square1V = 376;
		public int Square2V = 376;
		public int TriangleV = 426;
		public int NoiseV = 247;
		public int DMCV = 167;

		public int dmc_dma_countdown = -1;
		public bool call_from_write;

		public bool recalculate = false;

		NES nes;
		public APU(NES nes, APU old, bool pal)
		{
			this.nes = nes;
			dmc = new DMCUnit(this, pal);
			noise = new NoiseUnit(this, pal);
			triangle = new TriangleUnit(this);
			pulse[0] = new PulseUnit(this, 0);
			pulse[1] = new PulseUnit(this, 1);
			if (old != null)
			{
				Square1V = old.Square1V;
				Square2V = old.Square2V;
				TriangleV = old.TriangleV;
				NoiseV = old.NoiseV;
				DMCV = old.DMCV;
			}
		}

		static int[] DMC_RATE_NTSC = { 428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54 };
		static int[] DMC_RATE_PAL = { 398, 354, 316, 298, 276, 236, 210, 198, 176, 148, 132, 118, 98, 78, 66, 50 };
		static int[] LENGTH_TABLE = { 10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14, 12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30 };
		static byte[,] PULSE_DUTY = {
			{0,1,0,0,0,0,0,0}, //(12.5%)
			{0,1,1,0,0,0,0,0}, //(25%)
			{0,1,1,1,1,0,0,0}, //(50%)
			{1,0,0,1,1,1,1,1}, //(25% negated (75%))
		};
		static byte[] TRIANGLE_TABLE =
		{
			15, 14, 13, 12, 11, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0,
 			0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15
		};
		static int[] NOISE_TABLE_NTSC =
		{
			4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068
		};
		static int[] NOISE_TABLE_PAL =
		{
			4, 7, 14, 30, 60, 88, 118, 148, 188, 236, 354, 472, 708,  944, 1890, 3778
		};


		public sealed class PulseUnit
		{
			public PulseUnit(APU apu, int unit) { this.unit = unit; this.apu = apu; }
			public int unit;
			APU apu;

			//reg0
			int duty_cnt, env_loop, env_constant, env_cnt_value;
			public bool len_halt;
			//reg1
			int sweep_en, sweep_divider_cnt, sweep_negate, sweep_shiftcount;
			bool sweep_reload;
			//reg2/3
			int len_cnt;
			public int timer_raw_reload_value, timer_reload_value;

			//misc..
			int lenctr_en;

			public void SyncState(Serializer ser)
			{
				ser.BeginSection("Pulse" + unit);
				ser.Sync("duty_cnt", ref duty_cnt);
				ser.Sync("env_loop", ref env_loop);
				ser.Sync("env_constant", ref env_constant);
				ser.Sync("env_cnt_value", ref env_cnt_value);
				ser.Sync("len_halt", ref len_halt);

				ser.Sync("sweep_en", ref sweep_en);
				ser.Sync("sweep_divider_cnt", ref sweep_divider_cnt);
				ser.Sync("sweep_negate", ref sweep_negate);
				ser.Sync("sweep_shiftcount", ref sweep_shiftcount);
				ser.Sync("sweep_reload", ref sweep_reload);

				ser.Sync("len_cnt", ref len_cnt);
				ser.Sync("timer_raw_reload_value", ref timer_raw_reload_value);
				ser.Sync("timer_reload_value", ref timer_reload_value);

				ser.Sync("lenctr_en", ref lenctr_en);

				ser.Sync("swp_divider_counter", ref swp_divider_counter);
				ser.Sync("swp_silence", ref swp_silence);
				ser.Sync("duty_step", ref duty_step);
				ser.Sync("timer_counter", ref timer_counter);
				ser.Sync("sample", ref sample);
				ser.Sync("duty_value", ref duty_value);

				ser.Sync("env_start_flag", ref env_start_flag);
				ser.Sync("env_divider", ref env_divider);
				ser.Sync("env_counter", ref env_counter);
				ser.Sync("env_output", ref env_output);
				ser.EndSection();
			}

			public bool IsLenCntNonZero() { return len_cnt > 0; }

			public void WriteReg(int addr, byte val)
			{
				//Console.WriteLine("write pulse {0:X} {1:X}", addr, val);
				switch (addr)
				{
					case 0:
						env_cnt_value = val & 0xF;
						env_constant = (val >> 4) & 1;
						env_loop = (val >> 5) & 1;
						duty_cnt = (val >> 6) & 3;
						break;
					case 1:
						sweep_shiftcount = val & 7;
						sweep_negate = (val >> 3) & 1;
						sweep_divider_cnt = (val >> 4) & 7;
						sweep_en = (val >> 7) & 1;
						sweep_reload = true;
						break;
					case 2:
						timer_reload_value = (timer_reload_value & 0x700) | val;
						timer_raw_reload_value = timer_reload_value * 2 + 2;
						//if (unit == 1) Console.WriteLine("{0} timer_reload_value: {1}", unit, timer_reload_value);
						break;
					case 3:
						if (apu.len_clock_active)
						{
							if (len_cnt==0)
							{
								len_cnt = LENGTH_TABLE[(val >> 3) & 0x1F]+1;
							}						
						} else
						{
							len_cnt = LENGTH_TABLE[(val >> 3) & 0x1F];
						}

						timer_reload_value = (timer_reload_value & 0xFF) | ((val & 0x07) << 8);
						timer_raw_reload_value = timer_reload_value * 2 + 2;
						duty_step = 0; 
						env_start_flag = 1;

						//allow the lenctr_en to kill the len_cnt
						set_lenctr_en(lenctr_en);

						//serves as a useful note-on diagnostic
						//if(unit==1) Console.WriteLine("{0} timer_reload_value: {1}", unit, timer_reload_value);
						break;
				}
			}

			public void set_lenctr_en(int value)
			{
				lenctr_en = value;
				//if the length counter is not enabled, then we must disable the length system in this way
				if (lenctr_en == 0) len_cnt = 0;
			}

			//state
			//why was all of this stuff not in the savestate???????
			int swp_divider_counter;
			bool swp_silence;
			int duty_step;
			int timer_counter;
			public int sample;
			bool duty_value;

			int env_start_flag, env_divider, env_counter;
			public int env_output;

			public void clock_length_and_sweep()
			{
				//this should be optimized to update only when `timer_reload_value` changes
				int sweep_shifter = timer_reload_value >> sweep_shiftcount;
				if (sweep_negate == 1)
					sweep_shifter = -sweep_shifter + unit;
				sweep_shifter += timer_reload_value;

				//this sweep logic is always enabled:
				swp_silence = (timer_reload_value < 8 || (sweep_shifter > 0x7FF)); // && sweep_negate == 0));

				//does enable only block the pitch bend? does the clocking proceed?
				if (sweep_en == 1)
				{
					//clock divider
					if (swp_divider_counter != 0) swp_divider_counter--;
					if (swp_divider_counter == 0)
					{
						swp_divider_counter = sweep_divider_cnt + 1;

						//divider was clocked: process sweep pitch bend
						if (sweep_shiftcount != 0 && !swp_silence)
						{
							timer_reload_value = sweep_shifter;
							timer_raw_reload_value = (timer_reload_value << 1) + 2;
						}
						//TODO - does this change the user's reload value or the latched reload value?
					}

					//handle divider reload, after clocking happens
					if (sweep_reload)
					{
						swp_divider_counter = sweep_divider_cnt + 1;
						sweep_reload = false;
					}
				}

				//env_loopdoubles as "halt length counter"
				if ((env_loop == 0 || len_halt) && len_cnt > 0)
					len_cnt--;
			}

			public void clock_env()
			{
				if (env_start_flag == 1)
				{
					env_start_flag = 0;
					env_divider = env_cnt_value;
					env_counter = 15;
				}
				else
				{
					if (env_divider != 0)
					{
						env_divider--;
					} else if (env_divider == 0)
					{
						env_divider = env_cnt_value;
						if (env_counter == 0)
						{
							if (env_loop == 1)
							{
								env_counter = 15;
							}
						}
						else env_counter--;
					}
				}
			}

			public void Run()
			{
				if (env_constant == 1)
					env_output = env_cnt_value;
				else env_output = env_counter;

				if (timer_counter > 0) timer_counter--;
				if (timer_counter == 0 && timer_raw_reload_value != 0)
				{
					if (duty_step==7)
					{
						duty_step = 0;
					} else
					{
						duty_step++;
					}
					duty_value = PULSE_DUTY[duty_cnt, duty_step] == 1;
					//reload timer
					timer_counter = timer_raw_reload_value;
				}

				int newsample;

				if (duty_value) //high state of duty cycle
				{
					newsample = env_output;
					if (swp_silence || len_cnt == 0)
						newsample = 0; // silenced
				}
				else
					newsample = 0; //duty cycle is 0, silenced.

				//newsample -= env_output >> 1; //unbias
				if (newsample != sample)
				{
					apu.recalculate = true;
					sample = newsample;
				}
			}

			public bool Debug_IsSilenced
			{
				get
				{
					if (swp_silence || len_cnt == 0)
						return true;
					else return false;
				}
			}

			public int Debug_DutyType
			{
				get
				{
					return duty_cnt;
				}
			}

			public int Debug_Volume
			{
				get
				{
					return env_output;
				}
			}
		}

		public sealed class NoiseUnit
		{
			APU apu;

			//reg0 (sweep)
			int env_cnt_value, env_loop, env_constant;
			public bool len_halt;

			//reg2 (mode and period)
			int mode_cnt, period_cnt;

			//reg3 (length counter and envelop trigger)
			int len_cnt;

			//set from apu:
			int lenctr_en;

			//state
			int shift_register = 1;
			int timer_counter;
			public int sample;
			int env_output, env_start_flag, env_divider, env_counter;
			bool noise_bit = true;

			int[] NOISE_TABLE;

			public NoiseUnit(APU apu, bool pal)
			{
				this.apu = apu;
				NOISE_TABLE = pal ? NOISE_TABLE_PAL : NOISE_TABLE_NTSC;
			}

			public bool Debug_IsSilenced
			{
				get
				{
					if (len_cnt == 0) return true;
					else return false;
				}
			}

			public int Debug_Period
			{
				get
				{
					return period_cnt;
				}
			}

			public int Debug_Volume
			{
				get
				{
					return env_output;
				}
			}

			public void SyncState(Serializer ser)
			{
				ser.BeginSection("Noise");
				ser.Sync("env_cnt_value", ref env_cnt_value);
				ser.Sync("env_loop", ref env_loop);
				ser.Sync("env_constant", ref env_constant);
				ser.Sync("mode_cnt", ref mode_cnt);
				ser.Sync("period_cnt", ref period_cnt);

				ser.Sync("len_halt", ref len_halt);

				//ser.Sync("mode_cnt", ref mode_cnt);
				//ser.Sync("period_cnt", ref period_cnt);

				ser.Sync("len_cnt", ref len_cnt);

				ser.Sync("lenctr_en", ref lenctr_en);

				ser.Sync("shift_register", ref shift_register);
				ser.Sync("timer_counter", ref timer_counter);
				ser.Sync("sample", ref sample);

				ser.Sync("env_output", ref env_output);
				ser.Sync("env_start_flag", ref env_start_flag);
				ser.Sync("env_divider", ref env_divider);
				ser.Sync("env_counter", ref env_counter);
				ser.Sync("noise_bit", ref noise_bit);
				ser.EndSection();
			}


			public bool IsLenCntNonZero() { return len_cnt > 0; }

			public void WriteReg(int addr, byte val)
			{
				switch (addr)
				{
					case 0:
						env_cnt_value = val & 0xF;
						env_constant = (val >> 4) & 1;
						//we want to delay a halt until after a length clock if they happen on the same cycle
						if (env_loop==0 && ((val >> 5) & 1)==1)
						{
							len_halt = true;
						}
						env_loop = (val >> 5) & 1;
						break;
					case 1:
						break;
					case 2:
						period_cnt = NOISE_TABLE[val & 0xF];
						mode_cnt = (val >> 7) & 1;
						//Console.WriteLine("noise period: {0}, vol: {1}", (val & 0xF), env_cnt_value);
						break;
					case 3:
						if (apu.len_clock_active)
						{
							if (len_cnt == 0)
							{
								len_cnt = LENGTH_TABLE[(val >> 3) & 0x1F] + 1;
							}
						}
						else
						{
							len_cnt = LENGTH_TABLE[(val >> 3) & 0x1F];
						}

						set_lenctr_en(lenctr_en);
						env_start_flag = 1;
						break;
				}
			}

			public void set_lenctr_en(int value)
			{
				lenctr_en = value;
				//Console.WriteLine("noise lenctr_en: " + lenctr_en);
				//if the length counter is not enabled, then we must disable the length system in this way
				if (lenctr_en == 0) len_cnt = 0;
			}

			public void clock_env()
			{
				if (env_start_flag == 1)
				{
					env_start_flag = 0;
					env_divider = (env_cnt_value + 1);
					env_counter = 15;
				}
				else
				{
					if (env_divider != 0) env_divider--;
					if (env_divider == 0)
					{
						env_divider = (env_cnt_value + 1);
						if (env_counter == 0)
						{
							if (env_loop == 1)
							{
								env_counter = 15;
							}
						}
						else env_counter--;
					}
				}

			}
			public void clock_length_and_sweep()
			{

				if (len_cnt > 0 && (env_loop == 0 || len_halt))
					len_cnt--;
			}

			public void Run()
			{
				if (env_constant == 1)
					env_output = env_cnt_value;
				else env_output = env_counter;

				if (timer_counter > 0) timer_counter--;
				if (timer_counter == 0 && period_cnt != 0)
				{
					//reload timer
					timer_counter = period_cnt;
					int feedback_bit;
					if (mode_cnt == 1) feedback_bit = (shift_register >> 6) & 1;
					else feedback_bit = (shift_register >> 1) & 1;
					int feedback = feedback_bit ^ (shift_register & 1);
					shift_register >>= 1;
					shift_register &= ~(1 << 14);
					shift_register |= (feedback << 14);
					noise_bit = (shift_register & 1) != 0;
				}

				int newsample;
				if (len_cnt == 0) newsample = 0;
				else if (noise_bit) newsample = env_output; // switched, was 0?
				else newsample = 0;
				if (newsample != sample)
				{
					apu.recalculate = true;
					sample = newsample;
				}
			}
		}

		public sealed class TriangleUnit
		{
			//reg0
			int linear_counter_reload, control_flag;
			//reg1 (n/a)
			//reg2/3
			int timer_cnt, halt_flag, len_cnt;
			public bool halt_2;
			//misc..
			int lenctr_en;
			int linear_counter, timer, timer_cnt_reload;
			int seq = 15;
			public int sample;

			APU apu;
			public TriangleUnit(APU apu) { this.apu = apu; }

			public void SyncState(Serializer ser)
			{
				ser.BeginSection("Triangle");
				ser.Sync("linear_counter_reload", ref linear_counter_reload);
				ser.Sync("control_flag", ref control_flag);
				ser.Sync("timer_cnt", ref timer_cnt);
				ser.Sync("halt_flag", ref halt_flag);
				ser.Sync("len_cnt", ref len_cnt);

				ser.Sync("lenctr_en", ref lenctr_en);
				ser.Sync("linear_counter", ref linear_counter);
				ser.Sync("timer", ref timer);
				ser.Sync("timer_cnt_reload", ref timer_cnt_reload);
				ser.Sync("seq", ref seq);
				ser.Sync("sample", ref sample);
				ser.EndSection();
			}

			public bool IsLenCntNonZero() { return len_cnt > 0; }

			public void set_lenctr_en(int value)
			{
				lenctr_en = value;
				//if the length counter is not enabled, then we must disable the length system in this way
				if (lenctr_en == 0) len_cnt = 0;
			}

			public void WriteReg(int addr, byte val)
			{
				//Console.WriteLine("tri writes addr={0}, val={1:x2}", addr, val);
				switch (addr)
				{
					case 0:
						linear_counter_reload = (val & 0x7F);
						control_flag = (val >> 7) & 1;
						break;
					case 1: break;
					case 2:
						timer_cnt = (timer_cnt & ~0xFF) | val;
						timer_cnt_reload = timer_cnt + 1;
						break;
					case 3:
						timer_cnt = (timer_cnt & 0xFF) | ((val & 0x7) << 8);
						timer_cnt_reload = timer_cnt + 1;
						if (apu.len_clock_active)
						{
							if (len_cnt == 0)
							{
								len_cnt = LENGTH_TABLE[(val >> 3) & 0x1F] + 1;
							}
						}
						else
						{
							len_cnt = LENGTH_TABLE[(val >> 3) & 0x1F];
						}
						halt_flag = 1;

						//allow the lenctr_en to kill the len_cnt
						set_lenctr_en(lenctr_en);
						break;
				}
				//Console.WriteLine("tri timer_reload_value: {0}", timer_cnt_reload);
			}

			public bool Debug_IsSilenced
			{
				get
				{
					bool en = len_cnt != 0 && linear_counter != 0;
					return !en;
				}
			}

			public int Debug_PeriodValue
			{
				get
				{
					return timer_cnt;
				}
			}

			public void Run()
			{
				//when clocked by timer
				//seq steps forward
				//except when linear counter or
				//length counter is 0

				//dont stop the triangle channel until its level is 0. makes it sound nicer.
				bool need_declick = (seq != 16 && seq != 15);
				bool en = len_cnt != 0 && linear_counter != 0 || need_declick;

				//length counter and linear counter 
				//is clocked in frame counter.
				if (en)
				{
					int newsample;
					if (timer > 0) timer--;
					if (timer == 0)
					{
						seq = (seq + 1) & 0x1F;
						timer = timer_cnt_reload;
					}
					if (CFG_DECLICK) // this looks ugly...
						newsample = TRIANGLE_TABLE[(seq + 8) & 0x1F];
					else
						newsample = TRIANGLE_TABLE[seq];

					//special hack: frequently, games will use the maximum frequency triangle in order to mute it
					//apparently this results in the DAC for the triangle wave outputting a steady level at about 7.5
					//so we'll emulate it at the digital level
					if (timer_cnt_reload == 1) newsample = 8;

					//newsample -= 8; //unbias
					if (newsample != sample)
					{
						apu.recalculate = true;
						sample = newsample;
					}
				}

			}


			public void clock_length_and_sweep()
			{
				//env_loopdoubles as "halt length counter"
				if (len_cnt > 0 && halt_flag == 0)
					len_cnt--;
			}

			public void clock_linear_counter()
			{
				//	Console.WriteLine("linear_counter: {0}", linear_counter);
				if (halt_flag == 1)
				{
					linear_counter = linear_counter_reload;
				}
				else if (linear_counter != 0)
				{
					linear_counter--;
				}

				//declick when the sound begins
				//if (halt_flag == 1 && control_flag == 0)
				//{
				//    seq = 16;
				//   Console.WriteLine("declicked triangle");
				//}

				//declick on end of sound
				//bool en = len_cnt != 0 && linear_counter != 0;
				//if (!en)
				//    if (sample < 0) sample++; else if (sample > 0) sample--;

				halt_flag = control_flag;
			}
		} //class TriangleUnit

		sealed class DMCUnit
		{
			APU apu;
			int[] DMC_RATE;
			public DMCUnit(APU apu, bool pal)
			{
				this.apu = apu;
				out_silence = true;
				DMC_RATE = pal ? DMC_RATE_PAL : DMC_RATE_NTSC;
				timer_reload = DMC_RATE[0];
				timer = timer_reload;
				sample_buffer_filled = false;
				out_deltacounter = 64;
				out_bits_remaining = 0;
			}

			bool irq_enabled;
			bool loop_flag;
			int timer_reload;

			//dmc delay per visual 2a03
			int delay;

			// this timer never stops, ever, so it is convenient to use for even/odd timing used elsewhere
			public int timer;
			int user_address;
			public uint user_length, sample_length;
			int sample_address, sample_buffer;
			bool sample_buffer_filled;

			int out_shift, out_bits_remaining, out_deltacounter;
			bool out_silence;

			public int sample { get { return out_deltacounter /* - 64*/; } }

			public void SyncState(Serializer ser)
			{
				ser.BeginSection("DMC");
				ser.Sync("irq_enabled", ref irq_enabled);
				ser.Sync("loop_flag", ref loop_flag);
				ser.Sync("timer_reload", ref timer_reload);

				ser.Sync("timer", ref timer);
				ser.Sync("user_address", ref user_address);
				ser.Sync("user_length", ref user_length);

				ser.Sync("sample_address", ref sample_address);
				ser.Sync("sample_length", ref sample_length);
				ser.Sync("sample_buffer", ref sample_buffer);
				ser.Sync("sample_buffer_filled", ref sample_buffer_filled);

				ser.Sync("out_shift", ref out_shift);
				ser.Sync("out_bits_remaining", ref out_bits_remaining);
				ser.Sync("out_deltacounter", ref out_deltacounter);
				ser.Sync("out_silence", ref out_silence);

				ser.Sync("dmc_call_delay", ref delay);

				//int sample = 0; //junk
				//ser.Sync("sample", ref sample);
				ser.EndSection();
			}

			public void Run()
			{
				if (timer > 0) timer--;
				if (timer == 0)
				{
					timer = timer_reload;
					Clock();
				}

				//Any time the sample buffer is in an empty state and bytes remaining is not zero, the following occur: 
				// also note that the halt for DMC DMA occurs on APU cycles only (hence the timer check)
				
				
				
				if (!sample_buffer_filled && sample_length > 0  && apu.dmc_dma_countdown == -1 && delay==0)
				{
					// calls from write take one less cycle, but start on a write instead of a read
					if (!apu.call_from_write)
					{
						if (timer % 2 == 1)
						{
							delay = 3;
						} else
						{
							delay = 2;
						}
					} else
					{
						if (timer % 2 == 1)
						{
							delay = 2;
						}
						else
						{
							delay = 3;
						}
					}					
				}

				// I did some tests in Visual 2A03 and there seems to be some delay betwen when a DMC is first needed and when the 
				// process to execute the DMA starts. The details are not currently known, but it seems to be a 2 cycle delay
				if (delay != 0)
				{
					delay--;
					if (delay == 0)
					{
						if (!apu.call_from_write)
						{
							apu.dmc_dma_countdown = 4;
						}
						else
						{

							apu.dmc_dma_countdown = 3;
							apu.call_from_write = false;
						}
					}

				}
			}


			void Clock()
			{
				//If the silence flag is clear, bit 0 of the shift register is applied to the counter as follows: 
				//if bit 0 is clear and the delta-counter is greater than 1, the counter is decremented by 2; 
				//otherwise, if bit 0 is set and the delta-counter is less than 126, the counter is incremented by 2
				if (!out_silence)
				{
					//apply current sample bit to delta counter
					if (out_shift.Bit(0))
					{
						if (out_deltacounter < 126)
							out_deltacounter += 2;
					}
					else
					{
						if (out_deltacounter > 1)
							out_deltacounter -= 2;
					}
					//apu.nes.LogLine("dmc out sample: {0}", out_deltacounter);
					apu.recalculate = true;
				}

				//The right shift register is clocked. 
				out_shift >>= 1;

				//The bits-remaining counter is decremented. If it becomes zero, a new cycle is started. 
				if (out_bits_remaining == 0)
				{
					//The bits-remaining counter is loaded with 8. 
					out_bits_remaining = 7;
					//If the sample buffer is empty then the silence flag is set
					if (!sample_buffer_filled)
					{
						out_silence = true;
						//out_deltacounter = 64; //gonna go out on a limb here and guess this gets reset. could make some things pop, though, if they dont end at 0.
					}
					else
					//otherwise, the silence flag is cleared and the sample buffer is emptied into the shift register. 
					{
						out_silence = false;
						out_shift = sample_buffer;
						sample_buffer_filled = false;
					}
				}
				else out_bits_remaining--;


			}

			public void set_lenctr_en(bool en)
			{
				if (!en)
				{
					//If the DMC bit is clear, the DMC bytes remaining will be set to 0 
					sample_length = 0;
					//and the DMC will silence when it empties.
					//  (what does this mean? does out_deltacounter get reset to 0? maybe just that the out_silence flag gets set, but this is natural)
				}
				else
				{
					//only start playback if playback is stopped
					//Console.Write(sample_length); Console.Write(" "); Console.Write(sample_buffer_filled); Console.Write(" "); Console.Write(apu.dmc_irq); Console.Write("\n");
					if (sample_length == 0)
					{
						sample_address = user_address;
						sample_length = user_length;
						
					}
					if (!sample_buffer_filled)
					{
						// apparently the dmc is different if called from a cpu write, let's try
						apu.call_from_write = true;
					}
				}

				//irq is acknowledged or sure to be clear, in either case
				apu.dmc_irq = false;
				apu.SyncIRQ();
			}

			public bool IsLenCntNonZero()
			{
				return sample_length != 0;
			}

			public void WriteReg(int addr, byte val)
			{
				//Console.WriteLine("DMC writes addr={0}, val={1:x2}", addr, val);
				switch (addr)
				{
					case 0:
						irq_enabled = val.Bit(7);
						loop_flag = val.Bit(6);
						timer_reload = DMC_RATE[val & 0xF];
						if (!irq_enabled) apu.dmc_irq = false;
						//apu.dmc_irq = false;
						apu.SyncIRQ();
						break;
					case 1:
						out_deltacounter = val & 0x7F;
						//apu.nes.LogLine("~~ out_deltacounter set to {0}", out_deltacounter);
						apu.recalculate = true;
						break;
					case 2:
						user_address = 0xC000 | (val << 6);
						break;
					case 3:
						user_length = ((uint)val << 4) + 1;
						break;
				}
			}

			public void Fetch()
			{
				if (sample_length != 0)
				{
					sample_buffer = apu.nes.ReadMemory((ushort)sample_address);
					sample_buffer_filled = true;
					sample_address = (ushort)(sample_address + 1);
					//Console.WriteLine(sample_length);
					//Console.WriteLine(user_length);
					sample_length--;
					//apu.pending_length_change = 1;
				}
				if (sample_length == 0)
				{
					if (loop_flag)
					{
						sample_address = user_address;
						sample_length = user_length;
					}
					else if (irq_enabled) apu.dmc_irq = true;
				}
				//Console.WriteLine("fetching dmc byte: {0:X2}", sample_buffer);
			}
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("irq_pending", ref irq_pending);
			ser.Sync("dmc_irq", ref dmc_irq);
			ser.Sync("pending_reg", ref pending_reg);
			ser.Sync("pending_val", ref pending_val);

			ser.Sync("sequencer_counter", ref sequencer_counter);
			ser.Sync("sequencer_step", ref sequencer_step);
			ser.Sync("sequencer_mode", ref sequencer_mode);
			ser.Sync("sequencer_irq_inhibit;", ref sequencer_irq_inhibit);
			ser.Sync("sequencer_irq", ref sequencer_irq);
			ser.Sync("sequence_reset_pending", ref sequence_reset_pending);
			ser.Sync("sequencer_irq_clear_pending", ref sequencer_irq_clear_pending);
			ser.Sync("sequencer_irq_assert", ref sequencer_irq_assert);

			ser.Sync("dmc_dma_countdown", ref dmc_dma_countdown);
			ser.Sync("sample_length_delay", ref pending_length_change);
			ser.Sync("dmc_called_from_write", ref call_from_write);
			ser.Sync("sequencer_tick_delay", ref seq_tick);
			ser.Sync("seq_val_to_apply", ref seq_val);
			ser.Sync("sequencer_irq_flag", ref sequencer_irq_flag);
			ser.Sync("len_clock_active", ref len_clock_active);


			pulse[0].SyncState(ser);
			pulse[1].SyncState(ser);
			triangle.SyncState(ser);
			noise.SyncState(ser);
			dmc.SyncState(ser);
			SyncIRQ();
		}

		public PulseUnit[] pulse = new PulseUnit[2];
		public TriangleUnit triangle;
		public NoiseUnit noise;
		DMCUnit dmc;

		bool irq_pending;
		bool dmc_irq;
		int pending_reg = -1;
		byte pending_val = 0;
		public int seq_tick;
		public byte seq_val;
		public bool len_clock_active;

		int sequencer_counter, sequencer_step, sequencer_mode, sequencer_irq_inhibit, sequencer_irq_assert;
		bool sequencer_irq, sequence_reset_pending, sequencer_irq_clear_pending, sequencer_irq_flag;

		public void RunDMCFetch()
		{
			dmc.Fetch();
		}

		void sequencer_reset()
		{
			sequencer_counter = 0;

			if (sequencer_mode == 1)
			{
				sequencer_step = 0;
				QuarterFrame();
				HalfFrame();
			}
			else
				sequencer_step = 0;
		}

		//these figures are not valid for PAL. they must be recalculated with nintendulator's values above
		static int[][] sequencer_lut = new int[][]{
			new int[]{7457,14913,22371,29830},
			new int[]{7457,14913,22371,29830,37282}
		};

		
		void sequencer_write_tick(byte val)
		{
			if (seq_tick>0)
			{
				seq_tick--;
				if (seq_tick==0)
				{
					sequencer_mode = (val >> 7) & 1;
					//Console.WriteLine("apu 4017 = {0:X2}", val);
					sequencer_irq_inhibit = (val >> 6) & 1;
					if (sequencer_irq_inhibit == 1)
					{
						sequencer_irq_flag = false;
					}
					sequencer_reset();
				}
			}
		}

		void sequencer_tick()
		{
			sequencer_counter++;
			if (sequencer_mode==0 && sequencer_counter==29829)
			{
				if (sequencer_irq_inhibit==0)
				{
					sequencer_irq_assert = 2;
					sequencer_irq_flag = true;
				}
					
				HalfFrame();
			}
			if (sequencer_mode == 0 && sequencer_counter == 29828 && sequencer_irq_inhibit == 0)
			{
				//sequencer_irq_assert = 2;
				sequencer_irq_flag = true;
			}
			if (sequencer_mode == 1 && sequencer_counter == 37281)
			{
				HalfFrame();
			}
			if (sequencer_lut[sequencer_mode][sequencer_step] != sequencer_counter)
				return;
			sequencer_check();
		}

		public void SyncIRQ()
		{
			irq_pending = sequencer_irq | dmc_irq;
		}

		void sequencer_check()
		{
			//Console.WriteLine("sequencer mode {0} step {1}", sequencer_mode, sequencer_step);
			bool quarter, half, reset;
			switch (sequencer_mode)
			{
				case 0: //4-step
					quarter = true;
					half = sequencer_step == 1;
					reset = sequencer_step == 3;
					if (reset && sequencer_irq_inhibit == 0)
					{
						//Console.WriteLine("{0} {1,5} set irq_assert", nes.Frame, sequencer_counter);
						//sequencer_irq_assert = 2;
						sequencer_irq_flag = true;
					}
					break;

				case 1: //5-step
					quarter = sequencer_step != 3;
					half = sequencer_step == 1;
					reset = sequencer_step == 4;
					break;

				default:
					throw new InvalidOperationException();
			}

			if (reset)
			{
				sequencer_counter = 0;
				sequencer_step = 0;
			}
			else sequencer_step++;

			if (quarter) QuarterFrame();
			if (half) HalfFrame();
		}

		void HalfFrame()
		{
			pulse[0].clock_length_and_sweep();
			pulse[1].clock_length_and_sweep();
			triangle.clock_length_and_sweep();
			noise.clock_length_and_sweep();
		}

		void QuarterFrame()
		{
			pulse[0].clock_env();
			pulse[1].clock_env();
			triangle.clock_linear_counter();
			noise.clock_env();
		}

		public void NESSoftReset()
		{
			//need to study what happens to apu and stuff..
			sequencer_irq = false;
			sequencer_irq_flag = false;
			_WriteReg(0x4015, 0);

			//for 4017, its as if the last value written gets rewritten
			sequencer_mode = (seq_val >> 7) & 1;
			sequencer_irq_inhibit = (seq_val >> 6) & 1;
			if (sequencer_irq_inhibit == 1)
			{
				sequencer_irq_flag = false;
			}
			sequencer_counter = 0;
			sequencer_step = 0;

		}

		public void NESHardReset()
		{
			// "at power on it is as if $00 was written to $4017 9-12 cycles before the reset vector"
			// that translates to a starting value for the counter of 6

			sequencer_counter = 6;

		}

		public void WriteReg(int addr, byte val)
		{
			pending_reg = addr;
			pending_val = val;
		}

		void _WriteReg(int addr, byte val)
		{
			//Console.WriteLine("{0:X4} = {1:X2}", addr, val);
			int index = addr - 0x4000;
			int reg = index & 3;
			int channel = index >> 2;
			switch (channel)
			{
				case 0:
					pulse[0].WriteReg(reg, val);
					break;
				case 1:
					pulse[1].WriteReg(reg, val);
					break;
				case 2:
					triangle.WriteReg(reg, val);
					break;
				case 3:
					noise.WriteReg(reg, val);
					break;
				case 4:
					dmc.WriteReg(reg, val);
					break;
				case 5:
					if (addr == 0x4015)
					{
						pulse[0].set_lenctr_en(val & 1);
						pulse[1].set_lenctr_en((val >> 1) & 1);
						triangle.set_lenctr_en((val >> 2) & 1);
						noise.set_lenctr_en((val >> 3) & 1);
						dmc.set_lenctr_en(val.Bit(4));

					}
					else if (addr == 0x4017)
					{
						if (dmc.timer%2==0)
						{
							seq_tick = 3;

						} else
						{
							seq_tick = 4;
						}
						
						seq_val = val;
					}
					break;
			}
		}


		public byte PeekReg(int addr)
		{
			switch (addr)
			{
				case 0x4015:
					{
						//notice a missing bit here. should properly emulate with empty / Data bus
						//if an interrupt flag was set at the same moment of the read, it will read back as 1 but it will not be cleared. 
						int dmc_nonzero = dmc.IsLenCntNonZero() ? 1 : 0;
						int noise_nonzero = noise.IsLenCntNonZero() ? 1 : 0;
						int tri_nonzero = triangle.IsLenCntNonZero() ? 1 : 0;
						int pulse1_nonzero = pulse[1].IsLenCntNonZero() ? 1 : 0;
						int pulse0_nonzero = pulse[0].IsLenCntNonZero() ? 1 : 0;
						int ret = ((dmc_irq ? 1 : 0) << 7) | ((sequencer_irq_flag ? 1 : 0) << 6) | (dmc_nonzero << 4) | (noise_nonzero << 3) | (tri_nonzero << 2) | (pulse1_nonzero << 1) | (pulse0_nonzero);
						return (byte)ret;
					}
				default:
					//don't return 0xFF here or SMB will break
					return 0x00;
			}
		}

		public byte ReadReg(int addr)
		{
			switch (addr)
			{
				case 0x4015:
					{
						byte ret = PeekReg(0x4015);
						//Console.WriteLine("{0} {1,5} $4015 clear irq, was at {2}", nes.Frame, sequencer_counter, sequencer_irq);
						sequencer_irq_flag = false;
						SyncIRQ();
						return ret;
					}
				default:
					//don't return 0xFF here or SMB will break
					return 0x00;
			}
		}

		public Action DebugCallback;
		public int DebugCallbackDivider;
		public int DebugCallbackTimer;

		int pending_length_change;


		public void RunOne(bool read)
		{
			if (read)
			{
				pulse[0].Run();
				pulse[1].Run();
				triangle.Run();
				noise.Run();
				dmc.Run();

				pulse[0].len_halt = false;
				pulse[1].len_halt = false;
				noise.len_halt = false;

			}
			else
			{
				if (pending_length_change>0)
				{
					pending_length_change--;
					if (pending_length_change==0)
					{
						dmc.sample_length--;
					}
				}

				EmitSample();

				//we need to predict if there will be a length clock here, because the sequencer ticks last, but the 
				// timer reload shouldn't happen if length clock and write happen simultaneously
				// I'm not sure if we can avoid this by simply processing the sequencer first
				// but at the moment that would break everything, so this is good enough for now
				if (sequencer_counter==14912 || 
					(sequencer_counter == 29828 && sequencer_mode==0) ||
					(sequencer_counter == 37280 && sequencer_mode == 1))
				{
					len_clock_active = true;
				}

				//handle writes
				//notes: this set up is a bit convoluded at the moment, mainly because APU behaviour is not entirely understood
				//in partiuclar, there are several clock pulses affecting the APU, and when new written are latched is not known in detail
				//the current code simply matches known behaviour
				if (pending_reg != -1)
				{
					if (pending_reg == 0x4015 || pending_reg == 0x4017 || pending_reg==0x4003 || pending_reg==0x4007)
					{
						_WriteReg(pending_reg, pending_val);
						pending_reg = -1;
					}
					else if (dmc.timer%2==0)
					{
						_WriteReg(pending_reg, pending_val);
						pending_reg = -1;
					}
				}

				len_clock_active = false;

				sequencer_tick();
				sequencer_write_tick(seq_val);
				
				if (sequencer_irq_assert>0) {
					sequencer_irq_assert--;
					if (sequencer_irq_assert==0)
					{
						sequencer_irq = true;
					}					
				}				

				SyncIRQ();
				nes.irq_apu = irq_pending;
				
				//since the units run concurrently, the APU frame sequencer is ran last because
				//it can change the ouput values of the pulse/triangle channels
				//we want the changes to affect it on the *next* cycle.

				if (sequencer_irq_flag == false)
					sequencer_irq = false;

				if (DebugCallbackDivider != 0)
				{
					if (DebugCallbackTimer == 0)
					{
						if (DebugCallback != null)
							DebugCallback();
						DebugCallbackTimer = DebugCallbackDivider;
					}
					else DebugCallbackTimer--;

				}
			}

		}

		public struct Delta
		{
			public uint time;
			public int value;
			public Delta(uint time, int value)
			{
				this.time = time;
				this.value = value;
			}
		}
		public List<Delta> dlist = new List<Delta>();

		/// <summary>only call in board.ClockCPU()</summary>
		/// <param name="value"></param>
		public void ExternalQueue(int value)
		{
			// sampleclock is incremented right before board.ClockCPU()
			dlist.Add(new Delta(sampleclock - 1, value));
		}

		public uint sampleclock = 0;

		int oldmix = 0;


		void EmitSample()
		{
			if (recalculate)
			{
				recalculate = false;

				int s_pulse0 = pulse[0].sample;
				int s_pulse1 = pulse[1].sample;
				int s_tri = triangle.sample;
				int s_noise = noise.sample;
				int s_dmc = dmc.sample;
				//int s_ext = 0; //gamepak

				/*
				if (!EnableSquare1) s_pulse0 = 0;
				if (!EnableSquare2) s_pulse1 = 0;
				if (!EnableTriangle) s_tri = 0;
				if (!EnableNoise) s_noise = 0;
				if (!EnableDMC) s_dmc = 0;
				*/

				//more properly correct
				float pulse_out, tnd_out;
				if (s_pulse0 == 0 && s_pulse1 == 0)
				  pulse_out = 0;
				else pulse_out = 95.88f / ((8128.0f / (s_pulse0 + s_pulse1)) + 100.0f);
				if (s_tri == 0 && s_noise == 0 && s_dmc == 0)
				  tnd_out = 0;
				else tnd_out = 159.79f / (1 / ((s_tri / 8227.0f) + (s_noise / 12241.0f /* * NOISEADJUST*/) + (s_dmc / 22638.0f)) + 100);
				float output = pulse_out + tnd_out;
				//output = output * 2 - 1;
				//this needs to leave enough headroom for straying DC bias due to the DMC unit getting stuck outputs. smb3 is bad about that. 
				int mix = (int)(20000 * output);


				dlist.Add(new Delta(sampleclock, mix - oldmix));
				oldmix = mix;
			}
			


			sampleclock++;
		}
	}
}
