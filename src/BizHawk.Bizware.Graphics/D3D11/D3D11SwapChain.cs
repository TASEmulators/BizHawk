using Vortice.Direct3D11;
using Vortice.DXGI;

using DXGIResultCode = Vortice.DXGI.ResultCode;

namespace BizHawk.Bizware.Graphics
{
	public sealed class D3D11SwapChain : IDisposable
	{
		public readonly struct ControlParameters
		{
			public readonly IntPtr Handle;
			public readonly int Width;
			public readonly int Height;
			public readonly bool Vsync;
			public readonly bool AllowsTearing;

			public ControlParameters(IntPtr handle, int width, int height, bool vsync, bool allowsTearing)
			{
				Handle = handle;
				Width = Math.Max(width, 1);
				Height = Math.Max(height, 1);
				Vsync = vsync;
				AllowsTearing = allowsTearing;
			}
		}

		internal class SwapChainResources : IDisposable
		{
			public ID3D11Device Device;
			public ID3D11DeviceContext Context;
			public ID3D11DeviceContext1 Context1;
			public ID3D11Texture2D BackBufferTexture;
			public ID3D11RenderTargetView RTV;
			public IDXGISwapChain SwapChain;
			public bool HasFlipModel;
			public bool AllowsTearing;

			public void Dispose()
			{
				// Device/Context not owned by this class
				Device = null;
				Context = null;
				Context1?.Dispose();
				Context1 = null;
				RTV?.Dispose();
				RTV = null;
				BackBufferTexture?.Dispose();
				BackBufferTexture = null;
				SwapChain?.Dispose();
				SwapChain = null;
			}
		}

		private static readonly SharpGen.Runtime.Result D3DDDIERR_DEVICEREMOVED = new(2289436784UL);

		private readonly SwapChainResources _resources;
		private readonly Action<ControlParameters> _resetDeviceCallback;

		private ID3D11Device Device => _resources.Device;
		private ID3D11DeviceContext Context => _resources.Context;
		private ID3D11DeviceContext1 Context1 => _resources.Context1;
		private ID3D11Texture2D BackBufferTexture => _resources.BackBufferTexture;
		private ID3D11RenderTargetView RTV => _resources.RTV;
		private IDXGISwapChain SwapChain => _resources.SwapChain;
		private bool HasFlipModel => _resources.HasFlipModel;
		private bool AllowsTearing => _resources.AllowsTearing;

		internal D3D11SwapChain(SwapChainResources resources, Action<ControlParameters> resetDeviceCallback)
		{
			_resources = resources;
			_resetDeviceCallback = resetDeviceCallback;
		}

		public void Dispose()
			=> _resources.Dispose();

		public void PresentBuffer(ControlParameters cp)
		{
			Context.OMSetRenderTargets(RTV);

			PresentFlags presentFlags;
			if (cp.Vsync || !HasFlipModel)
			{
				presentFlags = PresentFlags.None;
			}
			else
			{
				presentFlags = cp.AllowsTearing && AllowsTearing ? PresentFlags.AllowTearing : PresentFlags.DoNotWait;
			}

			var result = SwapChain.Present(cp.Vsync ? 1 : 0, presentFlags);
			if (result == DXGIResultCode.DeviceReset
				|| result == DXGIResultCode.DeviceRemoved
				|| result == D3DDDIERR_DEVICEREMOVED)
			{
				_resetDeviceCallback(cp);
			}

			// optimization hint to the GPU (note: not always available, needs Win8+ or Win7 with the Platform Update)
			Context1?.DiscardView(RTV);
		}

		public void Refresh(ControlParameters cp)
		{
			// must be released in order to resize these buffers
			RTV.Dispose();
			_resources.RTV = null;
			BackBufferTexture.Dispose();
			_resources.BackBufferTexture = null;

			var result = SwapChain.ResizeBuffers(
				bufferCount: 2,
				cp.Width,
				cp.Height,
				Format.B8G8R8A8_UNorm,
				AllowsTearing ? SwapChainFlags.AllowTearing : SwapChainFlags.None);

			if (result == DXGIResultCode.DeviceReset
				|| result == DXGIResultCode.DeviceRemoved
				|| result == D3DDDIERR_DEVICEREMOVED)
			{
				_resetDeviceCallback(cp);
			}
			else
			{
				result.CheckError();
				_resources.BackBufferTexture = SwapChain.GetBuffer<ID3D11Texture2D>(0);
				var rtvd = new RenderTargetViewDescription(RenderTargetViewDimension.Texture2D, Format.B8G8R8A8_UNorm);
				_resources.RTV = Device.CreateRenderTargetView(BackBufferTexture, rtvd);
			}
		}
	}
}
