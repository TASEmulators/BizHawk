using System;
using System.Drawing;
using System.Collections.Generic;

using OpenTK;

namespace BizHawk.Bizware.BizwareGL
{
	public class MatrixStack
	{
		public MatrixStack()
		{
			LoadIdentity();
			IsDirty = false;
		}

		public static implicit operator Matrix4(MatrixStack ms) { return ms.Top; }
		public static implicit operator MatrixStack(Matrix4 m) { return new MatrixStack(m); }

		public MatrixStack(Matrix4 matrix) { LoadMatrix(matrix); }

		public bool IsDirty;

		Stack<Matrix4> stack = new Stack<Matrix4>();

		/// <summary>
		/// This is made public for performance reasons, to avoid lame copies of the matrix when necessary. Don't mess it up!
		/// </summary>
		public Matrix4 Top;

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
		public void Clear(Matrix4 value)
		{
			stack.Clear();
			Top = value;
			IsDirty = true;
		}

		public void LoadMatrix(Matrix4 value) { Top = value; IsDirty = true; }

		public void LoadIdentity() { Top = Matrix4.Identity; IsDirty = true; }

		public void Pop() { Top = stack.Pop(); IsDirty = true; }
		public void Push() { stack.Push(Top); IsDirty = true; }

		public void RotateAxis(Vector3 axisRotation, float angle) { Top = Matrix4.CreateFromAxisAngle(axisRotation, angle) * Top; IsDirty = true; }

		public void Scale(Vector3 scale) { Top = Matrix4.CreateScale(scale) * Top; IsDirty = true; }
		public void Scale(Vector2 scale) { Top = Matrix4.CreateScale(scale.X, scale.Y, 1) * Top; IsDirty = true; }
		public void Scale(float x, float y, float z) { Top = Matrix4.CreateScale(x, y, z) * Top; IsDirty = true; }
		public void Scale(float ratio) { Scale(ratio, ratio, ratio); IsDirty = true; }
		public void Scale(float x, float y) { Scale(x, y, 1); IsDirty = true; }

		public void RotateAxis(float x, float y, float z, float degrees) { MultiplyMatrix(Matrix4.CreateFromAxisAngle(new Vector3(x, y, z), degrees)); IsDirty = true; }
		public void RotateY(float degrees) { MultiplyMatrix(Matrix4.CreateRotationY(degrees)); IsDirty = true; }
		public void RotateX(float degrees) { MultiplyMatrix(Matrix4.CreateRotationX(degrees)); IsDirty = true; }
		public void RotateZ(float degrees) { MultiplyMatrix(Matrix4.CreateRotationZ(degrees)); IsDirty = true; }

		public void Translate(Vector2 v) { Translate(v.X, v.Y, 0); IsDirty = true; }
		public void Translate(Vector3 trans) { Top = Matrix4.CreateTranslation(trans) * Top; IsDirty = true; }
		public void Translate(float x, float y, float z) { Top = Matrix4.CreateTranslation(x, y, z) * Top; IsDirty = true; }
		public void Translate(float x, float y) { Translate(x, y, 0); IsDirty = true; }
		public void Translate(Point pt) { Translate(pt.X, pt.Y, 0); IsDirty = true; }

		public void MultiplyMatrix(MatrixStack ms) { MultiplyMatrix(ms.Top); IsDirty = true; }
		public void MultiplyMatrix(Matrix4 value) { Top = value * Top; IsDirty = true; }
	}
}