using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Used to pass into an Override method to manage the logic overriding input
	/// This only works with bool buttons!
	/// </summary>
	public class OverrideAdaptor : IController
	{
		public ControllerDefinition Definition { get; private set; }

		private readonly Dictionary<string, bool> _overrides = new Dictionary<string, bool>();
		private readonly Dictionary<string, float> _floatOverrides = new Dictionary<string, float>();
		private readonly List<string> _inverses = new List<string>();

		public bool IsPressed(string button)
		{
			if (_overrides.ContainsKey(button))
			{
				return _overrides[button];
			}

			throw new InvalidOperationException();
		}

		public float GetFloat(string name)
		{
			if (_floatOverrides.ContainsKey(name))
			{
				return _floatOverrides[name];
			}

			return 0.0F;
		}

		public IEnumerable<string> Overrides
		{
			get
			{
				foreach (var kvp in _overrides)
				{
					yield return kvp.Key;
				}
			}
		}

		public IEnumerable<string> FloatOverrides
		{
			get
			{
				foreach (var kvp in _floatOverrides)
				{
					yield return kvp.Key;
				}
			}
		}

		public IEnumerable<string> InversedButtons
		{
			get
			{
				foreach (var name in _inverses)
				{
					yield return name;
				}
			}
		}

		public void SetFloat(string name, float value)
		{
			if (_floatOverrides.ContainsKey(name))
			{
				_floatOverrides[name] = value;
			}
			else
			{
				_floatOverrides.Add(name, value);
			}
		}

		public void SetButton(string button, bool value)
		{
			if (_overrides.ContainsKey(button))
			{
				_overrides[button] = value;
			}
			else
			{
				_overrides.Add(button, value);
			}

			_inverses.Remove(button);
		}

		public void UnSet(string button)
		{
			_overrides.Remove(button);
			_inverses.Remove(button);
		}

		public void SetInverse(string button)
		{
			_inverses.Add(button);
		}

		public void FrameTick()
		{
			_overrides.Clear();
			_floatOverrides.Clear();
			_inverses.Clear();
		}
	}
}
