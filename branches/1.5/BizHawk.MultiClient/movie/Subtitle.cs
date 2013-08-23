using System;

namespace BizHawk.MultiClient
{
	public class Subtitle
	{
		public string Message;
		public int Frame;
		public int X;
		public int Y;
		public int Duration;
		public uint Color;

		public Subtitle()
		{
			Message = "";
			X = 0;
			Y = 0;
			Duration = 120;
			Frame = 0;
			Color = 0xFFFFFFFF;
		}

		public Subtitle(string message, int x, int y, int dur, int frame, UInt32 color)
		{
			Message = message;
			Frame = frame;
			X = x;
			Y = y;
			Duration = dur;
			Color = color;
		}

		public Subtitle(Subtitle s)
		{
			Message = s.Message;
			Frame = s.Frame;
			X = s.X;
			Y = s.Y;
			Duration = s.Duration;
			Color = s.Color;
		}
	}
}
