using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Just copies source to sink, or returns whatever a NullController would if it is disconnected. useful for immovable hard-points.
	/// </summary>
	public class CopyControllerAdapter : IInputAdapter
	{
		public ControllerDefinition Definition => Curr.Definition;

		public bool IsPressed(string button) => Curr.IsPressed(button);

		public int AxisValue(string name) => Curr.AxisValue(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Curr.GetHapticsSnapshot();

		public void SetHapticChannelStrength(string name, int strength) => Curr.SetHapticChannelStrength(name, strength);

		public IController Source { get; set; }

		private IController Curr => Source ?? NullController.Instance;
	}
}
