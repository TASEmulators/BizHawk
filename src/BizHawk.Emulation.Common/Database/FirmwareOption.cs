namespace BizHawk.Emulation.Common
{
	public readonly struct FirmwareOption : IEquatable<FirmwareOption>
	{
		public static bool operator ==(FirmwareOption a, FirmwareOption b)
			=> a.Equals(b);

		public static bool operator !=(FirmwareOption a, FirmwareOption b)
			=> !(a == b);

		public readonly string Hash;

		public readonly FirmwareID ID;

		public bool IsAcceptableOrIdeal => Status == FirmwareOptionStatus.Ideal || Status == FirmwareOptionStatus.Acceptable;

		public readonly long Size;

		public readonly FirmwareOptionStatus Status;

		public FirmwareOption(FirmwareID id, string hash, long size, FirmwareOptionStatus status)
		{
			FirmwareFile.CheckChecksumStrIsHex(ref hash);
			Hash = hash;
			ID = id;
			Size = size;
			Status = status;
		}

		public bool Equals(FirmwareOption other)
			=> Hash == other.Hash && ID == other.ID && Size == other.Size && Status == other.Status;

		public readonly override bool Equals(object? obj)
			=> obj is FirmwareOption fr && Equals(fr);

		public readonly override int GetHashCode()
			=> HashCode.Combine(Hash, ID, Size, Status);
	}
}
