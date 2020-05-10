using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common.InputAdapterExtensions;

namespace BizHawk.Client.Common
{
	
	// don't take my word for it, but here is a guide...
	// user -> Input -> ActiveController -> UDLR -> StickyXORPlayerInputAdapter -> TurboAdapter(TBD) -> Lua(?TBD?) -> ..
	// .. -> MultitrackRewiringControllerAdapter -> MovieInputSourceAdapter -> (MovieSession) -> MovieOutputAdapter -> ControllerOutput(1) -> Game
	// (1)->Input Display
	public class InputManager
	{
		// the movie will be spliced in between these if it is present
		public CopyControllerAdapter MovieInputSourceAdapter { get;  } = new CopyControllerAdapter();
		public CopyControllerAdapter MovieOutputHardpoint { get; } = new CopyControllerAdapter();
		public MultitrackRewiringControllerAdapter MultitrackRewiringAdapter { get; } = new MultitrackRewiringControllerAdapter();

		// the original source controller, bound to the user, sort of the "input" port for the chain, i think
		public Controller ActiveController { get; set; } // TODO: private setter, add a method that takes both controllers in 

		// rapid fire version on the user controller, has its own key bindings and is OR'ed against ActiveController
		public AutofireController AutoFireController { get; set; } // TODO: private setter, add a method that takes both controllers in 

		// the "output" port for the controller chain.
		public CopyControllerAdapter ControllerOutput { get; } = new CopyControllerAdapter();

		public UdlrControllerAdapter UdLRControllerAdapter { get; } = new UdlrControllerAdapter();

		public AutoFireStickyXorAdapter AutofireStickyXorAdapter { get; } = new AutoFireStickyXorAdapter();

		/// <summary>
		/// provides an opportunity to mutate the player's input in an autohold style
		/// </summary>
		public StickyXorAdapter StickyXorAdapter { get; } = new StickyXorAdapter();

		/// <summary>
		/// Used to AND to another controller, used for <see cref="JoypadApi.Set(Dictionary{string, bool}, int?)">JoypadApi.Set</see>
		/// </summary>
		public OverrideAdapter ButtonOverrideAdapter { get; } = new OverrideAdapter();

		/// <summary>
		/// fire off one-frame logical button clicks here. useful for things like ti-83 virtual pad and reset buttons
		/// </summary>
		public ClickyVirtualPadController ClickyVirtualPadController { get; } = new ClickyVirtualPadController();

		// Input state for game controller inputs are coalesced here
		// This relies on a client specific implementation!
		public SimpleController ControllerInputCoalescer { get; set; }

		public Controller ClientControls { get; set; }

		public void RewireInputChain()
		{
			ControllerInputCoalescer.Clear();
			ControllerInputCoalescer.Definition = ActiveController.Definition;

			UdLRControllerAdapter.Source = ActiveController.Or(AutoFireController);

			StickyXorAdapter.Source = UdLRControllerAdapter;
			AutofireStickyXorAdapter.Source = StickyXorAdapter;

			MultitrackRewiringAdapter.Source = AutofireStickyXorAdapter;
			MovieInputSourceAdapter.Source = MultitrackRewiringAdapter;
			ControllerOutput.Source = MovieOutputHardpoint;

			Global.MovieSession.RecreateMovieController(MovieInputSourceAdapter.Definition);

			// connect the movie session before MovieOutputHardpoint if it is doing anything
			// otherwise connect the MovieInputSourceAdapter to it, effectively bypassing the movie session
			if (Global.MovieSession != null)
			{
				MovieOutputHardpoint.Source = Global.MovieSession.MovieController;
			}
			else
			{
				MovieOutputHardpoint.Source = MovieInputSourceAdapter;
			}
		}

		public void SyncControls(IEmulator emulator, Config config)
		{
			var def = emulator.ControllerDefinition;

			ActiveController = BindToDefinition(def, config.AllTrollers, config.AllTrollersAnalog);
			AutoFireController = BindToDefinitionAF(def, emulator, config.AllTrollersAutoFire);

			// allow propagating controls that are in the current controller definition but not in the prebaked one
			// these two lines shouldn't be required anymore under the new system?
			ActiveController.ForceType(new ControllerDefinition(def));
			ClickyVirtualPadController.Definition = new ControllerDefinition(def);
			RewireInputChain();
		}

		private static Controller BindToDefinition(ControllerDefinition def, IDictionary<string, Dictionary<string, string>> allBinds, IDictionary<string, Dictionary<string, AnalogBind>> analogBinds)
		{
			var ret = new Controller(def);
			if (allBinds.TryGetValue(def.Name, out var binds))
			{
				foreach (var btn in def.BoolButtons)
				{
					if (binds.TryGetValue(btn, out var bind))
					{
						ret.BindMulti(btn, bind);
					}
				}
			}

			if (analogBinds.TryGetValue(def.Name, out var aBinds))
			{
				foreach (var btn in def.AxisControls)
				{
					if (aBinds.TryGetValue(btn, out var bind))
					{
						ret.BindAxis(btn, bind);
					}
				}
			}

			return ret;
		}

		private static AutofireController BindToDefinitionAF(ControllerDefinition def, IEmulator emulator, IDictionary<string, Dictionary<string, string>> allBinds)
		{
			var ret = new AutofireController(def, emulator);
			if (allBinds.TryGetValue(def.Name, out var binds))
			{
				foreach (var btn in def.BoolButtons)
				{
					if (binds.TryGetValue(btn, out var bind))
					{
						ret.BindMulti(btn, bind);
					}
				}
			}

			return ret;
		}
	}
}