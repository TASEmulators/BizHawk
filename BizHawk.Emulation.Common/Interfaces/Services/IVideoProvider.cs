namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service provides the ability to pass video output to the client
	/// If available the client will display video output to the user,
	/// If unavailable the client will fall back to a default video implementation, presumably
	/// a black screen of some arbitrary size
	/// </summary>
	public interface IVideoProvider : IEmulatorService
	{
		/// <summary>
		/// Returns a framebuffer of the current video content
		/// </summary>
		int[] GetVideoBuffer();

		// put together, these describe a metric on the screen
		// they should define the smallest size that the buffer can be placed inside such that:
		// 1. no actual pixel data is lost
		// 2. aspect ratio is accurate
		int VirtualWidth { get; }
		int VirtualHeight { get; }

		/// <summary>
		/// The width of the frame buffer
		/// </summary>
		int BufferWidth { get; }

		/// <summary>
		/// The height of the frame buffer
		/// </summary>
		int BufferHeight { get; }

		/// <summary>
		/// The default color when no other color is applied
		/// Often cores will set this to something other than black
		/// to show that the core is in fact loaded and frames are rendering
		/// which is less obvious if it is the same as the default video output
		/// </summary>
		int BackgroundColor { get; }
	}
}
