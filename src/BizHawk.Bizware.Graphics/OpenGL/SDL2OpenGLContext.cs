using System;

using Silk.NET.OpenGL.Legacy;

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
			// don't allow windows events to be pumped
			// it's not needed and can be dangerous in some rare cases
			SDL_SetHint(SDL_HINT_WINDOWS_ENABLE_MESSAGELOOP, "0");
		}

		private IntPtr _sdlWindow;
		private IntPtr _glContext;

		private void CreateContext(int majorVersion, int minorVersion, bool coreProfile, bool forwardCompatible)
		{
			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, majorVersion) != 0)
			{
				throw new($"Could not set GL Major Version! SDL Error: {SDL_GetError()}");
			}
			
			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, minorVersion) != 0)
			{
				throw new($"Could not set GL Minor Version! SDL Error: {SDL_GetError()}");
			}

			// TODO: Debug flag / debug callback with DEBUG build
			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_FLAGS, forwardCompatible 
					? (int)SDL_GLcontext.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG : 0) != 0)
			{
				throw new($"Could not set GL Context Flags! SDL Error: {SDL_GetError()}");
			}

			var profile = coreProfile
				? SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE
				: SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_COMPATIBILITY;

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

		public SDL2OpenGLContext(IntPtr nativeWindowhandle, int majorVersion, int minorVersion, bool coreProfile, bool forwardCompatible)
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

			CreateContext(majorVersion, minorVersion, coreProfile, forwardCompatible);
		}

		public SDL2OpenGLContext(int majorVersion, int minorVersion, bool coreProfile, bool forwardCompatible)
		{
			_sdlWindow = SDL_CreateWindow(null, SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED, 1, 1,
				SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL_WindowFlags.SDL_WINDOW_HIDDEN);
			if (_sdlWindow == IntPtr.Zero)
			{
				throw new($"Could not create SDL Window! SDL Error: {SDL_GetError()}");
			}

			// offscreen contexts are shared (as we want to send texture from it over to our control's context)
			// make sure to set the current graphics control context before creating this context
			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 1) != 0)
			{
				throw new($"Could not set share context attribute! SDL Error: {SDL_GetError()}");
			}

			CreateContext(majorVersion, minorVersion, coreProfile, forwardCompatible);
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