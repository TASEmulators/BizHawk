using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

/// <summary>
/// Provides a way to arrange displays inside a frame buffer.
/// </summary>
namespace BizHawk.Emulation.Common
{
	public class ScreenArranger
	{
		public ScreenLayoutSettings layoutSettings;
		private Size[] sizes;

		public ScreenArranger(Size[] sizes)
		{
			this.sizes = sizes;
		}

		public unsafe int[] GenerateFramebuffer(int*[] src, int[] srcLength)
		{
			if (src.Length != layoutSettings.locations.Length)
				return null;

			int[] ret = new int[layoutSettings.finalSize.Width * layoutSettings.finalSize.Height];
			for (int iBuf = 0; iBuf < src.Length; iBuf++)
			{
				int screen = layoutSettings.order[iBuf];
				Size size = sizes[screen];
				Point position = layoutSettings.locations[screen];

				int minSrcX = Math.Max(-position.X, 0);
				int maxSrcX = Math.Min(layoutSettings.finalSize.Width - position.X, size.Width);
				int minDstX = Math.Max(position.X, 0);

				int minSrcY = Math.Max(-position.Y, 0);
				int maxSrcY = Math.Min(layoutSettings.finalSize.Height - position.Y, size.Height);
				int minDstY = Math.Max(position.Y, 0);

				if ((maxSrcX - 1) + (maxSrcY - 1) * size.Width > srcLength[iBuf])
					throw new ArgumentException("The given source buffer is smaller than expected.");

				for (int iY = minSrcY; iY < maxSrcY; iY++)
				{
					int dstIndex = minDstX + (minDstY + iY - minSrcY) * layoutSettings.finalSize.Width;
					int srcIndex = minSrcX + iY * size.Width;
					for (int iX = minSrcX; iX < maxSrcX; iX++)
						ret[dstIndex++] = src[screen][srcIndex++];
				}
			}

			return ret;
		}

		public unsafe int[] GenerateFramebuffer(int[][] src)
		{
			GCHandle[] handles = new GCHandle[src.Length];
			try
			{
				for (int i = 0; i < src.Length; i++)
					handles[i] = GCHandle.Alloc(src[i], GCHandleType.Pinned);

				int*[] srcBuffers = new int*[src.Length];
				int[] lengths = new int[src.Length];
				for (int i = 0; i < src.Length; i++)
				{
					srcBuffers[i] = (int*)handles[i].AddrOfPinnedObject();
					lengths[i] = src[i].Length;
				}

				return GenerateFramebuffer(srcBuffers, lengths);
			}
			finally
			{   // unpin the memory
				foreach (var h in handles)
					if (h.IsAllocated) h.Free();
			}
		}
	}

	public class ScreenLayoutSettings
	{
		public Point[] locations;
		public int[] order;
		public Size finalSize;
	}
}
