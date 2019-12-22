using System.Collections.Generic;
using BizHawk.Emulation.Common;

// ReSharper disable StyleCop.SA1401
namespace BizHawk.Client.Common
{
	public static class Global
	{
		public static IEmulator Emulator;
		public static Config Config;
		public static GameInfo Game;
		public static CheatCollection CheatList;
		public static FirmwareManager FirmwareManager;

		public static IMovieSession MovieSession = new MovieSession();

		/// <summary>
		/// Used to disable secondary throttling (e.g. vsync, audio) for unthrottled modes or when the primary (clock) throttle is taking over (e.g. during fast forward/rewind).
		/// </summary>
		public static bool DisableSecondaryThrottling;

		/// <summary>
		/// The maximum number of milliseconds the sound output buffer can go below full before causing a noticeable sound interruption.
		/// </summary>
		public static int SoundMaxBufferDeficitMs;

		// the movie will be spliced in between these if it is present
		public static readonly CopyControllerAdapter MovieInputSourceAdapter = new CopyControllerAdapter();
		public static readonly CopyControllerAdapter MovieOutputHardpoint = new CopyControllerAdapter();
		public static readonly MultitrackRewiringControllerAdapter MultitrackRewiringAdapter = new MultitrackRewiringControllerAdapter();

		// dont take my word for it, since the final word is actually in RewireInputChain, but here is a guide...
		// user -> Input -> ActiveController -> UDLR -> StickyXORPlayerInputAdapter -> TurboAdapter(TBD) -> Lua(?TBD?) -> ..
		// .. -> MultitrackRewiringControllerAdapter -> MovieInputSourceAdapter -> (MovieSession) -> MovieOutputAdapter -> ControllerOutput(1) -> Game
		// (1)->Input Display

		// the original source controller, bound to the user, sort of the "input" port for the chain, i think
		public static Controller ActiveController;

		// rapid fire version on the user controller, has its own key bindings and is OR'ed against ActiveController
		public static AutofireController AutoFireController;

		// the "output" port for the controller chain.
		public static readonly CopyControllerAdapter ControllerOutput = new CopyControllerAdapter();

		public static readonly UD_LR_ControllerAdapter UD_LR_ControllerAdapter = new UD_LR_ControllerAdapter();

		public static readonly AutoFireStickyXorAdapter AutofireStickyXORAdapter = new AutoFireStickyXorAdapter();

		/// <summary>
		/// provides an opportunity to mutate the player's input in an autohold style
		/// </summary>
		public static readonly StickyXorAdapter StickyXORAdapter = new StickyXorAdapter();

		/// <summary>
		/// Used to AND to another controller, used for <see cref="JoypadApi.Set(System.Collections.Generic.Dictionary{string,bool},System.Nullable{int})">JoypadApi.Set</see>
		/// </summary>
		public static readonly OverrideAdaptor ButtonOverrideAdaptor = new OverrideAdaptor();

		/// <summary>
		/// fire off one-frame logical button clicks here. useful for things like ti-83 virtual pad and reset buttons
		/// </summary>
		public static readonly ClickyVirtualPadController ClickyVirtualPadController = new ClickyVirtualPadController();

		public static Controller ClientControls;

		// Input state which has been estine for game controller inputs are coalesce here
		// This relies on a client specific implementation!
		public static SimpleController ControllerInputCoalescer;

		public static Dictionary<string, object> UserBag = new Dictionary<string, object>();
	}
}
