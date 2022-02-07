using System;
using System.Drawing;

namespace BizHawk.Client.Common
{
	public interface IDisplaySurface : IDisposable
	{
		int Height { get; }

		int Width { get; }

		void Clear();

		/// <returns>a <see cref="Graphics"/> used to render to this surface; be sure to dispose it!</returns>
		Graphics GetGraphics();

		Bitmap PeekBitmap();
	}
}
