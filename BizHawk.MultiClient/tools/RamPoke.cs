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
	public partial class RamPoke : Form
	{
		//TODO:
		//If signed/unsigned/hex radios selected, auto-change the value box, and auto limit box length
		//Checked signed/u/h value on RamPoke_Load and set value appopriately
		//Memory domain selection
		//Output message, format number of digits appropriately
		public Watch watch = new Watch();
		public Point location = new Point();

		public RamPoke()
		{
			InitializeComponent();
		}

		public void SetWatchObject(Watch w)
		{
			watch = w;
		}

		private void RamPoke_Load(object sender, EventArgs e)
		{
			SetTypeRadio(watch.type);
			SetSignedRadio(watch.signed);
			if (watch.signed == asigned.HEX)
				ValueHexLabel.Text = "0x";
			else
				ValueHexLabel.Text = "";

			if (watch.bigendian == true)
				BigEndianRadio.Checked = true;
			else
				LittleEndianRadio.Checked = true;
			AddressBox.Text = String.Format("{0:X}", watch.address);


			ValueBox.Text = watch.value.ToString();


			if (location.X > 0 && location.Y > 0)
				this.Location = location;

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

		private void Cancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			//Put user settings in the watch file

			if (InputValidate.IsValidHexNumber(AddressBox.Text))
				watch.address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
			else
			{
				MessageBox.Show("Invalid Address, must be a valid hex number", "Invalid Address", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

			int x = GetSpecificValue();
			if (x == -99999999)
			{
				MessageBox.Show("Missing or invalid value", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Error);
				ValueBox.Focus();
				ValueBox.SelectAll();
				return;
			}
			else
				watch.value = int.Parse(ValueBox.Text);

			watch.PokeAddress(Global.Emulator.MainMemory);

			//TODO: format value based on watch.type
			OutputLabel.Text = watch.value.ToString() + " written to " + String.Format("{0:X}", watch.address);
		}

		private void AddressBox_Leave(object sender, EventArgs e)
		{
			AddressBox.Text = AddressBox.Text.Replace(" ", "");
			if (!InputValidate.IsValidHexNumber(AddressBox.Text))
			{
				AddressBox.Focus();
				AddressBox.SelectAll();
				ToolTip t = new ToolTip();
				t.Show("Must be a valid hexadecimal value", AddressBox, 5000);
			}
		}

		private void ValueBox_Leave(object sender, EventArgs e)
		{
			ValueBox.Text = ValueBox.Text.Replace(" ", "");
			if (!InputValidate.IsValidUnsignedNumber(ValueBox.Text))
			{
				ValueBox.Focus();
				ValueBox.SelectAll();
				ToolTip t = new ToolTip();
				t.Show("Must be a valid unsigned decimal value", ValueBox, 5000);
			}
		}

		private asigned GetDataType()
		{
			if (SignedRadio.Checked)
				return asigned.UNSIGNED;
			if (UnsignedRadio.Checked)
				return asigned.SIGNED;
			if (HexRadio.Checked)
				return asigned.HEX;

			return asigned.UNSIGNED;    //Just in case
		}

		private void AddressBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b') return;

			if (!InputValidate.IsValidHexNumber(e.KeyChar))
				e.Handled = true;
		}

		private void ValueBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b') return;

			switch (GetDataType())
			{
				case asigned.UNSIGNED:
					if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
						e.Handled = true;
					break;
				case asigned.SIGNED:
					if (!InputValidate.IsValidSignedNumber(e.KeyChar))
						e.Handled = true;
					break;
				case asigned.HEX:
					if (!InputValidate.IsValidHexNumber(e.KeyChar))
						e.Handled = true;
					break;
			}
		}

		private atype GetDataSize()
		{
			if (Byte1Radio.Checked)
				return atype.BYTE;
			if (Byte2Radio.Checked)
				return atype.WORD;
			if (Byte4Radio.Checked)
				return atype.DWORD;

			return atype.BYTE;
		}

		private int GetSpecificValue()
		{
			if (ValueBox.Text == "" || ValueBox.Text == "-") return 0;
			bool i = false;
			switch (GetDataType())
			{
				case asigned.UNSIGNED:
					i = InputValidate.IsValidUnsignedNumber(ValueBox.Text);
					if (!i) return -99999999;
					return (int)Int64.Parse(ValueBox.Text); //Note: 64 to be safe since 4 byte values can be entered
				case asigned.SIGNED:
					i = InputValidate.IsValidSignedNumber(ValueBox.Text);
					if (!i) return -99999999;
					return (int)Int64.Parse(ValueBox.Text);
				case asigned.HEX:
					i = InputValidate.IsValidHexNumber(ValueBox.Text);
					if (!i) return -99999999;
					return (int)Int64.Parse(ValueBox.Text, NumberStyles.HexNumber);
			}
			return -99999999; //What are the odds someone wants to search for this value?
		}

		private void HexRadio_Click(object sender, EventArgs e)
		{
			ValueHexLabel.Text = "0x";
		}

		private void UnsignedRadio_Click(object sender, EventArgs e)
		{
			ValueHexLabel.Text = "";
		}

		private void SignedRadio_Click(object sender, EventArgs e)
		{
			ValueHexLabel.Text = "";
		}
	}
}
