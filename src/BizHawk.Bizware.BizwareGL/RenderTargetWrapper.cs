using System;
using System.Drawing;
#if false
using System.Drawing.Drawing2D;
#endif

namespace BizHawk.Bizware.BizwareGL
{
	public class RenderTargetWrapper : IDisposable
	{
		public RenderTargetWrapper(
			Func<BufferedGraphicsContext> getBufferedGraphicsContext,
			IGraphicsControl control = null)
		{
			_getBufferedGraphicsContext = getBufferedGraphicsContext;
			Control = control;
		}

		public void Dispose()
		{
			MyBufferedGraphics?.Dispose();
		}

		private readonly Func<BufferedGraphicsContext> _getBufferedGraphicsContext;

		/// <summary>
		/// the control associated with this render target (if any)
		/// </summary>
		private readonly IGraphicsControl Control;

		/// <summary>
		/// the offscreen render target, if that's what this is representing
		/// </summary>
		public RenderTarget Target;

		public BufferedGraphics MyBufferedGraphics;

		public Graphics refGraphics; // ?? hacky?

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
				var tw = (GDIPTextureWrapper)Target.Texture2d.Opaque;
				r = Target.Texture2d.Rectangle;
				refGraphics = Graphics.FromImage(tw.SDBitmap);
			}

			MyBufferedGraphics?.Dispose();
			MyBufferedGraphics = _getBufferedGraphicsContext().Allocate(refGraphics, r);
#if false
			MyBufferedGraphics.Graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

			// not sure about this stuff...
			// it will wreck alpha blending, for one thing
			MyBufferedGraphics.Graphics.CompositingMode = CompositingMode.SourceCopy;
			MyBufferedGraphics.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
#endif
		}
	}
}
