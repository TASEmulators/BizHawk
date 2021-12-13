#nullable enable

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public readonly struct FirmwareEventArgs
	{
		public readonly SHA1Checksum? Hash;

		public readonly FirmwareID ID;

		public readonly long Size;

		public FirmwareEventArgs(FirmwareID id, SHA1Checksum? hash, long size)
		{
			Hash = hash;
			ID = id;
			Size = size;
		}
	}
}
