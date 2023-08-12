using System;
using System.Drawing;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Bizware.Graphics
{
	public class GDIPlusTexture : IDisposable
	{
		public Bitmap SDBitmap;
		public TextureMinFilter MinFilter = TextureMinFilter.Nearest;
		public TextureMagFilter MagFilter = TextureMagFilter.Nearest;

		public void Dispose()
		{
			SDBitmap?.Dispose();
			SDBitmap = null;
		}
	}
}
