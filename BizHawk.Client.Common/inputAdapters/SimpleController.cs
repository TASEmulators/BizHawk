using System;
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
		protected WorkingDictionary<string, float> Axes { get; private set; } = new WorkingDictionary<string, float>();

		public void Clear()
		{
			Buttons = new WorkingDictionary<string, bool>();
			Axes = new WorkingDictionary<string, float>();
		}

		public bool this[string button]
		{
			get => Buttons[button];
			set => Buttons[button] = value;
		}

		public virtual bool IsPressed(string button)
		{
			return this[button];
		}

		public float AxisValue(string name)
		{
			return Axes[name];
		}

		public IEnumerable<KeyValuePair<string, bool>> BoolButtons()
		{
			return Buttons;
		}

		public void AcceptNewAxes(Tuple<string, float> newValue)
		{
			Axes[newValue.Item1] = newValue.Item2;
		}

		public void AcceptNewAxes(IEnumerable<Tuple<string, float>> newValues)
		{
			foreach (var sv in newValues)
			{
				Axes[sv.Item1] = sv.Item2;
			}
		}
	}
}
