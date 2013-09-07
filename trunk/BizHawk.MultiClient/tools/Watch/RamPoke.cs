using System;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public partial class RamPoke : Form
	{
		public Watch_Legacy Watch = new Watch_Legacy();
		public MemoryDomain Domain = Global.Emulator.MainMemory;
		public Point NewLocation = new Point();

		public RamPoke()
		{
			InitializeComponent();
		}

		public void SetWatchObject(Watch_Legacy w)
		{
			PopulateMemoryDomainComboBox();
			Watch = new Watch_Legacy(w);
			Domain = w.Domain;
		}

		private void RamPoke_Load(object sender, EventArgs e)
		{
			if (Watch.Address == 0)
				PopulateMemoryDomainComboBox();
			SetTypeRadio(Watch.Type);
			SetSignedRadio(Watch.Signed);
			if (Watch.Signed == Watch_Legacy.DISPTYPE.HEX)
			{
				ValueHexLabel.Text = "0x";
			}
			else
			{
				ValueHexLabel.Text = "";
			}

			if (Watch.BigEndian)
			{
				BigEndianRadio.Checked = true;
			}
			else
			{
				LittleEndianRadio.Checked = true;
			}

			SetValueBox();
			SetAddressBox();

			AddressBox.MaxLength = GetNumDigits(Domain.Size);
			ValueBox.MaxLength = GetValueNumDigits();

			if (NewLocation.X > 0 && NewLocation.Y > 0)
			{
				Location = NewLocation;
			}

			UpdateTitleText();
			SetDomainSelection();

			SetValueBoxProperties();
		}

		private void SetValueBoxProperties()
		{
			switch (Watch.Signed)
			{
				case Watch_Legacy.DISPTYPE.SIGNED:
					SignedRadio.Checked = true;
					ValueHexLabel.Text = "";
					break;
				case Watch_Legacy.DISPTYPE.UNSIGNED:
					UnsignedRadio.Checked = true;
					ValueHexLabel.Text = "";
					break;
				case Watch_Legacy.DISPTYPE.HEX:
					ValueHexLabel.Text = "0x";
					HexRadio.Checked = true;
					break;
			}

			ValueBox.MaxLength = GetValueNumDigits();
			FormatValue();
		}

		private void HexRadio_Click(object sender, EventArgs e)
		{
			ValueHexLabel.Text = "0x";
			ValueBox.MaxLength = GetValueNumDigits();
			Watch.Signed = Watch_Legacy.DISPTYPE.HEX;
			FormatValue();
		}

		private void UnsignedRadio_Click(object sender, EventArgs e)
		{
			ValueHexLabel.Text = "";
			ValueBox.MaxLength = GetValueNumDigits();
			Watch.Signed = Watch_Legacy.DISPTYPE.UNSIGNED;
			FormatValue();
		}

		private void SignedRadio_Click(object sender, EventArgs e)
		{
			ValueHexLabel.Text = "";
			ValueBox.MaxLength = GetValueNumDigits();
			Watch.Signed = Watch_Legacy.DISPTYPE.SIGNED;
			FormatValue();
		}

		private void SetValueBox()
		{
			if (HexRadio.Checked)
				ValueBox.Text = String.Format("{0:X" +
					GetValueNumDigits() + "}", Watch.Value);
			else
				ValueBox.Text = Watch.Value.ToString();
		}

		private void SetAddressBox()
		{
			AddressBox.Text = String.Format("{0:X" +
				GetNumDigits(Watch.Address) + "}", Watch.Address);
		}

		private void UpdateTitleText()
		{
			Text = "Ram Poke - " + Domain;
		}

		private void SetTypeRadio(Watch_Legacy.TYPE a)
		{
			switch (a)
			{
				case Watch_Legacy.TYPE.BYTE:
					Byte1Radio.Checked = true;
					break;
				case Watch_Legacy.TYPE.WORD:
					Byte2Radio.Checked = true;
					break;
				case Watch_Legacy.TYPE.DWORD:
					Byte4Radio.Checked = true;
					break;
			}
		}

		private void SetSignedRadio(Watch_Legacy.DISPTYPE a)
		{
			switch (a)
			{
				case Watch_Legacy.DISPTYPE.SIGNED:
					SignedRadio.Checked = true;
					break;
				case Watch_Legacy.DISPTYPE.UNSIGNED:
					UnsignedRadio.Checked = true;
					break;
				case Watch_Legacy.DISPTYPE.HEX:
					HexRadio.Checked = true;
					break;
			}
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			//Put user settings in the watch file

			if (InputValidate.IsValidHexNumber(AddressBox.Text))
				Watch.Address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
			else
			{
				MessageBox.Show("Invalid Address, must be a valid hex number", "Invalid Address", MessageBoxButtons.OK, MessageBoxIcon.Error);
				AddressBox.Focus();
				AddressBox.SelectAll();
				return;
			}

			if (SignedRadio.Checked)
			{
				Watch.Signed = Watch_Legacy.DISPTYPE.SIGNED;
			}
			else if (UnsignedRadio.Checked)
			{
				Watch.Signed = Watch_Legacy.DISPTYPE.UNSIGNED;
			}
			else if (HexRadio.Checked)
			{
				Watch.Signed = Watch_Legacy.DISPTYPE.HEX;
			}

			if (Byte1Radio.Checked)
			{
				Watch.Type = Watch_Legacy.TYPE.BYTE;
			}
			else if (Byte2Radio.Checked)
			{
				Watch.Type = Watch_Legacy.TYPE.WORD;
			}
			else if (Byte4Radio.Checked)
			{
				Watch.Type = Watch_Legacy.TYPE.DWORD;
			}

			if (BigEndianRadio.Checked)
			{
				Watch.BigEndian = true;
			}
			else if (LittleEndianRadio.Checked)
			{
				Watch.BigEndian = false;
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
				Watch.TrySetValue(ValueBox.Text);
			}
			Watch.Domain = Domain;
			Watch.PokeAddress();

			string value;
			if (HexRadio.Checked)
				value = "0x" + String.Format("{0:X" + GetValueNumDigits() + "}", Watch.Value);
			else
				value = Watch.Value.ToString();
			string address = String.Format("{0:X" + GetNumDigits(Domain.Size).ToString()
				+ "}", Watch.Address);


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

			switch (Watch.Signed)
			{
				case Watch_Legacy.DISPTYPE.UNSIGNED:
					if (!InputValidate.IsValidUnsignedNumber(ValueBox.Text))
					{
						ValueBox.Focus();
						ValueBox.SelectAll();
						ToolTip t = new ToolTip();
						t.Show("Must be a valid unsigned decimal value", ValueBox, 5000);
					}
					break;
				case Watch_Legacy.DISPTYPE.SIGNED:
					if (!InputValidate.IsValidSignedNumber(ValueBox.Text))
					{
						ValueBox.Focus();
						ValueBox.SelectAll();
						ToolTip t = new ToolTip();
						t.Show("Must be a valid signed decimal value", ValueBox, 5000);
					}
					break;
				case Watch_Legacy.DISPTYPE.HEX:
					if (!InputValidate.IsValidHexNumber(ValueBox.Text))
					{
						ValueBox.Focus();
						ValueBox.SelectAll();
						ToolTip t = new ToolTip();
						t.Show("Must be a valid hexadecimal decimal value", ValueBox, 5000);
					}
					break;
			}
			
		}

		private Watch_Legacy.DISPTYPE GetDataType()
		{
			if (SignedRadio.Checked)
			{
				return Watch_Legacy.DISPTYPE.SIGNED;
			}
			if (UnsignedRadio.Checked)
			{
				return Watch_Legacy.DISPTYPE.UNSIGNED;
			}
			if (HexRadio.Checked)
			{
				return Watch_Legacy.DISPTYPE.HEX;
			}
			else
			{
				return Watch_Legacy.DISPTYPE.UNSIGNED;    //Just in case
			}
		}

		private void ValueBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b') return;

			switch (GetDataType())
			{
				case Watch_Legacy.DISPTYPE.UNSIGNED:
					if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch_Legacy.DISPTYPE.SIGNED:
					if (!InputValidate.IsValidSignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch_Legacy.DISPTYPE.HEX:
					if (!InputValidate.IsValidHexNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
			}
		}

		private Watch_Legacy.TYPE GetDataSize()
		{
			if (Byte1Radio.Checked)
			{
				return Watch_Legacy.TYPE.BYTE;
			}
			else if (Byte2Radio.Checked)
			{
				return Watch_Legacy.TYPE.WORD;
			}
			else if (Byte4Radio.Checked)
			{
				return Watch_Legacy.TYPE.DWORD;
			}
			else
			{
				return Watch_Legacy.TYPE.BYTE;
			}
		}

		private int? GetSpecificValue()
		{
			if (ValueBox.Text == "" || ValueBox.Text == "-") return 0;
			bool i;
			switch (GetDataType())
			{
				case Watch_Legacy.DISPTYPE.UNSIGNED:
					i = InputValidate.IsValidUnsignedNumber(ValueBox.Text);
					if (!i)
					{
						return null;
					}
					else
					{
						return (int)Int64.Parse(ValueBox.Text); //Note: 64 to be safe
					}
				case Watch_Legacy.DISPTYPE.SIGNED:
					i = InputValidate.IsValidSignedNumber(ValueBox.Text);
					if (!i)
					{
						return null;
					}
					else
					{
						return (int)Int64.Parse(ValueBox.Text);
					}
				case Watch_Legacy.DISPTYPE.HEX:
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

		private int GetValueNumDigits()
		{
			switch (GetDataSize())
			{
				default:
				case Watch_Legacy.TYPE.BYTE:
					if (HexRadio.Checked) return 2;
					else if (UnsignedRadio.Checked) return 3;
					else return 4;
				case Watch_Legacy.TYPE.WORD:
					if (HexRadio.Checked) return 4;
					else if (UnsignedRadio.Checked) return 5;
					else return 6;
				case Watch_Legacy.TYPE.DWORD:
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
				foreach (MemoryDomain t in Global.Emulator.MemoryDomains)
				{
					DomainComboBox.Items.Add(t.ToString());
				}
			}
			SetDomainSelection();
		}

		private void DomainComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			Domain = Global.Emulator.MemoryDomains[DomainComboBox.SelectedIndex];
			UpdateTitleText();
			GetNumDigits(Domain.Size);
			Watch.Address = 0;
			Watch.Value = 0;
			SetAddressBox();
			SetValueBox();
			AddressBox.MaxLength = GetNumDigits(Domain.Size);
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
				if (Domain.ToString() == DomainComboBox.Items[x].ToString())
					DomainComboBox.SelectedIndex = x;
			}
		}

		private void FormatValue()
		{
			Watch.Signed = GetDataType();
			Watch.TrySetValue(ValueBox.Text);
			ValueBox.Text = Watch.ValueString;
		}
	}
}
