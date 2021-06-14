using System;
using System.Drawing;

namespace BizHawk.Bizware.BizwareGL
{
	public class RenderTargetWrapper
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
			MyBufferedGraphics = _getBufferedGraphicsContext().Allocate(refGraphics, r);
//			MyBufferedGraphics.Graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

			//not sure about this stuff...
			//it will wreck alpha blending, for one thing
//			MyBufferedGraphics.Graphics.CompositingMode = CompositingMode.SourceCopy;
//			MyBufferedGraphics.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
		}
	}
}
