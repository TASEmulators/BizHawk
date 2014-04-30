using System.Linq;

namespace BizHawk.Client.Common
{
	public static class HeaderKeys
	{
		public const string MovieVersion1 = "BizHawk v0.0.1";
		public const string MovieVersion2 = "BizHawk v0.0.2";

		public const string EMULATIONVERSION = "emuVersion";
		public const string MOVIEVERSION = "MovieVersion";
		public const string PLATFORM = "Platform";
		public const string GAMENAME = "GameName";
		public const string AUTHOR = "Author";
		public const string RERECORDS = "rerecordCount";
		public const string STARTSFROMSAVESTATE = "StartsFromSavestate";
		public const string SAVESTATEBINARYBASE64BLOB = "SavestateBinaryBase64Blob"; //this string will not contain base64: ; it's implicit (this is to avoid another big string op to dice off the base64: substring)
		public const string FOURSCORE = "FourScore";
		public const string SHA1 = "SHA1";
		public const string FIRMWARESHA1 = "FirmwareSHA1";
		public const string PAL = "PAL";
		public const string BOARDNAME = "BoardName";

		// Core Setting
		public const string CORE = "Core";

		// Gameboy Settings that affect sync
		public const string GB_FORCEDMG = "Force_DMG_Mode";
		public const string GB_GBA_IN_CGB = "GBA_In_CGB";
		public const string SGB = "SGB"; // A snes movie will set this to indicate that it's actually SGB

		// BIO skipping setting (affects sync)
		public const string SKIPBIOS = "Skip_Bios";

		// Plugin Settings
		public const string VIDEOPLUGIN = "VideoPlugin";

		// Board properties
		public const string BOARDPROPERTIES = "BoardProperty";

		public static bool Contains(string val)
		{
			return typeof(HeaderKeys)
				.GetFields()
				.Select(field => field.GetValue(null).ToString())
				.Contains(val);
		}
	}
}
