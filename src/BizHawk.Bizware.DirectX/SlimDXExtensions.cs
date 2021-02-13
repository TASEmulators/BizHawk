using SlimDX;

namespace BizHawk.Bizware.DirectX
{
	internal static class Extensions
	{
		public static Matrix ToSlimDXMatrix(this OpenTK.Matrix4 m, bool transpose)
		{
			Matrix ret = new()
			{
				M11 = m.Row0.X, M12 = m.Row0.Y, M13 = m.Row0.Z, M14 = m.Row0.W,
				M21 = m.Row1.X, M22 = m.Row1.Y, M23 = m.Row1.Z, M24 = m.Row1.W,
				M31 = m.Row2.X, M32 = m.Row2.Y, M33 = m.Row2.Z, M34 = m.Row2.W,
				M41 = m.Row3.X, M42 = m.Row3.Y, M43 = m.Row3.Z, M44 = m.Row3.W
			};
			// Transpose call could be inlined to reduce 2 sets of copies to 1
			return transpose ? Matrix.Transpose(ret) : ret;
		}

		public static Vector2 ToSlimDXVector2(this OpenTK.Vector2 v) => new(v.X, v.Y);

		public static Vector4 ToSlimDXVector4(this OpenTK.Vector4 v) => new(v.X, v.Y, v.Z, v.W);
	}
}
