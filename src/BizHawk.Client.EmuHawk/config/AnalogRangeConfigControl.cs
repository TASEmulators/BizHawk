using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class AnalogRangeConfigControl : UserControl
	{
		private bool _suppressChange;

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
			_suppressChange = true;
			AnalogRange.MaxX = (int)XNumeric.Value;
			_suppressChange = false;
		}

		private void YNumeric_ValueChanged(object sender, EventArgs e)
		{
			_suppressChange = true;
			AnalogRange.MaxY = (int)YNumeric.Value;
			_suppressChange = false;
		}

		private void RadialCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			_suppressChange = true;
			AnalogRange.Radial = RadialCheckbox.Checked;
			_suppressChange = false;
		}

		private void AnalogControlChanged()
		{
			if (!_suppressChange)
			{
				XNumeric.Value = AnalogRange.MaxX;
				YNumeric.Value = AnalogRange.MaxY;
				RadialCheckbox.Checked = AnalogRange.Radial;
			}
		}
	}
}
