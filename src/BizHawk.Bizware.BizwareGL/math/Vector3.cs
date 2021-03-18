namespace BizHawk.Bizware.BizwareGL
{
	public readonly struct Vector3
	{
		public static bool AreEqual(in Vector3 a, in Vector3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

		public static bool operator ==(in Vector3 a, in Vector3 b) => AreEqual(in a, in b);

		public static bool operator !=(in Vector3 a, in Vector3 b) => !AreEqual(in a, in b);

		public readonly float X;

		public readonly float Y;

		public readonly float Z;

		public Vector3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public override readonly bool Equals(object obj) => obj is Vector3 other && AreEqual(in this, in other);

		public override readonly int GetHashCode() => (X.GetHashCode() * 397 ^ Y.GetHashCode()) * 397 ^ Z.GetHashCode();

		public override readonly string ToString() => $"({X}, {Y}, {Z})";
	}
}
