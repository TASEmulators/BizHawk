#nullable enable

using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	public static class DrawingExtensions
	{
		public static T GetMutableCopy<T>(this T b)
			where T : Brush
			=> (T) b.Clone();

		public static Pen GetMutableCopy(this Pen p)
			=> (Pen) p.Clone();
	}
}
