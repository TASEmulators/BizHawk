using System;
using System.Windows.Forms;
using System.Drawing;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadAnalogButton : UserControl, IVirtualPadControl
	{
		private string _displayName = string.Empty;
		private int _maxValue = 0;

		public VirtualPadAnalogButton()
		{
			InitializeComponent();
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

		// TODO
		public void Clear()
		{
		}

		private void AnalogTrackBar_ValueChanged(object sender, EventArgs e)
		{
			ValueLabel.Text = AnalogTrackBar.Value.ToString();
			Refresh();
			Global.StickyXORAdapter.SetFloat(Name, AnalogTrackBar.Value);
		}

		
	}
}
