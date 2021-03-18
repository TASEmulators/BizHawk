namespace BizHawk.Bizware.BizwareGL
{
	public struct Vector4
	{
		public static bool AreEqual(in Vector4 a, in Vector4 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;

		public static bool operator ==(in Vector4 a, in Vector4 b) => AreEqual(in a, in b);

		public static bool operator !=(in Vector4 a, in Vector4 b) => !AreEqual(in a, in b);

		public float X;

		public float Y;

		public float Z;

		public float W;

		public Vector4(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		public override readonly bool Equals(object obj) => obj is Vector4 other && AreEqual(in this, in other);

		public override readonly int GetHashCode() => ((X.GetHashCode() * 397 ^ Y.GetHashCode()) * 397 ^ Z.GetHashCode()) * 397 ^ W.GetHashCode();

		public override readonly string ToString() => $"({X}, {Y}, {Z}, {W})";
	}
}
