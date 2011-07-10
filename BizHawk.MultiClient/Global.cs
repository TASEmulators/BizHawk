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
		public static Controller GBControls;
		public static Controller NullControls;

		//TODO should have one of these per movie!!!! should not be global.
		public static MovieControllerAdapter MovieControllerAdapter = new MovieControllerAdapter();
		public static CopyControllerAdapter MovieInputSourceAdapter = new CopyControllerAdapter();

		public static MultitrackRewiringControllerAdapter MultitrackRewiringControllerAdapter = new MultitrackRewiringControllerAdapter();

		//user -> ActiveController -> TurboAdapter(TBD) -> Lua(?) -> MultitrackRewiringControllerAdapter -> MovieInputSourceAdapter -> MovieInputController -> ControllerOutput(1) -> Game
		//(1)->Input Display
		
		//the original source controller, bound to the user, sort of the "input" port for the chain, i think
		public static Controller ActiveController;
		
		//the "output" port for the controller chain. 
		public static IController ControllerOutput;

		public static Input.InputCoalescer InputCoalescer;

		public static string GetOutputControllersAsMnemonic()
		{
			MnemonicsGenerator mg = new MnemonicsGenerator();
			mg.SetSource(Global.ControllerOutput);
			return mg.GetControllersAsMnemonic();
		}

		//TODO - wtf is this being used for
		public static bool MovieMode;
	}
}