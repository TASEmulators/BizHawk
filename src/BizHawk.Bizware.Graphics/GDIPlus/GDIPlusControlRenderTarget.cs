using System.Drawing;

using SDGraphics = System.Drawing.Graphics;

namespace BizHawk.Bizware.Graphics
{
	public sealed class GDIPlusControlRenderTarget : IDisposable
	{
		private readonly Func<(SDGraphics, Rectangle)> _getControlRenderContext;
		private BufferedGraphicsContext _bufferedGraphicsContext = new();

		public SDGraphics ControlGraphics;
		public BufferedGraphics BufferedGraphics;

		internal GDIPlusControlRenderTarget(Func<(SDGraphics Graphics, Rectangle Rectangle)> getControlRenderContext)
			=> _getControlRenderContext = getControlRenderContext;

		public void Dispose()
		{
			ControlGraphics?.Dispose();
			ControlGraphics = null;
			BufferedGraphics?.Dispose();
			BufferedGraphics = null;
			_bufferedGraphicsContext?.Dispose();
			_bufferedGraphicsContext = null;
		}

		public void CreateGraphics()
		{
			ControlGraphics?.Dispose();
			BufferedGraphics?.Dispose();
			(ControlGraphics, var r) = _getControlRenderContext();
			BufferedGraphics = _bufferedGraphicsContext.Allocate(ControlGraphics, r);
		}
	}
}
