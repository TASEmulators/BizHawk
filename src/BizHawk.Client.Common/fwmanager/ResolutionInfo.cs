using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class ResolutionInfo
	{
		public string FilePath { get; set; }

		public string Hash { get; set; }

		public FirmwareFile KnownFirmwareFile { get; set; }

		public bool KnownMismatching { get; set; }

		public bool Missing { get; set; }

		public long Size { get; set; }

		public bool UserSpecified { get; set; }
	}
}
