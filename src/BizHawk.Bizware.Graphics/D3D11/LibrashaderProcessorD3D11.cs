namespace BizHawk.Bizware.Graphics
{
	public unsafe class LibrashaderProcessorD3D11 : IDisposable
	{
		private readonly IGL_D3D11 _gl;
		private IntPtr _preset = IntPtr.Zero;
		private IntPtr _chain = IntPtr.Zero;
		private uint _frameCount = 0;
		private bool _initialized = false;
		private int _filteredWidth;
		private int _filteredHeight;
		private readonly string _shaderPresetPath;

		public bool IsAvailable => _initialized;

		public LibrashaderProcessorD3D11(IGL_D3D11 gl, string shaderPresetPath)
		{
			_gl = gl;
			_shaderPresetPath = shaderPresetPath;
		}

		public bool Initialize(int width, int height)
		{
			if (_initialized && _filteredWidth == width && _filteredHeight == height)
				return true;

			_filteredWidth = width;
			_filteredHeight = height;

			if (_filteredWidth <= 0 || _filteredHeight <= 0) return false;
			if (!Librashader.Load()) return false;
			if (!System.IO.File.Exists(_shaderPresetPath)) return false;

			if (_preset == IntPtr.Zero)
			{
				IntPtr error = Librashader.PresetCreate(_shaderPresetPath, out _preset);
				if (error != IntPtr.Zero)
				{
					_ = Librashader.libra_error_print(error);
					return false;
				}
			}

			if (_chain == IntPtr.Zero)
			{
				var device = _gl.LibrashaderDevice;
				if (device == null) return false;

				var options = Librashader.CreateDefaultD3D11Options();
				IntPtr error = Librashader.libra_d3d11_filter_chain_create(ref _preset, device.NativePointer, ref options, out _chain);
				if (error != IntPtr.Zero)
				{
					_ = Librashader.libra_error_print(error);
					return false;
				}
			}

			return _initialized = true;
		}

		public void Render(IntPtr inputSRVPointer, IntPtr outputRTVPointer)
		{
			if (!_initialized || _chain == IntPtr.Zero) return;
			if (inputSRVPointer == IntPtr.Zero || outputRTVPointer == IntPtr.Zero) return;

			var context = _gl.LibrashaderContext;
			if (context == null) return;

			var viewport = new Librashader.libra_viewport_t
			{
				x = 0,
				y = 0,
				width = (uint)_filteredWidth,
				height = (uint)_filteredHeight,
			};

			_ = Librashader.libra_d3d11_filter_chain_frame(
				ref _chain,
				context.NativePointer,
				new UIntPtr(_frameCount++),
				inputSRVPointer,
				outputRTVPointer,
				ref viewport,
				IntPtr.Zero,
				IntPtr.Zero);
		}

		public IntPtr GetBackBufferRTVPointer()
		{
			var rtv = _gl.LibrashaderBackBufferRTV;
			return rtv?.NativePointer ?? IntPtr.Zero;
		}

		public void Dispose()
		{
			if (_chain != IntPtr.Zero)
			{
				_ = Librashader.libra_d3d11_filter_chain_free(ref _chain);
				_chain = IntPtr.Zero;
			}

			if (_preset != IntPtr.Zero)
			{
				_ = Librashader.libra_preset_free(ref _preset);
				_preset = IntPtr.Zero;
			}

			_initialized = false;
		}
	}
}
