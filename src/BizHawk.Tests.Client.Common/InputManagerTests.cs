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
		private class Context
		{
			public required InputManager manager;
			public required Config config;
			public required List<string> triggeredHotkeys = new();
			public required Func<string, bool> processHotkey;
			public required IEmulator emulator;
			public required FakeInputSource source = new();

			public void EmulateFrameAdvance(bool lag = false)
			{
				emulator.FrameAdvance(manager.ControllerOutput, false);
				BasicInputProcessing();

				if (lag) manager.AutoFireController.IncrementStarts();
				manager.StickyAutofireController.IncrementLoops(lag);
			}

			public void BasicInputProcessing()
			{
				manager.ProcessInput(source, processHotkey, config, (_) => { });
				manager.RunControllerChain(config);
			}
		}

		private static readonly string[] _hotkeys = [ "Hotkey 1", "Autofire", "Autohold" ];

		private static readonly IReadOnlyList<string> _modifierKeys = new[] { "Super", "Ctrl", "Alt", "Shift" };

		private static readonly int PRIORITY_BOTH = 0;
		private static readonly int PRIORITY_INPUT = 1;
		private static readonly int PRIORITY_HOTKEY = 2;

		private InputEvent MakePressEvent(string keyboardButton, uint modifiers = 0)
		{
			return new()
			{
				EventType = InputEventType.Press,
				LogicalButton = new(keyboardButton, modifiers, () => _modifierKeys),
				Source = Bizware.Input.HostInputType.Keyboard,
			};
		}

		private InputEvent MakeReleaseEvent(string keyboardButton, uint modifiers = 0)
		{
			return new()
			{
				EventType = InputEventType.Release,
				LogicalButton = new(keyboardButton, modifiers, () => _modifierKeys),
				Source = Bizware.Input.HostInputType.Keyboard,
			};
		}

		private Context MakeContext()
		{
			InputManager manager = new();
			FakeEmulator emu = new();
			Config config = new();
			manager.SyncControls(emu, new FakeMovieSession(emu), config);

			ControllerDefinition cd = new("fake")
			{
				BoolButtons = _hotkeys.ToList(),
			};
			manager.ClientControls = new Controller(cd.MakeImmutable());

			List<string> triggeredHotkeys = new();
			bool FakeProcessHotkey(string trigger)
			{
				triggeredHotkeys.Add(trigger);
				return _hotkeys.Contains(trigger);
			}
			Context context = new()
			{
				config = config,
				manager = manager,
				triggeredHotkeys = triggeredHotkeys,
				processHotkey = FakeProcessHotkey,
				emulator = emu,
				source = new FakeInputSource(),
			};

			return context;
		}

		[TestMethod]
		public void BasicControllerInput()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");

			source.AddInputEvent(MakePressEvent("Q"));
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.IsFalse(manager.ControllerOutput.IsPressed("B"));
			Assert.IsFalse(manager.ControllerOutput.IsPressed("C"));

			source.AddInputEvent(MakeReleaseEvent("Q"));
			context.BasicInputProcessing();
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void ControllerInputWithOneModifier()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Shift+Q");

			source.AddInputEvent(MakePressEvent("Shift"));
			source.AddInputEvent(MakePressEvent("Shift+Q"));

			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			source.AddInputEvent(MakeReleaseEvent("Shift"));
			context.BasicInputProcessing();
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void ControllerInputWithMultipleModifiers()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Ctrl+Shift+Q");

			source.AddInputEvent(MakePressEvent("Ctrl"));
			source.AddInputEvent(MakePressEvent("Ctrl+Shift"));
			source.AddInputEvent(MakePressEvent("Ctrl+Shift+Q"));

			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			source.AddInputEvent(MakeReleaseEvent("Shift"));
			context.BasicInputProcessing();
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void ControllerInputAcceptsOutOfOrderModifier()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Shift+Q");

			source.AddInputEvent(MakePressEvent("Q"));
			context.BasicInputProcessing();
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));

			source.AddInputEvent(MakePressEvent("Shift"));
			context.BasicInputProcessing();
			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void ControllerInputIgnoresExtraModifier()
		{
			// Extra modifiers are ignored so that we can do inputs while doing another input that's bound to a modifier.
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");
			manager.ActiveController.BindMulti("B", "Ctrl+Q");

			source.AddInputEvent(MakePressEvent("Shift"));
			source.AddInputEvent(MakePressEvent("Shift+Q"));
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			source.AddInputEvent(MakeReleaseEvent("Q"));
			context.BasicInputProcessing();
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));

			source.AddInputEvent(MakePressEvent("Shift+Ctrl")); // "Shift+Ctrl" not "Ctrl+Shift"
			source.AddInputEvent(MakePressEvent("Ctrl+Shift+Q"));
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("B"));
		}

		[TestMethod]
		public void BasicHotkeyInput()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ClientControls.BindMulti(_hotkeys[0], "Q");

			source.AddInputEvent(MakePressEvent("Q"));
			context.BasicInputProcessing();

			Assert.AreEqual(1, context.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], context.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void HotkeyInputWithOneModifier()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ClientControls.BindMulti(_hotkeys[0], "Shift+Q");

			source.AddInputEvent(MakePressEvent("Shift+Q"));
			context.BasicInputProcessing();

			Assert.AreEqual(1, context.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], context.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void HotkeyInputWithMultipleModifiers()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ClientControls.BindMulti(_hotkeys[0], "Ctrl+Shift+Q");

			source.AddInputEvent(MakePressEvent("Ctrl+Shift+Q"));
			context.BasicInputProcessing();

			Assert.AreEqual(1, context.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], context.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void HotkeyInputDoesNotTriggerWithOutOfOrderModifier()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ClientControls.BindMulti(_hotkeys[0], "Shift+Q");

			source.AddInputEvent(MakePressEvent("Q"));
			context.BasicInputProcessing();
			Assert.AreEqual(0, context.triggeredHotkeys.Count);

			source.AddInputEvent(MakePressEvent("Shift"));
			context.BasicInputProcessing();
			Assert.AreEqual(0, context.triggeredHotkeys.Count);
		}

		[TestMethod]
		public void HotkeyInputDoesNotTriggerWithExtraModifier()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ClientControls.BindMulti(_hotkeys[0], "Q");

			source.AddInputEvent(MakePressEvent("Shift"));
			source.AddInputEvent(MakePressEvent("Shift+Q"));
			context.BasicInputProcessing();

			Assert.AreEqual(0, context.triggeredHotkeys.Count);
		}

		[TestMethod]
		public void SinglePressCanDoControllerAndHotkeyInput()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			context.config.InputHotkeyOverrideOptions = PRIORITY_BOTH;

			manager.ClientControls.BindMulti(_hotkeys[0], "Z");
			manager.ActiveController.BindMulti("A", "Z");

			source.AddInputEvent(MakePressEvent("Z"));
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(1, context.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], context.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void HotkeyPriority()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			context.config.InputHotkeyOverrideOptions = PRIORITY_HOTKEY;

			manager.ClientControls.BindMulti(_hotkeys[0], "Z");
			manager.ActiveController.BindMulti("A", "Z");

			source.AddInputEvent(MakePressEvent("Z"));
			context.BasicInputProcessing();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(1, context.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], context.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void ControllerPriority()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			context.config.InputHotkeyOverrideOptions = PRIORITY_INPUT;

			manager.ClientControls.BindMulti(_hotkeys[0], "Z");
			manager.ActiveController.BindMulti("A", "Z");

			source.AddInputEvent(MakePressEvent("Z"));
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(0, context.triggeredHotkeys.Count);
		}

		[TestMethod]
		public void HotkeyPriorityWithModifier()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			context.config.InputHotkeyOverrideOptions = PRIORITY_HOTKEY;

			manager.ClientControls.BindMulti(_hotkeys[0], "Shift+Z");
			manager.ActiveController.BindMulti("A", "Shift+Z");

			source.AddInputEvent(MakePressEvent("Shift"));
			source.AddInputEvent(MakePressEvent("Shift+Z"));
			context.BasicInputProcessing();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(1, context.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], context.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void ControllerPriorityWithModifier()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			context.config.InputHotkeyOverrideOptions = PRIORITY_INPUT;

			manager.ClientControls.BindMulti(_hotkeys[0], "Shift+Z");
			manager.ActiveController.BindMulti("A", "Shift+Z");

			source.AddInputEvent(MakePressEvent("Shift"));
			source.AddInputEvent(MakePressEvent("Shift+Z"));
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(0, context.triggeredHotkeys.Count);
		}

		[TestMethod]
		public void HotkeyOverrideDoesNotEatReleaseEvents()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			context.config.InputHotkeyOverrideOptions = PRIORITY_HOTKEY;

			manager.ClientControls.BindMulti(_hotkeys[0], "Z");
			manager.ActiveController.BindMulti("A", "Shift+Z");

			source.AddInputEvent(MakePressEvent("Shift"));
			source.AddInputEvent(MakePressEvent("Shift+Z"));
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(0, context.triggeredHotkeys.Count);

			source.AddInputEvent(MakeReleaseEvent("Z"));
			context.BasicInputProcessing();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void AutofireController()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.AutoFireController.BindMulti("A", "Q");

			source.AddInputEvent(MakePressEvent("Q"));
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			context.EmulateFrameAdvance();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void AutofireHotkey()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");
			manager.ClientControls.BindMulti("Autofire", "W");

			source.AddInputEvent(MakePressEvent("W"));
			source.AddInputEvent(MakePressEvent("Q"));
			context.BasicInputProcessing();
			source.AddInputEvent(MakeReleaseEvent("Q"));
			source.AddInputEvent(MakeReleaseEvent("W"));
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			context.EmulateFrameAdvance();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void AutoholdHotkey()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");
			manager.ClientControls.BindMulti("Autohold", "W");

			source.AddInputEvent(MakePressEvent("W"));
			source.AddInputEvent(MakePressEvent("Q"));
			context.BasicInputProcessing();
			source.AddInputEvent(MakeReleaseEvent("Q"));
			source.AddInputEvent(MakeReleaseEvent("W"));
			context.BasicInputProcessing();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			context.EmulateFrameAdvance();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void AutofireHotkeyDoesNotRespondToAlreadyHeldButton()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");
			manager.ClientControls.BindMulti("Autofire", "W");

			source.AddInputEvent(MakePressEvent("Q"));
			context.BasicInputProcessing();
			source.AddInputEvent(MakePressEvent("W"));
			context.BasicInputProcessing();
			source.AddInputEvent(MakeReleaseEvent("Q"));
			source.AddInputEvent(MakeReleaseEvent("W"));
			context.BasicInputProcessing();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void HotkeyIsNotSeenAsUnbound()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ClientControls.BindMulti(_hotkeys[0], "Q");

			source.AddInputEvent(MakePressEvent("Q"));
			manager.ProcessInput(source, context.processHotkey, context.config, (_) => Assert.Fail("Bound key was seen as unbound."));
		}

		[TestMethod]
		public void InputIsNotSeenAsUnbound()
		{
			Context context = MakeContext();
			InputManager manager = context.manager;
			FakeInputSource source = context.source;
			manager.ActiveController.BindMulti("A", "Q");

			source.AddInputEvent(MakePressEvent("Q"));
			manager.ProcessInput(source, context.processHotkey, context.config, (_) => Assert.Fail("Bound key was seen as unbound."));
		}
	}
}
