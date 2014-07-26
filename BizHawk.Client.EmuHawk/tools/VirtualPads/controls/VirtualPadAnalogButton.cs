using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadAnalogButton : UserControl, IVirtualPadControl
	{
		private string _displayName = string.Empty;
		private int _maxValue;
		private bool _programmaticallyChangingValue;
		private bool _readonly;

		private bool _isSet = false;
		private bool IsSet
		{
			get
			{
				return _isSet;
			}

			set
			{
				_isSet = value;
				ValueLabel.ForeColor = DisplayNameLabel.ForeColor = _isSet ? SystemColors.HotTrack : SystemColors.WindowText;
			}
		}

		public VirtualPadAnalogButton()
		{
			InitializeComponent();
		}

		#region IVirtualPadControl Implementation

		public void UpdateValues()
		{
			if (AnalogTrackBar.Value != (int)Global.StickyXORAdapter.GetFloat(Name))
			{
				RefreshWidgets();
			}
		}

		public void Clear()
		{
			Global.StickyXORAdapter.Unset(Name);
			IsSet = false;
		}

		public void Set(IController controller)
		{
			var newVal = (int)controller.GetFloat(Name);
			var changed = AnalogTrackBar.Value != newVal;
			if (changed)
			{
				CurrentValue = newVal;
			}
		}

		public bool ReadOnly
		{
			get
			{
				return _readonly;
			}

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

		#endregion

		public void Bump(int? x)
		{
			if (x.HasValue)
			{
				CurrentValue = CurrentValue + x.Value;
			}
		}

		private void VirtualPadAnalogButton_Load(object sender, EventArgs e)
		{
			DisplayNameLabel.Text = DisplayName;
			ValueLabel.Text = AnalogTrackBar.Value.ToString();
			
		}

		public string DisplayName
		{
			get
			{
				return _displayName;
			}

			set
			{
				_displayName = value ?? string.Empty;
				if (DisplayNameLabel != null)
				{
					DisplayNameLabel.Text = _displayName;
				}
			}
		}

		public int MaxValue
		{
			get
			{
				return _maxValue;
			}

			set
			{
				_maxValue = value;
				if (AnalogTrackBar != null)
				{
					AnalogTrackBar.Maximum = _maxValue;
					AnalogTrackBar.TickFrequency = _maxValue / 10;
				}
			}
		}

		public int CurrentValue
		{
			get
			{
				return AnalogTrackBar.Value;
			}

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
				Global.StickyXORAdapter.SetFloat(Name, AnalogTrackBar.Value);
			}
		}

		private void RefreshWidgets()
		{
			if (!_isSet)
			{
				_programmaticallyChangingValue = true;
				AnalogTrackBar.Value = (int)Global.StickyXORAdapter.GetFloat(Name);
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
