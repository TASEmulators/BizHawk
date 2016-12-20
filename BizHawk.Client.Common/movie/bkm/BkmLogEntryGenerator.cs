using System;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class BkmLogEntryGenerator : ILogEntryGenerator
	{
		private IController _source;

		public void SetSource(IController source)
		{
			_source = source;
			_controlType = source.Definition.Name;
		}

		public string GenerateLogEntry()
		{
			if (_controlType == "Null Controller")
			{
				return "|.|";
			}

			if (_controlType == "Lynx Controller")
			{
				return GetLynxControllersAsMnemonic();
			}

			if (_controlType == "Atari 7800 ProLine Joystick Controller")
			{
				return GetA78ControllersAsMnemonic();
			}

			if (_controlType == "SNES Controller")
			{
				return GetSNESControllersAsMnemonic();
			}

			if (_controlType == "Commodore 64 Controller")
			{
				return GetC64ControllersAsMnemonic();
			}

			if (_controlType == "GBA Controller")
			{
				return GetGBAControllersAsMnemonic();
			}

			if (_controlType == "Dual Gameboy Controller")
			{
				return GetDualGameBoyControllerAsMnemonic();
			}

			if (_controlType == "WonderSwan Controller")
			{
				return GetWonderSwanControllerAsMnemonic();
			}

			if (_controlType == "Nintento 64 Controller")
			{
				return GetN64ControllersAsMnemonic();
			}

			if (_controlType == "Saturn Controller")
			{
				return GetSaturnControllersAsMnemonic();
			}

			if (_controlType == "PSP Controller")
			{
				return "|.|"; // TODO
			}

			if (_controlType == "GPGX Genesis Controller")
			{
				return GetGeneis6ButtonControllersAsMnemonic();
			}

			if (_controlType == "GPGX 3-Button Controller")
			{
				return GetGeneis3ButtonControllersAsMnemonic();
			}

			var input = new StringBuilder("|");

			if (_controlType == "PC Engine Controller")
			{
				input.Append(".");
			}
			else if (_controlType == "Atari 2600 Basic Controller")
			{
				input.Append(IsBasePressed("Reset") ? "r" : ".");
				input.Append(IsBasePressed("Select") ? "s" : ".");
			}
			else if (_controlType == "NES Controller")
			{
				if (IsBasePressed("Power"))
				{
					input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["Power"]);
				}
				else if (IsBasePressed("Reset"))
				{
					input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["Reset"]);
				}
				else if (IsBasePressed("FDS Eject"))
				{
					input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["FDS Eject"]);
				}
				else if (IsBasePressed("FDS Insert 0"))
				{
					input.Append("0");
				}
				else if (IsBasePressed("FDS Insert 1"))
				{
					input.Append("1");
				}
				else if (IsBasePressed("FDS Insert 2"))
				{
					input.Append("2");
				}
				else if (IsBasePressed("FDS Insert 3"))
				{
					input.Append("3");
				}
				else if (IsBasePressed("VS Coin 1"))
				{
					input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["VS Coin 1"]);
				}
				else if (IsBasePressed("VS Coin 2"))
				{
					input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["VS Coin 2"]);
				}
				else
				{
					input.Append('.');
				}
			}
			else if (_controlType == "Genesis 3-Button Controller")
			{
				if (IsBasePressed("Power"))
				{
					input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["Power"]);
				}
				else if (IsBasePressed("Reset"))
				{
					input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["Reset"]);
				}
				else
				{
					input.Append('.');
				}
			}
			else if (_controlType == "Gameboy Controller")
			{
				input.Append(IsBasePressed("Power") ? BkmMnemonicConstants.COMMANDS[_controlType]["Power"] : ".");
			}

			if (_controlType != "SMS Controller" && _controlType != "TI83 Controller" && _controlType != "ColecoVision Basic Controller")
			{
				input.Append("|");
			}

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[_controlType]; player++)
			{
				var prefix = String.Empty;
				if (_controlType != "Gameboy Controller" && _controlType != "TI83 Controller")
				{
					prefix = "P" + player + " ";
				}

				foreach (var button in BkmMnemonicConstants.BUTTONS[_controlType].Keys)
				{
					input.Append(IsBasePressed(prefix + button) ? BkmMnemonicConstants.BUTTONS[_controlType][button] : ".");
				}

				input.Append("|");
			}

			if (_controlType == "SMS Controller")
			{
				foreach (var command in BkmMnemonicConstants.COMMANDS[_controlType].Keys)
				{
					input.Append(IsBasePressed(command) ? BkmMnemonicConstants.COMMANDS[_controlType][command] : ".");
				}

				input.Append("|");
			}

			if (_controlType == "TI83 Controller")
			{
				input.Append(".|"); // TODO: perhaps ON should go here?
			}

			return input.ToString();
		}

		public string GenerateInputDisplay()
		{
			return GenerateLogEntry()
				.Replace(".", " ")
				.Replace("|", "")
				.Replace(" 000, 000", "         ");
		}

		public bool IsEmpty
		{
			get
			{
				return EmptyEntry == GenerateLogEntry();
			}
		}

		public string EmptyEntry
		{
			get
			{
				switch (Global.Emulator.SystemId)
				{
					default:
					case "NULL":
						return "|.|";
					case "A26":
						return "|..|.....|.....|";
					case "A78":
						return "|....|......|......|";
					case "TI83":
						return "|..................................................|.|";
					case "NES":
						return "|.|........|........|........|........|";
					case "SNES":
						return "|.|............|............|............|............|";
					case "SMS":
					case "GG":
					case "SG":
						return "|......|......|..|";
					case "GEN":
						return "|.|........|........|";
					case "GB":
						return "|.|........|";
					case "DGB":
						return "|.|........|.|........|";
					case "PCE":
					case "PCECD":
					case "SGX":
						return "|.|........|........|........|........|........|";
					case "Coleco":
						return "|..................|..................|";
					case "C64":
						return "|.....|.....|..................................................................|";
					case "GBA":
						return "|.|..........|";
					case "N64":
						return "|.|............|............|............|............|";
					case "SAT":
						return "|.|.............|.............|";
					case "WSWAN":
						return "|...........|...........|..|";
				}
			}
		}

		public IMovieController MovieControllerAdapter
		{
			get
			{
				return new BkmControllerAdapter();
			}
		}

		#region Privates

		private bool IsBasePressed(string name)
		{
			return _source.IsPressed(name);
		}

		private float GetBaseFloat(string name)
		{
			return _source.GetFloat(name);
		}

		private string _controlType;

		private string GetGBAControllersAsMnemonic()
		{
			var input = new StringBuilder("|");
			if (IsBasePressed("Power"))
			{
				input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["Power"]);
			}
			else
			{
				input.Append(".");
			}

			input.Append("|");
			foreach (var button in BkmMnemonicConstants.BUTTONS[_controlType].Keys)
			{
				input.Append(IsBasePressed(button) ? BkmMnemonicConstants.BUTTONS[_controlType][button] : ".");
			}

			input.Append("|");
			return input.ToString();
		}

		private string GetSNESControllersAsMnemonic()
		{
			var input = new StringBuilder("|");

			if (IsBasePressed("Power"))
			{
				input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["Power"]);
			}
			else if (IsBasePressed("Reset"))
			{
				input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["Reset"]);
			}
			else
			{
				input.Append('.');
			}

			input.Append("|");
			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[_controlType]; player++)
			{
				foreach (var button in BkmMnemonicConstants.BUTTONS[_controlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? BkmMnemonicConstants.BUTTONS[_controlType][button] : ".");
				}

				input.Append("|");
			}

			return input.ToString();
		}

		private string GetC64ControllersAsMnemonic()
		{
			var input = new StringBuilder("|");

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[_controlType]; player++)
			{
				foreach (var button in BkmMnemonicConstants.BUTTONS[_controlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? BkmMnemonicConstants.BUTTONS[_controlType][button] : ".");
				}

				input.Append('|');
			}

			foreach (var button in BkmMnemonicConstants.BUTTONS["Commodore 64 Keyboard"].Keys)
			{
				input.Append(IsBasePressed(button) ? BkmMnemonicConstants.BUTTONS["Commodore 64 Keyboard"][button] : ".");
			}

			input.Append('|');
			return input.ToString();
		}

		private string GetDualGameBoyControllerAsMnemonic()
		{
			// |.|........|.|........|
			var input = new StringBuilder();

			foreach (var t in BkmMnemonicConstants.DGBMnemonic)
			{
				if (t.Item1 != null)
				{
					input.Append(IsBasePressed(t.Item1) ? t.Item2 : '.');
				}
				else
				{
					input.Append(t.Item2); // Separator
				}
			}

			return input.ToString();
		}

		private string GetWonderSwanControllerAsMnemonic()
		{
			// |....|....|...|
			var input = new StringBuilder();

			foreach (var t in BkmMnemonicConstants.WSMnemonic)
			{
				if (t.Item1 != null)
				{
					input.Append(IsBasePressed(t.Item1) ? t.Item2 : '.');
				}
				else
				{
					input.Append(t.Item2); // Separator
				}
			}

			return input.ToString();
		}

		private string GetA78ControllersAsMnemonic()
		{
			var input = new StringBuilder("|");
			input.Append(IsBasePressed("Power") ? 'P' : '.');
			input.Append(IsBasePressed("Reset") ? 'r' : '.');
			input.Append(IsBasePressed("Select") ? 's' : '.');
			input.Append(IsBasePressed("Pause") ? 'p' : '.');
			input.Append('|');

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[_controlType]; player++)
			{
				foreach (var button in BkmMnemonicConstants.BUTTONS[_controlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? BkmMnemonicConstants.BUTTONS[_controlType][button] : ".");
				}

				input.Append('|');
			}

			return input.ToString();
		}

		private string GetLynxControllersAsMnemonic()
		{
			var input = new StringBuilder("|");
			input.Append(IsBasePressed("Power") ? 'P' : '.');
			input.Append('|');

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[_controlType]; player++)
			{
				foreach (var button in BkmMnemonicConstants.BUTTONS[_controlType].Keys)
				{
					input.Append(IsBasePressed(button) ? BkmMnemonicConstants.BUTTONS[_controlType][button] : ".");
				}

				input.Append('|');
			}

			return input.ToString();
		}

		private string GetN64ControllersAsMnemonic()
		{
			var input = new StringBuilder("|");
			if (IsBasePressed("Power"))
			{
				input.Append('P');
			}
			else if (IsBasePressed("Reset"))
			{
				input.Append('r');
			}
			else
			{
				input.Append('.');
			}

			input.Append('|');

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[_controlType]; player++)
			{
				foreach (var button in BkmMnemonicConstants.BUTTONS[_controlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? BkmMnemonicConstants.BUTTONS[_controlType][button] : ".");
				}

				if (BkmMnemonicConstants.ANALOGS[_controlType].Keys.Count > 0)
				{
					foreach (var name in BkmMnemonicConstants.ANALOGS[_controlType].Keys)
					{
						int val;

						// Nasty hackery
						if (name == "Y Axis")
						{
							if (IsBasePressed("P" + player + " A Up"))
							{
								val = 127;
							}
							else if (IsBasePressed("P" + player + " A Down"))
							{
								val = -127;
							}
							else
							{
								val = (int)GetBaseFloat("P" + player + " " + name);
							}
						}
						else if (name == "X Axis")
						{
							if (IsBasePressed("P" + player + " A Left"))
							{
								val = -127;
							}
							else if (IsBasePressed("P" + player + " A Right"))
							{
								val = 127;
							}
							else
							{
								val = (int)GetBaseFloat("P" + player + " " + name);
							}
						}
						else
						{
							val = (int)GetBaseFloat("P" + player + " " + name);
						}

						if (val >= 0)
						{
							input.Append(' ');
						}

						input.Append(String.Format("{0:000}", val)).Append(',');
					}

					input.Remove(input.Length - 1, 1);
				}

				input.Append('|');
			}

			return input.ToString();
		}

		private string GetSaturnControllersAsMnemonic()
		{
			var input = new StringBuilder("|");
			if (IsBasePressed("Power"))
			{
				input.Append('P');
			}
			else if (IsBasePressed("Reset"))
			{
				input.Append('r');
			}
			else
			{
				input.Append('.');
			}

			input.Append('|');

			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[_controlType]; player++)
			{
				foreach (var button in BkmMnemonicConstants.BUTTONS[_controlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? BkmMnemonicConstants.BUTTONS[_controlType][button] : ".");
				}

				input.Append('|');
			}

			return input.ToString();
		}

		private string GetGeneis6ButtonControllersAsMnemonic()
		{
			var input = new StringBuilder("|");

			if (IsBasePressed("Power"))
			{
				input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["Power"]);
			}
			else if (IsBasePressed("Reset"))
			{
				input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["Reset"]);
			}
			else
			{
				input.Append('.');
			}

			input.Append("|");
			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[_controlType]; player++)
			{
				foreach (var button in BkmMnemonicConstants.BUTTONS[_controlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? BkmMnemonicConstants.BUTTONS[_controlType][button] : ".");
				}

				input.Append("|");
			}

			input.Append("|");
			return input.ToString();
		}

		private string GetGeneis3ButtonControllersAsMnemonic()
		{
			var input = new StringBuilder("|");

			if (IsBasePressed("Power"))
			{
				input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["Power"]);
			}
			else if (IsBasePressed("Reset"))
			{
				input.Append(BkmMnemonicConstants.COMMANDS[_controlType]["Reset"]);
			}
			else
			{
				input.Append('.');
			}

			input.Append("|");
			for (int player = 1; player <= BkmMnemonicConstants.PLAYERS[_controlType]; player++)
			{
				foreach (var button in BkmMnemonicConstants.BUTTONS[_controlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? BkmMnemonicConstants.BUTTONS[_controlType][button] : ".");
				}

				input.Append("|");
			}

			input.Append("|");
			return input.ToString();
		}

		#endregion
	}
}
