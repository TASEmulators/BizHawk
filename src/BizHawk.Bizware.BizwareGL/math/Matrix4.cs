using System;

namespace BizHawk.Bizware.BizwareGL
{
	public struct Matrix4
	{
		public static readonly Matrix4 Identity = new(new(1.0f, 0.0f, 0.0f, 0.0f), new(0.0f, 1.0f, 0.0f, 0.0f), new(0.0f, 0.0f, 1.0f, 0.0f), new(0.0f, 0.0f, 0.0f, 1.0f));

		public static bool AreEqual(in Matrix4 a, in Matrix4 b) => Vector4.AreEqual(in a.Row0, in b.Row0) && Vector4.AreEqual(in a.Row1, in b.Row1) && Vector4.AreEqual(in a.Row2, in b.Row2) && Vector4.AreEqual(in a.Row3, in b.Row3);

		/// <returns>a <see cref="Matrix4"/> representing a rotation of <paramref name="angle"/> radians CCW around the given <paramref name="axis"/></returns>
		public static Matrix4 CreateFromAxisAngle(in Vector3 axis, float angle)
		{
			var num0 = (float) (1.0 / Math.Sqrt(axis.X * (double) axis.X + axis.Y * (double) axis.Y + axis.Z * (double) axis.Z));
			var x = axis.X * num0;
			var y = axis.Y * num0;
			var z = axis.Z * num0;
			var num1 = (float) Math.Cos(-(double) angle);
			var num2 = (float) Math.Sin(-(double) angle);
			var num3 = 1.0f - num1;
			var num4 = num3 * x * x;
			var num5 = num3 * x * y;
			var num6 = num3 * x * z;
			var num7 = num3 * y * y;
			var num8 = num3 * y * z;
			var num9 = num3 * z * z;
			var num10 = num2 * x;
			var num11 = num2 * y;
			var num12 = num2 * z;
			return new(
				new(num4 + num1, num5 - num12, num6 + num11, 0.0f),
				new(num5 + num12, num7 + num1, num8 - num10, 0.0f),
				new(num6 - num11, num8 + num10, num9 + num1, 0.0f),
				new(0.0f, 0.0f, 0.0f, 1.0f));
		}

		/// <returns>a <see cref="Matrix4"/> representing a rotation of <paramref name="angle"/> radians CCW around the X-axis</returns>
		public static Matrix4 CreateRotationX(float angle)
		{
			var num1 = (float) Math.Cos(angle);
			var num2 = (float) Math.Sin(angle);
			var result = Identity; // copy
			result.Row1.Y = num1;
			result.Row1.Z = num2;
			result.Row2.Y = -num2;
			result.Row2.Z = num1;
			return result;
		}

		/// <returns>a <see cref="Matrix4"/> representing a rotation of <paramref name="angle"/> radians CCW around the Y-axis</returns>
		public static Matrix4 CreateRotationY(float angle)
		{
			var num1 = (float) Math.Cos(angle);
			var num2 = (float) Math.Sin(angle);
			var result = Identity; // copy
			result.Row0.X = num1;
			result.Row0.Z = -num2;
			result.Row2.X = num2;
			result.Row2.Z = num1;
			return result;
		}

		/// <returns>a <see cref="Matrix4"/> representing a rotation of <paramref name="angle"/> radians CCW around the Z-axis</returns>
		public static Matrix4 CreateRotationZ(float angle)
		{
			var num1 = (float) Math.Cos(angle);
			var num2 = (float) Math.Sin(angle);
			var result = Identity; // copy
			result.Row0.X = num1;
			result.Row0.Y = num2;
			result.Row1.X = -num2;
			result.Row1.Y = num1;
			return result;
		}

		/// <returns>a <see cref="Matrix4"/> representing a scaling</returns>
		public static Matrix4 CreateScale(float x, float y, float z)
		{
			var result = Identity; // copy
			result.Row0.X = x;
			result.Row1.Y = y;
			result.Row2.Z = z;
			return result;
		}

		/// <returns>a <see cref="Matrix4"/> representing a scaling</returns>
		public static Matrix4 CreateScale(in Vector3 scale)
		{
			var result = Identity; // copy
			result.Row0.X = scale.X;
			result.Row1.Y = scale.Y;
			result.Row2.Z = scale.Z;
			return result;
		}

		/// <returns>a <see cref="Matrix4"/> representing a translation</returns>
		public static Matrix4 CreateTranslation(float x, float y, float z)
		{
			var result = Identity; // copy
			result.Row3.X = x;
			result.Row3.Y = y;
			result.Row3.Z = z;
			return result;
		}

