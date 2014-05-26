using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class AddBreakpointDialog : Form
	{
		public AddBreakpointDialog()
		{
			InitializeComponent();
		}

		public BreakpointType BreakType
		{
			get
			{
				if (ReadRadio.Checked)
				{
					return BreakpointType.Read;
				}

				if (WriteRadio.Checked)
				{
					return BreakpointType.Write;
				}

				if (ExecuteRadio.Checked)
				{
					return BreakpointType.Execute;
				}

				return BreakpointType.Read;
			}
		}

		public uint Address
		{
			get { return (uint)AddressBox.ToRawInt().Value; }
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

	public enum BreakpointType { Read, Write, Execute }
}
