using System.Text;

namespace BizHawk.Client.Common
{
	public class Subtitle
	{
		public Subtitle()
		{
			Message = string.Empty;
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
				.Append(string.Format("{0:X8}", Color)).Append(" ")
				.Append(Message);

			return sb.ToString();
		}

		public string ToSubRip(int index, double fps, bool addcolortag)
        {
            var sb = new StringBuilder();

            sb.Append(index.ToString());
            sb.Append("\r\n");

            // Frame timing
            double start = (double)Frame;
            double end = (double)(Frame + Duration);

            int startTime = (int)(start * 1000 / fps);
            int endTime = (int)(end * 1000 / fps);

            var startString = string.Format(
                "{0:d2}:{1:d2}:{2:d2},{3:d3}",
                startTime / 3600000,
                (startTime / 60000) % 60,
                (startTime / 1000) % 60,
                startTime % 1000
                );

            var endString = string.Format(
                "{0:d2}:{1:d2}:{2:d2},{3:d3}",
                endTime / 3600000,
                (endTime / 60000) % 60,
                (endTime / 1000) % 60,
                endTime % 1000
                );

            sb.Append(startString);
            sb.Append(" --> ");
            sb.Append(endString);
            sb.Append("\r\n");

            // TODO: Positioning

            // Color tag open
			if (addcolortag)
			{
				uint rgb = (Color & 0x00FFFFFF);
				sb.Append("<font color=\"#");
				sb.Append(rgb.ToString("X6"));
				sb.Append("\">");
			}

            // Message text
            sb.Append(Message.Trim());

            // Color tag closeaddcolortag
			if (addcolortag)
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
