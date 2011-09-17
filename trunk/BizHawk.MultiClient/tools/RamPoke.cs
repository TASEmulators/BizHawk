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
		//If signed/unsigned/hex radios selected, auto-change the value box
		//Memory domain selection
		public Watch watch = new Watch();
		public MemoryDomain domain = Global.Emulator.MainMemory;
		public Point location = new Point();

		public RamPoke()
		{
			InitializeComponent();
		}

		public void SetWatchObject(Watch w, MemoryDomain d)
		{
			watch = w;
			domain = d;
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
			AddressBox.Text = String.Format("{0:X" + 
				GetNumDigits(watch.address) + "}", watch.address);

			if (HexRadio.Checked)
				ValueBox.Text = String.Format("{0:X" +
					GetValueNumDigits() + "}", watch.value);
			else
				ValueBox.Text = watch.value.ToString();

			AddressBox.MaxLength = GetNumDigits(domain.Size);
			ValueBox.MaxLength = GetValueNumDigits();

			if (location.X > 0 && location.Y > 0)
				this.Location = location;

			Text = "Ram Poke - " + domain.ToString();
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

			watch.PokeAddress(domain);

			string value;
			if (HexRadio.Checked)
				value = "0x" + String.Format("{0:X" + GetValueNumDigits() + "}", watch.value);
			else
				value = watch.value.ToString();
			string address = String.Format("{0:X" + GetNumDigits(domain.Size).ToString()
				+ "}", watch.address);


			OutputLabel.Text = value + " written to " + address;
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
				return asigned.SIGNED;
			if (UnsignedRadio.Checked)
				return asigned.UNSIGNED;
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
					return (int)Int64.Parse(ValueBox.Text); //Note: 64 to be safe
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
			ValueBox.MaxLength = GetValueNumDigits();
		}

		private void UnsignedRadio_Click(object sender, EventArgs e)
		{
			ValueHexLabel.Text = "";
			ValueBox.MaxLength = GetValueNumDigits();
		}

		private void SignedRadio_Click(object sender, EventArgs e)
		{
			ValueHexLabel.Text = "";
			ValueBox.MaxLength = GetValueNumDigits();
		}

		private int GetValueNumDigits()
		{
			switch (GetDataSize())
			{
				default:
				case atype.BYTE:
					if (HexRadio.Checked) return 2;
					else if (UnsignedRadio.Checked) return 3;
					else return 4;
				case atype.WORD:
					if (HexRadio.Checked) return 4;
					else if (UnsignedRadio.Checked) return 5;
					else return 6;
				case atype.DWORD:
					if (HexRadio.Checked) return 8;
					else if (UnsignedRadio.Checked) return 10;
					else return 11;
			}
		}

		private int GetNumDigits(Int32 i)
		{
			//if (i == 0) return 0;
			//if (i < 0x10) return 1;
			//if (i < 0x100) return 2;
			//if (i < 0x1000) return 3; //adelikat: commenting these out because I decided that regardless of domain, 4 digits should be the minimum
			if (i < 0x10000) return 4;
			if (i < 0x100000) return 5;
			if (i < 0x1000000) return 6;
			if (i < 0x10000000) return 7;
			else return 8;
		}
	}
}
