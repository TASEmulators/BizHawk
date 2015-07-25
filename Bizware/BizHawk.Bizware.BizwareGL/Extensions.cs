using System;
using System.Drawing;
using System.Text;

using OpenTK;
using OpenTK.Graphics;

namespace BizHawk.Bizware.BizwareGL
{
	public static class BizwareGLExtensions
	{
		public static Vector2 ToVector2(this Size size)
		{
			return new Vector2(size.Width, size.Height);
		}
		public static PointF ToSDPointf(this Vector3 v)
		{
			return new PointF(v.X, v.Y);
		}
	}
}