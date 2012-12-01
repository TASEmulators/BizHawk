using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	static public class Port
	{
		static public byte CPUWrite(byte latch, byte val, byte dir)
		{
			byte result;
			result = (byte)(latch & (byte)(~dir & 0xFF));
			result |= (byte)(val & dir);
			return result;
		}

		static public byte ExternalWrite(byte latch, byte val, byte dir)
		{
			byte result;
			result = (byte)(latch & dir);
			result |= (byte)(val & (byte)(~dir & 0xFF));
			return result;
		}

		static public PortAdapter GetAdapter(Func<byte> newRead, Action<byte> newWrite, Action<byte> newWriteForce)
		{
			return new PortAdapter(newRead, newWrite, newWriteForce);
		}
	}

	public class PortAdapter
	{
		private Action<byte> actWrite;
		private Action<byte> actWriteMask;
		private Func<byte> funcRead;

		public PortAdapter(Func<byte> newRead, Action<byte> newWrite, Action<byte> newWriteMask)
		{
			funcRead = newRead;
			actWrite = newWrite;
			actWriteMask = newWriteMask;
		}

		public byte Data
		{
			get
			{
				return funcRead();
			}
			set
			{
				actWrite(value);
			}
		}

		public void MaskWrite(byte val)
		{
			actWriteMask(val);
		}
	}
}
