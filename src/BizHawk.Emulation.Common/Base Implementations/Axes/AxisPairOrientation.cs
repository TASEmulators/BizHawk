#nullable enable

namespace BizHawk.Emulation.Common
{
	/// <summary>represents the direction of <c>(+, +)</c></summary>
	/// <remarks>docs of individual controllers are being collected in comments of https://github.com/TASVideos/BizHawk/issues/1200</remarks>
	public enum AxisPairOrientation : byte
	{
		RightAndUp = 0,
		RightAndDown = 1,
		LeftAndUp = 2,
		LeftAndDown = 3
	}
}
