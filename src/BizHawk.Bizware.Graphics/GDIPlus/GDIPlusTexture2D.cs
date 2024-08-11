using System.Drawing;
using System.Drawing.Imaging;

namespace BizHawk.Bizware.Graphics
{
	internal class GDIPlusTexture2D : ITexture2D
	{
		public Bitmap SDBitmap;
		public bool LinearFiltering;

		public int Width { get; }
		public int Height { get; }
		public bool IsUpsideDown => false;

		public GDIPlusTexture2D(int width, int height)
		{
			Width = width;
			Height = height;
			SDBitmap = new(width, height, PixelFormat.Format32bppArgb);
		}

		public virtual void Dispose()
		{
			SDBitmap?.Dispose();
			SDBitmap = null;
		}

		public BitmapBuffer Resolve()
		{
			var blo = new BitmapLoadOptions
			{
				AllowWrap = false // must be an independent resource
			};

			return new(SDBitmap, blo);
		}

		public void LoadFrom(BitmapBuffer buffer)
			=> buffer.ToSysdrawingBitmap(SDBitmap);

		public void SetFilterLinear()
			=> LinearFiltering = true;

		public void SetFilterNearest()
			=> LinearFiltering = false;

		public override string ToString()
			=> $"GDI+ Texture2D: {Width}x{Height}";
	}
}
