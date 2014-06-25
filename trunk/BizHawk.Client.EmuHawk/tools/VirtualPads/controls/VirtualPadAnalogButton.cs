using System;
using System.Windows.Forms;
using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadAnalogButton : UserControl, IVirtualPadControl
	{
		private string _displayName = string.Empty;
		private int _maxValue = 0;
		private bool _programmaticallyChangingValue = false;

		public VirtualPadAnalogButton()
		{
			InitializeComponent();
		}

		#region IVirtualPadControl Implementation

		public void Clear()
		{
			// Nothing to do
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
			get;
			set; // TODO
		}

		#endregion

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
				_programmaticallyChangingValue = true;
				AnalogTrackBar.Value = value;
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
	}
}
