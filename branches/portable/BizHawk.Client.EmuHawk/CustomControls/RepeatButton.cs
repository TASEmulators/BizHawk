using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	// http://www.codeproject.com/Articles/2130/NET-port-of-Joe-s-AutoRepeat-Button-class
	public class RepeatButton : Button
	{
		private Timer m_timer;
		private bool down = false;
		private bool once = false;
		private int m_initdelay = 1000;
		private int m_repdelay = 400;

		public RepeatButton()
		{
			this.MouseUp +=
				new MouseEventHandler(RepeatButton_MouseUp);
			this.MouseDown +=
				new MouseEventHandler(RepeatButton_MouseDown);

			m_timer = new Timer();
			m_timer.Tick += new EventHandler(timerproc);
			m_timer.Enabled = false;
		}

		private void timerproc(object o1, EventArgs e1)
		{
			m_timer.Interval = m_repdelay;
			if (down)
			{
				once = true;
				this.PerformClick();
			}

		}

		protected override void OnClick(EventArgs e)
		{
			if (!once || down)
				base.OnClick(e);
		}



		private void RepeatButton_MouseDown(object sender,
			System.Windows.Forms.MouseEventArgs e)
		{
			m_timer.Interval = m_initdelay;
			m_timer.Enabled = true;
			down = true;
		}

		private void RepeatButton_MouseUp(object sender,
			System.Windows.Forms.MouseEventArgs e)
		{
			m_timer.Enabled = false;
			down = false;
		}

		public int InitialDelay
		{
			get
			{
				return m_initdelay;
			}
			set
			{
				m_initdelay = value;
				if (m_initdelay < 10)
					m_initdelay = 10;
			}
		}

		public int RepeatDelay
		{
			get
			{
				return m_repdelay;
			}
			set
			{
				m_repdelay = value;
				if (m_repdelay < 10)
					m_repdelay = 10;
			}
		}

	}
}