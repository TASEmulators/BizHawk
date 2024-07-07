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

	public enum OpposingDirPolicy
	{
		Priority = 0,
		Forbid = 1,
		Allow = 2,
	}
}
