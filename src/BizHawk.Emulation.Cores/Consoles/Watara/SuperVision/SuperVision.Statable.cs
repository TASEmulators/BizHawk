using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class SuperVision
	{
		private void SyncState(Serializer ser)
		{
			ser.BeginSection("SuperVision");
			ser.Sync(nameof(BankSelect), ref BankSelect);

			ser.Sync(nameof(_frameClock), ref _frameClock);
			ser.Sync(nameof(_frame), ref _frame);
			ser.Sync(nameof(_isLag), ref _isLag);
			ser.Sync(nameof(_lagCount), ref _lagCount);

			ser.Sync(nameof(_buttonsState), ref _buttonsState, false);

			ser.Sync(nameof(_cpuMemoryAccess), ref _cpuMemoryAccess);

			_cpu.SyncState(ser);
			_asic.SyncState(ser);

			ser.EndSection();

			if (ser.IsReader)
			{
				SyncAllByteArrayDomains();
			}
		}
	}
}
