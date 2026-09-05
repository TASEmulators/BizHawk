using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Tests.Client.Common.input;
using BizHawk.Tests.Client.Common.Movie;

namespace BizHawk.Tests.Client.Common
{
	[TestClass]
	public class InputManagerTests
	{
		internal class Context
		{
			public InputManager manager = new();
			public Config config = new();
			public List<string> triggeredHotkeys = new();
			public IEmulator emulator;
			public FakeInputSource source = new();

			private string[] _hotkeys;

			public Context(string[]? hotkeys = null)
			{
				hotkeys ??= [ ];

				emulator = new FakeEmulator();
				manager.SyncControls(emulator, new FakeMovieSession(emulator), config);

				ControllerDefinition cd = new("fake")
				{
					BoolButtons = hotkeys.ToList(),
				};
				manager.ClientControls = new Controller(cd.MakeImmutable());

				_hotkeys = hotkeys;
			}

			public Context(IEmulator emulator, IMovieSession movieSession)
			{
				this.emulator = emulator;
				manager.SyncControls(emulator, movieSession, config);

				_hotkeys = [ ];
				ControllerDefinition cd = new("fake")
				{
					BoolButtons = _hotkeys.ToList(),
				};
				manager.ClientControls = new Controller(cd.MakeImmutable());
			}

			/// <summary>
			/// After running this, <see cref="InputManager.ControllerOutput"/> will hold the values for the NEXT frame, not the one just simulated.
			/// </summary>
			public void EmulateFrameAdvance(bool lag = false)
			{
				BasicInputProcessing();
				emulator.FrameAdvance(manager.ControllerOutput, false);
				manager.AfterFrame(lag, config);
				BasicInputProcessing(); // must call this again to ensure asserts on ControllerOutput see all the results of AfterFrame
			}

			/// <summary>
			/// Process all the simulated inputs, and update the controllers.
			/// Call this before (or instead of) <see cref="EmulateFrameAdvance(bool)"/> when you need to test the input that will be given to the next frame.
			/// </summary>
			public void BasicInputProcessing()
			{
				manager.ProcessInput(source, ProcessHotkey, config, (_, _) => { });
				manager.RunControllerChain(config);
			}

			public bool ProcessHotkey(string trigger)
			{
				triggeredHotkeys.Add(trigger);
				return _hotkeys.Contains(trigger);
			}
		}

		private static readonly string[] _hotkeys = [ "Hotkey 1", "Autofire", "Autohold" ];

#pragma warning disable BHI1600 //TODO disambiguate assert calls
		[TestMethod]
		public void BasicControllerInput()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");

			source.MakePressEvent("Q");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.IsFalse(manager.ControllerOutput.IsPressed("B"));
			Assert.IsFalse(manager.ControllerOutput.IsPressed("C"));

			source.MakeReleaseEvent("Q");
			context.BasicInputProcessing();
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void ControllerInputWithOneModifier()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Shift+Q");

			source.MakePressEvent("Shift");
			source.MakePressEvent("Shift+Q");

			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			source.MakeReleaseEvent("Shift");
			context.BasicInputProcessing();
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void ControllerInputWithMultipleModifiers()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Ctrl+Shift+Q");

			source.MakePressEvent("Ctrl");
			source.MakePressEvent("Ctrl+Shift");
			source.MakePressEvent("Ctrl+Shift+Q");

			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			source.MakeReleaseEvent("Shift");
			context.BasicInputProcessing();
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void ControllerInputAcceptsOutOfOrderModifier()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Shift+Q");

