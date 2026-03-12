namespace BizHawk.Emulation.Common
{
	/// <summary>represents the direction of <c>(+, +)</c></summary>
	/// <remarks>docs of individual controllers are being collected in comments of https://github.com/TASEmulators/BizHawk/issues/1200</remarks>
	public enum AxisPairOrientation : byte
	{
		RightAndDown = 0,
		RightAndUp = 1,
		LeftAndDown = 2,
		LeftAndUp = 3,
	}
}
