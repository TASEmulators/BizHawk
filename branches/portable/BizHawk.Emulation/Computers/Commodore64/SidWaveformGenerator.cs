using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	
	// constants for the WaveformGenerator and calculation
	// methods come from the libsidplayfp residfp library.

	public partial class WaveformGenerator
	{
		// internal
		private byte control;
		private int freq;
		private int pw;

		public int accumulator;
		public int floatingOutputTtl;
		public bool msbRising;
		public int noiseOutput;
		public int pulseOutput;
		public int ringMsbMask;
		public int shiftRegister;
		public int shiftRegisterDelay;
		public int shiftRegisterResetDelay;
		public bool sync;
		public bool test;
		private short[] wave;
		public int waveform;
		public int waveformOutput;

		// these are temp values used to speed up calculation
		private int noNoise;
		private int noNoiseOrNoiseOutput;
		private int noPulse;

		public WaveformGenerator()
		{
			Reset();
		}

		public void Clock()
		{
			if (test)
			{
				pulseOutput = 0xFFF;
				if (shiftRegisterResetDelay != 0 && --shiftRegisterResetDelay == 0)
				{
					ResetShiftRegister();
				}
			}
			else
			{
				int accumulatorNext = (accumulator + freq) & 0xFFFFFF;
				int accumulatorBitsSet = ~accumulator & accumulatorNext;

				accumulator = accumulatorNext;
				msbRising = (accumulatorBitsSet & 0x800000) != 0;

				if ((accumulatorBitsSet & 0x080000) != 0)
				{
					shiftRegisterDelay = 2;
				}
				else if (shiftRegisterDelay != 0 && --shiftRegisterDelay == 0)
				{
					ClockShiftRegister();
				}
			}
		}

		private void ClockShiftRegister()
		{
			int bit0 = ((shiftRegister >> 22) ^ (shiftRegister >> 17)) & 0x1;
			shiftRegister = ((shiftRegister << 1) | bit0) & 0x7FFFFF;
			UpdateNoiseOutput();
		}

		public byte Control
		{
			get
			{
				return control;
			}
			set
			{
				control = value;

				int waveformPrev = waveform;
				bool testPrev = test;
				waveform = (control >> 4) & 0x0F;
				test = (control & 0x08) != 0;
				sync = (control & 0x02) != 0;

				wave = WaveformSamples[waveform & 0x7];
				ringMsbMask = ((~control >> 5) & (control >> 2) & 0x1) << 23;
				noNoise = (waveform & 0x8) != 0 ? 0x000 : 0xFFF;
				noNoiseOrNoiseOutput = noNoise | noiseOutput;
				noPulse = (waveform & 0x4) != 0 ? 0x000 : 0xFFF;

				if (!testPrev && test)
				{
					accumulator = 0;
					shiftRegisterDelay = 0;
					shiftRegisterResetDelay = 0x8000;
				}
				else if (testPrev && !test)
				{
					int bit0 = (~shiftRegister >> 17) & 0x1;
					shiftRegister = ((shiftRegister << 1) | bit0) & 0x7FFFFF;
					UpdateNoiseOutput();
				}

				if (waveform == 0 && waveformPrev != 0)
				{
					floatingOutputTtl = 0x28000;
				}
			}
		}


		public int Frequency
		{
			get
			{
				return freq;
			}
			set
			{
				freq = value;
			}
		}

		public short Output(WaveformGenerator ringModulator)
		{
			if (waveform != 0)
			{
				int ix = (accumulator ^ (ringModulator.accumulator & ringMsbMask)) >> 12;
				waveformOutput = wave[ix] & (noPulse | pulseOutput) & noNoiseOrNoiseOutput;
				if (waveform > 0x8)
				{
					WriteShiftRegister();
				}
			}
			else
			{
				if (floatingOutputTtl != 0 && --floatingOutputTtl == 0)
				{
					waveformOutput = 0;
				}
			}
			pulseOutput = ((accumulator >> 12) >= pw) ? 0xFFF : 0x000;
			return (short)waveformOutput;
		}

		public int PulseWidth
		{
			get
			{
				return pw;
			}
			set
			{
				pw = value;
			}
		}

		public void Reset()
		{
			control = 0;
			waveform = 0;
			freq = 0;
			pw = 0;
			accumulator = 0;
			test = false;
			sync = false;

			msbRising = false;
			wave = WaveformSamples[0];
			ringMsbMask = 0;

			noNoise = 0xFFF;
			noPulse = 0xFFF;
			pulseOutput = 0xFFF;

			ResetShiftRegister();

			shiftRegisterDelay = 0;
			waveformOutput = 0;
			floatingOutputTtl = 0;
		}

		private void ResetShiftRegister()
		{
			shiftRegister = 0x7FFFFF;
			shiftRegisterResetDelay = 0;
			UpdateNoiseOutput();
		}

		public void SetState(byte stateControl, int stateFreq, int statePulseWidth)
		{
			pw = statePulseWidth;
			freq = stateFreq;
			control = stateControl;
			noNoise = (waveform & 0x8) != 0 ? 0x000 : 0xFFF;
			noNoiseOrNoiseOutput = noNoise | noiseOutput;
			noPulse = (waveform & 0x4) != 0 ? 0x000 : 0xFFF;
			ringMsbMask = ((~control >> 5) & (control >> 2) & 0x1) << 23;
			waveform = (control >> 4) & 0x0F;
			test = (control & 0x08) != 0;
			sync = (control & 0x02) != 0;
			wave = WaveformSamples[waveform & 0x7];
		}

		public void Synchronize(WaveformGenerator syncDest, WaveformGenerator syncSource)
		{
			if (msbRising && syncDest.sync && !(sync && syncSource.msbRising))
			{
				syncDest.accumulator = 0;
			}
		}

		private void UpdateNoiseOutput()
		{
			noiseOutput =
				((shiftRegister & 0x100000) >> 9) |
				((shiftRegister & 0x040000) >> 8) |
				((shiftRegister & 0x004000) >> 5) |
				((shiftRegister & 0x000800) >> 3) |
				((shiftRegister & 0x000200) >> 2) |
				((shiftRegister & 0x000020) << 1) |
				((shiftRegister & 0x000004) << 3) |
				((shiftRegister & 0x000001) << 4);
			noNoiseOrNoiseOutput = noNoise | noiseOutput;
		}

		private void WriteShiftRegister()
		{
			shiftRegister &=
				~((1 << 20) | (1 << 18) | (1 << 14) | (1 << 11) |
				(1 << 9) | (1 << 5) | (1 << 2) | (1 << 0)) |
				((waveformOutput & 0x800) << 9) |
				((waveformOutput & 0x400) << 8) |
				((waveformOutput & 0x200) << 5) |
				((waveformOutput & 0x100) << 3) |
				((waveformOutput & 0x080) << 2) |
				((waveformOutput & 0x040) >> 1) |
				((waveformOutput & 0x020) >> 3) |
				((waveformOutput & 0x010) >> 4);
			noiseOutput &= waveformOutput;
			noNoiseOrNoiseOutput = noNoise | noiseOutput;
		}
	}
}
