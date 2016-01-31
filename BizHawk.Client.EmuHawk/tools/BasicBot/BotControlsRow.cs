using System;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class BotControlsRow : UserControl
	{
		private bool _programmaticallyChangingValues;

		public BotControlsRow()
		{
			InitializeComponent();
		}

		public Action ProbabilityChangedCallback { get; set; }

		public string ButtonName
		{
			get { return ButtonNameLabel.Text; }
			set { ButtonNameLabel.Text = value; }
		}

		public double Probability
		{
			get { return (double)ProbabilityUpDown.Value; }
			set { ProbabilityUpDown.Value = (decimal)value; }
		}

		private void ProbabilityUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValues)
			{
				_programmaticallyChangingValues = true;
				ProbabilitySlider.Value = (int)ProbabilityUpDown.Value;
				ChangedCallback();
				_programmaticallyChangingValues = false;
			}
		}

		private void ProbabilitySlider_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValues)
			{
				_programmaticallyChangingValues = true;
				ProbabilityUpDown.Value = ProbabilitySlider.Value;
				ChangedCallback();
				_programmaticallyChangingValues = false;
			}
		}

		private void ChangedCallback()
		{
			if (ProbabilityChangedCallback != null)
			{
				ProbabilityChangedCallback();
			}
		}
	}
}
