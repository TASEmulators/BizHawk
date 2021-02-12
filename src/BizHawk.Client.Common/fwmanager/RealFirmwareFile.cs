#nullable enable

using System.IO;

namespace BizHawk.Client.Common
{
	/// <summary>represents a file found on disk in the user's firmware directory matching a file in our database</summary>
	public readonly struct RealFirmwareFile
	{
		public readonly FileInfo FileInfo;

		public readonly string Hash;

		public RealFirmwareFile(FileInfo fileInfo, string hash)
		{
			FileInfo = fileInfo;
			Hash = hash;
		}
	}
}
