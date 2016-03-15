using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64 : IDriveLight
	{
		public bool DriveLightEnabled { get { return _board != null && (_board.CartPort.DriveLightEnabled || _board.Serial.DriveLightEnabled); } }
		public bool DriveLightOn { get { return _board != null && (_board.CartPort.DriveLightOn || _board.Serial.DriveLightOn);} }
	}
}