		/// <returns>a <see cref="Matrix4"/> representing a translation</returns>
		public static Matrix4 CreateTranslation(in Vector3 vector)
		{
			var result = Identity; // copy
			result.Row3.X = vector.X;
			result.Row3.Y = vector.Y;
			result.Row3.Z = vector.Z;
			return result;
		}

		private static float GetDeterminant(in Matrix4 m)
		{
			var x1 = (double) m.Row0.X;
			var y1 = (double) m.Row0.Y;
			var z1 = (double) m.Row0.Z;
			var w1 = (double) m.Row0.W;
			var x2 = (double) m.Row1.X;
			var y2 = (double) m.Row1.Y;
			var z2 = (double) m.Row1.Z;
			var w2 = (double) m.Row1.W;
			var x3 = (double) m.Row2.X;
			var y3 = (double) m.Row2.Y;
			var z3 = (double) m.Row2.Z;
			var w3 = (double) m.Row2.W;
			var x4 = (double) m.Row3.X;
			var y4 = (double) m.Row3.Y;
			var z4 = (double) m.Row3.Z;
			var w4 = (double) m.Row3.W;
			return (float) (x1 * y2 * z3 * w4
				- x1 * y2 * w3 * z4
				+ x1 * z2 * w3 * y4
				- x1 * z2 * y3 * w4
				+ x1 * w2 * y3 * z4
				- x1 * w2 * z3 * y4
				- y1 * z2 * w3 * x4
				+ y1 * z2 * x3 * w4
				- y1 * w2 * x3 * z4
				+ y1 * w2 * z3 * x4
				- y1 * x2 * z3 * w4
				+ y1 * x2 * w3 * z4
				+ z1 * w2 * x3 * y4
				- z1 * w2 * y3 * x4
				+ z1 * x2 * y3 * w4
				- z1 * x2 * w3 * y4
				+ z1 * y2 * w3 * x4
				- z1 * y2 * x3 * w4
				- w1 * x2 * y3 * z4
				+ w1 * x2 * z3 * y4
				- w1 * y2 * z3 * x4
				+ w1 * y2 * x3 * z4
				- w1 * z2 * x3 * y4
				+ w1 * z2 * y3 * x4);
		}

