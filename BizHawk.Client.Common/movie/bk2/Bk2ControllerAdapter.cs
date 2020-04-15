using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Bk2Controller : IMovieController
	{
		private readonly string _logKey = "";
		private readonly WorkingDictionary<string, bool> _myBoolButtons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, float> _myAxisControls = new WorkingDictionary<string, float>();

		private Bk2ControllerDefinition _type = new Bk2ControllerDefinition();
		private List<ControlMap> _controlsOrdered = new List<ControlMap>();

		public Bk2Controller()
		{
		}

		public Bk2Controller(string key)
		{
			_logKey = key;
			SetLogOverride();
		}

		#region IController Implementation

		public ControllerDefinition Definition
		{
			get => _type;
			set
			{
				_type = new Bk2ControllerDefinition(value);
				SetLogOverride();

				var def = Global.Emulator.ControllerDefinition;
				_controlsOrdered =  Definition.ControlsOrdered
					.SelectMany(c => c)
					.Select(c => new ControlMap
					{
						Name = c,
						IsBool = def.BoolButtons.Contains(c),
						IsAxis = def.AxisControls.Contains(c)
					})
					.ToList();
			}
		}

		public bool IsPressed(string button) => _myBoolButtons[button];
		public float AxisValue(string name) => _myAxisControls[name];

		#endregion

		#region IMovieController Implementation

		public void LatchFrom(IController source)
		{
			foreach (var button in Definition.BoolButtons)
			{
				_myBoolButtons[button] = source.IsPressed(button);
			}

			foreach (var name in Definition.AxisControls)
			{
				_myAxisControls[name] = source.AxisValue(name);
			}
		}

		public void LatchPlayerFrom(IController playerSource, int controllerNum)
		{
			foreach (var button in playerSource.Definition.BoolButtons)
			{
				var bnp = ButtonNameParser.Parse(button);

				if (bnp?.PlayerNum != controllerNum)
				{
					continue;
				}

				var val = playerSource.IsPressed(button);
				_myBoolButtons[button] = val;
			}

			foreach (var button in Definition.AxisControls)
			{
				var bnp = ButtonNameParser.Parse(button);

				if (bnp?.PlayerNum != controllerNum)
				{
					continue;
				}

				var val = playerSource.AxisValue(button);

				_myAxisControls[button] = val;
			}
		}

		public void LatchFromSticky(IStickyController controller)
		{
			foreach (var button in Definition.BoolButtons)
			{
				_myBoolButtons[button] = controller.IsSticky(button);
			}

			// float controls don't have sticky logic, so latch default value
			for (int i = 0; i < Definition.AxisControls.Count; i++)
			{
				_myAxisControls[Definition.AxisControls[i]] = Definition.AxisRanges[i].Mid;
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

		public void SetAxis(string buttonName, float value)
		{
			_myAxisControls[buttonName] = value;
		}

		#endregion

		private void SetLogOverride()
		{
			if (!string.IsNullOrEmpty(_logKey))
			{
				var groups = _logKey.Split(new[] { "#" }, StringSplitOptions.RemoveEmptyEntries);
				var controls = groups
					.Select(group => group.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).ToList())
					.ToList();

				_type.ControlsFromLog = controls;
			}
		}

		private class ControlMap
		{
			public string Name { get; set; }
			public bool IsBool { get; set; }
			public bool IsAxis { get; set; }
		}

		private class Bk2ControllerDefinition : ControllerDefinition
		{
			public Bk2ControllerDefinition()
			{
			}

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
