#nullable enable

using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	public static class DrawingExtensions
	{
		public static Pen GetMutableCopy(this Pen p)
			=> (Pen) p.Clone();
	}
}
