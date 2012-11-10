using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class DirectionalDataPort
	{
		protected byte _data;
		protected byte _remoteData;
		public byte Direction;
		public Action<byte> WritePort;

		public DirectionalDataPort(byte initData, byte initDirection, byte initRemoteData)
		{
			_remoteData = initRemoteData;
			_data = initData;
			Direction = initDirection;
			WritePort = WritePortDummy;
			WritePort(_data);
		}

		public byte Data
		{
			get
			{
				byte result = _remoteData;
				result &= (byte)~Direction;
				result |= (byte)(_data & Direction);
				return result;
			}
			set
			{
				_data = value;
				WritePort(_data);
			}
		}

		public byte RemoteData
		{
			get
			{
				return _remoteData;
			}
			set
			{
				_remoteData = value;
			}
		}

		private void WritePortDummy(byte val)
		{
		}
	}
}
