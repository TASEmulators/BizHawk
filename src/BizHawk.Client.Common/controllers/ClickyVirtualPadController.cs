using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Will hold buttons for 1 frame and then release them.
	/// (Calling Click() from your button click is what you want to do)
	/// TODO - should the duration be controllable?
	/// </summary>
	public class ClickyVirtualPadController : IController
	{
		private readonly HashSet<string> _pressed = new HashSet<string>();

		public ControllerDefinition Definition { get; set; }

		public bool IsPressed(string button) => _pressed.Contains(button);

		public int AxisValue(string name) => 0;

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Array.Empty<(string, int)>();

		public void SetHapticChannelStrength(string name, int strength) {}

		/// <summary>
		/// Call this once per frame to do the timekeeping for the hold and release
		/// </summary>
		public void FrameTick() => _pressed.Clear();

		/// <summary>
		/// Call this to hold the button down for one frame
		/// </summary>
		public void Click(string button) => _pressed.Add(button);

		public void Toggle(string button)
		{
			if (IsPressed(button))
			{
				_pressed.Remove(button);
			}
			else
			{
				_pressed.Add(button);
			}
		}

		public void SetBool(string button, bool value)
		{
			if (value)
			{
				_pressed.Remove(button);
			}
			else
			{
				_pressed.Add(button);
			}
		}
	}
}
