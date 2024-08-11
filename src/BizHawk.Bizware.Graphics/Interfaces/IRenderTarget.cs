namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// A render target, essentially just a 2D texture which can be written as if it was a window
	/// As this is effectively a texture, it inherits ITexture2D
	/// However, note that semantically the CPU shouldn't be writing to a render target, so LoadFrom might not work!
	/// </summary>
	public interface IRenderTarget : ITexture2D
	{
		/// <summary>
		/// Binds this render target
		/// </summary>
		void Bind();
	}
}
