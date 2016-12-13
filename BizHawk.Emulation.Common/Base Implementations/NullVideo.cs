namespace BizHawk.Emulation.Common
{
	// A default IVideoProvider implementation that simply returns a black screen
	public class NullVideo : IVideoProvider
	{
		public int[] GetVideoBuffer()
		{
			return new int[BufferWidth * BufferHeight];
		}

		public int VirtualWidth { get { return 256; } }
		public int VirtualHeight { get { return 192; } }

		public int BufferWidth { get { return 256; } }
		public int BufferHeight { get { return 192; } }

		public int BackgroundColor { get { return 0; } }

		private static NullVideo _nullVideo = new NullVideo();

		public static NullVideo Instance
		{
			get { return _nullVideo; }
		}
	}
}
