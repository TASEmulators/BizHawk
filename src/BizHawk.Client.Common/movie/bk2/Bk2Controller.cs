using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal class Bk2Controller : IMovieController
	{
		private readonly WorkingDictionary<string, bool> _myBoolButtons = new();
		private readonly WorkingDictionary<string, int> _myAxisControls = new();

		private readonly Bk2ControllerDefinition _type;
		private readonly string _systemId;

		private IList<ControlMap> _controlsOrdered;

		private IList<ControlMap> ControlsOrdered => _controlsOrdered ??= _type.OrderedControlsFlat
			.Select(c => new ControlMap
			{
				Name = c,
				IsBool = _type.BoolButtons.Contains(c),
				IsAxis = _type.Axes.ContainsKey(c)
			})
			.ToArray();

		public Bk2Controller(string key, ControllerDefinition definition, string systemId) : this(definition, systemId)
		{
			if (!string.IsNullOrEmpty(key))
			{
				var groups = key.Split(new[] { "#" }, StringSplitOptions.RemoveEmptyEntries);

				_type.ControlsFromLog = groups
					.Select(group => group.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
					.ToArray();
			}
		}

		public Bk2Controller(ControllerDefinition definition, string systemId)
		{
			_type = new Bk2ControllerDefinition(definition);
			_systemId = systemId;
			foreach ((string axisName, AxisSpec range) in definition.Axes)
			{
				_myAxisControls[axisName] = range.Neutral;
			}
		}

		public ControllerDefinition Definition => _type;

		public bool IsPressed(string button) => _myBoolButtons[button];
		public int AxisValue(string name) => _myAxisControls[name];

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Array.Empty<(string, int)>();

		public void SetHapticChannelStrength(string name, int strength) {}

		private Bk2LogEntryGenerator _logEntryGenerator;
		public Bk2LogEntryGenerator LogEntryGenerator => _logEntryGenerator ??= new Bk2LogEntryGenerator(_systemId, this);

		public void SetFrom(IController source)
		{
			for (int index = 0; index < Definition.BoolButtons.Count; index++)
			{
				string button = Definition.BoolButtons[index];
				_myBoolButtons[button] = source.IsPressed(button);
			}

			foreach (var name in Definition.Axes.Keys)
			{
				_myAxisControls[name] = source.AxisValue(name);
			}
		}

		public void SetFromSticky(IStickyAdapter controller)
		{
			foreach (var button in Definition.BoolButtons)
			{
				_myBoolButtons[button] = controller.IsSticky(button);
			}

			// axes don't have sticky logic, so latch default value
			foreach (var (k, v) in Definition.Axes) _myAxisControls[k] = v.Neutral;
		}

		public void SetFromMnemonic(string mnemonic)
		{
			if (!string.IsNullOrWhiteSpace(mnemonic))
			{
				var iterator = 0;

				foreach (var key in ControlsOrdered)
				{
					while (mnemonic[iterator] == '|') iterator++;

					if (key.IsBool)
					{
						_myBoolButtons[key.Name] = mnemonic[iterator] != '.';
						iterator++;
					}
					else if (key.IsAxis)
					{
						var commaIndex = mnemonic.IndexOf(',', iterator);
#if NET6_0_OR_GREATER
						var val = int.Parse(mnemonic.AsSpan(start: iterator, length: commaIndex - iterator));
#else
						var axisValueString = mnemonic.Substring(startIndex: iterator, length: commaIndex - iterator);
						var val = int.Parse(axisValueString);
#endif
						_myAxisControls[key.Name] = val;

						iterator = commaIndex + 1;
					}
				}
			}
		}

		public void SetBool(string buttonName, bool value)
		{
			_myBoolButtons[buttonName] = value;
		}

		public void SetAxis(string buttonName, int value)
		{
			_myAxisControls[buttonName] = value;
		}

		private class ControlMap
		{
			public string Name { get; set; }
			public bool IsBool { get; set; }
			public bool IsAxis { get; set; }
		}

		private class Bk2ControllerDefinition : ControllerDefinition
		{
			public IReadOnlyList<IReadOnlyList<string>> ControlsFromLog = null;

			public Bk2ControllerDefinition(ControllerDefinition source)
				: base(source)
			{
			}

			protected override IReadOnlyList<IReadOnlyList<string>> GenOrderedControls()
				=> ControlsFromLog is not null && ControlsFromLog.Count is not 0 ? ControlsFromLog : base.GenOrderedControls();
		}
	}
}
