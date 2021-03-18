using System.Windows.Forms;
using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.OpenTK3;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Adapts a GraphicsControl to gain the power of remembering what was drawn to it, and keeping it presented in response to Paint events
	/// </summary>
	public class RetainedGraphicsControl : GraphicsControl
	{
		public RetainedGraphicsControl(IGL gl)
			: base(gl)
		{
			_gl = gl;
			_guiRenderer = new GuiRenderer(gl);
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
		private RenderTarget _rt;
		private readonly GuiRenderer _guiRenderer;

		public override void Begin()
		{
			base.Begin();
			
			if (_retain)
			{
				// TODO - frugalize me
				_rt?.Dispose();
				_rt = _gl.CreateRenderTarget(Width, Height);
				_rt.Bind();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			// todo - check whether we're begun?
			base.Begin();
			Draw();
			base.End();
		}

		private void Draw()
		{
			if (_rt == null)
			{
				return;
			}

			_guiRenderer.Begin(Width, Height);
			_guiRenderer.SetBlendState(_gl.BlendNoneCopy);
			_guiRenderer.Draw(_rt.Texture2d);
			_guiRenderer.End();
			base.SwapBuffers();
		}

		public override void SwapBuffers()
		{
			// if we're not retaining, then we haven't been collecting into a framebuffer. just swap it
			if (!_retain)
			{
				base.SwapBuffers();
				return;
			}
			
			// if we're retaining, then we cant draw until we unbind! its semantically a bit odd, but we expect users to call SwapBuffers() before end, so we cant unbind in End() even thoug hit makes a bit more sense.
			_rt.Unbind();
			Draw();
		}

		public override void End()
		{
			if (_retain)
			{
				_rt.Unbind();
			}

			base.End();
		}
	}
}