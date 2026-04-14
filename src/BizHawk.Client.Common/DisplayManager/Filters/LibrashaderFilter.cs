using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.Bizware.Graphics;

using Silk.NET.OpenGL;

namespace BizHawk.Client.Common.Filters
{
	public unsafe class LibrashaderFilter : BaseFilter, IDisposable
	{
		private IntPtr _preset = IntPtr.Zero;
		private IntPtr _chain = IntPtr.Zero;
		private uint _frameCount = 0;
		private bool _initialized = false;
		private Size _outputSize;
		private Size _lastOutputSize;
		private string _shaderPresetPath;

		private uint _framebuffer;
		private uint _framebufferTexture;
		private int _filteredWidth;
		private int _filteredHeight;

		private static IntPtr _getProcAddressPtr = IntPtr.Zero;
		private static GetProcAddressDelegate _getProcAddressDelegate;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr GetProcAddressDelegate(byte* name);

		private GL _gl;

		public bool IsAvailable => _initialized;

		public LibrashaderFilter(string shaderPresetPath)
		{
			_shaderPresetPath = shaderPresetPath;
		}

		public override void Initialize()
		{
			DeclareInput(SurfaceDisposition.Texture);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			DeclareOutput(new SurfaceState(new(_outputSize), SurfaceDisposition.RenderTarget));
		}

		public override Size PresizeOutput(string channel, Size size)
		{
			_outputSize = size;
			return size;
		}

		public override Size PresizeInput(string channel, Size inSize)
		{
			return inSize;
		}

		private bool InitChain()
		{
			if (_initialized && _lastOutputSize == _outputSize) return true;

			if (FilterProgram.GL is not IGL_OpenGL igl)
			{
				return false;
			}

			_gl = GetSilkGL(igl);
			if (_gl == null)
			{
				return false;
			}

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

			_filteredWidth = _outputSize.Width;
			_filteredHeight = _outputSize.Height;
			_lastOutputSize = _outputSize;

			if (_filteredWidth <= 0 || _filteredHeight <= 0)
			{
				return false;
			}

			if (!Librashader.Load())
			{
				return false;
			}

			if (!File.Exists(_shaderPresetPath))
			{
				return false;
			}

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
				var options = Librashader.CreateDefaultOptions();

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
				return false;
			}

			_gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			return _initialized = true;
		}

		private static IntPtr GetProcAddressCallback(byte* name)
		{
			string procName = PtrToStringUTF8((IntPtr)name);
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

		private static string PtrToStringUTF8(IntPtr ptr)
		{
			if (ptr == IntPtr.Zero) return null;

			int len = 0;
			while (Marshal.ReadByte(ptr, len) != 0) len++;

			if (len == 0) return string.Empty;

			byte[] buffer = new byte[len];
			Marshal.Copy(ptr, buffer, 0, len);
			return Encoding.UTF8.GetString(buffer);
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

		public void Dispose()
		{
			if (!_initialized) return;

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

			if (_framebufferTexture != 0)
			{
				_gl.DeleteTexture(_framebufferTexture);
				_framebufferTexture = 0;
			}

			if (_framebuffer != 0)
			{
				_gl.DeleteFramebuffer(_framebuffer);
				_framebuffer = 0;
			}

			_initialized = false;
		}
	}
}
