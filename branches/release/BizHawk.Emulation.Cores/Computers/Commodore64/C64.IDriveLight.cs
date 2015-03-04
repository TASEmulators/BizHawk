using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64 : IDriveLight
	{
		public bool DriveLightEnabled { get { return true; } }
		public bool DriveLightOn { get; private set; }

		public bool DriveLED
		{
			get
			{
				//return (disk.PeekVia1(0x00) & 0x08) != 0;
				return false;
			}
		}
	}
}
