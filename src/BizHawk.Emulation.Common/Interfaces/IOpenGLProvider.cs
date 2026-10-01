using SDL2;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Defines an interface for cores to obtain OpenGL contexts and functions
	/// </summary>
	public interface IOpenGLProvider
	{
		/// <summary>
		/// Checks if specified OpenGL version is supported
		/// The current context will be preserved
		/// </summary>
		bool SupportsGLVersion(int major, int minor);

		/// <summary>
		/// Requests an OpenGL context with specified major / minor version
		/// The core profile can be requested (otherwise, the compatibility profile will be used)
		/// The requested OpenGL context will be shared with the current context
		/// Note: creating a context implicitly makes that created context current
		/// </summary>
		object RequestGLContext(int major, int minor, bool coreProfile, int width=1, int height=1);

		/// <summary>
		/// Frees this OpenGL or Vulkan context
		/// </summary>
		void ReleaseContext(object context);

		/// <summary>
		/// Sets this OpenGL context to current
		/// </summary>
		void ActivateGLContext(object context);

		/// <summary>
		/// Deactivates the current OpenGL context
		/// No context will be current after this call
		/// </summary>
		void DeactivateGLContext();

		/// <summary>
		/// Gets an OpenGL function pointer
		/// The user must make a context active before using this
		/// </summary>
		IntPtr GetGLProcAddress(string? proc);

		/// <summary>
		/// Gets the current value for <paramref name="attribute"/> in the current OpenGL context
		/// </summary>
		/// <param name="attribute">The attribute to check</param>
		int GLGetAttribute(SDL.SDL_GLattr attribute);

		void SwapBuffers(object context);

		object RequestVulkanContext(int width, int height);

		ulong CreateVulkanSurface(object context, IntPtr instance);

		IntPtr[] GetVulkanInstanceExtensions();
	}
}
