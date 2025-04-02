using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Consoles.Sony.PSP
{
	public partial class PPSSPP : IDriveLight
	{
		public bool DriveLightEnabled { get; private set; }
		public bool DriveLightOn { get; private set; }
		public string DriveLightIconDescription => "Drive Activity";
	}
}