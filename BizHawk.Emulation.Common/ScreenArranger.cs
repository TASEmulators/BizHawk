namespace BizHawk.Emulation.Common
{
	public unsafe class VideoScreen
	{
		public VideoScreen(int* buffer, int width, int height)
		{
			Buffer = buffer;
			Width = width;
			Height = height;
		}

		public int* Buffer { get; }
		public int Width { get; } 
		public int Height { get; }

		public int Length => Width * Height;
	}

	/// <summary>
	/// Provides a way to arrange displays inside a frame buffer.
	/// </summary>
	public static class ScreenArranger
	{
		// TODO: pad lines
		// TOOD: pass in int[] to reuse buffer
		public static unsafe int[] Stack(VideoScreen screen1, VideoScreen screen2, int padLines)
		{
			var ret = new int[screen1.Width * (screen1.Height + screen2.Height)];
			for (int i = 0; i < screen1.Length; i++)
			{
				ret[i] = screen1.Buffer[i];
			}

			for (int i = 0; i < screen2.Length; i++)
			{
				ret[screen1.Length + i] = screen2.Buffer[i];
			}

			return ret;
		}

		// Simply populates a buffer with a single screen
		public static unsafe int[] Copy(VideoScreen screen1)
		{
			var ret = new int[screen1.Length];

			for (int i = 0; i < ret.Length; i++)
			{
				ret[i] = screen1.Buffer[i];
			}

			return ret;
		}

		// TODO: pass in int[] to reuse buffer
		// TODO: there is a simpler algorithm for sure
		public static unsafe int[] SideBySide(VideoScreen screen1, VideoScreen screen2)
		{
			int width = screen1.Width + screen2.Width;
			int height = screen2.Height;
			var ret = new int[width * height];

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (x < screen1.Width)
					{
						ret[(y * width) + x] = screen1.Buffer[(y * width / 2) + x];
					}
					else
					{
						ret[(y * width) + x] = screen2.Buffer[(y * width / 2) + x];
					}
				}
				
			}

			return ret;
		}
	}
}
