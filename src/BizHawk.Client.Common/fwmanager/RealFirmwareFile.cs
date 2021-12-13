#nullable enable

using System.IO;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	/// <summary>represents a file found on disk in the user's firmware directory matching a file in our database</summary>
	public readonly struct RealFirmwareFile
	{
		public readonly FileInfo FileInfo;

		public readonly SHA1Checksum Hash;

		public RealFirmwareFile(FileInfo fileInfo, SHA1Checksum hash)
		{
			FileInfo = fileInfo;
			Hash = hash;
		}
	}
}
