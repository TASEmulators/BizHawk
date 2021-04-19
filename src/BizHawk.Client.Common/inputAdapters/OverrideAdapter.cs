using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Used to pass into an Override method to manage the logic overriding input
	/// This only works with bool buttons!
	/// </summary>
	public class OverrideAdapter : IController
	{
		public ControllerDefinition Definition { get; private set; }

		private readonly Dictionary<string, bool> _overrides = new Dictionary<string, bool>();
		private readonly Dictionary<string, int> _axisOverrides = new Dictionary<string, int>();
		private readonly List<string> _inverses = new List<string>();

		/// <exception cref="InvalidOperationException"><paramref name="button"/> not overridden</exception>
		public bool IsPressed(string button)
		{
			if (_overrides.ContainsKey(button))
			{
				return _overrides[button];
			}

			throw new InvalidOperationException();
		}

		public int AxisValue(string name)
			=> _axisOverrides.ContainsKey(name)
				? _axisOverrides[name]
				: 0;

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => throw new NotImplementedException(); // no idea --yoshi

		public void SetHapticChannelStrength(string name, int strength) => throw new NotImplementedException(); // no idea --yoshi

		public IEnumerable<string> Overrides => _overrides.Select(kvp => kvp.Key);

		public IEnumerable<string> AxisOverrides => _axisOverrides.Select(kvp => kvp.Key);

		public IEnumerable<string> InversedButtons => _inverses;

		public void SetAxis(string name, int value)
		{
			if (_axisOverrides.ContainsKey(name))
			{
				_axisOverrides[name] = value;
			}
			else
			{
				_axisOverrides.Add(name, value);
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
			_axisOverrides.Clear();
			_inverses.Clear();
		}
	}
}
