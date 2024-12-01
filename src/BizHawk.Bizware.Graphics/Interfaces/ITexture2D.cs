namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// A full-scale 2D texture, with mip levels and everything.
	/// In OpenGL tradition, this encapsulates the sampler state, as well, which is equal parts annoying and convenient
	/// </summary>
	public interface ITexture2D : IDisposable
	{
		int Width { get; }
		int Height { get; }

		/// <summary>
		/// OpenGL wrapped textures from IGLTextureProvider will be upside down
		/// </summary>
		bool IsUpsideDown { get; }

		/// <summary>
		/// Resolves the texture into a new BitmapBuffer
		/// </summary>
		BitmapBuffer Resolve();

		/// <summary>
		/// Loads the texture with new data. This isn't supposed to be especially versatile, it just blasts a bitmap buffer into the texture
		/// </summary>
		void LoadFrom(BitmapBuffer buffer);

		/// <summary>
		/// Sets the texture's filtering mode to linear
		/// </summary>
		void SetFilterLinear();

		/// <summary>
		/// Sets the texture's filtering mode to nearest neighbor
		/// </summary>
		void SetFilterNearest();
	}
}
