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

		private void BotControlsRow_Load(object sender, EventArgs e)
		{

		}

		private void ProbabilityUpDown_ValueChanged(object sender, EventArgs e)
		{
			_programmaticallyChangingValues = true;
			ProbabilitySlider.Value = (int)ProbabilityUpDown.Value;
			_programmaticallyChangingValues = false;
		}

		private void ProbabilitySlider_ValueChanged(object sender, EventArgs e)
		{
			_programmaticallyChangingValues = true;
			ProbabilityUpDown.Value = ProbabilitySlider.Value;
			_programmaticallyChangingValues = false;
		}
	}
}
