using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial
{
	public abstract class SerialPortDevice
	{
		public Func<bool> ReadMasterAtn = () => true;
		public Func<bool> ReadMasterClk = () => true;
		public Func<bool> ReadMasterData = () => true;

		public virtual void ExecutePhase()
		{
		}

		public virtual void ExecuteDeferred(int cycles)
		{
		}

		public virtual void HardReset()
		{
		}

		public virtual bool ReadDeviceClk()
		{
			return true;
		}

		public virtual bool ReadDeviceData()
		{
			return true;
		}

		public virtual bool ReadDeviceLight()
		{
			return false;
		}

		public abstract void SyncState(Serializer ser);
	}
}
