using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class HexFind : Form
	{
		public HexFind()
		{
			InitializeComponent();
			ChangeCasing();
		}

		public Point InitialLocation { get; set; }

		public string InitialValue
		{
			get { return FindBox.Text; }
			set { FindBox.Text = value ?? string.Empty; }
		}

		private void HexFind_Load(object sender, EventArgs e)
		{
			if (InitialLocation.X > 0 && InitialLocation.Y > 0)
			{
				Location = InitialLocation;
			}

			FindBox.Focus();
			FindBox.Select();
		}

		private string GetFindBoxChars()
		{
			if (string.IsNullOrWhiteSpace(FindBox.Text))
			{
				return string.Empty;
			}
			
			if (HexRadio.Checked)
			{
				return FindBox.Text;
			}
			
			
			var bytes = GlobalWin.Tools.HexEditor.ConvertTextToBytes(FindBox.Text);

			var bytestring = new StringBuilder();
			foreach (var b in bytes)
			{
				bytestring.Append(string.Format("{0:X2}", b));
			}

			return bytestring.ToString();
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
			var text = FindBox.Text;
			var location = FindBox.Location;
			var size = FindBox.Size;

			Controls.Remove(FindBox);

			if (HexRadio.Checked)
			{

				FindBox = new HexTextBox
				{
					CharacterCasing = CharacterCasing.Upper,
					Nullable = HexRadio.Checked,
					Text = text,
					Size = size,
					Location = location
				};
			}
			else
			{
				FindBox = new TextBox
				{
					Text = text,
					Size = size,
					Location = location
				};
			}

			Controls.Add(FindBox);
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
				Find_Next_Click(null, null);
				e.Handled = true;
			}
		}

		private void HexFind_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				e.Handled = true;
				Close();
			}
			else if (e.KeyData == Keys.Enter)
			{
				Find_Next_Click(null, null);
				e.Handled = true;
			}
		}
	}
}
