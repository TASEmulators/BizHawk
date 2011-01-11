using System;
using System.Collections;
using System.Windows.Forms;

namespace BizHawk
{
	public class TurboKey
	{
		public void Reset(int downTime, int upTime)
		{
			value = false;
			timer = 0;
			this.upTime = upTime;
			this.downTime = downTime;
		}

		public void Tick(bool down)
		{
			if (!down)
			{
				Reset(downTime, upTime);
				return;
			}

			timer++;

			value = true;
			if (timer > downTime)
				value = false;
			if(timer > (upTime+downTime))
			{
				timer = 0;
				value = true;
			}
		}

		public bool value;
		int upTime, downTime;
		int timer;
	}
}