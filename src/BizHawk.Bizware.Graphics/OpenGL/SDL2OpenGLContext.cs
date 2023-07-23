using System;
using System.Runtime.InteropServices;

using static SDL2.SDL;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Wraps an SDL2 OpenGL context
	/// </summary>
	public class SDL2OpenGLContext : IDisposable
	{
		static SDL2OpenGLContext()
		{
			// init SDL video
			if (SDL_Init(SDL_INIT_VIDEO) != 0)
			{
				throw new($"Could not init SDL video! SDL Error: {SDL_GetError()}");
			}

			// load the default OpenGL library
			if (SDL_GL_LoadLibrary(null) != 0)
			{
				throw new($"Could not load default OpenGL library! SDL Error: {SDL_GetError()}");
			}

			// set some sensible defaults
			SDL_GL_ResetAttributes();
			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_RED_SIZE, 8) != 0 ||
				SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_GREEN_SIZE, 8) != 0 ||
				SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_BLUE_SIZE, 8) != 0 ||
				SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_ALPHA_SIZE, 0) != 0 ||
				SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1) != 0)
			{
				throw new($"Could not set GL attributes! SDL Error: {SDL_GetError()}");
			}

			// we will be turning a foreign window into an SDL window
			// we need this so it knows that it is capable of using OpenGL functions
			SDL_SetHint(SDL_HINT_VIDEO_FOREIGN_WINDOW_OPENGL, "1");
		}

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		private delegate IntPtr glGetStringDelegate(int name);

		private static readonly Lazy<int> _version = new(() =>
		{
			var prevWindow = SDL_GL_GetCurrentWindow();
			var prevContext = SDL_GL_GetCurrentContext();

			try
			{
				using (new SDL2OpenGLContext(2, 0, false))
				{
					var getStringFp = GetGLProcAddress("glGetString");
					if (getStringFp == IntPtr.Zero) // uhhh?
					{
						return 0;
					}

					var getStringFunc = Marshal.GetDelegateForFunctionPointer<glGetStringDelegate>(getStringFp);
					const int GL_VERSION = 0x1F02;
					var version = getStringFunc(GL_VERSION);
					if (version == IntPtr.Zero)
					{
						return 0;
					}

					var versionString = Marshal.PtrToStringAnsi(version);
					var versionParts = versionString!.Split('.');
					var major = int.Parse(versionParts[0]);
					var minor = int.Parse(versionParts[1][0].ToString());
					return major * 100 + minor;
				}
			}
			finally
			{
				SDL_GL_MakeCurrent(prevWindow, prevContext);
			}
		});

		public static int Version => _version.Value;

		private IntPtr _sdlWindow;
		private IntPtr _glContext;

		private void CreateContext(int majorVersion, int minorVersion, bool forwardCompatible)
		{
			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, majorVersion) != 0)
			{
				throw new($"Could not set GL Major Version! SDL Error: {SDL_GetError()}");
			}
			
			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, minorVersion) != 0)
			{
				throw new($"Could not set GL Minor Version! SDL Error: {SDL_GetError()}");
			}

			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_FLAGS, forwardCompatible 
					? (int)SDL_GLcontext.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG : 0) != 0)
			{
				throw new($"Could not set GL Context Flags! SDL Error: {SDL_GetError()}");
			}

			// if we're requesting OpenGL 3.3+, get the core profile
			// profiles don't exist otherwise
			var profile = majorVersion * 10 + minorVersion >= 33
				? SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE
				: 0;

			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, profile) != 0)
			{
				throw new($"Could not set GL profile! SDL Error: {SDL_GetError()}");
			}

			_glContext = SDL_GL_CreateContext(_sdlWindow);
			if (_glContext == IntPtr.Zero)
			{
				throw new($"Could not create GL Context! SDL Error: {SDL_GetError()}");
			}
		}

		public SDL2OpenGLContext(IntPtr nativeWindowhandle, int majorVersion, int minorVersion, bool forwardCompatible)
		{
			_sdlWindow = SDL_CreateWindowFrom(nativeWindowhandle);
			if (_sdlWindow == IntPtr.Zero)
			{
				throw new($"Could not create SDL Window! SDL Error: {SDL_GetError()}");
			}

			// Controls are not shared, they are the sharees
			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 0) != 0)
			{
				throw new($"Could not set share context attribute! SDL Error: {SDL_GetError()}");
			}

			CreateContext(majorVersion, minorVersion, forwardCompatible);
		}

		public SDL2OpenGLContext(int majorVersion, int minorVersion, bool forwardCompatible)
		{
			_sdlWindow = SDL_CreateWindow(null, SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED, 1, 1,
				SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL_WindowFlags.SDL_WINDOW_HIDDEN);
			if (_sdlWindow == IntPtr.Zero)
			{
				throw new($"Could not create SDL Window! SDL Error: {SDL_GetError()}");
			}

			// offscreen contexts are shared (as we want to send texture from it over to our control's context)
			// make sure to set the current graphics' control context before creating this context
			// (if no context is set, i.e. first IGL, then this won't do anything)
			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 1) != 0)
			{
				throw new($"Could not set share context attribute! SDL Error: {SDL_GetError()}");
			}

			CreateContext(majorVersion, minorVersion, forwardCompatible);
		}

		public void Dispose()
		{
			if (_glContext != IntPtr.Zero)
			{
				SDL_GL_DeleteContext(_glContext);
				_glContext = IntPtr.Zero;
			}

			if (_sdlWindow != IntPtr.Zero)
			{
				SDL_DestroyWindow(_sdlWindow);
				_sdlWindow = IntPtr.Zero;
			}
		}

		public bool IsCurrent => SDL_GL_GetCurrentContext() == _glContext;

		public void MakeContextCurrent()
		{
			// no-op if already current
			_ = SDL_GL_MakeCurrent(_sdlWindow, _glContext);
		}

		public static void MakeNoneCurrent()
		{
			// no-op if nothing is current
			_ = SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero);
		}

		public static IntPtr GetGLProcAddress(string proc)
			=> SDL_GL_GetProcAddress(proc);

		public void SetVsync(bool state)
		{
			if (!IsCurrent)
			{
				throw new InvalidOperationException("Tried to set Vsync on non-active context");
			}

			_ = SDL_GL_SetSwapInterval(state ? 1 : 0);
		}

		public void SwapBuffers()
		{
			if (!IsCurrent)
			{
				throw new InvalidOperationException("Tried to swap buffers on non-active context");
			}

			SDL_GL_SwapWindow(_sdlWindow);
		}
	}
}