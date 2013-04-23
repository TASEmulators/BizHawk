using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public partial class SNESGameGenie : Form
	{
		public int Address = 0x000000;
		public int Value = 0x00;
		private readonly Dictionary<char, int> GameGenieTable = new Dictionary<char, int>();
		private bool Decoding = false;
		private bool Encoding = false;
         

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

		private void SNESGameGenie_Load(object sender, EventArgs e)
		{
			AddCheat.Enabled = false;

			if (Global.Config.SNESGGSaveWindowPosition && Global.Config.SNESGGWndx >= 0 && Global.Config.SNESGGWndy >= 0)
				Location = new Point(Global.Config.SNESGGWndx, Global.Config.SNESGGWndy);
		}

		private void SaveConfigSettings()
		{
			Global.Config.SNESGGWndx = Location.X;
			Global.Config.SNESGGWndy = Location.Y;
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

		}

		public void SNESDecodeGameGenieCode(string code)
		{

            //Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
            //Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
            //XXYY-YYYY, where XX is the value, and YY-YYYY is the address.
            //Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |
            //Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			//maps to|     Value     |i|j|k|l|q|r|s|t|o|p|a|b|c|d|u|v|w|x|e|f|g|h|m|n|
			//order  |     Value     |a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|
			//Decoding = true;

			int x;
			byte[] val = { 0, 0};
  
			// Getting Value
			if (code.Length > 0)
			{
				GameGenieTable.TryGetValue(code[0], out x);
				Value = x << 4;
			}

			if (code.Length > 1)
			{
				GameGenieTable.TryGetValue(code[1], out x);
				Value |= x;
			}
			//Address
			if (code.Length > 2)
			{
				GameGenieTable.TryGetValue(code[2], out x);
				Address = (x << 12);
			}
			
			if (code.Length > 3)
			{
				GameGenieTable.TryGetValue(code[3], out x);
				Address |= (x << 4);
			}
			
			if (code.Length > 4)
			{
				GameGenieTable.TryGetValue(code[4], out x);
				Address |= ((x & 0xC) << 6);
				Address |= ((x & 0x3) << 22);
			}
			
			if (code.Length > 5)
			{
				GameGenieTable.TryGetValue(code[5], out x);
				Address |= ((x & 0xC) << 18);
				Address |= ((x & 0x3) << 2);
			}

			if (code.Length > 6)
			{
				GameGenieTable.TryGetValue(code[6], out x);
				Address |= ((x & 0xC) >> 2);
				Address |= ((x & 0x3) << 18);
			}

			if (code.Length > 7)
			{
				GameGenieTable.TryGetValue(code[7], out x);
				Address |= ((x & 0xC) << 14);
				Address |= ((x & 0x3) << 10);
			}
			
			SetProperties();

        }

		private void SetProperties()
		{
			if (Address >= 0)
				AddressBox.Text = String.Format("{0:X6}", Address);
			else
				AddressBox.Text = "";

			if (Value >= 0)
				ValueBox.Text = String.Format("{0:X2}", Value);

		}

		private void ClearProperties()
		{
			Address = -1;
			Value = -1;
			AddressBox.Text = "";
			ValueBox.Text = "";
			AddCheat.Enabled = false;
			//Decoding = false;
		}

		private void GameGenieCode_TextChanged(object sender, EventArgs e)
		{
			if (Encoding == false)
			{
				if (GameGenieCode.Text.Length > 0)
				{
					Decoding = true;
					SNESDecodeGameGenieCode(GameGenieCode.Text);
				}
				else
					ClearProperties();
			}
			TryEnableAddCheat();
			Decoding = false;
		}

		private void Keypad_Click(object sender, EventArgs e)
		{
			if (GameGenieCode.Text.Length < 8)
			{
				string code = "";
				if (sender == B0) code = "0";
				if (sender == B1) code += "1";
				if (sender == B2) code += "2";
				if (sender == B3) code += "3";
				if (sender == B4) code += "4";
				if (sender == B5) code += "5";
				if (sender == B6) code += "6";
				if (sender == B7) code += "7";
				if (sender == B8) code += "8";
				if (sender == B9) code += "9";
				if (sender == BA) code += "A";
				if (sender == BB) code += "B";
				if (sender == BC) code += "C";
				if (sender == BD) code += "D";
				if (sender == BE) code += "E";
				if (sender == BF) code += "F";

				int x = GameGenieCode.SelectionStart;
				GameGenieCode.Text = GameGenieCode.Text.Insert(x, code);
				GameGenieCode.SelectionStart = x + 1;
			}
		}

		private void AddressBox_TextChanged(object sender, EventArgs e)
		{
			if (Decoding == false)
			{
				if (AddressBox.Text.Length > 0 )
				{
					Address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					if (ValueBox.Text.Length > 0)
						Value = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					else
						Value = 0;
					Encoding = true;
					SNESEncodeGameGenie();
					Encoding = false;
				}
			}
			TryEnableAddCheat();
			
		}



		private void TryEnableAddCheat()
		{
			if (AddressBox.Text.Length == 6 && ValueBox.Text.Length == 2)
				AddCheat.Enabled = true;
			else
				AddCheat.Enabled = false;
		}

		private void ValueBox_TextChanged(object sender, EventArgs e)
		{
			if (Decoding == false)
			{
				if (ValueBox.Text.Length > 0)
				{
					if (AddressBox.Text.Length > 0)
						Address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					else
						Address = 0x000000;
					Value = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					Encoding = true;
					SNESEncodeGameGenie();
					Encoding = false;
				}
			}
			TryEnableAddCheat();
			
		}


        private void SNESEncodeGameGenie()
		{
			//Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
			//Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
			//Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |
			//Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			//maps to|     Value     |i|j|k|l|q|r|s|t|o|p|a|b|c|d|u|v|w|x|e|f|g|h|m|n|
			//order  |     Value     |a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|
			char[] letters = { 'D', 'F', '4', '7', '0', '9', '1', '5', '6', 'B', 'C', '8', 'A', '2', '3', 'E' };
			GameGenieCode.Text = "";
			int[] num = { 0, 0, 0, 0, 0, 0, 0, 0 };
			num[0] = (Value & 0xF0) >>4;
			num[1] = Value & 0x0F;

			num[2] = (Address & 0x00F000) >> 12; //ijkl
			num[3] = (Address & 0x0000F0) >> 4; //qrst
			num[4] = ((Address & 0x000300) >> 6) | ((Address & 0xC00000) >> 22); //opab
			num[5] = ((Address & 0x300000) >> 18) | ((Address & 0x00000C) >>2); //cduv
			num[6] = ((Address & 0x000003) << 2)| ((Address & 0x0C0000) >> 18);//wxef
			num[7] = ((Address & 0x030000) >> 14) | ((Address & 0x000C00) >> 10);//ghmn*/
			for (int x = 0; x < 8; x++) 
				GameGenieCode.Text += letters[num[x]];
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			ClearProperties();
			GameGenieCode.Text = "";
		}

		private void AddCheat_Click(object sender, EventArgs e)
		{
			AddCheatClick();
		}

		private void AddCheatClick()
		{
			Cheat c = new Cheat {name = GameGenieCode.Text};

			if (String.IsNullOrWhiteSpace(AddressBox.Text))
			{
				return;
			}
			else if (String.IsNullOrWhiteSpace(ValueBox.Text))
			{
				return;
			}
			c.address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
			c.value = byte.Parse(ValueBox.Text, NumberStyles.HexNumber);
			c.compare = null;
			c.domain = Global.Emulator.MemoryDomains[7]; //Bus
			c.Enable();

			Global.MainForm.Cheats1.AddCheat(c);
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
