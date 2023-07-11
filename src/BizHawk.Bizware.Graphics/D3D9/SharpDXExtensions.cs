using System.Drawing;

using BizHawk.Bizware.BizwareGL;

using SharpDX.Mathematics.Interop;

namespace BizHawk.Bizware.Graphics
{
	internal static class SharpDXExtensions
	{
		// SharpDX RawMatrix and BizwareGL Matrix are identical in structure
		public static RawMatrix ToSharpDXMatrix(this Matrix4 m, bool transpose)
		{
			// Transpose call could be inlined to reduce 2 sets of copies to 1
			if (transpose)
			{
				m = Matrix4.Transpose(in m);
			}

			return new()
			{
				M11 = m.Row0.X, M12 = m.Row0.Y, M13 = m.Row0.Z, M14 = m.Row0.W,
				M21 = m.Row1.X, M22 = m.Row1.Y, M23 = m.Row1.Z, M24 = m.Row1.W,
				M31 = m.Row2.X, M32 = m.Row2.Y, M33 = m.Row2.Z, M34 = m.Row2.W,
				M41 = m.Row3.X, M42 = m.Row3.Y, M43 = m.Row3.Z, M44 = m.Row3.W
			};
		}

		public static RawVector2 ToSharpDXVector2(this Vector2 v)
			=> new(v.X, v.Y);

		public static RawVector4 ToSharpDXVector4(this Vector4 v)
			=> new(v.X, v.Y, v.Z, v.W);

		public static RawColorBGRA ToSharpDXColor(this Color c)
			=> new(c.B, c.G, c.R, c.A);
	}
}
