using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class HexFind : Form
	{
		private Point _location;
		public HexFind()
		{
			InitializeComponent();
		}

		public void SetInitialValue(string value)
		{
			FindBox.Text = value;
		}

		public void SetLocation(Point p)
		{
			_location = p;
			
		}

		private void HexFind_Load(object sender, EventArgs e)
		{
			if (_location.X > 0 && _location.Y > 0)
			{
				Location = _location;
			}
		}

		private string GetFindBoxChars()
		{
			if (String.IsNullOrWhiteSpace(FindBox.Text))
			{
				return String.Empty;
			}
			else if (HexRadio.Checked)
			{
				return FindBox.Text;
			}
			else
			{
				List<byte> bytes = FindBox.Text.Select(Convert.ToByte).ToList();

				StringBuilder bytestring = new StringBuilder();
				foreach (byte b in bytes)
				{
					bytestring.Append(String.Format("{0:X2}", b));
				}

				return bytestring.ToString();
			}
		}

		private void Find_Prev_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.HexEditor.FindPrev(GetFindBoxChars(), false);
		}

		private void Find_Next_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.HexEditor.FindNext(GetFindBoxChars(), false);
		}

		private void ChangeCasing()
		{
			if (HexRadio.Checked)
			{
				FindBox.CharacterCasing = CharacterCasing.Upper;
			}
			else
			{
				FindBox.CharacterCasing = CharacterCasing.Normal;
			}
		}

		private void HexRadio_CheckedChanged(object sender, EventArgs e)
		{
			ChangeCasing();
		}

		private void TextRadio_CheckedChanged(object sender, EventArgs e)
		{
			ChangeCasing();
		}

		private void FindBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				GlobalWin.Tools.HexEditor.FindNext(GetFindBoxChars(), false);
			}
		}
	}
}
