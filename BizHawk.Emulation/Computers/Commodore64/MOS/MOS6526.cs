using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// MOS technology 6526 "CIA"
	//
	// emulation notes:
	// * CS, R/W and RS# pins are not emulated. (not needed)
	// * A low RES pin is emulated via HardReset().

	public class MOS6526 : Timer, IStandardIO
	{
		// ------------------------------------

		private enum InMode
		{
			Phase2,
			CNT,
			TimerAUnderflow,
			TimerAUnderflowCNT
		}

		private enum OutMode
		{
			Pulse,
			Toggle
		}

		private enum RunMode
		{
			Continuous,
			Oneshot
		}

		private enum SPMode
		{
			Input,
			Output
		}

		// ------------------------------------

		private bool intAlarm;
		private bool intFlag;
		private bool intSP;
		private bool[] intTimer;
		private bool pinCnt;
		private bool pinFlag;
		private bool pinPC;
		private InMode[] timerInMode;
		private OutMode[] timerOutMode;
		private bool[] timerPortEnable;
		private byte[] tod;
		private byte[] todAlarm;
		private bool todAlarmPM;
		private bool todPM;
		private uint todCounter;
		private uint todCounterLatch;

		// ------------------------------------

		public MOS6526(Region region)
		{
			intTimer = new bool[2];
			timerInMode = new InMode[2];
			timerOutMode = new OutMode[2];
			timerPortEnable = new bool[2];
			tod = new byte[4];
			todAlarm = new byte[4];
			switch (region)
			{
				case Region.NTSC:
					todCounterLatch = 14318181 / 140;
					break;
				case Region.PAL:
					todCounterLatch = 17734472 / 180;
					break;
			}
			HardReset();
		}

		// ------------------------------------

		public void ExecutePhase1()
		{
			// unsure if the timer actually operates in ph1
		}

		public void ExecutePhase2()
		{
			pinPC = true;
			TimerRun(0);
			TimerRun(1);
		}

		public void HardReset()
		{
			HardResetInternal();
			intTimer[0] = false;
			intTimer[1] = false;
			timerPortEnable[0] = false;
			timerPortEnable[1] = false;
			timerInMode[0] = InMode.Phase2;
			timerInMode[1] = InMode.Phase2;
			timerOn[0] = false;
			timerOn[1] = false;
			timerOutMode[0] = OutMode.Pulse;
			timerOutMode[1] = OutMode.Pulse;
			tod[0] = 0;
			tod[1] = 0;
			tod[2] = 0;
			tod[3] = 0x12;
			todAlarm[0] = 0;
			todAlarm[1] = 0;
			todAlarm[2] = 0;
			todAlarm[3] = 0;
			todCounter = todCounterLatch;
			pinCnt = false;
			pinFlag = true;
			pinPC = true;
		}

		// ------------------------------------

		private byte BCDAdd(byte i, byte j, out bool overflow)
		{
			uint lo;
			uint hi;
			uint result;

			lo = (i & (uint)0x0F) + (j & (uint)0x0F);
			hi = (i & (uint)0x70) + (j & (uint)0x70);
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
			return (byte)(result & 0xFF);
		}

		private void TimerRun(uint index)
		{

		}

		private void TODRun()
		{
			bool todV;

			if (todCounter == 0)
			{
				todCounter = todCounterLatch;
				tod[0] = BCDAdd(tod[0], 1, out todV);
				if (tod[0] >= 10)
				{
					tod[0] = 0;
					tod[1] = BCDAdd(tod[1], 1, out todV);
					if (todV)
					{
						tod[1] = 0;
						tod[2] = BCDAdd(tod[2], 1, out todV);
						if (todV)
						{
							tod[2] = 0;
							tod[3] = BCDAdd(tod[3], 1, out todV);
							if (tod[3] > 12)
							{
								tod[3] = 1;
							}
							else if (tod[3] == 12)
							{
								todPM = !todPM;
							}
						}
					}
				}
			}
		}

		// ------------------------------------

		public bool CNT
		{
			get { return pinCnt; }
			set { pinCnt = value; }
		}

		public bool FLAG
		{
			get { return pinFlag; }
			set
			{
				if (pinFlag && !value)
					intFlag = true;
				pinFlag = value;
			}
		}

		public bool PC
		{
			get { return pinPC; }
		}

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
			return Read(addr, 0xFF);
		}

		public byte Read(ushort addr, byte mask)
		{
			addr &= 0xF;
			byte val;

			switch (addr)
			{
				case 0x01:
					val = ReadRegister(addr);
					pinPC = false;
					break;
				default:
					val = ReadRegister(addr);
					break;
			}

			val &= mask;
			return val;
		}

		private byte ReadRegister(ushort addr)
		{
			byte val = 0x00; //unused pin value

			switch (addr)
			{
				case 0x0:
					break;
				case 0x1:
					break;
				case 0x2:
					break;
				case 0x3:
					break;
				case 0x4:
					break;
				case 0x5:
					break;
				case 0x6:
					break;
				case 0x7:
					break;
				case 0x8:
					break;
				case 0x9:
					break;
				case 0xA:
					break;
				case 0xB:
					break;
				case 0xC:
					break;
				case 0xD:
					break;
				case 0xE:
					break;
				case 0xF:
					break;
			}

			return val;
		}

		public void Write(ushort addr, byte val)
		{
			Write(addr, val, 0xFF);
		}

		public void Write(ushort addr, byte val, byte mask)
		{
			val &= mask;
			val |= (byte)(ReadRegister(addr) & ~mask);
			addr &= 0xF;

			switch (addr)
			{
				case 0x1:
					WriteRegister(addr, val);
					pinPC = false;
					break;
				default:
					WriteRegister(addr, val);
					break;
			}
		}

		public void WriteRegister(ushort addr, byte val)
		{
			switch (addr)
			{
				case 0x0:
					break;
				case 0x1:
					break;
				case 0x2:
					break;
				case 0x3:
					break;
				case 0x4:
					break;
				case 0x5:
					break;
				case 0x6:
					break;
				case 0x7:
					break;
				case 0x8:
					break;
				case 0x9:
					break;
				case 0xA:
					break;
				case 0xB:
					break;
				case 0xC:
					break;
				case 0xD:
					break;
				case 0xE:
					break;
				case 0xF:
					break;
			}
		}

		// ------------------------------------
	}
}
