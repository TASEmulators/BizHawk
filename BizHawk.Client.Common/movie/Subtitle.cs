using System.Text;

namespace BizHawk.Client.Common
{
	public class Subtitle
	{
		public Subtitle()
		{
			Message = "";
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

		public string Message { get; set; }
		public int Frame { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public int Duration { get; set; }
		public uint Color { get; set; }

		public override string ToString()
		{
			var sb = new StringBuilder("subtitle ");
			sb
				.Append(Frame).Append(" ")
				.Append(X).Append(" ")
				.Append(Y).Append(" ")
				.Append(Duration).Append(" ")
				.Append($"{Color:X8}").Append(" ")
				.Append(Message);

			return sb.ToString();
		}

		public string ToSubRip(int index, double fps, bool addColorTag)
		{
			var sb = new StringBuilder();

			sb.Append(index.ToString());
			sb.Append("\r\n");

			// Frame timing
			double start = (double)Frame;
			double end = (double)(Frame + Duration);

			int startTime = (int)(start * 1000 / fps);
			int endTime = (int)(end * 1000 / fps);

			var startString = $"{startTime / 3600000:d2}:{(startTime / 60000) % 60:d2}:{(startTime / 1000) % 60:d2},{startTime % 1000:d3}";

			var endString = $"{endTime / 3600000:d2}:{(endTime / 60000) % 60:d2}:{(endTime / 1000) % 60:d2},{endTime % 1000:d3}";

			sb.Append(startString);
			sb.Append(" --> ");
			sb.Append(endString);
			sb.Append("\r\n");

			// TODO: Positioning

			// Color tag open
			if (addColorTag)
			{
				uint rgb = Color & 0x00FFFFFF;
				sb.Append("<font color=\"#");
				sb.Append(rgb.ToString("X6"));
				sb.Append("\">");
			}

			// Message text
			sb.Append(Message.Trim());

			// Color tag closeaddcolortag
			if (addColorTag)
			{
				sb.Append("</font>");
			}

			sb.Append("\r\n");

			// Seperator
			sb.Append("\r\n");

			return sb.ToString();
		}
	}
}
