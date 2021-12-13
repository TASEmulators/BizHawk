#nullable enable

using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	public readonly struct FirmwarePatchOption
	{
		/// <summary>hash of base file patch should be applied to</summary>
		public readonly SHA1Checksum BaseHash;

		public readonly IReadOnlyList<FirmwarePatchData> Patches;

		/// <summary>hash of file produced by patching</summary>
		public readonly SHA1Checksum TargetHash;

		public FirmwarePatchOption(SHA1Checksum baseHash, IReadOnlyList<FirmwarePatchData> patches, SHA1Checksum targetHash)
		{
			BaseHash = baseHash;
			Patches = patches;
			TargetHash = targetHash;
		}

		public FirmwarePatchOption(SHA1Checksum baseHash, FirmwarePatchData patch, SHA1Checksum targetHash)
			: this(baseHash, new[] { patch }, targetHash) {}
	}
}
