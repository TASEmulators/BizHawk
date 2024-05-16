using System.Drawing;
using System.Collections.Generic;
using System.Numerics;

namespace BizHawk.Bizware.Graphics
{
	// note: the sense of these matrices, as well as pre- and post- multiplying may be all mixed up.
	// conceptually here is how we should be working, in the example of spinning a sprite in place
	// 1. sprite starts with top left at origin
	// 2. translate half size, to center sprite at origin
	// 3. rotate around Z
	// 4. translate to position in world
	// this class is designed to make that work, that way. the takeaways:
	// * Use the scale, translate, rotate methods in the order given above
	// * Use PostMultiplyMatrix to apply more work to a prior matrix (in the manner described above) since I am calling this all post-work
	public class MatrixStack
	{
		private const float DEG_TO_RAD_FACTOR = (float) System.Math.PI / 180.0f;

		private static float DegreesToRadians(float degrees) => degrees * DEG_TO_RAD_FACTOR;

		public MatrixStack()
		{
			LoadIdentity();
			IsDirty = false;
		}

		public static implicit operator Matrix4x4(MatrixStack ms) { return ms.Top; }
		public static implicit operator MatrixStack(Matrix4x4 m) { return new(m); }

		public MatrixStack(Matrix4x4 matrix)
		{
			LoadMatrix(matrix);
		}

		public bool IsDirty;

		private readonly Stack<Matrix4x4> stack = new();

		/// <summary>
		/// This is made public for performance reasons, to avoid lame copies of the matrix when necessary. Don't mess it up!
		/// </summary>
		public Matrix4x4 Top;

		/// <summary>
		/// Resets the matrix stack to an empty identity matrix stack
		/// </summary>
		public void Clear()
		{
			stack.Clear();
			LoadIdentity();
			IsDirty = true;
		}

		/// <summary>
		/// Clears the matrix stack and loads the specified value
		/// </summary>
		public void Clear(Matrix4x4 value)
		{
			stack.Clear();
			Top = value;
			IsDirty = true;
		}

		public void LoadMatrix(Matrix4x4 value)
		{
			Top = value;
			IsDirty = true;
		}

		public void LoadIdentity()
		{
			Top = Matrix4x4.Identity;
			IsDirty = true;
		}

		public void Pop()
		{
			Top = stack.Pop();
			IsDirty = true;
		}

		public void Push()
		{
			stack.Push(Top);
			IsDirty = true;
		}

		public void RotateAxis(Vector3 axisRotation, float angle)
		{
			PostMultiplyMatrix(Matrix4x4.CreateFromAxisAngle(axisRotation, angle));
			IsDirty = true;
		}

		public void Scale(Vector3 scale)
		{
			PostMultiplyMatrix(Matrix4x4.CreateScale(scale));
			IsDirty = true;
		}

		public void Scale(Vector2 scale)
		{
			PostMultiplyMatrix(Matrix4x4.CreateScale(scale.X, scale.Y, 1));
			IsDirty = true;
		}

		public void Scale(float x, float y, float z)
		{
			PostMultiplyMatrix(Matrix4x4.CreateScale(x, y, z));
			IsDirty = true;
		}

		public void Scale(float ratio)
		{
			Scale(ratio, ratio, ratio);
			IsDirty = true;
		}

		public void Scale(float x, float y)
		{
			Scale(x, y, 1);
			IsDirty = true;
		}

		public void RotateAxis(float x, float y, float z, float degrees)
		{
			PostMultiplyMatrix(Matrix4x4.CreateFromAxisAngle(new(x, y, z), DegreesToRadians(degrees)));
			IsDirty = true;
		}

		public void RotateY(float degrees)
		{
			PostMultiplyMatrix(Matrix4x4.CreateRotationY(DegreesToRadians(degrees)));
			IsDirty = true;
		}

		public void RotateX(float degrees)
		{
			PostMultiplyMatrix(Matrix4x4.CreateRotationX(DegreesToRadians(degrees)));
			IsDirty = true;
		}

		public void RotateZ(float degrees)
		{
			PostMultiplyMatrix(Matrix4x4.CreateRotationZ(DegreesToRadians(degrees)));
			IsDirty = true;
		}

		public void Translate(Vector2 v)
		{
			Translate(v.X, v.Y, 0);
			IsDirty = true;
		}

		public void Translate(Vector3 trans)
		{
			PostMultiplyMatrix(Matrix4x4.CreateTranslation(trans));
			IsDirty = true;
		}

		public void Translate(float x, float y, float z)
		{
			PostMultiplyMatrix(Matrix4x4.CreateTranslation(x, y, z));
			IsDirty = true;
		}

		public void Translate(float x, float y)
		{
			Translate(x, y, 0);
			IsDirty = true;
		}

		public void Translate(Point pt)
		{
			Translate(pt.X, pt.Y, 0);
			IsDirty = true;
		}

		public void PostMultiplyMatrix(MatrixStack ms)
		{
			PostMultiplyMatrix(ms.Top);
			IsDirty = true;
		}

		public void PostMultiplyMatrix(Matrix4x4 value)
		{
			Top *= value;
			IsDirty = true;
		}

		public void PreMultiplyMatrix(Matrix4x4 value)
		{
			Top = value * Top;
			IsDirty = true;
		}
	}
}