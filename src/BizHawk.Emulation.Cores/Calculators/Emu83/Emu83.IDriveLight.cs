using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators.Emu83
{
	public partial class Emu83 : IDriveLight
	{
		public bool DriveLightEnabled => true;
		public bool DriveLightOn => LibEmu83.TI83_GetLinkActive(Context);

		public string DriveLightIconDescription => "Link Activity";
	}
}
