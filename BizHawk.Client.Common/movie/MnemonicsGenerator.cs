using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	// Scheduled for deletion
	public class MnemonicsGenerator
	{
		public IController Source; // Making this public is a temporary hack

		public bool this[int player, string mnemonic]
		{
			get
			{
				return IsBasePressed("P" + player + " " + mnemonic); //TODO: not every controller uses "P"
			}
		}

		public void SetSource(IController source)
		{
			Source = source;
			ControlType = source.Type.Name;
		}

		public bool IsEmpty
		{
			get
			{
				return EmptyMnemonic == GetControllersAsMnemonic();
			}
		}

		public string EmptyMnemonic
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
				}
			}
		}

		#region Privates

		private bool IsBasePressed(string name)
		{
			bool ret = Source.IsPressed(name);
			return ret;
		}

		private float GetBaseFloat(string name)
		{
			return Source.GetFloat(name);
		}

		private string ControlType;

		private string GetGBAControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");
			if (IsBasePressed("Power"))
			{
				input.Append(MnemonicConstants.COMMANDS[ControlType]["Power"]);
			}
			else
			{
				input.Append(".");
			}
			input.Append("|");
			foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
			{
				input.Append(IsBasePressed(button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
			}
			input.Append("|");
			return input.ToString();
		}

		private string GetSNESControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");

			if (IsBasePressed("Power"))
			{
				input.Append(MnemonicConstants.COMMANDS[ControlType]["Power"]);
			}
			else if (IsBasePressed("Reset"))
			{
				input.Append(MnemonicConstants.COMMANDS[ControlType]["Reset"]);
			}
			else
			{
				input.Append('.');
			}

			input.Append("|");
			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
				}
				input.Append("|");
			}

			return input.ToString();
		}

		private string GetC64ControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");

			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
				}
				input.Append('|');
			}

			foreach (string button in MnemonicConstants.BUTTONS["Commodore 64 Keyboard"].Keys)
			{
				input.Append(IsBasePressed(button) ? MnemonicConstants.BUTTONS["Commodore 64 Keyboard"][button] : ".");
			}
			input.Append('|');

			input.Append('|');
			return input.ToString();
		}

		private string GetDualGameBoyControllerAsMnemonic()
		{
			// |.|........|.|........|
			StringBuilder input = new StringBuilder();

			foreach (var t in MnemonicConstants.DGBMnemonic)
			{
				if (t.Item1 != null)
					input.Append(IsBasePressed(t.Item1) ? t.Item2 : '.');
				else
					input.Append(t.Item2); // seperator
			}
			return input.ToString();
		}

		private string GetA78ControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");
			input.Append(IsBasePressed("Power") ? 'P' : '.');
			input.Append(IsBasePressed("Reset") ? 'r' : '.');
			input.Append(IsBasePressed("Select") ? 's' : '.');
			input.Append(IsBasePressed("Pause") ? 'p' : '.');
			input.Append('|');
			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
				}
				input.Append('|');
			}

			return input.ToString();
		}

		private string GetN64ControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");
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

			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
				}

				if (MnemonicConstants.ANALOGS[ControlType].Keys.Count > 0)
				{
					foreach (string name in MnemonicConstants.ANALOGS[ControlType].Keys)
					{
						int val;

						//Nasty hackery
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
			StringBuilder input = new StringBuilder("|");
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

			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
				}
				input.Append('|');
			}

			return input.ToString();
		}

		public string GetControllersAsMnemonic()
		{
			if (ControlType == "Null Controller")
			{
				return "|.|";
			}
			else if (ControlType == "Atari 7800 ProLine Joystick Controller")
			{
				return GetA78ControllersAsMnemonic();
			}
			else if (ControlType == "SNES Controller")
			{
				return GetSNESControllersAsMnemonic();
			}
			else if (ControlType == "Commodore 64 Controller")
			{
				return GetC64ControllersAsMnemonic();
			}
			else if (ControlType == "GBA Controller")
			{
				return GetGBAControllersAsMnemonic();
			}
			else if (ControlType == "Dual Gameboy Controller")
			{
				return GetDualGameBoyControllerAsMnemonic();
			}
			else if (ControlType == "Nintento 64 Controller")
			{
				return GetN64ControllersAsMnemonic();
			}
			else if (ControlType == "Saturn Controller")
			{
				return GetSaturnControllersAsMnemonic();
			}
			else if (ControlType == "PSP Controller")
			{
				return "|.|"; // TODO
			}

			StringBuilder input = new StringBuilder("|");

			if (ControlType == "PC Engine Controller")
			{
				input.Append(".");
			}
			else if (ControlType == "Atari 2600 Basic Controller")
			{
				input.Append(IsBasePressed("Reset") ? "r" : ".");
				input.Append(IsBasePressed("Select") ? "s" : ".");
			}
			else if (ControlType == "NES Controller")
			{
				if (IsBasePressed("Power"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["Power"]);
				}
				else if (IsBasePressed("Reset"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["Reset"]);
				}
				else if (IsBasePressed("FDS Eject"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["FDS Eject"]);
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
					input.Append(MnemonicConstants.COMMANDS[ControlType]["VS Coin 1"]);
				}
				else if (IsBasePressed("VS Coin 2"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["VS Coin 2"]);
				}
				else
				{
					input.Append('.');
				}
			}
			else if (ControlType == "Genesis 3-Button Controller")
			{
				if (IsBasePressed("Power"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["Power"]);
				}
				else if (IsBasePressed("Reset"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["Reset"]);
				}
				else
				{
					input.Append('.');
				}
			}
			else if (ControlType == "Gameboy Controller")
			{
				input.Append(IsBasePressed("Power") ? MnemonicConstants.COMMANDS[ControlType]["Power"] : ".");
			}

			if (ControlType != "SMS Controller" && ControlType != "TI83 Controller" && ControlType != "ColecoVision Basic Controller")
			{
				input.Append("|");
			}

			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				string prefix = "";
				if (ControlType != "Gameboy Controller" && ControlType != "TI83 Controller")
				{
					prefix = "P" + player.ToString() + " ";
				}
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed(prefix + button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
				}
				input.Append("|");
			}
			if (ControlType == "SMS Controller")
			{
				foreach (string command in MnemonicConstants.COMMANDS[ControlType].Keys)
				{
					input.Append(IsBasePressed(command) ? MnemonicConstants.COMMANDS[ControlType][command] : ".");
				}
				input.Append("|");
			}
			if (ControlType == "TI83 Controller")
			{
				input.Append(".|"); //TODO: perhaps ON should go here?
			}
			return input.ToString();
		}

		#endregion
	}

	public class NewMnemonicsGenerator
	{
		public MnemonicLookupTable MnemonicLookup { get; private set; }
		public IController Source { get; set; }

		public List<string> ActivePlayers { get; set; }

		public NewMnemonicsGenerator(IController source)
		{
			MnemonicLookup = new MnemonicLookupTable();
			Source = source;
			ActivePlayers = MnemonicLookup[Global.Emulator.SystemId].Select(x => x.Name).ToList();
		}

		public bool IsEmpty
		{
			get
			{
				IEnumerable<MnemonicCollection> collections = MnemonicLookup[Global.Emulator.SystemId].Where(x => ActivePlayers.Contains(x.Name));

				foreach (var mc in collections)
				{
					foreach (var kvp in mc)
					{
						if (Source.IsPressed(kvp.Key))
						{
							return false;
						}
					}
				}

				return true;
			}
		}

		public string EmptyMnemonic
		{
			get
			{
				IEnumerable<MnemonicCollection> collections = MnemonicLookup[Global.Emulator.SystemId].Where(x => ActivePlayers.Contains(x.Name));
				StringBuilder sb = new StringBuilder();

				sb.Append('|');
				foreach (var mc in collections)
				{
					foreach (var kvp in mc)
					{
						sb.Append('.');
					}
					sb.Append('|');
				}

				return sb.ToString();
			}
		}

		public string GenerateMnemonicString(Dictionary<string, bool> buttons)
		{
			IEnumerable<MnemonicCollection> collections = MnemonicLookup[Global.Emulator.SystemId].Where(x => ActivePlayers.Contains(x.Name));
			StringBuilder sb = new StringBuilder();

			sb.Append('|');
			foreach (var mc in collections)
			{
				foreach (var kvp in mc)
				{
					if (buttons.ContainsKey(kvp.Key))
					{
						sb.Append(buttons[kvp.Key] ? kvp.Value : '.');
					}
				}

				sb.Append('|');
			}
			return sb.ToString();
		}

		public string MnemonicString
		{
			get
			{
				IEnumerable<MnemonicCollection> collections = MnemonicLookup[Global.Emulator.SystemId].Where(x => ActivePlayers.Contains(x.Name));
				StringBuilder sb = new StringBuilder();

				sb.Append('|');
				foreach (var mc in collections)
				{
					foreach(var kvp in mc)
					{
						sb.Append(Source.IsPressed(kvp.Key) ? kvp.Value : '.');
					}
					sb.Append('|');
				}

				return sb.ToString();
			}
		}

		public IEnumerable<char> Mnemonics
		{
			get
			{
				IEnumerable<MnemonicCollection> collections = MnemonicLookup[Global.Emulator.SystemId].Where(x => ActivePlayers.Contains(x.Name));

				List<char> mnemonics = new List<char>();
				foreach (var mc in collections)
				{
					mnemonics.AddRange(mc.Select(x => x.Value));
				}

				return mnemonics;
			}
		}

		public Dictionary<string, char> AvailableMnemonics
		{
			get
			{
				var buttons = new Dictionary<string, char>();
				IEnumerable<MnemonicCollection> collections = MnemonicLookup[Global.Emulator.SystemId].Where(x => ActivePlayers.Contains(x.Name));

				foreach (var mc in collections)
				{
					foreach (var kvp in mc)
					{
						buttons.Add(kvp.Key, kvp.Value);
					}
				}

				return buttons;
			}
		}

		public Dictionary<string, bool> GetBoolButtons()
		{
			var buttons = new Dictionary<string, bool>();
			IEnumerable<MnemonicCollection> collections = MnemonicLookup[Global.Emulator.SystemId].Where(x => ActivePlayers.Contains(x.Name));

			foreach (var mc in collections)
			{
				foreach (var kvp in mc)
				{
					buttons.Add(kvp.Key, Source.IsPressed(kvp.Key));
				}
			}

			return buttons;
		}
	}
}
