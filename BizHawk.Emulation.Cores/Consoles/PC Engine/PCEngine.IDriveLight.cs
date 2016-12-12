using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine : IDriveLight
	{
		public bool DriveLightEnabled { get; private set; }

		public bool DriveLightOn { get; internal set; }
	}
}
