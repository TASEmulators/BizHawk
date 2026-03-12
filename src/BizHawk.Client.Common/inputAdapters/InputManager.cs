using System.Collections.Generic;
using System.Drawing;
using System.Linq;

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
		public ControllerInputCoalescer ControllerInputCoalescer { get; set; } = new();

		/// <summary>
		/// Created for <see cref="IInputApi"/>. Receives only buttons, and receives them regardless of input priority setting.
		/// </summary>
		public ApiInputCoalescer HostInputCoalescer { get; } = new();

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
			// they were *mostly* not required, see https://github.com/TASEmulators/BizHawk/issues/3458 --yoshi 2022
			ClickyVirtualPadController.Definition = def;

			// Wire up input chain

			UdLRControllerAdapter.Source = ActiveController.Or(AutoFireController);
			UdLRControllerAdapter.OpposingDirPolicy = config.OpposingDirPolicy;

			StickyController = StickyHoldController.Or(StickyAutofireController);

			session.MovieIn = UdLRControllerAdapter.Xor(StickyController);
			session.StickySource = StickyController;
			ControllerOutput.Source = session.MovieOut;
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

		// input state which has been destined for client hotkey consumption are colesced here
		private readonly InputCoalescer _hotkeyCoalescer = new InputCoalescer();

		/// <summary>
		/// Processes queued inputs and triggers input evets (i.e. hotkeys), but does not update output controllers.<br/>
		/// </summary>
		/// <param name="processSpecialInput">All input events are forwarded out here.
		/// This allows things like Windows' standard alt hotkeys (for menu items) to be handled by the
		/// caller if the input didn't alrady do something else.
		/// <br/>The second parameter is true if the input already did something (hotkey or controller input).</param>
		public void ProcessInput(IPhysicalInputSource source, Func<string, bool> processHotkey, Config config, Action<InputEvent, bool> processSpecialInput)
		{
			// loop through all available events
			InputEvent ie;
			while ((ie = source.DequeueEvent()) != null)
			{
				// useful debugging:
				// Console.WriteLine(ie);

				// TODO - wonder what happens if we pop up something interactive as a response to one of these hotkeys? may need to purge further processing

				HostInputCoalescer.Receive(ie);

				var hotkeyTriggers = ClientControls.SearchBindings(ie.LogicalButton.ToString());
				bool isEmuInput = ActiveController.HasBinding(ie.LogicalButton.ToString());

				bool shouldDoHotkey = config.InputHotkeyOverrideOptions != Config.InputPriority.INPUT;
				bool shouldDoEmuInput = config.InputHotkeyOverrideOptions != Config.InputPriority.HOTKEY;
				if (shouldDoEmuInput && !isEmuInput) shouldDoHotkey = true;

				bool didHotkey = false;
				if (shouldDoHotkey)
				{
					if (ie.EventType is InputEventType.Press)
					{
						didHotkey = hotkeyTriggers.Aggregate(false, (current, trigger) => current | processHotkey(trigger));
					}
					_hotkeyCoalescer.Receive(ie);
				}

				if (!didHotkey) shouldDoEmuInput = true;
				if (shouldDoEmuInput)
				{
					// We have to do this even if it's not an emu input
					// because if Shift+A is bound it needs to see the Shift and A independently.
					ControllerInputCoalescer.Receive(ie);
				}
				bool didEmuInput = shouldDoEmuInput && isEmuInput;

				processSpecialInput(ie, didHotkey | didEmuInput);
			} // foreach event

			// also handle axes
			// we'll need to isolate the mouse coordinates so we can translate them
			int? mouseDeltaX = null, mouseDeltaY = null;
			foreach (var f in source.GetAxisValues())
			{
				if (f.Key == "RMouse X")
					mouseDeltaX = f.Value;
				else if (f.Key == "RMouse Y")
					mouseDeltaY = f.Value;
				else ControllerInputCoalescer.AcceptNewAxis(f.Key, f.Value);
			}

			if (mouseDeltaX != null && mouseDeltaY != null)
			{
				var mouseSensitivity = config.RelativeMouseSensitivity / 100.0f;
				var x = mouseDeltaX.Value * mouseSensitivity;
				var y = mouseDeltaY.Value * mouseSensitivity;
				const int MAX_REL_MOUSE_RANGE = 120; // arbitrary
				x = Math.Min(Math.Max(x, -MAX_REL_MOUSE_RANGE), MAX_REL_MOUSE_RANGE) / MAX_REL_MOUSE_RANGE;
				y = Math.Min(Math.Max(y, -MAX_REL_MOUSE_RANGE), MAX_REL_MOUSE_RANGE) / MAX_REL_MOUSE_RANGE;
				ControllerInputCoalescer.AcceptNewAxis("RMouse X", (int)(x * 10000));
				ControllerInputCoalescer.AcceptNewAxis("RMouse Y", (int)(y * 10000));
			}
		}

		/// <summary>
		/// Update output controllers. Call <see cref="ProcessInput(IPhysicalInputSource, Func{string, bool}, Config, Action{InputEvent, bool})"/> shortly before this.
		/// </summary>
		public void RunControllerChain(Config config)
		{
			// client, only one step
			ClientControls.LatchFromPhysical(_hotkeyCoalescer);

			// controller, which actually has a chain
			List<string> oldPressedButtons = ActiveController.PressedButtons;

			ActiveController.LatchFromPhysical(ControllerInputCoalescer);
			ActiveController.OR_FromLogical(ClickyVirtualPadController);
			AutoFireController.LatchFromPhysical(ControllerInputCoalescer);

			if (config.N64UseCircularAnalogConstraint)
			{
				ActiveController.ApplyAxisConstraints("Natural Circle");
			}

			if (ClientControls["Autohold"] || ClientControls["Autofire"])
			{
				List<string> newPressedButtons = ActiveController.PressedButtons;
				List<string> justPressedButtons = new();
				foreach (string button in newPressedButtons)
				{
					if (!oldPressedButtons.Contains(button)) justPressedButtons.Add(button);
				}
				if (justPressedButtons.Count != 0)
				{
					if (ClientControls["Autohold"])
					{
						StickyHoldController.MassToggleStickyState(justPressedButtons);
					}
					else
					{
						StickyAutofireController.MassToggleStickyState(justPressedButtons);
					}
				}
			}

			// autohold/autofire must not be affected by the following inputs
			ActiveController.Overrides(ButtonOverrideAdapter);
		}
	}
}
