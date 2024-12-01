using System.Windows.Forms;

using BizHawk.Common.NumberExtensions;

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
			get => ButtonNameLabel.Text;
			set => ButtonNameLabel.Text = value;
		}

		public double Probability
		{
			get => ProbabilityUpDown.Value.ConvertToF64();
			set => ProbabilityUpDown.Value = new(value);
		}

		private void ProbabilityUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValues)
			{
				_programmaticallyChangingValues = true;
				ProbabilitySlider.Value = (int)ProbabilityUpDown.Value;
				ProbabilityChangedCallback?.Invoke();
				_programmaticallyChangingValues = false;
			}
		}

		private void ProbabilitySlider_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValues)
			{
				_programmaticallyChangingValues = true;
				ProbabilityUpDown.Value = ProbabilitySlider.Value;
				ProbabilityChangedCallback?.Invoke();
				_programmaticallyChangingValues = false;
			}
		}
	}
}
