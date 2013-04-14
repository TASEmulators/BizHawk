using System;
using System.Collections.Generic;

using BizHawk.Emulation.Sound;

//http://wiki.nesdev.com/w/index.php/APU_Mixer_Emulation
//http://wiki.nesdev.com/w/index.php/APU
//http://wiki.nesdev.com/w/index.php/APU_Pulse
//sequencer ref: http://wiki.nesdev.com/w/index.php/APU_Frame_Counter

//TODO - refactor length counter to be separate component

namespace BizHawk.Emulation.Consoles.Nintendo
{
	partial class NES
	{
		public class BisqAPU : ISoundProvider
		{
			class RegBitSetStore
			{
				public RegBitSetStore(int numUints)
				{
					data = new uint[numUints];
				}
				public uint[] data;
			}

			class RegBitSet
			{
				RegBitSetStore store;
				int mask;
				int bitno, nbits, dim, index;
				public RegBitSet(RegBitSetStore store, int bitno, int nbits, int dim = 1, int index = 0)
				{
					this.store = store;
					this.bitno = bitno;
					this.nbits = nbits;
					this.dim = dim;
					this.index = index;
					mask = ((1 << (nbits / 2)) << (nbits - (nbits / 2))) - 1;
				}

				public static implicit operator int(RegBitSet rhs) { return (int)rhs.get(); }
				public static implicit operator uint(RegBitSet rhs) { return rhs.get(); }
				public static explicit operator bool(RegBitSet rhs) { return rhs.get() != 0; }

				public uint Value { get { return get(); } set { set(value); } }

				public uint this[int offset]
				{
					get
					{
						return get(offset);
					}
					set
					{
						set(value, offset);
					}
				}

				public uint get(int offset = 0)
				{
					return (uint)((store.data[index + offset] >> bitno) & mask);
				}

				public void set(uint val, int offset = 0)
				{
					long temp = (store.data[index + offset] & ~(mask << bitno))
						| ((nbits > 1 ? val & mask : ((val != 0) ? 1 : 0)) << bitno);
					store.data[index + offset] = (uint)temp;
				}

				public static uint operator ^(RegBitSet lhs, uint rhs) { return ((uint)(lhs.get() ^ rhs)); }
				public static uint operator |(RegBitSet lhs, uint rhs) { return ((uint)(lhs.get() | rhs)); }
				public static uint operator &(RegBitSet lhs, uint rhs) { return ((uint)(lhs.get() & rhs)); }
				public static uint operator +(RegBitSet lhs, uint rhs) { return ((uint)(lhs.get() + rhs)); }
				public static uint operator -(RegBitSet lhs, uint rhs) { return ((uint)(lhs.get() - rhs)); }
			};

			class SignedRegBitSet
			{
				RegBitSetStore store;
				int mask;
				int bitno, nbits, dim, index;
				public SignedRegBitSet(RegBitSetStore store, int bitno, int nbits, int dim = 1, int index = 0)
				{
					this.store = store;
					this.bitno = bitno;
					this.nbits = nbits;
					this.dim = dim;
					this.index = index;
					mask = ((1 << (nbits / 2)) << (nbits - (nbits / 2))) - 1;
				}

				public static implicit operator int(SignedRegBitSet rhs) { return rhs.get(); }
				public static implicit operator uint(SignedRegBitSet rhs) { return (uint)rhs.get(); }
				public static explicit operator bool(SignedRegBitSet rhs) { return rhs.get() != 0; }

				public int Value { get { return get(); } set { set(value); } }

				public int this[int offset]
				{
					get
					{
						return get(offset);
					}
					set
					{
						set(value, offset);
					}
				}

				public int get(int offset = 0)
				{
					return (int)((store.data[index + offset] >> bitno) & mask);
				}

				public void set(int val, int offset = 0)
				{
					long temp = ((int)store.data[index + offset] & (int)~(mask << bitno))
						| ((nbits > 1 ? val & mask : ((val != 0) ? 1 : 0)) << bitno);
					store.data[index + offset] = (uint)temp;
				}

