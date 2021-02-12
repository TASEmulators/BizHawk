#nullable enable

namespace BizHawk.Emulation.Common
{
	public readonly struct FirmwareOption
	{
		public readonly string Hash;

		public readonly FirmwareID ID;

		public bool IsAcceptableOrIdeal => Status == FirmwareOptionStatus.Ideal || Status == FirmwareOptionStatus.Acceptable;

		public readonly long Size;

		public readonly FirmwareOptionStatus Status;

		public FirmwareOption(FirmwareID id, string hash, long size, FirmwareOptionStatus status)
		{
			Hash = hash;
			ID = id;
			Size = size;
			Status = status;
		}
	}
}
