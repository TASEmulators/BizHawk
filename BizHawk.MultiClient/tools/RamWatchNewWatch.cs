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

		private void SetTypeRadio(atype a)
		{
			switch (a)
			{
				case atype.BYTE:
					Byte1Radio.Checked = true;
					break;
				case atype.WORD:
					Byte2Radio.Checked = true;
					break;
				case atype.DWORD:
					Byte4Radio.Checked = true;
					break;
				default:
					break;
			}
		}

		private void SetSignedRadio(asigned a)
		{
			switch (a)
			{
				case asigned.SIGNED:
					SignedRadio.Checked = true;
					break;
				case asigned.UNSIGNED:
					UnsignedRadio.Checked = true;
					break;
				case asigned.HEX:
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

			AddressBox.Text = string.Format("{0:X4}", w.address);
			NotesBox.Text = w.notes;

			SetTypeRadio(w.type);
			SetSignedRadio(w.signed);

			if (w.bigendian == true)
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
				SetTypeRadio(w.type);
				SetSignedRadio(w.signed);
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
				watch.address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
			else
			{
				MessageBox.Show("Not a valid address (enter a valid Hex number)", "Invalid Address", MessageBoxButtons.OK, MessageBoxIcon.Error);
				AddressBox.Focus();
				AddressBox.SelectAll();

				return;
			}

			if (SignedRadio.Checked)
				watch.signed = asigned.SIGNED;
			else if (UnsignedRadio.Checked)
				watch.signed = asigned.UNSIGNED;
			else if (HexRadio.Checked)
				watch.signed = asigned.HEX;

			if (Byte1Radio.Checked)
				watch.type = atype.BYTE;
			else if (Byte2Radio.Checked)
				watch.type = atype.WORD;
			else if (Byte4Radio.Checked)
				watch.type = atype.DWORD;

			if (BigEndianRadio.Checked)
				watch.bigendian = true;
			else if (LittleEndianRadio.Checked)
				watch.bigendian = false;

			watch.notes = NotesBox.Text;

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
