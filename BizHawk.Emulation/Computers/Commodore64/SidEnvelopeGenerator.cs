using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{

	// constants for the EnvelopeGenerator and calculation
	// methods come from the libsidplayfp residfp library.

	class EnvelopeGenerator
	{
		enum State
		{
			Attack, DecaySustain, Release
		}

		static int[] adsrTable = new int[]
		{
			0x7F00, 0x0006, 0x003C, 0x0330,
			0x20C0, 0x6755, 0x3800, 0x500E,
			0x1212, 0x0222, 0x1848, 0x59B8,
			0x3840, 0x77E2, 0x7625, 0x0A93
		};

		int attack;
		int decay;
		byte envelopeCounter;
		bool envelopePipeline;
		int exponentialCounter;
		int exponentialCounterPeriod;
		bool gate;
		bool holdZero;
		int lfsr;
		int rate;
		int release;
		State state;
		int sustain;

		public EnvelopeGenerator()
		{
			Reset();
		}

		public void Clock()
		{
			if (envelopePipeline)
			{
				--envelopeCounter;
				envelopePipeline = false;
				SetExponentialCounter();
			}

			if (lfsr != rate)
			{
				int feedback = ((lfsr >> 14) ^ (lfsr >> 13)) & 0x01;
				lfsr = ((lfsr << 1) & 0x7FFF) | feedback;
				return;
			}

			lfsr = 0x7FFF;

			if ((state == State.Attack) || (++exponentialCounter == exponentialCounterPeriod))
			{
				exponentialCounter = 0;
				if (holdZero)
				{
					return;
				}

				switch (state)
				{
					case State.Attack:
						++envelopeCounter;
						if (envelopeCounter == 0xFF)
						{
							state = State.DecaySustain;
							rate = adsrTable[decay];
						}
						break;
					case State.DecaySustain:
						if (envelopeCounter == ((sustain << 4) | sustain))
						{
							return;
						}
						if (exponentialCounterPeriod != 1)
						{
							envelopePipeline = true;
							return;
						}
						--envelopeCounter;
						break;
					case State.Release:
						if (exponentialCounterPeriod != 1)
						{
							envelopePipeline = true;
							return;
						}
						--envelopeCounter;
						break;
				}

				SetExponentialCounter();
			}
		}

		public short Output()
		{
			return envelopeCounter;
		}

		public byte ReadEnv()
		{
			return envelopeCounter;
		}

		public void Reset()
		{
			envelopeCounter = 0;
			envelopePipeline = false;
			attack = 0;
			decay = 0;
			sustain = 0;
			release = 0;
			gate = false;
			lfsr = 0x7FFF;
			exponentialCounter = 0;
			exponentialCounterPeriod = 1;
			state = State.Release;
			rate = adsrTable[release];
			holdZero = true;
		}

		private void SetExponentialCounter()
		{
			switch (envelopeCounter)
			{
				case 0xFF:
					exponentialCounterPeriod = 1;
					break;
				case 0x5D:
					exponentialCounterPeriod = 2;
					break;
				case 0x36:
					exponentialCounterPeriod = 4;
					break;
				case 0x1A:
					exponentialCounterPeriod = 8;
					break;
				case 0x0E:
					exponentialCounterPeriod = 16;
					break;
				case 0x06:
					exponentialCounterPeriod = 30;
					break;
				case 0x00:
					exponentialCounterPeriod = 1;
					holdZero = true;
					break;
			}
		}

		public void WriteAttackDecay(byte attackDecay)
		{
			attack = (attackDecay >> 4) & 0x0F;
			decay = attackDecay & 0x0F;
			if (state == State.Attack)
			{
				rate = adsrTable[attack];
			}
			else if (state == State.DecaySustain)
			{
				rate = adsrTable[decay];
			}
		}

		public void WriteControl(byte control)
		{
			bool gateNext = ((control & 0x01) != 0);

			if (!gate && gateNext)
			{
				state = State.Attack;
				rate = adsrTable[attack];
				holdZero = false;
				envelopePipeline = false;
			}
			else if (gate && !gateNext)
			{
				state = State.Release;
				rate = adsrTable[release];
			}

			gate = gateNext;
		}

		public void WriteSustainRelease(byte sustainRelease)
		{
			sustain = (sustainRelease >> 4) & 0x0F;
			release = sustainRelease & 0x0F;
			if (state == State.Release)
			{
				rate = adsrTable[release];
			}
		}
	}
}
