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
		protected uint[] timer;
		protected uint[] timerLatch;
		protected bool[] timerOn;
		protected bool[] underflow;

		public Timer()
		{
			portData = new byte[2];
			portDir = new byte[2];
			timer = new uint[2];
			timerLatch = new uint[2];
			timerOn = new bool[2];
			underflow = new bool[2];
		}

		public PortAdapter Adapter0
		{
			get
			{
				return Port.GetAdapter(ReadPort0, ExternalWritePort0);
			}
		}

		public PortAdapter Adapter1
		{
			get
			{
				return Port.GetAdapter(ReadPort1, ExternalWritePort1);
			}
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
