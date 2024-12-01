using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cassette
{
	public abstract class CassettePortDevice
	{
		public Func<bool> ReadDataOutput;
		public Func<bool> ReadMotor;

		public virtual void ExecutePhase2()
		{
		}

		public virtual void HardReset()
		{
		}

		public virtual bool ReadDataInputBuffer()
		{
			return true;
		}

		public virtual bool ReadSenseBuffer()
		{
			return true;
		}

		public abstract void SyncState(Serializer ser);

		// Exposed for memory domains, should not be used for actual emulation implementation
		public abstract byte[] TapeDataDomain { get; }
	}
}
