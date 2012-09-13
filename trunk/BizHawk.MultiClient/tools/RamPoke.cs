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
		public Watch watch = new Watch();
		public MemoryDomain domain = Global.Emulator.MainMemory;
		public Point location = new Point();

		public RamPoke()
		{
			InitializeComponent();
		}

		public void SetWatchObject(Watch w)
		{
			PopulateMemoryDomainComboBox();
			watch = new Watch(w);
			domain = w.Domain;
		}

		private void RamPoke_Load(object sender, EventArgs e)
		{
			if (watch.Address == 0)
				PopulateMemoryDomainComboBox();
			SetTypeRadio(watch.Type);
			SetSignedRadio(watch.Signed);
			if (watch.Signed == Watch.DISPTYPE.HEX)
			{
				ValueHexLabel.Text = "0x";
			}
			else
			{
				ValueHexLabel.Text = "";
			}

			if (watch.BigEndian == true)
			{
				BigEndianRadio.Checked = true;
			}
			else
			{
				LittleEndianRadio.Checked = true;
			}

			SetValueBox();
			SetAddressBox();

			AddressBox.MaxLength = GetNumDigits(domain.Size);
			ValueBox.MaxLength = GetValueNumDigits();

			if (location.X > 0 && location.Y > 0)
				this.Location = location;

			UpdateTitleText();
			SetDomainSelection();
			FormatValue();
		}

		private void SetValueBox()
		{
			if (HexRadio.Checked)
				ValueBox.Text = String.Format("{0:X" +
					GetValueNumDigits() + "}", watch.Value);
			else
				ValueBox.Text = watch.Value.ToString();
		}

		private void SetAddressBox()
		{
			AddressBox.Text = String.Format("{0:X" +
				GetNumDigits(watch.Address) + "}", watch.Address);
		}

		private void UpdateTitleText()
		{
			Text = "Ram Poke - " + domain.ToString();
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

		private void Cancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			//Put user settings in the watch file

			if (InputValidate.IsValidHexNumber(AddressBox.Text))
				watch.Address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
			else
			{
				MessageBox.Show("Invalid Address, must be a valid hex number", "Invalid Address", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

			int? x = GetSpecificValue();
			if (x == null)
			{
				MessageBox.Show("Missing or invalid value", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Error);
				ValueBox.Focus();
				ValueBox.SelectAll();
				return;
			}
			else
			{
				watch.Value = int.Parse(ValueBox.Text);
			}
			watch.Domain = domain;
			watch.PokeAddress();

			string value;
			if (HexRadio.Checked)
				value = "0x" + String.Format("{0:X" + GetValueNumDigits() + "}", watch.Value);
			else
				value = watch.Value.ToString();
			string address = String.Format("{0:X" + GetNumDigits(domain.Size).ToString()
				+ "}", watch.Address);


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

		private Watch.DISPTYPE GetDataType()
		{
			if (SignedRadio.Checked)
			{
				return Watch.DISPTYPE.SIGNED;
			}
			if (UnsignedRadio.Checked)
			{
				return Watch.DISPTYPE.UNSIGNED;
			}
			if (HexRadio.Checked)
			{
				return Watch.DISPTYPE.HEX;
			}
			else
			{
				return Watch.DISPTYPE.UNSIGNED;    //Just in case
			}
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
				case Watch.DISPTYPE.UNSIGNED:
					if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch.DISPTYPE.SIGNED:
					if (!InputValidate.IsValidSignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch.DISPTYPE.HEX:
					if (!InputValidate.IsValidHexNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
			}
		}

		private Watch.TYPE GetDataSize()
		{
			if (Byte1Radio.Checked)
			{
				return Watch.TYPE.BYTE;
			}
			else if (Byte2Radio.Checked)
			{
				return Watch.TYPE.WORD;
			}
			else if (Byte4Radio.Checked)
			{
				return Watch.TYPE.DWORD;
			}
			else
			{
				return Watch.TYPE.BYTE;
			}
		}

		private int? GetSpecificValue()
		{
			if (ValueBox.Text == "" || ValueBox.Text == "-") return 0;
			bool i = false;
			switch (GetDataType())
			{
				case Watch.DISPTYPE.UNSIGNED:
					i = InputValidate.IsValidUnsignedNumber(ValueBox.Text);
					if (!i)
					{
						return null;
					}
					else
					{
						return (int)Int64.Parse(ValueBox.Text); //Note: 64 to be safe
					}
				case Watch.DISPTYPE.SIGNED:
					i = InputValidate.IsValidSignedNumber(ValueBox.Text);
					if (!i)
					{
						return null;
					}
					else
					{
						return (int)Int64.Parse(ValueBox.Text);
					}
				case Watch.DISPTYPE.HEX:
					i = InputValidate.IsValidHexNumber(ValueBox.Text);
					if (!i)
					{
						return null;
					}
					else
					{
						return (int)Int64.Parse(ValueBox.Text, NumberStyles.HexNumber);
					}
			}
			return null;
		}

		private void HexRadio_Click(object sender, EventArgs e)
		{
			ValueHexLabel.Text = "0x";
			ValueBox.MaxLength = GetValueNumDigits();
			watch.Signed = Watch.DISPTYPE.HEX;
			FormatValue();
		}

		private void UnsignedRadio_Click(object sender, EventArgs e)
		{
			ValueHexLabel.Text = "";
			ValueBox.MaxLength = GetValueNumDigits();
			watch.Signed = Watch.DISPTYPE.UNSIGNED;
			FormatValue();
		}

		private void SignedRadio_Click(object sender, EventArgs e)
		{
			ValueHexLabel.Text = "";
			ValueBox.MaxLength = GetValueNumDigits();
			watch.Signed = Watch.DISPTYPE.SIGNED;
			FormatValue();
		}

		private int GetValueNumDigits()
		{
			switch (GetDataSize())
			{
				default:
				case Watch.TYPE.BYTE:
					if (HexRadio.Checked) return 2;
					else if (UnsignedRadio.Checked) return 3;
					else return 4;
				case Watch.TYPE.WORD:
					if (HexRadio.Checked) return 4;
					else if (UnsignedRadio.Checked) return 5;
					else return 6;
				case Watch.TYPE.DWORD:
					if (HexRadio.Checked) return 8;
					else if (UnsignedRadio.Checked) return 10;
					else return 11;
			}
		}

		private int GetNumDigits(Int32 i)
		{
			if (i < 0x10000) return 4;
			if (i < 0x100000) return 5;
			if (i < 0x1000000) return 6;
			if (i < 0x10000000) return 7;
			else return 8;
		}

		private void PopulateMemoryDomainComboBox()
		{
			DomainComboBox.Items.Clear();
			if (Global.Emulator.MemoryDomains.Count > 0)
			{
				for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
				{
					string str = Global.Emulator.MemoryDomains[x].ToString();
					DomainComboBox.Items.Add(str);
				}
			}
			SetDomainSelection();
		}

		private void DomainComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			domain = Global.Emulator.MemoryDomains[DomainComboBox.SelectedIndex];
			UpdateTitleText();
			int x = GetNumDigits(domain.Size);
			watch.Address = 0;
			watch.Value = 0;
			SetAddressBox();
			SetValueBox();
			AddressBox.MaxLength = GetNumDigits(domain.Size);
		}

		private void SetDomainSelection()
		{
			//Counts should always be the same, but just in case, let's check
			int max;
			if (Global.Emulator.MemoryDomains.Count < DomainComboBox.Items.Count)
				max = Global.Emulator.MemoryDomains.Count;
			else
				max = DomainComboBox.Items.Count;

			for (int x = 0; x < max; x++)
			{
				if (domain.ToString() == DomainComboBox.Items[x].ToString())
					DomainComboBox.SelectedIndex = x;
			}
		}

		private void FormatValue()
		{
			ValueBox.Text = watch.ValueString;
		}
	}
}
