using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;

namespace BizHawk.Client.EmuHawk
{
	[ToolAttributes(false, null)]
	public partial class GenGameGenie : Form, IToolFormAutoConfig
	{
		#pragma warning disable 675

		/// <summary>
		/// For now this is is an unecessary restriction to make sure it doesn't show up as available for non-genesis cores
		/// Note: this unnecessarily prevents it from being on the Genesis core, but that's okay it isn't released
		/// Eventually we want a generic game genie tool and a hack like this won't be necessary
		/// </summary>
		[RequiredService]
		private GPGX Emulator { get; set; }

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		private readonly Dictionary<char, int> _gameGenieTable = new Dictionary<char, int>
		{
			{ 'A', 0 },
			{ 'B', 1 },
			{ 'C', 2 },
			{ 'D', 3 },
			{ 'E', 4 },
			{ 'F', 5 },
			{ 'G', 6 },
			{ 'H', 7 },
			{ 'J', 8 },
			{ 'K', 9 },
			{ 'L', 10 },
			{ 'M', 11 },
			{ 'N', 12 },
			{ 'P', 13 },
			{ 'R', 14 },
			{ 'S', 15 },
			{ 'T', 16 },
			{ 'V', 17 },
			{ 'W', 18 },
			{ 'X', 19 },
			{ 'Y', 20 },
			{ 'Z', 21 },
			{ '0', 22 },
			{ '1', 23 },
			{ '2', 24 },
			{ '3', 25 },
			{ '4', 26 },
			{ '5', 27 },
			{ '6', 28 },
			{ '7', 29 },
			{ '8', 30 },
			{ '9', 31 }
		};

		private bool _processing;

		private void GenGameGenie_Load(object sender, EventArgs e)
		{

		}

		#region Public API

		public bool AskSaveChanges() { return true; }

		public bool UpdateBefore { get { return false; } }

		public void Restart()
		{
			if (Emulator.SystemId != "GEN")
			{
				Close();
			}
		}

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			if (Emulator.SystemId != "GEN")
			{
				Close();
			}
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		public GenGameGenie()
		{
			InitializeComponent();
		}

		#endregion

		// code is code to be converted, val is pointer to value, add is pointer to address
		private void GenGGDecode(string code, ref int val, ref int add)
		{
			long hexcode = 0;

			// convert code to a long binary string
			foreach (var t in code)
			{
				hexcode <<= 5;
				int y;
				_gameGenieTable.TryGetValue(t, out y);
				hexcode |= y;
			}

			long decoded = (hexcode & 0xFF00000000) >> 32;
			decoded |= hexcode & 0x00FF000000;
			decoded |= (hexcode & 0x0000FF0000) << 16;
			decoded |= (hexcode & 0x00000000700) << 5;
			decoded |= (hexcode & 0x000000F800) >> 3;
			decoded |= (hexcode & 0x00000000FF) << 16;

			val = (int)(decoded & 0x000000FFFF);
			add = (int)((decoded & 0xFFFFFF0000) >> 16);
		}

		private static string GenGGEncode(int val, int add)
		{
			long encoded;
			string code = null;

			encoded = (long)(val & 0x00FF) << 32;
			encoded |= (val & 0xE000) >> 5;
			encoded |= (val & 0x1F00) << 3;
			encoded |= add & 0xFF0000;
			encoded |= (add & 0x00FF00) << 16;
			encoded |= add & 0x0000FF;

			char[] letters = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'R', 'S', 'T', 'V', 'W', 'X', 'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
			for (var i = 0; i < 8; i++)
			{
				var chr = (int)(encoded & 0x1F);
				code += letters[chr];
				encoded >>= 5; 
			}

			// reverse string, as its build backward
			var array = code.ToCharArray();
			Array.Reverse(array);
			return new string(array);
		}

		#region Dialog and Control Events

		private void GGCodeMaskBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			// ignore I O Q U
			if ((e.KeyChar == 73) || (e.KeyChar == 79) || (e.KeyChar == 81) || (e.KeyChar == 85) ||
					(e.KeyChar == 105) || (e.KeyChar == 111) || (e.KeyChar == 113) || (e.KeyChar == 117))
			{
				e.KeyChar = '\n';
			}
		}

		private void GGCodeMaskBox_TextChanged(object sender, EventArgs e)
		{
			if (_processing == false)
			{
				_processing = true;

				// remove Invalid I O Q P if pasted
				GGCodeMaskBox.Text = GGCodeMaskBox.Text.Replace("I", string.Empty);
				GGCodeMaskBox.Text = GGCodeMaskBox.Text.Replace("O", string.Empty);
				GGCodeMaskBox.Text = GGCodeMaskBox.Text.Replace("Q", string.Empty);
				GGCodeMaskBox.Text = GGCodeMaskBox.Text.Replace("U", string.Empty);

				if (GGCodeMaskBox.Text.Length > 0)
				{
					int val = 0;
					int add = 0;
					GenGGDecode(GGCodeMaskBox.Text, ref val, ref add);
					AddressBox.Text = string.Format("{0:X6}", add);
					ValueBox.Text = string.Format("{0:X4}", val);
					AddCheatButton.Enabled = true;
				}
				else
				{
					AddressBox.Text = string.Empty;
					ValueBox.Text = string.Empty;
					AddCheatButton.Enabled = false;
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
					int val = 0;
					int add = 0;
					if (ValueBox.Text.Length > 0)
					{
						val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					}

					if (AddressBox.Text.Length > 0)
					{
						add = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					}

					GGCodeMaskBox.Text = GenGGEncode(val, add);
					AddCheatButton.Enabled = true;
				}
				else
				{
					GGCodeMaskBox.Text = string.Empty;
					AddCheatButton.Enabled = false;
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
					int val = 0;
					int add = 0;
					if (ValueBox.Text.Length > 0)
					{
						val = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
					}

					if (AddressBox.Text.Length > 0)
					{
						add = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					}

					GGCodeMaskBox.Text = GenGGEncode(val, add);
					AddCheatButton.Enabled = true;
				}
				else
				{
					GGCodeMaskBox.Text = string.Empty;
					AddCheatButton.Enabled = false;
				}

				_processing = false;
			}
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			AddressBox.Text = string.Empty;
			ValueBox.Text = string.Empty;
			GGCodeMaskBox.Text = string.Empty;
			AddCheatButton.Enabled = false;
		}

		private void AddCheatButton_Click(object sender, EventArgs e)
		{
			string name;
			var address = 0;
			var value = 0;

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
				value = ValueBox.ToRawInt() ?? 0;
			}

			var watch = Watch.GenerateWatch(
				MemoryDomains["MD CART"],
				address,
				WatchSize.Word,
				Client.Common.DisplayType.Hex,
				true,
				name
			);

			Global.CheatList.Add(new Cheat(
				watch,
				value
			));
		}

		#endregion
	}
}
