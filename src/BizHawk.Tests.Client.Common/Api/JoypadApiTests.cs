using System.Collections.Generic;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Tests.Client.Common.Movie;

namespace BizHawk.Tests.Client.Common.Api
{
	[TestClass]
	public class JoypadApiTests
	{
		private static Dictionary<string, object> DefaultInputs;
		static JoypadApiTests()
		{
			FakeEmulator e = new();
			DefaultInputs = new();
			foreach (string b in e.ControllerDefinition.BoolButtons)
				DefaultInputs[b] = false;
			foreach (var kvp in e.ControllerDefinition.Axes)
				DefaultInputs[kvp.Key] = kvp.Value.Neutral;
		}

		private class Context
		{

			public InputManagerTests.Context inputContext;
			public JoypadApi api;

			public IController OutputController => inputContext.manager.ControllerOutput;

			public Context()
			{
				FakeEmulator emulator = new();
				FakeMovieSession movieSession = new(emulator);
				inputContext = new(emulator, movieSession);
				api = new(null, inputContext.manager, movieSession);

				inputContext.manager.ActiveController.BindMulti("A", "A");
				inputContext.manager.ActiveController.BindMulti("B", "B");
				inputContext.manager.ActiveController.BindAxis("Stick", new AnalogBind("", 1f, 0f, "S", ""));
			}

			public void Press(string button)
			{
				inputContext.source.MakePressEvent(button, 0);
				inputContext.BasicInputProcessing();
			}

			public void Release(string button)
			{
				inputContext.source.MakeReleaseEvent(button, 0);
				inputContext.BasicInputProcessing();
			}
		}

		private static void AssertDictMatches(IReadOnlyDictionary<string, object> expected, IReadOnlyDictionary<string, object> actual)
		{
			foreach (var kvp in expected)
			{
				Assert.IsTrue(actual.TryGetValue(kvp.Key, out object value), $"Expected to find key {kvp.Key} but did not.");
				Assert.AreEqual(kvp.Value, value, $"Value of {kvp.Key} did not match. Expected {kvp.Value}, got {value}.");
			}
			Assert.AreEqual(expected.Count, actual.Count);
		}

		[TestMethod]
		public void GetDefaultInputs()
		{
			// arrange
			Context context = new();

			// act
			var inputs = context.api.Get();

			// assert
			AssertDictMatches(DefaultInputs, inputs);
		}

		[TestMethod]
		public void SetButton()
		{
			// arrange
			Context context = new();

			// act
			context.api.Set(new Dictionary<string, bool>() { ["A"] = true });

			// assert
			Assert.IsTrue(context.OutputController.IsPressed("A"));
		}

		[TestMethod]
		public void GetPressedButton()
		{
			// arrange
			Context context = new();
			context.Press("A");
			var inputsWithA = DefaultInputs.ToDictionary();
			inputsWithA["A"] = true;

			// act
			var inputs = context.api.Get();

			// assert
			AssertDictMatches(inputsWithA, inputs);
		}

		[TestMethod]
		public void RoundTripButton()
		{
			// arrange
			Context context = new();
			var inputsWithA = DefaultInputs.ToDictionary();
			inputsWithA["A"] = true;

			// act
			context.api.Set(new Dictionary<string, bool>() { ["A"] = true });
			var inputs = context.api.Get();

			// assert
			AssertDictMatches(inputsWithA, inputs);
		}

		[TestMethod]
		public void SetAnalog()
		{
			// arrange
			Context context = new();

			// act
			context.api.SetAnalog(new Dictionary<string, int>() { ["Stick"] = 2 });

			// assert
			Assert.AreEqual(2, context.OutputController.AxisValue("Stick"));
		}

		[TestMethod]
		public void GetAnalog()
		{
			// arrange
			Context context = new();
			context.Press("S");
			var inputsWithStick = DefaultInputs.ToDictionary();
			inputsWithStick["Stick"] = FakeEmulator.Definition.Axes["Stick"].Max;

			// act
			var inputs = context.api.Get();

			// assert
			AssertDictMatches(inputsWithStick, inputs);
		}

		[TestMethod]
		public void RoundTripAnalog()
		{
			// arrange
			Context context = new();
			var inputsWithStick = DefaultInputs.ToDictionary();
			inputsWithStick["Stick"] = 2;

			// act
			context.api.SetAnalog(new Dictionary<string, int>() { ["Stick"] = 2});
			var inputs = context.api.Get();

			// assert
			AssertDictMatches(inputsWithStick, inputs);
		}

		[TestMethod]
		public void OverridesButton()
		{
			// arrange
			Context context = new();
			context.Press("A");

			// act
			context.api.Set(new Dictionary<string, bool>() { ["A"] = false });

			// assert
			Assert.IsFalse(context.OutputController.IsPressed("A"));
		}

		[TestMethod]
		public void OverridesAnalog()
		{
			// arrange
			Context context = new();
			context.Press("S");

			// act
			context.api.SetAnalog(new Dictionary<string, int>() { ["Stick"] = 2});

			// assert
			Assert.AreEqual(2, context.OutputController.AxisValue("Stick"));
		}

		[TestMethod]
		public void UnsetButton()
		{
			// arrange
			Context context = new();
			context.api.Set(new Dictionary<string, bool>() { ["A"] = true });

			// act
			context.api.Set(new Dictionary<string, bool>());

			// assert
			Assert.IsFalse(context.OutputController.IsPressed("A"));
		}

		[TestMethod]
		public void UnsetAnalog()
		{
			// arrange
			Context context = new();
			context.api.SetAnalog(new Dictionary<string, int>() { ["Stick"] = 2 });

			// act
			context.api.SetAnalog(new Dictionary<string, int>());

			// assert
			Assert.AreEqual(FakeEmulator.Definition.Axes["Stick"].Neutral, context.OutputController.AxisValue("Stick"));
		}
	}
}
