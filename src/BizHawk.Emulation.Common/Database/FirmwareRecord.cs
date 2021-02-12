namespace BizHawk.Emulation.Common
{
	public sealed class FirmwareRecord
	{
		public string ConfigKey => $"{SystemId}+{FirmwareId}";

		public string Descr { get; set; }

		public string FirmwareId { get; set; }

		public string SystemId { get; set; }
	}
}
