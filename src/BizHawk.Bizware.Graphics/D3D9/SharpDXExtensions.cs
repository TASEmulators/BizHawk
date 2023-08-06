using System.Drawing;
using System.Numerics;

using SharpDX.Mathematics.Interop;

namespace BizHawk.Bizware.Graphics
{
	internal static class SharpDXExtensions
	{
		// SharpDX RawMatrix and Numerics Matrix4x4 are identical in structure
		public static RawMatrix ToSharpDXMatrix(this Matrix4x4 m, bool transpose)
		{
			// Transpose call could be inlined to reduce 2 sets of copies to 1
			if (transpose)
			{
				m = Matrix4x4.Transpose(m);
			}

			return new()
			{
				M11 = m.M11, M12 = m.M12, M13 = m.M13, M14 = m.M14,
				M21 = m.M21, M22 = m.M22, M23 = m.M23, M24 = m.M24,
				M31 = m.M31, M32 = m.M32, M33 = m.M33, M34 = m.M34,
				M41 = m.M41, M42 = m.M42, M43 = m.M43, M44 = m.M44
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
