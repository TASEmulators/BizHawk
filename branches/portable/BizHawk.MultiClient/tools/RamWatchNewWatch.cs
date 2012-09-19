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
		public Watch Watch = new Watch();
		public bool SelectionWasMade = false;
		public Point location = new Point();

		private bool DoNotResetAddress = false;

		public RamWatchNewWatch()
		{
			InitializeComponent();
		}

		public void SetWatch(Watch watch, string message = "New Watch")
		{
			DoNotResetAddress = true; //Hack for the drop down event changing when initializing the drop down
			Watch = new Watch(watch);
			this.Text = message;

			NotesBox.Text = watch.Notes;
			setTypeRadio();
			setSignedRadio();
			setEndianBox();
			setDomainSelection();
			setAddressBox();
		}

		#region Events

		private void RamWatchNewWatch_Load(object sender, EventArgs e)
		{
			if (location.X > 0 && location.Y > 0)
			{
				this.Location = location;
			}

			populateMemoryDomainComboBox();
			DoNotResetAddress = false;
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			SelectionWasMade = false;
			this.Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			//Put user settings in the watch file
			SelectionWasMade = true;
			if (InputValidate.IsValidHexNumber(AddressBox.Text))
			{
				Watch.Address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
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
				Watch.Signed = Watch.DISPTYPE.SIGNED;
			}
			else if (UnsignedRadio.Checked)
			{
				Watch.Signed = Watch.DISPTYPE.UNSIGNED;
			}
			else if (HexRadio.Checked)
			{
				Watch.Signed = Watch.DISPTYPE.HEX;
			}

			if (Byte1Radio.Checked)
			{
				Watch.Type = Watch.TYPE.BYTE;
			}
			else if (Byte2Radio.Checked)
			{
				Watch.Type = Watch.TYPE.WORD;
			}
			else if (Byte4Radio.Checked)
			{
				Watch.Type = Watch.TYPE.DWORD;
			}

			if (BigEndianRadio.Checked)
			{
				Watch.BigEndian = true;
			}
			else if (LittleEndianRadio.Checked)
			{
				Watch.BigEndian = false;
			}

			Watch.Domain = Global.Emulator.MemoryDomains[DomainComboBox.SelectedIndex];
			Watch.Notes = NotesBox.Text;

			this.Close();
		}

		private void AddressBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b' || e.KeyChar == 22)
			{
				return;
			}

			if (!InputValidate.IsValidHexNumber(e.KeyChar))
			{
				e.Handled = true;
			}
		}

		private void AddressBox_Leave(object sender, EventArgs e)
		{
			AddressBox.Text = AddressBox.Text.Replace(" ", "");
			if (!InputValidate.IsValidHexNumber(AddressBox.Text))
			{
				AddressBox.Focus();
				AddressBox.SelectAll();
				ToolTip t = new ToolTip();
				t.Show("Must be a valid hexadecimal vaue", AddressBox, 5000);
			}
			else
			{
				try
				{
					Watch.Address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
					AddressBox.Text = String.Format("{0:X" + getNumDigits(Watch.Domain.Size - 1) + "}", Watch.Address);
				}
				catch
				{
					//Do nothing
				}
			}
		}

		private void DomainComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!DoNotResetAddress)
			{
				Watch.Domain = Global.Emulator.MemoryDomains[DomainComboBox.SelectedIndex];
				int x = getNumDigits(Watch.Domain.Size);
				Watch.Address = 0;
				Watch.Value = 0;
				setAddressBox();
				AddressBox.MaxLength = getNumDigits(Watch.Domain.Size);
			}
		}

		#endregion

		#region Helpers

		private void setTypeRadio()
		{
			switch (Watch.Type)
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

		private void setEndianBox()
		{
			if (Watch.BigEndian == true)
			{
				BigEndianRadio.Checked = true;
			}
			else
			{
				LittleEndianRadio.Checked = true;
			}
		}

		private void setDomainSelection()
		{
			//Counts should always be the same, but just in case, let's check
			int max;
			if (Global.Emulator.MemoryDomains.Count < DomainComboBox.Items.Count)
			{
				max = Global.Emulator.MemoryDomains.Count;
			}
			else
			{
				max = DomainComboBox.Items.Count;
			}

			for (int i = 0; i < max; i++)
			{
				if (Watch.Domain.ToString() == DomainComboBox.Items[i].ToString())
				{
					DomainComboBox.SelectedIndex = i;
				}
			}
		}

		private void populateMemoryDomainComboBox()
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
			setDomainSelection();
		}

		private void setAddressBox()
		{
			AddressBox.Text = String.Format("{0:X" + getNumDigits(Watch.Domain.Size - 1) + "}", Watch.Address);
		}

		private void setSignedRadio()
		{
			switch (Watch.Signed)
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

		private int getNumDigits(Int32 i)
		{
			if (i < 0x10000) return 4;
			if (i < 0x1000000) return 6;
			if (i < 0x10000000) return 7;
			else return 8;
		}

		#endregion
	}
}
