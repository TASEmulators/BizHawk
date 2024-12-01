using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.User
{
	public sealed class UserPort
	{
		public Func<bool> ReadCounter1;
		public Func<bool> ReadCounter2;
		public Func<bool> ReadHandshake;
		public Func<bool> ReadSerial1;
		public Func<bool> ReadSerial2;

		private bool _connected;
		private UserPortDevice _device;

		public void Connect(UserPortDevice device)
		{
			_device = device;
			_connected = _device != null;
			if (_device != null)
			{
				_device.ReadCounter1 = () => ReadCounter1();
				_device.ReadCounter2 = () => ReadCounter2();
				_device.ReadHandshake = () => ReadHandshake();
				_device.ReadSerial1 = () => ReadSerial1();
				_device.ReadSerial2 = () => ReadSerial2();
			}
		}

		public void Disconnect()
		{
			_connected = false;
			_device = null;
		}

		public void HardReset()
		{
			if (_connected)
			{
				_device.HardReset();
			}
		}

		public bool ReadAtn()
		{
			return !_connected || _device.ReadAtn();
		}

		public int ReadData()
		{
			return !_connected ? 0xFF : _device.ReadData();
		}

		public bool ReadFlag2()
		{
			return !_connected || _device.ReadFlag2();
		}

		public bool ReadPa2()
		{
			return !_connected || _device.ReadPa2();
		}

		public bool ReadReset()
		{
			return !_connected || _device.ReadReset();
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_connected), ref _connected);
			_device?.SyncState(ser);
		}
	}
}
