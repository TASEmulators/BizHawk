using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
	public partial class NESGameGenie : Form
	{
		private int? _address = null;
		private int? _value = null;
		private int? _compare = null;
		private readonly Dictionary<char, int> GameGenieTable = new Dictionary<char, int>();

		public int? Address { get { return _address; } }
		public int? Value { get { return _value; } }
		public int? Compare { get { return _compare; } }

		public NESGameGenie()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();

			GameGenieTable.Add('A', 0);     //0000
			GameGenieTable.Add('P', 1);     //0001
			GameGenieTable.Add('Z', 2);     //0010
			GameGenieTable.Add('L', 3);     //0011
			GameGenieTable.Add('G', 4);     //0100
			GameGenieTable.Add('I', 5);     //0101
			GameGenieTable.Add('T', 6);     //0110
			GameGenieTable.Add('Y', 7);     //0111
			GameGenieTable.Add('E', 8);     //1000
			GameGenieTable.Add('O', 9);     //1001
			GameGenieTable.Add('X', 10);    //1010
			GameGenieTable.Add('U', 11);    //1011
			GameGenieTable.Add('K', 12);    //1100
			GameGenieTable.Add('S', 13);    //1101
			GameGenieTable.Add('V', 14);    //1110
			GameGenieTable.Add('N', 15);    //1111
		}

		private void NESGameGenie_Load(object sender, EventArgs e)
		{
			AddCheat.Enabled = false;

			if (Global.Config.NESGGSaveWindowPosition && Global.Config.NESGGWndx >= 0 && Global.Config.NESGGWndy >= 0)
			{
				Location = new Point(Global.Config.NESGGWndx, Global.Config.NESGGWndy);
			}
		}

		private void SaveConfigSettings()
		{
			Global.Config.NESGGWndx = Location.X;
			Global.Config.NESGGWndy = Location.Y;
		}

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

		private void GameGenieCode_TextChanged(object sender, EventArgs e)
		{
			if (Encoding.Checked == false)
			{
				if (GameGenieCode.Text.Length == 6 || GameGenieCode.Text.Length == 8)
					DecodeGameGenieCode(GameGenieCode.Text);
				else
					ClearProperties();
			}
			TryEnableAddCheat();
		}

		private void Keypad_Click(object sender, EventArgs e)
		{
			if (GameGenieCode.Text.Length < 8)
			{
				string code = "";
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
				int a = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
				if (ValueBox.Text.Length > 0)
				{
					_address = a;
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

		private void TryEnableAddCheat()
		{
			if (AddressBox.Text.Length > 0 && ValueBox.Text.Length > 0 && GameGenieCode.Text.Length > 0)
				AddCheat.Enabled = true;
			else
				AddCheat.Enabled = false;
		}

		private void ValueBox_TextChanged(object sender, EventArgs e)
		{
			if (Encoding.Checked && ValueBox.Text.Length > 0)
			{
				int v = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
				if (v > 0 && v < 0x100)
				{
					if (AddressBox.Text.Length > 0)
					{
						_value = v;
						EncodeGameGenie();
					}
				}
			}

			TryEnableAddCheat();
		}

		private void EncodeGameGenie()
		{
			char[] letters = { 'A', 'P', 'Z', 'L', 'G', 'I', 'T', 'Y', 'E', 'O', 'X', 'U', 'K', 'S', 'V', 'N' };
			if (_address >= 0x8000)
				_address -= 0x8000;
			GameGenieCode.Text = "";
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
				for (int x = 0; x < 8; x++)
					GameGenieCode.Text += letters[num[x]];
			}

		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			ClearProperties();
			GameGenieCode.Text = "";
			Encoding.Checked = false;
		}

		private void AddCheat_Click(object sender, EventArgs e)
		{
			AddCheatClick();
		}

		private void AddCheatClick()
		{
			if (Global.Emulator is NES)
			{
				if (String.IsNullOrWhiteSpace(AddressBox.Text) || (String.IsNullOrWhiteSpace(ValueBox.Text)))
				{
					return;
				}

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

				Global.MainForm.Cheats_UpdateValues();
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.NESGGSaveWindowPosition ^= true;
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.NESGGAutoload ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = Global.Config.NESGGAutoload;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.NESGGSaveWindowPosition;
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
	}
}
