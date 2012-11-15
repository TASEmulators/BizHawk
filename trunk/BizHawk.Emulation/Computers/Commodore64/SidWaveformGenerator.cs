using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	
	// constants for the WaveformGenerator and calculation
	// methods come from the libsidplayfp residfp library.

	class WaveformGenerator
	{
		private int accumulator;
		private int floatingOutputTtl;
		private int freq;
		private short[][] modelWave;
		private bool msbRising;
		private int noiseOutput;
		private int noNoise;
		private int noNoiseOrNoiseOutput;
		private int noPulse;
		private int pulseOutput;
		private int pw;
		private int ringMsbMask;
		private int shiftPipeline;
		private int shiftRegister;
		private int shiftRegisterReset;
		private bool sync;
		private bool test;
		private short[] wave;
		private int waveform;
		private int waveformOutput;

		public WaveformGenerator(short[][] newModelWave)
		{
			modelWave = newModelWave;
			Reset();
		}

		public void Clock()
		{
			if (test)
			{
				if (shiftRegisterReset != 0 && --shiftRegisterReset == 0)
				{
					ResetShiftRegister();
				}
				pulseOutput = 0xFFF;
			}
			else
			{
				int accumulatorNext = (accumulator + freq) & 0xFFFFFF;
				int accumulatorBitsSet = ~accumulator & accumulatorNext;

				accumulator = accumulatorNext;
				msbRising = (accumulatorBitsSet & 0x800000) != 0;

				if ((accumulatorBitsSet & 0x080000) != 0)
				{
					shiftPipeline = 2;
				}
				else if (shiftPipeline != 0 && --shiftPipeline == 0)
				{
					ClockShiftRegister();
				}
			}
		}

		private void ClockShiftRegister()
		{
			int bit0 = ((shiftRegister >> 22) ^ (shiftRegister >> 17)) & 0x1;
			shiftRegister = ((shiftRegister << 1) | bit0) & 0x7FFFFF;
			SetNoiseOutput();
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

		public int ReadAccumulator()
		{
			return accumulator;
		}

		public int ReadFreq()
		{
			return freq;
		}

		public byte ReadOsc()
		{
			return (byte)(waveformOutput >> 4);
		}

		public bool ReadSync()
		{
			return sync;
		}

		public bool ReadTest()
		{
			return test;
		}

		public void Reset()
		{
			accumulator = 0;
			freq = 0;
			pw = 0;
			msbRising = false;
			waveform = 0;
			test = false;
			sync = false;
			wave = modelWave[0];
			ringMsbMask = 0;
			noNoise = 0xFFF;
			noPulse = 0xFFF;
			pulseOutput = 0xFFF;
			ResetShiftRegister();
			shiftPipeline = 0;
			waveformOutput = 0;
			floatingOutputTtl = 0;
		}

		private void ResetShiftRegister()
		{
			shiftRegister = 0x7FFFFF;
			shiftRegisterReset = 0;
			SetNoiseOutput();
		}

		private void SetNoiseOutput()
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

		public void Synchronize(WaveformGenerator syncDest, WaveformGenerator syncSource)
		{
			if (msbRising && syncDest.sync && !(sync && syncSource.msbRising))
			{
				syncDest.accumulator = 0;
			}
		}

		public void WriteControl(byte control)
		{
			int waveformPrev = waveform;
			bool testPrev = test;
			waveform = (control >> 4) & 0x0F;
			test = (control & 0x08) != 0;
			sync = (control & 0x02) != 0;

			wave = modelWave[waveform & 0x7];
			ringMsbMask = ((~control >> 5) & (control >> 2) & 0x1) << 23;
			noNoise = (waveform & 0x8) != 0 ? 0x000 : 0xFFF;
			noNoiseOrNoiseOutput = noNoise | noiseOutput;
			noPulse = (waveform & 0x4) != 0 ? 0x000 : 0xFFF;

			if (!testPrev && test)
			{
				accumulator = 0;
				shiftPipeline = 0;
				shiftRegisterReset = 0x8000;
			}
			else if (testPrev && !test)
			{
				int bit0 = (~shiftRegister >> 17) & 0x1;
				shiftRegister = ((shiftRegister << 1) | bit0) & 0x7FFFFF;
				SetNoiseOutput();
			}

			if (waveform == 0 && waveformPrev != 0)
			{
				floatingOutputTtl = 0x28000;
			}
		}

		public void WriteFreqLo(byte freqLo)
		{
			freq &= 0xFF00;
			freq |= freqLo;
		}

		public void WriteFreqHi(byte freqHi)
		{
			freq &= 0x00FF;
			freq |= (int)(freqHi << 8) & 0xFF00;
		}

		public void WritePWLo(byte pwLo)
		{
			pw &= 0x0F00;
			pw |= pwLo;
		}

		public void WritePWHi(byte pwHi)
		{
			pw &= 0x00FF;
			pw |= (int)(pwHi << 8) & 0x0F00;
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
