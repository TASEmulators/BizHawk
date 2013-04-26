using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BizHawk.MultiClient
{
	public partial class SNESGameGenie : Form
	{
		private readonly Dictionary<char, int> GameGenieTable = new Dictionary<char, int>();
		private bool Processing = false;

         		public SNESGameGenie()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
            //including transposition
            //Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
            //Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F

			GameGenieTable.Add('D', 0);  	//0000
			GameGenieTable.Add('F', 1);	//0001
			GameGenieTable.Add('4', 2);	 //0010
			GameGenieTable.Add('7', 3);	 //0011
			GameGenieTable.Add('0', 4);	 //0100
			GameGenieTable.Add('9', 5);	 //0101
			GameGenieTable.Add('1', 6);	 //0110
			GameGenieTable.Add('5', 7);	 //0111
			GameGenieTable.Add('6', 8);	 //1000
			GameGenieTable.Add('B', 9);	 //1001
			GameGenieTable.Add('C', 10); 	//1010 
			GameGenieTable.Add('8', 11); 	//1011 
			GameGenieTable.Add('A', 12); 	//1100 
			GameGenieTable.Add('2', 13); 	//1101 
			GameGenieTable.Add('3', 14); 	//1110 
			GameGenieTable.Add('E', 15); 	//1111 
		}

		public void SNESGGDecode(string code, ref int val, ref int add)
		{

            //Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
            //Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
            //XXYY-YYYY, where XX is the value, and YY-YYYY is the address.
            //Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |
            //Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			//maps to|     Value     |i|j|k|l|q|r|s|t|o|p|a|b|c|d|u|v|w|x|e|f|g|h|m|n|
			//order  |     Value     |a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|

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
				add = (x << 12);
			}
			
			if (code.Length > 3)
			{
				GameGenieTable.TryGetValue(code[3], out x);
				add |= (x << 4);
			}
			
			if (code.Length > 4)
			{
				GameGenieTable.TryGetValue(code[4], out x);
				add |= ((x & 0xC) << 6);
				add |= ((x & 0x3) << 22);
			}
			
			if (code.Length > 5)
			{
				GameGenieTable.TryGetValue(code[5], out x);
				add |= ((x & 0xC) << 18);
				add |= ((x & 0x3) << 2);
			}

			if (code.Length > 6)
			{
				GameGenieTable.TryGetValue(code[6], out x);
				add |= ((x & 0xC) >> 2);
				add |= ((x & 0x3) << 18);
			}

			if (code.Length > 7)
			{
				GameGenieTable.TryGetValue(code[7], out x);
				add |= ((x & 0xC) << 14);
				add |= ((x & 0x3) << 10);
			}

        }
		
		private string SNESGGEncode(int val, int add)
		{
			//Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
			//Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
			//Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |
			//Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			//maps to|     Value     |i|j|k|l|q|r|s|t|o|p|a|b|c|d|u|v|w|x|e|f|g|h|m|n|
			//order  |     Value     |a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|
			char[] letters = { 'D', 'F', '4', '7', '0', '9', '1', '5', '6', 'B', 'C', '8', 'A', '2', '3', 'E' };
			string code = "";
			int[] num = { 0, 0, 0, 0, 0, 0, 0, 0 };
			num[0] = (val & 0xF0) >>4;
			num[1] = val & 0x0F;

			num[2] = (add & 0x00F000) >> 12; //ijkl
			num[3] = (add & 0x0000F0) >> 4; //qrst
			num[4] = ((add & 0x000300) >> 6) | ((add & 0xC00000) >> 22); //opab
			num[5] = ((add & 0x300000) >> 18) | ((add & 0x00000C) >>2); //cduv
			num[6] = ((add & 0x000003) << 2)| ((add & 0x0C0000) >> 18);//wxef
			num[7] = ((add & 0x030000) >> 14) | ((add & 0x000C00) >> 10);//ghmn*/
			for (int x = 0; x < 8; x++) 
				code += letters[num[x]];
			return code;
		}
		private void SNESGameGenie_Load(object sender, EventArgs e)
		{
			addcheatbt.Enabled = false;

			if (Global.Config.SNESGGSaveWindowPosition && Global.Config.SNESGGWndx >= 0 && Global.Config.SNESGGWndy >= 0)
				Location = new Point(Global.Config.SNESGGWndx, Global.Config.SNESGGWndy);
		}

		private void SaveConfigSettings()
		{
			Global.Config.SNESGGWndx = Location.X;
			Global.Config.SNESGGWndy = Location.Y;
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
					int val = 0;
					int add = 0;
					SNESGGDecode(GGCodeMaskBox.Text, ref val, ref add);
					AddressBox.Text = String.Format("{0:X6}", add);
					ValueBox.Text = String.Format("{0:X2}", val);
					addcheatbt.Enabled = true;
				}
				else
				{
					AddressBox.Text = "";
					ValueBox.Text = "";
					addcheatbt.Enabled = false;
				}
				Processing = false;
			}
		}

		private void Keypad_Click(object sender, EventArgs e)
		{
			if (GGCodeMaskBox.Text.Length < 8)
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
					if (ValueBox.Text.Length > 0)
						val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					if (AddressBox.Text.Length > 0)
						add = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					GGCodeMaskBox.Text = SNESGGEncode(val, add);
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
					if (ValueBox.Text.Length > 0)
						val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					if (AddressBox.Text.Length > 0)
						add = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					GGCodeMaskBox.Text = SNESGGEncode(val, add);
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


 

		private void ClearButton_Click(object sender, EventArgs e)
		{
			AddressBox.Text = "";
			ValueBox.Text = "";
			GGCodeMaskBox.Text = "";
			addcheatbt.Enabled = false;
		}

		private void AddCheat_Click(object sender, EventArgs e)
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
			{
				c.address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
				c.address += 0x8000;
			}
			if (String.IsNullOrWhiteSpace(ValueBox.Text))
				c.value = 0;
			else
				c.value = (byte)(int.Parse(ValueBox.Text, NumberStyles.HexNumber));

			c.compare = null;
			for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)

				if (Global.Emulator.MemoryDomains[x].ToString() == "BUS")
				{
					c.domain = Global.Emulator.MemoryDomains[x];
					c.Enable();
					Global.MainForm.Cheats1.AddCheat(c);
					break;
				}

		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SNESGGSaveWindowPosition ^= true;
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SNESGGAutoload ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = Global.Config.SNESGGAutoload;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.SNESGGSaveWindowPosition;
		}






	}
}
