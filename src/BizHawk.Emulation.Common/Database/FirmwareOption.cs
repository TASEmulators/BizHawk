namespace BizHawk.Emulation.Common
{
	public sealed class FirmwareOption
	{
		public string Hash { get; set; }

		public FirmwareID ID { get; set; }

		public bool IsAcceptableOrIdeal => Status == FirmwareOptionStatus.Ideal || Status == FirmwareOptionStatus.Acceptable;

		public long Size { get; set; }

		public FirmwareOptionStatus Status { get; set; }
	}
}
