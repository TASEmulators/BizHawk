using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using SDGraphics = System.Drawing.Graphics;

namespace BizHawk.Bizware.Graphics.Controls
{
	internal sealed class GDIPlusControl : GraphicsControl
	{
		public GDIPlusControl(Func<Func<(SDGraphics Graphics, Rectangle Rectangle)>, GDIPlusRenderTarget> createControlRenderTarget)
		{
			RenderTarget = createControlRenderTarget(GetControlRenderContext);

			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserMouse, true);
		}

		private (SDGraphics Graphics, Rectangle Rectangle) GetControlRenderContext()
			=> (CreateGraphics(), ClientRectangle);

		/// <summary>
		/// The render target for rendering to this control
		/// </summary>
		private GDIPlusRenderTarget RenderTarget { get; }

		public override void AllowTearing(bool state)
		{
			// not controllable
		}

		public override void SetVsync(bool state)
		{
			// not really supported now...
		}

		public override void Begin()
			=> RenderTarget.CreateGraphics();

		public override void End()
		{
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			RenderTarget.CreateGraphics();
		}

		public override void SwapBuffers()
		{
			if (RenderTarget.BufferedGraphics == null)
			{
				return;
			}

			using var g = CreateGraphics();
			// not sure we had proof we needed this but it cant hurt
			g.CompositingMode = CompositingMode.SourceCopy;
			g.CompositingQuality = CompositingQuality.HighSpeed;
			RenderTarget.BufferedGraphics.Render(g);
		}
	}
}