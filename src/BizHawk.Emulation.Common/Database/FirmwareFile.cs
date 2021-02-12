namespace BizHawk.Emulation.Common
{
	public sealed class FirmwareFile
	{
		public bool Bad { get; set; }

		public string Description { get; set; }

		public string Hash { get; set; }

		public string Info { get; set; }

		public string RecommendedName { get; set; }

		public long Size { get; set; }
	}
}
