namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Provides an interface to convey the effective X, Y coordinates that represent
	/// the start of the viewable screen area. Used by cores that provide options to clip the edges
	/// of the screen (that might likely have junk, such as NES)
	/// </summary>
	public interface IVideoLogicalOffsets : ISpecializedEmulatorService
	{
		int ScreenX { get; }
		int ScreenY { get; }
	}
}
