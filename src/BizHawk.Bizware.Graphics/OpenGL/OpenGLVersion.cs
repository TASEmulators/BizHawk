using System;
using System.Collections.Generic;
using BizHawk.Common.CollectionExtensions;

using Silk.NET.OpenGL.Legacy;

using static SDL2.SDL;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Wraps checking OpenGL versions
	/// </summary>
	public static class OpenGLVersion
	{
		// TODO: make this a ref struct once we're c# 10 (parameterless struct ctor)
		private class SavedOpenGLContext : IDisposable
		{
			private readonly IntPtr _sdlWindow, _glContext;

			public SavedOpenGLContext()
			{
				_sdlWindow = SDL_GL_GetCurrentWindow();
				_glContext = SDL_GL_GetCurrentContext();
			}

			public void Dispose()
			{
				_ = SDL_GL_MakeCurrent(_sdlWindow, _glContext);
			}
		}

		private static readonly IDictionary<int, bool> _glSupport = new Dictionary<int, bool>();

		private static int PackGLVersion(int major, int minor)
			=> major * 10 + minor;

		private static bool CheckVersion(int requestedMajor, int requestedMinor)
		{
			using (new SavedOpenGLContext())
			{
				try
				{
					using (new SDL2OpenGLContext(requestedMajor, requestedMinor, true, false))
					{
						using var gl = GL.GetApi(SDL2OpenGLContext.GetGLProcAddress);
						var versionString = gl.GetStringS(StringName.Version);
						var versionParts = versionString!.Split('.');
						var major = int.Parse(versionParts[0]);
						var minor = int.Parse(versionParts[1][0].ToString());
						return PackGLVersion(major, minor) >= PackGLVersion(requestedMajor, requestedMinor);
					}
				}
				catch
				{
					return false;
				}
			}
		}

		public static bool SupportsVersion(int major, int minor)
			=> _glSupport.GetValueOrPut(PackGLVersion(major, minor),
				static version => CheckVersion(version / 10, version % 10));
	}
}