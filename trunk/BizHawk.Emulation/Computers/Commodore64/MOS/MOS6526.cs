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

		private static byte[] PBOnBit = new byte[] { 0x40, 0x80 };
		private static byte[] PBOnMask = new byte[] { 0xBF, 0x7F };

		// ------------------------------------

		private bool alarmSelect;
		private Region chipRegion;
		private bool cntPos;
		private bool enableIntAlarm;
		private bool enableIntFlag;
		private bool enableIntSP;
		private bool[] enableIntTimer;
		private bool intAlarm;
		private bool intFlag;
		private bool intSP;
		private bool[] intTimer;
		private bool pinCnt;
		private bool pinFlag;
		private bool pinPC;
		private byte sr;
		private uint[] timerDelay;
		private InMode[] timerInMode;
		private OutMode[] timerOutMode;
		private bool[] timerPortEnable;
		private bool[] timerPulse;
		private RunMode[] timerRunMode;
		private SPMode timerSPMode;
		private byte[] tod;
		private byte[] todAlarm;
		private bool todAlarmPM;
		private uint todCounter;
		private uint todCounterLatch;
		private bool todIn;
		private bool todPM;

		// ------------------------------------

		public MOS6526(Region region)
		{
			chipRegion = region;
			enableIntTimer = new bool[2];
			intTimer = new bool[2];
			timerDelay = new uint[2];
			timerInMode = new InMode[2];
			timerOutMode = new OutMode[2];
			timerPortEnable = new bool[2];
			timerPulse = new bool[2];
			timerRunMode = new RunMode[2];
			tod = new byte[4];
			todAlarm = new byte[4];

			SetTodIn(chipRegion);
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
			TODRun();

			if (timerPulse[0])
				portData[0] &= PBOnMask[0];
			if (timerPulse[1])
				portData[1] &= PBOnMask[0];

			if (timerDelay[0] == 0)
				TimerRun(0);
			else
				timerDelay[0]--;

			if (timerDelay[1] == 0)
				TimerRun(1);
			else
				timerDelay[1]--;

			intAlarm |= (
				tod[0] == todAlarm[0] &&
				tod[1] == todAlarm[1] &&
				tod[2] == todAlarm[2] &&
				tod[3] == todAlarm[3] &&
				todPM == todAlarmPM);

			cntPos = false;
			underflow[0] = false;
			underflow[1] = false;

			pinIRQ = !(
				(intTimer[0] && enableIntTimer[0]) ||
				(intTimer[1] && enableIntTimer[1]) ||
				(intAlarm && enableIntAlarm) ||
				(intSP && enableIntSP) ||
				(intFlag && enableIntFlag)
				);
		}

		public void HardReset()
		{
			HardResetInternal();
			alarmSelect = false;
			cntPos = false;
			enableIntAlarm = false;
			enableIntFlag = false;
			enableIntSP = false;
			enableIntTimer[0] = false;
			enableIntTimer[1] = false;
			intAlarm = false;
			intFlag = false;
			intSP = false;
			intTimer[0] = false;
			intTimer[1] = false;
			sr = 0;
			timerDelay[0] = 0;
			timerDelay[1] = 0;
			timerInMode[0] = InMode.Phase2;
			timerInMode[1] = InMode.Phase2;
			timerOn[0] = false;
			timerOn[1] = false;
			timerOutMode[0] = OutMode.Pulse;
			timerOutMode[1] = OutMode.Pulse;
			timerPortEnable[0] = false;
			timerPortEnable[1] = false;
			timerPulse[0] = false;
			timerPulse[1] = false;
			timerRunMode[0] = RunMode.Continuous;
			timerRunMode[1] = RunMode.Continuous;
			timerSPMode = SPMode.Input;
			tod[0] = 0;
			tod[1] = 0;
			tod[2] = 0;
			tod[3] = 0x12;
			todAlarm[0] = 0;
			todAlarm[1] = 0;
			todAlarm[2] = 0;
			todAlarm[3] = 0;
			todCounter = todCounterLatch;
			todIn = (chipRegion == Region.PAL);
			todPM = false;

			pinCnt = false;
			pinFlag = true;
			pinPC = true;
	}

		private void SetTodIn(Region region)
		{
			switch (region)
			{
				case Region.NTSC:
					todCounterLatch = 14318181 / 140;
					todIn = false;
					break;
				case Region.PAL:
					todCounterLatch = 17734472 / 180;
					todIn = true;
					break;
			}
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
			if (timerOn[index])
			{
				uint t = timer[index];
				bool u = false;

				unchecked
				{
					switch (timerInMode[index])
					{
						case InMode.CNT:
							// CNT positive
							if (cntPos)
							{
								u = (t == 0);
								t--;
								intTimer[index] |= (t == 0);
							}
							break;
						case InMode.Phase2:
							// every clock
							u = (t == 0);
							t--;
							intTimer[index] |= (t == 0);
							break;
						case InMode.TimerAUnderflow:
							// every underflow[0]
							if (underflow[0])
							{
								u = (t == 0);
								t--;
								intTimer[index] |= (t == 0);
							}
							break;
						case InMode.TimerAUnderflowCNT:
							// every underflow[0] while CNT high
							if (underflow[0] && pinCnt)
							{
								u = (t == 0);
								t--;
								intTimer[index] |= (t == 0);
							}
							break;
					}

					// underflow?
					if (u)
					{
						t = timerLatch[index];
						if (timerRunMode[index] == RunMode.Oneshot)
							timerOn[index] = false;

						if (timerPortEnable[index])
						{
							// force port B bit to output
							portDir[index] |= PBOnBit[index];
							switch (timerOutMode[index])
							{
								case OutMode.Pulse:
									timerPulse[index] = true;
									portData[index] |= PBOnBit[index];
									break;
								case OutMode.Toggle:
									portData[index] ^= PBOnBit[index];
									break;
							}
						}
					}

					underflow[index] = u;
					timer[index] = t;
				}
			}
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
			todCounter--;
		}

		// ------------------------------------

		public bool CNT
		{
			get { return pinCnt; }
			set { cntPos |= (!pinCnt && value); pinCnt = value; }
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
				case 0x0D:
					val = ReadRegister(addr);
					intTimer[0] = false;
					intTimer[1] = false;
					intAlarm = false;
					intSP = false;
					intFlag = false;
					pinIRQ = true;
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
			uint timerVal;

			switch (addr)
			{
				case 0x0:
					val = (byte)(portData[0] & portMask[0]);
					break;
				case 0x1:
					val = (byte)(portData[1] & portMask[1]);
					break;
				case 0x2:
					val = portDir[0];
					break;
				case 0x3:
					val = portDir[1];
					break;
				case 0x4:
					timerVal = ReadTimerValue(0);
					val = (byte)(timerVal & 0xFF);
					break;
				case 0x5:
					timerVal = ReadTimerValue(0);
					val = (byte)(timerVal >> 8);
					break;
				case 0x6:
					timerVal = ReadTimerValue(1);
					val = (byte)(timerVal & 0xFF);
					break;
				case 0x7:
					timerVal = ReadTimerValue(1);
					val = (byte)(timerVal >> 8);
					break;
				case 0x8:
					val = tod[0];
					break;
				case 0x9:
					val = tod[1];
					break;
				case 0xA:
					val = tod[2];
					break;
				case 0xB:
					val = tod[3];
					break;
				case 0xC:
					val = sr;
					break;
				case 0xD:
					val = (byte)(
						(intTimer[0] ? 0x01 : 0x00) |
						(intTimer[1] ? 0x02 : 0x00) |
						(intAlarm ? 0x04 : 0x00) |
						(intSP ? 0x08 : 0x00) |
						(intFlag ? 0x10 : 0x00) |
						(!pinIRQ ? 0x80 : 0x00)
						);
					break;
				case 0xE:
					val = (byte)(
						(timerOn[0] ? 0x01 : 0x00) |
						(timerPortEnable[0] ? 0x02 : 0x00) |
						(todIn ? 0x80 : 0x00));
					if (timerOutMode[0] == OutMode.Toggle)
						val |= 0x04;
					if (timerRunMode[0] == RunMode.Oneshot)
						val |= 0x08;
					if (timerInMode[0] == InMode.CNT)
						val |= 0x20;
					if (timerSPMode == SPMode.Output)
						val |= 0x40;
					break;
				case 0xF:
					val = (byte)(
						(timerOn[1] ? 0x01 : 0x00) |
						(timerPortEnable[1] ? 0x02 : 0x00) |
						(alarmSelect ? 0x80 : 0x00));
					if (timerOutMode[1] == OutMode.Toggle)
						val |= 0x04;
					if (timerRunMode[1] == RunMode.Oneshot)
						val |= 0x08;
					switch (timerInMode[1])
					{
						case InMode.CNT:
							val |= 0x20;
							break;
						case InMode.TimerAUnderflow:
							val |= 0x40;
							break;
						case InMode.TimerAUnderflowCNT:
							val |= 0x60;
							break;
					}
					break;
			}

			return val;
		}

		private uint ReadTimerValue(uint index)
		{
			if (timerOn[index])
			{
				if (timer[index] == 0)
					return timerLatch[index];
				else
					return timer[index];
			}
			else
			{
				return timer[index];
			}
		}

		public void SyncState(Serializer ser)
		{
			int chipRegionInt = (int)chipRegion;
			int timerInModeInt0 = (int)timerInMode[0];
			int timerInModeInt1 = (int)timerInMode[1];
			int timerOutModeInt0 = (int)timerOutMode[0];
			int timerOutModeInt1 = (int)timerOutMode[1];
			int timerRunModeInt0 = (int)timerRunMode[0];
			int timerRunModeInt1 = (int)timerRunMode[1];
			int timerSPModeInt = (int)timerSPMode;

			SyncInternal(ser);
			ser.Sync("alarmSelect", ref alarmSelect);
			ser.Sync("chipRegion", ref chipRegionInt);
			ser.Sync("cntPos", ref cntPos);
			ser.Sync("enableIntAlarm", ref enableIntAlarm);
			ser.Sync("enableIntFlag", ref enableIntFlag);
			ser.Sync("enableIntSP", ref enableIntSP);
			ser.Sync("enableIntTimer0", ref enableIntTimer[0]);
			ser.Sync("enableIntTimer1", ref enableIntTimer[1]);
			ser.Sync("intAlarm", ref intAlarm);
			ser.Sync("intFlag", ref intFlag);
			ser.Sync("intSP", ref intSP);
			ser.Sync("intTimer0", ref intTimer[0]);
			ser.Sync("intTimer1", ref intTimer[1]);
			ser.Sync("pinCnt", ref pinCnt);
			ser.Sync("pinFlag", ref pinFlag);
			ser.Sync("pinPC", ref pinPC);
			ser.Sync("sr", ref sr);
			ser.Sync("timerDelay0", ref timerDelay[0]);
			ser.Sync("timerDelay1", ref timerDelay[1]);
			ser.Sync("timerInMode0", ref timerInModeInt0);
			ser.Sync("timerInMode1", ref timerInModeInt1);
			ser.Sync("timerOutMode0", ref timerOutModeInt0);
			ser.Sync("timerOutMode1", ref timerOutModeInt1);
			ser.Sync("timerPortEnable0", ref timerPortEnable[0]);
			ser.Sync("timerPortEnable1", ref timerPortEnable[1]);
			ser.Sync("timerPulse0", ref timerPulse[0]);
			ser.Sync("timerPulse1", ref timerPulse[1]);
			ser.Sync("timerRunMode0", ref timerRunModeInt0);
			ser.Sync("timerRunMode1", ref timerRunModeInt1);
			ser.Sync("timerSPMode", ref timerSPModeInt);
			ser.Sync("tod0", ref tod[0]);
			ser.Sync("tod1", ref tod[1]);
			ser.Sync("tod2", ref tod[2]);
			ser.Sync("tod3", ref tod[3]);
			ser.Sync("todAlarm0", ref todAlarm[0]);
			ser.Sync("todAlarm1", ref todAlarm[1]);
			ser.Sync("todAlarm2", ref todAlarm[2]);
			ser.Sync("todAlarm3", ref todAlarm[3]);
			ser.Sync("todAlarmPM", ref todAlarmPM);
			ser.Sync("todCounter", ref todCounter);
			ser.Sync("todCounterLatch", ref todCounterLatch);
			ser.Sync("todIn", ref todIn);
			ser.Sync("todPM", ref todPM);

			chipRegion = (Region)chipRegionInt;
			timerInMode[0] = (InMode)timerInModeInt0;
			timerInMode[1] = (InMode)timerInModeInt1;
			timerOutMode[0] = (OutMode)timerOutModeInt0;
			timerOutMode[1] = (OutMode)timerOutModeInt1;
			timerRunMode[0] = (RunMode)timerRunModeInt0;
			timerRunMode[1] = (RunMode)timerRunModeInt1;
			timerSPMode = (SPMode)timerSPModeInt;
		}

		public void Write(ushort addr, byte val)
		{
			Write(addr, val, 0xFF);
		}

		public void Write(ushort addr, byte val, byte mask)
		{
			addr &= 0xF;
			val &= mask;
			val |= (byte)(ReadRegister(addr) & ~mask);

			switch (addr)
			{
				case 0x0:
					WritePort0(val);
					break;
				case 0x1:
					WritePort1(val);
					pinPC = false;
					break;
				case 0x5:
					WriteRegister(addr, val);
					if (!timerOn[0])
						timer[0] = timerLatch[0];
					break;
				case 0x7:
					WriteRegister(addr, val);
					if (!timerOn[1])
						timer[1] = timerLatch[1];
					break;
				case 0xE:
					WriteRegister(addr, val);
					if ((val & 0x10) != 0)
						timer[0] = timerLatch[0];
					break;
				case 0xF:
					WriteRegister(addr, val);
					if ((val & 0x10) != 0)
						timer[1] = timerLatch[1];
					break;
				default:
					WriteRegister(addr, val);
					break;
			}
		}

		public void WriteRegister(ushort addr, byte val)
		{
			bool intReg;

			switch (addr)
			{
				case 0x0:
					portData[0] = val;
					break;
				case 0x1:
					portData[1] = val;
					break;
				case 0x2:
					portDir[0] = val;
					break;
				case 0x3:
					portDir[1] = val;
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
					timerLatch[1] &= 0xFF00;
					timerLatch[1] |= val;
					break;
				case 0x7:
					timerLatch[1] &= 0x00FF;
					timerLatch[1] |= (uint)val << 8;
					break;
				case 0x8:
					if (alarmSelect)
						todAlarm[0] = (byte)(val & 0xF);
					else
						tod[0] = (byte)(val & 0xF);
					break;
				case 0x9:
					if (alarmSelect)
						todAlarm[1] = (byte)(val & 0x7F);
					else
						tod[1] = (byte)(val & 0x7F);
					break;
				case 0xA:
					if (alarmSelect)
						todAlarm[2] = (byte)(val & 0x7F);
					else
						tod[2] = (byte)(val & 0x7F);
					break;
				case 0xB:
					if (alarmSelect)
					{
						todAlarm[3] = (byte)(val & 0x1F);
						todAlarmPM = ((val & 0x80) != 0);
					}
					else
					{
						tod[3] = (byte)(val & 0x1F);
						todPM = ((val & 0x80) != 0);
					}
					break;
				case 0xC:
					sr = val;
					break;
				case 0xD:
					intReg = ((val & 0x80) != 0);
					if ((val & 0x01) != 0)
						enableIntTimer[0] = intReg;
					if ((val & 0x02) != 0)
						enableIntTimer[1] = intReg;
					if ((val & 0x04) != 0)
						enableIntAlarm = intReg;
					if ((val & 0x08) != 0)
						enableIntSP = intReg;
					if ((val & 0x10) != 0)
						enableIntFlag = intReg;
					break;
				case 0xE:
					if ((val & 0x01) != 0 && !timerOn[0])
						timerDelay[0] = 2;
					timerOn[0] = ((val & 0x01) != 0);
					timerPortEnable[0] = ((val & 0x02) != 0);
					timerOutMode[0] = ((val & 0x04) != 0) ? OutMode.Toggle : OutMode.Pulse;
					timerRunMode[0] = ((val & 0x08) != 0) ? RunMode.Oneshot : RunMode.Continuous;
					timerInMode[0] = ((val & 0x20) != 0) ? InMode.CNT : InMode.Phase2;
					timerSPMode = ((val & 0x40) != 0) ? SPMode.Output : SPMode.Input;
					todIn = ((val & 0x80) != 0);
					break;
				case 0xF:
					if ((val & 0x01) != 0 && !timerOn[1])
						timerDelay[1] = 2;
					timerOn[1] = ((val & 0x01) != 0);
					timerPortEnable[1] = ((val & 0x02) != 0);
					timerOutMode[1] = ((val & 0x04) != 0) ? OutMode.Toggle : OutMode.Pulse;
					timerRunMode[1] = ((val & 0x08) != 0) ? RunMode.Oneshot : RunMode.Continuous;
					switch (val & 0x60)
					{
						case 0x00: timerInMode[1] = InMode.Phase2; break;
						case 0x20: timerInMode[1] = InMode.CNT; break;
						case 0x40: timerInMode[1] = InMode.TimerAUnderflow; break;
						case 0x60: timerInMode[1] = InMode.TimerAUnderflowCNT; break;
					}
					alarmSelect = ((val & 0x80) != 0);
					break;
			}
		}

		// ------------------------------------
	}
}
