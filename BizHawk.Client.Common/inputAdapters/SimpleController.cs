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
		protected WorkingDictionary<string, float> Floats { get; private set; } = new WorkingDictionary<string, float>();

		public void Clear()
		{
			Buttons = new WorkingDictionary<string, bool>();
			Floats = new WorkingDictionary<string, float>();
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

		public float GetFloat(string name)
		{
			return Floats[name];
		}

		public IEnumerable<KeyValuePair<string, bool>> BoolButtons()
		{
			return Buttons;
		}

		public void AcceptNewFloats(IEnumerable<Tuple<string, float>> newValues)
		{
			foreach (var sv in newValues)
			{
				Floats[sv.Item1] = sv.Item2;
			}
		}
	}
}
