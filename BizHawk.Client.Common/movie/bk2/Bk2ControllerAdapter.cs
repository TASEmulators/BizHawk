using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Bk2ControllerAdapter : IMovieController
	{
		private readonly string _logKey = "";
		private readonly WorkingDictionary<string, bool> _myBoolButtons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, float> _myFloatControls = new WorkingDictionary<string, float>();

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

		// TODO: get rid of this, add a SetBool() method or something for the set access, replace get wtih IsPressed
		public bool this[string button]
		{
			get
			{
				return _myBoolButtons[button];
			}

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

		public float GetFloat(string name)
		{
			return _myFloatControls[name];
		}

		#endregion

		#region IMovieController Implementation

		private Bk2ControllerDefinition _type = new Bk2ControllerDefinition();

		public ControllerDefinition Definition
		{
			get
			{
				return _type;
			}

			set
			{
				_type = new Bk2ControllerDefinition(value);
				SetLogOverride();
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

			foreach (var button in Definition.FloatControls)
			{
				var bnp = ButtonNameParser.Parse(button);

				if (bnp?.PlayerNum != playerNum)
				{
					continue;
				}

				var val = playerSource.GetFloat(button);

				_myFloatControls[button] = val;
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

			foreach (var name in Definition.FloatControls)
			{
				_myFloatControls[name] = source.GetFloat(name);
			}
		}

		/// <summary>
		/// latches sticky buttons from <see cref="Global.AutofireStickyXORAdapter"/>
		/// </summary>
		public void LatchSticky()
		{
			foreach (var button in Definition.BoolButtons)
			{
				_myBoolButtons[button] = Global.AutofireStickyXORAdapter.IsSticky(button);
			}

			// float controls don't have sticky logic. but old values remain in MovieOutputHardpoint if we don't update this here
			foreach (var name in Definition.FloatControls)
			{
				_myFloatControls[name] = Global.AutofireStickyXORAdapter.GetFloat(name);
			}
		}

		/// <summary>
		/// latches all buttons from the supplied mnemonic string
		/// </summary>
		public void SetControllersAsMnemonic(string mnemonic)
		{
			if (!string.IsNullOrWhiteSpace(mnemonic))
			{
				var def = Global.Emulator.ControllerDefinition;
				var trimmed = mnemonic.Replace("|", "");
				var buttons = Definition.ControlsOrdered.SelectMany(c => c);
				var iterator = 0;

				foreach (var key in buttons)
				{
					if (def.BoolButtons.Contains(key))
					{
						_myBoolButtons[key] = trimmed[iterator] != '.';
						iterator++;
					}
					else if (def.FloatControls.Contains(key))
					{
						var commaIndex = trimmed.Substring(iterator).IndexOf(',');
						var temp = trimmed.Substring(iterator, commaIndex);
						var val = int.Parse(temp.Trim());
						_myFloatControls[key] = val;

						iterator += commaIndex + 1;
					}
				}
			}
		}

		#endregion

		public void SetFloat(string buttonName, float value)
		{
			_myFloatControls[buttonName] = value;
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

			public override IEnumerable<IEnumerable<string>> ControlsOrdered
			{
				get
				{
					if (ControlsFromLog.Any())
					{
						return ControlsFromLog;
					}

					return base.ControlsOrdered;
				}
			}
		}
	}
}
