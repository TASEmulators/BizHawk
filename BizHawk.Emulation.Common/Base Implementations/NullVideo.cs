namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A default IVideoProvider that simply returns
	/// a black screen at an arbitrary size
	/// </summary>
	/// <seealso cref="IVideoProvider" />
	public class NullVideo : IVideoProvider
	{
		public int[] GetVideoBuffer()
		{
			return new int[BufferWidth * BufferHeight];
		}

		public static NullVideo Instance { get; } = new NullVideo();

		public int VirtualWidth => 256;

		public int VirtualHeight => 192;

		public int BufferWidth => 256;

		public int BufferHeight => 192;

		public int BackgroundColor => 0;
	}
}
