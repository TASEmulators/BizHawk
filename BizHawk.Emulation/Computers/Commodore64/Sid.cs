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

	public class SidRegs
	{
		public int[] ATK = new int[3];
		public bool BP;
		public bool D3;
		public int[] DCY = new int[3];
		public int[] ENV = new int[3];
		public int[] F = new int[3];
		public int FC;
		public bool[] FILT = new bool[3];
		public bool FILTEX;
		public bool[] GATE = new bool[3];
		public bool HP;
		public bool LP;
		public bool[] NOISE = new bool[3];
		public int[] OSC = new int[3];
		public int POTX;
		public int POTY;
		public int[] PW = new int[3];
		public int RES;
		public int[] RLS = new int[3];
		public bool[] RMOD = new bool[3];
		public bool[] SAW = new bool[3];
		public int[] SR = new int[3];
		public bool[] SQU = new bool[3];
		public int[] STN = new int[3];
		public bool[] SYNC = new bool[3];
		public bool[] TEST = new bool[3];
		public bool[] TRI = new bool[3];
		public int VOL;

		public SidRegs()
		{
			// power on state
			SR[0] = 0x7FFFFF;
			SR[1] = 0x7FFFFF;
			SR[2] = 0x7FFFFF;
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
						result = F[addr / 7] & 0xFF;
						break;
					case 0x01:
					case 0x08:
					case 0x0F:
						result = (F[addr / 7] & 0xFF00) >> 8;
						break;
					case 0x02:
					case 0x09:
					case 0x10:
						result = PW[addr / 7] & 0xFF;
						break;
					case 0x03:
					case 0x0A:
					case 0x11:
						result = (PW[addr / 7] & 0x0F00) >> 8;
						break;
					case 0x04:
					case 0x0B:
					case 0x12:
						index = addr / 7;
						result = GATE[index] ? 0x01 : 0x00;
						result |= SYNC[index] ? 0x02 : 0x00;
						result |= RMOD[index] ? 0x04 : 0x00;
						result |= TEST[index] ? 0x08 : 0x00;
						result |= TRI[index] ? 0x10 : 0x00;
						result |= SAW[index] ? 0x20 : 0x00;
						result |= SQU[index] ? 0x40 : 0x00;
						result |= NOISE[index] ? 0x80 : 0x00;
						break;
					case 0x05:
					case 0x0C:
					case 0x13:
						index = addr / 7;
						result = (ATK[index] & 0xF) << 4;
						result |= DCY[index] & 0xF;
						break;
					case 0x06:
					case 0x0D:
					case 0x14:
						index = addr / 7;
						result = (STN[index] & 0xF) << 4;
						result |= RLS[index] & 0xF;
						break;
					case 0x15:
						result = FC & 0x7;
						break;
					case 0x16:
						result = (FC & 0x7F8) >> 3;
						break;
					case 0x17:
						result = FILT[0] ? 0x01 : 0x00;
						result |= FILT[1] ? 0x02 : 0x00;
						result |= FILT[2] ? 0x04 : 0x00;
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
						result = OSC[2] >> 4;
						break;
					case 0x1C:
						result = ENV[2];
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
						F[index] &= 0xFF00;
						F[index] |= val;
						break;
					case 0x01:
					case 0x08:
					case 0x0F:
						index = addr / 7;
						F[index] &= 0xFF;
						F[index] |= val << 8;
						break;
					case 0x02:
					case 0x09:
					case 0x10:
						index = addr / 7;
						PW[index] &= 0x0F00;
						PW[index] |= val;
						break;
					case 0x03:
					case 0x0A:
					case 0x11:
						index = addr / 7;
						PW[index] &= 0xFF;
						PW[index] |= (val & 0x0F) << 8;
						break;
					case 0x04:
					case 0x0B:
					case 0x12:
						index = addr / 7;
						GATE[index] = ((val & 0x01) != 0x00);
						SYNC[index] = ((val & 0x02) != 0x00);
						RMOD[index] = ((val & 0x04) != 0x00);
						TEST[index] = ((val & 0x08) != 0x00);
						TRI[index] = ((val & 0x10) != 0x00);
						SAW[index] = ((val & 0x20) != 0x00);
						SQU[index] = ((val & 0x40) != 0x00);
						NOISE[index] = ((val & 0x80) != 0x00);
						break;
					case 0x05:
					case 0x0C:
					case 0x13:
						index = addr / 7;
						ATK[index] = (val >> 4) & 0xF;
						DCY[index] = val & 0xF;
						break;
					case 0x06:
					case 0x0D:
					case 0x14:
						index = addr / 7;
						STN[index] = (val >> 4) & 0xF;
						RLS[index] = val & 0xF;
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
						FILT[0] = ((val & 0x01) != 0x00);
						FILT[1] = ((val & 0x02) != 0x00);
						FILT[2] = ((val & 0x04) != 0x00);
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
						OSC[2] = val << 4;
						break;
					case 0x1C:
						ENV[2] = val;
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

		private int[] syncIndex = { 2, 0, 1 };

		public Func<int> ReadPotX;
		public Func<int> ReadPotY;

		public int clock;
		public int cyclesPerSample;
		public bool[] envEnable = new bool[3];
		public int[] envExpCounter = new int[3];
		public int[] envRate = new int[3];
		public int[] envRateCounter = new int[3];
		public SidEnvelopeState[] envState = new SidEnvelopeState[3];
		public bool[] gateLastCycle = new bool[3];
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
		}

		private short Mix(int input, short mixSource)
		{
			input &= 0xFFF;
			input -= 0x800;
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

			ProcessVoice(0);
			ProcessVoice(1);
			ProcessVoice(2);

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

		private void ProcessEnvelope(int index)
		{
			// envelope counter is 15 bits
			envRateCounter[index] &= 0x7FFF;

			if (!gateLastCycle[index] && regs.GATE[index])
			{
				envState[index] = SidEnvelopeState.Attack;
				envEnable[index] = true;
			}
			else if (gateLastCycle[index] && !regs.GATE[index])
			{
				envState[index] = SidEnvelopeState.Release;
			}

			if (envRateCounter[index] == envRate[index])
			{
				envExpCounter[index] = 0;
				if (envEnable[index])
				{

				}
			}

			gateLastCycle[index] = regs.GATE[index];
		}

		private void ProcessShiftRegister(int index)
		{
			int newBit = ((regs.SR[index] >> 22) ^ (regs.SR[index] >> 17)) & 0x1;
			regs.SR[index] = ((regs.SR[index] << 1) | newBit) & 0x7FFFFF;
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
			if (regs.TRI[index])
			{
				triOutput = (waveClock[index] >> 12) & 0xFFF;
				if (regs.SYNC[index])
					triOutput ^= regs.OSC[syncIndex[index]] & 0x800;

				if ((triOutput & 0x800) != 0x000)
					triOutput ^= 0x7FF;

				triOutput &= 0x7FF;
				triOutput <<= 1;
				finalOutput &= triOutput;
				outputEnabled = true;
			}

			// saw waveform
			if (regs.SAW[index])
			{
				sawOutput = (waveClock[index] >> 12) & 0xFFF;
				finalOutput &= sawOutput;
				outputEnabled = true;
			}

			// square waveform
			if (regs.SQU[index])
			{
				if (regs.TEST[index])
				{
					squOutput = 0xFFF;
				}
				else
				{
					squOutput = (waveClock[index] >> 12) >= regs.PW[index] ? 0xFFF : 0x000;
				}
				finalOutput &= squOutput;
				outputEnabled = true;
			}

			// noise waveform
			if (regs.NOISE[index])
			{
				// shift register information is from reSID
				int sr = regs.SR[index];
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
			}

			// test bit resets the oscillator and silences output
			if (regs.TEST[index])
			{
				waveClock[index] = 0x000000;
				outputEnabled = false;
				regs.SR[index] = 0x7FFFFF;
			}
			else
			{
				// shift register for generating noise
				if ((waveClock[index] & 0x100000) != 0)
					ProcessShiftRegister(index);

				// increment wave clock
				waveClock[index] = (waveClock[index] + regs.F[index]) & 0x00FFFFFF;
			}

			// process the envelope generator
			//ProcessEnvelope(index);

			// a little hack until we fix the envelope generator
			outputEnabled = regs.GATE[index];


			// write to internal reg
			if (outputEnabled)
				regs.OSC[index] = finalOutput;
			else
				regs.OSC[index] = 0x000;
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
					envRate[index] = envRateIndex[regs.ATK[index]];
					break;
				case SidEnvelopeState.Decay:
					envRate[index] = envRateIndex[regs.DCY[index]];
					break;
				case SidEnvelopeState.Release:
					envRate[index] = envRateIndex[regs.RLS[index]];
					break;
			}
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
					// can't write these regs
					break;
				default:
					regs[addr] = val;
					break;
			}
		}

		private void WriteShiftRegister(int index, int sample)
		{
			regs.SR[index] &=
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
