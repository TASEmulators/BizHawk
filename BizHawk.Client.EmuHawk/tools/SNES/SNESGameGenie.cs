using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	[ToolAttributes(false, null)]
	public partial class SNESGameGenie : Form, IToolFormAutoConfig
	{
		[RequiredService]
		public LibsnesCore Emulator { get; set; }

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		// including transposition
		// Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
		// Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
		private readonly Dictionary<char, int> _gameGenieTable = new Dictionary<char, int>
		{
			{ 'D', 0 },  // 0000
			{ 'F', 1 },  // 0001
			{ '4', 2 },  // 0010
			{ '7', 3 },  // 0011
			{ '0', 4 },  // 0100
			{ '9', 5 },  // 0101
			{ '1', 6 },  // 0110
			{ '5', 7 },  // 0111
			{ '6', 8 },  // 1000
			{ 'B', 9 },  // 1001
			{ 'C', 10 }, // 1010 
			{ '8', 11 }, // 1011 
			{ 'A', 12 }, // 1100 
			{ '2', 13 }, // 1101 
			{ '3', 14 }, // 1110 
			{ 'E', 15 }  // 1111 
		};

		private bool _processing;

		public SNESGameGenie()
		{
			InitializeComponent();
		}

		private void SNESGameGenie_Load(object sender, EventArgs e)
		{
			addcheatbt.Enabled = false;
		}

		#region Public API

		public bool AskSaveChanges() { return true; }
		public bool UpdateBefore { get { return false; } }
		public void Restart()
		{
			// Do nothing
		}

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			// Do nothing
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		#endregion

		private void SnesGGDecode(string code, ref int val, ref int add)
		{
			// Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
			// Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
			// XXYY-YYYY, where XX is the value, and YY-YYYY is the address.
			// Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |
			// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			// maps to|     Value     |i|j|k|l|q|r|s|t|o|p|a|b|c|d|u|v|w|x|e|f|g|h|m|n|
			// order  |     Value     |a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|
			int x;
  
			// Getting Value
			if (code.Length > 0)
			{
				_gameGenieTable.TryGetValue(code[0], out x);
				val = x << 4;
			}

			if (code.Length > 1)
			{
				_gameGenieTable.TryGetValue(code[1], out x);
				val |= x;
			}

			// Address
			if (code.Length > 2)
			{
				_gameGenieTable.TryGetValue(code[2], out x);
				add = x << 12;
			}
			
			if (code.Length > 3)
			{
				_gameGenieTable.TryGetValue(code[3], out x);
				add |= x << 4;
			}
			
			if (code.Length > 4)
			{
				_gameGenieTable.TryGetValue(code[4], out x);
				add |= (x & 0xC) << 6;
				add |= (x & 0x3) << 22;
			}
			
			if (code.Length > 5)
			{
				_gameGenieTable.TryGetValue(code[5], out x);
				add |= (x & 0xC) << 18;
				add |= (x & 0x3) << 2;
			}

			if (code.Length > 6)
			{
				_gameGenieTable.TryGetValue(code[6], out x);
				add |= (x & 0xC) >> 2;
				add |= (x & 0x3) << 18;
			}

			if (code.Length > 7)
			{
				_gameGenieTable.TryGetValue(code[7], out x);
				add |= (x & 0xC) << 14;
				add |= (x & 0x3) << 10;
			}
		}
		
		private static string SnesGGEncode(int val, int add)
		{
			// Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
			// Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
			// Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |
			// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			// maps to|     Value     |i|j|k|l|q|r|s|t|o|p|a|b|c|d|u|v|w|x|e|f|g|h|m|n|
			// order  |     Value     |a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|
			char[] letters = { 'D', 'F', '4', '7', '0', '9', '1', '5', '6', 'B', 'C', '8', 'A', '2', '3', 'E' };
			var code = string.Empty;
			int[] num = { 0, 0, 0, 0, 0, 0, 0, 0 };
			num[0] = (val & 0xF0) >> 4;
			num[1] = val & 0x0F;

			num[2] = (add & 0x00F000) >> 12; // ijkl
			num[3] = (add & 0x0000F0) >> 4; // qrst
			num[4] = ((add & 0x000300) >> 6) | ((add & 0xC00000) >> 22); // opab
			num[5] = ((add & 0x300000) >> 18) | ((add & 0x00000C) >> 2); // cduv
			num[6] = ((add & 0x000003) << 2) | ((add & 0x0C0000) >> 18); // wxef
			num[7] = ((add & 0x030000) >> 14) | ((add & 0x000C00) >> 10); // ghmn
			
			for (int i = 0; i < 8; i++)
			{
				code += letters[num[i]];
			}

			return code;
		}

		#region Dialog and Control Events

		private void ClearButton_Click(object sender, EventArgs e)
		{
			AddressBox.Text = string.Empty;
			ValueBox.Text = string.Empty;
			GGCodeMaskBox.Text = string.Empty;
			addcheatbt.Enabled = false;
		}

		private void AddCheat_Click(object sender, EventArgs e)
		{
			string name;
			var address = 0;
			var value = 0;

			if (!string.IsNullOrWhiteSpace(CheatNameBox.Text))
			{
				name = CheatNameBox.Text;
			}
			else
			{
				_processing = true;
				GGCodeMaskBox.TextMaskFormat = MaskFormat.IncludeLiterals;
				name = GGCodeMaskBox.Text;
				GGCodeMaskBox.TextMaskFormat = MaskFormat.ExcludePromptAndLiterals;
				_processing = false;
			}

			if (!string.IsNullOrWhiteSpace(AddressBox.Text))
			{
				address = int.Parse(AddressBox.Text, NumberStyles.HexNumber)
					+ 0x8000;
			}

			if (!string.IsNullOrWhiteSpace(ValueBox.Text))
			{
				value = byte.Parse(ValueBox.Text, NumberStyles.HexNumber);
			}

			var watch = Watch.GenerateWatch(
				MemoryDomains["System Bus"],
				address,
				WatchSize.Byte,
				Client.Common.DisplayType.Hex,
				false,
				name
			);

			Global.CheatList.Add(new Cheat(
				watch,
				value
			));
		}

		private void AddressBox_TextChanged(object sender, EventArgs e)
		{
			// Remove invalid character when pasted
			if (_processing == false)
			{
				_processing = true;
				if (Regex.IsMatch(AddressBox.Text, @"[^a-fA-F0-9]"))
				{
					AddressBox.Text = Regex.Replace(AddressBox.Text, @"[^a-fA-F0-9]", string.Empty);
				}

				if (!string.IsNullOrEmpty(AddressBox.Text) || !string.IsNullOrEmpty(ValueBox.Text))
				{
					int val = 0, add = 0;
					if (!string.IsNullOrEmpty(AddressBox.Text))
					{
						add = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					}

					if (!string.IsNullOrEmpty(ValueBox.Text))
					{
						val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					}

					GGCodeMaskBox.Text = SnesGGEncode(val, add);
					addcheatbt.Enabled = true;
				}
				else
				{
					GGCodeMaskBox.Text = string.Empty;
					addcheatbt.Enabled = false;
				}

				_processing = false;
			}
		}

		private void ValueBox_TextChanged(object sender, EventArgs e)
		{
			if (_processing == false)
			{
				_processing = true;

				// remove invalid character when pasted
				if (Regex.IsMatch(ValueBox.Text, @"[^a-fA-F0-9]"))
				{
					ValueBox.Text = Regex.Replace(ValueBox.Text, @"[^a-fA-F0-9]", string.Empty);
				}

				if ((AddressBox.Text.Length > 0) || (ValueBox.Text.Length > 0))
				{
					int val = 0, add = 0;
					if (ValueBox.Text.Length > 0)
					{
						val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					}

					if (AddressBox.Text.Length > 0)
					{
						add = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					}

					GGCodeMaskBox.Text = SnesGGEncode(val, add);
					addcheatbt.Enabled = true;
				}
				else
				{
					GGCodeMaskBox.Text = string.Empty;
					addcheatbt.Enabled = false;
				}

				_processing = false;
			}
		}

		private void GGCodeMaskBox_TextChanged(object sender, EventArgs e)
		{
			if (_processing == false)
			{
				_processing = true;

				// insert REGEX Remove non HEXA char
				if (Regex.IsMatch(GGCodeMaskBox.Text, @"[^a-fA-F0-9]"))
				{
					GGCodeMaskBox.Text = Regex.Replace(GGCodeMaskBox.Text, @"[^a-fA-F0-9]", string.Empty);
				}

				if (GGCodeMaskBox.Text.Length > 0)
				{
					int val = 0, add = 0;
					SnesGGDecode(GGCodeMaskBox.Text, ref val, ref add);
					AddressBox.Text = string.Format("{0:X6}", add);
					ValueBox.Text = string.Format("{0:X2}", val);
					addcheatbt.Enabled = true;
				}
				else
				{
					AddressBox.Text = string.Empty;
					ValueBox.Text = string.Empty;
					addcheatbt.Enabled = false;
				}

				_processing = false;
			}
		}

		private void Keypad_Click(object sender, EventArgs e)
		{
			if (GGCodeMaskBox.Text.Length < 8)
			{
				var code = string.Empty;
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
			}
		}

		#endregion
	}
} 