using SDGraphics = System.Drawing.Graphics;

namespace BizHawk.Bizware.Graphics
{
	internal sealed class GDIPlusRenderTarget : GDIPlusTexture2D, IRenderTarget
	{
		private readonly IGL_GDIPlus _gdiPlus;
		public SDGraphics TextureGraphics;

		internal GDIPlusRenderTarget(IGL_GDIPlus gdiPlus, int width, int height)
			: base(width, height)
		{
			_gdiPlus = gdiPlus;
			TextureGraphics = SDGraphics.FromImage(SDBitmap);
		}

		public override void Dispose()
		{
			TextureGraphics?.Dispose();
			TextureGraphics = null;
			base.Dispose();
		}

		public void Bind()
			=> _gdiPlus.CurRenderTarget = this;

		public override string ToString()
			=> $"GDI+ RenderTarget: {Width}x{Height}";
	}
}
