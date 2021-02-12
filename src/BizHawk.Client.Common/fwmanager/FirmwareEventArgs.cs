namespace BizHawk.Client.Common
{
	public sealed class FirmwareEventArgs
	{
		public string FirmwareId { get; set; }

		public string Hash { get; set; }

		public long Size { get; set; }

		public string SystemId { get; set; }
	}
}
