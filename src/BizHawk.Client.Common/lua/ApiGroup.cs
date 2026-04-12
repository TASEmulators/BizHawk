namespace BizHawk.Client.Common
{
	[Flags]
	public enum ApiGroup
	{
		NONE = 0,

		/// <summary>
		/// yield or frameadvance
		/// </summary>
		YIELDING = 1,

		/// <summary>
		/// any method that may result in (re)booting a core, such as loading a ROM
		/// </summary>
		BOOTING = 2,

		/// <summary>
		/// any method that may result in saving or loading a savestate
		/// </summary>
		STATES = 4,

		PROHIBITED_MID_FRAME = YIELDING | BOOTING | STATES,
	}
}
