using BizHawk.Bizware.Graphics;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Provides a way for a core to use OpenGL
	/// </summary>
	public class OpenGLProvider : IOpenGLProvider
	{
		public bool SupportsGLVersion(int major, int minor)
			=> OpenGLVersion.SupportsVersion(major, minor);

		public object RequestGLContext(int major, int minor, bool coreProfile)
			=> new SDL2OpenGLContext(major, minor, coreProfile);

		public void ReleaseGLContext(object context)
			=> ((SDL2OpenGLContext)context).Dispose();

		public void ActivateGLContext(object context)
			=> ((SDL2OpenGLContext)context).MakeContextCurrent();

		public void DeactivateGLContext()
			=> SDL2OpenGLContext.MakeNoneCurrent();

		public IntPtr GetGLProcAddress(string proc)
			=> SDL2OpenGLContext.GetGLProcAddress(proc);
	}
}
