using System.Collections.Generic;
using BizHawk.Emulation.Common;

// ReSharper disable StyleCop.SA1401
namespace BizHawk.Client.Common
{
	public static class Global
	{
		public static IEmulator Emulator { get; set; }
		public static Config Config { get; set; }
		public static GameInfo Game { get; set; }
		public static CheatCollection CheatList { get; set; } = new CheatCollection();
		public static FirmwareManager FirmwareManager { get; set; }

		public static IMovieSession MovieSession { get; set; } = new MovieSession();

		/// <summary>
		/// Used to disable secondary throttling (e.g. vsync, audio) for unthrottled modes or when the primary (clock) throttle is taking over (e.g. during fast forward/rewind).
		/// </summary>
		public static bool DisableSecondaryThrottling { get; set; }

		/// <summary>
		/// The maximum number of milliseconds the sound output buffer can go below full before causing a noticeable sound interruption.
		/// </summary>
		public static int SoundMaxBufferDeficitMs { get; set; }

		// the movie will be spliced in between these if it is present
		public static CopyControllerAdapter MovieInputSourceAdapter { get;  } = new CopyControllerAdapter();
		public static CopyControllerAdapter MovieOutputHardpoint { get; } = new CopyControllerAdapter();
		public static MultitrackRewiringControllerAdapter MultitrackRewiringAdapter { get; } = new MultitrackRewiringControllerAdapter();

		// don't take my word for it, since the final word is actually in RewireInputChain, but here is a guide...
		// user -> Input -> ActiveController -> UDLR -> StickyXORPlayerInputAdapter -> TurboAdapter(TBD) -> Lua(?TBD?) -> ..
		// .. -> MultitrackRewiringControllerAdapter -> MovieInputSourceAdapter -> (MovieSession) -> MovieOutputAdapter -> ControllerOutput(1) -> Game
		// (1)->Input Display

		// the original source controller, bound to the user, sort of the "input" port for the chain, i think
		public static Controller ActiveController { get; set; }

		// rapid fire version on the user controller, has its own key bindings and is OR'ed against ActiveController
		public static AutofireController AutoFireController { get; set; }

		// the "output" port for the controller chain.
		public static CopyControllerAdapter ControllerOutput { get; } = new CopyControllerAdapter();

		public static UdlrControllerAdapter UD_LR_ControllerAdapter { get; } = new UdlrControllerAdapter();

		public static AutoFireStickyXorAdapter AutofireStickyXORAdapter { get; } = new AutoFireStickyXorAdapter();

		/// <summary>
		/// provides an opportunity to mutate the player's input in an autohold style
		/// </summary>
		public static StickyXorAdapter StickyXORAdapter { get; } = new StickyXorAdapter();

		/// <summary>
		/// Used to AND to another controller, used for <see cref="JoypadApi.Set(Dictionary{string, bool}, int?)">JoypadApi.Set</see>
		/// </summary>
		public static OverrideAdapter ButtonOverrideAdaptor { get; } = new OverrideAdapter();

		/// <summary>
		/// fire off one-frame logical button clicks here. useful for things like ti-83 virtual pad and reset buttons
		/// </summary>
		public static ClickyVirtualPadController ClickyVirtualPadController { get; } = new ClickyVirtualPadController();

		public static Controller ClientControls { get; set; }

		// Input state for game controller inputs are coalesced here
		// This relies on a client specific implementation!
		public static SimpleController ControllerInputCoalescer { get; set; }

		public static Dictionary<string, object> UserBag { get; set; } = new Dictionary<string, object>();
	}
}
