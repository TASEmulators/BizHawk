using System;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class BkmControllerAdapter : IMovieController
	{
		#region IController Implementation

		public ControllerDefinition Definition { get; set; }

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
			if (ControlType == "Null Controller")
			{
				return;
			}

			if (ControlType == "Lynx Controller")
			{
				SetLynxControllersAsMnemonic(mnemonic);
				return;
			}

			if (ControlType == "SNES Controller")
			{
				SetSNESControllersAsMnemonic(mnemonic);
				return;
			}

			if (ControlType == "Commodore 64 Controller")
			{
				SetC64ControllersAsMnemonic(mnemonic);
				return;
			}

			if (ControlType == "GBA Controller")
			{
				SetGBAControllersAsMnemonic(mnemonic);
				return;
			}

			if (ControlType == "Atari 7800 ProLine Joystick Controller")
			{
				SetAtari7800AsMnemonic(mnemonic);
				return;
			}

			if (ControlType == "Dual Gameboy Controller")
			{
				SetDualGameBoyControllerAsMnemonic(mnemonic);
				return;
			}

			if (ControlType == "WonderSwan Controller")
			{
				SetWonderSwanControllerAsMnemonic(mnemonic);
				return;
			}

			if (ControlType == "Nintento 64 Controller")
			{
				SetN64ControllersAsMnemonic(mnemonic);
				return;
			}

			if (ControlType == "Saturn Controller")
			{
				SetSaturnControllersAsMnemonic(mnemonic);
				return;
			}

			if (ControlType == "PSP Controller")
			{
				// TODO
				return;
			}

			if (ControlType == "GPGX Genesis Controller")
			{
				if (IsGenesis6Button())
				{
					SetGenesis6ControllersAsMnemonic(mnemonic);
				}
				else
				{
					SetGenesis3ControllersAsMnemonic(mnemonic);
				}

				return;
			}

			var c = new MnemonicChecker(mnemonic);

			MyBoolButtons.Clear();

			int start = 3;
			if (ControlType == "NES Controller")
			{
				if (mnemonic.Length < 2)
				{
					return;
				}
				else if (mnemonic[1] == 'P')
				{
					Force("Power", true);
				}
				else if (mnemonic[1] == 'E')
				{
					Force("FDS Eject", true);
				}
				else if (mnemonic[1] == '0')
				{
					Force("FDS Insert 0", true);
				}
				else if (mnemonic[1] == '1')
				{
					Force("FDS Insert 1", true);
				}
				else if (mnemonic[1] == '2')
				{
					Force("FDS Insert 2", true);
				}
				else if (mnemonic[1] == '3')
				{
					Force("FDS Insert 3", true);
				}
				else if (mnemonic[1] == 'c')
				{
					Force("VS Coin 1", true);
				}
				else if (mnemonic[1] == 'C')
				{
					Force("VS Coin 2", true);
				}
				else if (mnemonic[1] != '.')
				{
					Force("Reset", true);
				}
			}
			if (ControlType == "Gameboy Controller")
			{
				if (mnemonic.Length < 2) return;
				Force("Power", mnemonic[1] != '.');
			}
			if (ControlType == "Genesis 3-Button Controller")
			{
				if (mnemonic.Length < 2) return;
				Force("Reset", mnemonic[1] != '.');
			}
			if (ControlType == "SMS Controller" || ControlType == "TI83 Controller" || ControlType == "ColecoVision Basic Controller")
			{
				start = 1;
			}
			if (ControlType == "Atari 2600 Basic Controller")
			{
				if (mnemonic.Length < 2) return;
				Force("Reset", mnemonic[1] != '.' && mnemonic[1] != '0');
				Force("Select", mnemonic[2] != '.' && mnemonic[2] != '0');
				start = 4;
			}
			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (BkmMnemonicConstants.BUTTONS[ControlType].Count + 1);
				int ctr = start;
				if (mnemonic.Length < srcindex + ctr + BkmMnemonicConstants.BUTTONS[ControlType].Count - 1)
				{
					return;
				}
				string prefix = "";
				if (ControlType != "Gameboy Controller" && ControlType != "TI83 Controller")
				{
					prefix = "P" + player + " ";
				}
				foreach (string button in BkmMnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force(prefix + button, c[srcindex + ctr++]);
				}
			}
			if (ControlType == "SMS Controller")
			{
				int srcindex = BkmMnemonicConstants.PLAYERS[ControlType] * (BkmMnemonicConstants.BUTTONS[ControlType].Count + 1);
				int ctr = start;
				foreach (var command in BkmMnemonicConstants.COMMANDS[ControlType].Keys)
				{
					Force(command, c[srcindex + ctr++]);
				}
			}
		}

		#endregion

		private readonly WorkingDictionary<string, bool> MyBoolButtons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, float> MyFloatControls = new WorkingDictionary<string, float>();

		private bool IsGenesis6Button()
		{
			return this.Definition.BoolButtons.Contains("P1 X");
		}

		private void Force(string button, bool state)
		{
			MyBoolButtons[button] = state;
		}

		private void Force(string name, float state)
		{
			MyFloatControls[name] = state;
		}

		private string ControlType { get { return Definition.Name; } }

		private void SetGBAControllersAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();
			if (mnemonic.Length < 2)
			{
				return;
			}
			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			int start = 3;
			foreach (string button in BkmMnemonicConstants.BUTTONS[ControlType].Keys)
			{
				Force(button, c[start++]);
			}
		}

		private void SetGenesis6ControllersAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();

			if (mnemonic.Length < 2)
			{
				return;
			}

			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			else if (mnemonic[1] != '.' && mnemonic[1] != '0')
			{
				Force("Reset", true);
			}

			if (mnemonic.Length < 9)
			{
				return;
			}

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (BkmMnemonicConstants.BUTTONS[ControlType].Count + 1);

				if (mnemonic.Length < srcindex + 3 + BkmMnemonicConstants.BUTTONS[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (string button in BkmMnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}
			}
		}

		private void SetGenesis3ControllersAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();

			if (mnemonic.Length < 2)
			{
				return;
			}

			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			else if (mnemonic[1] != '.' && mnemonic[1] != '0')
			{
				Force("Reset", true);
			}

			if (mnemonic.Length < 9)
			{
				return;
			}

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (BkmMnemonicConstants.BUTTONS["GPGX 3-Button Controller"].Count + 1);

				if (mnemonic.Length < srcindex + 3 + BkmMnemonicConstants.BUTTONS["GPGX 3-Button Controller"].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (string button in BkmMnemonicConstants.BUTTONS["GPGX 3-Button Controller"].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}
			}
		}

		private void SetSNESControllersAsMnemonic(string mnemonic)
		{
			var c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();

			if (mnemonic.Length < 2)
			{
				return;
			}

			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			else if (mnemonic[1] != '.' && mnemonic[1] != '0')
			{
				Force("Reset", true);
			}

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (BkmMnemonicConstants.BUTTONS[ControlType].Count + 1);

				if (mnemonic.Length < srcindex + 3 + BkmMnemonicConstants.BUTTONS[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (var button in BkmMnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}
			}
		}

		private void SetLynxControllersAsMnemonic(string mnemonic)
		{
			var c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();

			if (mnemonic.Length < 2)
			{
				return;
			}

			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (BkmMnemonicConstants.BUTTONS[ControlType].Count + 1);

				if (mnemonic.Length < srcindex + 3 + BkmMnemonicConstants.BUTTONS[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (var button in BkmMnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force(button, c[srcindex + start++]);
				}
			}
		}

		private void SetN64ControllersAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();

			if (mnemonic.Length < 2)
			{
				return;
			}

			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			else if (mnemonic[1] != '.' && mnemonic[1] != '0')
			{
				Force("Reset", true);
			}

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (BkmMnemonicConstants.BUTTONS[ControlType].Count + BkmMnemonicConstants.ANALOGS[ControlType].Count * 4 + 1 + 1);

				if (mnemonic.Length < srcindex + 3 + BkmMnemonicConstants.BUTTONS[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (string button in BkmMnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}

				foreach (string name in BkmMnemonicConstants.ANALOGS[ControlType].Keys)
				{
					Force("P" + player + " " + name, Int32.Parse(mnemonic.Substring(srcindex + start, 4)));
					start += 5;
				}
			}
		}

		private void SetSaturnControllersAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();

			if (mnemonic.Length < 2)
			{
				return;
			}

			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			else if (mnemonic[1] != '.' && mnemonic[1] != '0')
			{
				Force("Reset", true);
			}

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (BkmMnemonicConstants.BUTTONS[ControlType].Count + 1);

				if (mnemonic.Length < srcindex + 3 + BkmMnemonicConstants.BUTTONS[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (string button in BkmMnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}
			}
		}

		private void SetAtari7800AsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();

			if (mnemonic.Length < 5)
			{
				return;
			}
			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			if (mnemonic[2] == 'r')
			{
				Force("Reset", true);
			}
			if (mnemonic[3] == 's')
			{
				Force("Select", true);
			}
			if (mnemonic[4] == 'p')
			{
				Force("Pause", true);
			}

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (BkmMnemonicConstants.BUTTONS[ControlType].Count + 1);
				int start = 6;
				if (mnemonic.Length < srcindex + start + BkmMnemonicConstants.BUTTONS[ControlType].Count)
				{
					return;
				}

				foreach (string button in BkmMnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}
			}
		}

		private void SetDualGameBoyControllerAsMnemonic(string mnemonic)
		{
			var checker = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();
			for (int i = 0; i < BkmMnemonicConstants.DGBMnemonic.Length; i++)
			{
				var t = BkmMnemonicConstants.DGBMnemonic[i];
				if (t.Item1 != null)
				{
					Force(t.Item1, checker[i]);
				}
			}
		}

		private void SetWonderSwanControllerAsMnemonic(string mnemonic)
		{
			var checker = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();
			for (int i = 0; i < BkmMnemonicConstants.WSMnemonic.Length; i++)
			{
				var t = BkmMnemonicConstants.WSMnemonic[i];
				if (t.Item1 != null)
				{
					Force(t.Item1, checker[i]);
				}
			}
		}

		private void SetC64ControllersAsMnemonic(string mnemonic)
		{
			var c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();


			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (BkmMnemonicConstants.BUTTONS[ControlType].Count + 1);

				if (mnemonic.Length < srcindex + 1 + BkmMnemonicConstants.BUTTONS[ControlType].Count - 1)
				{
					return;
				}

				int start = 1;
				foreach (var button in BkmMnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}
			}

			int startk = 13;
			foreach (string button in BkmMnemonicConstants.BUTTONS["Commodore 64 Keyboard"].Keys)
			{
				Force(button, c[startk++]);
			}
		}

		private sealed class MnemonicChecker
		{
			private readonly string _mnemonic;

			public MnemonicChecker(string mnemonic)
			{
				_mnemonic = mnemonic;
			}

			public bool this[int c]
			{
				get
				{
					if (string.IsNullOrEmpty(_mnemonic))
					{
						return false;
					}

					if (_mnemonic[c] == '.')
					{
						return false;
					}

					if (_mnemonic[c] == '?')
					{
						return new Random((int)DateTime.Now.Ticks).Next(0, 10) > 5;
					}

					return true;
				}
			}
		}
	}
}
