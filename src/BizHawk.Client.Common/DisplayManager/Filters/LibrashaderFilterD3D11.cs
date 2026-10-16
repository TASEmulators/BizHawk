using BizHawk.Bizware.Graphics;

namespace BizHawk.Client.Common.Filters
{
	public unsafe class LibrashaderFilterD3D11 : LibrashaderFilterBase
	{
		private LibrashaderProcessorD3D11 _processor;

		public LibrashaderFilterD3D11(string shaderPresetPath) : base(shaderPresetPath)
		{
		}

		private bool InitProcessor()
		{
			if (!ShouldReinitialize) return true;

			if (FilterProgram.GL is not IGL_D3D11 d3d11) return false;

			UpdateOutputSize();

			_processor ??= new LibrashaderProcessorD3D11(d3d11, _shaderPresetPath);
			return _initialized = _processor.Initialize(_filteredWidth, _filteredHeight);
		}

		public override void Run()
		{
			if (!InitProcessor()) return;

			if (InputTexture is not D3D11Texture2D inputD3D11Texture) return;

			var inputSRV = inputD3D11Texture.SRV;
			if (inputSRV == null) return;

			var outputRTV = (FilterProgram.CurrRenderTarget as D3D11RenderTarget)?.RTV;
			var outputRTVPointer = outputRTV?.NativePointer ?? _processor.GetBackBufferRTVPointer();

			if (outputRTVPointer == IntPtr.Zero) return;

			_processor.Render(inputSRV.NativePointer, outputRTVPointer);
		}

		public override void Dispose()
		{
			_processor?.Dispose();
			_processor = null;
			_initialized = false;
		}
	}
}
