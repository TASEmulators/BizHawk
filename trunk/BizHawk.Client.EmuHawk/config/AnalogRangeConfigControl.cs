using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class AnalogRangeConfigControl : UserControl
	{
		private bool _supressChange = false;

		public AnalogRangeConfigControl()
		{
			InitializeComponent();
		}

		private void AnalogRangeConfigControl_Load(object sender, EventArgs e)
		{
			AnalogRange.ChangeCallback = AnalogControlChanged;
		}

		private void XNumeric_ValueChanged(object sender, EventArgs e)
		{
			_supressChange = true;
			AnalogRange.MaxX = (int)XNumeric.Value;
			_supressChange = false;
		}

		private void YNumeric_ValueChanged(object sender, EventArgs e)
		{
			_supressChange = true;
			AnalogRange.MaxY = (int)YNumeric.Value;
			_supressChange = false;
		}

		private void RadialCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			_supressChange = true;
			AnalogRange.Radial = RadialCheckbox.Checked;
			_supressChange = false;
		}

		private void AnalogControlChanged()
		{
			if (!_supressChange)
			{
				XNumeric.Value = AnalogRange.MaxX;
				YNumeric.Value = AnalogRange.MaxY;
				RadialCheckbox.Checked = AnalogRange.Radial;
			}
		}
	}
}
