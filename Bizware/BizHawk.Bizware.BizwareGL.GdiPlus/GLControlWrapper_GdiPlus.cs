using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Bizware.BizwareGL;


namespace BizHawk.Bizware.BizwareGL.Drivers.GdiPlus
{
	public class GLControlWrapper_GdiPlus : Control, IGraphicsControl
	{

		public GLControlWrapper_GdiPlus(IGL_GdiPlus gdi)
		{
			Gdi = gdi;
			//uhhh not sure what we need to be doing here
			//SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserMouse, true);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			//if(MyBufferedGraphics != null)
		}

		IGL_GdiPlus Gdi;

		public Control Control { get { return this; } }


		/// <summary>
		/// the render target for rendering to this control
		/// </summary>
		public IGL_GdiPlus.RenderTargetWrapper RenderTargetWrapper;

		public void SetVsync(bool state)
		{
			//not really supported now...
		}

		public void Begin()
		{
			Gdi.BeginControl(this);
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
			Gdi.EndControl(this);
		}

		public void SwapBuffers()
		{
			Gdi.SwapControl(this);
			if (RenderTargetWrapper.MyBufferedGraphics == null)
				return;

			using (var g = CreateGraphics())
			{
				//not sure we had proof we needed this but it cant hurt
				g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
				g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
				RenderTargetWrapper.MyBufferedGraphics.Render(g);
			}

			//not too sure about this.. i think we have to re-allocate it so we can support a changed window size. did we do this at the right time anyway?
			//maybe I should try caching harder, I hate to reallocate these constantly
			RenderTargetWrapper.CreateGraphics();
		}
	}
}