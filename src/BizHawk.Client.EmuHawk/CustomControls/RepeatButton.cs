using System;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	// http://www.codeproject.com/Articles/2130/NET-port-of-Joe-s-AutoRepeat-Button-class
	public class RepeatButton : Button
	{
		private readonly Timer _mTimer;
		private bool _down;
		private bool _once;
		private int _mInitDelay = 1000;
		private int _mRepeatDelay = 400;

		public RepeatButton()
		{
			MouseUp += RepeatButton_MouseUp;
			MouseDown += RepeatButton_MouseDown;

			_mTimer = new Timer();
			_mTimer.Tick += TimerProcess;
			_mTimer.Enabled = false;
		}

		private void TimerProcess(object o1, EventArgs e1)
		{
			_mTimer.Interval = _mRepeatDelay;
			if (_down)
			{
				_once = true;
				PerformClick();
			}

		}

		protected override void OnClick(EventArgs e)
		{
			if (!_once || _down)
			{
				base.OnClick(e);
			}
		}

		private void RepeatButton_MouseDown(object sender, MouseEventArgs e)
		{
			_mTimer.Interval = _mInitDelay;
			_mTimer.Enabled = true;
			_down = true;
		}

		private void RepeatButton_MouseUp(object sender, MouseEventArgs e)
		{
			_mTimer.Enabled = false;
			_down = false;
		}

		public int InitialDelay
		{
			get => _mInitDelay;
			set
			{
				_mInitDelay = value;
				if (_mInitDelay < 10)
				{
					_mInitDelay = 10;
				}
			}
		}

		public int RepeatDelay
		{
			get => _mRepeatDelay;
			set
			{
				_mRepeatDelay = value;
				if (_mRepeatDelay < 10)
				{
					_mRepeatDelay = 10;
				}
			}
		}

	}
}