using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class CiaRegs
	{
		public bool ALARM;
		public int ALARM10;
		public int ALARMHR;
		public int ALARMMIN;
		public bool ALARMPM;
		public int ALARMSEC;
		public int[] DOR = new int[2];
		public bool IALARM;
		public bool IFLG;
		public int[] INMODE = new int[2];
		public bool IRQ;
		public bool ISP;
		public bool[] IT = new bool[2];
		public bool[] LOAD = new bool[2];
		public bool[] OUTMODE = new bool[2];
		public bool[] PBON = new bool[2];
		public int[] PR = new int[2];
		public bool[] RUNMODE = new bool[2];
		public int SDR;
		public bool SPMODE;
		public bool[] START = new bool[2];
		public int[] T = new int[2];
		public int TOD10;
		public bool TODIN;
		public int TODHR;
		public int TODMIN;
		public bool TODPM;
		public int TODSEC;

		private DirectionalDataPort[] ports;
		private ChipSignals signal;

		public CiaRegs(ChipSignals newSignal, DirectionalDataPort[] newPorts)
		{
			ports = newPorts;
			signal = newSignal;

			// power on state
			this[0x04] = 0xFF;
			this[0x05] = 0xFF;
			this[0x06] = 0xFF;
			this[0x07] = 0xFF;
			this[0x0B] = 0x01;
		}

		public byte this[int addr]
		{
			get
			{
				// value of open bits
				int result = 0x00;

				addr &= 0x0F;
				switch (addr)
				{
					case 0x00:
						result = ports[0].Data;
						break;
					case 0x01:
						result = ports[1].Data;
						break;
					case 0x02:
						result = ports[0].Direction;
						break;
					case 0x03:
						result = ports[1].Direction;
						break;
					case 0x04:
						result = (T[0] & 0xFF);
						break;
					case 0x05:
						result = ((T[0] >> 8) & 0xFF);
						break;
					case 0x06:
						result = (T[1] & 0xFF);
						break;
					case 0x07:
						result = ((T[1] >> 8) & 0xFF);
						break;
					case 0x08:
						result |= (TOD10 & 0x0F);
						break;
					case 0x09:
						result &= 0x80;
						result |= (TODSEC & 0x7F);
						break;
					case 0x0A:
						result &= 0x80;
						result |= (TODMIN & 0x7F);
						break;
					case 0x0B:
						result &= 0x40;
						result |= ((TODHR & 0x3F) | (TODPM ? 0x80 : 0x00));
						break;
					case 0x0C:
						result = SDR;
						break;
					case 0x0D:
						result &= 0x9F;
						result |= (IT[0] ? 0x01 : 0x00);
						result |= (IT[1] ? 0x02 : 0x00);
						result |= (IALARM ? 0x04 : 0x00);
						result |= (ISP ? 0x08 : 0x00);
						result |= (IFLG ? 0x10 : 0x00);
						result |= (IRQ ? 0x80 : 0x00);
						break;
					case 0x0E:
						result = (START[0] ? 0x01 : 0x00);
						result = (PBON[0] ? 0x02 : 0x00);
						result = (OUTMODE[0] ? 0x04 : 0x00);
						result = (RUNMODE[0] ? 0x08 : 0x00);
						result = (LOAD[0] ? 0x10 : 0x00);
						result = ((INMODE[0] & 0x01) << 5);
						result = (SPMODE ? 0x40 : 0x00);
						result = (TODIN ? 0x80 : 0x00);
						break;
					case 0x0F:
						result = (START[1] ? 0x01 : 0x00);
						result = (PBON[1] ? 0x02 : 0x00);
						result = (OUTMODE[1] ? 0x04 : 0x00);
						result = (RUNMODE[1] ? 0x08 : 0x00);
						result = (LOAD[1] ? 0x10 : 0x00);
						result = ((INMODE[1] & 0x03) << 5);
						result = (ALARM ? 0x80 : 0x00);
						break;
				}

				return (byte)(result & 0xFF);
			}

			set
			{
				byte val = value;
				addr &= 0x0F;
				switch (addr)
				{
					case 0x00:
						ports[0].Data = val;
						break;
					case 0x01:
						ports[1].Data = val;
						break;
					case 0x02:
						ports[0].Direction = val;
						break;
					case 0x03:
						ports[1].Direction = val;
						break;
					case 0x04:
						T[0] &= 0xFF00;
						T[0] |= val;
						break;
					case 0x05:
						T[0] &= 0x00FF;
						T[0] |= ((int)val << 8);
						break;
					case 0x06:
						T[1] &= 0xFF00;
						T[1] |= val;
						break;
					case 0x07:
						T[1] &= 0x00FF;
						T[1] |= ((int)val << 8);
						break;
					case 0x08:
						TOD10 = val & 0x0F;
						break;
					case 0x09:
						TODSEC = val & 0x7F;
						break;
					case 0x0A:
						TODMIN = val & 0x7F;
						break;
					case 0x0B:
						val &= 0x9F;
						TODHR = val;
						TODPM = ((val & 0x80) != 0x00);
						break;
					case 0x0C:
						SDR = val;
						break;
					case 0x0D:
						IT[0] = ((val & 0x01) != 0x00);
						IT[1] = ((val & 0x02) != 0x00);
						IALARM = ((val & 0x04) != 0x00);
						ISP = ((val & 0x08) != 0x00);
						IFLG = ((val & 0x10) != 0x00);
						IRQ = ((val & 0x80) != 0x00);
						break;
					case 0x0E:
						START[0] = ((val & 0x01) != 0x00);
						PBON[0] = ((val & 0x02) != 0x00);
						OUTMODE[0] = ((val & 0x04) != 0x00);
						RUNMODE[0] = ((val & 0x08) != 0x00);
						LOAD[0] = ((val & 0x10) != 0x00);
						INMODE[0] = ((val & 0x20) >> 5);
						SPMODE = ((val & 0x40) != 0x00);
						TODIN = ((val & 0x80) != 0x00);
						break;
					case 0x0F:
						START[1] = ((val & 0x01) != 0x00);
						PBON[1] = ((val & 0x02) != 0x00);
						OUTMODE[1] = ((val & 0x04) != 0x00);
						RUNMODE[1] = ((val & 0x08) != 0x00);
						LOAD[1] = ((val & 0x10) != 0x00);
						INMODE[1] = ((val & 0x60) >> 5);
						ALARM = ((val & 0x80) != 0x00);
						break;
				}
			}
		}
	}

	public class Cia
	{
		public int intMask;
		public DirectionalDataPort[] ports;
		public CiaRegs regs;
		public ChipSignals signal;

		public Cia(ChipSignals newSignal)
		{
			signal = newSignal;
			HardReset();
		}

		public void HardReset()
		{
			ports = new DirectionalDataPort[2];
			regs = new CiaRegs(signal, ports);
		}

		public void PerformCycle()
		{
		}

		public byte Read(ushort addr)
		{
			byte result;

			switch (addr)
			{
				case 0x0D:
					// reading this reg clears it
					result = regs[0x0D];
					regs[0x0D] = 0x00;
					return result;
				default:
					return regs[addr];
			}
		}

		public void TimerTick(int index)
		{
		}

		public void Write(ushort addr, byte val)
		{
			switch (addr)
			{
				case 0x08:
					if (regs.ALARM)
						regs.ALARM10 = val & 0x0F;
					else
						regs[addr] = val;
					break;
				case 0x09:
					if (regs.ALARM)
						regs.ALARMSEC = val & 0x7F;
					else
						regs[addr] = val;
					break;
				case 0x0A:
					if (regs.ALARM)
						regs.ALARMMIN = val & 0x7F;
					else
						regs[addr] = val;
					break;
				case 0x0B:
					if (regs.ALARM)
					{
						regs.ALARMHR = val & 0x1F;
						regs.ALARMPM = ((val & 0x80) != 0x00);
					}
					else
						regs[addr] = val;
					break;
				case 0x0D:
					intMask &= ~val;
					if ((val & 0x80) != 0x00)
						intMask ^= val;
					break;
				default:
					regs[addr] = val;
					break;
			}
			
		}
	}
}
