using System;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Defines an interface for cores to obtain OpenGL contexts and functions
	/// </summary>
	public interface IOpenGLProvider
	{
		/// <summary>
		/// OpenGL version, using major.minor.build format with decimal points ommitted (e.g. 1.2.3 turns into 123)
		/// </summary>
		public int GLVersion { get; }

		/// <summary>
		/// Requests an OpenGL context with specified major / minor / forwardCompatible
		/// The requested OpenGL context will be shared with the current context
		/// Note: creating a context implicitly makes that created context current
		/// </summary>
		public object RequestGLContext(int major, int minor, bool forwardCompatible);

		/// <summary>
		/// Frees this OpenGL context
		/// </summary>
		public void ReleaseGLContext(object context);

		/// <summary>
		/// Sets this OpenGL context to current
		/// </summary>
		public void ActivateGLContext(object context);

		/// <summary>
		/// Deactivates the current OpenGL context
		/// No context will be current after this call
		/// </summary>
		public void DeactivateGLContext();

		/// <summary>
		/// Gets an OpenGL function pointer
		/// The user must make a context active before using this
		/// </summary>
		public IntPtr GetGLProcAddress(string proc);
	}
}
