using System.Reflection;
using System.Runtime.InteropServices;

using BizHawk.Bizware.Graphics;
using BizHawk.Common;

using Silk.NET.OpenGL;

namespace BizHawk.Client.Common.Filters
{
	public unsafe class LibrashaderFilterGL : LibrashaderFilterBase
	{
		private uint _framebuffer;
		private uint _framebufferTexture;

		private static IntPtr _getProcAddressPtr = IntPtr.Zero;
		private static GetProcAddressDelegate _getProcAddressDelegate;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr GetProcAddressDelegate(byte* name);

		private GL _gl;

		public LibrashaderFilterGL(string shaderPresetPath) : base(shaderPresetPath)
		{
		}

		private bool InitChain()
		{
			if (!ShouldReinitialize) return true;

			if (FilterProgram.GL is not IGL_OpenGL igl) return false;

			_gl = GetSilkGL(igl);
			if (_gl == null) return false;

			CleanupFramebuffer();

			UpdateOutputSize();

			if (!ValidateCommonPrerequisites()) return false;

			if (!CreatePresetIfNeeded()) return false;

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
				_gl.DeleteFramebuffer(_framebuffer);
				_framebuffer = 0;
			}

			if (_framebufferTexture != 0)
			{
				_gl.DeleteTexture(_framebufferTexture);
				_framebufferTexture = 0;
			}
		}

		private void CreateFramebuffer()
		{
			_framebufferTexture = _gl.GenTexture();
			_gl.BindTexture(TextureTarget.Texture2D, _framebufferTexture);
			_gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, (uint)_filteredWidth, (uint)_filteredHeight, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
			_gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			_gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			_gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			_gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

			_framebuffer = _gl.GenFramebuffer();
			_gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
			_gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _framebufferTexture, 0);

			var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
			if (status != GLEnum.FramebufferComplete)
			{
				_initialized = false;
				return;
			}

			_gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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

		private static GL GetSilkGL(IGL_OpenGL igl)
		{
			var glField = typeof(IGL_OpenGL).GetField("GL", BindingFlags.NonPublic | BindingFlags.Instance);
			return glField?.GetValue(igl) as GL;
		}

		public override void Run()
		{
			if (!InitChain()) return;

			if (_chain == IntPtr.Zero) return;

			var inputTexId = (InputTexture as OpenGLTexture2D)?.TexID ?? default;
			if (inputTexId == 0) return;

			var drawFbo = (FilterProgram.CurrRenderTarget as OpenGLRenderTarget)?.FBO ?? default;

			_gl.ActiveTexture(TextureUnit.Texture0);
			_gl.BindTexture(TextureTarget.Texture2D, inputTexId);
			_gl.GenerateMipmap(TextureTarget.Texture2D);
			_gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			var input = new Librashader.libra_image_gl_t
			{
				handle = inputTexId,
				format = 0x8058,
				width = (uint)InputTexture.Width,
				height = (uint)InputTexture.Height,
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

			_gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _framebuffer);
			_gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, drawFbo);
			_gl.BlitFramebuffer(0, _filteredHeight, _filteredWidth, 0,
				0, 0, _filteredWidth, _filteredHeight,
				ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
			_gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
		}

		public override void Dispose()
		{
			if (!_initialized) return;

			if (_chain != IntPtr.Zero)
			{
				_ = Librashader.libra_gl_filter_chain_free(ref _chain);
				_chain = IntPtr.Zero;
			}

			FreePreset();

			CleanupFramebuffer();

			_initialized = false;
		}
	}
}
