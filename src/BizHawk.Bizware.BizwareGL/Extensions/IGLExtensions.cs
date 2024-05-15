using System.IO;
using System.Drawing;
using System.Numerics;

namespace BizHawk.Bizware.BizwareGL
{
	public static class IGLExtensions
	{
		/// <summary>
		/// Loads a texture from disk
		/// </summary>
		public static Texture2d LoadTexture(this IGL igl, string path)
		{
			using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			return igl.LoadTexture(fs);
		}

		/// <summary>
		/// Loads a texture from the stream
		/// </summary>
		public static Texture2d LoadTexture(this IGL igl, Stream stream)
		{
			using var bmp = new BitmapBuffer(stream, new());
			return igl.LoadTexture(bmp);
		}

		/// <summary>
		/// Loads a texture from the System.Drawing.Bitmap
		/// </summary>
		public static Texture2d LoadTexture(this IGL igl, Bitmap bitmap)
		{
			using var bmp = new BitmapBuffer(bitmap, new());
			return igl.LoadTexture(bmp);
		}

		/// <summary>
		/// Loads a texture from the BitmapBuffer
		/// </summary>
		public static Texture2d LoadTexture(this IGL igl, BitmapBuffer buffer)
		{
			var ret = igl.CreateTexture(buffer.Width, buffer.Height);
			igl.LoadTextureData(ret, buffer);
			return ret;
		}

		/// <summary>
		/// sets the viewport (and scissor) according to the provided specifications
		/// </summary>
		public static void SetViewport(this IGL igl, int width, int height)
			=> igl.SetViewport(0, 0, width, height);

		/// <summary>
		/// sets the viewport (and scissor) according to the provided specifications
		/// </summary>
		public static void SetViewport(this IGL igl, Size size)
			=> igl.SetViewport(0, 0, size.Width, size.Height);

		/// <summary>
		/// generates a proper 2d othographic projection for the given destination size, suitable for use in a GUI
		/// </summary>
		public static Matrix4x4 CreateGuiProjectionMatrix(this IGL igl, Size dims)
			=> igl.CreateGuiProjectionMatrix(dims.Width, dims.Height);

		/// <summary>
		/// generates a proper view transform for a standard 2d ortho projection, including half-pixel jitter if necessary and
		/// re-establishing of a normal 2d graphics top-left origin. suitable for use in a GUI
		/// </summary>
		public static Matrix4x4 CreateGuiViewMatrix(this IGL igl, Size dims, bool autoflip = true)
			=> igl.CreateGuiViewMatrix(dims.Width, dims.Height, autoflip);
	}
}
