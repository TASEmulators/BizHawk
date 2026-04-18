using System.Reflection;

using BizHawk.Bizware.Graphics;

using Vortice.Direct3D11;

namespace BizHawk.Client.Common.Filters
{
	public unsafe class LibrashaderFilterD3D11 : LibrashaderFilterBase
	{
		private ID3D11Device _device;
		private ID3D11DeviceContext _context;

		private static readonly FieldInfo ResourcesField = typeof(IGL_D3D11).GetField("_resources", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly FieldInfo DeviceField = typeof(D3D11Resources).GetField("Device", BindingFlags.Public | BindingFlags.Instance);
		private static readonly FieldInfo ContextField = typeof(D3D11Resources).GetField("Context", BindingFlags.Public | BindingFlags.Instance);

		private static readonly FieldInfo ControlSwapChainField = typeof(IGL_D3D11).GetField("_controlSwapChain", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly Type SwapChainResourcesType = typeof(D3D11SwapChain).GetNestedType("SwapChainResources", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly FieldInfo SwapChainRTVField = SwapChainResourcesType?.GetField("RTV", BindingFlags.Public | BindingFlags.Instance);

		public LibrashaderFilterD3D11(string shaderPresetPath) : base(shaderPresetPath)
		{
		}

		private bool InitChain()
		{
			if (!ShouldReinitialize) return true;

			if (FilterProgram.GL is not IGL_D3D11 d3d11) return false;

			if (ResourcesField?.GetValue(d3d11) is not D3D11Resources resources) return false;

			_device = DeviceField?.GetValue(resources) as ID3D11Device;
			_context = ContextField?.GetValue(resources) as ID3D11DeviceContext;
			if (_device == null || _context == null) return false;

			UpdateOutputSize();

			if (!ValidateCommonPrerequisites()) return false;

			if (!CreatePresetIfNeeded()) return false;

			if (_chain == IntPtr.Zero)
			{
				var options = Librashader.CreateDefaultD3D11Options();
				IntPtr error = Librashader.libra_d3d11_filter_chain_create(ref _preset, _device.NativePointer, ref options, out _chain);
				if (error != IntPtr.Zero)
				{
					_ = Librashader.libra_error_print(error);
					return false;
				}
			}

			return _initialized = true;
		}

		private ID3D11RenderTargetView GetBackBufferRTV()
		{
			if (FilterProgram.GL is not IGL_D3D11 d3d11) return null;

			var swapChainResources = ControlSwapChainField?.GetValue(d3d11);
			return SwapChainRTVField?.GetValue(swapChainResources) as ID3D11RenderTargetView;
		}

		public override void Run()
		{
			if (!InitChain()) return;

			if (_chain == IntPtr.Zero) return;

			if (InputTexture is not D3D11Texture2D inputD3D11Texture) return;

			var inputSRV = inputD3D11Texture.SRV;
			if (inputSRV == null) return;

			ID3D11RenderTargetView outputRTV = (FilterProgram.CurrRenderTarget as D3D11RenderTarget)?.RTV
				?? GetBackBufferRTV();

			if (outputRTV == null) return;

			var viewport = new Librashader.libra_viewport_t
			{
				x = 0,
				y = 0,
				width = (uint)_filteredWidth,
				height = (uint)_filteredHeight,
			};

			_ = Librashader.libra_d3d11_filter_chain_frame(
				ref _chain,
				_context.NativePointer,
				new UIntPtr(_frameCount++),
				inputSRV.NativePointer,
				outputRTV.NativePointer,
				ref viewport,
				IntPtr.Zero,
				IntPtr.Zero);
		}

		public override void Dispose()
		{
			if (!_initialized) return;

			if (_chain != IntPtr.Zero)
			{
				_ = Librashader.libra_d3d11_filter_chain_free(ref _chain);
				_chain = IntPtr.Zero;
			}

			FreePreset();

			_initialized = false;
		}
	}
}
