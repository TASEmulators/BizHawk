using System;
using System.Windows.Forms;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Common;

using SharpDX.Direct3D9;

namespace BizHawk.Bizware.Graphics
{
	internal sealed class D3D9Control : Control, IGraphicsControl
	{
		private readonly IGL_D3D9 _owner;
		internal SwapChain SwapChain;
		internal bool Vsync;

		public D3D9Control(IGL_D3D9 owner)
		{
			_owner = owner;

			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserMouse, true);
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
			IGL_D3D9.FreeControlSwapChain(this);
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