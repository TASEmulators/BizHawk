using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64 : IDriveLight
	{
		public bool DriveLightEnabled => _board != null && (_board.CartPort.DriveLightEnabled || _board.Serial.DriveLightEnabled);
		public bool DriveLightOn => _board != null && (_board.CartPort.DriveLightOn || _board.Serial.DriveLightOn);

		public string DriveLightIconDescription => "Cart or Disk Activity LED";
	}
}
