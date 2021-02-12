namespace BizHawk.Emulation.Common
{
	public enum FirmwareOptionStatus
	{
		/// <summary>Preferred to get checkmarks, and for TASing</summary>
		Ideal,

		/// <summary>Works with our core, but not preferred for TASing</summary>
		Acceptable,

		/// <summary>A good file, but it doesn't work with our core</summary>
		Unacceptable,

		/// <summary>Nonlegitimate files that are notable enough to be worth detecting, even if mainly to categorize as a BAD option</summary>
		Bad
	}
}
