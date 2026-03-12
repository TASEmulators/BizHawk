using System.Collections.Generic;
using System.Linq;
using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.Api
{
	// Please note that these tests create press events as seen by the input manager.
	// Sending incorrect press events can make a test incorrect.
	// Example of correct events:
	// context.MakePressEvent("Shift");
	// context.MakePressEvent("Shift+A");
	// context.MakeRleaseEvent("A");
	// context.MakeRleaseEvent("Shift");
	//
	// There is no press event for just "A", and there are never release events for combinations.
	[TestClass]
	public class InputApiTests
	{
		private class Context
		{
			public InputManagerTests.Context inputContext;
			public InputApi api;

			public Context(string[]? hotkeys = null)
			{
				inputContext = new(hotkeys);
				// null DisplayManagerBase: We don't have one and it isn't used for anything other than GetMouse, which we aren't testing.
				api = new(null, inputContext.manager);
			}

			public void MakePressEvent(string keyboardButton, uint modifiers = 0)
			{
				inputContext.source.MakePressEvent(keyboardButton, modifiers);
				inputContext.BasicInputProcessing();
			}

			public void MakeReleaseEvent(string keyboardButton, uint modifiers = 0)
			{
				inputContext.source.MakeReleaseEvent(keyboardButton, modifiers);
				inputContext.BasicInputProcessing();
			}
		}

		[TestMethod]
		public void TestNoPressedButtons()
		{
			// arrange
			Context context = new();

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			Assert.AreEqual(0, buttons.Count);
		}

		[TestMethod]
		public void TestPressPlainButton()
		{
			// arrange
			Context context = new();
			context.MakePressEvent("A");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			Assert.IsTrue(buttons.Contains("A"));
			Assert.AreEqual(1, buttons.Count);
		}

		[TestMethod]
		public void TestReleasePlainButton()
		{
			// arrange
			Context context = new();
			context.MakePressEvent("A");
			context.MakeReleaseEvent("A");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			Assert.IsFalse(buttons.Contains("A"));
			Assert.AreEqual(0, buttons.Count);
		}

		[TestMethod]
		public void TestMultipleButtons()
		{
			// arrange
			Context context = new();
			context.MakePressEvent("A");
			context.MakePressEvent("B");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			Assert.IsTrue(buttons.Contains("A"));
			Assert.IsTrue(buttons.Contains("B"));
			Assert.AreEqual(2, buttons.Count);
		}

		[TestMethod]
		public void TestModifierAlone()
		{
			// arrange
			Context context = new();
			context.MakePressEvent("Shift");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			Assert.IsTrue(buttons.Contains("Shift"));
			Assert.AreEqual(1, buttons.Count);
		}

		[TestMethod]
		public void TestModifierCombinationShowsBothIndividualButtons()
		{
			// arrange
			Context context = new();
			context.MakePressEvent("Shift");
			context.MakePressEvent("Shift+A");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			Assert.IsTrue(buttons.Contains("Shift"));
			Assert.IsTrue(buttons.Contains("A"));
		}

		[TestMethod]
		public void TestModifierCombinationAsSingleButton()
		{
			// arrange
			Context context = new();
			context.MakePressEvent("Shift");
			context.MakePressEvent("Shift+A");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			Assert.IsTrue(buttons.Contains("Shift+A"));
		}

		[TestMethod]
		public void TestReleaseRegularButtonReleasesCombination()
		{
			// arrange
			Context context = new();
			context.MakePressEvent("Shift");
			context.MakePressEvent("Shift+A");
			context.MakeReleaseEvent("A");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			Assert.IsFalse(buttons.Contains("Shift+A"));
			Assert.AreEqual(1, buttons.Count);
		}

		[TestMethod]
		public void TestOutOfOrderModifierCombinationIsNotCombined()
		{
			// arrange
			Context context = new();
			context.MakePressEvent("A");
			context.MakePressEvent("Shift");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			Assert.IsFalse(buttons.Contains("Shift+A"));
			Assert.AreEqual(2, buttons.Count);
		}

		[TestMethod]
		public void TestReleaseModifierButtonReleasesCombination()
		{
			// arrange
			Context context = new();
			context.MakePressEvent("Shift");
			context.MakePressEvent("Shift+A");
			context.MakeReleaseEvent("Shift");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			Assert.IsFalse(buttons.Contains("Shift+A"));
			Assert.AreEqual(1, buttons.Count);
		}

		[TestMethod]
		public void TestButtonIsVisibleWithControllerPriority()
		{
			// arrange
			Context context = new([ "hotkey 1" ]);
			context.inputContext.manager.ClientControls.BindMulti("hotkey 1", "A");
			context.inputContext.manager.ActiveController.BindMulti("A", "A");
			context.inputContext.config.InputHotkeyOverrideOptions = Config.InputPriority.INPUT;
			context.MakePressEvent("A");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			Assert.IsTrue(buttons.Contains("A"));
		}

		[TestMethod]
		public void TestButtonIsVisibleWithHotkeyPriority()
		{
			// arrange
			Context context = new([ "hotkey 1" ]);
			context.inputContext.manager.ClientControls.BindMulti("hotkey 1", "A");
			context.inputContext.manager.ActiveController.BindMulti("A", "A");
			context.inputContext.config.InputHotkeyOverrideOptions = Config.InputPriority.HOTKEY;
			context.MakePressEvent("A");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			Assert.IsTrue(buttons.Contains("A"));
		}

		[TestMethod]
		public void TestMultipleModifiersBeforeKey()
		{
			// arrange
			Context context = new();
			context.MakePressEvent("Shift");
			context.MakePressEvent("Shift+Ctrl");
			context.MakePressEvent("Ctrl+Shift+A");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			CollectionAssert.AreEquivalent(
				new[] { "Shift", "Ctrl", "Shift+Ctrl", "A", "Ctrl+Shift+A" },
				buttons.ToArray());
		}

		[TestMethod]
		public void TestMultipleModifiersAfterKey()
		{
			// arrange
			Context context = new();
			context.MakePressEvent("A");
			context.MakePressEvent("Shift");
			context.MakePressEvent("Shift+Ctrl");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			CollectionAssert.AreEquivalent(
				new[] { "A", "Shift", "Ctrl", "Shift+Ctrl" },
				buttons.ToArray());
		}

		[TestMethod]
		public void TestModifierAfterCombination()
		{
			// arrange
			Context context = new();
			context.MakePressEvent("Shift");
			context.MakePressEvent("Shift+A");
			context.MakePressEvent("Shift+Ctrl");

			// act
			IReadOnlyList<string> buttons = context.api.GetPressedButtons();

			// assert
			CollectionAssert.AreEquivalent(
				new[] { "A", "Shift", "Ctrl", "Shift+Ctrl", "Shift+A" },
				buttons.ToArray());
		}
	}
}
