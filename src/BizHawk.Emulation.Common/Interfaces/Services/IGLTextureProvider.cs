namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service is an extension of IVideoProvider, providing the ability to pass an OpenGL texture to the client
	/// If available and the client is using OpenGL for display, this texture will be used
	/// If unavailable or the client is not using OpenGL for display, the client will fall back to the base <see cref="IVideoProvider"/>
	/// </summary>
	public interface IGLTextureProvider : IVideoProvider, ISpecializedEmulatorService
	{
		/// <summary>
		/// Returns an OpenGL texture of the current video content
		/// </summary>
		int GetGLTexture();
	}
}
