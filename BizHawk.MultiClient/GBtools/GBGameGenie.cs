using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public partial class GBGameGenie : Form
	{
		public int Address = -1;
		public int Value = -1;
		public int Compare = -1;
		private readonly Dictionary<char, int> GameGenieTable = new Dictionary<char, int>();
		private bool Decoding = false;
		private bool Encoding = false;

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

		private void GBGameGenie_Load(object sender, EventArgs e)
		{
			AddCheat.Enabled = false;

			if (Global.Config.GBGGSaveWindowPosition && Global.Config.GBGGWndx >= 0 && Global.Config.GBGGWndy >= 0)
				Location = new Point(Global.Config.GBGGWndx, Global.Config.GBGGWndy);
		}

		private void SaveConfigSettings()
		{
			Global.Config.GBGGWndx = Location.X;
			Global.Config.GBGGWndy = Location.Y;
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

		public void GBDecodeGameGenieCode(string code)
		{
			//No cypher on value
			//Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |   8   |
			//Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			//maps to|      Value    |A|B|C|D|E|F|G|H|I|J|K|L|XOR 0xF|a|b|c|c|NotUsed|e|f|g|h|
			//proper |      Value    |XOR 0xF|A|B|C|D|E|F|G|H|I|J|K|L|g|h|a|b|Nothing|c|d|e|f|
  
			int x;
			byte[] val = { 0, 0 };

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
				Address = (x << 8);
			}
			else
				Address = -1;

			if (code.Length > 3)
			{
				GameGenieTable.TryGetValue(code[3], out x);
				Address |= (x << 4);
			}

			if (code.Length > 4)
			{
				GameGenieTable.TryGetValue(code[4], out x);
				Address |= x;
			}

			if (code.Length > 5)
			{
				GameGenieTable.TryGetValue(code[5], out x);
				Address |= ((x ^ 0xF) <<12);
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
				Compare = comp ^ 0xBA;
			}
			else
				Compare = -1;

			SetProperties();
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
			bool tempenc = Encoding;
			bool tempdec = Decoding;
			Encoding = true;
			Decoding = true;
			Address = -1;
			Value = -1;
			Compare = -1;
			AddressBox.Text = "";
			CompareBox.Text = "";
			ValueBox.Text = "";
			AddCheat.Enabled = false;
			Encoding = tempenc;
			Decoding = tempdec;
		}

		private void GameGenieCode_TextChanged(object sender, EventArgs e)
		{

			if (Encoding == false)
			{
				if (GameGenieCode.Text.Length > 0)
				{
					Decoding = true;
					GBDecodeGameGenieCode(GameGenieCode.Text);
				}
				else
					ClearProperties();
			}
			TryEnableAddCheat();
			Decoding = false;
		}

		private void Keypad_Click(object sender, EventArgs e)
		{
			if (GameGenieCode.Text.Length < 9)
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
				if (AddressBox.Text.Length > 0)
				{
					Address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					if (ValueBox.Text.Length > 0)
						Value = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					else
						Value = 0;
					if (CompareBox.Text.Length == 2)
						Compare = int.Parse(CompareBox.Text, NumberStyles.HexNumber);
					else
						Compare = 0;
					Encoding = true;
					GBEncodeGameGenie();
					Encoding = false;
				} 
			} 

			TryEnableAddCheat();
		}

		private void CompareBox_TextChanged(object sender, EventArgs e)
		{
			if (Decoding == false)
			{
				if (CompareBox.Text.Length == 2)
					Compare = int.Parse(CompareBox.Text, NumberStyles.HexNumber);
				if (AddressBox.Text.Length > 0)
					Address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
				else
					Address = 0x0000;
				if (ValueBox.Text.Length > 0)
						Value = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
				else
					Value = 0;
				Encoding = true;
				GBEncodeGameGenie();
				Encoding = false;
				
			}
			TryEnableAddCheat();
		

		}

		private void TryEnableAddCheat()
		{
			if (AddressBox.Text.Length == 4 && ValueBox.Text.Length == 2)
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
						Address = 0x0000;
					if (CompareBox.Text.Length == 2)
						Compare = int.Parse(CompareBox.Text, NumberStyles.HexNumber);
					else
						Compare = 0;
					Value = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					Encoding = true;
					GBEncodeGameGenie();
					Encoding = false;
				}
			}
			TryEnableAddCheat();
		}

		private void GBEncodeGameGenie()
		{
			char[] letters = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
			GameGenieCode.Text = "";
			int[] num = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			num[0] = (Value & 0xF0) >> 4;
			num[1] = Value & 0x0F;

			num[2] = (Address & 0x0F00) >> 8; 
			num[3] = (Address & 0x00F0) >> 4;
 			num[4] = (Address & 0x000F);
			num[5] = (((Address & 0xF000)  >> 12) ^ 0xF);
			if (CompareBox.Text.Length == 2)
			{
				
				int xoredcomp = ((Compare &0xFF) ^ 0xBA);
				num[6] = ((xoredcomp & 0x30) >> 2);
				num[6] |= ((xoredcomp & 0x0C) >> 2);
				// 8th char has no real use (its value is not reflected in the address:value:compare
				// probably a protection to stop people making code up, xor the 7th digit with 0x8
				// to get back what the original code had (Might be more to it)
				num[7] = num[6] ^ 8;
				num[8] = ((xoredcomp & 0xC0) >> 6);
				num[8] |= ((xoredcomp &0x03) << 2);
				for (int x = 0; x < 9; x++)
					GameGenieCode.Text += letters[num[x]];
			}	
			else
			{
				for (int x = 0; x < 6; x++)
					GameGenieCode.Text += letters[num[x]];
			}


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
			c.Enable();
			for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
				if (Global.Emulator.MemoryDomains[x].ToString() == "System Bus")
				{
					c.domain = Global.Emulator.MemoryDomains[x]; //Bus
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
