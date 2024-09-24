using System.Drawing;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadAnalogButton : UserControl, IVirtualPadControl
	{
		private readonly StickyHoldController _stickyHoldController;
		private bool _programmaticallyChangingValue;
		private bool _readonly;

		private bool _isSet = false;
		private bool IsSet
		{
			get => _isSet;
			set
			{
				_isSet = value;
				ValueLabel.ForeColor = DisplayNameLabel.ForeColor = _isSet ? SystemColors.HotTrack : SystemColors.WindowText;
			}
		}

		public VirtualPadAnalogButton(
			StickyHoldController stickyHoldController,
			string name,
			string displayName,
			int minValue,
			int maxValue,
			Orientation orientation)
		{
			_stickyHoldController = stickyHoldController;

			InitializeComponent();

			// Name, AnalogTrackBar, DisplayNameLabel, and ValueLabel are now assigned
			Name = name;
			var trackbarWidth = Size.Width - 15;
			int trackbarHeight;
			if ((AnalogTrackBar.Orientation = orientation) == Orientation.Vertical)
			{
				AnalogTrackBar.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
				trackbarHeight = Size.Height - 30;
				ValueLabel.Top = Size.Height / 2;
			}
			else
			{
				AnalogTrackBar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
				trackbarHeight = Size.Height - 15;
			}
			AnalogTrackBar.Size = new Size(trackbarWidth, trackbarHeight);

			AnalogTrackBar.Minimum = minValue;
			AnalogTrackBar.Maximum = maxValue;

			// try to base it on the width, lets make a tick every 10 pixels at the minimum
			// yo none of this makes any sense --yoshi
			var range = maxValue - minValue + 1;
			var canDoTicks = Math.Min(Math.Max(2, trackbarWidth / 10), range);
			AnalogTrackBar.TickFrequency = range / Math.Max(1, canDoTicks);

			DisplayNameLabel.Text = displayName ?? string.Empty;
			ValueLabel.Text = AnalogTrackBar.Value.ToString();
		}

		public void UpdateValues()
		{
			if (AnalogTrackBar.Value != _stickyHoldController.AxisValue(Name))
			{
				RefreshWidgets();
			}
		}

		public void Clear()
		{
			_stickyHoldController.SetAxisHold(Name, null);
			IsSet = false;
		}

		public void Set(IController controller)
		{
			var newVal = controller.AxisValue(Name);
			var changed = AnalogTrackBar.Value != newVal;
			if (changed)
			{
				CurrentValue = newVal;
			}
		}

		public bool ReadOnly
		{
			get => _readonly;
			set
			{
				if (_readonly != value)
				{
					AnalogTrackBar.Enabled =
						DisplayNameLabel.Enabled =
						ValueLabel.Enabled =
						!value;

					_readonly = value;
				
					Refresh();
				}
			}
		}

		public void Bump(int? x)
		{
			if (x.HasValue)
			{
				CurrentValue += x.Value;
			}
		}

		public int CurrentValue
		{
			get => AnalogTrackBar.Value;
			set
			{
				int val;
				if (value > AnalogTrackBar.Maximum)
				{
					val = AnalogTrackBar.Maximum;
				}
				else if (value < AnalogTrackBar.Minimum)
				{
					val = AnalogTrackBar.Minimum;
				}
				else
				{
					val = value;
				}

				IsSet = true;

				_programmaticallyChangingValue = true;
				AnalogTrackBar.Value = val;
				ValueLabel.Text = AnalogTrackBar.Value.ToString();
				_programmaticallyChangingValue = false;
			}
		}

		private void AnalogTrackBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValue)
			{
				CurrentValue = AnalogTrackBar.Value;
				_stickyHoldController.SetAxisHold(Name, AnalogTrackBar.Value);
			}
		}

		private void RefreshWidgets()
		{
			if (!_isSet)
			{
				_programmaticallyChangingValue = true;
				AnalogTrackBar.Value = _stickyHoldController.AxisValue(Name);
				ValueLabel.Text = AnalogTrackBar.Value.ToString();
				_programmaticallyChangingValue = false;
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			RefreshWidgets();
			base.OnPaint(e);
		}
	}
}