				public static int operator ^(SignedRegBitSet lhs, int rhs) { return ((int)(lhs.get() ^ rhs)); }
				public static int operator |(SignedRegBitSet lhs, int rhs) { return ((int)(lhs.get() | rhs)); }
				public static int operator &(SignedRegBitSet lhs, int rhs) { return ((int)(lhs.get() & rhs)); }
				public static int operator +(SignedRegBitSet lhs, int rhs) { return ((int)(lhs.get() + rhs)); }
				public static int operator -(SignedRegBitSet lhs, int rhs) { return ((int)(lhs.get() - rhs)); }

			};


			public static bool CFG_USE_METASPU = true;

			class APUdata
			{
				public APUdata()
				{
					bs = new RegBitSetStore(5);
					ChannelsEnabled = new RegBitSet(bs, 15, 1, 5, 0);
					frame_delay = new RegBitSet(bs, 0, 15, 5, 0);
					frame = new RegBitSet(bs, 0, 8, 5, 1);
					FiveCycleDivider = new RegBitSet(bs, 8, 1, 5, 1);
					IRQdisable = new RegBitSet(bs, 9, 1, 5, 1);
					DMC_CycleCost = new RegBitSet(bs, 10, 3, 5, 1);
					IRQ_delay = new RegBitSet(bs, 0, 15, 5, 2);
				}
				public RegBitSetStore bs;
				public RegBitSet ChannelsEnabled;
				public RegBitSet frame_delay;
				public RegBitSet frame;
				public RegBitSet FiveCycleDivider;
				public RegBitSet IRQdisable;
				public RegBitSet DMC_CycleCost;
				public RegBitSet IRQ_delay;
			}
			APUdata data = new APUdata();

			class APUchannel
			{
				public APUchannel()
				{
					bs = new RegBitSetStore(11);
					
					reg0 = new RegBitSet(bs,0, 8, 11, 0);
					DutyCycle = new RegBitSet(bs,6, 2, 11, 0);
					EnvDecayDisable = new RegBitSet(bs,4, 1, 11, 0);
					EnvDecayRate = new RegBitSet(bs,0, 4, 11, 0);
					EnvDecayLoopEnable = new RegBitSet(bs,5, 1, 11, 0);
					FixedVolume = new RegBitSet(bs,0, 4, 11, 0);
					LengthCounterDisable = new RegBitSet(bs,5, 1, 11, 0);
					LinearCounterInit = new RegBitSet(bs,0, 7, 11, 0);
					LinearCounterDisable = new RegBitSet(bs,7, 1, 11, 0);
					
					reg1 = new RegBitSet(bs,8, 8, 11, 0);
					SweepShift = new RegBitSet(bs,8, 3, 11, 0);
					SweepDecrease = new RegBitSet(bs,11, 1, 11, 0);
					SweepRate = new RegBitSet(bs,12, 3, 11, 0);
					SweepEnable = new RegBitSet(bs,15, 1, 11, 0);
					PCMlength = new RegBitSet(bs,8, 8, 11, 0);
					
					reg2 = new RegBitSet(bs,16, 8, 11, 0);
					NoiseFreq = new RegBitSet(bs,16, 4, 11, 0);
					NoiseType = new RegBitSet(bs,23, 1, 11, 0);
					WaveLength = new RegBitSet(bs,16, 11, 11, 0);
					
					reg3 = new RegBitSet(bs,24, 8, 11, 0);
					LengthCounterInit = new RegBitSet(bs,27, 5, 11, 0);
					LoopEnabled = new RegBitSet(bs,30, 1, 11, 0);
					IRQenable = new RegBitSet(bs,31, 1, 11, 0);
					
					length_counter = new SignedRegBitSet(bs,0, 32, 11, 1);
					linear_counter = new SignedRegBitSet(bs,0, 32, 11, 2);
					address = new SignedRegBitSet(bs,0, 32, 11, 3);
					envelope = new SignedRegBitSet(bs,0, 32, 11, 4);
					sweep_delay = new SignedRegBitSet(bs,0, 32, 11, 5);
					env_delay = new SignedRegBitSet(bs,0, 32, 11, 6);
					wave_counter = new SignedRegBitSet(bs,0, 32, 11, 7);
					hold = new SignedRegBitSet(bs,0, 32, 11, 8);
					phase = new SignedRegBitSet(bs,0, 32, 11, 9);
					level = new SignedRegBitSet(bs,0, 32, 11, 10);
				}
				public RegBitSetStore bs;
				// 4000, 4004, 400C, 4012:
				public RegBitSet reg0, DutyCycle, EnvDecayDisable, EnvDecayRate, EnvDecayLoopEnable, FixedVolume, LengthCounterDisable, LinearCounterInit, LinearCounterDisable;
				// 4001, 4005, 4013:  
				public RegBitSet reg1, SweepShift, SweepDecrease, SweepRate, SweepEnable, PCMlength;
				// 4002, 4006, 400A, 400E:            
				public RegBitSet reg2, NoiseFreq, NoiseType, WaveLength;
				// 4003, 4007, 400B, 400F, 4010:
				public RegBitSet reg3, LengthCounterInit, LoopEnabled, IRQenable;
				// Internals:
				public SignedRegBitSet length_counter, linear_counter, address, envelope, sweep_delay, env_delay, wave_counter, hold, phase, level;
			}

