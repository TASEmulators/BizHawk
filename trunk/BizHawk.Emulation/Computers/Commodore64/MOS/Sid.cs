using System;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	sealed public partial class Sid
	{
		// ------------------------------------

		sealed class Envelope
		{
			const int stateAttack = 0;
			const int stateDecay = 1;
			const int stateRelease = 2;

			int attack;
			int decay;
			bool delay;
			int envCounter;
			int expCounter;
			int expPeriod;
			bool freeze;
			int lfsr;
			bool gate;
			int rate;
			int release;
			int state;
			int sustain;

			static int[] adsrTable = new int[]
			{
				0x7F00, 0x0006, 0x003C, 0x0330,
				0x20C0, 0x6755, 0x3800, 0x500E,
				0x1212, 0x0222, 0x1848, 0x59B8,
				0x3840, 0x77E2, 0x7625, 0x0A93
			};

			static int[] expCounterTable = new int[]
			{
				0xFF, 0x5D, 0x36, 0x1A, 0x0E, 0x06, 0x00
			};

			static int[] expPeriodTable = new int[]
			{
				0x01, 0x02, 0x04, 0x08, 0x10, 0x1E, 0x01
			};

			static int[] sustainTable = new int[]
			{
				0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
				0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
			};

			public Envelope()
			{
				HardReset();
			}

			public void ExecutePhase2()
			{
				
				{
					if (!delay)
					{
						envCounter--;
						delay = true;
						UpdateExpCounter();
					}

					if (lfsr != rate)
					{
						int feedback = ((lfsr >> 14) ^ (lfsr >> 13)) & 0x1;
						lfsr = ((lfsr << 1) & 0x7FFF) | feedback;
						return;
					}
					lfsr = 0x7FFF;

					if (state == stateAttack || ++expCounter == expPeriod)
					{
						expCounter = 0;
						if (freeze)
							return;

						switch (state)
						{
							case stateAttack:
								envCounter++;
								if (envCounter == 0xFF)
								{
									state = stateDecay;
									rate = adsrTable[decay];
								}
								break;
							case stateDecay:
								if (envCounter == sustainTable[sustain])
								{
									return;
								}
								if (expPeriod != 1)
								{
									delay = false;
									return;
								}
								envCounter--;
								break;
							case stateRelease:
								if (expPeriod != 1)
								{
									delay = false;
									return;
								}
								envCounter--;
								break;
						}
						envCounter &= 0xFF;
						UpdateExpCounter();
					}
				}
			}

			public void HardReset()
			{
				attack = 0;
				decay = 0;
				delay = true;
				envCounter = 0;
				expCounter = 0;
				expPeriod = expPeriodTable[0];
				freeze = false;
				gate = false;
				lfsr = 0x7FFF;
				rate = adsrTable[release];
				release = 0;
				state = stateRelease;
				sustain = 0;
			}

			private void UpdateExpCounter()
			{
				
				{
					for (int i = 0; i < 7; i++)
					{
						if (envCounter == expCounterTable[i])
							expPeriod = expPeriodTable[i];
					}
					if (envCounter == 0)
						freeze = true;
				}
			}

			// ------------------------------------

			public int Attack
			{
				get
				{
					return attack;
				}
				set
				{
					attack = (value & 0xF);
					if (state == stateAttack)
						rate = adsrTable[attack];
				}
			}

			public int Decay
			{
				get
				{
					return decay;
				}
				set
				{
					decay = (value & 0xF);
					if (state == stateDecay)
						rate = adsrTable[decay];
				}
			}

			public bool Gate
			{
				get
				{
					return gate;
				}
				set
				{
					bool nextGate = value;
					if (nextGate && !gate)
					{
						state = stateAttack;
						rate = adsrTable[attack];
						delay = true;
						freeze = false;
					}
					else if (!nextGate && gate)
					{
						state = stateRelease;
						rate = adsrTable[release];
					}
					gate = nextGate;
				}
			}

			public int Level
			{
				get
				{
					return envCounter;
				}
			}

			public int Release
			{
				get
				{
					return release;
				}
				set
				{
					release = (value & 0xF);
					if (state == stateRelease)
						rate = adsrTable[release];
				}
			}

			public int Sustain
			{
				get
				{
					return sustain;
				}
				set
				{
					sustain = (value & 0xF);
				}
			}

			// ------------------------------------

			public void SyncState(Serializer ser)
			{
                Sync.SyncObject(ser, this);
            }

			// ------------------------------------
		}

		sealed class Voice
		{
            int accBits;
            int accNext;
			int accumulator;
            bool controlTestPrev;
            int controlWavePrev;
			int delay;
			int floatOutputTTL;
			int frequency;
            bool msbRising;
			int noise;
			int noNoise;
			int noNoiseOrNoise;
			int noPulse;
			int output;
			int pulse;
			int pulseWidth;
			bool ringMod;
			int ringMsbMask;
			int shiftRegister;
			int shiftRegisterReset;
			bool sync;
			bool test;
			int[] wave;
			int waveform;
            int waveformIndex;
			int[][] waveTable;

			public Voice(int[][] newWaveTable)
			{
				waveTable = newWaveTable;
				HardReset();
			}

			public void HardReset()
			{
				accumulator = 0;
				delay = 0;
				floatOutputTTL = 0;
				frequency = 0;
				msbRising = false;
				noNoise = 0xFFF;
				noPulse = 0xFFF;
				output = 0x000;
				pulse = 0xFFF;
				pulseWidth = 0;
				ringMsbMask = 0;
				sync = false;
				test = false;
				wave = waveTable[0];
				waveform = 0;

				ResetShiftReg();
			}

			public void ExecutePhase2()
			{
				
				{
					if (test)
					{
						if (shiftRegisterReset != 0 && --shiftRegisterReset == 0)
						{
							ResetShiftReg();
						}
						pulse = 0xFFF;
					}
					else
					{
						accNext = (accumulator + frequency) & 0xFFFFFF;
						accBits = ~accumulator & accNext;
						accumulator = accNext;
						msbRising = ((accBits & 0x800000) != 0);

						if ((accBits & 0x080000) != 0)
							delay = 2;
						else if (delay != 0 && --delay == 0)
							ClockShiftReg();
					}
				}
			}

			// ------------------------------------

			private void ClockShiftReg()
			{
				
				{
					shiftRegister = ((shiftRegister << 1) |
                        (((shiftRegister >> 22) ^ (shiftRegister >> 17)) & 0x1)
                        ) & 0x7FFFFF;
					SetNoise();
				}
			}

			private void ResetShiftReg()
			{
				
				{
					shiftRegister = 0x7FFFFF;
					shiftRegisterReset = 0;
					SetNoise();
				}
			}

			private void SetNoise()
			{
				
				{
					noise =
						((shiftRegister & 0x100000) >> 9) |
						((shiftRegister & 0x040000) >> 8) |
						((shiftRegister & 0x004000) >> 5) |
						((shiftRegister & 0x000800) >> 3) |
						((shiftRegister & 0x000200) >> 2) |
						((shiftRegister & 0x000020) << 1) |
						((shiftRegister & 0x000004) << 3) |
						((shiftRegister & 0x000001) << 4);
					noNoiseOrNoise = noNoise | noise;
				}
			}

			private void WriteShiftReg()
			{
				
				{
					output &=
						0xBB5DA |
						((output & 0x800) << 9) |
						((output & 0x400) << 8) |
						((output & 0x200) << 5) |
						((output & 0x100) << 3) |
						((output & 0x040) >> 1) |
						((output & 0x020) >> 3) |
						((output & 0x010) >> 4);
					noise &= output;
					noNoiseOrNoise = noNoise | noise;
				}
			}

			// ------------------------------------

			public int Control
			{
				set
				{
					controlWavePrev = waveform;
					controlTestPrev = test;

					sync = ((value & 0x02) != 0);
					ringMod = ((value & 0x04) != 0);
					test = ((value & 0x08) != 0);
					waveform = (value >> 4) & 0x0F;
					wave = waveTable[waveform & 0x07];
					ringMsbMask = ((~value >> 5) & (value >> 2) & 0x1) << 23;
					noNoise = ((waveform & 0x8) != 0) ? 0x000 : 0xFFF;
					noNoiseOrNoise = noNoise | noise;
					noPulse = ((waveform & 0x4) != 0) ? 0x000 : 0xFFF;

                    if (!controlTestPrev && test)
					{
						accumulator = 0;
						delay = 0;
						shiftRegisterReset = 0x8000;
					}
                    else if (controlTestPrev && !test)
					{
						shiftRegister = ((shiftRegister << 1) |
                            ((~shiftRegister >> 17) & 0x1)
                            ) & 0x7FFFFF;
						SetNoise();
					}

                    if (waveform == 0 && controlWavePrev != 0)
						floatOutputTTL = 0x28000;
				}
			}

			public int Frequency
			{
				get
				{
					return frequency;
				}
				set
				{
					frequency = value;
				}
			}

			public int FrequencyLo
			{
				get
				{
					return (frequency & 0xFF);
				}
				set
				{
					frequency &= 0xFF00;
					frequency |= value & 0x00FF;
				}
			}

			public int FrequencyHi
			{
				get
				{
					return (frequency >> 8);
				}
				set
				{
					frequency &= 0x00FF;
					frequency |= (value & 0x00FF) << 8;
				}
			}

			public int Oscillator
			{
				get
				{
					return output;
				}
			}

			public int Output(Voice ringModSource)
			{
				
				{
					if (waveform != 0)
					{
						waveformIndex = (accumulator ^ (ringModSource.accumulator & ringMsbMask)) >> 12;
                        output = wave[waveformIndex] & (noPulse | pulse) & noNoiseOrNoise;
						if (waveform > 8)
							WriteShiftReg();
					}
					else
					{
						if (floatOutputTTL != 0 && --floatOutputTTL == 0)
							output = 0x000;
					}
					pulse = ((accumulator >> 12) >= pulseWidth) ? 0xFFF : 0x000;
					return output;
				}
			}

			public int PulseWidth
			{
				get
				{
					return pulseWidth;
				}
				set
				{
					pulseWidth = value;
				}
			}

			public int PulseWidthLo
			{
				get
				{
					return (pulseWidth & 0xFF);
				}
				set
				{
					pulseWidth &= 0x0F00;
					pulseWidth |= value & 0x00FF;
				}
			}

			public int PulseWidthHi
			{
				get
				{
					return (pulseWidth >> 8);
				}
				set
				{
					pulseWidth &= 0x00FF;
					pulseWidth |= (value & 0x000F) << 8;
				}
			}

			public bool RingMod
			{
				get
				{
					return ringMod;
				}
			}

			public bool Sync
			{
				get
				{
					return sync;
				}
			}

			public void Synchronize(Voice target, Voice source)
			{
				if (msbRising && target.sync && !(sync && source.msbRising))
					target.accumulator = 0;
			}

			public bool Test
			{
				get
				{
					return test;
				}
			}

			public int Waveform
			{
				get
				{
					return waveform;
				}
			}

			// ------------------------------------

			public void SyncState(Serializer ser)
			{
                BizHawk.Emulation.Computers.Commodore64.Sync.SyncObject(ser, this);

				if (ser.IsReader)
					wave = waveTable[waveform];
			}
		}

		// ------------------------------------

		public Sound.Utilities.SpeexResampler resampler;

		static int[] syncNextTable = new int[] { 1, 2, 0 };
		static int[] syncPrevTable = new int[] { 2, 0, 1 };

        int cachedCycles;
		bool disableVoice3;
		int[] envelopeOutput;
		Envelope[] envelopes;
		bool[] filterEnable;
		int filterFrequency;
		int filterResonance;
		bool filterSelectBandPass;
		bool filterSelectLoPass;
		bool filterSelectHiPass;
        int mixer;
        int potCounter;
		int potX;
        int potY;
        short sample;
        int[] voiceOutput;
		Voice[] voices;
		int volume;
		int[][] waveformTable;

		public Func<byte> ReadPotX;
		public Func<byte> ReadPotY;

		public Sid(int[][] newWaveformTable, int newSampleRate, Region newRegion)
		{
			uint cyclesPerSec = 0;

			switch (newRegion)
			{
				case Region.NTSC: cyclesPerSec = 14318181 / 14; break;
				case Region.PAL: cyclesPerSec = 17734472 / 18; break;
			}

			waveformTable = newWaveformTable;

			envelopes = new Envelope[3];
			for (int i = 0; i < 3; i++)
				envelopes[i] = new Envelope();
			envelopeOutput = new int[3];

			voices = new Voice[3];
			for (int i = 0; i < 3; i++)
				voices[i] = new Voice(newWaveformTable);
			voiceOutput = new int[3];

			filterEnable = new bool[3];
			for (int i = 0; i < 3; i++)
				filterEnable[i] = false;

			resampler = new Sound.Utilities.SpeexResampler(0, cyclesPerSec, 44100, cyclesPerSec, 44100, null, null);
		}

		public void Dispose()
		{
			if (resampler != null)
			{
				resampler.Dispose();
				resampler = null;
			}
		}

		// ------------------------------------

		public void HardReset()
		{
			for (int i = 0; i < 3; i++)
			{
				envelopes[i].HardReset();
				voices[i].HardReset();
			}
			potCounter = 0;
			potX = 0;
			potY = 0;
		}

		// ------------------------------------

        public void ExecutePhase2()
		{
            cachedCycles++;

            // potentiometer values refresh every 512 cycles
            if (potCounter == 0)
            {
                potCounter = 512;
                potX = ReadPotX();
                potY = ReadPotY();
                Flush(); //this is here unrelated to the pots, just to keep the buffer somewhat loaded
            }
            potCounter--;
        }

        public void Flush()
        {
            while (cachedCycles > 0)
            {
                // process voices and envelopes
                voices[0].ExecutePhase2();
                voices[1].ExecutePhase2();
                voices[2].ExecutePhase2();
                envelopes[0].ExecutePhase2();
                envelopes[1].ExecutePhase2();
                envelopes[2].ExecutePhase2();

                // process sync
                for (int i = 0; i < 3; i++)
                    voices[i].Synchronize(voices[syncNextTable[i]], voices[syncPrevTable[i]]);

                // get output
                voiceOutput[0] = voices[0].Output(voices[2]);
                voiceOutput[1] = voices[1].Output(voices[0]);
                voiceOutput[2] = voices[2].Output(voices[1]);
                envelopeOutput[0] = envelopes[0].Level;
                envelopeOutput[1] = envelopes[1].Level;
                envelopeOutput[2] = envelopes[2].Level;

                mixer = ((voiceOutput[0] * envelopeOutput[0]) >> 7);
                mixer += ((voiceOutput[1] * envelopeOutput[1]) >> 7);
                mixer += ((voiceOutput[2] * envelopeOutput[2]) >> 7);
                mixer = (mixer * volume) >> 4;

                sample = (short)mixer;
                resampler.EnqueueSample(sample, sample);
                cachedCycles--;
            }
        }

		// ------------------------------------

		public byte Peek(int addr)
		{
			return ReadRegister((addr & 0x1F));
		}

		public void Poke(int addr, byte val)
		{
			WriteRegister((addr & 0x1F), val);
		}

		public byte Read(int addr)
		{
			addr &= 0x1F;
			byte result = 0x00;
			switch (addr)
			{
				case 0x19:
				case 0x1A:
				case 0x1B:
				case 0x1C:
                    Flush();
					result = ReadRegister(addr);
					break;
			}
			return result;
		}

		private byte ReadRegister(int addr)
		{
			byte result = 0x00;

			switch (addr)
			{
				case 0x00: result = (byte)voices[0].FrequencyLo; break;
				case 0x01: result = (byte)voices[0].FrequencyHi; break;
				case 0x02: result = (byte)voices[0].PulseWidthLo; break;
				case 0x03: result = (byte)voices[0].PulseWidthHi; break;
				case 0x04:
					result = (byte)(
						(envelopes[0].Gate ? 0x01 : 0x00) |
						(voices[0].Sync ? 0x02 : 0x00) |
						(voices[0].RingMod ? 0x04 : 0x00) |
						(voices[0].Test ? 0x08 : 0x00) |
						(byte)(voices[0].Waveform << 4)
						);
					break;
				case 0x05:
					result = (byte)(
						(envelopes[0].Attack << 4) |
						(envelopes[0].Decay)
						);
					break;
				case 0x06: 
					result = (byte)(
						(envelopes[0].Sustain << 4) |
						(envelopes[0].Release)
						);
					break;
				case 0x07: result = (byte)voices[1].FrequencyLo; break;
				case 0x08: result = (byte)voices[1].FrequencyHi; break;
				case 0x09: result = (byte)voices[1].PulseWidthLo; break;
				case 0x0A: result = (byte)voices[1].PulseWidthHi; break;
				case 0x0B:
					result = (byte)(
						(envelopes[1].Gate ? 0x01 : 0x00) |
						(voices[1].Sync ? 0x02 : 0x00) |
						(voices[1].RingMod ? 0x04 : 0x00) |
						(voices[1].Test ? 0x08 : 0x00) |
						(byte)(voices[1].Waveform << 4)
						);
					break;
				case 0x0C:
					result = (byte)(
						(envelopes[1].Attack << 4) |
						(envelopes[1].Decay)
						);
					break;
				case 0x0D:
					result = (byte)(
						(envelopes[1].Sustain << 4) |
						(envelopes[1].Release)
						);
					break;
				case 0x0E: result = (byte)voices[2].FrequencyLo; break;
				case 0x0F: result = (byte)voices[2].FrequencyHi; break;
				case 0x10: result = (byte)voices[2].PulseWidthLo; break;
				case 0x11: result = (byte)voices[2].PulseWidthHi; break;
				case 0x12:
					result = (byte)(
						(envelopes[2].Gate ? 0x01 : 0x00) |
						(voices[2].Sync ? 0x02 : 0x00) |
						(voices[2].RingMod ? 0x04 : 0x00) |
						(voices[2].Test ? 0x08 : 0x00) |
						(byte)(voices[2].Waveform << 4)
						);
					break;
				case 0x13:
					result = (byte)(
						(envelopes[2].Attack << 4) |
						(envelopes[2].Decay)
						);
					break;
				case 0x14:
					result = (byte)(
						(envelopes[2].Sustain << 4) |
						(envelopes[2].Release)
						);
					break;
				case 0x15: result = (byte)(filterFrequency & 0x7); break;
				case 0x16: result = (byte)((filterFrequency >> 3) & 0xFF); break;
				case 0x17:
					result = (byte)(
						(filterEnable[0] ? 0x01 : 0x00) |
						(filterEnable[1] ? 0x02 : 0x00) |
						(filterEnable[2] ? 0x04 : 0x00) |
						(byte)(filterResonance << 4)
						);
					break;
				case 0x18:
					result = (byte)(
						(byte)volume |
						(filterSelectLoPass ? 0x10 : 0x00) |
						(filterSelectBandPass ? 0x20 : 0x00) |
						(filterSelectHiPass ? 0x40 : 0x00) |
						(disableVoice3 ? 0x80 : 0x00)
						);
					break;
				case 0x19: result = (byte)potX; break;
				case 0x1A: result = (byte)potY;	break;
				case 0x1B: result = (byte)(voiceOutput[2] >> 4); break;
				case 0x1C: result = (byte)(envelopeOutput[2]); break;
			}

			return result;
		}

		public void Write(int addr, byte val)
		{
			addr &= 0x1F;
			switch (addr)
			{
				case 0x19:
				case 0x1A:
				case 0x1B:
				case 0x1C:
				case 0x1D:
				case 0x1E:
				case 0x1F:
					// can't write to these
					break;
				default:
                    Flush();
					WriteRegister(addr, val);
					break;
			}
		}

		private void WriteRegister(int addr, byte val)
		{
			switch (addr)
			{
				case 0x00: voices[0].FrequencyLo = val; break;
				case 0x01: voices[0].FrequencyHi = val; break;
				case 0x02: voices[0].PulseWidthLo = val; break;
				case 0x03: voices[0].PulseWidthHi = val; break;
				case 0x04: voices[0].Control = val; envelopes[0].Gate = ((val & 0x01) != 0); break;
				case 0x05: envelopes[0].Attack = (val >> 4); envelopes[0].Decay = (val & 0xF); break;
				case 0x06: envelopes[0].Sustain = (val >> 4); envelopes[0].Release = (val & 0xF); break;
				case 0x07: voices[1].FrequencyLo = val; break;
				case 0x08: voices[1].FrequencyHi = val; break;
				case 0x09: voices[1].PulseWidthLo = val; break;
				case 0x0A: voices[1].PulseWidthHi = val; break;
				case 0x0B: voices[1].Control = val; envelopes[1].Gate = ((val & 0x01) != 0); break;
				case 0x0C: envelopes[1].Attack = (val >> 4); envelopes[1].Decay = (val & 0xF); break;
				case 0x0D: envelopes[1].Sustain = (val >> 4); envelopes[1].Release = (val & 0xF); break;
				case 0x0E: voices[2].FrequencyLo = val; break;
				case 0x0F: voices[2].FrequencyHi = val; break;
				case 0x10: voices[2].PulseWidthLo = val; break;
				case 0x11: voices[2].PulseWidthHi = val; break;
				case 0x12: voices[2].Control = val; envelopes[2].Gate = ((val & 0x01) != 0); break;
				case 0x13: envelopes[2].Attack = (val >> 4); envelopes[2].Decay = (val & 0xF); break;
				case 0x14: envelopes[2].Sustain = (val >> 4); envelopes[2].Release = (val & 0xF); break;
				case 0x15: filterFrequency &= 0x3FF; filterFrequency |= (val & 0x7); break;
				case 0x16: filterFrequency &= 0x7; filterFrequency |= val << 3; break;
				case 0x17:
					filterEnable[0] = ((val & 0x1) != 0);
					filterEnable[1] = ((val & 0x2) != 0);
					filterEnable[2] = ((val & 0x4) != 0);
					filterResonance = val >> 4;
					break;
				case 0x18:
					volume = (val & 0xF);
					filterSelectLoPass = ((val & 0x10) != 0);
					filterSelectBandPass = ((val & 0x20) != 0);
					filterSelectHiPass = ((val & 0x40) != 0);
					disableVoice3 = ((val & 0x40) != 0);
					break;
				case 0x19:
					potX = val;
					break;
				case 0x1A:
					potY = val;
					break;
			}
		}

		// ----------------------------------

        public void SyncState(Serializer ser)
		{
            Sync.SyncObject(ser, this);
            ser.BeginSection("env0");
			envelopes[0].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("wav0");
			voices[0].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("env1");
			envelopes[1].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("wav1");
			voices[1].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("env2");
			envelopes[2].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("wav2");
			voices[2].SyncState(ser);
			ser.EndSection();
		}
	}
}
