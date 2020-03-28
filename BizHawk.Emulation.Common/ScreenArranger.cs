using System;
using System.Drawing;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Provides a way to arrange displays inside a frame buffer.
	/// </summary>
	public class ScreenArranger
	{
		private readonly Size[] _sizes;

		public ScreenArranger(Size[] sizes)
		{
			_sizes = sizes;
		}

		public ScreenLayoutSettings LayoutSettings { get; set; }

		public unsafe int[] GenerateFramebuffer(int*[] src, int[] srcLength)
		{
			if (src.Length != LayoutSettings.Locations.Length)
				return null;

			var ret = new int[LayoutSettings.FinalSize.Width * LayoutSettings.FinalSize.Height];
			for (int iBuf = 0; iBuf < src.Length; iBuf++)
			{
				int screen = LayoutSettings.Order[iBuf];
				Size size = _sizes[screen];
				Point position = LayoutSettings.Locations[screen];

				int minSrcX = Math.Max(-position.X, 0);
				int maxSrcX = Math.Min(LayoutSettings.FinalSize.Width - position.X, size.Width);
				int minDstX = Math.Max(position.X, 0);

				int minSrcY = Math.Max(-position.Y, 0);
				int maxSrcY = Math.Min(LayoutSettings.FinalSize.Height - position.Y, size.Height);
				int minDstY = Math.Max(position.Y, 0);

				if ((maxSrcX - 1) + (maxSrcY - 1) * size.Width > srcLength[iBuf])
					throw new ArgumentException("The given source buffer is smaller than expected.");

				for (int iY = minSrcY; iY < maxSrcY; iY++)
				{
					int dstIndex = minDstX + (minDstY + iY - minSrcY) * LayoutSettings.FinalSize.Width;
					int srcIndex = minSrcX + iY * size.Width;
					for (int iX = minSrcX; iX < maxSrcX; iX++)
						ret[dstIndex++] = src[screen][srcIndex++];
				}
			}

			return ret;
		}
	}

	public class ScreenLayoutSettings
	{
		public Point[] Locations { get; set; }
		public int[] Order { get; set; }
		public Size FinalSize { get; set; }
	}
}
