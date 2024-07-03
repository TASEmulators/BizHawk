using static SDL2.SDL;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Helper ref struct for tempoarily saving the current OpenGL context and restoring it
	/// </summary>
	public readonly ref struct SavedOpenGLContext
	{
		private readonly IntPtr _sdlWindow, _glContext;

		public SavedOpenGLContext()
		{
			_sdlWindow = SDL_GL_GetCurrentWindow();
			_glContext = SDL_GL_GetCurrentContext();
		}

		public void Dispose()
			=> _ = SDL_GL_MakeCurrent(_sdlWindow, _glContext);
	}
}
