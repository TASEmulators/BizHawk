namespace BizHawk.Emulation.Common
{
	public sealed class FirmwareOption
	{
		public string ConfigKey => $"{SystemId}+{FirmwareId}";

		public string FirmwareId { get; set; }

		public string Hash { get; set; }

		public bool IsAcceptableOrIdeal => Status == FirmwareOptionStatus.Ideal || Status == FirmwareOptionStatus.Acceptable;

		public long Size { get; set; }

		public FirmwareOptionStatus Status { get; set; }

		public string SystemId { get; set; }
	}
}
