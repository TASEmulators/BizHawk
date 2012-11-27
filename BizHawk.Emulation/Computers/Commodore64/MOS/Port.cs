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
			result = (byte)(latch & ~dir);
			result |= (byte)(val & dir);
			return result;
		}

		static public byte ExternalWrite(byte latch, byte val, byte dir)
		{
			byte result;
			result = (byte)(latch & dir);
			result |= (byte)(val & ~dir);
			return result;
		}

		static public PortAdapter GetAdapter(Func<byte> newRead, Action<byte> newWrite)
		{
			return new PortAdapter(newRead, newWrite);
		}
	}

	public class PortAdapter
	{
		private Action<byte> actWrite;
		private Func<byte> funcRead;

		public PortAdapter(Func<byte> newRead, Action<byte> newWrite)
		{
			funcRead = newRead;
			actWrite = newWrite;
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
	}
}