			APUchannel[] channels = new APUchannel[5] { new APUchannel(), new APUchannel(), new APUchannel(), new APUchannel(), new APUchannel() };

			static readonly byte[] LengthCounters = new byte[32]
			{ 10,254,20, 2,40, 4,80, 6,  160, 8,60,10,14,12,26,14,
				12, 16,24,18,48,20,96,22,  192,24,72,26,16,28,32,30 };

			static readonly ushort[] NoisePeriods = new ushort[16]
			{ 2,4,8,16,32,48,64,80,101,127,190,254,381,508,1017,2034 };

			static readonly ushort[] DMCperiods = new ushort[16]
			{ 428,380,340,320,  286,254,226,214,  190,160,142,128,  106,84,72,54 };
			/*
			For PAL:
			static const u16 DMCperiods[16] =
			{ 0x18E,0x162,0x13C,0x12A, 0x114,0x0EC,0x0D2,0x0C6,
				0x0B0,0x094,0x084,0x076, 0x062,0x04E,0x042,0x032 };
			*/

			const int frame_period = 7458;

			// Utility function for sound
			bool count(ref int v, int reset)
			{
				if (--v < 0)
				{
					v = reset;
					return true;
				}
				else return false;
			}

			bool count(ref int v, int reset, int n_at_time)
			{
				if ((v -= n_at_time) < 0)
				{
					v += reset;
					return true;
				}
				else return false;
			}


