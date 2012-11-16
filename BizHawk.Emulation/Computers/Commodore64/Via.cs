using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	// MOS Technologies VIA 6522
	// register count: 16
	// IO port count: 2

	public class ViaRegs
	{
		public int[] CACONTROL = new int[2];
		public int[] CBCONTROL = new int[2];
		public bool[] EICA = new bool[2];
		public bool[] EICB = new bool[2];
		public bool EISR;
		public bool[] EIT = new bool[2];
		public bool[] ICA = new bool[2];
		public bool[] ICB = new bool[2];
		public bool IRQ;
		public bool ISR;
		public bool[] IT = new bool[2];
		public bool PALE;
		public bool PBLE;
		public int SR;
		public int SRCONTROL;
		public int[] TC = new int[2];
		public int[] TCONTROL = new int[2];
		public int[] TL = new int[2];

		public DataPortBus[] ports;

		private DataPortConnector[] connectors;

		public ViaRegs()
		{
			// power on state
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
				int result = 0xFF;

				addr &= 0xF;
				switch (addr)
				{
					case 0x0: // port B data
						result = connectors[1].Data;
						break;
					case 0x1: // port A data
					case 0xF: // port A data without handshake
						result = connectors[0].Data;
						break;
					case 0x2: // port B direction
						result = connectors[1].Direction;
						break;
					case 0x3: // port A direction
						result = connectors[0].Direction;
						break;
					case 0x4: // timer 0 lo
						result = TC[0] & 0xFF;
						break;
					case 0x5: // timer 0 hi
						result = (TC[0] & 0xFF00) >> 8;
						break;
					case 0x6: // timer 0 latch lo
						result = TL[0] & 0xFF;
						break;
					case 0x7: // timer 0 latch hi
						result = (TL[0] & 0xFF00) >> 8;
						break;
					case 0x8: // timer 1 lo
						result = TC[1] & 0xFF;
						break;
					case 0x9: // timer 1 hi
						result = (TC[1] & 0xFF00) >> 8;
						break;
					case 0xA: // shift register
						result = SR;
						break;
					case 0xB: // peripheral control register
						result = (CACONTROL[0] & 0x01);
						result |= (CACONTROL[1] & 0x07) << 1;
						result |= (CBCONTROL[0] & 0x01) << 4;
						result |= (CBCONTROL[1] & 0x07) << 5;
						break;
					case 0xC: // auxilary control register
						result = (PALE) ? 0x01 : 0x00;
						result |= (PBLE) ? 0x02 : 0x00;
						result |= (SRCONTROL & 0x7) << 2;
						result |= (TCONTROL[0] & 0x1) << 5;
						result |= (TCONTROL[1] & 0x3) << 6;
						break;
					case 0xD: // interrupt status register
						result = ICA[1] ? 0x01 : 0x00;
						result |= ICA[0] ? 0x02 : 0x00;
						result |= ISR ? 0x04 : 0x00;
						result |= ICB[1] ? 0x08 : 0x00;
						result |= ICB[0] ? 0x10 : 0x00;
						result |= IT[1] ? 0x20 : 0x00;
						result |= IT[0] ? 0x40 : 0x00;
						result |= IRQ ? 0x80 : 0x00;
						break;
					case 0xE: // interrupt control register
						result = EICA[1] ? 0x01 : 0x00;
						result |= EICA[0] ? 0x02 : 0x00;
						result |= EISR ? 0x04 : 0x00;
						result |= EICB[1] ? 0x08 : 0x00;
						result |= EICB[0] ? 0x10 : 0x00;
						result |= EIT[1] ? 0x20 : 0x00;
						result |= EIT[0] ? 0x40 : 0x00;
						result |= 0x80; // TODO: check if this is needed
						break;
				}

				return (byte)result;
			}
			set
			{
				byte val = value;
				addr &= 0xF;

				switch (addr)
				{
					case 0x0: // port B data
						connectors[1].Data = val;
						break;
					case 0x1: // port A data
					case 0xF: // port A data without handshake
						connectors[0].Data = val;
						break;
					case 0x2: // port B direction
						connectors[1].Direction = val;
						break;
					case 0x3: // port A direction
						connectors[0].Direction = val;
						break;
					case 0x4: // timer 0 lo
						TC[0] &= 0xFF00;
						TC[0] |= val;
						break;
					case 0x5: // timer 0 hi
						TC[0] &= 0x00FF;
						TC[0] |= (int)val << 8;
						break;
					case 0x6: // timer 0 latch lo
						TL[0] &= 0xFF00;
						TL[0] |= val;
						break;
					case 0x7: // timer 0 latch hi
						TL[0] &= 0x00FF;
						TL[0] |= (int)val << 8;
						break;
					case 0x8: // timer 1 lo
						TC[1] &= 0xFF00;
						TC[1] |= val;
						break;
					case 0x9: // timer 1 hi
						TC[1] &= 0x00FF;
						TC[1] |= (int)val << 8;
						break;
					case 0xA: // shift register
						SR = val;
						break;
					case 0xB: // peripheral control register
						CACONTROL[0] = (val & 0x1);
						CACONTROL[1] = ((val >> 1) & 0x7);
						CBCONTROL[0] = ((val >> 4) & 0x1);
						CBCONTROL[1] = ((val >> 5) & 0x7);
						break;
					case 0xC: // auxilary control register
						PALE = ((val & 0x01) != 0);
						PBLE = ((val & 0x02) != 0);
						SRCONTROL = (val >> 2) & 0x7;
						TCONTROL[0] = (val >> 5) & 0x1;
						TCONTROL[1] = (val >> 6) & 0x3;
						break;
					case 0xD: // interrupt status register
						ICA[1] = ((val & 0x01) != 0);
						ICA[0] = ((val & 0x02) != 0);
						ISR = ((val & 0x04) != 0);
						ICB[1] = ((val & 0x08) != 0);
						ICB[0] = ((val & 0x10) != 0);
						IT[1] = ((val & 0x20) != 0);
						IT[0] = ((val & 0x40) != 0);
						IRQ = ((val & 0x80) != 0);
						break;
					case 0xE: // interrupt control register
						EICA[1] = ((val & 0x01) != 0);
						EICA[0] = ((val & 0x02) != 0);
						EISR = ((val & 0x04) != 0);
						EICB[1] = ((val & 0x08) != 0);
						EICB[0] = ((val & 0x10) != 0);
						EIT[1] = ((val & 0x20) != 0);
						EIT[0] = ((val & 0x40) != 0);
						break;
				}
			}
		}
	}

	// 0x0: port B
	// 0x1: port A
	// 0x2: port B data direction
	// 0x3: port A data direction
	// 0x4: timer lo
	// 0x5: timer hi
	// 0x6: timer latch lo
	// 0x7: timer latch hi
	// 0x8: unused
	// 0x9: unused
	// 0xA: unused
	// 0xB: timer control
	// 0xC: auxilary control
	// 0xD: interrupt status
	// 0xE: interrupt control
	// 0xF: unused

	public class Via
	{
		private ViaRegs regs;

		public Via()
		{
			HardReset();
		}

		public void HardReset()
		{
			regs = new ViaRegs();
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
			// do stuff
			UpdateInterrupts();
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
				case 0xD:
					// reading this clears it
					result = regs[addr];
					regs[addr] = 0x00;
					break;
				default:
					result = regs[addr];
					break;
			}

			return result;
		}

		private void UpdateInterrupts()
		{
			regs.IRQ =
				(regs.ICA[0] & regs.EICA[0]) |
				(regs.ICA[1] & regs.EICA[1]) |
				(regs.ICB[0] & regs.EICB[0]) |
				(regs.ICB[1] & regs.EICB[1]) |
				(regs.IT[0] & regs.EIT[0]) |
				(regs.IT[1] & regs.EIT[1]) |
				(regs.ISR & regs.EISR);
		}

		public void Write(ushort addr, byte val)
		{
			addr &= 0xF;
			switch (addr)
			{
				default:
					regs[addr] = val;
					break;
			}
		}
	}
}
