using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal class Bk2Controller : IMovieController
	{
		private readonly WorkingDictionary<string, bool> _myBoolButtons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, int> _myAxisControls = new WorkingDictionary<string, int>();

		private readonly Bk2ControllerDefinition _type;
		private readonly List<ControlMap> _controlsOrdered;

		public Bk2Controller(string key, ControllerDefinition definition) : this(definition)
		{
			if (!string.IsNullOrEmpty(key))
			{
				var groups = key.Split(new[] { "#" }, StringSplitOptions.RemoveEmptyEntries);

				_type.ControlsFromLog = groups
					.Select(group => group.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).ToList())
					.ToList();
			}
		}

		public Bk2Controller(ControllerDefinition definition)
		{
			_type = new Bk2ControllerDefinition(definition);
			_controlsOrdered =  Definition.ControlsOrdered
				.SelectMany(c => c)
				.Select(c => new ControlMap
				{
					Name = c,
					IsBool = _type.BoolButtons.Contains(c),
					IsAxis = _type.Axes.ContainsKey(c)
				})
				.ToList();
		}

		public ControllerDefinition Definition => _type;

		public bool IsPressed(string button) => _myBoolButtons[button];
		public int AxisValue(string name) => _myAxisControls[name];

		public void SetFrom(IController source)
		{
			foreach (var button in Definition.BoolButtons)
			{
				_myBoolButtons[button] = source.IsPressed(button);
			}

			foreach (var name in Definition.Axes.Keys)
			{
				_myAxisControls[name] = source.AxisValue(name);
			}
		}

		public void SetFromSticky(IStickyController controller)
		{
			foreach (var button in Definition.BoolButtons)
			{
				_myBoolButtons[button] = controller.IsSticky(button);
			}

			// axes don't have sticky logic, so latch default value
			foreach (var kvp in Definition.Axes)
			{
				_myAxisControls[kvp.Key] = kvp.Value.Range.Mid;
			}
		}

		public void SetFromMnemonic(string mnemonic)
		{
			if (!string.IsNullOrWhiteSpace(mnemonic))
			{
				var trimmed = mnemonic.Replace("|", "");
				var iterator = 0;

				foreach (var key in _controlsOrdered)
				{
					if (key.IsBool)
					{
						_myBoolButtons[key.Name] = trimmed[iterator] != '.';
						iterator++;
					}
					else if (key.IsAxis)
					{
						var commaIndex = trimmed.Substring(iterator).IndexOf(',');
						var temp = trimmed.Substring(iterator, commaIndex);
						var val = int.Parse(temp.Trim());
						_myAxisControls[key.Name] = val;

						iterator += commaIndex + 1;
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
			public Bk2ControllerDefinition(ControllerDefinition source)
				: base(source)
			{
			}

			public List<List<string>> ControlsFromLog { private get; set; } = new List<List<string>>();

			public override IEnumerable<IEnumerable<string>> ControlsOrdered =>
				ControlsFromLog.Any()
					? ControlsFromLog
					: base.ControlsOrdered;
		}
	}
}
