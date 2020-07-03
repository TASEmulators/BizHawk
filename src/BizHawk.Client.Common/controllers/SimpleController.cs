using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// A basic implementation of IController
	/// </summary>
	public class SimpleController : IController
	{
		public ControllerDefinition Definition { get; set; }

		protected WorkingDictionary<string, bool> Buttons { get; private set; } = new WorkingDictionary<string, bool>();
		protected WorkingDictionary<string, int> Axes { get; private set; } = new WorkingDictionary<string, int>();

		public void Clear()
		{
			Buttons = new WorkingDictionary<string, bool>();
			Axes = new WorkingDictionary<string, int>();
		}

		public bool this[string button]
		{
			get => Buttons[button];
			set => Buttons[button] = value;
		}

		public virtual bool IsPressed(string button) => this[button];

		public int AxisValue(string name) => Axes[name];

		public IDictionary<string, bool> BoolButtons() => Buttons;

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
