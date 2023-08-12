using System;

using SharpDX;
using SharpDX.Direct3D9;

namespace BizHawk.Bizware.Graphics
{
	public sealed class D3D9SwapChain : IDisposable
	{
		private const int D3DERR_DEVICELOST = unchecked((int)0x88760868);

		private readonly Device _device;
		private readonly Func<PresentParameters, SwapChain> _resetDeviceCallback;
		private readonly Func<PresentParameters, SwapChain> _resetSwapChainCallback;

		private SwapChain _swapChain;

		internal D3D9SwapChain(Device device, SwapChain swapChain,
			Func<PresentParameters, SwapChain> resetDeviceCallback, Func<PresentParameters, SwapChain> resetSwapChainCallback)
		{
			_device = device;
			_swapChain = swapChain;
			_resetDeviceCallback = resetDeviceCallback;
			_resetSwapChainCallback = resetSwapChainCallback;
		}

		public void Dispose()
		{
			_swapChain?.Dispose();
			_swapChain = null;
		}

		public void SetBackBuffer()
		{
			using var surface = _swapChain.GetBackBuffer(0);
			_device.SetRenderTarget(0, surface);
			_device.DepthStencilSurface = null;
		}

		public void PresentBuffer()
		{
			SetBackBuffer();

			try
			{
				_swapChain.Present(Present.None);
			}
			catch (SharpDXException ex)
			{
				if (ex.ResultCode.Code == D3DERR_DEVICELOST)
				{
					var pp = _swapChain.PresentParameters;
					pp.BackBufferWidth = pp.BackBufferHeight = 0;
					_swapChain = _resetDeviceCallback(pp);
				}
			}
		}

		public void Refresh(bool vsync)
		{
			var pp = _swapChain.PresentParameters;
			pp.BackBufferWidth = pp.BackBufferHeight = 0;
			pp.PresentationInterval = vsync ? PresentInterval.One : PresentInterval.Immediate;
			_swapChain = _resetSwapChainCallback(pp);
		}
	}
}