			int tick(int c)
			{
				APUchannel ch = channels[c];
				int wl = ch.WaveLength;
				if (c != 4) ++wl;
				if (c < 2) wl *= 2;

				if (c == 3) wl = NoisePeriods[ch.NoiseFreq];

				//if(c != 4) wl = wl * (IO::UISpeed);
				// ^ Match to the UI speed (but don't for DPCM, because it would skew the timings)

				int volume = (bool)ch.length_counter ? (bool)ch.EnvDecayDisable ? (int)ch.FixedVolume : ch.envelope : 0;
				// Sample may change at wavelen intervals.
				int ref_S = ch.level;
				int ref_ch_wave_counter = ch.wave_counter;
				if (!(data.ChannelsEnabled[c] != 0)
				|| !count(ref ref_ch_wave_counter, wl))
				{
					ch.wave_counter.Value = ref_ch_wave_counter;
					return ref_S;
				}
				ch.wave_counter.Value = ref_ch_wave_counter;
				switch (c)
				{
					case 0:
					default:
					case 1: // Square wave. With four different 8-step binary waveforms (32 bits of data total).
						ch.phase.Value++;
						if (wl < 8) return ref_S;
						if ((bool)ch.SweepEnable && !(bool)ch.SweepDecrease)
							if (wl + (wl >> ch.SweepShift) >= 0x800)
								return ref_S;
						return ref_S = ch.level.Value = (0xF33C0C04u & (1u << (ch.phase % 8 + ch.DutyCycle * 8))) != 0 ? volume : 0;
					case 2: // Triangle wave
						if ((bool)ch.length_counter && (bool)ch.linear_counter && wl >= 3) ++ch.phase.Value;
						return ref_S = ch.level.Value = (ch.phase & 15) ^ (((ch.phase & 16) != 0) ? 15 : 0);

					case 3: // Noise: Linear feedback shift register
						if (!(bool)ch.hold) ch.hold.Value = 1;
						ch.hold.Value = (ch.hold >> 1)
									| (((ch.hold ^ (ch.hold >> ((bool)ch.NoiseType ? 6 : 1))) & 1) << 14);
						return ref_S = ch.level.Value = ((ch.hold & 1) != 0) ? 0 : volume;

					case 4: // Delta modulation channel (DMC)
						// hold           = 8 bit value
						// phase          = number of bits buffered
						// length_counter = 
						if (wl == 0) return ref_S;
						if (ch.phase == 0) // Nothing in sample buffer?
						{
							if (!(bool)ch.length_counter && (bool)ch.LoopEnabled) // Loop?
							{
								ch.length_counter.Value = ch.PCMlength * 16 + 1;
								ch.address.Value = (int)((ch.reg0 | 0x300) << 6);
							}
							if (ch.length_counter > 0) // Load next 8 bits if available
							{
								//==========================TODO============== DMC COST=====================
								//for(unsigned t = data.DMC_CycleCost; t > 1; --t)
								//      CPU::RB(u16(ch.address) | 0x8000); // timing
								ch.hold.Value = nes.ReadMemory((ushort)((ch.address.Value++) | 0x8000)); // Fetch byte
								ch.phase.Value = 8;
								--ch.length_counter.Value;
							}
							else // Otherwise, disable channel or issue IRQ
							{
								if ((bool)ch.IRQenable)
								{
									//CPU::reg.APU_DMC_IRQ = true;
									dmc_irq = true;
									SyncIRQ();
								}

								data.ChannelsEnabled[4] = 0;
							}
						}
						if (ch.phase != 0) // Update the signal if sample buffer nonempty
						{
							int v = ch.linear_counter;
							if (((ch.hold << --ch.phase.Value) & 0x80)!=0) v += 2; else v -= 2;
							if (v >= 0 && v <= 0x7F) ch.linear_counter.Value = v;
						}
						return ref_S = ch.level = ch.linear_counter;
				}
			}

			bool dmc_irq;
			bool sequencer_irq;
			public bool irq_pending;
			void SyncIRQ()
			{
				irq_pending = sequencer_irq | dmc_irq;
			}

			NES nes;
			public BisqAPU(NES nes)
			{
				this.nes = nes;
			}

