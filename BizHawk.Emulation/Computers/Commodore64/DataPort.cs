using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class DirectionalDataPort
	{
		private int _data;
		public byte Direction;

		public DirectionalDataPort(byte initData, byte initDirection)
		{
			_data = initData;
			Direction = initDirection;
		}

		public byte Data
		{
			get
			{
				return (byte)(_data);
			}
			set
			{
				_data &= ~Direction;
				_data |= (value & Direction);
			}
		}
	}
}
