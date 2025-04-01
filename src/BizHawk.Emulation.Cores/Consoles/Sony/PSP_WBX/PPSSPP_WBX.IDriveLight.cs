using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Consoles.Sony.PSP_WBX
{
	public partial class PPSSPP_WBX : IDriveLight
	{
		public bool DriveLightEnabled { get; private set; }
		public bool DriveLightOn { get; private set; }
		public string DriveLightIconDescription => "Drive Activity";
	}
}