			public void RunOne()
			{
				if (data.IRQ_delay != 0x7FFF)
				{
					if (data.IRQ_delay > 0) --data.IRQ_delay.Value;
					else { sequencer_irq = true; SyncIRQ(); data.IRQ_delay.Value = 0x7FFF; }
				}
				if (data.frame_delay > 0)
					--data.frame_delay.Value;
				else
				{
					bool Do240 = true, Do120 = false;
					data.frame_delay.Value += frame_period;
					switch (data.frame.Value++)
					{
						case 0:
							if (!(bool)data.IRQdisable && !(bool)data.FiveCycleDivider)
								data.IRQ_delay.Value = frame_period * 4 + 2;
							// passthru
							goto case 2;
						case 2:
							Do120 = true;
							break;
						case 1:
							data.frame_delay.Value -= 2;
							break;
						case 3:
							data.frame.Value = 0;
							if ((bool)data.FiveCycleDivider)
								data.frame_delay.Value += frame_period - 6;
							break;
					}
					// Some events are invoked at 96 Hz or 120 Hz rate. Others, 192 Hz or 240 Hz.
					for (int c = 0; c < 4; ++c)
					{
						APUchannel ch = channels[c];
						int wl = ch.WaveLength;

						// 96/120 Hz events:
						if (Do120)
						{
							// Length tick (all channels except DMC, but different disable bit for triangle wave)
							if ((bool)ch.length_counter
							&& !(c == 2 ? (bool)ch.LinearCounterDisable : (bool)ch.LengthCounterDisable))
								ch.length_counter.Value -= 1;

							// Sweep tick (square waves only)
							int ref_ch_sweep_delay = ch.sweep_delay;
							if (c < 2 && count(ref ref_ch_sweep_delay, ch.SweepRate))
								if (wl >= 8 && (bool)ch.SweepEnable && (bool)ch.SweepShift)
								{
									int s = wl >> ch.SweepShift;
									wl += ((bool)ch.SweepDecrease ? ((c != 0) ? -s : ~s) : s);
									if (wl < 0x800) ch.WaveLength.Value = (uint)wl;
								}

							ch.sweep_delay.Value = ref_ch_sweep_delay;
						}

						// 240/192 Hz events:
						if (Do240)
						{
							// Linear tick (triangle wave only) (all ticks)
							if (c == 2)
								ch.linear_counter.Value = (bool)ch.LinearCounterDisable
								? ch.LinearCounterInit
								: (ch.linear_counter > 0 ? ch.linear_counter - 1 : 0);

							// Envelope tick (square and noise channels) (all ticks)
							int ref_ch_env_delay = ch.env_delay;
							if (c != 2 && count(ref ref_ch_env_delay, ch.EnvDecayRate))
								if (ch.envelope > 0 || (bool)ch.EnvDecayLoopEnable)
									ch.envelope.Value = (ch.envelope - 1) & 15;
							ch.env_delay.Value = ref_ch_env_delay;
						}

					}
				}

       // Mix the audio: Get the momentary sample from each channel and mix them.
       // #define s(c) tick<c>()
        //v = [](float m,float n, float d) { return n!=0.f ? m/n : d; };
				Func<float,float,float,float> v = (float m,float n, float d) => ( n!=0.0f ? m/n : d );

        short sample = (short)(30000 *
          (
           // Square 0 and 1
           v(95.88f,  (100.0f + v(8128.0f, tick(0) + tick(1), -100.0f)), 0.0f)
           // Triangle, noise, DMC
        +  v(159.79f, (100.0f + v(1.0f, tick(2)/8227.0f + tick(3)/12241.0f + tick(4)/22638.0f, -1000.0f)), 0.0f)
           // GamePak audio (these volume values are bogus, but sound acceptable)
        +  v(95.88f,  (100.0f + v(32512.0f, /*GamePak::ExtAudio()*/ 0, -100.0f)), 0.0f)
          - 0.5f
          ));

				EmitSample(sample);

				////this (and the similar line below) is a crude hack
				////we should be generating logic to suppress the $4015 clear when the assert signal is set instead
				////be sure to test "apu_test" if you mess with this
				//sequencer_irq |= sequencer_irq_assert;
			}

			void WriteC(int chno, int index, byte value)
			{
				APUchannel ch = channels[chno];
				switch (index)
				{
					case 0:
						if ((bool)ch.LinearCounterDisable) ch.linear_counter.Value = value & 0x7F;
						ch.reg0.Value = value;
						break;
					case 1:
						ch.reg1.Value = value;
						ch.sweep_delay.Value = ch.SweepRate;
						break;
					case 2:
						ch.reg2.Value = value;
						break;
					case 3:
						ch.reg3.Value = value;
						if (data.ChannelsEnabled[chno]!=0)
							ch.length_counter.Value = LengthCounters[ch.LengthCounterInit];
						ch.linear_counter.Value = ch.LinearCounterInit;
						ch.env_delay.Value = ch.EnvDecayRate;
						ch.envelope.Value = 15;
						if (index < 8) ch.phase.Value = 0;
						break;
					case 0x12:
						ch.reg0.Value = value;
						ch.address.Value = ((int)ch.reg0 | 0x300) << 6;
						break;
					case 0x10:
						ch.reg3.Value = value;
						ch.WaveLength.Value = (uint)(DMCperiods[value & 0x0F] - 1);
						if (!(bool)ch.IRQenable) { dmc_irq = false; SyncIRQ(); }
						break;
					case 0x13: // sample length
						ch.reg1.Value = value;
						if (ch.length_counter == 0)
							ch.length_counter.Value = ch.PCMlength * 16 + 1;
						break;
					case 0x11: // dac value
						ch.linear_counter.Value = value & 0x7F;
						break;
					case 0x15:
						for (int c = 0; c < 5; ++c)
							data.ChannelsEnabled[c] = (uint)((value >> c) & 1); //noteworthy tweak
						for (int c = 0; c < 5; ++c)
							if (data.ChannelsEnabled[c]==0)
								channels[c].length_counter.Value = 0;
							else if (c == 4 && channels[c].length_counter == 0)
							{
								APUchannel chh = channels[c];
								chh.length_counter.Value = chh.PCMlength * 16 + 1;
								chh.address.Value = ((int)chh.reg0 | 0x300) << 6;
								chh.phase.Value = 0;
							}
						//CPU::reg.APU_DMC_IRQ = false;
						dmc_irq = false; SyncIRQ();
						break;
					case 0x17:
						data.IRQdisable.Value = (uint)(value & 0x40);
						data.FiveCycleDivider.Value = (uint)(value & 0x80);
						// apu_test 1-len_ctr: Writing $80 to $4017 should clock length immediately
						//                 But Writing $00 to $4017 shouldn't clock length immediately
						data.frame_delay.Value &= 1;
						data.frame.Value = 0;
						data.IRQ_delay.Value = 0x7FFF;
						if ((bool)data.IRQdisable) { sequencer_irq = false; SyncIRQ(); }
						if (!(bool)data.FiveCycleDivider)
						{
							data.frame.Value = 1;
							data.frame_delay.Value += frame_period;

							if (!(bool)data.IRQdisable)
							{
								data.IRQ_delay.Value = data.frame_delay + frame_period * 3 + 1 - 3;
								// ^ "- 3" makes apu_test "4-jitter" not complain
								//   that "Frame irq is set too late"
							}
						}
						break;
				}
			}

