using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	
	// don't take my word for it, but here is a guide...
	// user -> Input -> ActiveController -> UDLR -> StickyXORPlayerInputAdapter -> TurboAdapter(TBD) -> Lua(?TBD?) -> ..
	// .. -> MovieInputSourceAdapter -> (MovieSession) -> MovieOutputAdapter -> ControllerOutput(1) -> Game
	// (1)->Input Display
#pragma warning disable MA0104 // unlikely to conflict with System.Windows.Input.InputManager
	public class InputManager
#pragma warning restore MA0104
	{
		// the original source controller, bound to the user, sort of the "input" port for the chain, i think
		public Controller ActiveController { get; private set; }

		// rapid fire version on the user controller, has its own key bindings and is OR'ed against ActiveController
		public AutofireController AutoFireController { get; private set; }

		// the "output" port for the controller chain.
		public CopyControllerAdapter ControllerOutput { get; } = new CopyControllerAdapter();

		private UdlrControllerAdapter UdLRControllerAdapter { get; } = new UdlrControllerAdapter();

		public StickyHoldController StickyHoldController { get; private set; }
		public StickyAutofireController StickyAutofireController { get; private set; }

		// StickyHold OR StickyAutofire
		public IController StickyController { get; private set; }

		/// <summary>
		/// Used to AND to another controller, used for <see cref="IJoypadApi.Set(IReadOnlyDictionary{string, bool}, int?)">JoypadApi.Set</see>
		/// </summary>
		public OverrideAdapter ButtonOverrideAdapter { get; } = new OverrideAdapter();

		/// <summary>
		/// fire off one-frame logical button clicks here. useful for things like ti-83 virtual pad and reset buttons
		/// </summary>
		public ClickyVirtualPadController ClickyVirtualPadController { get; } = new ClickyVirtualPadController();

		// Input state for game controller inputs are coalesced here
		// This relies on a client specific implementation!
		public ControllerInputCoalescer ControllerInputCoalescer { get; set; }

		public Controller ClientControls { get; set; }

		public Func<(Point Pos, long Scroll, bool LMB, bool MMB, bool RMB, bool X1MB, bool X2MB)> GetMainFormMouseInfo { get; set; }

		public void SyncControls(IEmulator emulator, IMovieSession session, Config config)
		{
			var def = emulator.ControllerDefinition;
			def.BuildMnemonicsCache(emulator.SystemId);

			ActiveController = BindToDefinition(def, config.AllTrollers, config.AllTrollersAnalog, config.AllTrollersFeedbacks);
			AutoFireController = BindToDefinitionAF(emulator, config.AllTrollersAutoFire, config.AutofireOn, config.AutofireOff);
			StickyHoldController = new StickyHoldController(def);
			StickyAutofireController = new StickyAutofireController(def, config.AutofireOn, config.AutofireOff);

			// allow propagating controls that are in the current controller definition but not in the prebaked one
			// these two lines shouldn't be required anymore under the new system? --natt 2013
			// they were mostly not required, see #3458 --yoshi 2022
			ClickyVirtualPadController.Definition = def;

			// Wire up input chain

			UdLRControllerAdapter.Source = ActiveController.Or(AutoFireController);
			UdLRControllerAdapter.OpposingDirPolicy = config.OpposingDirPolicy;

			StickyController = StickyHoldController.Or(StickyAutofireController);

			session.MovieIn = UdLRControllerAdapter.Xor(StickyController);
			session.StickySource = StickyController;
			ControllerOutput.Source = session.MovieOut;
		}

		public void ToggleStickies()
		{
			StickyHoldController.MassToggleStickyState(ActiveController.PressedButtons);
			StickyAutofireController.MassToggleStickyState(AutoFireController.PressedButtons); // does this even make sense?
		}

		public void ToggleAutoStickies()
		{
			StickyAutofireController.MassToggleStickyState(ActiveController.PressedButtons);
		}

		private static Controller BindToDefinition(
			ControllerDefinition def,
			IDictionary<string, Dictionary<string, string>> allBinds,
			IDictionary<string, Dictionary<string, AnalogBind>> analogBinds,
			IDictionary<string, Dictionary<string, FeedbackBind>> feedbackBinds)
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
				foreach (var btn in def.Axes.Keys)
				{
					if (aBinds.TryGetValue(btn, out var bind))
					{
						ret.BindAxis(btn, bind);
					}
				}
			}

			if (feedbackBinds.TryGetValue(def.Name, out var fBinds))
			{
				foreach (var channel in def.HapticsChannels)
				{
					if (fBinds.TryGetValue(channel, out var bind)) ret.BindFeedbackChannel(channel, bind);
				}
			}

			return ret;
		}

		private static AutofireController BindToDefinitionAF(
			IEmulator emulator,
			IDictionary<string, Dictionary<string, string>> allBinds,
			int on,
			int off)
		{
			var ret = new AutofireController(emulator, on, off);
			if (allBinds.TryGetValue(emulator.ControllerDefinition.Name, out var binds))
			{
				foreach (var btn in emulator.ControllerDefinition.BoolButtons)
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
