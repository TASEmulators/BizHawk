using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class Cia
	{
		public int alarmTime;
		public bool alarmWriteEnabled;
		public int cycles;
		public bool flagPin;
		public bool flagPinInterrupt;
		public bool flagPinInterruptEnabled;
		public bool[] generatePositiveEdgeOnUnderflow = new bool[2];
		public bool interrupt;
		public bool[] loadStartValue = new bool[2];
		public bool palMode;
		public byte[] regs;
		public int shiftRegisterCycles;
		public bool shiftRegisterInterrupt;
		public bool shiftRegisterInterruptEnabled;
		public bool shiftRegisterIsOutput;
		public bool[] stopOnUnderflow = new bool[2];
		public int timeOfDay;
		public bool timeOfDayAlarmInterrupt;
		public bool timeOfDayAlarmInterruptEnabled;
		public int[] timerConfig = new int[2];
		public bool[] timerEnabled = new bool[2];
		public bool[] timerInterruptEnabled = new bool[2];
		public ushort[] timerLatch = new ushort[2];
		public bool[] timerUnderflowMonitor = new bool[2];
		public ushort[] timerValue = new ushort[2];
		public bool[] underflowTimerInterrupt = new bool[2];
		public bool[] underflowTimerInterruptEnabled = new bool[2];

		public Func<byte> ReadPortA;
		public Func<byte> ReadPortB;
		public Action<byte, byte> WritePortA;
		public Action<byte, byte> WritePortB;

		public Cia(Func<byte> funcReadPortA, Func<byte> funcReadPortB, Action<byte, byte> actWritePortA, Action<byte, byte> actWritePortB)
		{
			ReadPortA = funcReadPortA;
			ReadPortB = funcReadPortB;
			WritePortA = actWritePortA;
			WritePortB = actWritePortB;
			HardReset();
		}

		static public byte DummyReadPort()
		{
			return 0x00;
		}

		static public void DummyWritePort(byte val, byte direction)
		{
			// do nothing
		}

		public void HardReset()
		{
			regs = new byte[0x10];
			Write(0x0004, 0xFF);
			Write(0x0005, 0xFF);
			Write(0x0006, 0xFF);
			Write(0x0007, 0xFF);
			Write(0x000B, 0x01);

		}

		public void PerformCycle()
		{
			unchecked
			{
				for (int i = 0; i < 2; i++)
				{
					if (timerConfig[i] == 0)
						TimerTick(i);
				}
			}

			regs[0x04] = (byte)(timerValue[0] & 0xFF);
			regs[0x05] = (byte)(timerValue[0] >> 8);
			regs[0x06] = (byte)(timerValue[1] & 0xFF);
			regs[0x07] = (byte)(timerValue[1] >> 8);
		}

		public void PollSerial(ref bool bit)
		{
			// this has the same effect as raising CNT

			for (int i = 0; i < 2; i++)
			{
				switch (timerConfig[i])
				{
					case 1:
					case 3:
						TimerTick(i);
						break;
				}
			}
			if (shiftRegisterIsOutput)
			{
				bit = ((regs[0x0C] & 0x01) != 0x00);
				regs[0x0C] >>= 1;
			}
			else
			{
				regs[0x0C] >>= 1;
				if (bit)
					regs[0x0C] |= 0x80;
			}
		}

		public byte Read(ushort addr)
		{
			byte result = 0;
			addr &= 0x0F;

			switch (addr)
			{
				case 0x00:
					result = ReadPortA();
					regs[addr] = result;
					break;
				case 0x01:
					result = ReadPortB();
					regs[addr] = result;
					break;
				case 0x0D:
					result = regs[addr];
					shiftRegisterInterrupt = false;
					timeOfDayAlarmInterrupt = false;
					underflowTimerInterrupt[0] = false;
					underflowTimerInterrupt[1] = false;
					interrupt = false;
					UpdateInterruptReg();
					break;
				default:
					result = regs[addr];
					break;
			}

			return result;
		}

		public void TimerTick(int index)
		{
			if (timerEnabled[index])
			{
				unchecked
				{
					timerValue[index]--;
				}
				if (timerValue[index] == 0xFFFF)
				{
					if (underflowTimerInterruptEnabled[index])
					{
						underflowTimerInterrupt[index] = true;
						interrupt = true;
					}

					// timer B can count on timer A's underflows
					if (index == 0)
					{
						switch (timerConfig[1])
						{
							case 2:
							case 3:
								TimerTick(1);
								break;
						}
					}
				}
			}
		}

		public void UpdateInterruptReg()
		{
			byte result;
			result = (byte)(shiftRegisterInterrupt ? 0x01 : 0x00);
			result |= (byte)(timeOfDayAlarmInterrupt ? 0x02 : 0x00);
			result |= (byte)(underflowTimerInterrupt[0] ? 0x04 : 0x00);
			result |= (byte)(underflowTimerInterrupt[1] ? 0x08 : 0x00);
			result |= (byte)(flagPinInterrupt ? 0x10 : 0x00);
			result |= (byte)(interrupt ? 0x80 : 0x00);
			regs[0x0D] = result;
		}

		public void Write(ushort addr, byte val)
		{
			bool allowWrite = true;
			addr &= 0x0F;

			switch (addr)
			{
				case 0x00:
					WritePortA(val, regs[0x02]);
					allowWrite = false;
					break;
				case 0x01:
					WritePortB(val, regs[0x03]);
					allowWrite = false;
					break;
				case 0x04:
					timerValue[0] &= 0xFF00;
					timerValue[0] |= val;
					break;
				case 0x05:
					timerValue[0] &= 0xFF;
					timerValue[0] |= (ushort)(val << 8);
					break;
				case 0x06:
					timerValue[1] &= 0xFF00;
					timerValue[1] |= val;
					break;
				case 0x07:
					timerValue[1] &= 0xFF;
					timerValue[1] |= (ushort)(val << 8);
					break;
				case 0x0D:
					if ((val & 0x01) != 0x00)
						timerInterruptEnabled[0] = ((val & 0x80) != 0x00);
					if ((val & 0x02) != 0x00)
						timerInterruptEnabled[1] = ((val & 0x80) != 0x00);
					if ((val & 0x04) != 0x00)
						timeOfDayAlarmInterruptEnabled = ((val & 0x80) != 0x00);
					if ((val & 0x08) != 0x00)
						shiftRegisterInterruptEnabled = ((val & 0x80) != 0x00);
					if ((val & 0x10) != 0x00)
						flagPinInterruptEnabled = ((val & 0x80) != 0x00);
					allowWrite = false;
					break;
				case 0x0E:
					timerEnabled[0] = ((val & 0x01) != 0x00);
					timerUnderflowMonitor[0] = ((val & 0x02) != 0x00);
					generatePositiveEdgeOnUnderflow[0] = ((val & 0x04) != 0x00);
					stopOnUnderflow[0] = ((val & 0x08) != 0x00);
					loadStartValue[0] = ((val & 0x10) != 0x00);
					timerConfig[0] = ((val & 0x20) >> 5);
					shiftRegisterIsOutput = ((val & 0x40) != 0x00);
					palMode = ((val & 0x80) != 0x00);
					break;
				case 0x0F:
					timerEnabled[1] = ((val & 0x01) != 0x00);
					timerUnderflowMonitor[1] = ((val & 0x02) != 0x00);
					generatePositiveEdgeOnUnderflow[1] = ((val & 0x04) != 0x00);
					stopOnUnderflow[1] = ((val & 0x08) != 0x00);
					loadStartValue[1] = ((val & 0x10) != 0x00);
					timerConfig[1] = ((val & 0x60) >> 5);
					alarmWriteEnabled = ((val & 0x80) != 0x00);
					break;
				default:
					break;
			}

			if (allowWrite)
				regs[addr] = val;
		}
	}
}
