using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IStickyAdapter : IInputAdapter
	{
		bool IsSticky(string buttonOrAxis);
	}

	public class StickyXorAdapter : IStickyAdapter
	{
		private readonly HashSet<string> _buttonHolds = [ ];
		// if SetAxis() is called (typically virtual pads), then that axis will entirely override the Source input
		// otherwise, the source is passed thru.
		private readonly Dictionary<string, int> _axisHolds = [ ];

		public IController Source { get; set; }
		public ControllerDefinition Definition => Source.Definition;

		public IReadOnlyCollection<string> CurrentStickies => _buttonHolds; // the callsite doesn't care about sticky axes

		public bool IsPressed(string button)
		{
			var source = Source.IsPressed(button);
			source ^= _buttonHolds.Contains(button);
			return source;
		}

		public int AxisValue(string name)
		{
			return _axisHolds.TryGetValue(name, out int axisValue) ? axisValue : Source.AxisValue(name);
		}

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Source.GetHapticsSnapshot();

		public void SetHapticChannelStrength(string name, int strength) => Source.SetHapticChannelStrength(name, strength);

		public void SetButtonHold(string button, bool enabled)
		{
			if (enabled)
			{
				_buttonHolds.Add(button);
			}
			else
			{
				_buttonHolds.Remove(button);
			}
		}

		public void SetAxisHold(string name, int? value)
		{
			if (value.HasValue)
			{
				_axisHolds[name] = value.Value;
			}
			else
			{
				_axisHolds.Remove(name);
			}
		}

		public bool IsSticky(string buttonOrAxis) => _buttonHolds.Contains(buttonOrAxis) || _axisHolds.ContainsKey(buttonOrAxis);

		public void ClearStickies()
		{
			_buttonHolds.Clear();
			_axisHolds.Clear();
		}

		private List<string> _justPressed = [ ];

		public void MassToggleStickyState(List<string> buttons)
		{
			foreach (var button in buttons.Where(button => !_justPressed.Contains(button)))
			{
				if (_buttonHolds.Contains(button))
				{
					_buttonHolds.Remove(button);
				}
				else
				{
					_buttonHolds.Add(button);
				}
			}

			_justPressed = buttons;
		}
	}

	public class AutoFireStickyXorAdapter : IStickyAdapter, IInputAdapter
	{
		// TODO: Change the AutoHold adapter to be one of these, with an 'Off' value of 0?
		// Probably would have slightly lower performance, but it seems weird to have such a similar class that is only used once.
		private int _onFrames;
		private int _offFrames;

		private readonly Dictionary<string, AutoPatternBool> _boolPatterns = [ ];
		private readonly Dictionary<string, AutoPatternAxis> _axisPatterns = [ ];

		public IController Source { get; set; }
		public ControllerDefinition Definition => Source.Definition;

		public IReadOnlyCollection<string> CurrentStickies => _boolPatterns.Keys; // the callsite doesn't care about sticky axes

		public AutoFireStickyXorAdapter()
		{
			_onFrames = 1;
			_offFrames = 1;
		}

		public bool IsPressed(string button)
		{
			var source = Source.IsPressed(button);
			bool patternValue = false;
			if (_boolPatterns.TryGetValue(button, out var pattern))
			{
				patternValue = pattern.PeekNextValue();
			}

			source ^= patternValue;

			return source;
		}

		public int AxisValue(string name)
			=> _axisPatterns.TryGetValue(name, out var pattern)
				? pattern.PeekNextValue()
				: Source.AxisValue(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Source.GetHapticsSnapshot();

		public void SetHapticChannelStrength(string name, int strength) => Source.SetHapticChannelStrength(name, strength);

		public void SetOnOffPatternFromConfig(int onFrames, int offFrames)
		{
			_onFrames = Math.Max(onFrames, 1);
			_offFrames = Math.Max(offFrames, 1);
		}

		public void SetButtonAutofire(string button, bool enabled, AutoPatternBool pattern = null)
		{
			if (enabled)
			{
				pattern ??= new AutoPatternBool(_onFrames, _offFrames);
				_boolPatterns[button] = pattern;
			}
			else
			{
				_boolPatterns.Remove(button);
			}
		}

		public void SetAxisAutofire(string name, int? value, AutoPatternAxis pattern = null)
		{
			if (value.HasValue)
			{
				pattern ??= new AutoPatternAxis(value.Value, _onFrames, 0, _offFrames);
				_axisPatterns[name] = pattern;
			}
			else
			{
				_axisPatterns.Remove(name);
			}
		}

		public bool IsSticky(string buttonOrAxis) => _boolPatterns.ContainsKey(buttonOrAxis) || _axisPatterns.ContainsKey(buttonOrAxis);

		public void ClearStickies()
		{
			_boolPatterns.Clear();
			_axisPatterns.Clear();
		}

		public void IncrementLoops(bool lagged)
		{
			foreach (var v in _boolPatterns.Values) v.GetNextValue(lagged);
			foreach (var v in _axisPatterns.Values) v.GetNextValue(lagged);
		}

		private List<string> _justPressed = [ ];

		public void MassToggleStickyState(List<string> buttons)
		{
			foreach (var button in buttons.Where(button => !_justPressed.Contains(button)))
			{
				SetButtonAutofire(button, !_boolPatterns.ContainsKey(button));
			}

			_justPressed = buttons;
		}
	}
}
