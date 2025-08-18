using System.Drawing;
using System.Windows.Forms;
using BizHawk.Common.NumberExtensions;
using static BizHawk.Client.Common.DisplayManagerBase;

namespace BizHawk.Client.EmuHawk
{
	public static class UIHelper
	{
		private static SizeF AutoScaleCurrentSize { get; } = GetCurrentAutoScaleSize();

		public static float AutoScaleFactorX { get; } = AutoScaleCurrentSize.Width / DEFAULT_DPI;

		public static float AutoScaleFactorY { get; } = AutoScaleCurrentSize.Height / DEFAULT_DPI;

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

		private static SizeF GetCurrentAutoScaleSize()
		{
			using var form = new Form { AutoScaleMode = AutoScaleMode.Dpi };
			return form.CurrentAutoScaleDimensions;
		}

		public static Point Unscale(Point p)
		{
			return new Point((p.X / AutoScaleFactorX).RoundToInt(), (p.Y / AutoScaleFactorY).RoundToInt());
		}

		public static int UnscaleX(int size)
			=> (int) Math.Round(size / AutoScaleFactorX);
	}
}
