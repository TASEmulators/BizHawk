using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
    sealed public partial class MOS6526_2
    {
		int read_data;

		public byte Peek(int addr)
		{
			addr &= 0xF;
			switch (addr)
			{
				case 0x0:
					return (byte)(portA.ReadInput(ReadPortA()) & PortAMask);
				case 0x1:
					return (byte)(portB.ReadInput(ReadPortB()) & PortBMask);
				case 0x2:
					return (byte)portA.Direction;
				case 0x3:
					return (byte)portB.Direction;
				case 0x4:
					return (byte)(a.getTimer() & 0xFF);
				case 0x5:
					return (byte)((a.getTimer() >> 8) & 0xFF);
				case 0x6:
					return (byte)(b.getTimer() & 0xFF);
				case 0x7:
					return (byte)((b.getTimer() >> 8) & 0xFF);
				case 0x8:
				case 0x9:
				case 0xA:
				case 0xB:
					return (byte)(tod_clock[addr - 0x08] & 0xFF);
				case 0xC:
					return (byte)(sdr & 0xFF);
				case 0xD:
					return (byte)(idr & 0xFF);
				case 0xE:
					return (byte)((cra & 0xEE) | (a.state & 1));
				case 0xF:
					return (byte)((crb & 0xEE) | (b.state & 1));
			}
			return 0xFF;
		}

		public void Poke(int addr, byte val)
		{
			// TODO
		}

		public byte Read(int addr)
		{
			return Read(addr, 0xFF);
		}

		public byte Read(int addr, byte mask)
		{
			addr &= 0xF;
			switch (addr)
			{
				case 0x0:
					return (byte)(portA.ReadInput(ReadPortA()) & PortAMask);
				case 0x1:
					read_data = portB.ReadInput(ReadPortB()) & PortBMask;
					if ((cra & 0x02) != 0)
					{
						read_data &= 0xBF;
						if (((cra & 0x04) != 0) ? (a.getPbToggle()) : ((a.state & CiaTimer.TIMER_OUT) != 0))
						{
							read_data |= 0x40;
						}
					}
					if ((crb & 0x02) != 0)
					{
						read_data &= 0x7F;
						if (((crb & 0x04) != 0) ? (b.getPbToggle()) : ((b.state & CiaTimer.TIMER_OUT) != 0))
						{
							read_data |= 0x80;
						}
					}
					return (byte)(read_data & 0xFF);
				case 0x2:
					return (byte)portA.Direction;
				case 0x3:
					return (byte)portB.Direction;
				case 0x4:
					return (byte)(a.getTimer() & 0xFF);
				case 0x5:
					return (byte)((a.getTimer() >> 8) & 0xFF);
				case 0x6:
					return (byte)(b.getTimer() & 0xFF);
				case 0x7:
					return (byte)((b.getTimer() >> 8) & 0xFF);
				case 0x8:
				case 0x9:
				case 0xA:
				case 0xB:
					if (!tod_latched)
					{
						tod_latch[0] = tod_clock[0];
						tod_latch[1] = tod_clock[1];
						tod_latch[2] = tod_clock[2];
						tod_latch[3] = tod_clock[3];
					}
					if (addr == 0x8)
					{
						tod_latched = false;
					}
					else if (addr == 0xB)
					{
						tod_latched = true;
					}
					return (byte)(tod_latch[addr - 0x08] & 0xFF);
				case 0xC:
					return (byte)(sdr & 0xFF);
				case 0xD:
					int_clear();
					return (byte)(icr & 0xFF);
				case 0xE:
					return (byte)((cra & 0xEE) | (a.state & 1));
				case 0xF:
					return (byte)((crb & 0xEE) | (b.state & 1));
			}
			return 0xFF;
		}

		public bool ReadCNTBuffer()
		{
			return true;
		}

		public bool ReadPCBuffer()
		{
			return true;
		}

		public bool ReadSPBuffer()
		{
			return true;
		}

		public void Write(int addr, byte val)
		{
			Write(addr, val, 0xFF);
		}

		public void Write(int addr, byte val, byte mask)
		{
			addr &= 0xF;
			switch (addr)
			{
				case 0x0:
					portA.Latch = val;
					pra = val;
					break;
				case 0x1:
					portB.Latch = val;
					prb = val;
					break;
				case 0x2:
					portA.Direction = val;
					ddra = val;
					break;
				case 0x3:
					portB.Direction = val;
					ddrb = val;
					break;
				case 0x4:
					a.setLatchLow(val);
					ta = ((int)val | (ta & 0xFF00));
					break;
				case 0x5:
					a.setLatchHigh(val);
					ta = (((int)val << 8) | (ta & 0xFF));
					break;
				case 0x6:
					b.setLatchLow(val);
					tb = ((int)val | (tb & 0xFF00));
					break;
				case 0x7:
					b.setLatchHigh(val);
					tb = (((int)val << 8) | (tb & 0xFF));
					break;
				case 0x8:
				case 0x9:
				case 0xA:
				case 0xB:
					if (addr == 0xB)
					{
						val &= 0x9F;
						if ((val & 0x1F) == 0x12 && (crb & 0x80) == 0)
						{
							val ^= 0x80;
						}
					}
					if ((crb & 0x80) != 0)
					{
						tod_alarm[addr - 0x8] = val;
					}
					else
					{
						if (addr == 0x8)
						{
							tod_stopped = false;
						}
						else if (addr == 0xB)
						{
							tod_stopped = true;
						}
					}
					tod_clock[addr - 0x8] = val;
					break;
				case 0xC:
					if ((cra & 0x40) != 0)
					{
						sdr_buffered = true;
					}
					break;
				case 0xD:
					if ((val & 0x80) != 0)
					{
						int_setEnabled(val);
					}
					else
					{
						int_clearEnabled(val);
					}
					break;
				case 0xE:
					if ((val & 0x1) != 0 && (cra & 0x1) == 0)
					{
						a.setPbToggle(true);
					}
					a.setControlRegister(val);
					break;
				case 0xF:
					if ((val & 0x1) != 0 && (crb & 0x1) == 0)
					{
						b.setPbToggle(true);
					}
					b.setControlRegister((val | (val & 0x40) >> 1));
					break;
			}
		}
	}
}
