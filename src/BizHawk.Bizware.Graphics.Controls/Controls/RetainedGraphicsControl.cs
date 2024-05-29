using System.Windows.Forms;

namespace BizHawk.Bizware.Graphics.Controls
{
	/// <summary>
	/// Adapts a GraphicsControl to gain the power of remembering what was drawn to it, and keeping it presented in response to Paint events
	/// </summary>
	public class RetainedGraphicsControl : GraphicsControl
	{
		public RetainedGraphicsControl(IGL gl)
		{
			_gl = gl;
			_graphicsControl = GraphicsControlFactory.CreateGraphicsControl(gl);
			_guiRenderer = new(gl);

			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserMouse, true);

			_graphicsControl.Dock = DockStyle.Fill;
			_graphicsControl.MouseDoubleClick += (_, e) => OnMouseDoubleClick(e);
			_graphicsControl.MouseClick += (_, e) => OnMouseClick(e);
			_graphicsControl.MouseEnter += (_, e) => OnMouseEnter(e);
			_graphicsControl.MouseLeave += (_, e) => OnMouseLeave(e);
			_graphicsControl.MouseMove += (_, e) => OnMouseMove(e);
			_graphicsControl.Paint += (_, e) => OnPaint(e);
			Controls.Add(_graphicsControl);
		}

		/// <summary>
		/// Control whether rendering goes into the retaining buffer (it's slower) or straight to the viewport.
		/// This could be useful as a performance hack, or if someone was very clever, they could wait for a control to get deactivated, enable retaining, and render it one more time.
		/// Of course, even better still is to be able to repaint viewports continually, but sometimes that's annoying.
		/// </summary>
		public bool Retain
		{
			get => _retain;
			set
			{
				if (_retain && !value)
				{
					_rt.Dispose();
					_rt = null;
				}
				_retain = value;
			}
		}

		private bool _retain = true;

		private readonly IGL _gl;
		private readonly GraphicsControl _graphicsControl;
		private IRenderTarget _rt;
		private readonly GuiRenderer _guiRenderer;

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			// todo - check whether we're begun?
			_graphicsControl.Begin();
			Draw();
			_graphicsControl.End();
		}

		public override void AllowTearing(bool state)
			=> _graphicsControl.AllowTearing(state);

		public override void SetVsync(bool state)
			=> _graphicsControl.SetVsync(state);

		public override void Begin()
		{
			_graphicsControl.Begin();

			if (_retain)
			{
				// TODO - frugalize me
				_rt?.Dispose();
				_rt = _gl.CreateRenderTarget(Width, Height);
				_rt.Bind();
			}
		}

		public override void End()
		{
			if (_retain)
			{
				_gl.BindDefaultRenderTarget();
			}

			_graphicsControl.End();
		}

		public override void SwapBuffers()
		{
			// if we're not retaining, then we haven't been collecting into a framebuffer. just swap it
			if (!_retain)
			{
				_graphicsControl.SwapBuffers();
				return;
			}
			
			// if we're retaining, then we cant draw until we unbind! its semantically a bit odd, but we expect users to call SwapBuffers() before end, so we cant unbind in End() even thoug hit makes a bit more sense.
			_gl.BindDefaultRenderTarget();
			Draw();
		}

		private void Draw()
		{
			if (_rt == null)
			{
				return;
			}

			_guiRenderer.Begin(Width, Height);
			_guiRenderer.DisableBlending();
			_guiRenderer.Draw(_rt);
			_guiRenderer.End();
			_graphicsControl.SwapBuffers();
		}
	}
}
