using System;
using System.Drawing;

namespace BizHawk.Bizware.Graphics
{
	public class GDIPlusTexture : IDisposable
	{
		public Bitmap SDBitmap;
		public bool LinearFiltering;

		public void Dispose()
		{
			SDBitmap?.Dispose();
			SDBitmap = null;
		}
	}
}
