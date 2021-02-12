using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class FirmwareEventArgs
	{
		public string Hash { get; set; }

		public FirmwareID ID { get; set; }

		public long Size { get; set; }
	}
}
