using System;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class BotControlsRow : UserControl
	{
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
	}
}
