using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// via
	public class MOS6522 : Timer
	{
		private uint acrShiftMode;
		private bool[] enableIrqCA;
		private bool[] enableIrqCB;
		private bool enableIrqSR;
		private bool[] enableIrqT;
		private bool[] irqCA;
		private bool[] irqCB;
		private bool irqSR;
		private bool[] irqT;
		private bool paLatchEnable;
		private bool pbLatchEnable;
		private uint[] pcrControlA;
		private uint[] pcrControlB;
		private byte sr;
		private uint srControl;
		private uint[] tControl;

		public MOS6522()
		{
			enableIrqCA = new bool[2];
			enableIrqCB = new bool[2];
			enableIrqT = new bool[2];
			irqCA = new bool[2];
			irqCB = new bool[2];
			irqT = new bool[2];
			pcrControlA = new uint[2];
			pcrControlB = new uint[2];
			tControl = new uint[2];
		}

		public void HardReset()
		{
			acrShiftMode = 0;
			enableIrqCA[0] = false;
			enableIrqCA[1] = false;
			enableIrqCB[0] = false;
			enableIrqCB[1] = false;
			enableIrqSR = false;
			enableIrqT[0] = false;
			enableIrqT[1] = false;
			irqCA[0] = false;
			irqCA[1] = false;
			irqCB[0] = false;
			irqCB[1] = false;
			irqSR = false;
			irqT[0] = false;
			irqT[1] = false;
			pcrControlA[0] = 0;
			pcrControlA[1] = 0;
			pcrControlB[0] = 0;
			pcrControlB[1] = 0;
			tControl[0] = 0;
			tControl[1] = 0;
		}

		// ------------------------------------

		public void ExecutePhase1()
		{
		}

		public void ExecutePhase2()
		{
			pinIRQ = !(irqCA[0] | irqCA[1] |
				irqCB[0] | irqCB[1] |
				irqSR | irqT[0] | irqT[1]
				);
		}

		// ------------------------------------

		public byte Peek(int addr)
		{
			return ReadRegister((ushort)(addr & 0xF));
		}

		public void Poke(int addr, byte val)
		{
			WriteRegister((ushort)(addr & 0xF), val);
		}

		public byte Read(ushort addr)
		{
			addr &= 0xF;
			switch (addr)
			{
				default:
					return ReadRegister(addr);
			}
		}

		private byte ReadRegister(ushort addr)
		{
			switch (addr)
			{
				case 0x0:
					return ReadPortB();
				case 0x1:
					return ReadPortA();
				case 0x2:
					return ReadDirB();
				case 0x3:
					return ReadDirA();
				case 0x4:
					return (byte)(timer[0] & 0xFF);
				case 0x5:
					return (byte)(timer[0] >> 8);
				case 0x6:
					return (byte)(timerLatch[0] & 0xFF);
				case 0x7:
					return (byte)(timerLatch[0] >> 8);
				case 0x8:
					return (byte)(timer[1] & 0xFF);
				case 0x9:
					return (byte)(timer[1] >> 8);
				case 0xA:
					return sr;
				case 0xB:
					return (byte)(
						(paLatchEnable ? 0x01 : 0x00) |
						(pbLatchEnable ? 0x02 : 0x00) |
						(byte)((srControl & 0x7) << 2) |
						(byte)((tControl[1] & 0x1) << 5) |
						(byte)((tControl[0] & 0x3) << 6)
						);
				case 0xC:
					return (byte)(
						(byte)(pcrControlA[0] & 0x1) |
						(byte)((pcrControlA[1] & 0x3) << 1) |
						(byte)((pcrControlB[0] & 0x1) << 4) |
						(byte)((pcrControlB[1] & 0x3) << 5)
						);
				case 0xD:
					return (byte)(
						(irqCA[1] ? 0x01 : 0x00) |
						(irqCA[0] ? 0x02 : 0x00) |
						(irqSR ? 0x04 : 0x00) |
						(irqCB[1] ? 0x08 : 0x00) |
						(irqCB[0] ? 0x10 : 0x00) |
						(irqT[1] ? 0x20 : 0x00) |
						(irqT[0] ? 0x40 : 0x00) |
						(pinIRQ ? 0x00 : 0x80)
						);
				case 0xE:
					return (byte)(
						(enableIrqCA[1] ? 0x01 : 0x00) |
						(enableIrqCA[0] ? 0x02 : 0x00) |
						(enableIrqSR ? 0x04 : 0x00) |
						(enableIrqCB[1] ? 0x08 : 0x00) |
						(enableIrqCB[0] ? 0x10 : 0x00) |
						(enableIrqT[1] ? 0x20 : 0x00) |
						(enableIrqT[0] ? 0x40 : 0x00) |
						(0x80)
						);
				default:
					return 0x00;
			}
		}

		public void Write(ushort addr, byte val)
		{
			addr &= 0xF;
			switch (addr)
			{
				default:
					WriteRegister(addr, val);
					break;
			}
		}

		private void WriteRegister(ushort addr, byte val)
		{
			switch (addr)
			{
				case 0x0:
					WritePortB(val);
					break;
				case 0x1:
					WritePortA(val);
					break;
				case 0x2:
					WriteDirB(val);
					break;
				case 0x3:
					WriteDirA(val);
					break;
				case 0x4:
					timerLatch[0] &= 0xFF00;
					timerLatch[0] |= val;
					break;
				case 0x5:
					timerLatch[0] &= 0x00FF;
					timerLatch[0] |= (uint)val << 8;
					break;
				case 0x6:
					timerLatch[0] &= 0xFF00;
					timerLatch[0] |= val;
					break;
				case 0x7:
					timerLatch[0] &= 0x00FF;
					timerLatch[0] |= (uint)val << 8;
					break;
				case 0x8:
					timerLatch[1] &= 0xFF00;
					timerLatch[1] |= val;
					break;
				case 0x9:
					timerLatch[1] &= 0x00FF;
					timerLatch[1] |= (uint)val << 8;
					break;
				case 0xA:
					sr = val;
					break;
				case 0xB:
					paLatchEnable = ((val & 0x01) != 0);
					pbLatchEnable = ((val & 0x02) != 0);
					srControl = (((uint)val >> 2) & 0x7);
					tControl[1] = (((uint)val >> 5) & 0x1);
					tControl[0] = (((uint)val >> 6) & 0x3);
					break;
				case 0xC:
					pcrControlA[0] = (uint)(val & 0x1);
					pcrControlA[1] = ((uint)val >> 1) & 0x7;
					pcrControlB[0] = ((uint)val >> 4) & 0x1;
					pcrControlB[1] = ((uint)val >> 5) & 0x7;
					break;
				case 0xD:
					irqCA[1] = ((val & 0x01) != 0);
					irqCA[0] = ((val & 0x02) != 0);
					irqSR = ((val & 0x04) != 0);
					irqCB[1] = ((val & 0x08) != 0);
					irqCB[0] = ((val & 0x10) != 0);
					irqT[1] = ((val & 0x20) != 0);
					irqT[0] = ((val & 0x40) != 0);
					break;
				case 0xE:
					enableIrqCA[1] = ((val & 0x01) != 0);
					enableIrqCA[0] = ((val & 0x02) != 0);
					enableIrqSR = ((val & 0x04) != 0);
					enableIrqCB[1] = ((val & 0x08) != 0);
					enableIrqCB[0] = ((val & 0x10) != 0);
					enableIrqT[1] = ((val & 0x20) != 0);
					enableIrqT[0] = ((val & 0x40) != 0);
					break;
				default:
					break;
			}
		}
	
		// ------------------------------------
	}
}
