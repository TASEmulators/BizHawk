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

		protected WorkingDictionary<string, bool> Buttons = new WorkingDictionary<string, bool>();
		protected WorkingDictionary<string, float> Floats = new WorkingDictionary<string, float>();

		public virtual void Clear()
		{
			Buttons = new WorkingDictionary<string, bool>();
			Floats = new WorkingDictionary<string, float>();
		}

		public virtual bool this[string button]
		{
			get { return Buttons[button]; }
			set { Buttons[button] = value; }
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

		public virtual void LatchFrom(IController source)
		{
			foreach (var button in source.Definition.BoolButtons)
			{
				Buttons[button] = source.IsPressed(button);
			}
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
