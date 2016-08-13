using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	[ToolAttributes(false, null)]
	public partial class GBGameGenie : Form, IToolFormAutoConfig
	{
		// TODO: fix the use of Global.Game.System and Emulator.SystemId
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		private readonly Dictionary<char, int> _gameGenieTable = new Dictionary<char, int>();
		private bool _processing;

		public bool AskSaveChanges() { return true; }

		public bool UpdateBefore { get { return false; } }
		
		public void Restart()
		{
			if ((Emulator.SystemId != "GB") && (Global.Game.System != "GG"))
			{
				Close();
			}
		}

		public void NewUpdate(ToolFormUpdateType type) { }
		
		public void UpdateValues()
		{
			if ((Emulator.SystemId != "GB") && (Global.Game.System != "GG"))
			{
				Close();
			}
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		public GBGameGenie()
		{
			InitializeComponent();

			_gameGenieTable.Add('0', 0);     // 0000
			_gameGenieTable.Add('1', 1);     // 0001
			_gameGenieTable.Add('2', 2);     // 0010
			_gameGenieTable.Add('3', 3);     // 0011
			_gameGenieTable.Add('4', 4);     // 0100
			_gameGenieTable.Add('5', 5);     // 0101
			_gameGenieTable.Add('6', 6);     // 0110
			_gameGenieTable.Add('7', 7);     // 0111
			_gameGenieTable.Add('8', 8);     // 1000
			_gameGenieTable.Add('9', 9);     // 1001
			_gameGenieTable.Add('A', 10);    // 1010
			_gameGenieTable.Add('B', 11);    // 1011
			_gameGenieTable.Add('C', 12);    // 1100
			_gameGenieTable.Add('D', 13);    // 1101
			_gameGenieTable.Add('E', 14);    // 1110
			_gameGenieTable.Add('F', 15);    // 1111
		}

		public void GBGGDecode(string code, ref int val, ref int add, ref int cmp)
		{

			// No cypher on value
			// Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |   8   |
			// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			// maps to|      Value    |A|B|C|D|E|F|G|H|I|J|K|L|XOR 0xF|a|b|c|c|NotUsed|e|f|g|h|
			// proper |      Value    |XOR 0xF|A|B|C|D|E|F|G|H|I|J|K|L|g|h|a|b|Nothing|c|d|e|f|
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
				add = x << 8;
			}
			else
			{
				add = -1;
			}

			if (code.Length > 3)
			{
				_gameGenieTable.TryGetValue(code[3], out x);
				add |= x << 4;
			}

			if (code.Length > 4)
			{
				_gameGenieTable.TryGetValue(code[4], out x);
				add |= x;
			}

			if (code.Length > 5)
			{
				_gameGenieTable.TryGetValue(code[5], out x);
				add |= (x ^ 0xF) << 12;
			}

			// compare need to be full
			if (code.Length > 8)
			{
				int comp = 0;
				_gameGenieTable.TryGetValue(code[6], out x);
				comp = x << 2;

				// 8th character ignored
				_gameGenieTable.TryGetValue(code[8], out x);
				comp |= (x & 0xC) >> 2;
				comp |= (x & 0x3) << 6;
				cmp = comp ^ 0xBA;
			}
			else
			{
				cmp = -1;
			}
		}

		private string GBGGEncode(int val, int add, int cmp)
		{
			char[] letters = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
			string code = string.Empty;
			int[] num = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			num[0] = (val & 0xF0) >> 4;
			num[1] = val & 0x0F;

			num[2] = (add & 0x0F00) >> 8;
			num[3] = (add & 0x00F0) >> 4;
			num[4] = add & 0x000F;
			num[5] = ((add & 0xF000) >> 12) ^ 0xF;
			if (cmp > -1)
			{
				int xoredcomp = (cmp & 0xFF) ^ 0xBA;
				num[6] = (xoredcomp & 0x30) >> 2;
				num[6] |= (xoredcomp & 0x0C) >> 2;

				// 8th char has no real use (its value is not reflected in the address:value:compare
				// probably a protection to stop people making code up, xor the 7th digit with 0x8
				// to get back what the original code had (Might be more to it)
				num[7] = num[6] ^ 8;
				num[8] = (xoredcomp & 0xC0) >> 6;
				num[8] |= (xoredcomp & 0x03) << 2;

				for (int i = 0; i < 9; i++)
				{
					code += letters[num[i]];
				}
			}
			else
			{
				for (int i = 0; i < 6; i++)
				{
					code += letters[num[i]];
				}
			}

			return code;
		}

		private void GBGameGenie_Load(object sender, EventArgs e)
		{
			addcheatbt.Enabled = false;

			//"Game Boy/Game Gear Game Genie Encoder / Decoder"
			if (Emulator.SystemId == "GB")
			{
				Text = "Game Boy Game Genie Encoder/Decoder";
			}
			else
			{
				Text = "Game Gear Game Genie Encoder/Decoder";
			}
		}

		#region Dialog and Control Events

		private void AddCheatClick(object sender, EventArgs e)
		{
			if ((Emulator.SystemId == "GB") || (Global.Game.System == "GG"))
			{
				string name;
				var address = 0;
				var value = 0;
				int? compare = null;

				if (!string.IsNullOrWhiteSpace(cheatname.Text))
				{
					name = cheatname.Text;
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
					address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
				}

				if (!string.IsNullOrWhiteSpace(ValueBox.Text))
				{
					value = (byte)int.Parse(ValueBox.Text, NumberStyles.HexNumber);
				}

				if (!string.IsNullOrWhiteSpace(CompareBox.Text))
				{
					try
					{
						compare = byte.Parse(CompareBox.Text, NumberStyles.HexNumber);
					}
					catch
					{
						compare = null;
					}
				}

				var watch = Watch.GenerateWatch(
					MemoryDomains["System Bus"],
					address,
					WatchSize.Byte,
					Client.Common.DisplayType.Hex,
					false,
					name);

				Global.CheatList.Add(new Cheat(
					watch,
					value,
					compare));
			}
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			AddressBox.Text = string.Empty;
			ValueBox.Text = string.Empty;
			CompareBox.Text = string.Empty;
			GGCodeMaskBox.Text = string.Empty;
			addcheatbt.Enabled = false;
		}

		private void CompareBox_TextChanged(object sender, EventArgs e)
		{
			if (_processing == false)
			{
				_processing = true;

				// remove invalid character when pasted
				if (Regex.IsMatch(CompareBox.Text, @"[^a-fA-F0-9]"))
				{
					CompareBox.Text = Regex.Replace(CompareBox.Text, @"[^a-fA-F0-9]", string.Empty);
				}

				if ((CompareBox.Text.Length == 2) || (CompareBox.Text.Length == 0))
				{
					if ((AddressBox.Text.Length > 0) || (ValueBox.Text.Length > 0))
					{
						int val = 0;
						int add = 0;
						int cmp = -1;
						if (ValueBox.Text.Length > 0)
						{
							val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
						}

						if (AddressBox.Text.Length > 0)
						{
							add = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
						}

						if (CompareBox.Text.Length == 2)
						{
							cmp = int.Parse(CompareBox.Text, NumberStyles.HexNumber);
						}

						GGCodeMaskBox.Text = GBGGEncode(val, add, cmp);
						addcheatbt.Enabled = true;
					}
					else
					{
						GGCodeMaskBox.Text = string.Empty;
						addcheatbt.Enabled = false;
					}
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
					var val = 0;
					var add = 0;
					var cmp = -1;
					if (ValueBox.Text.Length > 0)
					{
						val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					}

					if (AddressBox.Text.Length > 0)
					{
						add = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					}

					if (CompareBox.Text.Length == 2)
					{
						cmp = int.Parse(CompareBox.Text, NumberStyles.HexNumber);
					}

					GGCodeMaskBox.Text = GBGGEncode(val, add, cmp);
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

		private void AddressBox_TextChanged(object sender, EventArgs e)
		{
			// remove invalid character when pasted
			if (_processing == false)
			{
				_processing = true;
				if (Regex.IsMatch(AddressBox.Text, @"[^a-fA-F0-9]"))
				{
					AddressBox.Text = Regex.Replace(AddressBox.Text, @"[^a-fA-F0-9]", string.Empty);
				}

				if ((AddressBox.Text.Length > 0) || (ValueBox.Text.Length > 0))
				{
					var val = 0;
					var add = 0;
					var cmp = -1;

					if (ValueBox.Text.Length > 0)
					{
						val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					}

					if (AddressBox.Text.Length > 0)
					{
						add = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					}

					if (CompareBox.Text.Length == 2)
					{
						cmp = int.Parse(CompareBox.Text, NumberStyles.HexNumber);
					}

					GGCodeMaskBox.Text = GBGGEncode(val, add, cmp);
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

		private void Keypad_Click(object sender, EventArgs e)
		{
			if (GGCodeMaskBox.Text.Length < 9)
			{
				var code = string.Empty;

				if (sender == B0) { code = "0"; }
				else if (sender == B1) { code = "1"; }
				else if (sender == B2) { code = "2"; }
				else if (sender == B3) { code = "3"; }
				else if (sender == B4) { code = "4"; }
				else if (sender == B5) { code = "5"; }
				else if (sender == B6) { code = "6"; }
				else if (sender == B7) { code = "7"; }
				else if (sender == B8) { code = "8"; }
				else if (sender == B9) { code = "9"; }
				else if (sender == BA) { code = "A"; }
				else if (sender == BB) { code = "B"; }
				else if (sender == BC) { code = "C"; }
				else if (sender == BD) { code = "D"; }
				else if (sender == BE) { code = "E"; }
				else if (sender == BF) { code = "F"; }

				GGCodeMaskBox.Text += code;
			}
		}

		private void GGCodeMaskBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			// Find a better way to remove all NON HEX char, while still allowing copy/paste
			// Right now its all done through removing em GGCodeMaskBox_TextChanged 
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
					var val = -1;
					var add = -1;
					var cmp = -1;
					GBGGDecode(GGCodeMaskBox.Text, ref val, ref add, ref cmp);
					if (add > -1)
					{
						AddressBox.Text = string.Format("{0:X4}", add);
					}

					if (val > -1)
					{
						ValueBox.Text = string.Format("{0:X2}", val);
					}

					if (cmp > -1)
					{
						CompareBox.Text = string.Format("{0:X2}", cmp);
					}
					else
					{
						CompareBox.Text = string.Empty;
					}

					addcheatbt.Enabled = true;
				}
				else
				{
					AddressBox.Text = string.Empty;
					ValueBox.Text = string.Empty;
					CompareBox.Text = string.Empty;
					addcheatbt.Enabled = false;
				}

				_processing = false;
			}
		}

		#endregion
	}
}
