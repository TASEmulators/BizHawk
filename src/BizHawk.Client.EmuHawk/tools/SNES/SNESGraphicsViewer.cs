using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public class SNESGraphicsViewer : RetainedViewportPanel
	{
		protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
		{
			int x = Location.X;
			int y = Location.Y;
			if (specified.HasFlag(BoundsSpecified.X))
				x = (int)(x * factor.Width);
			if (specified.HasFlag(BoundsSpecified.Y))
				y = (int)(y * factor.Height);
			var pt = new Point(x, y);
			if (pt != Location)
				Location = pt;
		}

		public SNESGraphicsViewer()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			if (!DesignMode)
			{
				BackColor = Color.Transparent;
			}
		}
	}
}