		private static unsafe Matrix4 Invert(in Matrix4 mat)
		{
			var pDbTemp = stackalloc double[16];
			fixed (Matrix4* pMatIn = &mat)
			{
				var pFlIn = (float*) pMatIn;
				for (var i = 0; i < 16; i++) pDbTemp[i] = pFlIn[i];
			}
			Matrix4 result = new();
			ref var refResult = ref result;
			fixed (Matrix4* pMatOut = &refResult)
			{
				var pFlOut = (float*) pMatOut;
				pFlOut[0] = (float) (pDbTemp[5] * pDbTemp[10] * pDbTemp[15] - pDbTemp[5] * pDbTemp[11] * pDbTemp[14] - pDbTemp[9] * pDbTemp[6] * pDbTemp[15] + pDbTemp[9] * pDbTemp[7] * pDbTemp[14] + pDbTemp[13] * pDbTemp[6] * pDbTemp[11] - pDbTemp[13] * pDbTemp[7] * pDbTemp[10]);
				pFlOut[4] = (float) (-pDbTemp[4] * pDbTemp[10] * pDbTemp[15] + pDbTemp[4] * pDbTemp[11] * pDbTemp[14] + pDbTemp[8] * pDbTemp[6] * pDbTemp[15] - pDbTemp[8] * pDbTemp[7] * pDbTemp[14] - pDbTemp[12] * pDbTemp[6] * pDbTemp[11] + pDbTemp[12] * pDbTemp[7] * pDbTemp[10]);
				pFlOut[8] = (float) (pDbTemp[4] * pDbTemp[9] * pDbTemp[15] - pDbTemp[4] * pDbTemp[11] * pDbTemp[13] - pDbTemp[8] * pDbTemp[5] * pDbTemp[15] + pDbTemp[8] * pDbTemp[7] * pDbTemp[13] + pDbTemp[12] * pDbTemp[5] * pDbTemp[11] - pDbTemp[12] * pDbTemp[7] * pDbTemp[9]);
				pFlOut[12] = (float) (-pDbTemp[4] * pDbTemp[9] * pDbTemp[14] + pDbTemp[4] * pDbTemp[10] * pDbTemp[13] + pDbTemp[8] * pDbTemp[5] * pDbTemp[14] - pDbTemp[8] * pDbTemp[6] * pDbTemp[13] - pDbTemp[12] * pDbTemp[5] * pDbTemp[10] + pDbTemp[12] * pDbTemp[6] * pDbTemp[9]);
				pFlOut[1] = (float) (-pDbTemp[1] * pDbTemp[10] * pDbTemp[15] + pDbTemp[1] * pDbTemp[11] * pDbTemp[14] + pDbTemp[9] * pDbTemp[2] * pDbTemp[15] - pDbTemp[9] * pDbTemp[3] * pDbTemp[14] - pDbTemp[13] * pDbTemp[2] * pDbTemp[11] + pDbTemp[13] * pDbTemp[3] * pDbTemp[10]);
				pFlOut[5] = (float) (pDbTemp[0] * pDbTemp[10] * pDbTemp[15] - pDbTemp[0] * pDbTemp[11] * pDbTemp[14] - pDbTemp[8] * pDbTemp[2] * pDbTemp[15] + pDbTemp[8] * pDbTemp[3] * pDbTemp[14] + pDbTemp[12] * pDbTemp[2] * pDbTemp[11] - pDbTemp[12] * pDbTemp[3] * pDbTemp[10]);
				pFlOut[9] = (float) (-pDbTemp[0] * pDbTemp[9] * pDbTemp[15] + pDbTemp[0] * pDbTemp[11] * pDbTemp[13] + pDbTemp[8] * pDbTemp[1] * pDbTemp[15] - pDbTemp[8] * pDbTemp[3] * pDbTemp[13] - pDbTemp[12] * pDbTemp[1] * pDbTemp[11] + pDbTemp[12] * pDbTemp[3] * pDbTemp[9]);
				pFlOut[13] = (float) (pDbTemp[0] * pDbTemp[9] * pDbTemp[14] - pDbTemp[0] * pDbTemp[10] * pDbTemp[13] - pDbTemp[8] * pDbTemp[1] * pDbTemp[14] + pDbTemp[8] * pDbTemp[2] * pDbTemp[13] + pDbTemp[12] * pDbTemp[1] * pDbTemp[10] - pDbTemp[12] * pDbTemp[2] * pDbTemp[9]);
				pFlOut[2] = (float) (pDbTemp[1] * pDbTemp[6] * pDbTemp[15] - pDbTemp[1] * pDbTemp[7] * pDbTemp[14] - pDbTemp[5] * pDbTemp[2] * pDbTemp[15] + pDbTemp[5] * pDbTemp[3] * pDbTemp[14] + pDbTemp[13] * pDbTemp[2] * pDbTemp[7] - pDbTemp[13] * pDbTemp[3] * pDbTemp[6]);
				pFlOut[6] = (float) (-pDbTemp[0] * pDbTemp[6] * pDbTemp[15] + pDbTemp[0] * pDbTemp[7] * pDbTemp[14] + pDbTemp[4] * pDbTemp[2] * pDbTemp[15] - pDbTemp[4] * pDbTemp[3] * pDbTemp[14] - pDbTemp[12] * pDbTemp[2] * pDbTemp[7] + pDbTemp[12] * pDbTemp[3] * pDbTemp[6]);
				pFlOut[10] = (float) (pDbTemp[0] * pDbTemp[5] * pDbTemp[15] - pDbTemp[0] * pDbTemp[7] * pDbTemp[13] - pDbTemp[4] * pDbTemp[1] * pDbTemp[15] + pDbTemp[4] * pDbTemp[3] * pDbTemp[13] + pDbTemp[12] * pDbTemp[1] * pDbTemp[7] - pDbTemp[12] * pDbTemp[3] * pDbTemp[5]);
				pFlOut[14] = (float) (-pDbTemp[0] * pDbTemp[5] * pDbTemp[14] + pDbTemp[0] * pDbTemp[6] * pDbTemp[13] + pDbTemp[4] * pDbTemp[1] * pDbTemp[14] - pDbTemp[4] * pDbTemp[2] * pDbTemp[13] - pDbTemp[12] * pDbTemp[1] * pDbTemp[6] + pDbTemp[12] * pDbTemp[2] * pDbTemp[5]);
				pFlOut[3] = (float) (-pDbTemp[1] * pDbTemp[6] * pDbTemp[11] + pDbTemp[1] * pDbTemp[7] * pDbTemp[10] + pDbTemp[5] * pDbTemp[2] * pDbTemp[11] - pDbTemp[5] * pDbTemp[3] * pDbTemp[10] - pDbTemp[9] * pDbTemp[2] * pDbTemp[7] + pDbTemp[9] * pDbTemp[3] * pDbTemp[6]);
				pFlOut[7] = (float) (pDbTemp[0] * pDbTemp[6] * pDbTemp[11] - pDbTemp[0] * pDbTemp[7] * pDbTemp[10] - pDbTemp[4] * pDbTemp[2] * pDbTemp[11] + pDbTemp[4] * pDbTemp[3] * pDbTemp[10] + pDbTemp[8] * pDbTemp[2] * pDbTemp[7] - pDbTemp[8] * pDbTemp[3] * pDbTemp[6]);
				pFlOut[11] = (float) (-pDbTemp[0] * pDbTemp[5] * pDbTemp[11] + pDbTemp[0] * pDbTemp[7] * pDbTemp[9] + pDbTemp[4] * pDbTemp[1] * pDbTemp[11] - pDbTemp[4] * pDbTemp[3] * pDbTemp[9] - pDbTemp[8] * pDbTemp[1] * pDbTemp[7] + pDbTemp[8] * pDbTemp[3] * pDbTemp[5]);
				pFlOut[15] = (float) (pDbTemp[0] * pDbTemp[5] * pDbTemp[10] - pDbTemp[0] * pDbTemp[6] * pDbTemp[9] - pDbTemp[4] * pDbTemp[1] * pDbTemp[10] + pDbTemp[4] * pDbTemp[2] * pDbTemp[9] + pDbTemp[8] * pDbTemp[1] * pDbTemp[6] - pDbTemp[8] * pDbTemp[2] * pDbTemp[5]);
				var num1 = (float) (pDbTemp[0] * pFlOut[0] + pDbTemp[1] * pFlOut[4] + pDbTemp[2] * pFlOut[8] + pDbTemp[3] * pFlOut[12]);
				if (num1 == 0.0f) throw new InvalidOperationException("Matrix is singular and cannot be inverted.");
				var num2 = 1.0f / num1;
				for (var i = 0; i < 16; i++) pFlOut[i] *= num2;
			}
			return result;
		}

