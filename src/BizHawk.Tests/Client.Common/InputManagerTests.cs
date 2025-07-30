using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Tests.Client.Common.input;
using BizHawk.Tests.Client.Common.Movie;
using BizHawk.Tests.Emulation.Common;

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

			public void EmulateFrameAdvance(bool lag = false)
			{
				emulator.FrameAdvance(manager.ControllerOutput, false);
				manager.ProcessInput(new FakeInputSource(), processHotkey, config, (_) => { });

				if (lag) manager.AutoFireController.IncrementStarts();
				manager.StickyAutofireController.IncrementLoops(lag);
			}
		}

		private string[] _hotkeys = [ "Hotkey 1", "Autofire", "Autohold" ];

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
			};

			return context;
		}

		[TestMethod]
		public void BasicControllerInput()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ActiveController.BindMulti("A", "Q");

			source.AddInputEvent(MakePressEvent("Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.IsFalse(manager.ControllerOutput.IsPressed("B"));
			Assert.IsFalse(manager.ControllerOutput.IsPressed("C"));

			source.AddInputEvent(MakeReleaseEvent("Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void ControllerInputWithOneModifier()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ActiveController.BindMulti("A", "Shift+Q");

			source.AddInputEvent(MakePressEvent("Shift"));
			source.AddInputEvent(MakePressEvent("Shift+Q"));

			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			source.AddInputEvent(MakeReleaseEvent("Shift"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void ControllerInputWithMultipleModifiers()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ActiveController.BindMulti("A", "Ctrl+Shift+Q");

			source.AddInputEvent(MakePressEvent("Ctrl"));
			source.AddInputEvent(MakePressEvent("Ctrl+Shift"));
			source.AddInputEvent(MakePressEvent("Ctrl+Shift+Q"));

			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			source.AddInputEvent(MakeReleaseEvent("Shift"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void ControllerInputAcceptsOutOfOrderModifier()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ActiveController.BindMulti("A", "Shift+Q");

			source.AddInputEvent(MakePressEvent("Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));

			source.AddInputEvent(MakePressEvent("Shift"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });
			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void ControllerInputIgnoresExtraModifier()
		{
			// Extra modifiers are ignored so that we can do inputs while doing another input that's bound to a modifier.
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ActiveController.BindMulti("A", "Q");
			manager.ActiveController.BindMulti("B", "Ctrl+Q");

			source.AddInputEvent(MakePressEvent("Shift"));
			source.AddInputEvent(MakePressEvent("Shift+Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			source.AddInputEvent(MakeReleaseEvent("Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });
			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));

			source.AddInputEvent(MakePressEvent("Shift+Ctrl")); // "Shift+Ctrl" not "Ctrl+Shift"
			source.AddInputEvent(MakePressEvent("Ctrl+Shift+Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsTrue(manager.ControllerOutput.IsPressed("B"));
		}

		[TestMethod]
		public void BasicHotkeyInput()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ClientControls.BindMulti(_hotkeys[0], "Q");

			source.AddInputEvent(MakePressEvent("Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.AreEqual(1, c.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], c.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void HotkeyInputWithOneModifier()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ClientControls.BindMulti(_hotkeys[0], "Shift+Q");

			source.AddInputEvent(MakePressEvent("Shift+Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.AreEqual(1, c.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], c.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void HotkeyInputWithMultipleModifiers()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ClientControls.BindMulti(_hotkeys[0], "Ctrl+Shift+Q");

			source.AddInputEvent(MakePressEvent("Ctrl+Shift+Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.AreEqual(1, c.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], c.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void HotkeyInputDoesNotTriggerWithOutOfOrderModifier()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ClientControls.BindMulti(_hotkeys[0], "Shift+Q");

			source.AddInputEvent(MakePressEvent("Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });
			Assert.AreEqual(0, c.triggeredHotkeys.Count);

			source.AddInputEvent(MakePressEvent("Shift"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });
			Assert.AreEqual(0, c.triggeredHotkeys.Count);
		}

		[TestMethod]
		public void HotkeyInputDoesNotTriggerWithExtraModifier()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ClientControls.BindMulti(_hotkeys[0], "Q");

			source.AddInputEvent(MakePressEvent("Shift"));
			source.AddInputEvent(MakePressEvent("Shift+Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.AreEqual(0, c.triggeredHotkeys.Count);
		}

		[TestMethod]
		public void SinglePressCanDoControllerAndHotkeyInput()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			c.config.InputHotkeyOverrideOptions = PRIORITY_BOTH;

			manager.ClientControls.BindMulti(_hotkeys[0], "Z");
			manager.ActiveController.BindMulti("A", "Z");

			source.AddInputEvent(MakePressEvent("Z"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(1, c.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], c.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void HotkeyPriority()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			c.config.InputHotkeyOverrideOptions = PRIORITY_HOTKEY;

			manager.ClientControls.BindMulti(_hotkeys[0], "Z");
			manager.ActiveController.BindMulti("A", "Z");

			source.AddInputEvent(MakePressEvent("Z"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(1, c.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], c.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void ControllerPriority()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			c.config.InputHotkeyOverrideOptions = PRIORITY_INPUT;

			manager.ClientControls.BindMulti(_hotkeys[0], "Z");
			manager.ActiveController.BindMulti("A", "Z");

			source.AddInputEvent(MakePressEvent("Z"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(0, c.triggeredHotkeys.Count);
		}

		[TestMethod]
		public void HotkeyPriorityWithModifier()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			c.config.InputHotkeyOverrideOptions = PRIORITY_HOTKEY;

			manager.ClientControls.BindMulti(_hotkeys[0], "Shift+Z");
			manager.ActiveController.BindMulti("A", "Shift+Z");

			source.AddInputEvent(MakePressEvent("Shift"));
			source.AddInputEvent(MakePressEvent("Shift+Z"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(1, c.triggeredHotkeys.Count);
			Assert.AreEqual(_hotkeys[0], c.triggeredHotkeys[0]);
		}

		[TestMethod]
		public void ControllerPriorityWithModifier()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			c.config.InputHotkeyOverrideOptions = PRIORITY_INPUT;

			manager.ClientControls.BindMulti(_hotkeys[0], "Shift+Z");
			manager.ActiveController.BindMulti("A", "Shift+Z");

			source.AddInputEvent(MakePressEvent("Shift"));
			source.AddInputEvent(MakePressEvent("Shift+Z"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(0, c.triggeredHotkeys.Count);
		}

		[TestMethod]
		public void HotkeyOverrideDoesNotEatReleaseEvents()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			c.config.InputHotkeyOverrideOptions = PRIORITY_HOTKEY;

			manager.ClientControls.BindMulti(_hotkeys[0], "Z");
			manager.ActiveController.BindMulti("A", "Shift+Z");

			source.AddInputEvent(MakePressEvent("Shift"));
			source.AddInputEvent(MakePressEvent("Shift+Z"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
			Assert.AreEqual(0, c.triggeredHotkeys.Count);

			source.AddInputEvent(MakeReleaseEvent("Z"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void AutofireController()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.AutoFireController.BindMulti("A", "Q");

			source.AddInputEvent(MakePressEvent("Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			c.EmulateFrameAdvance();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void AutofireHotkey()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ActiveController.BindMulti("A", "Q");
			manager.ClientControls.BindMulti("Autofire", "W");

			source.AddInputEvent(MakePressEvent("W"));
			source.AddInputEvent(MakePressEvent("Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });
			source.AddInputEvent(MakeReleaseEvent("Q"));
			source.AddInputEvent(MakeReleaseEvent("W"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			c.EmulateFrameAdvance();

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void AutoholdHotkey()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ActiveController.BindMulti("A", "Q");
			manager.ClientControls.BindMulti("Autohold", "W");

			source.AddInputEvent(MakePressEvent("W"));
			source.AddInputEvent(MakePressEvent("Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });
			source.AddInputEvent(MakeReleaseEvent("Q"));
			source.AddInputEvent(MakeReleaseEvent("W"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));

			c.EmulateFrameAdvance();

			Assert.IsTrue(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void AutofireHotkeyDoesNotRespondToAlreadyHeldButton()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ActiveController.BindMulti("A", "Q");
			manager.ClientControls.BindMulti("Autofire", "W");

			source.AddInputEvent(MakePressEvent("Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });
			source.AddInputEvent(MakePressEvent("W"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });
			source.AddInputEvent(MakeReleaseEvent("Q"));
			source.AddInputEvent(MakeReleaseEvent("W"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => { });

			Assert.IsFalse(manager.ControllerOutput.IsPressed("A"));
		}

		[TestMethod]
		public void HotkeyIsNotSeenAsUnbound()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ClientControls.BindMulti(_hotkeys[0], "Q");

			source.AddInputEvent(MakePressEvent("Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => Assert.Fail("Bound key was seen as unbound."));
		}

		[TestMethod]
		public void InputIsNotSeenAsUnbound()
		{
			Context c = MakeContext();
			InputManager manager = c.manager;
			FakeInputSource source = new FakeInputSource();
			manager.ActiveController.BindMulti("A", "Q");

			source.AddInputEvent(MakePressEvent("Q"));
			manager.ProcessInput(source, c.processHotkey, c.config, (_) => Assert.Fail("Bound key was seen as unbound."));
		}
	}
}
