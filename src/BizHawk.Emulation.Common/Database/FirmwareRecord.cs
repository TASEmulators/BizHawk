namespace BizHawk.Emulation.Common
{
	public readonly struct FirmwareRecord : IEquatable<FirmwareRecord>
	{
		public static bool operator ==(FirmwareRecord a, FirmwareRecord b)
			=> a.Equals(b);

		public static bool operator !=(FirmwareRecord a, FirmwareRecord b)
			=> !(a == b);

		public readonly string Description;

		public readonly FirmwareID ID;

		public FirmwareRecord(FirmwareID id, string desc)
		{
			Description = desc;
			ID = id;
		}

		public bool Equals(FirmwareRecord other)
			=> ID == other.ID && Description == other.Description;

		public readonly override bool Equals(object? obj)
			=> obj is FirmwareRecord fr && Equals(fr);

		public readonly override int GetHashCode()
			=> HashCode.Combine(ID, Description);
	}
}
