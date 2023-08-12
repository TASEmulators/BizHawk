using System;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Bizware.Graphics.Controls
{
	internal sealed class D3D9Control : GraphicsControl
	{
		private readonly Func<IntPtr, D3D9SwapChain> _createSwapChain;
		private D3D9SwapChain _swapChain;
		private bool Vsync;

		public D3D9Control(Func<IntPtr, D3D9SwapChain> createSwapChain)
		{
			_createSwapChain = createSwapChain;

			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserMouse, true);
			DoubleBuffered = false;
		}

		protected override Size DefaultSize => new(1, 1);

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			_swapChain = _createSwapChain(Handle);
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			base.OnHandleDestroyed(e);
			_swapChain.Dispose();
			_swapChain = null;
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			_swapChain.Refresh(Vsync);
		}

		public override void SetVsync(bool state)
		{
			if (Vsync != state)
			{
				Vsync = state;
				_swapChain.Refresh(Vsync);
			}
		}

		public override void Begin()
			=> _swapChain.SetBackBuffer();

		public override void End()
			=> _swapChain.SetBackBuffer();

		public override void SwapBuffers()
			=> _swapChain.PresentBuffer();
	}
}