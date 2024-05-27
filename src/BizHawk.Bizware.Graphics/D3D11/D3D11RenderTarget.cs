using Vortice.Direct3D11;
using Vortice.DXGI;

namespace BizHawk.Bizware.Graphics
{
	internal sealed class D3D11RenderTarget : D3D11Texture2D, IRenderTarget
	{
		public ID3D11RenderTargetView RTV;

		public D3D11RenderTarget(D3D11Resources resources, int width, int height)
			: base(resources, BindFlags.ShaderResource | BindFlags.RenderTarget, ResourceUsage.Default, CpuAccessFlags.None, width, height)
		{
		}

		public override void CreateTexture()
		{
			base.CreateTexture();
			var rtvd = new RenderTargetViewDescription(RenderTargetViewDimension.Texture2D, Format.B8G8R8A8_UNorm);
			RTV = Device.CreateRenderTargetView(Texture, rtvd);
		}

		public override void DestroyTexture()
		{
			RTV?.Dispose();
			RTV = null;
			base.DestroyTexture();
		}

		public void Bind()
		{
			Context.OMSetRenderTargets(RTV);
			_resources.CurRenderTarget = this;
		}

		public override string ToString()
			=> $"D3D11 RenderTarget: {Width}x{Height}";
	}
}
