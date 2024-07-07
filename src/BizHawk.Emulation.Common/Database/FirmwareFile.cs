using System.Linq;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.Common
{
	public readonly struct FirmwareFile
	{
		internal static void CheckChecksumStrIsHex(ref string digest)
		{
			static bool IsAllowedCharacter(char c)
				=> c is (>= '0' and <= '9') or (>= 'A' and <= 'F'); //TODO SearchValues?
			if (digest.All(IsAllowedCharacter)) return;
			if (!digest.IsHex()) throw new ArgumentOutOfRangeException(paramName: nameof(digest), actualValue: digest, message: "malformed checksum digest: must match /[0-9A-F]+/ (no lowercase please)");
			// but if it is hex, let's be lenient
			Console.Write("interpreting as hex checksum digest and uppercasing (please fix in source): ");
			Console.WriteLine(digest);
			digest = digest.ToUpperInvariant();
		}

		public readonly string Description;

		public readonly string Hash;

		public readonly string Info;

		public readonly bool IsBad;

		public readonly string RecommendedName;

		public readonly long Size;

		public FirmwareFile(
			string hash,
			long size,
			string recommendedName,
			string desc,
			string additionalInfo = "",
			bool isBad = false)
		{
			CheckChecksumStrIsHex(ref hash);
			Description = desc;
			Hash = hash;
			Info = additionalInfo;
			IsBad = isBad;
			RecommendedName = recommendedName;
			Size = size;
		}
	}
}
