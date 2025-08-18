﻿using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public static class HeaderKeys
	{
		public const string EmulatorVersion = "emuVersion";
		public const string OriginalEmulatorVersion = "OriginalEmuVersion";
		public const string MovieVersion = "MovieVersion";
		public const string Platform = "Platform";
		public const string GameName = "GameName";
		public const string Author = "Author";
		public const string Rerecords = "rerecordCount";
		public const string StartsFromSavestate = "StartsFromSavestate";
		public const string StartsFromSaveram = "StartsFromSaveRam";
		public const string SavestateBinaryBase64Blob = "SavestateBinaryBase64Blob"; // this string will not contain base64: ; it's implicit (this is to avoid another big string op to dice off the base64: substring)
		public const string Sha1 = "SHA1"; // misleading name; either CRC32, MD5, or SHA1, hex-encoded, unprefixed
		public const string Sha256 = "SHA256";
		public const string Md5 = "MD5";
		public const string Crc32 = "CRC32";
		public const string FirmwareSha1 = "FirmwareSHA1";
		public const string Pal = "PAL";
		public const string BoardName = "BoardName";
		public const string SyncSettings = "SyncSettings";
		public const string CycleCount = "CycleCount";
		public const string ClockRate = "ClockRate";
		public const string VsyncAttoseconds = "VsyncAttoseconds"; // used for Arcade due to it representing thousands of different systems with different vsync rates
		public const string Core = "Core";

		private static FrozenSet<string> field;

		private static ISet<string> AllValues
			=> field ??= typeof(HeaderKeys).GetFields()
				.Select(static fi => fi.GetValue(null).ToString())
				.ToFrozenSet();

		public static bool Contains(string val)
			=> AllValues.Contains(val);
	}
}
