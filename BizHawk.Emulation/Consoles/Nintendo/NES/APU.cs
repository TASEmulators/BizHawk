using System;
using System.Collections.Generic;

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


			class PulseUnit
			{
				public PulseUnit(int unit) { this.unit = unit; }
				public int unit; 

				//reg0
				int duty, length_halt, envelope_constant, envelope_cnt_value;
				//reg1
				int sweep_en, timer_period, negate, shiftcount;
				//reg2/3
				int len_cnt;
				int timer_raw_reload_value, timer_reload_value;

				//from other apu regs
				public int lenctr_en;

				public void WriteReg(int addr, byte val)
				{
					//Console.WriteLine("write pulse {0:X} {1:X}", addr, val);
					switch(addr)
					{
						case 0:
							envelope_cnt_value = val & 0xF;
							envelope_constant = (val >> 4) & 1;
							length_halt = (val >> 5) & 1;
							duty = (val >> 6) & 3;
							break;
						case 1:
							shiftcount = val & 7;
							negate = (val >> 3) & 1;
							timer_period = (val >> 4) & 7;
							sweep_en = (val >> 7) & 1;
							break;
						case 2:
							timer_reload_value = (timer_reload_value & ~0xFF) | val;
							timer_raw_reload_value = timer_reload_value;
							calc_sweep_unit();
							break;
						case 3:
							len_cnt = LENGTH_TABLE[(val >> 3) & 0x1F];
							timer_reload_value = (timer_reload_value & 0xFF) | ((val & 0x07) << 8);
							timer_raw_reload_value = timer_reload_value;
							sq_seq = 0;
							calc_sweep_unit();
							//serves as a useful note-on diagnostic
							Console.WriteLine("{0} timer_reload_value: {1}", unit, timer_reload_value);
							break;
					}
				}

				int swp_val_result;
				bool swp_silence;
				int sq_seq;
				public int sample;
				int envelope_value;

				void calc_sweep_unit()
				{
					//1's complement for chan 0, 2's complement if chan 1
					if (negate == 1) //check to see if negate is on
						swp_val_result = ~swp_val_result + unit;
					//add with the shifter chan
					swp_val_result += timer_reload_value;

					if ((timer_reload_value < 8) ||
						 ((swp_val_result > 0x7FF) && (negate==0)))
						swp_silence = true; //silence
					else
						swp_silence = false; //don't silence

				}

				public void clock_sweep()
				{
					//(as well as length counter)
					if(sweep_en==1)
						timer_raw_reload_value = swp_val_result & 0x7FF;
				}

				public void clock_env()
				{
				}

				public void Run()
				{
					envelope_value = 15; //HAX

					//sweep units are figured out during memory writes to the regs
					//that set the timer, length counter are figured out in the
					//writes and frame counter, and envelope is set through the memory
					//regs also, so we just need to deal with the timer and sequencer here

					if (--timer_period==0)
					{
						sq_seq = (sq_seq + 1) & 7;
						//reload timer
						timer_period = timer_raw_reload_value + 2;
					}
					if (PULSE_DUTY[duty,sq_seq] == 1) //we are outputting something
					{
						if (envelope_constant==1)
							sample = envelope_cnt_value;
						else
							sample = envelope_value;

						if (swp_silence)
							sample = 0;

						if (len_cnt==0) //length counter is 0
							sample = 0; //silenced
					}
					else
						sample = 0; //duty cycle is 0, silenced.

				}

			}
			PulseUnit[] pulse = { new PulseUnit(0), new PulseUnit(1) };


			int sequencer_counter, sequencer_step, sequencer_mode, sequencer_irq_inhibit, sequencer_irq_flag;
			void sequencer_reset()
			{
				sequencer_counter = 0;
				sequencer_step = 0;
				sequencer_check();
			}
			void sequencer_tick()
			{
				sequencer_counter++;
				//this figure is not valid for PAL. it must be recalculated
				if (sequencer_counter != 89490) return;
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
						if (sequencer_step == 1 || sequencer_step == 3)
						{
							pulse[0].clock_sweep();
							pulse[1].clock_sweep();
						}
						if (sequencer_step == 3)
						{
							if (sequencer_irq_inhibit == 0)
							{
								sequencer_irq_flag = 1;
								//TODO - actually fire IRQ
							}
							sequencer_step = 0;
						}
						break;
					case 1: //5-step
						if (sequencer_step != 4)
						{
							pulse[0].clock_env();
							pulse[1].clock_env();
						}
						if (sequencer_step == 0 || sequencer_step == 2)
						{
							pulse[0].clock_sweep();
							pulse[1].clock_sweep();
						}
						if (sequencer_step == 4)
							sequencer_step = 0;
						break;
				}
			}


			public void WriteReg(int addr, byte val)
			{
				if (addr >= 0x4000 && addr <= 0x4003)
					pulse[0].WriteReg(addr - 0x4000, val);
				if (addr >= 0x4004 && addr <= 0x4007)
					pulse[1].WriteReg(addr - 0x4004, val);
				switch (addr)
				{
					case 0x4015:
						pulse[0].lenctr_en = (val & 1);
						pulse[1].lenctr_en = ((val>>1) & 1);
						break;
					case 0x4017:
						sequencer_mode = (val>>7)&1;
						if(((val>>6)&1)==1)
							sequencer_irq_inhibit = 0;
						sequencer_reset();
						break;
				}
			}

			public byte ReadReg(int addr)
			{
				switch (addr)
				{
					default:
						return 0x00;
				}
			}

			public void Run(int cycles)
			{
				for (int i = 0; i < cycles; i++)
					RunOne();
			}

			void RunOne()
			{
				pulse[0].Run();
				pulse[1].Run();
				sequencer_tick();

				int mix = pulse[0].sample;
				mix += pulse[1].sample;

				EmitSample(mix);

				//since the units run concurrently, the APU frame sequencer 
				//is ran last because
				//it can change the ouput values of the pulse/triangle channels, 
				//we want the
				//changes to affect it on the *next* cycle.
			}

			int accumulate;
			double timer;
			Queue<int> squeue = new Queue<int>();
			void EmitSample(int samp)
			{
				const double kMixRate = 44100.0/1789772.0;
				const double kInvMixRate = (1 / kMixRate);
				timer += kMixRate;
				for(;;)
				{
					if (timer <= 1)
					{
						accumulate += samp;
						break;
					}
					else
					{
						timer -= 1;
						int outsamp = (int)(accumulate / kInvMixRate);
						squeue.Enqueue(outsamp);
						accumulate -= (int)(outsamp*kInvMixRate);
					}
				}
			}

			void ISoundProvider.GetSamples(short[] samples)
			{
				//Console.WriteLine("a: {0} with todo: {1}",squeue.Count,samples.Length/2);

				for (int i = 0; i < samples.Length/2; i++)
				{
					int samp = 0;
					if (squeue.Count != 0)
						samp = squeue.Dequeue();
					
					//if(samp != 0) Console.WriteLine("samp: {0}", samp);
					samples[i*2+0] = (short)(samp * 256);
					samples[i*2+1] = (short)(samp * 256);
				}
			}

		} //class APU


	}

}