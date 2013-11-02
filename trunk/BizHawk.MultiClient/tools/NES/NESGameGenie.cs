using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;

using BizHawk.Client.Common;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
	public partial class NESGameGenie : Form
	{
		private int? _address = null;
		private int? _value = null;
		private int? _compare = null;
		private readonly Dictionary<char, int> GameGenieTable = new Dictionary<char, int>
		{
			{ 'A', 0 }, //0000
			{ 'P', 1 }, //0001
			{ 'Z', 2 }, //0010
			{ 'L', 3 }, //0011
			{ 'G', 4 }, //0100
			{ 'I', 5 }, //0101
			{ 'T', 6 }, //0110
			{ 'Y', 7 }, //0111
			{ 'E', 8 }, //1000
			{ 'O', 9 }, //1001
			{ 'X', 10}, //1010
			{ 'U', 11}, //1011
			{ 'K', 12}, //1100
			{ 'S', 13}, //1101
			{ 'V', 14}, //1110
			{ 'N', 15}, //1111
		};

		public int? Address { get { return _address; } }
		public int? Value { get { return _value; } }
		public int? Compare { get { return _compare; } }

		public NESGameGenie()
		{
			InitializeComponent();
			Closing += (o, e) =>
				{
					Global.Config.NESGGWndx = Location.X;
					Global.Config.NESGGWndy = Location.Y;
				};
			AddressBox.SetHexProperties(0x10000);
			ValueBox.SetHexProperties(0x100);
			CompareBox.SetHexProperties(0x100);
		}

		private void NESGameGenie_Load(object sender, EventArgs e)
		{
			AddCheat.Enabled = false;

			if (Global.Config.NESGGSaveWindowPosition && Global.Config.NESGGWndx >= 0 && Global.Config.NESGGWndy >= 0)
			{
				Location = new Point(Global.Config.NESGGWndx, Global.Config.NESGGWndy);
			}
		}

		public void DecodeGameGenieCode(string code)
		{
			//char 3 bit 3 denotes the code length.
			if (code.Length == 6)
			{
				//Char # |   1   |   2   |   3   |   4   |   5   |   6   |
				//Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
				//maps to|1|6|7|8|H|2|3|4|-|I|J|K|L|A|B|C|D|M|N|O|5|E|F|G|
				_value = 0;
				_address = 0x8000;
				int x;

				GameGenieTable.TryGetValue(code[0], out x);
				_value |= (x & 0x07);
				_value |= (x & 0x08) << 4;

				GameGenieTable.TryGetValue(code[1], out x);
				_value |= (x & 0x07) << 4;
				_address |= (x & 0x08) << 4;

				GameGenieTable.TryGetValue(code[2], out x);
				_address |= (x & 0x07) << 4;

				GameGenieTable.TryGetValue(code[3], out x);
				_address |= (x & 0x07) << 12;
				_address |= (x & 0x08);

				GameGenieTable.TryGetValue(code[4], out x);
				_address |= (x & 0x07);
				_address |= (x & 0x08) << 8;

				GameGenieTable.TryGetValue(code[5], out x);
				_address |= (x & 0x07) << 8;
				_value |= (x & 0x08);

				SetProperties();
			}
			else if (code.Length == 8)
			{
				//Char # |   1   |   2   |   3   |   4   |   5   |   6   |   7   |   8   |
				//Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
				//maps to|1|6|7|8|H|2|3|4|-|I|J|K|L|A|B|C|D|M|N|O|%|E|F|G|!|^|&|*|5|@|#|$|
				_value = 0;
				_address = 0x8000;
				_compare = 0;
				int x;

				GameGenieTable.TryGetValue(code[0], out x);
				_value |= (x & 0x07);
				_value |= (x & 0x08) << 4;

				GameGenieTable.TryGetValue(code[1], out x);
				_value |= (x & 0x07) << 4;
				_address |= (x & 0x08) << 4;

				GameGenieTable.TryGetValue(code[2], out x);
				_address |= (x & 0x07) << 4;

				GameGenieTable.TryGetValue(code[3], out x);
				_address |= (x & 0x07) << 12;
				_address |= (x & 0x08);

				GameGenieTable.TryGetValue(code[4], out x);
				_address |= (x & 0x07);
				_address |= (x & 0x08) << 8;

				GameGenieTable.TryGetValue(code[5], out x);
				_address |= (x & 0x07) << 8;
				_compare |= (x & 0x08);

				GameGenieTable.TryGetValue(code[6], out x);
				_compare |= (x & 0x07);
				_compare |= (x & 0x08) << 4;

				GameGenieTable.TryGetValue(code[7], out x);
				_compare |= (x & 0x07) << 4;
				_value |= (x & 0x08);
				SetProperties();
			}
		}

		private void SetProperties()
		{
			if (_address.HasValue)
			{
				AddressBox.SetFromRawInt(_address.Value);
			}
			else
			{
				AddressBox.ResetText();
			}

			if (_compare.HasValue)
			{
				CompareBox.SetFromRawInt(_compare.Value);
			}
			else
			{
				CompareBox.ResetText();
			}

			if (_value.HasValue)
			{
				ValueBox.SetFromRawInt(_value.Value);
			}
			else
			{
				ValueBox.ResetText();
			}
		}

		private void ClearProperties()
		{
			_address = _value = _compare = null;

			AddressBox.Text =
				CompareBox.Text =
				ValueBox.Text =
				String.Empty;

			AddCheat.Enabled = false;
		}

		private void TryEnableAddCheat()
		{
			AddCheat.Enabled = !String.IsNullOrWhiteSpace(AddressBox.Text)
				&& !String.IsNullOrWhiteSpace(ValueBox.Text)
				&& !String.IsNullOrWhiteSpace(GameGenieCode.Text);
		}

		private void EncodeGameGenie()
		{
			_address = AddressBox.ToRawInt();
			_value = ValueBox.ToRawInt();
			_compare = CompareBox.ToRawInt();

			char[] letters = { 'A', 'P', 'Z', 'L', 'G', 'I', 'T', 'Y', 'E', 'O', 'X', 'U', 'K', 'S', 'V', 'N' };
			if (_address >= 0x8000)
				_address -= 0x8000;
			GameGenieCode.Text = String.Empty;
			byte[] num = { 0, 0, 0, 0, 0, 0, 0, 0 };
			num[0] = (byte)((_value & 7) + ((_value >> 4) & 8));
			num[1] = (byte)(((_value >> 4) & 7) + ((_address >> 4) & 8));
			num[2] = (byte)(((_address >> 4) & 7));
			num[3] = (byte)((_address >> 12) + (_address & 8));
			num[4] = (byte)((_address & 7) + ((_address >> 8) & 8));
			num[5] = (byte)(((_address >> 8) & 7));

			if (_compare < 0 || CompareBox.Text.Length == 0)
			{
				num[5] += (byte)(_value & 8);
				for (int x = 0; x < 6; x++)
					GameGenieCode.Text += letters[num[x]];
			}
			else
			{
				num[2] += 8;
				num[5] += (byte)(_compare & 8);
				num[6] = (byte)((_compare & 7) + ((_compare >> 4) & 8));
				num[7] = (byte)(((_compare >> 4) & 7) + (_value & 8));
				for (int i = 0; i < 8; i++)
				{
					GameGenieCode.Text += letters[num[i]];
				}
			}
		}

		private void AddCheatClick()
		{
			if (!String.IsNullOrWhiteSpace(AddressBox.Text) && !String.IsNullOrWhiteSpace(ValueBox.Text))
			{
				Watch watch = Watch.GenerateWatch(
					Global.Emulator.MemoryDomains[1], /*System Bus*/
					AddressBox.ToRawInt(),
					Watch.WatchSize.Byte,
					Watch.DisplayType.Hex,
					GameGenieCode.Text,
					false);

				int? compare = null;
				if (!String.IsNullOrWhiteSpace(CompareBox.Text))
				{
					compare = CompareBox.ToRawInt();
				}

				Global.CheatList.Add(new Cheat(
					watch,
					ValueBox.ToRawInt(),
					compare,
					enabled: true));

				GlobalWinF.MainForm.Cheats_UpdateValues();
			}
		}

		#region Events

		#region File Menu

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadMenuItem.Checked = Global.Config.NESGGAutoload;
			SaveWindowPositionMenuItem.Checked = Global.Config.NESGGSaveWindowPosition;
		}

		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.NESGGAutoload ^= true;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.NESGGSaveWindowPosition ^= true;
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion
		
		#region Control Events

		private void GameGenieCode_KeyPress(object sender, KeyPressEventArgs e)
		{
			//Make uppercase
			if (e.KeyChar >= 97 && e.KeyChar < 123)
				e.KeyChar -= (char)32;

			if (!(GameGenieTable.ContainsKey(e.KeyChar)))
			{
				if (e.KeyChar != (char)Keys.Back || e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
				{
					e.Handled = true;
				}
			}
			else
			{
				Encoding.Checked = false;
			}
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			ClearProperties();
			GameGenieCode.Text = String.Empty;
			Encoding.Checked = false;
		}

		private void AddCheat_Click(object sender, EventArgs e)
		{
			AddCheatClick();
		}

		private void GameGenieCode_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				if (AddCheat.Enabled)
				{
					AddCheatClick();
				}
			}
		}

		private void ValueBox_TextChanged(object sender, EventArgs e)
		{
			if (Encoding.Checked && !String.IsNullOrWhiteSpace(ValueBox.Text))
			{
				int val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
				if (val > 0 && val < 0x100)
				{
					if (!String.IsNullOrWhiteSpace(AddressBox.Text))
					{
						_value = val;
						EncodeGameGenie();
					}
				}
			}

			TryEnableAddCheat();
		}

		private void Keypad_Click(object sender, EventArgs e)
		{
			if (GameGenieCode.Text.Length < 8)
			{
				string code = String.Empty;
				if (sender == A) code = "A";
				if (sender == P) code += "P";
				if (sender == Z) code += "Z";
				if (sender == L) code += "L";
				if (sender == G) code += "G";
				if (sender == I) code += "I";
				if (sender == T) code += "T";
				if (sender == Y) code += "Y";
				if (sender == E) code += "E";
				if (sender == O) code += "O";
				if (sender == X) code += "X";
				if (sender == U) code += "U";
				if (sender == K) code += "K";
				if (sender == S) code += "S";
				if (sender == V) code += "V";
				if (sender == N) code += "N";

				int x = GameGenieCode.SelectionStart;
				GameGenieCode.Text = GameGenieCode.Text.Insert(x, code);
				GameGenieCode.SelectionStart = x + 1;
				Encoding.Checked = false;
			}
		}

		private void AddressBox_TextChanged(object sender, EventArgs e)
		{
			if (Encoding.Checked && AddressBox.Text.Length > 0)
			{
				int _address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
				if (!String.IsNullOrEmpty(ValueBox.Text))
				{
					EncodeGameGenie();
				}
			}
			TryEnableAddCheat();
		}

		private void CompareBox_TextChanged(object sender, EventArgs e)
		{
			if (Encoding.Checked)
			{
				if (CompareBox.Text.Length > 0)
				{
					int c = int.Parse(CompareBox.Text, NumberStyles.HexNumber);
					if (c > 0 && c < 0x100)
					{
						if (ValueBox.Text.Length > 0 && AddressBox.Text.Length > 0)
						{
							_compare = c;
							EncodeGameGenie();
						}
					}
				}
				else
				{
					_compare = -1;
					EncodeGameGenie();
				}
			}
			TryEnableAddCheat();
		}

		private void GameGenieCode_TextChanged(object sender, EventArgs e)
		{
			if (Encoding.Checked == false)
			{
				if (GameGenieCode.Text.Length == 6 || GameGenieCode.Text.Length == 8)
				{
					DecodeGameGenieCode(GameGenieCode.Text);
				}
				else
				{
					ClearProperties();
				}
			}
			TryEnableAddCheat();
		}

		#endregion

		#endregion
	}
}
