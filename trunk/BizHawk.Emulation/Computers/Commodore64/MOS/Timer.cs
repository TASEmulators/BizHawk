using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// base class for MOS Technology timer chips

	public abstract class Timer
	{
		protected bool pinIRQ;
		protected byte[] portData;
		protected byte[] portDir;
		protected byte[] portMask;
		protected uint[] timer;
		protected uint[] timerLatch;
		protected bool[] timerOn;
		protected bool[] underflow;

		public Timer()
		{
			portData = new byte[2];
			portDir = new byte[2];
			portMask = new byte[2];
			timer = new uint[2];
			timerLatch = new uint[2];
			timerOn = new bool[2];
			underflow = new bool[2];
		}

		public PortAdapter Adapter0
		{
			get
			{
				return Port.GetAdapter(ReadPort0, ExternalWritePort0, ExternalWriteMask0);
			}
		}

		public PortAdapter Adapter1
		{
			get
			{
				return Port.GetAdapter(ReadPort1, ExternalWritePort1, ExternalWriteMask1);
			}
		}

		private void ExternalWriteMask0(byte data)
		{
			portMask[0] = data;
		}

		private void ExternalWriteMask1(byte data)
		{
			portMask[1] = data;
		}

		private void ExternalWritePort(uint index, byte data)
		{
			portData[index] = Port.ExternalWrite(portData[index], data, portDir[index]);
		}

		private void ExternalWritePort0(byte data)
		{
			ExternalWritePort(0, data);
		}

		private void ExternalWritePort1(byte data)
		{
			ExternalWritePort(1, data);
		}

		protected void HardResetInternal()
		{
			timer[0] = 0xFFFF;
			timer[1] = 0xFFFF;
			timerLatch[0] = timer[0];
			timerLatch[1] = timer[1];
			pinIRQ = true;
			portDir[0] = 0xFF;
			portDir[1] = 0xFF;
			portMask[0] = 0xFF;
			portMask[1] = 0xFF;
			portData[0] = 0xFF;
			portData[1] = 0xFF;
		}

		public bool IRQ
		{
			get
			{
				return pinIRQ;
			}
		}

		public byte ReadPort0()
		{
			return portData[0];
		}

		public byte ReadPort1()
		{
			return portData[1];
		}

		protected void SyncInternal(Serializer ser)
		{
			ser.Sync("pinIRQ", ref pinIRQ);
			ser.Sync("portData0", ref portData[0]);
			ser.Sync("portData1", ref portData[1]);
			ser.Sync("portDir0", ref portDir[0]);
			ser.Sync("portDir1", ref portDir[1]);
			ser.Sync("portMask0", ref portMask[0]);
			ser.Sync("portMask1", ref portMask[1]);
			ser.Sync("timer0", ref timer[0]);
			ser.Sync("timer1", ref timer[1]);
			ser.Sync("timerLatch0", ref timerLatch[0]);
			ser.Sync("timerLatch1", ref timerLatch[1]);
			ser.Sync("timerOn0", ref timerOn[0]);
			ser.Sync("timerOn1", ref timerOn[1]);
			ser.Sync("underflow0", ref underflow[0]);
			ser.Sync("underflow1", ref underflow[1]);
		}

		private void WritePort(uint index, byte data)
		{
			portData[index] = Port.CPUWrite(portData[index], data, portDir[index]);
		}

		public void WritePort0(byte data)
		{
			WritePort(0, data);
		}

		public void WritePort1(byte data)
		{
			WritePort(1, data);
		}
	}
}
