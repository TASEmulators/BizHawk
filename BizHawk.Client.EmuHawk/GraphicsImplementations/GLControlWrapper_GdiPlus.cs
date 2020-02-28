using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{
	public class GLControlWrapper_GdiPlus : Control, IGraphicsControl
	{
		public GLControlWrapper_GdiPlus(IGL_GdiPlus gdi)
		{
			_gdi = gdi;
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserMouse, true);
		}

		private readonly IGL_GdiPlus _gdi;

		public Control Control => this;


		/// <summary>
		/// the render target for rendering to this control
		/// </summary>
		public IGL_GdiPlus.RenderTargetWrapper RenderTargetWrapper;

		public void SetVsync(bool state)
		{
			// not really supported now...
		}

		public void Begin()
		{
			_gdi.BeginControl(this);
			RenderTargetWrapper.CreateGraphics();
			//using (var g = CreateGraphics())
			//  MyBufferedGraphics = Gdi.MyBufferedGraphicsContext.Allocate(g, ClientRectangle);
			//MyBufferedGraphics.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;

			////not sure about this stuff...
			////it will wreck alpha blending, for one thing
			//MyBufferedGraphics.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
			//MyBufferedGraphics.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
		}

		public void End()
		{
			_gdi.EndControl(this);
		}

		public void SwapBuffers()
		{
			_gdi.SwapControl(this);
			if (RenderTargetWrapper.MyBufferedGraphics == null)
			{
				return;
			}

			using (var g = CreateGraphics())
			{
				// not sure we had proof we needed this but it cant hurt
				g.CompositingMode = CompositingMode.SourceCopy;
				g.CompositingQuality = CompositingQuality.HighSpeed;
				RenderTargetWrapper.MyBufferedGraphics.Render(g);
			}

			// not too sure about this.. i think we have to re-allocate it so we can support a changed window size. did we do this at the right time anyway?
			// maybe I should try caching harder, I hate to reallocate these constantly
			RenderTargetWrapper.CreateGraphics();
		}
	}
}