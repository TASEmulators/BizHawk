namespace BizHawk.Bizware.DirectX
{
	internal static class Extensions
	{
		public static global::SlimDX.Matrix ToSlimDXMatrix(this OpenTK.Matrix4 m, bool transpose)
		{
			global::SlimDX.Matrix ret = new global::SlimDX.Matrix
			{
				M11 = m.M11,
				M12 = m.M12,
				M13 = m.M13,
				M14 = m.M14,
				M21 = m.M21,
				M22 = m.M22,
				M23 = m.M23,
				M24 = m.M24,
				M31 = m.M31,
				M32 = m.M32,
				M33 = m.M33,
				M34 = m.M34,
				M41 = m.M41,
				M42 = m.M42,
				M43 = m.M43,
				M44 = m.M44
			};

			//could be optimized later into the above copies
			if (transpose)
			{
				ret = global::SlimDX.Matrix.Transpose(ret);
			}

			return ret;
		}

		public static global::SlimDX.Vector4 ToSlimDXVector4(this OpenTK.Vector4 v)
		{
			return new global::SlimDX.Vector4(v.X, v.Y, v.Z, v.W);
		}

		public static global::SlimDX.Vector2 ToSlimDXVector2(this OpenTK.Vector2 v)
		{
			return new global::SlimDX.Vector2(v.X, v.Y);
		}
	}
}