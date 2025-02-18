using System.Collections.Generic;
using System.Linq;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal class BkmControllerAdapter : IController
	{
		public BkmControllerAdapter(string systemId)
		{
			Definition = BkmMnemonicConstants.ControllerDefinitions[systemId];
			Definition.BuildMnemonicsCache(systemId);
		}

		public ControllerDefinition Definition { get; set; }

		public bool IsPressed(string button)
			=> _myBoolButtons.GetValueOrDefault(button);

		public int AxisValue(string name)
			=> _myAxisControls.GetValueOrDefault(name);

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => throw new NotSupportedException();

		public void SetHapticChannelStrength(string name, int strength) => throw new NotSupportedException();

		private void SetFromMnemonic(ReadOnlySpan<char> mnemonic)
		{
			if (mnemonic.IsEmpty) return;
			var iterator = 0;

			foreach ((string buttonName, AxisSpec? axisSpec) in Definition.ControlsOrdered.Skip(1).SelectMany(static x => x))
			{
				while (mnemonic[iterator] == '|') iterator++;

				if (axisSpec.HasValue)
				{
					var separatorIndex = iterator + mnemonic[iterator..].IndexOfAny(',', '|');
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
					var val = int.Parse(mnemonic[iterator..separatorIndex]);
#else
					var axisValueString = mnemonic[iterator..separatorIndex].ToString();
					var val = int.Parse(axisValueString);
#endif
					_myAxisControls[buttonName] = val;

					iterator = separatorIndex + 1;
				}
				else
				{
					_myBoolButtons[buttonName] = mnemonic[iterator] != '.';
					iterator++;
				}
			}
		}

		/// <summary>
		/// latches all buttons from the supplied mnemonic string
		/// </summary>
		public void SetControllersAsMnemonic(string mnemonic)
		{
			// _myBoolButtons.Clear();

			switch (Definition.Name)
			{
				case "Gameboy Controller":
					Force("Power", mnemonic[1] == 'P');
					SetFromMnemonic(mnemonic.AsSpan(3));
					break;
				case "GBA Controller":
					Force("Power", mnemonic[1] == 'P');
					SetFromMnemonic(mnemonic.AsSpan(3));
					break;
				case "GPGX Genesis Controller":
					Force("Power", mnemonic[1] == 'P');
					Force("Reset", mnemonic[1] == 'r');
					SetFromMnemonic(mnemonic.AsSpan(3));
					break;
				case "NES Controller":
					Force("Power", mnemonic[1] == 'P');
					Force("Reset", mnemonic[1] == 'r');
					Force("FDS Eject", mnemonic[1] == 'E');
					Force("FDS Insert 0", mnemonic[1] == '0');
					Force("FDS Insert 1", mnemonic[1] == '1');
					Force("FDS Insert 2", mnemonic[1] == '2');
					Force("FDS Insert 3", mnemonic[1] == '3');
					Force("VS Coin 1", mnemonic[1] == 'c');
					Force("VS Coin 2", mnemonic[1] == 'C');
					SetFromMnemonic(mnemonic.AsSpan(3));
					break;
				case "SNES Controller":
					Force("Power", mnemonic[1] == 'P');
					Force("Reset", mnemonic[1] == 'r');
					SetFromMnemonic(mnemonic.AsSpan(3));
					break;
				case "PC Engine Controller":
					SetFromMnemonic(mnemonic.AsSpan(3));
					break;
				case "SMS Controller":
					SetFromMnemonic(mnemonic.AsSpan(1));
					Force("Pause", mnemonic[^3] == 'p');
					Force("Reset", mnemonic[^2] == 'r');
					break;
				case "TI83 Controller":
					SetFromMnemonic(mnemonic.AsSpan(1));
					break;
				case "Atari 2600 Basic Controller":
					Force("Reset", mnemonic[1] == 'r');
					Force("Select", mnemonic[2] == 's');
					SetFromMnemonic(mnemonic.AsSpan(4));
					break;
				case "Atari 7800 ProLine Joystick Controller":
					Force("Power", mnemonic[1] == 'P');
					Force("Reset", mnemonic[2] == 'r');
					Force("Select", mnemonic[3] == 's');
					Force("Pause", mnemonic[4] == 'p');
					SetFromMnemonic(mnemonic.AsSpan(6));
					break;
				case "Commodore 64 Controller":
					SetFromMnemonic(mnemonic.AsSpan(1));
					break;
				case "ColecoVision Basic Controller":
					SetFromMnemonic(mnemonic.AsSpan(1));
					break;
				case "Nintento 64 Controller":
					Force("Power", mnemonic[1] == 'P');
					Force("Reset", mnemonic[1] == 'r');
					SetFromMnemonic(mnemonic.AsSpan(3));
					break;
				case "Saturn Controller":
					Force("Power", mnemonic[1] == 'P');
					Force("Reset", mnemonic[1] == 'r');
					SetFromMnemonic(mnemonic.AsSpan(3));
					break;
				case "Dual Gameboy Controller":
					SetFromMnemonic(mnemonic.AsSpan());
					break;
				case "WonderSwan Controller":
					SetFromMnemonic(mnemonic.AsSpan(1));
					Force("Power", mnemonic[^3] == 'P');
					Force("Rotate", mnemonic[^2] == 'R');
					break;
			}
		}

		private readonly Dictionary<string, int> _myAxisControls = new();

		private readonly Dictionary<string, bool> _myBoolButtons = new();

		private void Force(string button, bool state)
		{
			_myBoolButtons[button] = state;
		}
	}
}
