namespace BizHawk.Emulation.Common
{
	public enum FirmwareOptionStatus
	{
		Unset = 0,

		/// <summary>Nonlegitimate files that are notable enough to be worth detecting, even if mainly to categorize as a BAD option</summary>
		Bad = 1,

		Unknown = 2,

		/// <summary>A good file, but it doesn't work with our core</summary>
		Unacceptable = 3,

		/// <summary>Works with our core, but not preferred for TASing</summary>
		Acceptable = 4,

		/// <summary>Preferred to get checkmarks, and for TASing</summary>
		Ideal = 5,
	}
}
