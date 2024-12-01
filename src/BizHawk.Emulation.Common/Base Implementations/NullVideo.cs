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
			Array.Clear(VideoBuffer, 0, VideoBuffer.Length);
			return VideoBuffer;
		}

		public static NullVideo Instance { get; } = new NullVideo();

		public static int DefaultWidth { get; } = 256;
		public static int DefaultHeight { get; } = 192;
		public static int DefaultBackgroundColor { get; } = 0;
		public static int DefaultVsyncNum { get; } = 60;
		public static int DefaultVsyncDen { get; } = 1;

		public int VirtualWidth => DefaultWidth;

		public int VirtualHeight => DefaultHeight;

		public int BufferWidth => DefaultWidth;

		public int BufferHeight => DefaultHeight;

		public int BackgroundColor => DefaultBackgroundColor;

		public int VsyncNumerator => DefaultVsyncNum;

		public int VsyncDenominator => DefaultVsyncDen;

		private static readonly int[] VideoBuffer = new int[DefaultWidth * DefaultHeight];
	}
}