			public byte ReadReg(int addr)
			{
				byte res = 0;
				for (int c = 0; c < 5; ++c) res |= (byte)(((bool)channels[c].length_counter ? 1 << c : 0));
				if (sequencer_irq) res |= 0x40; sequencer_irq = false; SyncIRQ();
				if (dmc_irq) res |= 0x80;
				return res;
			}

			public void WriteReg(int addr, byte val)
			{
				int index = addr - 0x4000;
				WriteC((index / 4) % 5, index < 0x10 ? index % 4 : index, val);
			}

			public void NESSoftReset()
			{
			}

			public void DiscardSamples()
			{
				metaspu.buffer.clear();
			}

			public void SyncState(Serializer ser)
			{
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
				const double kMixRate = 44100.0 / 1789772.0;
				const double kInvMixRate = (1 / kMixRate);
				timer += kMixRate;
				accumulate += samp;
				if (timer <= 1)
					return;

				accumulate -= samp;
				timer -= 1;
				double ratio = (timer / kMixRate);
				double fractional = (this_samp - last_hwsamp) * ratio;
				double factional_remainder = (this_samp - last_hwsamp) * (1 - ratio);
				accumulate += fractional;

				accumulate *= 436; //32768/(15*4) -- adjust later for other sound channels
				int outsamp = (int)(accumulate / kInvMixRate);
				if (CFG_USE_METASPU)
					metaspu.buffer.enqueue_sample((short)outsamp, (short)outsamp);
				else squeue.Enqueue(outsamp);
				accumulate = factional_remainder;

				last_hwsamp = this_samp;
			}

			MetaspuSoundProvider metaspu = new MetaspuSoundProvider(ESynchMethod.ESynchMethod_V);
			public int MaxVolume { get; set; } // not supported

			void ISoundProvider.GetSamples(short[] samples)
			{
				if (CFG_USE_METASPU)
				{
					metaspu.GetSamples(samples);
					//foreach(short sample in samples) bw.Write((short)sample);
				}
				else
					MyGetSamples(samples);

				//mix in the cart's extra sound circuit
				nes.board.ApplyCustomAudio(samples);
			}

			//static BinaryWriter bw = new BinaryWriter(new FileStream("d:\\out.raw",FileMode.Create,FileAccess.Write,FileShare.Read));
			void MyGetSamples(short[] samples)
			{
				//Console.WriteLine("a: {0} with todo: {1}",squeue.Count,samples.Length/2);

				for (int i = 0; i < samples.Length / 2; i++)
				{
					int samp = 0;
					if (squeue.Count != 0)
						samp = squeue.Dequeue();

					samples[i * 2 + 0] = (short)(samp);
					samples[i * 2 + 1] = (short)(samp);
					//bw.Write((short)samp);
				}
			}

		} //class BisqAPU


	}

}