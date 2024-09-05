using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES : IDriveLight
	{
		public bool DriveLightEnabled { get; }

		public bool DriveLightOn { get; private set; }

		public string DriveLightIconDescription => "Disk Drive Activity";
	}
}
