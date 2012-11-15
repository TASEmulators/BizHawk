using System;
using System.Collections.Generic;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public enum SidMode
	{
		Sid6581,
		Sid8580
	}

	public class VoiceRegs
	{
		public EnvelopeGenerator Envelope;
		public WaveformGenerator Generator;
		public WaveformGenerator SyncSource;

		public int EnvelopeLatch;
		public int GeneratorLatch;

		public VoiceRegs()
		{
			Envelope = new EnvelopeGenerator();
			Generator = new WaveformGenerator(WaveformCalculator.BuildTable());
		}

		public void Clock()
		{
			Generator.Clock();
			GeneratorLatch = Generator.Output(SyncSource);
			Envelope.Clock();
			EnvelopeLatch = Envelope.Output;
		}

		public short Output()
		{
			int result = (GeneratorLatch * EnvelopeLatch) >> 4;
			result -= 32768;
			if (result > 32767)
				result = 32767;
			else if (result < -32768)
				result = -32768;
			return (short)result;
		}

		public void Reset()
		{
			Generator.Reset();
			Envelope.Reset();
		}
	}

	public class SidRegs
	{
		public bool BP;
		public bool D3;
		public int FC;
		public bool[] FILT = new bool[3];
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
			Voices[0] = new VoiceRegs();
			Voices[1] = new VoiceRegs();
			Voices[2] = new VoiceRegs();

			Voices[0].SyncSource = Voices[2].Generator;
			Voices[1].SyncSource = Voices[0].Generator;
			Voices[2].SyncSource = Voices[1].Generator;
		}

		public byte this[int addr]
		{

			get
			{
				int result;

				addr &= 0x1F;
				switch (addr)
				{
					case 0x19:
						result = POTX;
						break;
					case 0x1A:
						result = POTY;
						break;
					case 0x1B:
						result = Voices[2].GeneratorLatch >> 4;
						break;
					case 0x1C:
						result = Voices[2].EnvelopeLatch;
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
						Voices[index].Generator.WriteFreqLo(value);
						break;
					case 0x01:
					case 0x08:
					case 0x0F:
						index = addr / 7;
						Voices[index].Generator.WriteFreqHi(value);
						break;
					case 0x02:
					case 0x09:
					case 0x10:
						index = addr / 7;
						Voices[index].Generator.WritePWLo(value);
						break;
					case 0x03:
					case 0x0A:
					case 0x11:
						index = addr / 7;
						Voices[index].Generator.WritePWHi(value);
						break;
					case 0x04:
					case 0x0B:
					case 0x12:
						index = addr / 7;
						Voices[index].Generator.WriteControl(value);
						Voices[index].Envelope.Gate = ((value & 0x01) != 0x00);
						break;
					case 0x05:
					case 0x0C:
					case 0x13:
						index = addr / 7;
						Voices[index].Envelope.Attack = (value >> 4);
						Voices[index].Envelope.Decay = (value & 0xF);
						break;
					case 0x06:
					case 0x0D:
					case 0x14:
						index = addr / 7;
						Voices[index].Envelope.Sustain = (value >> 4);
						Voices[index].Envelope.Release = (value & 0xF);
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
				}
			}
		}

	}

	public partial class Sid : ISoundProvider
	{
		private int[] syncIndex = { 2, 0, 1 };

		private VoiceRegs[] voices;

		public Func<int> ReadPotX;
		public Func<int> ReadPotY;

		public int clock;
		public int cyclesPerSample;
		public int output;
		public SidRegs regs;

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

		public byte Peek(int addr)
		{
			return regs[addr & 0x1F];
		}

		public void PerformCycle()
		{
			// process each voice
			voices[0].Clock();
			voices[1].Clock();
			voices[2].Clock();
			SubmitSample();

			// query pots every 512 cycles
			if ((clock & 0x1FF) == 0x000)
			{
				regs.POTX = ReadPotX() & 0xFF;
				regs.POTY = ReadPotY() & 0xFF;
			}

			clock = (clock + 1) & 0xFFFFFF;
		}

		public void Poke(int addr, byte val)
		{
			regs[addr & 0x1F] = val;
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
	}
}
