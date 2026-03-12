#nullable enable

using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Client.EmuHawk
{
	public static class DrawingExtensions
	{
#pragma warning disable RCS1224 // don't want extension on nonspecific `Point`
		public static Rectangle? BoundsOfDisplayContaining(Point p)
#pragma warning restore RCS1224
			=> Screen.AllScreens.Select(static scr => scr.WorkingArea)
				.FirstOrNull(rect => rect.Contains(p));

		public static T GetMutableCopy<T>(this T b)
			where T : Brush
			=> (T) b.Clone();

		public static Pen GetMutableCopy(this Pen p)
			=> (Pen) p.Clone();
	}
}
