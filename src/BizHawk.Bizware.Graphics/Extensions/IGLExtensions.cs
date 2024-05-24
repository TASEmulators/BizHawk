using System.IO;
using System.Drawing;
using System.Numerics;

namespace BizHawk.Bizware.Graphics
{
	public static class IGLExtensions
	{
		public static IGuiRenderer CreateGuiRenderer(this IGL gl)
			=> gl is IGL_GDIPlus gdipImpl
				? new GDIPlusGuiRenderer(gdipImpl)
				: new GuiRenderer(gl);

		public static I2DRenderer Create2DRenderer(this IGL gl, ImGuiResourceCache resourceCache)
			=> gl is IGL_GDIPlus gdipImpl
				? new SDLImGui2DRenderer(gdipImpl, resourceCache)
				: new ImGui2DRenderer(gl, resourceCache);

		/// <summary>
		/// Loads a texture from disk
		/// </summary>
		public static ITexture2D LoadTexture(this IGL igl, string path)
		{
			using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			return igl.LoadTexture(fs);
		}

		/// <summary>
		/// Loads a texture from the stream
		/// </summary>
		public static ITexture2D LoadTexture(this IGL igl, Stream stream)
		{
			using var bmp = new BitmapBuffer(stream, new());
			return igl.LoadTexture(bmp);
		}

		/// <summary>
		/// Loads a texture from the System.Drawing.Bitmap
		/// </summary>
		public static ITexture2D LoadTexture(this IGL igl, Bitmap bitmap)
		{
			using var bmp = new BitmapBuffer(bitmap, new());
			return igl.LoadTexture(bmp);
		}

		/// <summary>
		/// Loads a texture from the BitmapBuffer
		/// </summary>
		public static ITexture2D LoadTexture(this IGL igl, BitmapBuffer buffer)
		{
			var ret = igl.CreateTexture(buffer.Width, buffer.Height);
			ret.LoadFrom(buffer);
			return ret;
		}

		/// <summary>
		/// Sets the viewport (and scissor) according to the provided specifications
		/// </summary>
		public static void SetViewport(this IGL igl, int width, int height)
			=> igl.SetViewport(0, 0, width, height);

		/// <summary>
		/// Sets the viewport (and scissor) according to the provided specifications
		/// </summary>
		public static void SetViewport(this IGL igl, Size size)
			=> igl.SetViewport(0, 0, size.Width, size.Height);

		/// <summary>
		/// Generates a proper 2d othographic projection for the given destination size, suitable for use in a GUI
		/// </summary>
		public static Matrix4x4 CreateGuiProjectionMatrix(this IGL igl, Size dims)
			=> igl.CreateGuiProjectionMatrix(dims.Width, dims.Height);

		/// <summary>
		/// Generates a proper view transform for a standard 2d ortho projection, including half-pixel jitter if necessary and
		/// re-establishing of a normal 2d graphics top-left origin. suitable for use in a GUI
		/// </summary>
		public static Matrix4x4 CreateGuiViewMatrix(this IGL igl, Size dims, bool autoflip = true)
			=> igl.CreateGuiViewMatrix(dims.Width, dims.Height, autoflip);
	}
}
