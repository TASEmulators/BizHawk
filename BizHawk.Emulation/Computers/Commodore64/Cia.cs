using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class CiaRegs
	{
		public bool ALARM; // alarm enabled
		public int ALARM10; // alarm 10ths of a second
		public int ALARMHR; // alarm hours
		public int ALARMMIN; // alarm minutes
		public bool ALARMPM; // alarm AM/PM
		public int ALARMSEC; // alarm seconds
		public bool CNT; // external counter bit input
		public bool EIALARM; // enable alarm interrupt (internal)
		public bool EIFLG; // enable flag pin interrupt (internal)
		public bool EISP; // enable shift register interrupt (internal)
		public bool[] EIT = new bool[2]; // enable timer interrupt (internal)
		public bool FLG; // external flag bit input
		public bool IALARM; // alarm interrupt triggered
		public bool IFLG; // interrupt triggered on FLAG pin
		public int[] INMODE = new int[2]; // timer input mode
		public bool IRQ; // interrupt triggered
		public bool ISP; // shift register interrupt
		public bool[] IT = new bool[2]; // timer interrupt
		public bool[] LOAD = new bool[2]; // force load timer
		public bool[] OUTMODE = new bool[2]; // timer output mode
		public bool[] PBON = new bool[2]; // port bit modify on
		public bool[] RUNMODE = new bool[2]; // running mode
		public int SDR; // serial shift register
		public int SDRCOUNT; // serial shift register bit count
		public bool SPMODE; // shift register mode
		public bool[] START = new bool[2]; // timer enabled
		public int[] T = new int[2]; // timer counter
		public bool[] TICK = new bool[2]; // execute timer tick
		public int[] TLATCH = new int[2]; // timer latch (internal)
		public int TOD10; // time of day 10ths of a second
		public bool TODIN; // time of day/alarm set
		public int TODHR; // time of day hour
		public int TODMIN; // time of day minute
		public bool TODPM; // time of day AM/PM
		public bool TODREADLATCH; // read latch (internal)
		public int TODREADLATCH10; // tod read latch (internal)
		public int TODREADLATCHSEC; // tod read latch (internal)
		public int TODREADLATCHMIN; // tod read latch (internal)
		public int TODREADLATCHHR; // tod read latch (internal)
		public int TODSEC; // time of day seconds

		public DataPortBus[] ports;

		private DataPortConnector[] connectors;

		public CiaRegs()
		{
			// power on state
			TLATCH[0] = 0xFFFF;
			TLATCH[1] = 0xFFFF;
			T[0] = TLATCH[0];
			T[1] = TLATCH[1];

			this[0x0B] = 0x01;

			ports = new DataPortBus[2];
			ports[0] = new DataPortBus();
			ports[1] = new DataPortBus();
			connectors = new DataPortConnector[2];
			connectors[0] = ports[0].Connect();
			connectors[1] = ports[1].Connect();
			connectors[0].Data = 0xFF;
			connectors[1].Data = 0xFF;
			connectors[0].Direction = 0xFF;
			connectors[1].Direction = 0xFF;
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
						result = connectors[0].Data;
						break;
					case 0x01:
						result = connectors[1].Data;
						break;
					case 0x02:
						result = connectors[0].Direction;
						break;
					case 0x03:
						result = connectors[1].Direction;
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
						connectors[0].Data = val;
						break;
					case 0x01:
						connectors[1].Data = val;
						break;
					case 0x02:
						connectors[0].Direction = val;
						break;
					case 0x03:
						connectors[1].Direction = val;
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
		public bool lastCNT;
		public byte[] outputBitMask;
		private CiaRegs regs;
		public int todCounter;
		public int todFrequency;
		public bool[] underflow;

		public Func<bool> ReadSerial;
		public Action<bool> WriteSerial;

		public Cia(Region newRegion)
		{
			ReadSerial = ReadSerialDummy;
			WriteSerial = WriteSerialDummy;
			switch (newRegion)
			{
				case Region.NTSC:
					todFrequency = 14318181 / 14 / 10;
					break;
				case Region.PAL:
					todFrequency = 14318181 / 18 / 10;
					break;
			}
			HardReset();
		}

		private void AdvanceTOD()
		{
			bool overflow;
			int tenths = regs.TOD10;
			int seconds = regs.TODSEC;
			int minutes = regs.TODMIN;
			int hours = regs.TODHR;
			bool ampm = regs.TODPM;
			todCounter = todFrequency;

			tenths = BCDAdd(tenths, 1, out overflow);
			if (tenths >= 10)
			{
				tenths = 0;
				seconds = BCDAdd(seconds, 1, out overflow);
				if (overflow)
				{
					seconds = 0;
					minutes = BCDAdd(minutes, 1, out overflow);
					if (overflow)
					{
						minutes = 0;
						hours = BCDAdd(hours, 1, out overflow);
						if (hours > 12)
						{
							hours = 1;
							ampm = !ampm;
						}
					}
				}
			}

			regs.TOD10 = tenths;
			regs.TODSEC = seconds;
			regs.TODMIN = minutes;
			regs.TODHR = hours;
			regs.TODPM = ampm;
		}

		public void AttachWriteHook(int index, Action act)
		{
			regs.ports[index].AttachWriteHook(act);
		}

		private int BCDAdd(int i, int j, out bool overflow)
		{
			int lo;
			int hi;
			int result;

			lo = (i & 0x0F) + (j & 0x0F);
			hi = (i & 0x70) + (j & 0x70);
			if (lo > 0x09)
			{
				hi += 0x10;
				lo += 0x06;
			}
			if (hi > 0x50)
			{
				hi += 0xA0;
			}
			overflow = hi >= 0x60;
			result = (hi & 0x70) + (lo & 0x0F);
			return result;
		}

		public DataPortConnector ConnectPort(int index)
		{
			return regs.ports[index].Connect();
		}

		public DataPortConnector ConnectSerialPort(int index)
		{
			DataPortConnector result = regs.ports[index].Connect();
			regs.ports[index].AttachInputConverter(result, new DataPortSerialInputConverter());
			regs.ports[index].AttachOutputConverter(result, new DataPortSerialOutputConverter());
			return result;
		}

		public void HardReset()
		{
			outputBitMask = new byte[] { 0x40, 0x80 };
			regs = new CiaRegs();
			underflow = new bool[2];
			todCounter = todFrequency;
		}

		public bool IRQ
		{
			get
			{
				return regs.IRQ;
			}
		}

		public byte Peek(int addr)
		{
			addr &= 0xF;
			return regs[addr];
		}

		public void PerformCycle()
		{
			// process time of day counter
			todCounter--;
			if (todCounter <= 0)
				AdvanceTOD();

			for (int i = 0; i < 2; i++)
			{
				if (regs.START[i])
				{
					TimerTick(i);
					if (regs.PBON[i])
					{
						// output the clock data to port B

						if (regs.OUTMODE[i])
						{
							// clear bit if set
							regs[0x01] &= (byte)~outputBitMask[i];
						}
						if (underflow[i])
						{
							if (regs.OUTMODE[i])
							{
								// toggle bit
								regs[0x01] ^= outputBitMask[i];
							}
							else
							{
								// set for a cycle
								regs[0x01] |= outputBitMask[i];
							}
						}
					}
				}
			}

			lastCNT = regs.CNT;
			regs.CNT = false;
			UpdateInterrupt();
		}

		public void Poke(int addr, byte val)
		{
			addr &= 0xF;
			regs[addr] = val;
		}

		public byte Read(ushort addr)
		{
			byte result;
			addr &= 0xF;

			switch (addr)
			{
				case 0x08:
					regs.TODREADLATCH = false;
					return (byte)regs.TODREADLATCH10;
				case 0x09:
					if (!regs.TODREADLATCH)
						return regs[addr];
					else
						return (byte)regs.TODREADLATCHSEC;
				case 0x0A:
					if (!regs.TODREADLATCH)
						return regs[addr];
					else
						return (byte)regs.TODREADLATCHMIN;
				case 0x0B:
					regs.TODREADLATCH = true;
					regs.TODREADLATCH10 = regs.TOD10;
					regs.TODREADLATCHSEC = regs.TODSEC;
					regs.TODREADLATCHMIN = regs.TODMIN;
					regs.TODREADLATCHHR = regs.TODHR;
					return (byte)regs.TODREADLATCHHR;
				case 0x0D:
					// reading this reg clears it
					result = regs[0x0D];
					regs[0x0D] = 0x00;
					UpdateInterrupt();
					return result;
				default:
					return regs[addr];
			}
		}

		private bool ReadSerialDummy()
		{
			return false;
		}

		public void TimerDec(int index)
		{
			int timer = regs.T[index];
			timer--;
			if (timer < 0)
			{
				underflow[index] = true;
				if (regs.RUNMODE[index])
				{
					// one shot timer
					regs.START[index] = false;
				}
				timer = regs.TLATCH[index];
			}
			else
			{
				underflow[index] = false;
			}

			regs.IT[index] |= underflow[index];
			regs.T[index] = timer;
		}

		public void TimerTick(int index)
		{
			switch (regs.INMODE[index])
			{
				case 0:
					regs.TICK[index] = true;
					break;
				case 1:
					regs.TICK[index] |= (regs.CNT && !lastCNT);
					break;
				case 2:
					regs.TICK[index] |= underflow[0];
					break;
				case 3:
					regs.TICK[index] |= (regs.CNT && !lastCNT) || underflow[0];
					break;
			}
			if (regs.TICK[index])
			{
				TimerDec(index);
				regs.TICK[index] = false;
			}
		}

		public void UpdateInterrupt()
		{
			bool irq = false;
			irq |= (regs.EIT[0] & regs.IT[0]);
			irq |= (regs.EIT[1] & regs.IT[1]);
			irq |= (regs.EIFLG & regs.IFLG);
			irq |= (regs.EISP & regs.ISP);
			irq |= (regs.EIALARM & regs.IALARM);
			regs.IRQ = irq;
		}

		public void Write(ushort addr, byte val)
		{
			addr &= 0xF;
			switch (addr)
			{
				case 0x04:
					regs.TLATCH[0] &= 0xFF00;
					regs.TLATCH[0] |= val;
					if (regs.LOAD[0])
						regs.T[0] = regs.TLATCH[0];
					break;
				case 0x05:
					regs.TLATCH[0] &= 0xFF;
					regs.TLATCH[0] |= (int)val << 8;
					if (regs.LOAD[0] || !regs.START[0])
						regs.T[0] = regs.TLATCH[0];
					break;
				case 0x06:
					regs.TLATCH[1] &= 0xFF00;
					regs.TLATCH[1] |= val;
					if (regs.LOAD[1])
						regs.T[1] = regs.TLATCH[1];
					break;
				case 0x07:
					regs.TLATCH[1] &= 0xFF;
					regs.TLATCH[1] |= (int)val << 8;
					if (regs.LOAD[1] || !regs.START[1])
						regs.T[1] = regs.TLATCH[1];
					break;
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
					{
						regs[addr] = val;
					}
					break;
				case 0x0D:
					intMask &= ~val;
					if ((val & 0x80) != 0x00)
						intMask ^= val;
					regs.EIT[0] = ((intMask & 0x01) != 0x00);
					regs.EIT[1] = ((intMask & 0x02) != 0x00);
					regs.EIALARM = ((intMask & 0x04) != 0x00);
					regs.EISP = ((intMask & 0x08) != 0x00);
					regs.EIFLG = ((intMask & 0x10) != 0x00);
					UpdateInterrupt();
					break;
				default:
					regs[addr] = val;
					break;
			}
			
		}

		public void WriteCNT(bool val)
		{
			if (!lastCNT && val)
			{
				if (!regs.SPMODE)
				{
					// read bit into shift register
					bool inputBit = ReadSerial();
					regs.SDR = ((regs.SDR << 1) | (inputBit ? 0x01 : 0x00)) & 0xFF;

					regs.SDRCOUNT = (regs.SDRCOUNT - 1) & 0x7;
					if (regs.SDRCOUNT == 0)
					{
						regs.ISP = true;
					}
				}
				else
				{
					// write bit out from shift register
					bool outputBit = ((regs.SDR & 0x01) != 0);
					regs.SDR >>= 1;
					WriteSerial(outputBit);

					regs.SDRCOUNT = (regs.SDRCOUNT - 1) & 0x7;
					if (regs.SDRCOUNT == 0)
					{
						regs.ISP = true;
					}
				}
			}
			regs.CNT = val;
		}

		public void WriteFLG(bool val)
		{
			regs.FLG = val;
			regs.IFLG |= val;
		}

		private void WriteSerialDummy(bool val)
		{
			// do nothing
		}
	}
}