		public static Matrix4 Transpose(in Matrix4 mat) => new(
			new(mat.Row0.X, mat.Row1.X, mat.Row2.X, mat.Row3.X),
			new(mat.Row0.Y, mat.Row1.Y, mat.Row2.Y, mat.Row3.Y),
			new(mat.Row0.Z, mat.Row1.Z, mat.Row2.Z, mat.Row3.Z),
			new(mat.Row0.W, mat.Row1.W, mat.Row2.W, mat.Row3.W));

		public static bool operator ==(in Matrix4 a, in Matrix4 b) => AreEqual(in a, in b);

		public static bool operator !=(in Matrix4 a, in Matrix4 b) => !AreEqual(in a, in b);

		/// <summary>Matrix multiplication</summary>
		/// <param name="left">left-hand operand</param>
		/// <param name="right">right-hand operand</param>
		/// <returns>A new Matrix4 which holds the result of the multiplication</returns>
		public static Matrix4 operator *(in Matrix4 left, in Matrix4 right)
		{
			var x1 = (double) left.Row0.X;
			var y1 = (double) left.Row0.Y;
			var z1 = (double) left.Row0.Z;
			var w1 = (double) left.Row0.W;
			var x2 = (double) left.Row1.X;
			var y2 = (double) left.Row1.Y;
			var z2 = (double) left.Row1.Z;
			var w2 = (double) left.Row1.W;
			var x3 = (double) left.Row2.X;
			var y3 = (double) left.Row2.Y;
			var z3 = (double) left.Row2.Z;
			var w3 = (double) left.Row2.W;
			var x4 = (double) left.Row3.X;
			var y4 = (double) left.Row3.Y;
			var z4 = (double) left.Row3.Z;
			var w4 = (double) left.Row3.W;
			var x5 = (double) right.Row0.X;
			var y5 = (double) right.Row0.Y;
			var z5 = (double) right.Row0.Z;
			var w5 = (double) right.Row0.W;
			var x6 = (double) right.Row1.X;
			var y6 = (double) right.Row1.Y;
			var z6 = (double) right.Row1.Z;
			var w6 = (double) right.Row1.W;
			var x7 = (double) right.Row2.X;
			var y7 = (double) right.Row2.Y;
			var z7 = (double) right.Row2.Z;
			var w7 = (double) right.Row2.W;
			var x8 = (double) right.Row3.X;
			var y8 = (double) right.Row3.Y;
			var z8 = (double) right.Row3.Z;
			var w8 = (double) right.Row3.W;
			Matrix4 result;
			result.Row0.X = (float) (x1 * x5 + y1 * x6 + z1 * x7 + w1 * x8);
			result.Row0.Y = (float) (x1 * y5 + y1 * y6 + z1 * y7 + w1 * y8);
			result.Row0.Z = (float) (x1 * z5 + y1 * z6 + z1 * z7 + w1 * z8);
			result.Row0.W = (float) (x1 * w5 + y1 * w6 + z1 * w7 + w1 * w8);
			result.Row1.X = (float) (x2 * x5 + y2 * x6 + z2 * x7 + w2 * x8);
			result.Row1.Y = (float) (x2 * y5 + y2 * y6 + z2 * y7 + w2 * y8);
			result.Row1.Z = (float) (x2 * z5 + y2 * z6 + z2 * z7 + w2 * z8);
			result.Row1.W = (float) (x2 * w5 + y2 * w6 + z2 * w7 + w2 * w8);
			result.Row2.X = (float) (x3 * x5 + y3 * x6 + z3 * x7 + w3 * x8);
			result.Row2.Y = (float) (x3 * y5 + y3 * y6 + z3 * y7 + w3 * y8);
			result.Row2.Z = (float) (x3 * z5 + y3 * z6 + z3 * z7 + w3 * z8);
			result.Row2.W = (float) (x3 * w5 + y3 * w6 + z3 * w7 + w3 * w8);
			result.Row3.X = (float) (x4 * x5 + y4 * x6 + z4 * x7 + w4 * x8);
			result.Row3.Y = (float) (x4 * y5 + y4 * y6 + z4 * y7 + w4 * y8);
			result.Row3.Z = (float) (x4 * z5 + y4 * z6 + z4 * z7 + w4 * z8);
			result.Row3.W = (float) (x4 * w5 + y4 * w6 + z4 * w7 + w4 * w8);
			return result;
		}

