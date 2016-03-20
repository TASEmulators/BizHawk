namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// This enum describe how an external tool is handled
	/// </summary>
	public enum BizHawkExternalToolUsage : short
	{
		/// <summary>
		/// General usage, works even with null emulator
		/// </summary>
		Global = 0,
		/// <summary>
		/// Specific to an emulator (NES,SNES,etc...)
		/// </summary>
		EmulatorSpecific = 1,
		/// <summary>
		/// Specific to a Game
		/// </summary>
		GameSpecific = 2
	}
}
