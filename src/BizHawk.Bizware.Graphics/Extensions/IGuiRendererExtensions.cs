using System.Drawing;

namespace BizHawk.Bizware.Graphics
{
	public static class IGuiRendererExtensions
	{
		/// <summary>
		/// Begin rendering, initializing viewport and projections to the given dimensions
		/// </summary>
		public static void Begin(this IGuiRenderer guiRenderer, Size size)
			=> guiRenderer.Begin(size.Width, size.Height);

		/// <summary>
		/// Draws the specified texture2d resource.
		/// </summary>
		public static void Draw(this IGuiRenderer guiRenderer, ITexture2D tex)
			=> guiRenderer.Draw(tex, 0, 0, tex.Width, tex.Height);

		/// <summary>
		/// Draws the specified texture2d resource with the specified offset.
		/// </summary>
		public static void Draw(this IGuiRenderer guiRenderer, ITexture2D tex, float x, float y)
			=> guiRenderer.Draw(tex, x, y, tex.Width, tex.Height);

		/// <summary>
		/// Draws the specified Texture with the specified offset and the specified size. This could be tricky if youve applied other rotate or scale transforms first.
		/// </summary>
		public static void Draw(this IGuiRenderer guiRenderer, ITexture2D tex, float x, float y, float width, float height)
		{
			const float u0 = 0, u1 = 1;
			float v0, v1;

			if (tex.IsUpsideDown)
			{
				v0 = 1;
				v1 = 0;
			}
			else
			{
				v0 = 0;
				v1 = 1;
			}

			guiRenderer.DrawSubrect(tex, x, y, width, height, u0, v0, u1, v1);
		}
	}
}
