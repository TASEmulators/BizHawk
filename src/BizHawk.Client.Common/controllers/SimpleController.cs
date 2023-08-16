using System.Collections.Generic;
using System.Linq;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// A basic implementation of IController
	/// </summary>
	public class SimpleController : IController
	{
		public ControllerDefinition Definition { get; }

		protected Dictionary<string, int> Axes { get; private set; } = new();

		protected Dictionary<string, bool> Buttons { get; private set; } = new();

		protected Dictionary<string, int> HapticFeedback { get; private set; } = new();

		public SimpleController(ControllerDefinition definition)
			=> Definition = definition;

		public void Clear()
		{
			Buttons = new();
			Axes = new();
			HapticFeedback = new();
		}

		public bool this[string button]
		{
			get => Buttons.GetValueOrDefault(button);
			set => Buttons[button] = value;
		}

		public virtual bool IsPressed(string button)
			=> Buttons.GetValueOrDefault(button);

		public int AxisValue(string name)
			=> Axes.GetValueOrDefault(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot()
			=> HapticFeedback.Select(kvp => (kvp.Key, kvp.Value)).ToArray();

		public void SetHapticChannelStrength(string name, int strength) => HapticFeedback[name] = strength;

		public IReadOnlyDictionary<string, int> AxisValues() => Axes;

		public IReadOnlyDictionary<string, bool> BoolButtons() => Buttons;

		public void AcceptNewAxis(string axisId, int value)
		{
			Axes[axisId] = value;
		}

		public void AcceptNewAxes(IEnumerable<(string AxisID, int Value)> newValues)
		{
			foreach (var (axisID, value) in newValues)
			{
				Axes[axisID] = value;
			}
		}
	}
}
