using System;
using System.IO;
using System.Collections.Generic;

using BizHawk.Emulation.Sound;

//http://wiki.nesdev.com/w/index.php/APU_Mixer_Emulation
//http://wiki.nesdev.com/w/index.php/APU
//http://wiki.nesdev.com/w/index.php/APU_Pulse
//sequencer ref: http://wiki.nesdev.com/w/index.php/APU_Frame_Counter

namespace BizHawk.Emulation.Consoles.Nintendo
{

	partial class NES
	{
		public class APU : ISoundProvider
		{
			public static bool CFG_USE_METASPU = true;
			public static bool CFG_DECLICK = true;

			NES nes;
			public APU(NES nes)
			{
				this.nes = nes;
			}

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
			static int[] NOISE_TABLE = 
			{
				4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068 //NTSC
				//4, 7, 14, 30, 60, 88, 118, 148, 188, 236, 354, 472, 708,  944, 1890, 3778 //PAL
			};

	
			class PulseUnit
			{
				public PulseUnit(int unit) { this.unit = unit; }
				public int unit; 

				//reg0
				int duty_cnt, env_loop, env_constant, env_cnt_value;
				//reg1
				int sweep_en, sweep_divider_cnt, sweep_negate, sweep_shiftcount;
				bool sweep_reload;
				//reg2/3
				int len_cnt;
				int timer_raw_reload_value, timer_reload_value;

				//misc..
				int lenctr_en;

				public bool IsLenCntNonZero() { return len_cnt > 0; }

				public void WriteReg(int addr, byte val)
				{
					//Console.WriteLine("write pulse {0:X} {1:X}", addr, val);
					switch(addr)
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
							timer_reload_value = (timer_reload_value & ~0xFF) | val;
							timer_raw_reload_value = timer_reload_value * 2 + 2;
							//if (unit == 1) Console.WriteLine("{0} timer_reload_value: {1}", unit, timer_reload_value);
							break;
						case 3:
							len_cnt = LENGTH_TABLE[(val >> 3) & 0x1F];
							timer_reload_value = (timer_reload_value & 0xFF) | ((val & 0x07) << 8);
							timer_raw_reload_value = timer_reload_value * 2 + 2;
							//duty_step = 0; //?just a guess?
							timer_counter = timer_raw_reload_value;
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

				int swp_divider_counter;
				bool swp_silence;
				int duty_step;
				int timer_counter;
				public int sample;

				int env_start_flag, env_divider, env_counter, env_output;

				public void clock_length_and_sweep()
				{
					//this should be optimized to update only when `timer_reload_value` changes
					int sweep_shifter = timer_reload_value >> sweep_shiftcount;
					if (sweep_negate == 1)
						sweep_shifter = ~sweep_shifter + unit;
					sweep_shifter += timer_reload_value;

					//this sweep logic is always enabled:
					swp_silence = (timer_reload_value < 8 || (sweep_shifter > 0x7FF && sweep_negate == 0));

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
								timer_raw_reload_value = timer_reload_value * 2 + 2;
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
					if (env_loop == 0 && len_cnt > 0)
						len_cnt--;
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
						if(env_divider != 0) env_divider--;
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
					if (env_constant == 1)
						env_output = env_cnt_value;
					else env_output = env_counter;
				}

				public void Run()
				{
					if (timer_counter > 0) timer_counter--;
					if (timer_counter == 0)
					{
						duty_step = (duty_step + 1) & 7;
						//reload timer
						timer_counter = timer_raw_reload_value;
					}
					if (PULSE_DUTY[duty_cnt, duty_step] == 1) //we are outputting something
					{
						sample = env_output;

						if (swp_silence)
							sample = 0;

						if (len_cnt==0) //length counter is 0
						    sample = 0; //silenced
					}
					else
						sample = 0; //duty cycle is 0, silenced.

				}
			}

			class NoiseUnit
			{
				//reg0 (sweep)
				int env_cnt_value, env_loop, env_constant;

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

				public bool IsLenCntNonZero() { return len_cnt > 0; }

				public void WriteReg(int addr, byte val)
				{
					switch (addr)
					{
						case 0:
							env_cnt_value = val & 0xF;
							env_constant = (val >> 4) & 1;
							env_loop = (val>>5)&1;
							break;
						case 1:
							break;
						case 2:
							period_cnt = NOISE_TABLE[val & 0xF];
							mode_cnt = (val>>7)&1;
							//Console.WriteLine("noise period: {0}, vol: {1}", (val & 0xF), env_cnt_value);
							break;
						case 3:
							len_cnt = LENGTH_TABLE[(val >> 3) & 0x1F];
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

				public void clock_env() {}
				public void clock_length_and_sweep()
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
						if (env_constant == 1)
							env_output = env_cnt_value;
						else env_output = env_counter;
					}

					if (len_cnt > 0 && env_loop == 0)
						len_cnt--;
				}

