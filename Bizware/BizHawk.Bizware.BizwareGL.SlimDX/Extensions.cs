namespace BizHawk.Bizware.BizwareGL.Drivers.SlimDX
{
	public static class Extensions
	{
		public static global::SlimDX.Matrix ToSlimDXMatrix(this OpenTK.Matrix4 m, bool transpose)
		{
			global::SlimDX.Matrix	ret = new global::SlimDX.Matrix();
			ret.M11 = m.M11;
			ret.M12 = m.M12;
			ret.M13 = m.M13;
			ret.M14 = m.M14;
			ret.M21 = m.M21;
			ret.M22 = m.M22;
			ret.M23 = m.M23;
			ret.M24 = m.M24;
			ret.M31 = m.M31;
			ret.M32 = m.M32;
			ret.M33 = m.M33;
			ret.M34 = m.M34;
			ret.M41 = m.M41;
			ret.M42 = m.M42;
			ret.M43 = m.M43;
			ret.M44 = m.M44;

			//could be optimized later into the above copies
			if (transpose)
				ret = global::SlimDX.Matrix.Transpose(ret);

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