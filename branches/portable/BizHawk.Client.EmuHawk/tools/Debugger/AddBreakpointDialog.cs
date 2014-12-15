using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class AddBreakpointDialog : Form
	{
		public AddBreakpointDialog()
		{
			InitializeComponent();
		}

		public MemoryCallbackType BreakType
		{
			get
			{
				if (ReadRadio.Checked)
				{
					return MemoryCallbackType.Read;
				}

				if (WriteRadio.Checked)
				{
					return MemoryCallbackType.Write;
				}

				if (ExecuteRadio.Checked)
				{
					return MemoryCallbackType.Execute;
				}

				return MemoryCallbackType.Read;
			}
		}

		public uint Address
		{
			get { return (uint)AddressBox.ToRawInt().Value; }
		}

		public int MaxAddressSize
		{
			get
			{
				return AddressBox.MaxLength;
			}

			set
			{
				AddressBox.SetHexProperties(value);
			}
		}

		private void AddButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void AddBreakpointDialog_Load(object sender, EventArgs e)
		{

		}
	}
}
