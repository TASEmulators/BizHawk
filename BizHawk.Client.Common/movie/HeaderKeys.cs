using System.Linq;

namespace BizHawk.Client.Common
{
	public static class HeaderKeys
	{
		public const string EMULATIONVERSION = "emuVersion";
		public const string MOVIEVERSION = "MovieVersion";
		public const string PLATFORM = "Platform";
		public const string GAMENAME = "GameName";
		public const string AUTHOR = "Author";
		public const string RERECORDS = "rerecordCount";
		public const string STARTSFROMSAVESTATE = "StartsFromSavestate";
		public const string STARTSFROMSAVERAM = "StartsFromSaveRam";
		public const string SAVESTATEBINARYBASE64BLOB = "SavestateBinaryBase64Blob"; //this string will not contain base64: ; it's implicit (this is to avoid another big string op to dice off the base64: substring)
		public const string FOURSCORE = "FourScore";
		public const string SHA1 = "SHA1";
		public const string FIRMWARESHA1 = "FirmwareSHA1";
		public const string PAL = "PAL";
		public const string BOARDNAME = "BoardName";
		public const string SYNCSETTINGS = "SyncSettings";
		public const string LOOPOFFSET = "LoopOffset";
		// Core Setting
		public const string CORE = "Core";

		// Plugin Settings
		public const string VIDEOPLUGIN = "VideoPlugin";

		public static bool Contains(string val)
		{
			return typeof(HeaderKeys)
				.GetFields()
				.Select(field => field.GetValue(null).ToString())
				.Contains(val);
		}
	}
}
