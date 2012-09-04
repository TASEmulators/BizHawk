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
		public MemoryDomain domain = Global.Emulator.MainMemory;
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

		public void SetDomain(MemoryDomain domain)
		{
			watch.Domain = domain;
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
			{
				this.Location = location;
			}

			PopulateMemoryDomainComboBox();
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
			watch.Domain = domain;
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

		private int GetNumDigits(Int32 i)
		{
			if (i < 0x10000) return 4;
			if (i < 0x100000) return 5;
			if (i < 0x1000000) return 6;
			if (i < 0x10000000) return 7;
			else return 8;
		}

		private void SetAddressBox()
		{
			AddressBox.Text = String.Format("{0:X" +
				GetNumDigits(watch.Address) + "}", watch.Address);
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

		private void DomainComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			domain = Global.Emulator.MemoryDomains[DomainComboBox.SelectedIndex];
			int x = GetNumDigits(domain.Size);
			watch.Address = 0;
			watch.Value = 0;
			watch.Domain = domain;
			SetAddressBox();
			AddressBox.MaxLength = GetNumDigits(domain.Size);
		}
	}
}
