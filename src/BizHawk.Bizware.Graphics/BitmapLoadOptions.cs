using System.Drawing;

namespace BizHawk.Bizware.Graphics
{
	public class BitmapLoadOptions
	{
		/// <summary>
		/// A callback to be issued when a 24bpp image is detected, which will allow you to return a colorkey
		/// </summary>
		public Func<Bitmap, int> ColorKey24bpp;

		/// <summary>
		/// Specifies whether palette entry 0 (if there is a palette) shall represent transparent (Alpha=0)
		/// </summary>
		public bool TransparentPalette0 = true;

		/// <summary>
		/// Specifies whether (r,g,b,0) pixels shall be turned into (0,0,0,0).
		/// This is useful for cleaning up junk which you might not know you had littering purely transparent areas, which can mess up a lot of stuff during rendering.
		/// </summary>
		public bool CleanupAlpha0 = true;

		/// <summary>
		/// Applies the Premultiply post-process (not supported yet; and anyway it could be done as it loads for a little speedup, in many cases)
		/// </summary>
		public bool Premultiply = false;

		/// <summary>
		/// Applies Pad() post-process
		/// </summary>
		public bool Pad = false;

		/// <summary>
		/// Allows the BitmapBuffer to wrap a System.Drawing.Bitmap, if one is provided for loading.
		/// This System.Drawing.Bitmap must be 32bpp and these other options may be valid (since this approach is designed for quickly getting things into textures)
		/// Ownership of the bitmap remains with the user.
		/// </summary>
		public bool AllowWrap = true;
	}
}