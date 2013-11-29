using System;
using System.Text;

namespace BizHawk.Client.Common
{
	public class Subtitle
	{
		public string Message { get; set; }
		public int Frame { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public int Duration { get; set; }
		public uint Color { get; set; }

		public Subtitle()
		{
			Message = String.Empty;
			X = 0;
			Y = 0;
			Duration = 120;
			Frame = 0;
			Color = 0xFFFFFFFF;
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

		public override string ToString()
		{
			var sb = new StringBuilder("subtitle ");
			sb
				.Append(Frame).Append(" ")
				.Append(X).Append(" ")
				.Append(Y).Append(" ")
				.Append(Duration).Append(" ")
				.Append(String.Format("{0:X8}", Color)).Append(" ")
				.Append(Message);

			return sb.ToString();
		}
	}
}
