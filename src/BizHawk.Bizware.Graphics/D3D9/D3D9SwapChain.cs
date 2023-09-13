using System;

using SharpDX;
using SharpDX.Direct3D9;

namespace BizHawk.Bizware.Graphics
{
	public sealed class D3D9SwapChain : IDisposable
	{
		public readonly struct ControlParameters
		{
			public readonly IntPtr Handle;
			public readonly int Width;
			public readonly int Height;
			public readonly bool Vsync;

			public ControlParameters(IntPtr handle, int width, int height, bool vsync)
			{
				Handle = handle;
				Width = Math.Max(width, 1);
				Height = Math.Max(height, 1);
				Vsync = vsync;
			}
		}

		private const int D3DERR_DEVICELOST = unchecked((int)0x88760868);

		private readonly Device _device;
		private readonly Func<ControlParameters, SwapChain> _resetDeviceCallback;
		private readonly Func<ControlParameters, SwapChain> _resetSwapChainCallback;

		private SwapChain _swapChain;

		internal D3D9SwapChain(Device device, SwapChain swapChain,
			Func<ControlParameters, SwapChain> resetDeviceCallback, Func<ControlParameters, SwapChain> resetSwapChainCallback)
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

		public void PresentBuffer(ControlParameters cp)
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
					_swapChain = _resetDeviceCallback(cp);
				}
			}
		}

		public void Refresh(ControlParameters cp)
			=> _swapChain = _resetSwapChainCallback(cp);
	}
}
