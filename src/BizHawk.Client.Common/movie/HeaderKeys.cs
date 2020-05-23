using System.Linq;

namespace BizHawk.Client.Common
{
	public static class HeaderKeys
	{
		public const string EmulationVersion = "emuVersion";
		public const string MovieVersion = "MovieVersion";
		public const string Platform = "Platform";
		public const string GameName = "GameName";
		public const string Author = "Author";
		public const string Rerecords = "rerecordCount";
		public const string StartsFromSavestate = "StartsFromSavestate";
		public const string StartsFromSaveram = "StartsFromSaveRam";
		public const string SavestateBinaryBase64Blob = "SavestateBinaryBase64Blob"; // this string will not contain base64: ; it's implicit (this is to avoid another big string op to dice off the base64: substring)
		public const string Sha1 = "SHA1";
		public const string FirmwareSha1 = "FirmwareSHA1";
		public const string Pal = "PAL";
		public const string BoardName = "BoardName";
		public const string SyncSettings = "SyncSettings";
		public const string VBlankCount = "VBlankCount";
		public const string CycleCount = "CycleCount";
		public const string Core = "Core";

		public static bool Contains(string val) =>
			typeof(HeaderKeys)
				.GetFields()
				.Select(field => field.GetValue(null).ToString())
				.Contains(val);
	}
}
