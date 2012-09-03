using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public partial class RamWatchNewWatch : Form
	{
		public Watch watch = new Watch();
		public bool userSelected = false;
		public bool customSetup = false;
		public Point location = new Point();

		public RamWatchNewWatch()
		{
			InitializeComponent();
		}

		private void SetTypeRadio(Watch.TYPE a)
		{
			switch (a)
			{
				case Watch.TYPE.BYTE:
					Byte1Radio.Checked = true;
					break;
				case Watch.TYPE.WORD:
					Byte2Radio.Checked = true;
					break;
				case Watch.TYPE.DWORD:
					Byte4Radio.Checked = true;
					break;
				default:
					break;
			}
		}

		private void SetSignedRadio(Watch.DISPTYPE a)
		{
			switch (a)
			{
				case Watch.DISPTYPE.SIGNED:
					SignedRadio.Checked = true;
					break;
				case Watch.DISPTYPE.UNSIGNED:
					UnsignedRadio.Checked = true;
					break;
				case Watch.DISPTYPE.HEX:
					HexRadio.Checked = true;
					break;
				default:
					break;
			}
		}

		public void SetToEditWatch(Watch w, string message)
		{
			//Sets this dialog to Edit Watch and receives default values
			this.Text = message;
			customSetup = true;

			AddressBox.Text = string.Format("{0:X4}", w.Address);
			NotesBox.Text = w.Notes;

			SetTypeRadio(w.Type);
			SetSignedRadio(w.Signed);

			if (w.BigEndian == true)
				BigEndianRadio.Checked = true;
			else
				LittleEndianRadio.Checked = true;
		}

		public void SetEndian(Endian endian)
		{
			if (endian == Endian.Big)
			{
				BigEndianRadio.Checked = true;
			}
			else
			{
				LittleEndianRadio.Checked = true;
			}
		}

		private void RamWatchNewWatch_Load(object sender, EventArgs e)
		{
			if (!customSetup)
			{
				Watch w = new Watch();
				SetTypeRadio(w.Type);
				SetSignedRadio(w.Signed);
				AddressBox.Text = "0000";
			}

			if (location.X > 0 && location.Y > 0)
				this.Location = location;
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			userSelected = false;
			this.Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			//Put user settings in the watch file
			userSelected = true;
			if (InputValidate.IsValidHexNumber(AddressBox.Text))
			{
				watch.Address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
			}
			else
			{
				MessageBox.Show("Not a valid address (enter a valid Hex number)", "Invalid Address", MessageBoxButtons.OK, MessageBoxIcon.Error);
				AddressBox.Focus();
				AddressBox.SelectAll();

				return;
			}

			if (SignedRadio.Checked)
			{
				watch.Signed = Watch.DISPTYPE.SIGNED;
			}
			else if (UnsignedRadio.Checked)
			{
				watch.Signed = Watch.DISPTYPE.UNSIGNED;
			}
			else if (HexRadio.Checked)
			{
				watch.Signed = Watch.DISPTYPE.HEX;
			}

			if (Byte1Radio.Checked)
			{
				watch.Type = Watch.TYPE.BYTE;
			}
			else if (Byte2Radio.Checked)
			{
				watch.Type = Watch.TYPE.WORD;
			}
			else if (Byte4Radio.Checked)
			{
				watch.Type = Watch.TYPE.DWORD;
			}

			if (BigEndianRadio.Checked)
			{
				watch.BigEndian = true;
			}
			else if (LittleEndianRadio.Checked)
			{
				watch.BigEndian = false;
			}

			watch.Notes = NotesBox.Text;

			this.Close();
		}

		private void AddressBox_Leave(object sender, EventArgs e)
		{
			AddressBox.Text = AddressBox.Text.Replace(" ", "");
			if (!InputValidate.IsValidHexNumber(AddressBox.Text))
			{
				AddressBox.Focus();
				AddressBox.SelectAll();
				ToolTip t = new ToolTip();
				t.Show("MUst be a valid hexadecimal vaue", AddressBox, 5000);
			}
		}

		private void AddressBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b') return;

			if (!InputValidate.IsValidHexNumber(e.KeyChar))
				e.Handled = true;
		}
	}
}
