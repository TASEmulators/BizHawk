using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public readonly struct FirmwarePatchOption
	{
		/// <summary>hash of base file patch should be applied to</summary>
		public readonly string BaseHash;

		public readonly IReadOnlyList<FirmwarePatchData> Patches;

		/// <summary>hash of file produced by patching</summary>
		public readonly string TargetHash;

		public FirmwarePatchOption(string baseHash, IReadOnlyList<FirmwarePatchData> patches, string targetHash)
		{
			FirmwareFile.CheckChecksumStrIsHex(ref baseHash);
			FirmwareFile.CheckChecksumStrIsHex(ref targetHash);
			BaseHash = baseHash;
			Patches = patches;
			TargetHash = targetHash;
		}

		public FirmwarePatchOption(string baseHash, FirmwarePatchData patch, string targetHash)
			: this(baseHash, new[] { patch }, targetHash) {}
	}
}
