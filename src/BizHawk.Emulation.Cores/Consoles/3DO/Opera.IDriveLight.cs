using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Panasonic3DO
{
	public partial class Opera : IDriveLight
	{
		public bool DriveLightEnabled { get; private set; }
		public bool DriveLightOn { get; private set; }
		public string DriveLightIconDescription => "Drive Activity";
	}
}
