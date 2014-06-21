using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Bk2ControllerAdapter : IMovieController
	{
		private string _logKey = string.Empty;

		public Bk2ControllerAdapter(string key)
		{
			_logKey = key;
			SetLogOverride();
		}

		private void SetLogOverride()
		{
			if (!string.IsNullOrEmpty(_logKey))
			{
				// TODO: this could be cleaned up into a LINQ select
				List<List<string>> controls = new List<List<string>>();
				var groups = _logKey.Split(new[] { "#" }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var group in groups)
				{
					var buttons = group.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).ToList();
					controls.Add(buttons);
				}

				_type.ControlsFromLog = controls;
			}
		}

		#region IController Implementation

		public bool this[string button]
		{
			get { return MyBoolButtons[button]; }
		}

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

		public ControllerDefinition Type
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
			foreach (string button in playerSource.Type.BoolButtons)
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

				bool val = playerSource[button];
				MyBoolButtons[button] = val;
			}
		}

		/// <summary>
		/// latches all buttons from the provided source
		/// </summary>
		public void LatchFromSource(IController source)
		{
			foreach (string button in Type.BoolButtons)
			{
				MyBoolButtons[button] = source[button];
			}

			foreach (string name in Type.FloatControls)
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
				var buttons = Type.ControlsOrdered.SelectMany(x => x).ToList();
				var iterator = 0;
				var boolIt = 0;
				var floatIt = 0;

				for (int i = 0; i < buttons.Count; i++)
				{
					var b = buttons[i];
					if (def.BoolButtons.Contains(buttons[i]))
					{
						MyBoolButtons[buttons[i]] = trimmed[iterator] == '.' ? false : true;
						iterator++;
						boolIt++;
					}
					else if (def.FloatControls.Contains(buttons[i]))
					{
						var temp = trimmed.Substring(iterator, 3);
						var val = int.Parse(temp);

						MyFloatControls[buttons[i]] = val;
						iterator += 4;
						floatIt++;
					}
				}
			}
		}

		#endregion

		public class Bk2ControllerDefinition : ControllerDefinition
		{
			public Bk2ControllerDefinition()
				: base()
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

		

		private readonly WorkingDictionary<string, bool> MyBoolButtons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, float> MyFloatControls = new WorkingDictionary<string, float>();
	}
}
