using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Bk2ControllerAdapter : IMovieController
	{
		private class ControlMap
		{
			public string Name { get; set; }
			public bool IsBool { get; set; }
			public bool IsAxis { get; set; }
		}

		private List<ControlMap> _controlsOrdered = new List<ControlMap>();

		private readonly string _logKey = "";
		private readonly WorkingDictionary<string, bool> _myBoolButtons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, float> _myAxisControls = new WorkingDictionary<string, float>();

		public Bk2ControllerAdapter()
		{
		}

		public Bk2ControllerAdapter(string key)
		{
			_logKey = key;
			SetLogOverride();
		}

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

		// TODO: get rid of this, add a SetBool() method or something for the set access, replace get with IsPressed
		public bool this[string button]
		{
			get => _myBoolButtons[button];
			set
			{
				if (_myBoolButtons.ContainsKey(button))
				{
					_myBoolButtons[button] = value;
				}
			}
		}

		#region IController Implementation

		public bool IsPressed(string button)
		{
			return _myBoolButtons[button];
		}

		public float AxisValue(string name)
		{
			return _myAxisControls[name];
		}

		#endregion

		#region IMovieController Implementation

		private Bk2ControllerDefinition _type = new Bk2ControllerDefinition();

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

		/// <summary>
		/// latches one player from the source
		/// </summary>
		public void LatchPlayerFromSource(IController playerSource, int playerNum)
		{
			foreach (var button in playerSource.Definition.BoolButtons)
			{
				var bnp = ButtonNameParser.Parse(button);

				if (bnp?.PlayerNum != playerNum)
				{
					continue;
				}

				var val = playerSource.IsPressed(button);
				_myBoolButtons[button] = val;
			}

			foreach (var button in Definition.AxisControls)
			{
				var bnp = ButtonNameParser.Parse(button);

				if (bnp?.PlayerNum != playerNum)
				{
					continue;
				}

				var val = playerSource.AxisValue(button);

				_myAxisControls[button] = val;
			}
		}

		/// <summary>
		/// latches all buttons from the provided source
		/// </summary>
		public void LatchFromSource(IController source)
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

		/// <summary>
		/// latches sticky buttons from <see cref="Global.AutofireStickyXORAdapter"/>
		/// </summary>
		public void LatchSticky()
		{
			foreach (var button in Definition.BoolButtons)
			{
				_myBoolButtons[button] = Global.InputManager.AutofireStickyXorAdapter.IsSticky(button);
			}

			// float controls don't have sticky logic, so latch default value
			for (int i = 0; i < Definition.AxisControls.Count; i++)
			{
				_myAxisControls[Definition.AxisControls[i]] = Definition.AxisRanges[i].Mid;
			}
		}

		/// <summary>
		/// latches all buttons from the supplied mnemonic string
		/// </summary>
		public void SetControllersAsMnemonic(string mnemonic)
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

		#endregion

		public void SetAxis(string buttonName, float value)
		{
			_myAxisControls[buttonName] = value;
		}

		public class Bk2ControllerDefinition : ControllerDefinition
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
