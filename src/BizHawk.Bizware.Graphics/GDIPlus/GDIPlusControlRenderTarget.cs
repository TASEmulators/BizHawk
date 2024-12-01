using System.Drawing;

using SDGraphics = System.Drawing.Graphics;

namespace BizHawk.Bizware.Graphics
{
	public readonly record struct GDIPlusControlRenderContext(SDGraphics Graphics, Rectangle Rectangle) : IDisposable
	{
		public void Dispose()
			=> Graphics.Dispose();
	}

	public sealed class GDIPlusControlRenderTarget : IDisposable
	{
		private readonly Func<GDIPlusControlRenderContext> _getControlRenderContext;
		private BufferedGraphicsContext _bufferedGraphicsContext = new();

		public BufferedGraphics BufferedGraphics;

		internal GDIPlusControlRenderTarget(Func<GDIPlusControlRenderContext> getControlRenderContext)
			=> _getControlRenderContext = getControlRenderContext;

		public void Dispose()
		{
			BufferedGraphics?.Dispose();
			BufferedGraphics = null;
			_bufferedGraphicsContext?.Dispose();
			_bufferedGraphicsContext = null;
		}

		public void CreateBufferedGraphics()
		{
			BufferedGraphics?.Dispose();
			using var controlRenderContext = _getControlRenderContext();
			BufferedGraphics = _bufferedGraphicsContext.Allocate(controlRenderContext.Graphics, controlRenderContext.Rectangle);
		}
	}
}
