using System.IO;

namespace BizHawk.Client.Common
{
	/// <summary>represents a file found on disk in the user's firmware directory matching a file in our database</summary>
	public sealed class RealFirmwareFile
	{
		public FileInfo FileInfo { get; set; }

		public string Hash { get; set; }
	}
}
