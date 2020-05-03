using System;

using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL
{
	public class GDIPTextureWrapper : IDisposable
	{
		public System.Drawing.Bitmap SDBitmap;
		public TextureMinFilter MinFilter = TextureMinFilter.Nearest;
		public TextureMagFilter MagFilter = TextureMagFilter.Nearest;
		public void Dispose()
		{
			if (SDBitmap != null)
			{
				SDBitmap.Dispose();
				SDBitmap = null;
			}
		}
	}
}