				public void Run()
				{
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
						noise_bit = (shift_register & 1)!=0;
					}

					if (noise_bit || len_cnt==0) sample = 0;
					else
						sample = env_output;
				}
			}

			class TriangleUnit
			{
				//reg0
				int linear_counter_reload, control_flag;
				//reg1 (n/a)
				//reg2/3
				int timer_cnt, length_counter_load, halt_flag;

				public void WriteReg(int addr, byte val)
				{
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
							length_counter_load = (val>>3)&0x1F;
							halt_flag = 1;
							break;
					}
					//Console.WriteLine("tri timer_reload_value: {0}", timer_cnt_reload);
				}

				int linear_counter, timer, timer_cnt_reload;
				int seq;
				public int sample;

				public void Run()
				{
					//when clocked by timer
					//seq steps forward
					//except when linear counter or
					//length counter is 0

					bool en = length_counter_load != 0 && linear_counter != 0;

					//length counter and linear counter 
					//is clocked in frame counter.
					if (en)
					{
						if(timer>0) timer--;
						if (timer == 0)
						{
							seq = (seq + 1) & 0x1F;
							timer = timer_cnt_reload;
						}
						//if(CFG_DECLICK)
						//    sample = TRIANGLE_TABLE[(seq+8)&0x1F];
						//else
							sample = TRIANGLE_TABLE[seq];
					}
				}

				
				public void clock_length_and_sweep()
				{
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
					if (control_flag == 0)
					{
						halt_flag = 0;
					}
				}
			}

			PulseUnit[] pulse = { new PulseUnit(0), new PulseUnit(1) };
			TriangleUnit triangle = new TriangleUnit();
			NoiseUnit noise = new NoiseUnit();

			int sequencer_counter, sequencer_step, sequencer_mode, sequencer_irq_inhibit;
			void sequencer_reset()
			{
				sequencer_counter = 0;
				sequencer_step = 1;
				if(sequencer_mode == 1) sequencer_check();
			}

			//21477272 master clock
			//1789772 cpu clock (master / 12)
			//240 apu clock (master / 89490) = (cpu / 7457)
			void sequencer_tick()
			{
				sequencer_counter++;
				//this figure is not valid for PAL. it must be recalculated
				if (sequencer_counter != 7457) return;
				sequencer_counter = 0;
				sequencer_step++;
				sequencer_check();
			}

			void sequencer_check()
			{
				switch (sequencer_mode)
				{
					case 0: //4-step
						pulse[0].clock_env();
						pulse[1].clock_env();
						triangle.clock_linear_counter();
						noise.clock_env();
						if (sequencer_step == 2 || sequencer_step == 4)
						{
							pulse[0].clock_length_and_sweep();
							pulse[1].clock_length_and_sweep();
							triangle.clock_length_and_sweep();
							noise.clock_length_and_sweep();
						}
						if (sequencer_step == 4)
						{
							if (sequencer_irq_inhibit == 0)
							{
								nes.irq_apu = true;
							}
							sequencer_step = 0;
						}
						break;
					case 1: //5-step
						if (sequencer_step != 5)
						{
							pulse[0].clock_env();
							pulse[1].clock_env();
							triangle.clock_linear_counter();
							noise.clock_env();
						}
						if (sequencer_step == 1 || sequencer_step == 3)
						{
							pulse[0].clock_length_and_sweep();
							pulse[1].clock_length_and_sweep();
							triangle.clock_length_and_sweep();
							noise.clock_length_and_sweep();
						}
						if (sequencer_step == 5)
							sequencer_step = 0;
						break;
				}
			}


			public void WriteReg(int addr, byte val)
			{
				switch (addr)
				{
					case 0x4000: case 0x4001: case 0x4002: case 0x4003:
						pulse[0].WriteReg(addr - 0x4000, val);
						break;
					case 0x4004: case 0x4005: case 0x4006: case 0x4007:
						pulse[1].WriteReg(addr - 0x4004, val);
						break;
					case 0x4008: case 0x4009: case 0x400A: case 0x400B:
						triangle.WriteReg(addr - 0x4008, val);
						break;
					case 0x400C: case 0x400D: case 0x400E: case 0x400F:
						noise.WriteReg(addr - 0x400C, val);
						break;
					case 0x4015:
						pulse[0].set_lenctr_en(val & 1);
						pulse[1].set_lenctr_en((val >> 1) & 1);
						//todo - triangle length counter?
						noise.set_lenctr_en((val >> 3) & 1);
						break;
					case 0x4017:
						sequencer_mode = (val>>7)&1;
						sequencer_irq_inhibit = (val >> 6) & 1;
						if (sequencer_irq_inhibit == 1)
							nes.irq_apu = false;
						sequencer_reset();
						break;
				}
			}