		/// <returns><paramref name="vec"/> transformed by <paramref name="mat"/></returns>
		public static Vector4 operator *(in Vector4 vec, in Matrix4 mat) => new(
			(float) (vec.X * (double) mat.Row0.X + vec.Y * (double) mat.Row1.X + vec.Z * (double) mat.Row2.X + vec.W * (double) mat.Row3.X),
			(float) (vec.X * (double) mat.Row0.Y + vec.Y * (double) mat.Row1.Y + vec.Z * (double) mat.Row2.Y + vec.W * (double) mat.Row3.Y),
			(float) (vec.X * (double) mat.Row0.Z + vec.Y * (double) mat.Row1.Z + vec.Z * (double) mat.Row2.Z + vec.W * (double) mat.Row3.Z),
			(float) (vec.X * (double) mat.Row0.W + vec.Y * (double) mat.Row1.W + vec.Z * (double) mat.Row2.W + vec.W * (double) mat.Row3.W));

		/// <summary>Top row of the matrix.</summary>
		public Vector4 Row0;

		/// <summary>2nd row of the matrix.</summary>
		public Vector4 Row1;

		/// <summary>3rd row of the matrix.</summary>
		public Vector4 Row2;

		/// <summary>Bottom row of the matrix.</summary>
		public Vector4 Row3;

		/// <param name="row0">Top row of the matrix.</param>
		/// <param name="row1">Second row of the matrix.</param>
		/// <param name="row2">Third row of the matrix.</param>
		/// <param name="row3">Bottom row of the matrix.</param>
		public Matrix4(Vector4 row0, Vector4 row1, Vector4 row2, Vector4 row3)
		{
			Row0 = row0;
			Row1 = row1;
			Row2 = row2;
			Row3 = row3;
		}

		public unsafe float this[int rowIndex, int columnIndex]
		{
			readonly get
			{
				var i = rowIndex * 4 + columnIndex;
				if (i < 0 || 15 < i) throw new IndexOutOfRangeException($"no such element m[{rowIndex}, {columnIndex}] of {nameof(Matrix4)}");
				fixed (Matrix4* p = &this) return ((float*) p)[i];
			}
			set
			{
				var i = rowIndex * 4 + columnIndex;
				if (i < 0 || 15 < i) throw new IndexOutOfRangeException($"no such element m[{rowIndex}, {columnIndex}] of {nameof(Matrix4)}");
				fixed (Matrix4* p = &this) ((float*) p)[i] = value;
			}
		}

		/// <returns>an inverted copy of this instance, or an identical copy if it is singular</returns>
		public readonly Matrix4 Inverted() => GetDeterminant(in this) == 0.0f ? this : Invert(in this);

		public override readonly bool Equals(object obj) => obj is Matrix4 other && AreEqual(in this, in other);

		public override readonly int GetHashCode() => ((Row0.GetHashCode() * 397 ^ Row1.GetHashCode()) * 397 ^ Row2.GetHashCode()) * 397 ^ Row3.GetHashCode();

		public override readonly string ToString() => string.Join("\n", Row0, Row1, Row2, Row3);
	}
}
