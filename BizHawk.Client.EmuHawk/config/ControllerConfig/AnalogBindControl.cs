using System;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class AnalogBindControl : UserControl
	{
		private AnalogBindControl()
		{
			InitializeComponent();
		}

		public AnalogBindControl(string buttonName, AnalogBind bind)
			: this()
		{
			_bind = bind;
			ButtonName = buttonName;
			labelButtonName.Text = buttonName;
			trackBarSensitivity.Value = (int)(bind.Mult * 10.0f);
			trackBarDeadzone.Value = (int)(bind.Deadzone * 20.0f);
			TrackBarSensitivity_ValueChanged(null, null);
			TrackBarDeadzone_ValueChanged(null, null);
			textBox1.Text = bind.Value;
		}

		public string ButtonName { get; }
		public AnalogBind Bind => _bind;

		private AnalogBind _bind;
		private bool _listening;

		private void Timer1_Tick(object sender, EventArgs e)
		{
			string bindValue = Input.Instance.GetNextFloatEvent();
			if (bindValue != null)
			{
				timer1.Stop();
				_listening = false;
				_bind.Value = bindValue;
				textBox1.Text = Bind.Value;
				buttonBind.Text = "Bind!";
				Input.Instance.StopListeningForFloatEvents();
			}
		}

		private void ButtonBind_Click(object sender, EventArgs e)
		{
			if (_listening)
			{
				timer1.Stop();
				_listening = false;
				buttonBind.Text = "Bind!";
				Input.Instance.StopListeningForFloatEvents();
			}
			else
			{
				Input.Instance.StartListeningForFloatEvents();
				_listening = true;
				buttonBind.Text = "Cancel!";
				timer1.Start();
			}
		}

		private void TrackBarSensitivity_ValueChanged(object sender, EventArgs e)
		{
			_bind.Mult = trackBarSensitivity.Value / 10.0f;
			labelSensitivity.Text = $"Sensitivity: {Bind.Mult * 100}%";
		}

		private void TrackBarDeadzone_ValueChanged(object sender, EventArgs e)
		{
			_bind.Deadzone = trackBarDeadzone.Value / 20.0f;
			labelDeadzone.Text = $"Deadzone: {Bind.Deadzone * 100}%";
		}

		private void ButtonFlip_Click(object sender, EventArgs e)
		{
			trackBarSensitivity.Value *= -1;
		}

		public void Unbind_Click(object sender, EventArgs e)
		{
			_bind.Value = "";
			textBox1.Text = "";
		}
	}
}
