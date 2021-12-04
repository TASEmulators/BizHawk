#nullable enable

using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class InputCoalescer : SimpleController
	{
		public InputCoalescer()
			: base(NullController.Instance.Definition) {} // is Definition ever read on these subclasses? --yoshi

		protected virtual void ProcessSubsets(string button, bool state) {}

		public void Receive(InputEvent ie)
		{
			var state = ie.EventType is InputEventType.Press;
			var button = ie.LogicalButton.ToString();
			Buttons[button] = state;
			ProcessSubsets(button, state);
			if (state) return;
			// when a button is released, all modified variants of it are released as well
			foreach (var k in Buttons.Keys.Where(k => k.EndsWith($"+{ie.LogicalButton.Button}")).ToList()) Buttons[k] = false;
		}
	}

	public sealed class ControllerInputCoalescer : InputCoalescer
	{
		protected override void ProcessSubsets(string button, bool state)
		{
			// For controller input, we want Shift+X to register as both Shift and X (for Keyboard controllers)
			foreach (var s in button.Split('+')) Buttons[s] = state;
		}
	}
}
