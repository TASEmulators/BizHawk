using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Bizware.BizwareGL;
using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// Adapts a GraphicsControl to gain the power of remembering what was drawn to it, and keeping it presented in response to Paint events
	/// </summary>
	public class RetainedGraphicsControl : GraphicsControl
	{
		public RetainedGraphicsControl(IGL gl)
			: base(gl)
		{
			GL = gl;
			GuiRenderer = new GuiRenderer(gl);
		}

		/// <summary>
		/// Control whether rendering goes into the retaining buffer (it's slower) or straight to the viewport.
		/// This could be useful as a performance hack, or if someone was very clever, they could wait for a control to get deactivated, enable retaining, and render it one more time.
		/// Of course, even better still is to be able to repaint viewports continually, but sometimes thats annoying.
		/// </summary>
		public bool Retain
		{
			get { return _retain; }
			set
			{
				if (_retain && !value)
				{
					rt.Dispose();
					rt = null;
				}
				_retain = value;
			}
		}
		bool _retain = true;

		IGL GL;
		RenderTarget rt;
		GuiRenderer GuiRenderer;

		public override void Begin()
		{
			base.Begin();
			
			if (_retain)
			{
				//TODO - frugalize me
				if (rt != null)
					rt.Dispose();
				rt = GL.CreateRenderTarget(Width, Height);
				rt.Bind();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			//todo - check whether we're begun?
			base.Begin();
			Draw();
			base.End();
		}

		void Draw()
		{
			if (rt == null) return;
			GuiRenderer.Begin(Width, Height);
			GuiRenderer.SetBlendState(GL.BlendNoneCopy);
			GuiRenderer.Draw(rt.Texture2d);
			GuiRenderer.End();
			base.SwapBuffers();
		}

		public override void SwapBuffers()
		{
			//if we're not retaining, then we havent been collecting into a framebuffer. just swap it
			if (!_retain)
			{
				base.SwapBuffers();
				return;
			}
			
			//if we're retaining, then we cant draw until we unbind! its semantically a bit odd, but we expect users to call SwapBuffers() before end, so we cant unbind in End() even thoug hit makes a bit more sense.
			rt.Unbind();
			Draw();
		}

		public override void End()
		{
			if (_retain)
				rt.Unbind();
			base.End();
		}
	}

}