using System.Drawing;
using System.Windows.Forms;
using BizHawk.Common.NumberExtensions;
using static BizHawk.Client.Common.DisplayManagerBase;

namespace BizHawk.Client.EmuHawk
{
	public static class UIHelper
	{
		private static SizeF AutoScaleCurrentSize { get; } = GetCurrentAutoScaleSize(AutoScaleMode.Font);

		public static float AutoScaleFactorX { get; } = AutoScaleCurrentSize.Width / 6F;

		public static float AutoScaleFactorY { get; } = AutoScaleCurrentSize.Height / 13F;

		public static float DpiFactor { get; } = GetCurrentAutoScaleSize(AutoScaleMode.Dpi).Width / DEFAULT_DPI;

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

		private static SizeF GetCurrentAutoScaleSize(AutoScaleMode mode)
		{
			using var form = new Form { AutoScaleMode = mode };
			return form.CurrentAutoScaleDimensions;
		}

		public static int ScaleDpi(int size)
		{
			return (size * DpiFactor).RoundToInt();
		}

		public static Size ScaleDpi(Size size)
		{
			return new Size(ScaleDpi(size.Width), ScaleDpi(size.Height));
		}

		public static Point UnscaleDpi(Point p)
		{
			return new Point((p.X / DpiFactor).RoundToInt(), (p.Y / DpiFactor).RoundToInt());
		}

		public static int UnscaleX(int size)
			=> (int) Math.Round(size / AutoScaleFactorX);
	}
}
