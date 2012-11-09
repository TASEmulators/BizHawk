using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class DirectionalDataPort
	{
		protected byte _data;
		public byte Direction;
		public Action<byte> WritePort;

		public DirectionalDataPort(byte initData, byte initDirection)
		{
			_data = initData;
			Direction = initDirection;
			WritePort = WritePortDummy;
			WritePort(_data);
		}

		public byte Data
		{
			get
			{
				return (byte)(_data);
			}
			set
			{
				_data &= (byte)~Direction;
				_data |= (byte)(value & Direction);
				WritePort(_data);
			}
		}

		private void WritePortDummy(byte val)
		{
		}
	}
}
