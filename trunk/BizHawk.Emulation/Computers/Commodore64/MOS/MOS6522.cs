using System;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// via
	public class MOS6522 : Timer
	{
		private const uint acrShiftModeDisabled = 0;
		private const uint acrShiftModeInT1 = 1;
		private const uint acrShiftModeInClock = 2;
		private const uint acrShiftModeInExtClock = 3;
		private const uint acrShiftModeOutFree = 4;
		private const uint acrShiftModeOutT1 = 5;
		private const uint acrShiftModeOutClock = 6;
		private const uint acrShiftModeOutExtClock = 7;

		private const uint pcrControlInNegative = 0;
		private const uint pcrControlInNegativeIndep = 1;
		private const uint pcrControlInPositive = 2;
		private const uint pcrControlInPositiveIndep = 3;
		private const uint pcrControlHandshake = 4;
		private const uint pcrControlPulse = 5;
		private const uint pcrControlLow = 6;
		private const uint pcrControlHigh = 7;

		private const uint tControlLoad = 0;
		private const uint tControlContinuous = 1;
		private const uint tControlLoadPB = 2;
		private const uint tControlContinuousPB = 3;
		private const uint tControlPulseCounter = 4;

		private static byte[] portBit = new byte[] { 0x80, 0x40 };
		private static byte[] portMask = new byte[] { 0x7F, 0xBF };

		private bool caPulse;
		private bool cbPulse;
		private bool[] enableIrqCA;
		private bool[] enableIrqCB;
		private bool enableIrqSR;
		private bool[] enableIrqT;
		private bool[] irqCA;
		private bool[] irqCB;
		private bool irqSR;
		private bool[] irqT;
		private bool[] lastca;
		private bool[] lastcb;
		private byte lastpb;
		private byte paLatch;
		private bool paLatchEnable;
        private byte paOut;
        private byte pbLatch;
        private bool pbLatchEnable;
		private byte pbOut;
		private readonly bool[] pbPulse;
		private readonly uint[] pcrControlA;
		private readonly uint[] pcrControlB;
		private byte sr;
		private uint srControl;
		private readonly uint[] tControl;

		public Func<bool> ReadCA0;
		public Func<bool> ReadCA1;
		public Func<bool> ReadCB0;
		public Func<bool> ReadCB1;
		public Action<bool> WriteCA0;
		public Action<bool> WriteCA1;
		public Action<bool> WriteCB0;
		public Action<bool> WriteCB1;

		public MOS6522()
		{
			enableIrqCA = new bool[2];
			enableIrqCB = new bool[2];
			enableIrqT = new bool[2];
			irqCA = new bool[2];
			irqCB = new bool[2];
			irqT = new bool[2];
			lastca = new bool[2];
			lastcb = new bool[2];
			pbPulse = new bool[2];
			pcrControlA = new uint[2];
			pcrControlB = new uint[2];
			tControl = new uint[2];
		}

		public void HardReset()
		{
			caPulse = false;
			cbPulse = false;
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
			lastca[0] = ReadCA0();
			lastca[1] = ReadCA1();
			lastcb[0] = ReadCB0();
			lastcb[1] = ReadCB1();
			pbPulse[0] = false;
			pbPulse[1] = false;
			pcrControlA[0] = 0;
			pcrControlA[1] = 0;
			pcrControlB[0] = 0;
			pcrControlB[1] = 0;
			tControl[0] = 0;
			tControl[1] = 0;
			HardResetInternal();
		}

		// ------------------------------------

		public void ExecutePhase1()
		{
		}

		public void ExecutePhase2()
		{
			bool ca0 = ReadCA0();
			bool ca1 = ReadCA1();
			bool cb0 = ReadCB0();
			bool cb1 = ReadCB1();
			bool ca0Trans = (lastca[0] != ca0);
			bool ca1Trans = (lastca[1] != ca1);
			bool cb0Trans = (lastcb[0] != cb0);
			bool cb1Trans = (lastcb[1] != cb1);

			// edge triggered interrupts

			switch (pcrControlA[0])
			{
				case pcrControlInNegative:
					if (lastca[0] && !ca0)
						irqCA[0] = true;
					break;
				case pcrControlInPositive:
					if (!lastca[0] && ca0)
						irqCA[0] = true;
					break;
			}

			switch (pcrControlB[0])
			{
				case pcrControlInNegative:
					if (lastcb[0] && !cb0)
						irqCB[0] = true;
					break;
				case pcrControlInPositive:
					if (!lastcb[0] && cb0)
						irqCB[0] = true;
					break;
			}

			switch (pcrControlA[1])
			{
				case pcrControlInNegative:
				case pcrControlInNegativeIndep:
					if (lastca[1] && !ca1)
						irqCA[1] = true;
					break;
				case pcrControlInPositive:
				case pcrControlInPositiveIndep:
					if (!lastca[1] && ca1)
						irqCA[1] = true;
					break;
				case pcrControlHandshake:
					if (lastca[0] != ca0)
						WriteCA1(true);
					break;
				case pcrControlPulse:
					if (caPulse)
						caPulse = false;
					else
						WriteCA1(true);
					break;
				case pcrControlLow:
					WriteCA1(false);
					break;
				case pcrControlHigh:
					WriteCA1(true);
					break;
			}

			switch (pcrControlB[1])
			{
				case pcrControlInNegative:
				case pcrControlInNegativeIndep:
					if (lastcb[1] && !cb1)
						irqCB[1] = true;
					break;
				case pcrControlInPositive:
				case pcrControlInPositiveIndep:
					if (!lastcb[1] && cb1)
						irqCB[1] = true;
					break;
				case pcrControlHandshake:
					if (lastcb[0] != cb0)
						WriteCB1(true);
					break;
				case pcrControlPulse:
					if (cbPulse)
						cbPulse = false;
					else
						WriteCB1(true);
					break;
				case pcrControlLow:
					WriteCB1(false);
					break;
				case pcrControlHigh:
					WriteCB1(true);
					break;
			}

			// run timers
			for (uint i = 0; i < 2; i++)
			{
				switch (tControl[i])
				{
					case tControlLoad:
						if (timer[i] > 0)
						{
							timer[i]--;
							if (timer[i] == 0)
								irqT[i] = true;
						}
						break;
					case tControlContinuous:
						if (timer[i] > 0)
						{
							timer[i]--;
							if (timer[i] == 0)
							{
								irqT[i] = true;
								timer[i] = timerLatch[i];
							}
						}
						break;
					case tControlLoadPB:
						if (!pbPulse[i])
							WritePortB((byte)(ReadPortB() & portMask[i]));

						if (timer[i] > 0)
						{
							timer[i]--;
							if (timer[i] == 0)
							{
								irqT[i] = true;
								if (irqT[i])
									WritePortB((byte)(ReadPortB() | portBit[i]));
							}
						}
						break;
					case tControlContinuousPB:
						if (timer[i] > 0)
						{
							timer[i]--;
							if (timer[i] == 0)
							{
								irqT[i] = true;
								timer[i] = timerLatch[i];
								WritePortB((byte)(ReadPortB() ^ portBit[i]));
							}
						}
						break;
					case tControlPulseCounter:
						if ((lastpb & 0x40) != 0 && (ReadPortB() & 0x40) == 0)
						{
							if (timer[i] > 0)
							{
								timer[i]--;
								if (timer[i] == 0)
								{
									irqT[i] = true;
								}
							}
						}
						break;
				}
			}

			lastca[0] = ca0;
			lastca[1] = ca1;
			lastcb[0] = cb0;
			lastcb[1] = cb1;
			lastpb = ReadPortB();

			pinIRQ = !((irqCA[0] & enableIrqCA[0]) |
				(irqCA[1] & enableIrqCA[1]) |
				(irqCB[0] & enableIrqCB[0]) |
				(irqCB[1] & enableIrqCB[1]) |
				(irqSR & enableIrqSR) |
				(irqT[0] & enableIrqT[0]) |
				(irqT[1] & enableIrqT[1])
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

			//Console.WriteLine("via R: reg" + C64Util.ToHex(addr, 4));
			
			addr &= 0xF;
			switch (addr)
			{
				case 0x0:
					irqCB[0] = false;
					irqCB[1] = false;

					if (pbLatchEnable)
						return Port.ExternalWrite(pbLatch, ReadPortB(), ReadDirB());
					else
						return ReadPortB();
				case 0x1:
					if (pcrControlA[0] != pcrControlInNegativeIndep && pcrControlA[0] != pcrControlInPositiveIndep)
						irqCA[0] = false;
					if (pcrControlA[1] != pcrControlInNegativeIndep && pcrControlA[1] != pcrControlInPositiveIndep)
						irqCA[1] = false;

					if (paLatchEnable)
						return Port.ExternalWrite(paLatch, ReadPortA(), ReadDirA());
					else
						return ReadPortA();
				case 0x4:
					irqT[0] = false;
					return ReadRegister(addr);
				case 0x8:
					irqT[1] = false;
					return ReadRegister(addr);
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
						(byte)((tControl[1] & 0x4) << 3) |
						(byte)((tControl[0] & 0x3) << 6)
						);
				case 0xC:
					return (byte)(
						(byte)((pcrControlA[0] & 0x2) >> 1) |
						(byte)((pcrControlA[1] & 0x3) << 1) |
						(byte)((pcrControlB[0] & 0x2) << 3) |
						(byte)((pcrControlB[1] & 0x3) << 5)
						);
				case 0xD: //IFR
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
				case 0xE: //IER
					return (byte)(
						(enableIrqCA[1] ? 0x01 : 0x00) |
						(enableIrqCA[0] ? 0x02 : 0x00) |
						(enableIrqSR ? 0x04 : 0x00) |
						(enableIrqCB[1] ? 0x08 : 0x00) |
						(enableIrqCB[0] ? 0x10 : 0x00) |
						(enableIrqT[1] ? 0x20 : 0x00) |
						(enableIrqT[0] ? 0x40 : 0x00) |
						(0x00)
						);
				default:
					return 0x00;
			}
		}

		public void Write(ushort addr, byte val)
		{
			byte result;
			bool intEnable;

			//Console.WriteLine("via W: reg" + C64Util.ToHex(addr, 4) + " val" + C64Util.ToHex(val, 2));

			addr &= 0xF;
			switch (addr)
			{
				case 0x0:
					irqCB[0] = false;
					irqCB[1] = false;
					pbOut = val;
					WritePortB(val);
					break;
				case 0x1:
					if (pcrControlA[0] != pcrControlInNegativeIndep && pcrControlA[0] != pcrControlInPositiveIndep)
						irqCA[0] = false;
					if (pcrControlA[1] != pcrControlInNegativeIndep && pcrControlA[1] != pcrControlInPositiveIndep)
						irqCA[1] = false;
					paOut = val;
					WritePortA(val);
					break;
				case 0x2:
					WriteDirB(val);
					WritePortB(pbOut);
					break;
				case 0x3:
					WriteDirA(val);
					WritePortA(paOut);
					break;
				case 0x4:
				case 0x6:
					WriteRegister(0x6, val);
					break;
				case 0x5:
					WriteRegister(0x7, val);
					WriteRegister(0x4, ReadRegister(0x6));
					WriteRegister(0x5, ReadRegister(0x7));
					irqT[0] = false;
					pbPulse[0] = false;
					break;
				case 0x9:
					timer[1] = timerLatch[1];
					irqT[1] = false;
					pbPulse[1] = false;
					break;
				case 0xE:
					intEnable = ((val & 0x80) != 0);
					result = ReadRegister(addr);
					if (intEnable)
						result |= (byte)(val & 0x7F);
					else
						result &= (byte)((val & 0x7F) ^ 0x7F);
					WriteRegister(0xE, result);
					break;
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
					timer[0] &= 0xFF00;
					timer[0] |= val;
					break;
				case 0x5:
					timer[0] &= 0x00FF;
					timer[0] |= (uint)val << 8;
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
					timer[1] &= 0x00FF;
					timer[1] |= (uint)val << 8;
					break;
				case 0xA:
					sr = val;
					break;
				case 0xB:
					paLatchEnable = ((val & 0x01) != 0);
					pbLatchEnable = ((val & 0x02) != 0);
					srControl = (((uint)val >> 2) & 0x7);
					tControl[1] = (((uint)val >> 3) & 0x4);
					tControl[0] = (((uint)val >> 6) & 0x3);
					break;
				case 0xC:
					pcrControlA[0] = ((uint)val << 1) & 0x2;
					pcrControlA[1] = ((uint)val >> 1) & 0x7;
					pcrControlB[0] = ((uint)val >> 3) & 0x2;
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
