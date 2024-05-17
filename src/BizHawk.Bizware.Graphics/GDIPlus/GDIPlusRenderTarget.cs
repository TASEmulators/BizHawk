using System;
using System.Drawing;

using SDGraphics = System.Drawing.Graphics;

namespace BizHawk.Bizware.Graphics
{
	public class GDIPlusRenderTarget : IDisposable
	{
		internal GDIPlusRenderTarget(Func<BufferedGraphicsContext> getBufferedGraphicsContext,
			Func<(SDGraphics Graphics, Rectangle Rectangle)> getControlRenderContext = null)
		{
			_getBufferedGraphicsContext = getBufferedGraphicsContext;
			_getControlRenderContext = getControlRenderContext;
		}

		public void Dispose()
		{
			if (_getControlRenderContext != null)
			{
				CurGraphics?.Dispose();
				CurGraphics = null;
			}

			BufferedGraphics?.Dispose();
			BufferedGraphics = null;
		}

		private readonly Func<BufferedGraphicsContext> _getBufferedGraphicsContext;

		/// <summary>
		/// get Graphics and Rectangle from a control, if any
		/// </summary>
		private readonly Func<(SDGraphics, Rectangle)> _getControlRenderContext;

		/// <summary>
		/// the offscreen render target, if that's what this is representing
		/// </summary>
		public RenderTarget Target;

		public SDGraphics CurGraphics;
		public BufferedGraphics BufferedGraphics;

		public void CreateGraphics()
		{
			Rectangle r;
			if (_getControlRenderContext != null)
			{
				(CurGraphics, r) = _getControlRenderContext();
			}
			else
			{
				var gtex = (GDIPlusTexture2D)Target.Texture2D;
				CurGraphics?.Dispose();
				CurGraphics = SDGraphics.FromImage(gtex.SDBitmap);
				r = gtex.GetRectangle();
			}

			BufferedGraphics?.Dispose();
			BufferedGraphics = _getBufferedGraphicsContext().Allocate(CurGraphics, r);
		}
	}
}
