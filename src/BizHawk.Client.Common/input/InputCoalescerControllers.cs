#nullable enable

using System.Linq;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class InputCoalescer : SimpleController
	{
		public InputCoalescer()
			: base(NullController.Instance.Definition) {} // is Definition ever read on these subclasses? --yoshi

		protected virtual void ProcessInput(string button, bool state)
		{
			Buttons[button] = state;
		}

		public void Receive(InputEvent ie)
		{
			var state = ie.EventType is InputEventType.Press;
			var button = ie.LogicalButton.ToString();
			ProcessInput(button, state);
			if (state) return;
			// when a button or modifier key is released, all modified key variants with it are released as well
			foreach (var k in Buttons.Keys.Where(k =>
						k.EndsWithOrdinal($"+{ie.LogicalButton.Button}") || k.StartsWithOrdinal($"{ie.LogicalButton.Button}+") || k.Contains($"+{ie.LogicalButton.Button}+"))
						.ToArray())
				Buttons[k] = false;
		}
	}

	public sealed class ControllerInputCoalescer : InputCoalescer
	{
		protected override void ProcessInput(string button, bool state)
		{
			// For controller input, we want Shift+X to register as both Shift and X (for Keyboard controllers)
			foreach (var s in button.Split('+')) Buttons[s] = state;
		}

		public override bool IsPressed(string button)
		{
			// Since we split all inputs into their separate physical buttons, we need to check combinations here.
			string[] buttons = button.Split('+');
			return buttons.All(Buttons.GetValueOrDefault);
		}
	}

	public sealed class ApiInputCoalescer : InputCoalescer
	{
		protected override void ProcessInput(string button, bool state)
		{
			// For controller input, we want Shift+X to register as both Shift and X
			foreach (var s in button.Split('+')) Buttons[s] = state;
			// AND as the combination
			base.ProcessInput(button, state);
		}
	}
}