			source.MakePressEvent("Q");
			context.BasicInputProcessing();
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));

			source.MakePressEvent("Shift");
			context.BasicInputProcessing();
			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void ControllerInputIgnoresExtraModifier()
		{
			// Extra modifiers are ignored so that we can do inputs while doing another input that's bound to a modifier.
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");
			manager.ActiveController.BindMulti("B", "Ctrl+Q");

			source.MakePressEvent("Shift");
			source.MakePressEvent("Shift+Q");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			source.MakeReleaseEvent("Q");
			context.BasicInputProcessing();
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));

			source.MakePressEvent("Shift+Ctrl"); // "Shift+Ctrl" not "Ctrl+Shift"
			source.MakePressEvent("Ctrl+Shift+Q");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("B"));
		}

		[TestMethod]
		public void BasicHotkeyInput()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ClientControls.BindMulti(_hotkeys[0], "Q");

			source.MakePressEvent("Q");
			context.BasicInputProcessing();

			Assert.AreEqual(1, context.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], context.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void HotkeyInputWithOneModifier()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ClientControls.BindMulti(_hotkeys[0], "Shift+Q");

			source.MakePressEvent("Shift+Q");
			context.BasicInputProcessing();

			Assert.AreEqual(1, context.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], context.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void HotkeyInputWithMultipleModifiers()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ClientControls.BindMulti(_hotkeys[0], "Ctrl+Shift+Q");

			source.MakePressEvent("Ctrl+Shift+Q");
			context.BasicInputProcessing();

			Assert.AreEqual(1, context.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], context.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void HotkeyInputDoesNotTriggerWithOutOfOrderModifier()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ClientControls.BindMulti(_hotkeys[0], "Shift+Q");

			source.MakePressEvent("Q");
			context.BasicInputProcessing();
			Assert.AreEqual(0, context.triggeredHotkeys.Count);

			source.MakePressEvent("Shift");
			context.BasicInputProcessing();
			Assert.AreEqual(0, context.triggeredHotkeys.Count);
		}

		[TestMethod]
		public void HotkeyInputDoesNotTriggerWithExtraModifier()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ClientControls.BindMulti(_hotkeys[0], "Q");

			source.MakePressEvent("Shift");
			source.MakePressEvent("Shift+Q");
			context.BasicInputProcessing();

			Assert.AreEqual(0, context.triggeredHotkeys.Count);
		}

		[TestMethod]
		public void SinglePressCanDoControllerAndHotkeyInput()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			context.config.InputHotkeyOverrideOptions = Config.InputPriority.BOTH;

			manager.ClientControls.BindMulti(_hotkeys[0], "Z");
			manager.ActiveController.BindMulti("A", "Z");

			source.MakePressEvent("Z");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(1, context.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], context.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void HotkeyPriority()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			context.config.InputHotkeyOverrideOptions = Config.InputPriority.HOTKEY;

			manager.ClientControls.BindMulti(_hotkeys[0], "Z");
			manager.ActiveController.BindMulti("A", "Z");

			source.MakePressEvent("Z");
			context.BasicInputProcessing();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(1, context.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], context.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void ControllerPriority()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			context.config.InputHotkeyOverrideOptions = Config.InputPriority.INPUT;

			manager.ClientControls.BindMulti(_hotkeys[0], "Z");
			manager.ActiveController.BindMulti("A", "Z");

			source.MakePressEvent("Z");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(0, context.triggeredHotkeys.Count);
		}

		[TestMethod]
		public void HotkeyPriorityWithModifier()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			context.config.InputHotkeyOverrideOptions = Config.InputPriority.HOTKEY;

			manager.ClientControls.BindMulti(_hotkeys[0], "Shift+Z");
			manager.ActiveController.BindMulti("A", "Shift+Z");

			source.MakePressEvent("Shift");
			source.MakePressEvent("Shift+Z");
			context.BasicInputProcessing();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(1, context.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], context.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void ControllerPriorityWithModifier()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			context.config.InputHotkeyOverrideOptions = Config.InputPriority.INPUT;

			manager.ClientControls.BindMulti(_hotkeys[0], "Shift+Z");
			manager.ActiveController.BindMulti("A", "Shift+Z");

			source.MakePressEvent("Shift");
			source.MakePressEvent("Shift+Z");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(0, context.triggeredHotkeys.Count);
		}

		[TestMethod]
		public void HotkeyOverrideDoesNotEatReleaseEvents()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			context.config.InputHotkeyOverrideOptions = Config.InputPriority.HOTKEY;

			manager.ClientControls.BindMulti(_hotkeys[0], "Z");
			manager.ActiveController.BindMulti("A", "Shift+Z");

			source.MakePressEvent("Shift");
			source.MakePressEvent("Shift+Z");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(0, context.triggeredHotkeys.Count);

			source.MakeReleaseEvent("Z");
			context.BasicInputProcessing();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void AutofireController()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.AutoFireController.BindMulti("A", "Q");

			source.MakePressEvent("Q");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			context.EmulateFrameAdvance();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void AutofireHotkey()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");
			manager.ClientControls.BindMulti("Autofire", "W");

			source.MakePressEvent("W");
			source.MakePressEvent("Q");
			context.BasicInputProcessing();
			source.MakeReleaseEvent("Q");
			source.MakeReleaseEvent("W");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			context.EmulateFrameAdvance();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void AutoholdHotkey()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");
			manager.ClientControls.BindMulti("Autohold", "W");

			source.MakePressEvent("W");
			source.MakePressEvent("Q");
			context.BasicInputProcessing();
			source.MakeReleaseEvent("Q");
			source.MakeReleaseEvent("W");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			context.EmulateFrameAdvance();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void AutofireHotkeyDoesNotRespondToAlreadyHeldButton()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");
			manager.ClientControls.BindMulti("Autofire", "W");

			source.MakePressEvent("Q");
			context.BasicInputProcessing();
			source.MakePressEvent("W");
			context.BasicInputProcessing();
			source.MakeReleaseEvent("Q");
			source.MakeReleaseEvent("W");
			context.BasicInputProcessing();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void HotkeyIsNotSeenAsUnhandled()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ClientControls.BindMulti(_hotkeys[0], "Q");

			source.MakePressEvent("Q");
			manager.ProcessInput(source, context.ProcessHotkey, context.config, (_, handled) => Assert.IsTrue(handled, "Bound key was seen as unbound."));
		}

		[TestMethod]
		public void InputIsNotSeenAsUnhandled()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");

			source.MakePressEvent("Q");
			manager.ProcessInput(source, context.ProcessHotkey, context.config, (_, handled) => Assert.IsTrue(handled, "Bound key was seen as unbound."));
		}

		[TestMethod]
		public void UnboundInputIsSeenAsUnhandled()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;

			source.MakePressEvent("A");

			manager.ProcessInput(source, context.ProcessHotkey, context.config, (_, handled) => Assert.IsFalse(handled, "Unbound key was seen as handled."));
		}

		[TestMethod]
		public void ControllerInputsWithPlusAreNotSplit()
		{
			Context context = new(_hotkeys);
			InputManager manager = context.manager;
			FakeInputSource source = context.source;

			manager.ActiveController.BindMulti("Right", "J1 X+");
			manager.ActiveController.BindMulti("Down", "J1 Y+");

			source.MakePressEvent("J1 X+");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("Right"));

			source.MakePressEvent("J1 Y+");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("Down"));

			source.MakeReleaseEvent("J1 X+");
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("Down"));
		}

		[TestMethod]
		public void ClickyController()
		{
			// arrange
			Context context = new();
			InputManager manager = context.manager;
			manager.ClickyController.Click("A");

			// act + assert
			context.BasicInputProcessing();
			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			context.EmulateFrameAdvance();
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void StickyHoldControllerButton()
		{
			// arrange
			Context context = new();
			InputManager manager = context.manager;
			manager.StickyHoldController.SetButtonHold("A", true);

			// act + assert
			context.BasicInputProcessing();
			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			context.EmulateFrameAdvance();
			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void StickyButtonPlusUserEqualsOff()
		{
			// arrange
			Context context = new();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");
			source.MakePressEvent("Q");
			manager.StickyHoldController.SetButtonHold("A", true);

			// act
			context.BasicInputProcessing();

			// assert
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void StickyHoldControllerAxis()
		{
			// arrange
			Context context = new();
			InputManager manager = context.manager;
			manager.StickyHoldController.SetAxisHold("Stick", 2);

			// act + assert
			context.BasicInputProcessing();
			Assert.AreEqual(2, manager.ControllerOutput.AxisValue("Stick"));
			context.EmulateFrameAdvance();
			Assert.AreEqual(2, manager.ControllerOutput.AxisValue("Stick"));
		}

		[TestMethod]
		public void StickyAutofireControllerButton()
		{
			// arrange
			Context context = new();
			InputManager manager = context.manager;
			manager.StickyAutofireController.SetButtonAutofire("A", true);

			// act + assert
			context.BasicInputProcessing();
			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			context.EmulateFrameAdvance();
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void StickyAutofireControllerAxis()
		{
			// arrange
			Context context = new();
			InputManager manager = context.manager;
			manager.StickyAutofireController.SetAxisAutofire("Stick", 2);

			// act + assert
			context.BasicInputProcessing();
			Assert.AreEqual(2, manager.ControllerOutput.AxisValue("Stick"));
			context.EmulateFrameAdvance();
			Assert.AreEqual(context.emulator.ControllerDefinition.Axes["Stick"].Neutral, manager.ControllerOutput.AxisValue("Stick"));
		}

		[TestMethod]
		public void OverrideAdapterButton()
		{
			// arrange
			Context context = new();
			InputManager manager = context.manager;
			manager.OverrideAdapter.SetButton("A", true);

			// act
			context.BasicInputProcessing();

			// assert
			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void OverrideAdapterAxis()
		{
			// arrange
			Context context = new();
			InputManager manager = context.manager;
			manager.OverrideAdapter.SetAxis("Stick", 2);

			// act
			context.BasicInputProcessing();

			// assert
			Assert.AreEqual(2, manager.ControllerOutput.AxisValue("Stick"));
		}

		[TestMethod]
		public void RegularAxisInput()
		{
			// arrange
			Context context = new();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindAxis("Stick", new AnalogBind("", 1f, 0f, "Q", ""));
			source.MakePressEvent("Q");

			// act
			context.BasicInputProcessing();

			// assert
			Assert.AreEqual(100, manager.ControllerOutput.AxisValue("Stick"));
		}

		[TestMethod]
		public void OverrideAdapterAxisOverridesNonNeutral()
		{
			// arrange
			Context context = new();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.OverrideAdapter.SetAxis("Stick", 2);
			manager.ActiveController.BindAxis("Stick", new AnalogBind("", 1f, 0f, "Q", ""));
			source.MakePressEvent("Q");

			// act
			context.BasicInputProcessing();

			// assert
			Assert.AreEqual(2, manager.ControllerOutput.AxisValue("Stick"));
		}
#pragma warning restore BHI1600
	}
}
