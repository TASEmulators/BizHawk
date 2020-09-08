using System;
using System.Windows.Forms;

using BizHawk.Bizware.BizwareGL;
using SlimDX.Direct3D9;

namespace BizHawk.Client.EmuHawk
{
	public class GLControlWrapperSlimDX9 : Control, IGraphicsControl
	{
		public GLControlWrapperSlimDX9(IGL_SlimDX9 sdx)
		{
			_sdx = sdx;

			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserMouse, true);

			Resize += GLControlWrapper_SlimDX_Resize;
		}

		public bool Vsync;

		private void GLControlWrapper_SlimDX_Resize(object sender, EventArgs e)
		{
			_sdx.RefreshControlSwapChain(this);
		}

		protected override void Dispose(bool disposing)
		{
			_sdx.FreeControlSwapChain(this);

			base.Dispose(disposing);
		}

		private readonly IGL_SlimDX9 _sdx;

		public Control Control => this;
		public SwapChain SwapChain;

		public void SetVsync(bool state)
		{
			Vsync = state;
			_sdx.RefreshControlSwapChain(this);
		}

		public void Begin()
		{
			_sdx.BeginControl(this);
		}

		public void End()
		{
			_sdx.EndControl(this);
		}

		public void SwapBuffers()
		{
			_sdx.SwapControl(this);
		}
	}
}