using System.Collections.Generic;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal class BkmControllerAdapter : IController
	{
		public BkmControllerAdapter(ControllerDefinition definition, string systemId)
		{
			// We do need to map the definition name to the legacy
			// controller names that were used back in the bkm days
			var name = systemId switch
			{
				"Lynx" => "Lynx Controller",
				"SNES" => "SNES Controller",
				"C64" => "Commodore 64 Controller",
				"GBA" => "GBA Controller",
				"A78" => "Atari 7800 ProLine Joystick Controller",
				"DGB" => "Dual Gameboy Controller",
				"WSWAN" => "WonderSwan Controller",
				"N64" => "Nintendo 64 Controller",
				"SAT" => "Saturn Controller",
				"GEN" => "GPGX Genesis Controller",
				"NES" => "NES Controller",
				"GB" => "Gameboy Controller",
				"A26" => "Atari 2600 Basic Controller",
				"TI83" => "TI83 Controller",
				"Coleco" => "ColecoVision Basic Controller",
				"SMS Controller" => "SMS",
				_ => "Null Controller",
			};
			Definition = new(copyFrom: definition, withName: name);
			Definition.BuildMnemonicsCache(systemId); //TODO these aren't the same...
		}

		public ControllerDefinition Definition { get; set; }

		public bool IsPressed(string button)
			=> _myBoolButtons.GetValueOrDefault(button);

		public int AxisValue(string name)
			=> _myAxisControls.GetValueOrDefault(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => throw new NotImplementedException(); // no idea --yoshi

		public void SetHapticChannelStrength(string name, int strength) => throw new NotImplementedException(); // no idea --yoshi

		/// <summary>
		/// latches all buttons from the supplied mnemonic string
		/// </summary>
		public void SetControllersAsMnemonic(string mnemonic)
		{
			switch (ControlType)
			{
				case "Null Controller":
					return;
				case "Lynx Controller":
					SetLynxControllersAsMnemonic(mnemonic);
					return;
				case "SNES Controller":
					SetSNESControllersAsMnemonic(mnemonic);
					return;
				case "Commodore 64 Controller":
					SetC64ControllersAsMnemonic(mnemonic);
					return;
				case "GBA Controller":
					SetGBAControllersAsMnemonic(mnemonic);
					return;
				case "Atari 7800 ProLine Joystick Controller":
					SetAtari7800AsMnemonic(mnemonic);
					return;
				case "Dual Gameboy Controller":
					SetDualGameBoyControllerAsMnemonic(mnemonic);
					return;
				case "WonderSwan Controller":
					SetWonderSwanControllerAsMnemonic(mnemonic);
					return;
				case "Nintendo 64 Controller":
					SetN64ControllersAsMnemonic(mnemonic);
					return;
				case "Saturn Controller":
					SetSaturnControllersAsMnemonic(mnemonic);
					return;
				case "GPGX Genesis Controller":
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
			}

			var c = new MnemonicChecker(mnemonic);

			_myBoolButtons.Clear();

			int start = 3;
			if (ControlType == "NES Controller")
			{
				if (mnemonic.Length < 2)
				{
					return;
				}

				switch (mnemonic[1])
				{
					case 'P':
						Force("Power", true);
						break;
					case 'E':
						Force("FDS Eject", true);
						break;
					case '0':
						Force("FDS Insert 0", true);
						break;
					case '1':
						Force("FDS Insert 1", true);
						break;
					case '2':
						Force("FDS Insert 2", true);
						break;
					case '3':
						Force("FDS Insert 3", true);
						break;
					case 'c':
						Force("VS Coin 1", true);
						break;
					case 'C':
						Force("VS Coin 2", true);
						break;
					default:
					{
						if (mnemonic[1] != '.')
						{
							Force("Reset", true);
						}

						break;
					}
				}
			}

			if (ControlType == "Gameboy Controller")
			{
				if (mnemonic.Length < 2)
				{
					return;
				}

				Force("Power", mnemonic[1] != '.');
			}

			if (ControlType == "SMS Controller" || ControlType == "TI83 Controller" || ControlType == "ColecoVision Basic Controller")
			{
				start = 1;
			}

			if (ControlType == "Atari 2600 Basic Controller")
			{
				if (mnemonic.Length < 2)
				{
					return;
				}

				Force("Reset", mnemonic[1] != '.' && mnemonic[1] != '0');
				Force("Select", mnemonic[2] != '.' && mnemonic[2] != '0');
				start = 4;
			}

			for (int player = 1; player <= BkmMnemonicConstants.Players[ControlType]; player++)
			{
				int srcIndex = (player - 1) * (BkmMnemonicConstants.Buttons[ControlType].Count + 1);
				int ctr = start;
				if (mnemonic.Length < srcIndex + ctr + BkmMnemonicConstants.Buttons[ControlType].Count - 1)
				{
					return;
				}

				string prefix = "";
				if (ControlType != "Gameboy Controller" && ControlType != "TI83 Controller")
				{
					prefix = $"P{player} ";
				}

				foreach (string button in BkmMnemonicConstants.Buttons[ControlType].Keys)
				{
					Force(prefix + button, c[srcIndex + ctr++]);
				}
			}

			if (ControlType == "SMS Controller")
			{
				int srcIndex = BkmMnemonicConstants.Players[ControlType] * (BkmMnemonicConstants.Buttons[ControlType].Count + 1);
				int ctr = start;
				foreach (var command in BkmMnemonicConstants.Commands[ControlType].Keys)
				{
					Force(command, c[srcIndex + ctr++]);
				}
			}
		}

		private readonly Dictionary<string, int> _myAxisControls = new();

		private readonly Dictionary<string, bool> _myBoolButtons = new();

		private bool IsGenesis6Button() => Definition.BoolButtons.Contains("P1 X");

		private void Force(string button, bool state)
		{
			_myBoolButtons[button] = state;
		}

		private void Force(string name, int state)
		{
			_myAxisControls[name] = state;
		}

		private string ControlType => Definition.Name;

		private void SetGBAControllersAsMnemonic(string mnemonic)
		{
			var c = new MnemonicChecker(mnemonic);
			_myBoolButtons.Clear();
			if (mnemonic.Length < 2)
			{
				return;
			}

			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}

			int start = 3;
			foreach (string button in BkmMnemonicConstants.Buttons[ControlType].Keys)
			{
				Force(button, c[start++]);
			}
		}

		private void SetGenesis6ControllersAsMnemonic(string mnemonic)
		{
			var c = new MnemonicChecker(mnemonic);
			_myBoolButtons.Clear();

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

			for (int player = 1; player <= BkmMnemonicConstants.Players[ControlType]; player++)
			{
				int srcIndex = (player - 1) * (BkmMnemonicConstants.Buttons[ControlType].Count + 1);

				if (mnemonic.Length < srcIndex + 3 + BkmMnemonicConstants.Buttons[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (string button in BkmMnemonicConstants.Buttons[ControlType].Keys)
				{
					Force($"P{player} {button}", c[srcIndex + start++]);
				}
			}
		}

		private void SetGenesis3ControllersAsMnemonic(string mnemonic)
		{
			var c = new MnemonicChecker(mnemonic);
			_myBoolButtons.Clear();

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

			for (int player = 1; player <= BkmMnemonicConstants.Players[ControlType]; player++)
			{
				int srcIndex = (player - 1) * (BkmMnemonicConstants.Buttons["GPGX 3-Button Controller"].Count + 1);

				if (mnemonic.Length < srcIndex + 3 + BkmMnemonicConstants.Buttons["GPGX 3-Button Controller"].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (string button in BkmMnemonicConstants.Buttons["GPGX 3-Button Controller"].Keys)
				{
					Force($"P{player} {button}", c[srcIndex + start++]);
				}
			}
		}

		private void SetSNESControllersAsMnemonic(string mnemonic)
		{
			var c = new MnemonicChecker(mnemonic);
			_myBoolButtons.Clear();

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

			for (int player = 1; player <= BkmMnemonicConstants.Players[ControlType]; player++)
			{
				int srcIndex = (player - 1) * (BkmMnemonicConstants.Buttons[ControlType].Count + 1);

				if (mnemonic.Length < srcIndex + 3 + BkmMnemonicConstants.Buttons[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (var button in BkmMnemonicConstants.Buttons[ControlType].Keys)
				{
					Force($"P{player} {button}", c[srcIndex + start++]);
				}
			}
		}

		private void SetLynxControllersAsMnemonic(string mnemonic)
		{
			var c = new MnemonicChecker(mnemonic);
			_myBoolButtons.Clear();

			if (mnemonic.Length < 2)
			{
				return;
			}

			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}

			for (int player = 1; player <= BkmMnemonicConstants.Players[ControlType]; player++)
			{
				int srcIndex = (player - 1) * (BkmMnemonicConstants.Buttons[ControlType].Count + 1);

				if (mnemonic.Length < srcIndex + 3 + BkmMnemonicConstants.Buttons[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (var button in BkmMnemonicConstants.Buttons[ControlType].Keys)
				{
					Force(button, c[srcIndex + start++]);
				}
			}
		}

		private void SetN64ControllersAsMnemonic(string mnemonic)
		{
			var c = new MnemonicChecker(mnemonic);
			_myBoolButtons.Clear();

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

			for (int player = 1; player <= BkmMnemonicConstants.Players[ControlType]; player++)
			{
				int srcIndex = (player - 1) * (BkmMnemonicConstants.Buttons[ControlType].Count + (BkmMnemonicConstants.Analogs[ControlType].Count * 4) + 1 + 1);

				if (mnemonic.Length < srcIndex + 3 + BkmMnemonicConstants.Buttons[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (string button in BkmMnemonicConstants.Buttons[ControlType].Keys)
				{
					Force($"P{player} {button}", c[srcIndex + start++]);
				}

				foreach (string name in BkmMnemonicConstants.Analogs[ControlType].Keys)
				{
					Force($"P{player} {name}", int.Parse(mnemonic.Substring(srcIndex + start, 4)));
					start += 5;
				}
			}
		}

		private void SetSaturnControllersAsMnemonic(string mnemonic)
		{
			var c = new MnemonicChecker(mnemonic);
			_myBoolButtons.Clear();

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

			for (int player = 1; player <= BkmMnemonicConstants.Players[ControlType]; player++)
			{
				int srcIndex = (player - 1) * (BkmMnemonicConstants.Buttons[ControlType].Count + 1);

				if (mnemonic.Length < srcIndex + 3 + BkmMnemonicConstants.Buttons[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (string button in BkmMnemonicConstants.Buttons[ControlType].Keys)
				{
					Force($"P{player} {button}", c[srcIndex + start++]);
				}
			}
		}

		private void SetAtari7800AsMnemonic(string mnemonic)
		{
			var c = new MnemonicChecker(mnemonic);
			_myBoolButtons.Clear();

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

			for (int player = 1; player <= BkmMnemonicConstants.Players[ControlType]; player++)
			{
				int srcIndex = (player - 1) * (BkmMnemonicConstants.Buttons[ControlType].Count + 1);
				int start = 6;
				if (mnemonic.Length < srcIndex + start + BkmMnemonicConstants.Buttons[ControlType].Count)
				{
					return;
				}

				foreach (string button in BkmMnemonicConstants.Buttons[ControlType].Keys)
				{
					Force($"P{player} {button}", c[srcIndex + start++]);
				}
			}
		}

		private void SetDualGameBoyControllerAsMnemonic(string mnemonic)
		{
			var checker = new MnemonicChecker(mnemonic);
			_myBoolButtons.Clear();
			for (int i = 0; i < BkmMnemonicConstants.DgbMnemonic.Length; i++)
			{
				var t = BkmMnemonicConstants.DgbMnemonic[i];
				if (t.Item1 != null)
				{
					Force(t.Item1, checker[i]);
				}
			}
		}

		private void SetWonderSwanControllerAsMnemonic(string mnemonic)
		{
			var checker = new MnemonicChecker(mnemonic);
			_myBoolButtons.Clear();
			for (int i = 0; i < BkmMnemonicConstants.WsMnemonic.Length; i++)
			{
				var t = BkmMnemonicConstants.WsMnemonic[i];
				if (t.Item1 != null)
				{
					Force(t.Item1, checker[i]);
				}
			}
		}

		private void SetC64ControllersAsMnemonic(string mnemonic)
		{
			var c = new MnemonicChecker(mnemonic);
			_myBoolButtons.Clear();

			for (int player = 1; player <= BkmMnemonicConstants.Players[ControlType]; player++)
			{
				int srcIndex = (player - 1) * (BkmMnemonicConstants.Buttons[ControlType].Count + 1);

				if (mnemonic.Length < srcIndex + 1 + BkmMnemonicConstants.Buttons[ControlType].Count - 1)
				{
					return;
				}

				int start = 1;
				foreach (var button in BkmMnemonicConstants.Buttons[ControlType].Keys)
				{
					Force($"P{player} {button}", c[srcIndex + start++]);
				}
			}

			int startKey = 13;
			foreach (string button in BkmMnemonicConstants.Buttons["Commodore 64 Keyboard"].Keys)
			{
				Force(button, c[startKey++]);
			}
		}

		private sealed class MnemonicChecker
		{
			private readonly string _mnemonic;

			public MnemonicChecker(string mnemonic)
			{
				_mnemonic = mnemonic;
			}

			public bool this[int c] => !string.IsNullOrEmpty(_mnemonic) && _mnemonic[c] != '.';
		}
	}
}
