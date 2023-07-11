#if false
using System;
using System.Windows.Forms;

using BizHawk.Bizware.BizwareGL;

using Vortice.DXGI;

namespace BizHawk.Bizware.Graphics
{
	internal sealed class D3D11Control : Control, IGraphicsControl
	{
		private readonly IGL_D3D11 _owner;
		internal IDXGISwapChain SwapChain;
		internal bool Vsync;

		public D3D11Control(IGL_D3D11 owner)
		{
			_owner = owner;

			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			DoubleBuffered = false;
		}

		public RenderTargetWrapper RenderTargetWrapper
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			_owner.RefreshControlSwapChain(this);
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			base.OnHandleDestroyed(e);
			IGL_D3D11.FreeControlSwapChain(this);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			_owner.RefreshControlSwapChain(this);
		}

		public void SetVsync(bool state)
		{
			Vsync = state;
			_owner.RefreshControlSwapChain(this);
		}

		public void Begin()
			=> _owner.BeginControl(this);

		public void End()
			=> _owner.EndControl(this);

		public void SwapBuffers()
			=> _owner.SwapControl(this);
	}
}
#endif