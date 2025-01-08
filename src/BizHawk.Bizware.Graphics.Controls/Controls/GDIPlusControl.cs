using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BizHawk.Bizware.Graphics.Controls
{
	internal sealed class GDIPlusControl : GraphicsControl
	{
		/// <summary>
		/// The render target for rendering to this control
		/// </summary>
		private readonly GDIPlusControlRenderTarget _renderTarget;

		public GDIPlusControl(Func<Func<GDIPlusControlRenderContext>, GDIPlusControlRenderTarget> createControlRenderTarget)
		{
			_renderTarget = createControlRenderTarget(GetControlRenderContext);

			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserMouse, true);
			DoubleBuffered = false;
		}

		private GDIPlusControlRenderContext GetControlRenderContext()
		{
			var graphics = CreateGraphics();
			graphics.CompositingMode = CompositingMode.SourceCopy;
			graphics.CompositingQuality = CompositingQuality.HighSpeed;
			return new(graphics, ClientRectangle with
			{
				Width = Math.Max(ClientRectangle.Width, 1),
				Height = Math.Max(ClientRectangle.Height, 1)
			});
		}

		public override void AllowTearing(bool state)
		{
			// not controllable
		}

		public override void SetVsync(bool state)
		{
			// not really supported now...
		}

		public override void Begin()
		{
		}

		public override void End()
		{
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			_renderTarget.CreateBufferedGraphics();
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			base.OnHandleDestroyed(e);
			_renderTarget.Dispose();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			_renderTarget.CreateBufferedGraphics();
		}

		public override void SwapBuffers()
		{
			if (_renderTarget.BufferedGraphics is null)
			{
				return;
			}

			using var graphics = CreateGraphics();
			graphics.CompositingMode = CompositingMode.SourceCopy;
			graphics.CompositingQuality = CompositingQuality.HighSpeed;
			_renderTarget.BufferedGraphics.Render(graphics);
		}
	}
}
