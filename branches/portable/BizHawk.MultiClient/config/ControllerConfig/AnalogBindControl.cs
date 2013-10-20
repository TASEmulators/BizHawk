using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class AnalogBindControl : UserControl
	{
		private AnalogBindControl()
		{
			InitializeComponent();
		}

		public string ButtonName;
		public Config.AnalogBind Bind;
		bool listening = false;

		public AnalogBindControl(string ButtonName, Config.AnalogBind Bind)
			: this()
		{
			this.Bind = Bind;
			this.ButtonName = ButtonName;
			labelButtonName.Text = ButtonName;
			trackBarSensitivity.Value = (int)(Bind.Mult * 10.0f);
			trackBarDeadzone.Value = (int)(Bind.Deadzone * 20.0f);
			trackBarSensitivity_ValueChanged(null, null);
			trackBarDeadzone_ValueChanged(null, null);
			textBox1.Text = Bind.Value;
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			string bindval = Input.Instance.GetNextFloatEvent();
			if (bindval != null)
			{
				timer1.Stop();
				listening = false;
				Bind.Value = bindval;
				textBox1.Text = Bind.Value;
				buttonBind.Text = "Bind!";
				Input.Instance.StopListeningForFloatEvents();
			}
		}

		private void buttonBind_Click(object sender, EventArgs e)
		{
			if (listening)
			{
				timer1.Stop();
				listening = false;
				buttonBind.Text = "Bind!";
				Input.Instance.StopListeningForFloatEvents();
			}
			else
			{
				Input.Instance.StartListeningForFloatEvents();
				listening = true;
				buttonBind.Text = "Cancel!";
				timer1.Start();
			}
		}

		private void trackBarSensitivity_ValueChanged(object sender, EventArgs e)
		{
			Bind.Mult = trackBarSensitivity.Value / 10.0f;
			labelSensitivity.Text = String.Format("Sensitivity: {0}", (Bind.Mult*100)) + "%";
		}

		private void trackBarDeadzone_ValueChanged(object sender, EventArgs e)
		{
			Bind.Deadzone = trackBarDeadzone.Value / 20.0f;
			labelDeadzone.Text = String.Format("Deadzone: {0}", (Bind.Deadzone*100)) + "%";
		}
	}
}
