namespace BizHawk.Client.Common
{
	public enum ESoundOutputMethod
	{
		LegacyDirectSound, // kept here to handle old configs
		XAudio2,
		OpenAL,
		Dummy
	}

	public enum EDispManagerAR
	{
		None = 0,
		System = 1,
		CustomSize = 2,
		CustomRatio = 3,
	}

	public enum SaveStateType
	{
		Binary, Text
	}

	public enum ClientProfile
	{
		Unknown = 0,
		Casual = 1,
		Longplay = 2,
		Tas = 3,
		N64Tas = 4
	}

	/// <summary>
	/// indicates one of the possible approaches for handling simultaneous opposing cardinal directions (SOCD)
	/// e.g. <c>DPad Left</c>+<c>DPad Right</c>, which may not have been possible with original gamepad hardware
	/// </summary>
	public enum OpposingDirPolicy
	{
		/// <summary>if both directions are pressed, only the most recently pressed (of the pair) will be sent</summary>
		Priority = 0,

		/// <summary>if both directions are pressed, they will "cancel out" and neither will be sent</summary>
		Forbid = 1,

		/// <summary>both directions will be sent when both are pressed</summary>
		Allow = 2,
	}
}
