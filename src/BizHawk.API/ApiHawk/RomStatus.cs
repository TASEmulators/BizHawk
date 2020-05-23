namespace BizHawk.API.ApiHawk
{
	/// <summary>Quality of rom as determined by gamedb lookup, or <see cref="NotInDatabase"/>.</summary>
	public enum RomStatus : int
	{
		GoodDump = 0,
		BadDump = 1,
		Homebrew = 2,
		TranslatedRom = 3,
		Hack = 4,
		Unknown = 5,
		Bios = 6,
		Overdump = 7,
		NotInDatabase = 8 //TODO this could be (byte) 0xff, as long as this enum isn't serialised as an int anywhere
	}
}
