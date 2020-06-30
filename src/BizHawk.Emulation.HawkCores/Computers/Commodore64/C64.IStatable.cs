using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public sealed partial class C64
	{
		private void SyncState(Serializer ser)
		{
			ser.BeginSection("core");
			ser.Sync(nameof(_frameCycles), ref _frameCycles);
			ser.Sync(nameof(Frame), ref _frame);
			ser.Sync(nameof(IsLagFrame), ref _isLagFrame);
			ser.Sync(nameof(LagCount), ref _lagCount);
			ser.Sync(nameof(CurrentDisk), ref _currentDisk);
			ser.Sync("PreviousDiskPressed", ref _prevPressed);
			ser.Sync("NextDiskPressed", ref _nextPressed);
			ser.BeginSection("Board");
			_board.SyncState(ser);
			ser.EndSection();
			ser.EndSection();
		}
	}
}
