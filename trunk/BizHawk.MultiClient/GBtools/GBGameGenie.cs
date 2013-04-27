using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BizHawk.MultiClient
{
	public partial class GBGameGenie : Form
	{
		private readonly Dictionary<char, int> GameGenieTable = new Dictionary<char, int>();
		private bool Processing = false;

        public GBGameGenie()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();

			GameGenieTable.Add('0', 0);     //0000
			GameGenieTable.Add('1', 1);     //0001
			GameGenieTable.Add('2', 2);     //0010
			GameGenieTable.Add('3', 3);     //0011
			GameGenieTable.Add('4', 4);     //0100
			GameGenieTable.Add('5', 5);     //0101
			GameGenieTable.Add('6', 6);     //0110
			GameGenieTable.Add('7', 7);     //0111
			GameGenieTable.Add('8', 8);     //1000
			GameGenieTable.Add('9', 9);     //1001
			GameGenieTable.Add('A', 10);    //1010
			GameGenieTable.Add('B', 11);    //1011
			GameGenieTable.Add('C', 12);    //1100
			GameGenieTable.Add('D', 13);    //1101
			GameGenieTable.Add('E', 14);    //1110
			GameGenieTable.Add('F', 15);    //1111
		}

		public void GBGGDecode(string code, ref int val, ref int add, ref int cmp)
		{

			//No cypher on value
			//Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |   8   |
			//Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			//maps to|      Value    |A|B|C|D|E|F|G|H|I|J|K|L|XOR 0xF|a|b|c|c|NotUsed|e|f|g|h|
			//proper |      Value    |XOR 0xF|A|B|C|D|E|F|G|H|I|J|K|L|g|h|a|b|Nothing|c|d|e|f|

			int x;
			// Getting Value
			if (code.Length > 0)
			{
				GameGenieTable.TryGetValue(code[0], out x);
				val = x << 4;
			}

			if (code.Length > 1)
			{
				GameGenieTable.TryGetValue(code[1], out x);
				val |= x;
			}
			//Address
			if (code.Length > 2)
			{
				GameGenieTable.TryGetValue(code[2], out x);
				add = (x << 8);
			}
			else
				add = -1;

			if (code.Length > 3)
			{
				GameGenieTable.TryGetValue(code[3], out x);
				add |= (x << 4);
			}

			if (code.Length > 4)
			{
				GameGenieTable.TryGetValue(code[4], out x);
				add |= x;
			}

			if (code.Length > 5)
			{
				GameGenieTable.TryGetValue(code[5], out x);
				add |= ((x ^ 0xF) << 12);
			}
			// compare need to be full
			if (code.Length > 8)
			{
				int comp = 0;
				GameGenieTable.TryGetValue(code[6], out x);
				comp = (x << 2);
				// 8th character ignored
				GameGenieTable.TryGetValue(code[8], out x);
				comp |= ((x & 0xC) >> 2);
				comp |= ((x & 0x3) << 6);
				cmp = comp ^ 0xBA;
			}
			else
				cmp = -1;

        }
		
		private string GBGGEncode(int val, int add, int cmp)
		{
			char[] letters = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
			string code = "";
			int[] num = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			num[0] = (val & 0xF0) >> 4;
			num[1] = val & 0x0F;

			num[2] = (add & 0x0F00) >> 8;
			num[3] = (add & 0x00F0) >> 4;
			num[4] = (add & 0x000F);
			num[5] = (((add & 0xF000) >> 12) ^ 0xF);
			if (cmp > -1)
			{

				int xoredcomp = ((cmp & 0xFF) ^ 0xBA);
				num[6] = ((xoredcomp & 0x30) >> 2);
				num[6] |= ((xoredcomp & 0x0C) >> 2);
				// 8th char has no real use (its value is not reflected in the address:value:compare
				// probably a protection to stop people making code up, xor the 7th digit with 0x8
				// to get back what the original code had (Might be more to it)
				num[7] = num[6] ^ 8;
				num[8] = ((xoredcomp & 0xC0) >> 6);
				num[8] |= ((xoredcomp & 0x03) << 2);
				for (int x = 0; x < 9; x++)
					code += letters[num[x]];
			}
			else
			{
				for (int x = 0; x < 6; x++)
					code += letters[num[x]];
			}
			return code;
		}
		private void GBGameGenie_Load(object sender, EventArgs e)
		{
			addcheatbt.Enabled = false;

			if (Global.Config.GBGGSaveWindowPosition && Global.Config.GBGGWndx >= 0 && Global.Config.GBGGWndy >= 0)
				Location = new Point(Global.Config.GBGGWndx, Global.Config.GBGGWndy);
		}

		private void SaveConfigSettings()
		{
			Global.Config.GBGGWndx = Location.X;
			Global.Config.GBGGWndy = Location.Y;
		}

		private void GGCodeMaskBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			// Find a better way to remove all NON HEX char, while still allowing copy/paste
			//Right now its all done through removing em GGCodeMaskBox_TextChanged 
		}

		private void GGCodeMaskBox_TextChanged(object sender, EventArgs e)
		{

			if (Processing == false)
			{
				Processing = true;
				//insert REGEX Remove non HEXA char
				if (Regex.IsMatch(GGCodeMaskBox.Text, @"[^a-fA-F0-9]"))
				{
					string temp = Regex.Replace(GGCodeMaskBox.Text, @"[^a-fA-F0-9]", string.Empty);
					GGCodeMaskBox.Text = temp;
				}

				if (GGCodeMaskBox.Text.Length > 0)
				{
					int val = -1;
					int add = -1;
					int cmp = -1;
					GBGGDecode(GGCodeMaskBox.Text, ref val, ref add, ref cmp);
					if (add > -1)
						AddressBox.Text = String.Format("{0:X4}", add);
					if (val > -1)
						ValueBox.Text = String.Format("{0:X2}", val);
					if (cmp > -1)
						CompareBox.Text = String.Format("{0:X2}", cmp);
					else
						CompareBox.Text = "";

					addcheatbt.Enabled = true;
				}
				else
				{
					AddressBox.Text = "";
					ValueBox.Text = "";
					CompareBox.Text = "";
					addcheatbt.Enabled = false;
				}
				Processing = false;
			}
		}

		private void Keypad_Click(object sender, EventArgs e)
		{
			if (GGCodeMaskBox.Text.Length < 9)
			{
				string code = "";
				if (sender == B0) code = "0";
				if (sender == B1) code = "1";
				if (sender == B2) code = "2";
				if (sender == B3) code = "3";
				if (sender == B4) code = "4";
				if (sender == B5) code = "5";
				if (sender == B6) code = "6";
				if (sender == B7) code = "7";
				if (sender == B8) code = "8";
				if (sender == B9) code = "9";
				if (sender == BA) code = "A";
				if (sender == BB) code = "B";
				if (sender == BC) code = "C";
				if (sender == BD) code = "D";
				if (sender == BE) code = "E";
				if (sender == BF) code = "F";

				GGCodeMaskBox.Text += code;
				//int x = GGCodeMaskBox.SelectionStart;
				//GGCodeMaskBox.Text = GGCodeMaskBox.Text.Insert(x, code);
				//GGCodeMaskBox.SelectionStart = x + 1;
			}
		}

		private void AddressBox_TextChanged(object sender, EventArgs e)
		{
			//remove invalid character when pasted
			if (Processing == false)
			{
				Processing = true;
				if (Regex.IsMatch(AddressBox.Text, @"[^a-fA-F0-9]"))
				{
					string temp = Regex.Replace(AddressBox.Text, @"[^a-fA-F0-9]", string.Empty);
					AddressBox.Text = temp;
				}
				if ((AddressBox.Text.Length > 0) || (ValueBox.Text.Length > 0))
				{
					int val = 0;
					int add = 0;
					int cmp = -1;
					if (ValueBox.Text.Length > 0)
						val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					if (AddressBox.Text.Length > 0)
						add = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					if (CompareBox.Text.Length == 2)
						cmp = int.Parse(CompareBox.Text, NumberStyles.HexNumber);

					GGCodeMaskBox.Text = GBGGEncode(val, add, cmp);
					addcheatbt.Enabled = true;
				}
				else
				{
					GGCodeMaskBox.Text = "";
					addcheatbt.Enabled = false;
				}
				Processing = false;
			}
			
		}


		private void ValueBox_TextChanged(object sender, EventArgs e)
		{
			if (Processing == false)
			{
				Processing = true;
				//remove invalid character when pasted
				if (Regex.IsMatch(ValueBox.Text, @"[^a-fA-F0-9]"))
				{
					string temp = Regex.Replace(ValueBox.Text, @"[^a-fA-F0-9]", string.Empty);
					ValueBox.Text = temp;
				}
				if ((AddressBox.Text.Length > 0) || (ValueBox.Text.Length > 0))
				{
					int val = 0;
					int add = 0;
					int cmp = -1;
					if (ValueBox.Text.Length > 0)
						val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					if (AddressBox.Text.Length > 0)
						add = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					if (CompareBox.Text.Length == 2)
						cmp = int.Parse(CompareBox.Text, NumberStyles.HexNumber);
					GGCodeMaskBox.Text = GBGGEncode(val, add, cmp);
					addcheatbt.Enabled = true;

				}
				else
				{
					GGCodeMaskBox.Text = "";
					addcheatbt.Enabled = false;
				}
				Processing = false;
			}
			
		}

		private void CompareBox_TextChanged(object sender, EventArgs e)
		{
			if (Processing == false)
			{
				Processing = true;
				//remove invalid character when pasted
				if (Regex.IsMatch(CompareBox.Text, @"[^a-fA-F0-9]"))
				{
					string temp = Regex.Replace(CompareBox.Text, @"[^a-fA-F0-9]", string.Empty);
					CompareBox.Text = temp;
				}
				if ((CompareBox.Text.Length == 2) || (CompareBox.Text.Length == 0))
				{
					if ((AddressBox.Text.Length > 0) || (ValueBox.Text.Length > 0))
					{
						int val = 0;
						int add = 0;
						int cmp = -1;
						if (ValueBox.Text.Length > 0)
							val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
						if (AddressBox.Text.Length > 0)
							add = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
						if (CompareBox.Text.Length == 2)
							cmp = int.Parse(CompareBox.Text, NumberStyles.HexNumber);
						GGCodeMaskBox.Text = GBGGEncode(val, add, cmp);
						addcheatbt.Enabled = true;

					}
					else
					{
						GGCodeMaskBox.Text = "";
						addcheatbt.Enabled = false;
					}
				}
				Processing = false;
			}
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			AddressBox.Text = "";
			ValueBox.Text = "";
			CompareBox.Text = "";
			GGCodeMaskBox.Text = "";
			addcheatbt.Enabled = false;
		}

		private void AddCheatClick(object sender, EventArgs e)
		{
			if ((Global.Emulator.SystemId == "GB") || (Global.Game.System == "GG"))
			{
				Cheat c = new Cheat();
				if (cheatname.Text.Length > 0)
					c.name = cheatname.Text;
				else
				{
					Processing = true;
					GGCodeMaskBox.TextMaskFormat = MaskFormat.IncludeLiterals;
					c.name = GGCodeMaskBox.Text;
					GGCodeMaskBox.TextMaskFormat = MaskFormat.ExcludePromptAndLiterals;
					Processing = false;
				}

				if (String.IsNullOrWhiteSpace(AddressBox.Text))
					c.address = 0;
				else
					c.address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);

				if (String.IsNullOrWhiteSpace(ValueBox.Text))
					c.value = 0;
				else
					c.value = (byte)(int.Parse(ValueBox.Text, NumberStyles.HexNumber));

				if (!String.IsNullOrWhiteSpace(CompareBox.Text))
				{
					try
					{
						c.compare = byte.Parse(CompareBox.Text, NumberStyles.HexNumber);
					}
					catch
					{
						c.compare = null;
					}
				}
				else
				{
					c.compare = null;
				}
				for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)

					if (Global.Emulator.MemoryDomains[x].ToString() == "System Bus")
					{
						c.domain = Global.Emulator.MemoryDomains[x];
						c.Enable();
						Global.MainForm.Cheats1.AddCheat(c);
						break;
					}
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GBGGSaveWindowPosition ^= true;
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GBGGAutoload ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = Global.Config.GBGGAutoload;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.GBGGSaveWindowPosition;
		}


	}
}
