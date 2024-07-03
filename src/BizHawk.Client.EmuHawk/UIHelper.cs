using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public static class UIHelper
	{
		public static AutoScaleMode AutoScaleMode { get; } = AutoScaleMode.Font;

		public static SizeF AutoScaleBaseSize { get; } = new SizeF(6F, 13F);

		public static SizeF AutoScaleCurrentSize { get; } = GetCurrentAutoScaleSize(AutoScaleMode);

		public static float AutoScaleFactorX { get; } = AutoScaleCurrentSize.Width / AutoScaleBaseSize.Width;

		public static float AutoScaleFactorY { get; } = AutoScaleCurrentSize.Height / AutoScaleBaseSize.Height;

		public static SizeF AutoScaleFactor { get; } = new SizeF(AutoScaleFactorX, AutoScaleFactorY);

		public static int ScaleX(int size)
		{
			return (int)Math.Round(size * AutoScaleFactorX);
		}

		public static int ScaleY(int size)
		{
			return (int)Math.Round(size * AutoScaleFactorY);
		}

		public static Point Scale(Point p)
		{
			return new Point(ScaleX(p.X), ScaleY(p.Y));
		}

		public static Size Scale(Size s)
		{
			return new Size(ScaleX(s.Width), ScaleY(s.Height));
		}

		private static SizeF GetCurrentAutoScaleSize(AutoScaleMode autoScaleMode)
		{
			using var form = new Form { AutoScaleMode = autoScaleMode };
			return form.CurrentAutoScaleDimensions;
		}

		public static int UnscaleX(int size)
			=> (int) Math.Round(size / AutoScaleFactorX);
	}
}
