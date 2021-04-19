using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IStickyAdapter : IInputAdapter
	{
		bool IsSticky(string button);
	}

	public class StickyXorAdapter : IStickyAdapter
	{
		public ControllerDefinition Definition => Source.Definition;

		public bool IsPressed(string button)
		{
			var source = Source.IsPressed(button);
			source ^= CurrentStickies.Contains(button);
			return source;
		}

		public int AxisValue(string name)
		{
			var val = _axisSet[name];

			if (val.HasValue)
			{
				return val.Value;
			}

			if (Source == null)
			{
				return 0;
			}

			return Source.AxisValue(name);
		}

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Source.GetHapticsSnapshot();

		public void SetHapticChannelStrength(string name, int strength) => Source.SetHapticChannelStrength(name, strength);

		public IController Source { get; set; }

		private List<string> _justPressed = new List<string>();

		// if SetAxis() is called (typically virtual pads), then that axis will entirely override the Source input
		// otherwise, the source is passed thru.
		private readonly WorkingDictionary<string, int?> _axisSet = new WorkingDictionary<string, int?>();

		public void SetAxis(string name, int? value)
		{
			if (value.HasValue)
			{
				_axisSet[name] = value;
			}
			else
			{
				_axisSet.Remove(name);
			}
		}

		public void ClearStickyAxes() => _axisSet.Clear();

		public void SetSticky(string button, bool isSticky)
		{
			if (isSticky)
			{
				CurrentStickies.Add(button);
			}
			else
			{
				CurrentStickies.Remove(button);
			}
		}

		public void Unset(string button)
		{
			CurrentStickies.Remove(button);
			_axisSet.Remove(button);
		}

		public bool IsSticky(string button) => CurrentStickies.Contains(button);

		public HashSet<string> CurrentStickies { get; } = new HashSet<string>();

		public void ClearStickies()
		{
			CurrentStickies.Clear();
			_axisSet.Clear();
		}

		public void MassToggleStickyState(List<string> buttons)
		{
			foreach (var button in buttons.Where(button => !_justPressed.Contains(button)))
			{
				if (CurrentStickies.Contains(button))
				{
					CurrentStickies.Remove(button);
				}
				else
				{
					CurrentStickies.Add(button);
				}
			}

			_justPressed = buttons;
		}
	}

	public class AutoFireStickyXorAdapter : IStickyAdapter, IInputAdapter
	{
		public ControllerDefinition Definition => Source.Definition;

		public bool IsPressed(string button)
		{
			var source = Source.IsPressed(button);
			bool patternValue = false;
			if (_boolPatterns.ContainsKey(button))
			{
				// I can't figure a way to determine right here if it should Peek or Get.
				patternValue = _boolPatterns[button].PeekNextValue();
			}

			source ^= patternValue;

			return source;
		}

		public int AxisValue(string name)
		{
			if (_axisPatterns.ContainsKey(name))
			{
				return _axisPatterns[name].PeekNextValue();
			}

			if (Source == null)
			{
				return 0;
			}

			return Source.AxisValue(name);
		}

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Source.GetHapticsSnapshot();

		public void SetHapticChannelStrength(string name, int strength) => Source.SetHapticChannelStrength(name, strength);

		// TODO: Change the AutoHold adapter to be one of these, with an 'Off' value of 0?
		// Probably would have slightly lower performance, but it seems weird to have such a similar class that is only used once.
		private int _on;
		private int _off;

		public void SetOnOffPatternFromConfig(int on, int off)
		{
			_on = on < 0 ? 0 : on;
			_off = off < 0 ? 0 : off;
		}

		private readonly WorkingDictionary<string, AutoPatternBool> _boolPatterns = new WorkingDictionary<string, AutoPatternBool>();
		private readonly WorkingDictionary<string, AutoPatternAxis> _axisPatterns = new WorkingDictionary<string, AutoPatternAxis>();

		public AutoFireStickyXorAdapter()
		{
			_on = 1;
			_off = 1;
		}

		public IController Source { get; set; }

		public void SetAxis(string name, int? value, AutoPatternAxis pattern = null)
		{
			if (value.HasValue)
			{
				pattern ??= new AutoPatternAxis(value.Value, _on, 0, _off);
				_axisPatterns[name] = pattern;
			}
			else
			{
				_axisPatterns.Remove(name);
			}
		}

		public void SetSticky(string button, bool isSticky, AutoPatternBool pattern = null)
		{
			if (isSticky)
			{
				pattern ??= new AutoPatternBool(_on, _off);
				_boolPatterns[button] = pattern;
			}
			else
			{
				_boolPatterns.Remove(button);
			}
		}

		public bool IsSticky(string button)
		{
			return _boolPatterns.ContainsKey(button) || _axisPatterns.ContainsKey(button);
		}

		public HashSet<string> CurrentStickies => new HashSet<string>(_boolPatterns.Keys);

		public void ClearStickies()
		{
			_boolPatterns.Clear();
			_axisPatterns.Clear();
		}

		public void IncrementLoops(bool lagged)
		{
			for (int i = 0; i < _boolPatterns.Count; i++)
			{
				_boolPatterns.ElementAt(i).Value.GetNextValue(lagged);
			}

			for (int i = 0; i < _axisPatterns.Count; i++)
			{
				_axisPatterns.ElementAt(i).Value.GetNextValue(lagged);
			}
		}

		private List<string> _justPressed = new List<string>();

		public void MassToggleStickyState(List<string> buttons)
		{
			foreach (var button in buttons.Where(button => !_justPressed.Contains(button)))
			{
				SetSticky(button, !_boolPatterns.ContainsKey(button));
			}

			_justPressed = buttons;
		}
	}
}