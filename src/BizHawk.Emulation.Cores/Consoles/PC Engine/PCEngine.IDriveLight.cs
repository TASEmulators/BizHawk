using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine : IDriveLight
	{
		public bool DriveLightEnabled { get; } = true;

		public bool DriveLightOn { get; internal set; }

		public string DriveLightIconDescription => "CD Drive Activity";
	}
}
