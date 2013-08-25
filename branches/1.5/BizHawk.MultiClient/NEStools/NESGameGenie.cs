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
		public int Address = -1;
		public int Value = -1;
		public int Compare = -1;
		private readonly Dictionary<char, int> GameGenieTable = new Dictionary<char, int>();

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
				Location = new Point(Global.Config.NESGGWndx, Global.Config.NESGGWndy);
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
				Value = 0;
				Address = 0x8000;
				int x;

				GameGenieTable.TryGetValue(code[0], out x);
				Value |= (x & 0x07);
				Value |= (x & 0x08) << 4;

				GameGenieTable.TryGetValue(code[1], out x);
				Value |= (x & 0x07) << 4;
				Address |= (x & 0x08) << 4;

				GameGenieTable.TryGetValue(code[2], out x);
				Address |= (x & 0x07) << 4;

				GameGenieTable.TryGetValue(code[3], out x);
				Address |= (x & 0x07) << 12;
				Address |= (x & 0x08);

				GameGenieTable.TryGetValue(code[4], out x);
				Address |= (x & 0x07);
				Address |= (x & 0x08) << 8;

				GameGenieTable.TryGetValue(code[5], out x);
				Address |= (x & 0x07) << 8;
				Value |= (x & 0x08);

				SetProperties();

			}
			else if (code.Length == 8)
			{
				//Char # |   1   |   2   |   3   |   4   |   5   |   6   |   7   |   8   |
				//Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
				//maps to|1|6|7|8|H|2|3|4|-|I|J|K|L|A|B|C|D|M|N|O|%|E|F|G|!|^|&|*|5|@|#|$|
				Value = 0;
				Address = 0x8000;
				Compare = 0;
				int x;

				GameGenieTable.TryGetValue(code[0], out x);
				Value |= (x & 0x07);
				Value |= (x & 0x08) << 4;

				GameGenieTable.TryGetValue(code[1], out x);
				Value |= (x & 0x07) << 4;
				Address |= (x & 0x08) << 4;

				GameGenieTable.TryGetValue(code[2], out x);
				Address |= (x & 0x07) << 4;

				GameGenieTable.TryGetValue(code[3], out x);
				Address |= (x & 0x07) << 12;
				Address |= (x & 0x08);

				GameGenieTable.TryGetValue(code[4], out x);
				Address |= (x & 0x07);
				Address |= (x & 0x08) << 8;

				GameGenieTable.TryGetValue(code[5], out x);
				Address |= (x & 0x07) << 8;
				Compare |= (x & 0x08);

				GameGenieTable.TryGetValue(code[6], out x);
				Compare |= (x & 0x07);
				Compare |= (x & 0x08) << 4;

				GameGenieTable.TryGetValue(code[7], out x);
				Compare |= (x & 0x07) << 4;
				Value |= (x & 0x08);
				SetProperties();
			}
		}

		private void SetProperties()
		{
			if (Address >= 0)
				AddressBox.Text = String.Format("{0:X4}", Address);
			else
				AddressBox.Text = "";

			if (Compare >= 0)
				CompareBox.Text = String.Format("{0:X2}", Compare);
			else
				CompareBox.Text = "";

			if (Value >= 0)
				ValueBox.Text = String.Format("{0:X2}", Value);

		}

		private void ClearProperties()
		{
			Address = -1;
			Value = -1;
			Compare = -1;
			AddressBox.Text = "";
			CompareBox.Text = "";
			ValueBox.Text = "";
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
					Address = a;
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
							Compare = c;
							EncodeGameGenie();
						}
					}
				}
				else
				{
					Compare = -1;
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
						Value = v;
						EncodeGameGenie();
					}
				}
			}

			TryEnableAddCheat();
		}

		private void EncodeGameGenie()
		{
			char[] letters = { 'A', 'P', 'Z', 'L', 'G', 'I', 'T', 'Y', 'E', 'O', 'X', 'U', 'K', 'S', 'V', 'N' };
			if (Address >= 0x8000)
				Address -= 0x8000;
			GameGenieCode.Text = "";
			byte[] num = { 0, 0, 0, 0, 0, 0, 0, 0 };
			num[0] = (byte)((Value & 7) + ((Value >> 4) & 8));
			num[1] = (byte)(((Value >> 4) & 7) + ((Address >> 4) & 8));
			num[2] = (byte)(((Address >> 4) & 7));
			num[3] = (byte)((Address >> 12) + (Address & 8));
			num[4] = (byte)((Address & 7) + ((Address >> 8) & 8));
			num[5] = (byte)(((Address >> 8) & 7));

			if (Compare < 0 || CompareBox.Text.Length == 0)
			{
				num[5] += (byte)(Value & 8);
				for (int x = 0; x < 6; x++)
					GameGenieCode.Text += letters[num[x]];
			}
			else
			{
				num[2] += 8;
				num[5] += (byte)(Compare & 8);
				num[6] = (byte)((Compare & 7) + ((Compare >> 4) & 8));
				num[7] = (byte)(((Compare >> 4) & 7) + (Value & 8));
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
				Cheat c = new Cheat { Name = GameGenieCode.Text };

				if (String.IsNullOrWhiteSpace(AddressBox.Text))
				{
					return;
				}
				else if (String.IsNullOrWhiteSpace(ValueBox.Text))
				{
					return;
				}
				c.Address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
				c.Value = byte.Parse(ValueBox.Text, NumberStyles.HexNumber);

				if (!String.IsNullOrWhiteSpace(CompareBox.Text))
				{
					try
					{
						c.Compare = byte.Parse(CompareBox.Text, NumberStyles.HexNumber);
					}
					catch
					{
						c.Compare = null;
					}
				}
				else
				{
					c.Compare = null;
				}

				c.Domain = Global.Emulator.MemoryDomains[1]; //System Bus only
				c.Enable();

				Global.MainForm.Cheats1.AddCheat(c);
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
