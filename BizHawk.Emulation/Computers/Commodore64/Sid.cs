using System;
using System.Collections.Generic;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public enum SidEnvelopeState
	{
		Disabled,
		Attack,
		Decay,
		Release
	}

	public enum SidMode
	{
		Sid6581,
		Sid8580
	}

	public class VoiceRegs
	{
		public int ATK;
		public int DCY;
		public int ENV;
		public int F;
		public bool FILT;
		public bool GATE;
		public bool NOISE;
		public int OSC;
		public int PW;
		public int RLS;
		public bool RMOD;
		public bool SAW;
		public int SR;
		public bool SQU;
		public int STN;
		public bool SYNC;
		public bool TEST;
		public bool TRI;
	}

	public class SidRegs
	{
		public bool BP;
		public bool D3;
		public int FC;
		public bool FILTEX;
		public bool HP;
		public bool LP;
		public int POTX;
		public int POTY;
		public int RES;
		public int VOL;

		public VoiceRegs[] Voices;

		public SidRegs()
		{
			Voices = new VoiceRegs[3];
			for (int i = 0; i < 3; i++)
				Voices[i] = new VoiceRegs();

			// power on state
			Voices[0].SR = 0x7FFFFF;
			Voices[1].SR = 0x7FFFFF;
			Voices[2].SR = 0x7FFFFF;
		}

		public byte this[int addr]
		{

			get
			{
				int result;
				int index;

				addr &= 0x1F;
				switch (addr)
				{
					case 0x00:
					case 0x07:
					case 0x0E:
						result = Voices[addr / 7].F & 0xFF;
						break;
					case 0x01:
					case 0x08:
					case 0x0F:
						result = (Voices[addr / 7].F & 0xFF00) >> 8;
						break;
					case 0x02:
					case 0x09:
					case 0x10:
						result = Voices[addr / 7].PW & 0xFF;
						break;
					case 0x03:
					case 0x0A:
					case 0x11:
						result = (Voices[addr / 7].PW & 0x0F00) >> 8;
						break;
					case 0x04:
					case 0x0B:
					case 0x12:
						index = addr / 7;
						result = Voices[index].GATE ? 0x01 : 0x00;
						result |= Voices[index].SYNC ? 0x02 : 0x00;
						result |= Voices[index].RMOD ? 0x04 : 0x00;
						result |= Voices[index].TEST ? 0x08 : 0x00;
						result |= Voices[index].TRI ? 0x10 : 0x00;
						result |= Voices[index].SAW ? 0x20 : 0x00;
						result |= Voices[index].SQU ? 0x40 : 0x00;
						result |= Voices[index].NOISE ? 0x80 : 0x00;
						break;
					case 0x05:
					case 0x0C:
					case 0x13:
						index = addr / 7;
						result = (Voices[index].ATK & 0xF) << 4;
						result |= Voices[index].DCY & 0xF;
						break;
					case 0x06:
					case 0x0D:
					case 0x14:
						index = addr / 7;
						result = (Voices[index].STN & 0xF) << 4;
						result |= Voices[index].RLS & 0xF;
						break;
					case 0x15:
						result = FC & 0x7;
						break;
					case 0x16:
						result = (FC & 0x7F8) >> 3;
						break;
					case 0x17:
						result = Voices[0].FILT ? 0x01 : 0x00;
						result |= Voices[1].FILT ? 0x02 : 0x00;
						result |= Voices[2].FILT ? 0x04 : 0x00;
						result |= FILTEX ? 0x08 : 0x00;
						result |= (RES & 0xF) << 4;
						break;
					case 0x18:
						result = (VOL & 0xF);
						result |= LP ? 0x10 : 0x00;
						result |= BP ? 0x20 : 0x00;
						result |= HP ? 0x40 : 0x00;
						result |= D3 ? 0x80 : 0x00;
						break;
					case 0x19:
						result = POTX ;
						break;
					case 0x1A:
						result = POTY;
						break;
					case 0x1B:
						result = Voices[2].OSC >> 4;
						break;
					case 0x1C:
						result = Voices[2].ENV;
						break;
					default:
						result = 0;
						break;
				}
				return (byte)(result & 0xFF);
			}
			set
			{
				int val = value;
				int index;

				addr &= 0x1F;
				switch (addr)
				{
					case 0x00:
					case 0x07:
					case 0x0E:
						index = addr / 7;
						Voices[index].F &= 0xFF00;
						Voices[index].F |= val;
						break;
					case 0x01:
					case 0x08:
					case 0x0F:
						index = addr / 7;
						Voices[index].F &= 0xFF;
						Voices[index].F |= val << 8;
						break;
					case 0x02:
					case 0x09:
					case 0x10:
						index = addr / 7;
						Voices[index].PW &= 0x0F00;
						Voices[index].PW |= val;
						break;
					case 0x03:
					case 0x0A:
					case 0x11:
						index = addr / 7;
						Voices[index].PW &= 0xFF;
						Voices[index].PW |= (val & 0x0F) << 8;
						break;
					case 0x04:
					case 0x0B:
					case 0x12:
						index = addr / 7;
						Voices[index].GATE = ((val & 0x01) != 0x00);
						Voices[index].SYNC = ((val & 0x02) != 0x00);
						Voices[index].RMOD = ((val & 0x04) != 0x00);
						Voices[index].TEST = ((val & 0x08) != 0x00);
						Voices[index].TRI = ((val & 0x10) != 0x00);
						Voices[index].SAW = ((val & 0x20) != 0x00);
						Voices[index].SQU = ((val & 0x40) != 0x00);
						Voices[index].NOISE = ((val & 0x80) != 0x00);
						break;
					case 0x05:
					case 0x0C:
					case 0x13:
						index = addr / 7;
						Voices[index].ATK = (val >> 4) & 0xF;
						Voices[index].DCY = val & 0xF;
						break;
					case 0x06:
					case 0x0D:
					case 0x14:
						index = addr / 7;
						Voices[index].STN = (val >> 4) & 0xF;
						Voices[index].RLS = val & 0xF;
						break;
					case 0x15:
						FC &= 0x7F8;
						FC |= val & 0x7;
						break;
					case 0x16:
						FC &= 0x7;
						FC |= val << 3;
						break;
					case 0x17:
						Voices[0].FILT = ((val & 0x01) != 0x00);
						Voices[1].FILT = ((val & 0x02) != 0x00);
						Voices[2].FILT = ((val & 0x04) != 0x00);
						FILTEX = ((val & 0x08) != 0x00);
						RES = (val >> 4);
						break;
					case 0x18:
						VOL = (val & 0xF);
						LP = ((val & 0x10) != 0x00);
						BP = ((val & 0x20) != 0x00);
						HP = ((val & 0x40) != 0x00);
						D3 = ((val & 0x80) != 0x00);
						break;
					case 0x19:
						POTX = val;
						break;
					case 0x1A:
						POTY = val;
						break;
					case 0x1B:
						Voices[2].OSC = val << 4;
						break;
					case 0x1C:
						Voices[2].ENV = val;
						break;
				}
			}
		}

	}

	public partial class Sid : ISoundProvider
	{
		private int[] envRateIndex = {
			9, 32, 63, 95,
			149, 220, 267, 313,
			392, 977, 1954, 3126,
			3907, 11720, 19532, 31251
		};
		private int[] sustainLevels = {
			0x00, 0x11, 0x22, 0x33,
			0x44, 0x55, 0x66, 0x77,
			0x88, 0x99, 0xAA, 0xBB,
			0xCC, 0xDD, 0xEE, 0xFF
		};
		private int[] syncIndex = { 2, 0, 1 };

		private VoiceRegs[] voices;

		public Func<int> ReadPotX;
		public Func<int> ReadPotY;

		public int clock;
		public int cyclesPerSample;
		public bool[] envEnable = new bool[3];
		public int[] envExp = new int[3];
		public int[] envExpCounter = new int[3];
		public int[] envRate = new int[3];
		public int[] envRateCounter = new int[3];
		public SidEnvelopeState[] envState = new SidEnvelopeState[3];
		public bool[] lastGate = new bool[3];
		public int output;
		public SidRegs regs;
		public int[] waveClock = new int[3];

		public Sid(Region newRegion, int sampleRate)
		{
			ReadPotX = DummyReadPot;
			ReadPotY = DummyReadPot;
			switch (newRegion)
			{
				case Region.NTSC:
					cyclesPerSample = 14318181 / 14 / sampleRate;
					break;
				case Region.PAL:
					cyclesPerSample = 14318181 / 18 / sampleRate;
					break;
			}
			InitSound(sampleRate);
			HardReset();
		}

		private int DummyReadPot()
		{
			return 0;
		}

		public void HardReset()
		{
			regs = new SidRegs();
			voices = regs.Voices;
		}

		private short Mix(int input, int volume, short mixSource)
		{
			// logarithmic volume (probably inaccurate)
			int logVolume = volume * 256;
			logVolume = (int)Math.Sqrt(logVolume);

			// combine the volumes together 1:1
			volume = (volume + logVolume) >> 1;

			input &= 0xFFF;
			input -= 0x800;
			input *= volume;
			input /= 255;
			input += mixSource;
			if (input > 32767)
				input = 32767;
			else if (input < -32768)
				input = -32768;
			return (short)input;
		}

		public byte Peek(int addr)
		{
			return regs[addr & 0x1F];
		}

		public void PerformCycle()
		{
			// accumulator is 24 bits
			clock = (clock + 1) & 0xFFFFFF;
			ProcessAccumulator(0);
			ProcessAccumulator(1);
			ProcessAccumulator(2);

			// process each voice
			ProcessVoice(0);
			ProcessVoice(1);
			ProcessVoice(2);

			// process voices again for best hard sync
			if (voices[1].SYNC)
				ProcessVoice(0);
			if (voices[2].SYNC)
				ProcessVoice(1);
			if (voices[0].SYNC)
				ProcessVoice(2);

			// process each envelope
			ProcessEnvelope(0);
			ProcessEnvelope(1);
			ProcessEnvelope(2);

			// submit sample to soundprovider
			SubmitSample();

			// query pots every 512 cycles
			if ((clock & 0x1FF) == 0x000)
			{
				regs.POTX = ReadPotX() & 0xFF;
				regs.POTY = ReadPotY() & 0xFF;
			}
		}

		public void Poke(int addr, byte val)
		{
			regs[addr & 0x1F] = val;
		}

		private void ProcessAccumulator(int index)
		{
			// test bit resets the oscillator
			if (voices[index].TEST)
			{
				waveClock[index] = 0x000000;
				voices[index].SR = 0x7FFFFF;
			}
			else
			{
				int lastWaveClock = waveClock[index];

				// increment wave clock
				waveClock[index] = (waveClock[index] + voices[index].F) & 0x00FFFFFF;

				// process shift register if needed
				if ((lastWaveClock & 0x100000) != (waveClock[index] & 0x100000))
					ProcessShiftRegister(index);
			}
		}

		private void ProcessEnvelope(int index)
		{
			envRateCounter[index] = (envRateCounter[index] + 1) & 0xFFFF;
			if (envRateCounter[index] == envRate[index])
			{
				envRateCounter[index] = 0;
				if (envState[index] != SidEnvelopeState.Disabled)
				{
					envExpCounter[index] = (envExpCounter[index] + 1) & 0xFF;
					if (envExpCounter[index] == 0)
						envState[index] = SidEnvelopeState.Disabled;

					if (envExpCounter[index] == envExp[index])
					{
						switch (envState[index])
						{
							case SidEnvelopeState.Attack:
								if (voices[index].ENV < 0xFF)
									voices[index].ENV++;
								if (voices[index].ENV == 0xFF)
								{
									envState[index] = SidEnvelopeState.Decay;
									UpdateEnvelopeRateCounter(index);
								}
								break;
							case SidEnvelopeState.Decay:
								if (voices[index].ENV > sustainLevels[voices[index].STN] && voices[index].ENV > 0)
									voices[index].ENV--;
								break;
							case SidEnvelopeState.Release:
								if (voices[index].ENV > 0)
									voices[index].ENV--;
								if (voices[index].ENV == 0)
								{
									envState[index] = SidEnvelopeState.Disabled;
									UpdateEnvelopeRateCounter(index);
								}
								break;
						}
						envExpCounter[index] = 0;
						ProcessEnvelopeExpCounter(index);
					}
					else if (envState[index] == SidEnvelopeState.Attack)
					{
						ProcessEnvelopeExpCounter(index);
					}
				}
			}

		}

		private void ProcessEnvelopeExpCounter(int index)
		{
			switch (voices[index].ENV)
			{
				case 0xFF:
					envExp[index] = 1;
					break;
				case 0x5D:
					envExp[index] = 2;
					break;
				case 0x36:
					envExp[index] = 4;
					break;
				case 0x1A:
					envExp[index] = 8;
					break;
				case 0x0E:
					envExp[index] = 16;
					break;
				case 0x06:
					envExp[index] = 30;
					break;
				case 0x00:
					envExp[index] = 1;
					break;
			}
		}

		private void ProcessShiftRegister(int index)
		{
			int newBit = ((voices[index].SR >> 22) ^ (voices[index].SR >> 17)) & 0x1;
			voices[index].SR = ((voices[index].SR << 1) | newBit) & 0x7FFFFF;
		}

		private void ProcessVoice(int index)
		{
			int triOutput;
			int sawOutput;
			int squOutput;
			int noiseOutput;
			int finalOutput = 0x00000FFF;
			bool outputEnabled = false;

			// triangle waveform
			if (voices[index].TRI)
			{
				triOutput = (waveClock[index] >> 12) & 0xFFF;
				if (voices[index].SYNC)
					triOutput ^= voices[syncIndex[index]].OSC & 0x800;

				if ((triOutput & 0x800) != 0x000)
					triOutput ^= 0x7FF;

				triOutput &= 0x7FF;
				triOutput <<= 1;
				finalOutput &= triOutput;
				outputEnabled = true;
			}

			// saw waveform
			if (voices[index].SAW)
			{
				sawOutput = (waveClock[index] >> 12) & 0xFFF;
				finalOutput &= sawOutput;
				outputEnabled = true;
			}

			// square waveform
			if (voices[index].SQU)
			{
				if (voices[index].TEST)
				{
					squOutput = 0xFFF;
				}
				else
				{
					squOutput = (waveClock[index] >> 12) >= voices[index].PW ? 0xFFF : 0x000;
				}
				finalOutput &= squOutput;
				outputEnabled = true;
			}

			// noise waveform
			if (voices[index].NOISE)
			{
				// shift register information is from reSID
				int sr = voices[index].SR;
				noiseOutput = sr & 0x100000 >> 9;
				noiseOutput |= sr & 0x040000 >> 8;
				noiseOutput |= sr & 0x004000 >> 5;
				noiseOutput |= sr & 0x000800 >> 3;
				noiseOutput |= sr & 0x000200 >> 2;
				noiseOutput |= sr & 0x000020 << 1;
				noiseOutput |= sr & 0x000004 << 3;
				noiseOutput |= sr & 0x000001 << 4;
				finalOutput &= noiseOutput;
				outputEnabled = true;

				// other waveforms write into the shift register
				if (voices[index].SQU || voices[index].TRI || voices[index].SAW)
					WriteShiftRegister(index, finalOutput);
			}

			// write to internal reg
			if (outputEnabled)
				voices[index].OSC = finalOutput;
			else
				voices[index].OSC = 0x000;
		}

		public byte Read(ushort addr)
		{
			addr &= 0x1F;
			switch (addr)
			{
				case 0x19:
				case 0x1A:
				case 0x1B:
				case 0x1C:
					// can only read these regs
					return regs[addr];
				default:
					return 0;
			}
		}

		private void UpdateEnvelopeRateCounter(int index)
		{
			switch (envState[index])
			{
				case SidEnvelopeState.Attack:
					envRate[index] = envRateIndex[voices[index].ATK] / 3;
					break;
				case SidEnvelopeState.Decay:
					envRate[index] = envRateIndex[voices[index].DCY];
					break;
				case SidEnvelopeState.Release:
					envRate[index] = envRateIndex[voices[index].RLS];
					break;
			}
			ProcessEnvelopeExpCounter(index);
		}

		public void Write(ushort addr, byte val)
		{
			int index;
			bool gate;

			addr &= 0x1F;
			switch (addr)
			{
				case 0x04:
				case 0x0B:
				case 0x12:
					// set control
					index = addr / 7;
					gate = lastGate[index];
					regs[addr] = val;
					lastGate[index] = voices[index].GATE;
					if (!gate && lastGate[index])
					{
						envExpCounter[index] = 0;
						envState[index] = SidEnvelopeState.Attack;
						voices[index].ENV = 0;
						envRateCounter[index] = 0;
						UpdateEnvelopeRateCounter(index);
						ProcessEnvelopeExpCounter(index);
					}
					else if (gate && !lastGate[index])
					{
						envExpCounter[index] = 0;
						envState[index] = SidEnvelopeState.Release;
						UpdateEnvelopeRateCounter(index);
					}
					break;
				case 0x05:
				case 0x0C:
				case 0x13:
					// set attack/decay
					index = addr / 7;
					regs[addr] = val;
					UpdateEnvelopeRateCounter(index);
					break;
				case 0x06:
				case 0x0D:
				case 0x14:
					// set sustain/release
					index = addr / 7;
					regs[addr] = val;
					UpdateEnvelopeRateCounter(index);
					break;
				case 0x19:
				case 0x1A:
				case 0x1B:
				case 0x1C:
					// can't write these regs
					break;
				default:
					regs[addr] = val;
					break;
			}
		}

		private void WriteShiftRegister(int index, int sample)
		{
			voices[index].SR &=
				~((1 << 20) | (1 << 18) | (1 << 14) | (1 << 11) | (1 << 9) | (1 << 5) | (1 << 2) | (1 << 0)) |
				((sample & 0x800) << 9) |
				((sample & 0x400) << 8) |
				((sample & 0x200) << 5) |
				((sample & 0x100) << 3) |
				((sample & 0x080) << 2) |
				((sample & 0x040) >> 1) |
				((sample & 0x020) >> 3) |
				((sample & 0x010) >> 4);
		}
	}
}
