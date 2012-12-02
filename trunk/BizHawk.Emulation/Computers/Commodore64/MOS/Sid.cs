using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public abstract partial class Sid : IStandardIO
	{
		// ------------------------------------

		private class Envelope
		{
			private const uint stateAttack = 0;
			private const uint stateDecay = 1;
			private const uint stateRelease = 2;

			private uint attack;
			private uint decay;
			private bool delay;
			private uint envCounter;
			private uint expCounter;
			private uint expPeriod;
			private bool freeze;
			private uint lfsr;
			private bool gate;
			private uint rate;
			private uint release;
			private uint state;
			private uint sustain;

			private static uint[] adsrTable = new uint[]
			{
				0x7F00, 0x0006, 0x003C, 0x0330,
				0x20C0, 0x6755, 0x3800, 0x500E,
				0x1212, 0x0222, 0x1848, 0x59B8,
				0x3840, 0x77E2, 0x7625, 0x0A93
			};

			private static uint[] expCounterTable = new uint[]
			{
				0xFF, 0x5D, 0x36, 0x1A, 0x0E, 0x06, 0x00
			};

			private static uint[] expPeriodTable = new uint[]
			{
				0x01, 0x02, 0x04, 0x08, 0x10, 0x1E, 0x01
			};

			private static uint[] sustainTable = new uint[]
			{
				0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
				0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
			};

			public Envelope()
			{
				HardReset();
			}

			public void ExecutePhase1()
			{
				// do nothing
			}

			public void ExecutePhase2()
			{
				if (!delay)
				{
					envCounter--;
					delay = true;
					UpdateExpCounter();
				}
				if (lfsr != rate)
				{
					uint feedback = ((lfsr >> 14) ^ (lfsr >> 13)) & 0x1;
					lfsr = ((lfsr << 1) & 0x7FFF) | feedback;
				}
				else
				{
					lfsr = 0x7FFF;
					if (state == stateAttack || ++expCounter == expPeriod)
					{
						expCounter = 0;
						if (!freeze)
						{
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
							UpdateExpCounter();
						}
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
				for (uint i = 0; i < 7; i++)
				{
					if (envCounter == expCounterTable[i])
						expPeriod = expPeriodTable[i];
				}
				if (envCounter == 0)
					freeze = true;
			}

			// ------------------------------------

			public uint Attack
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

			public uint Decay
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

			public uint Level
			{
				get
				{
					return envCounter;
				}
			}

			public uint Release
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

			public uint Sustain
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
		}

		private class Voice
		{
			private uint accumulator;
			private uint delay;
			private uint floatOutputTTL;
			private uint frequency;
			private bool msbRising;
			private uint noise;
			private uint noNoise;
			private uint noNoiseOrNoise;
			private uint noPulse;
			private uint output;
			private uint pulse;
			private uint pulseWidth;
			private bool ringMod;
			private uint ringMsbMask;
			private uint shiftRegister;
			private uint shiftRegisterReset;
			private bool sync;
			private bool test;
			private uint[] wave;
			private uint waveform;
			private uint[][] waveTable;

			public Voice(uint[][] newWaveTable)
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

			public void ExecutePhase1()
			{
				// do nothing
			}

			public void ExecutePhase2()
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
					uint accNext = (accumulator + frequency) & 0xFFFFFF;
					uint accBits = ~accumulator & accNext;
					accumulator = accNext;
					msbRising = ((accBits & 0x800000) != 0);

					if ((accBits & 0x080000) != 0)
						delay = 2;
					else if (delay != 0 && --delay == 0)
						ClockShiftReg();
				}
			}

			// ------------------------------------

			private void ClockShiftReg()
			{
				uint bit0 = ((shiftRegister >> 22) ^ (shiftRegister >> 17)) & 0x1;
				shiftRegister = ((shiftRegister << 1) | bit0) & 0x7FFFFF;
				SetNoise();
			}

			private void ResetShiftReg()
			{
				shiftRegister = 0x7FFFFF;
				shiftRegisterReset = 0;
				SetNoise();
			}

			private void SetNoise()
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

			private void WriteShiftReg()
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

			// ------------------------------------

			public uint Control
			{
				set
				{
					uint wavePrev = waveform;
					bool testPrev = test;

					sync = ((value & 0x02) != 0);
					ringMod = ((value & 0x04) != 0);
					test = ((value & 0x08) != 0);
					waveform = (value >> 4) & 0x0F;
					wave = waveTable[waveform & 0x07];
					ringMsbMask = ((~value >> 5) & (value >> 2) & 0x1) << 23;
					noNoise = ((waveform & 0x8) != 0) ? (uint)0x000 : (uint)0xFFF;
					noNoiseOrNoise = noNoise | noise;
					noPulse = ((waveform & 0x4) != 0) ? (uint)0x000 : (uint)0xFFF;

					if (!testPrev && test)
					{
						accumulator = 0;
						delay = 0;
						shiftRegisterReset = 0x8000;
					}
					else if (testPrev && !test)
					{
						uint bit0 = (~shiftRegister >> 17) & 0x1;
						shiftRegister = ((shiftRegister << 1) | bit0) & 0x7FFFFF;
						SetNoise();
					}

					if (waveform == 0 && wavePrev != 0)
						floatOutputTTL = 0x28000;
				}
			}

			public uint Frequency
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

			public uint FrequencyLo
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

			public uint FrequencyHi
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

			public uint Oscillator
			{
				get
				{
					return output;
				}
			}

			public uint Output(Voice ringModSource)
			{
				if (waveform != 0)
				{
					uint index = (accumulator ^ (ringModSource.accumulator & ringMsbMask)) >> 12;
					output = wave[index] & (noPulse | pulse) & noNoiseOrNoise;
					if (waveform > 8)
						WriteShiftReg();
				}
				else
				{
					if (floatOutputTTL != 0 && --floatOutputTTL == 0)
						output = 0x000;
				}
				pulse = ((accumulator >> 12) >= pulseWidth) ? (uint)0xFFF : (uint)0x000;
				return 0;
			}

			public uint PulseWidth
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

			public uint PulseWidthLo
			{
				get
				{
					return (pulseWidth & 0xFF);
				}
				set
				{
					pulseWidth &= 0xFF00;
					pulseWidth |= value & 0x00FF;
				}
			}

			public uint PulseWidthHi
			{
				get
				{
					return (pulseWidth >> 8);
				}
				set
				{
					pulseWidth &= 0x00FF;
					pulseWidth |= (value & 0x00FF) << 8;
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

			public uint Waveform
			{
				get
				{
					return waveform;
				}
			}
		}

		// ------------------------------------

		private static uint[] syncNextTable = new uint[] { 1, 2, 0 };
		private static uint[] syncPrevTable = new uint[] { 2, 0, 1 };

		private bool disableVoice3;
		private uint[] envelopeOutput;
		private Envelope[] envelopes;
		private bool[] filterEnable;
		private uint filterFrequency;
		private uint filterResonance;
		private bool filterSelectBandPass;
		private bool filterSelectLoPass;
		private bool filterSelectHiPass;
		private uint potCounter;
		private uint potX;
		private uint potY;
		private uint[] voiceOutput;
		private Voice[] voices;
		private uint volume;
		private uint[][] waveformTable;

		public Sid(uint[][] newWaveformTable)
		{
			waveformTable = newWaveformTable;

			envelopes = new Envelope[3];
			for (int i = 0; i < 3; i++)
				envelopes[i] = new Envelope();
			envelopeOutput = new uint[3];

			voices = new Voice[3];
			for (int i = 0; i < 3; i++)
				voices[i] = new Voice(newWaveformTable);
			voiceOutput = new uint[3];

			filterEnable = new bool[3];
			for (int i = 0; i < 3; i++)
				filterEnable[i] = false;
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

		public void ExecutePhase1()
		{
			// do nothing
		}

		public void ExecutePhase2()
		{
			// potentiometer values refresh every 512 cycles
			if (potCounter == 0)
			{
				potCounter = 512;
				potX = 0; //todo: implement paddles
				potY = 0;
			}

			// process voices and envelopes
			voices[0].ExecutePhase2();
			voices[1].ExecutePhase2();
			voices[2].ExecutePhase2();
			envelopes[0].ExecutePhase2();
			envelopes[1].ExecutePhase2();
			envelopes[2].ExecutePhase2();

			// process sync
			for (uint i = 0; i < 3; i++)
				voices[i].Synchronize(voices[syncNextTable[i]], voices[syncPrevTable[i]]);

			// get output
			voiceOutput[0] = voices[0].Output(voices[1]);
			voiceOutput[1] = voices[1].Output(voices[2]);
			voiceOutput[2] = voices[2].Output(voices[0]);
			envelopeOutput[0] = envelopes[0].Level;
			envelopeOutput[1] = envelopes[1].Level;
			envelopeOutput[2] = envelopes[2].Level;
		}

		// ------------------------------------

		public byte Peek(int addr)
		{
			return ReadRegister((ushort)(addr & 0x1F));
		}

		public void Poke(int addr, byte val)
		{
			WriteRegister((ushort)(addr & 0x1F), val);
		}

		public byte Read(ushort addr)
		{
			addr &= 0x1F;
			byte result = 0x00;
			switch (addr)
			{
				case 0x19:
				case 0x1A:
				case 0x1B:
				case 0x1C:
					result = ReadRegister(addr);
					break;
			}
			return result;
		}

		private byte ReadRegister(ushort addr)
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
				case 0x1B: result = (byte)(voices[2].Oscillator >> 4); break;
				case 0x1C: result = (byte)(envelopes[2].Level); break;
			}

			return result;
		}

		public void Write(ushort addr, byte val)
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
					WriteRegister(addr, val);
					break;
			}
		}

		private void WriteRegister(ushort addr, byte val)
		{
		}
	}
}
