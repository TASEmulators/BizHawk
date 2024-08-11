using System.Drawing;
using System.Numerics;

namespace BizHawk.Bizware.Graphics
{
	public static class DrawingExtensions
	{
		public static void Deconstruct(this Size size, out int width, out int height)
		{
			width = size.Width;
			height = size.Height;
		}

		public static Vector2 ToVector(this Point point)
			=> new(point.X, point.Y);
	}
}
