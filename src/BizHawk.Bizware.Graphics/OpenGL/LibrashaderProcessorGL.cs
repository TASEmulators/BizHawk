using System.Runtime.InteropServices;

using BizHawk.Common;

using Silk.NET.OpenGL;

namespace BizHawk.Bizware.Graphics
{
	public unsafe class LibrashaderProcessorGL : IDisposable
	{
		private readonly IGL_OpenGL _gl;
		private readonly GL _api;
		private IntPtr _preset = IntPtr.Zero;
		private IntPtr _chain = IntPtr.Zero;
		private uint _frameCount = 0;
		private bool _initialized = false;
		private int _filteredWidth;
		private int _filteredHeight;
		private readonly string _shaderPresetPath;

		private uint _framebuffer;
		private uint _framebufferTexture;

		private static IntPtr _getProcAddressPtr = IntPtr.Zero;
		private static GetProcAddressDelegate _getProcAddressDelegate;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr GetProcAddressDelegate(byte* name);

		public bool IsAvailable => _initialized;

		public LibrashaderProcessorGL(IGL_OpenGL gl, string shaderPresetPath)
		{
			_gl = gl;
			_api = gl.LibrashaderGL;
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

			CleanupFramebuffer();

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
				var options = Librashader.CreateDefaultGLOptions();

				if (_getProcAddressPtr == IntPtr.Zero)
				{
					_getProcAddressDelegate = GetProcAddressCallback;
					_getProcAddressPtr = Marshal.GetFunctionPointerForDelegate(_getProcAddressDelegate);
				}

				IntPtr error = Librashader.libra_gl_filter_chain_create(ref _preset, _getProcAddressPtr, ref options, out _chain);
				if (error != IntPtr.Zero)
				{
					_ = Librashader.libra_error_print(error);
					return false;
				}
			}

			CreateFramebuffer();

			return _initialized = true;
		}

		private void CleanupFramebuffer()
		{
			if (_framebuffer != 0)
			{
				_api.DeleteFramebuffer(_framebuffer);
				_framebuffer = 0;
			}

			if (_framebufferTexture != 0)
			{
				_api.DeleteTexture(_framebufferTexture);
				_framebufferTexture = 0;
			}
		}

		private void CreateFramebuffer()
		{
			_framebufferTexture = _api.GenTexture();
			_api.BindTexture(TextureTarget.Texture2D, _framebufferTexture);
			_api.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, (uint)_filteredWidth, (uint)_filteredHeight, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
			_api.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			_api.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			_api.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			_api.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

			_framebuffer = _api.GenFramebuffer();
			_api.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
			_api.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _framebufferTexture, 0);

			var status = _api.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
			if (status != GLEnum.FramebufferComplete)
			{
				_initialized = false;
				return;
			}

			_api.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}

		private static IntPtr GetProcAddressCallback(byte* name)
		{
			string procName = Mershul.PtrToStringUtf8((IntPtr)name);
			if (procName == null) return IntPtr.Zero;

			try
			{
				return SDL2OpenGLContext.GetGLProcAddress(procName);
			}
			catch
			{
				return IntPtr.Zero;
			}
		}

		public void Render(uint inputTexId, uint outputFbo, int inputWidth, int inputHeight)
		{
			if (!_initialized || _chain == IntPtr.Zero) return;
			if (inputTexId == 0) return;

			_api.ActiveTexture(TextureUnit.Texture0);
			_api.BindTexture(TextureTarget.Texture2D, inputTexId);
			_api.GenerateMipmap(TextureTarget.Texture2D);
			_api.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			var input = new Librashader.libra_image_gl_t
			{
				handle = inputTexId,
				format = 0x8058,
				width = (uint)inputWidth,
				height = (uint)inputHeight,
			};

			var output = new Librashader.libra_image_gl_t
			{
				handle = _framebufferTexture,
				format = 0x1907,
				width = (uint)_filteredWidth,
				height = (uint)_filteredHeight,
			};

			_ = Librashader.libra_gl_filter_chain_frame(ref _chain, new UIntPtr(_frameCount++), input, output,
				IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

			_api.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _framebuffer);
			_api.BindFramebuffer(FramebufferTarget.DrawFramebuffer, outputFbo);
			_api.BlitFramebuffer(0, _filteredHeight, _filteredWidth, 0,
				0, 0, _filteredWidth, _filteredHeight,
				ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
			_api.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
		}

		public void Dispose()
		{
			if (_chain != IntPtr.Zero)
			{
				_ = Librashader.libra_gl_filter_chain_free(ref _chain);
				_chain = IntPtr.Zero;
			}

			if (_preset != IntPtr.Zero)
			{
				_ = Librashader.libra_preset_free(ref _preset);
				_preset = IntPtr.Zero;
			}

			CleanupFramebuffer();

			_initialized = false;
		}
	}
}
