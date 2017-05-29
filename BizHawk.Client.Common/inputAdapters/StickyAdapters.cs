using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface ISticky : IController
	{
		bool StickyIsInEffect(string button);
	}

	/// <summary>
	/// Used by input display, to determine if either autofire or regular stickies
	/// are "in effect" because we color this scenario differently
	/// </summary>
	public class StickyOrAdapter : IController
	{
		public ControllerDefinition Definition => Source.Definition;

		public bool IsPressed(string button)
		{
			return Source.StickyIsInEffect(button)
				|| SourceStickyOr.StickyIsInEffect(button);
		}

		// pass floats solely from the original source
		// this works in the code because SourceOr is the autofire controller
		public float GetFloat(string name)
		{
			int i = Source.Definition.FloatControls.IndexOf(name);
			return Source.Definition.FloatRanges[i].Mid; // Floats don't make sense in sticky land
		}

		public ISticky Source { get; set; }
		public ISticky SourceStickyOr { get; set; }
	}

	public class StickyXorAdapter : ISticky, IController
	{
		/// <summary>
		/// Determines if a sticky is current mashing the button itself,
		/// If sticky is not set then false, if set, it returns true if the Source is not pressed, else false
		/// </summary>
		public bool StickyIsInEffect(string button)
		{
			if (IsSticky(button))
			{
				return !Source.IsPressed(button);
			}

			return false;
		}

		public ControllerDefinition Definition => Source.Definition;

		public bool IsPressed(string button)
		{
			var source = Source.IsPressed(button);
			source ^= _stickySet.Contains(button);
			return source;
		}

		public float GetFloat(string name)
		{
			var val = _floatSet[name];

			if (val.HasValue)
			{
				return val.Value;
			}

			if (Source == null)
			{
				return 0;
			}

			return Source.GetFloat(name);
		}

		public IController Source { get; set; }

		private List<string> _justPressed = new List<string>();

		private readonly HashSet<string> _stickySet = new HashSet<string>();

		// if SetFloat() is called (typically virtual pads), then that float will entirely override the Source input
		// otherwise, the source is passed thru.
		private readonly WorkingDictionary<string, float?> _floatSet = new WorkingDictionary<string, float?>();

		public void SetFloat(string name, float? value)
		{
			if (value.HasValue)
			{
				_floatSet[name] = value;
			}
			else
			{
				_floatSet.Remove(name);
			}
		}

		public void ClearStickyFloats()
		{
			_floatSet.Clear();
		}

		public void SetSticky(string button, bool isSticky)
		{
			if (isSticky)
			{
				_stickySet.Add(button);
			}
			else
			{
				_stickySet.Remove(button);
			}
		}

		public void Unset(string button)
		{
			_stickySet.Remove(button);
			_floatSet.Remove(button);
		}

		public bool IsSticky(string button)
		{
			return _stickySet.Contains(button);
		}

		public HashSet<string> CurrentStickies => _stickySet;

		public void ClearStickies()
		{
			_stickySet.Clear();
			_floatSet.Clear();
		}

		public void MassToggleStickyState(List<string> buttons)
		{
			foreach (var button in buttons.Where(button => !_justPressed.Contains(button)))
			{
				if (_stickySet.Contains(button))
				{
					_stickySet.Remove(button);
				}
				else
				{
					_stickySet.Add(button);
				}
			}

			_justPressed = buttons;
		}
	}

	public class AutoFireStickyXorAdapter : ISticky, IController
	{
		/// <summary>
		/// Determines if a sticky is current mashing the button itself,
		/// If sticky is not set then false, if set, it returns true if the Source is not pressed, else false
		/// </summary>
		public bool StickyIsInEffect(string button)
		{
			if (IsSticky(button))
			{
				return !Source.IsPressed(button);
			}

			return false;
		}

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

		public float GetFloat(string name)
		{
			if (_floatPatterns.ContainsKey(name))
			{
				return _floatPatterns[name].PeekNextValue();
			}

			if (Source == null)
			{
				return 0;
			}

			return Source.GetFloat(name);
		}

		// TODO: Change the AutoHold adapter to be one of these, with an 'Off' value of 0?
		// Probably would have slightly lower performance, but it seems weird to have such a similar class that is only used once.
		private int _on;
		private int _off;

		public void SetOnOffPatternFromConfig()
		{
			_on = Global.Config.AutofireOn < 1 ? 0 : Global.Config.AutofireOn;
			_off = Global.Config.AutofireOff < 1 ? 0 : Global.Config.AutofireOff;
		}

		private readonly WorkingDictionary<string, AutoPatternBool> _boolPatterns = new WorkingDictionary<string, AutoPatternBool>();
		private readonly WorkingDictionary<string, AutoPatternFloat> _floatPatterns = new WorkingDictionary<string, AutoPatternFloat>();

		public AutoFireStickyXorAdapter()
		{
			_on = 1;
			_off = 1;
		}

		public IController Source { get; set; }

		public void SetFloat(string name, float? value, AutoPatternFloat pattern = null)
		{
			if (value.HasValue)
			{
				if (pattern == null)
				{
					pattern = new AutoPatternFloat(value.Value, _on, 0, _off);
				}

				_floatPatterns[name] = pattern;
			}
			else
			{
				_floatPatterns.Remove(name);
			}
		}

		public void SetSticky(string button, bool isSticky, AutoPatternBool pattern = null)
		{
			if (isSticky)
			{
				if (pattern == null)
				{
					pattern = new AutoPatternBool(_on, _off);
				}

				_boolPatterns[button] = pattern;
			}
			else
			{
				_boolPatterns.Remove(button);
			}
		}

		public bool IsSticky(string button)
		{
			return _boolPatterns.ContainsKey(button) || _floatPatterns.ContainsKey(button);
		}

		public HashSet<string> CurrentStickies => new HashSet<string>(_boolPatterns.Keys);

		public void ClearStickies()
		{
			_boolPatterns.Clear();
			_floatPatterns.Clear();
		}

		public void IncrementLoops(bool lagged)
		{
			for (int i = 0; i < _boolPatterns.Count; i++)
			{
				_boolPatterns.ElementAt(i).Value.GetNextValue(lagged);
			}

			for (int i = 0; i < _floatPatterns.Count; i++)
			{
				_floatPatterns.ElementAt(i).Value.GetNextValue(lagged);
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