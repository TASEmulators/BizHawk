using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class OverrideAdapter : IInputAdapter
	{
		public IController Source { get; set; }

		public ControllerDefinition Definition => Source.Definition;

		private readonly Dictionary<string, bool> _overrides = new Dictionary<string, bool>();
		private readonly Dictionary<string, int> _axisOverrides = new Dictionary<string, int>();
		private readonly List<string> _inverses = new List<string>();

		public OverrideAdapter(IController source)
		{
			this.Source = source;
		}

		/// <exception cref="InvalidOperationException"><paramref name="button"/> not overridden</exception>
		public bool IsPressed(string button)
		{
			if (_overrides.TryGetValue(button, out var b)) return b;

			bool invert = _inverses.Contains(button);
			return Source.IsPressed(button) ^ invert;
		}

		public int AxisValue(string name)
			=> _axisOverrides.TryGetValue(name, out var i) ? i : Source.AxisValue(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => throw new NotImplementedException(); // no idea --yoshi

		public void SetHapticChannelStrength(string name, int strength) => throw new NotImplementedException(); // no idea --yoshi

		public void SetAxis(string name, int value)
			=> _axisOverrides[name] = value;

		public void SetButton(string button, bool value)
		{
			_overrides[button] = value;
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
