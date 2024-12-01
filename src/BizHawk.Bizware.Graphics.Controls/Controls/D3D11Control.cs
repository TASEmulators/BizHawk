using System.Windows.Forms;

namespace BizHawk.Bizware.Graphics.Controls
{
	internal sealed class D3D11Control : GraphicsControl
	{
		private readonly Func<D3D11SwapChain.ControlParameters, D3D11SwapChain> _createSwapChain;
		private D3D11SwapChain _swapChain;
		private bool Vsync, AllowsTearing;

		private D3D11SwapChain.ControlParameters ControlParameters => new(Handle, Width, Height, Vsync, AllowsTearing);

		public D3D11Control(Func<D3D11SwapChain.ControlParameters, D3D11SwapChain> createSwapChain)
		{
			_createSwapChain = createSwapChain;

			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserMouse, true);
			DoubleBuffered = false;
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			_swapChain = _createSwapChain(ControlParameters);
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
			_swapChain.Refresh(ControlParameters);
		}

		public override void AllowTearing(bool state)
			=> AllowsTearing = state;

		public override void SetVsync(bool state)
			=> Vsync = state;

		public override void Begin()
		{
		}

		public override void End()
		{
		}

		public override void SwapBuffers()
			=> _swapChain.PresentBuffer(ControlParameters);
	}
}