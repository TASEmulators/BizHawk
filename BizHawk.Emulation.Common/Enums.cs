namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// DisplayType, used in <seealso cref="IEmulator"/>
	/// </summary>
	public enum DisplayType
	{
		NTSC,
		PAL,
		Dendy
	}

	/// <summary>
	/// The type/increment of stepping in the Step method of <seealso cref="IDebuggable"/>
	/// </summary>
	public enum StepType
	{
		Into,
		Out,
		Over
	}

	/// <summary>
	/// In the game database, the status of the rom found in the database
	/// </summary>
	public enum RomStatus
	{
		GoodDump,
		BadDump,
		Homebrew,
		TranslatedRom,
		Hack,
		Unknown,
		Bios,
		Overdump,
		NotInDatabase
	}
}
