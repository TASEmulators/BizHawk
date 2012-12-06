using BizHawk.Emulation.CPUs.M6502;
using BizHawk.Emulation.Computers.Commodore64.MOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Disk
{
	public class VIC1541
	{
		public VIC1541Motherboard board;

		public VIC1541(Region initRegion, byte[] rom)
		{
			board = new VIC1541Motherboard(initRegion);
		}

		public void ConnectSerial(SerialPort newSerialPort)
		{
			board.Connect(newSerialPort);
		}

		public void Execute()
		{
		}
	}

	public class VIC1541Motherboard
	{
		public MOS6502X cpu;
		public VIC1541PLA pla;
		public SerialPort serPort;
		public MOS6522 via0;
		public MOS6522 via1;

		public VIC1541Motherboard(Region initRegion)
		{
			cpu = new MOS6502X();
			pla = new VIC1541PLA();
			serPort = new SerialPort();
			via0 = new MOS6522();
			via1 = new MOS6522();
		}

		public void Connect(SerialPort newSerPort)
		{
			serPort = newSerPort;
			serPort.SystemReadAtn = (() => { return true; });
			serPort.SystemReadClock = (() => { return true; });
			serPort.SystemReadData = (() => { return true; });
			serPort.SystemReadSrq = (() => { return true; });
			serPort.SystemWriteAtn = ((bool val) => { });
			serPort.SystemWriteClock = ((bool val) => { });
			serPort.SystemWriteData = ((bool val) => { });
			serPort.SystemWriteReset = ((bool val) => { });
		}
	}
}
