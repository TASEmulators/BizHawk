namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service provides the ability to pass video output to the client
	/// If available the client will display video output to the user
	/// </summary>
	public interface IVideoProvider : IEmulatorService
	{
		int[] GetVideoBuffer();

		// put together, these describe a metric on the screen
		// they should define the smallest size that the buffer can be placed inside such that:
		// 1. no actual pixel data is lost
		// 2. aspect ratio is accurate
		int VirtualWidth { get; }
		int VirtualHeight { get; }

		int BufferWidth { get; }
		int BufferHeight { get; }
		int BackgroundColor { get; }
	}
}
