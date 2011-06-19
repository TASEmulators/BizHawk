using SlimDX.Direct3D9;
using SlimDX.DirectSound;

namespace BizHawk.MultiClient
{
	public static class Global
	{
		public static MainForm MainForm;
		public static DirectSound DSound;
		public static Direct3D Direct3D;
		public static Sound Sound;
		public static IRenderer RenderPanel;
		public static Config Config;
		public static IEmulator Emulator;
		public static CoreInputComm CoreInputComm;
		public static RomGame Game;
		public static Controller ClientControls;
		public static Controller SMSControls;
		public static Controller PCEControls;
		public static Controller GenControls;
		public static Controller TI83Controls;
		public static Controller NESControls;
		public static Controller ActiveController;
		public static Controller NullControls;
	}
}