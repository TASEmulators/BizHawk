using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IDriveLight
	{
		public bool DriveLightEnabled { get; }
		public bool DriveLightOn { get; private set; }

		public string DriveLightIconDescription => "CD Drive Activity";

		private bool _driveLight;
	}
}
