using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Bizware.BizwareGL;
using SlimDX.Direct3D9;

namespace BizHawk.Bizware.BizwareGL.Drivers.SlimDX
{
	public class GLControlWrapper_SlimDX9 : Control, IGraphicsControl
	{

		public GLControlWrapper_SlimDX9(IGL_SlimDX9 sdx)
		{
			this.sdx = sdx;

			//uhhh not sure what we need to be doing here
			//SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserMouse, true);

			Resize += new EventHandler(GLControlWrapper_SlimDX_Resize);
		}

		public bool Vsync;

		void GLControlWrapper_SlimDX_Resize(object sender, EventArgs e)
		{
			sdx.RefreshControlSwapChain(this);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			//if(MyBufferedGraphics != null)
		}

		IGL_SlimDX9 sdx;

		public Control Control { get { return this; } }
		public SwapChain SwapChain;

		public void SetVsync(bool state)
		{
			Vsync = state;
			sdx.RefreshControlSwapChain(this);
		}

		public void Begin()
		{
			sdx.BeginControl(this);
		}

		public void End()
		{
			sdx.EndControl(this);
		}

		public void SwapBuffers()
		{
			sdx.SwapControl(this);
		}
	}
}