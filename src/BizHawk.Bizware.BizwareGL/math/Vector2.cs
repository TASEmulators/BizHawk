namespace BizHawk.Bizware.BizwareGL
{
	public struct Vector2
	{
		public static bool AreEqual(in Vector2 a, in Vector2 b) => a.X == b.X && a.Y == b.Y;

		public static bool operator ==(in Vector2 a, in Vector2 b) => AreEqual(in a, in b);

		public static bool operator !=(in Vector2 a, in Vector2 b) => !AreEqual(in a, in b);

		public static Vector2 operator +(in Vector2 left, in Vector2 right) => new(left.X + right.X, left.Y + right.Y);

		public float X;

		public float Y;

		public Vector2(float x, float y)
		{
			X = x;
			Y = y;
		}

		public override readonly bool Equals(object obj) => obj is Vector2 other && AreEqual(in this, in other);

		public override readonly int GetHashCode() => X.GetHashCode() * 397 ^ Y.GetHashCode();

		public override readonly string ToString() => $"({X}, {Y})";
	}
}
