using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using SDGraphics = System.Drawing.Graphics;

namespace BizHawk.Bizware.Graphics.Controls
{
	internal sealed class GDIPlusControl : GraphicsControl
	{
		/// <summary>
		/// The render target for rendering to this control
		/// </summary>
		private readonly GDIPlusControlRenderTarget _renderTarget;

		public GDIPlusControl(Func<Func<(SDGraphics Graphics, Rectangle Rectangle)>, GDIPlusControlRenderTarget> createControlRenderTarget)
		{
			_renderTarget = createControlRenderTarget(GetControlRenderContext);

			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserMouse, true);
			DoubleBuffered = true;
		}

		private (SDGraphics Graphics, Rectangle Rectangle) GetControlRenderContext()
		{
			var graphics = CreateGraphics();
			graphics.CompositingMode = CompositingMode.SourceCopy;
			graphics.CompositingQuality = CompositingQuality.HighSpeed;
			return (graphics, ClientRectangle);
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
			_renderTarget.CreateGraphics();
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			base.OnHandleDestroyed(e);
			_renderTarget.Dispose();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			_renderTarget.CreateGraphics();
		}

		public override void SwapBuffers()
			=> _renderTarget.BufferedGraphics?.Render(_renderTarget.ControlGraphics);
	}
}
