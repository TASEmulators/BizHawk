namespace BizHawk
{
	public class CoreInputComm
	{
		public int NES_BackdropColor;
		public bool NES_UnlimitedSprites;
		public bool NES_ShowBG, NES_ShowOBJ;
		public bool PCE_ShowBG1, PCE_ShowOBJ1, PCE_ShowBG2, PCE_ShowOBJ2;
	}

	public class CoreOutputComm
	{
		public double VsyncRate = 60;
		public string RomStatusAnnotation;
		public string RomStatusDetails;
	}
}
