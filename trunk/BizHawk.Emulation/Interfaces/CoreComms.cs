using System.Collections.Generic;

namespace BizHawk
{
	public class CoreInputComm
	{
		public int NES_BackdropColor;
		public bool NES_UnlimitedSprites;
		public bool NES_ShowBG, NES_ShowOBJ;
	}

	public class CoreOutputComm
	{
		public double VsyncRate = 60;
		public RomStatus RomStatus;
		public string RomStatusAnnotation;
		public string RomStatusDetails;
	}

}