			public byte ReadReg(int addr)
			{
				switch (addr)
				{
					case 0x4015:
					{
						//notice a missing bit here. should properly emulate with empty bus
						//if an interrupt flag was set at the same moment of the read, it will read back as 1 but it will not be cleared. 
						int dmc_irq_flag = 0; //todo
						int dmc_nonzero = 0; //todo
						int noise_nonzero = noise.IsLenCntNonZero() ? 1 : 0;
						int tri_nonzero = 0; //todo
						int pulse1_nonzero = pulse[1].IsLenCntNonZero() ? 1 : 0;
						int pulse0_nonzero = pulse[0].IsLenCntNonZero() ? 1 : 0;
						int ret = (dmc_irq_flag << 7) | ((nes.irq_apu?1:0) << 6) | (dmc_nonzero << 4) | (noise_nonzero << 3) | (tri_nonzero<<2) | (pulse1_nonzero<<1) | (pulse0_nonzero);
						nes.irq_apu = false;
						return (byte)ret;
					}
					default:
						return 0x00;
				}
			}

			public void Run(int cycles)
			{
				for (int i = 0; i < cycles; i++)
					RunOne();
			}

			public void DiscardSamples()
			{
				metaspu.buffer.clear();
			}

			public void RunOne()
			{
				pulse[0].Run();
				pulse[1].Run();
				triangle.Run();
				noise.Run();

				int mix = 0;
				mix += pulse[0].sample;
				mix += pulse[1].sample;
				mix += triangle.sample;
				mix += noise.sample;

				EmitSample(mix);

				sequencer_tick();

				//since the units run concurrently, the APU frame sequencer 
				//is ran last because
				//it can change the ouput values of the pulse/triangle channels, 
				//we want the
				//changes to affect it on the *next* cycle.
			}

			double accumulate;
			double timer;
			Queue<int> squeue = new Queue<int>();
			int last_hwsamp;
			int panic_sample, panic_count;
			void EmitSample(int samp)
			{
				//kill the annoying hum that is a consequence of the shitty code below
				if (samp == panic_sample)
					panic_count++;
				else panic_count = 0;
				if (panic_count > 178977)
					samp = 0;
				else
					panic_sample = samp;
				
				int this_samp = samp;
				const double kMixRate = 44100.0/1789772.0;
				const double kInvMixRate = (1 / kMixRate);
				timer += kMixRate;
				accumulate += samp;
				if (timer <= 1)
					return;

				accumulate -= samp;
				timer -= 1;
				double ratio = (timer / kMixRate);
				double fractional = (this_samp - last_hwsamp) * ratio;
				double factional_remainder = (this_samp - last_hwsamp) * (1-ratio);
				accumulate += fractional;

				accumulate *= 540; //32768/(15*4) -- adjust later for other sound channels
				int outsamp = (int)(accumulate / kInvMixRate);
				if (CFG_USE_METASPU)
					metaspu.buffer.enqueue_sample((short)outsamp, (short)outsamp);
				else squeue.Enqueue(outsamp);
				accumulate = factional_remainder;

				last_hwsamp = this_samp;
			}

			MetaspuSoundProvider metaspu = new MetaspuSoundProvider(ESynchMethod.ESynchMethod_Z);

			void ISoundProvider.GetSamples(short[] samples)
			{
				if (CFG_USE_METASPU)
				{
					metaspu.GetSamples(samples);
					foreach(short sample in samples) bw.Write((short)sample);
				}
				else
					MyGetSamples(samples);
			}

			static BinaryWriter bw = new BinaryWriter(File.OpenWrite("d:\\out.raw"));
			void MyGetSamples(short[] samples)
			{
				//Console.WriteLine("a: {0} with todo: {1}",squeue.Count,samples.Length/2);

				for (int i = 0; i < samples.Length/2; i++)
				{
					int samp = 0;
					if (squeue.Count != 0)
						samp = squeue.Dequeue();
					
					samples[i*2+0] = (short)(samp);
					samples[i*2+1] = (short)(samp);
					//bw.Write((short)samp);
				}
			}

		} //class APU


	}

}