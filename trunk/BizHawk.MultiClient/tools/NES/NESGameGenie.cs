using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;

using BizHawk.Client.Common;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESGameGenie : Form, IToolForm
	{
		private int? _address;
		private int? _value;
		private int? _compare;
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

		public bool AskSave() { return true; }
		public bool UpdateBefore { get { return false; } }
		public void Restart()
		{
			if (!(Global.Emulator is NES))
			{
				Close();
			}
		}
		public void UpdateValues()
		{
			if (!(Global.Emulator is NES))
			{
				Close();
			}
		}

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

			var decoder = new NESGameGenieDecoder(code);
			_address = decoder.Address;
			_value = decoder.Value;
			_compare = decoder.Compare;
			SetProperties();
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
			GameGenieCode.Text = new NESGameGenieEncoder(_address.Value, _value.Value, _compare).Encode();
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
					compare
				));

				ToolHelpers.UpdateCheatRelatedTools();
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
