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
        public static RomGame Game;
        public static IController ClientControls;
        public static IController SMSControls;
        public static IController PCEControls;
        public static IController GenControls;
		public static IController TI83Controls;
    }
}