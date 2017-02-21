using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Bk2ControllerAdapter : IMovieController
	{
		private readonly string _logKey = string.Empty;
		private readonly WorkingDictionary<string, bool> MyBoolButtons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, float> MyFloatControls = new WorkingDictionary<string, float>();

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
					.Select(@group => @group.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).ToList())
					.ToList();

				_type.ControlsFromLog = controls;
			}
		}

		// TODO: get rid of this, add a SetBool() method or something for the set access, replace get wtih IsPressed
		public bool this[string button]
		{
			get
			{
				return MyBoolButtons[button];
			}

			set
			{
				if (MyBoolButtons.ContainsKey(button))
				{
					MyBoolButtons[button] = value;
				}
			}
		}

		#region IController Implementation

		public bool IsPressed(string button)
		{
			return MyBoolButtons[button];
		}

		public float GetFloat(string name)
		{
			return MyFloatControls[name];
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
				if (bnp == null)
				{
					continue;
				}

				if (bnp.PlayerNum != playerNum)
				{
					continue;
				}

				var val = playerSource.IsPressed(button);
				MyBoolButtons[button] = val;
			}

			foreach (var button in Definition.FloatControls)
			{
				var bnp = ButtonNameParser.Parse(button);
				if (bnp == null)
				{
					continue;
				}

				if (bnp.PlayerNum != playerNum)
				{
					continue;
				}

				var val = playerSource.GetFloat(button);

				MyFloatControls[button] = val;
			}
		}

		/// <summary>
		/// latches all buttons from the provided source
		/// </summary>
		public void LatchFromSource(IController source)
		{
			foreach (var button in Definition.BoolButtons)
			{
				MyBoolButtons[button] = source.IsPressed(button);
			}

			foreach (var name in Definition.FloatControls)
			{
				MyFloatControls[name] = source.GetFloat(name);
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
				var buttons = Definition.ControlsOrdered.SelectMany(x => x).ToList();
				var iterator = 0;

				foreach (var key in buttons)
				{
					if (def.BoolButtons.Contains(key))
					{
						this.MyBoolButtons[key] = trimmed[iterator] != '.';
						iterator++;
					}
					else if (def.FloatControls.Contains(key))
					{
						var temp = trimmed.Substring(iterator, 5);
						var val = int.Parse(temp.Trim());
						this.MyFloatControls[key] = val;

						iterator += 6;
					}
				}
			}
		}

		#endregion

		public void SetFloat(string buttonName, float value)
		{
			MyFloatControls[buttonName] = value;
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

			public List<List<string>> ControlsFromLog = new List<List<string>>();

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
