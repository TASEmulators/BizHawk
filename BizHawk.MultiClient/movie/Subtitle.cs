using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
	class Subtitle
	{
		public string Message;
		public int Frame;
		public int X;
		public int Y;
		public int Duration;

		public Subtitle()
		{
			Message = "";
			X = 0;
			Y = 0;
			Duration = 0;
			Frame = 0;
		}

		public Subtitle(string message, int x, int y, int dur, int frame)
		{
			Message = message;
			Frame = frame;
			X = x;
			Y = y;
			Duration = dur;
		}
	}
}
