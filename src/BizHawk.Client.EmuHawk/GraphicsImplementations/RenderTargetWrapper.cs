using System.Drawing;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{
	public class RenderTargetWrapper
	{
		public RenderTargetWrapper(IGL_GdiPlus gdi)
		{
			Gdi = gdi;
		}

		public void Dispose()
		{
		}

		private readonly IGL_GdiPlus Gdi;

		/// <summary>
		/// the control associated with this render target (if any)
		/// </summary>
		public GLControlWrapper_GdiPlus Control;

		/// <summary>
		/// the offscreen render target, if that's what this is representing
		/// </summary>
		public RenderTarget Target;

		public BufferedGraphics MyBufferedGraphics;

		public Graphics refGraphics; //?? hacky?

		public void CreateGraphics()
		{
			Rectangle r;
			if (Control != null)
			{
				r = Control.ClientRectangle;
				refGraphics = Control.CreateGraphics();
			}
			else
			{
				var tw = Target.Texture2d.Opaque as GDIPTextureWrapper;
				r = Target.Texture2d.Rectangle;
				refGraphics = Graphics.FromImage(tw.SDBitmap);
			}

			MyBufferedGraphics?.Dispose();
			MyBufferedGraphics = Gdi.MyBufferedGraphicsContext.Allocate(refGraphics, r);
//			MyBufferedGraphics.Graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

			//not sure about this stuff...
			//it will wreck alpha blending, for one thing
//			MyBufferedGraphics.Graphics.CompositingMode = CompositingMode.SourceCopy;
//			MyBufferedGraphics.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
		}
	}
}
