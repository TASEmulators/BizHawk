using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
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
		public int ENV3;
		public int[] F = new int[3];
		public int FC;
		public bool[] FILT = new bool[3];
		public bool FILTEX;
		public bool[] GATE = new bool[3];
		public bool HP;
		public bool LP;
		public bool[] NOISE = new bool[3];
		public int OSC3;
		public int POTX;
		public int POTY;
		public int[] PW = new int[3];
		public int RES;
		public int[] RLS = new int[3];
		public bool[] RMOD = new bool[3];
		public bool[] SAW = new bool[3];
		public bool[] SQU = new bool[3];
		public int[] STN = new int[3];
		public bool[] SYNC = new bool[3];
		public bool[] TEST = new bool[3];
		public bool[] TRI = new bool[3];
		public int VOL;

		public SidRegs()
		{
			// power on state
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
						result = OSC3;
						break;
					case 0x1C:
						result = ENV3;
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
						PW[index] &= 0x0700;
						PW[index] |= val;
						break;
					case 0x03:
					case 0x0A:
					case 0x11:
						index = addr / 7;
						F[index] &= 0xFF;
						F[index] |= (val & 0x07) << 8;
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
						OSC3 = val;
						break;
					case 0x1C:
						ENV3 = val;
						break;
				}
			}
		}

	}

	public class Sid
	{
		public Func<int> ReadPotX;
		public Func<int> ReadPotY;

		public int output;
		public int potCycle;
		public SidRegs regs;

		public Sid()
		{
			ReadPotX = DummyReadPot;
			ReadPotY = DummyReadPot;
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

		public void PerformCycle()
		{
			output = 0;
			if (potCycle == 0)
			{
				regs.POTX = ReadPotX() & 0xFF;
				regs.POTY = ReadPotY() & 0xFF;
			}
			potCycle = (potCycle + 1) & 0x1FF;
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
