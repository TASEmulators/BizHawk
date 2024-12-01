using System.Drawing;
using System.Numerics;

namespace BizHawk.Bizware.Graphics
{
	public interface IGuiRenderer : IDisposable
	{
		/// <summary>
		/// Begin rendering, initializing viewport and projections to the given dimensions
		/// </summary>
		void Begin(int width, int height);

		/// <summary>
		/// Draws a subrectangle from the provided texture. For advanced users only
		/// </summary>
		void DrawSubrect(ITexture2D tex, float x, float y, float w, float h, float u0, float v0, float u1, float v1);

		/// <summary>
		/// Ends rendering
		/// </summary>
		void End();

		/// <summary>
		/// Use this, if you must do something sneaky to OpenGL without this GuiRenderer knowing.
		/// It might be faster than End and Beginning again, and certainly prettier
		/// </summary>
		void Flush();

		bool IsActive { get; }

		MatrixStack ModelView { get; set; }

		IGL Owner { get; }

		MatrixStack Projection { get; set; }

		void EnableBlending();

		void DisableBlending();

		/// <summary>
		/// Sets the specified corner color (for the gradient effect)
		/// </summary>
		/// <remarks>(x, y, z, w) is (r, g, b, a)</remarks>
		void SetCornerColor(int which, Vector4 color);

		/// <summary>
		/// Sets all four corner colors at once
		/// </summary>
		/// <remarks>(x, y, z, w) is (r, g, b, a)</remarks>
		void SetCornerColors(Vector4[] colors);

		/// <summary>
		/// Restores the pipeline to the default
		/// </summary>
		void SetDefaultPipeline();

		void SetModulateColor(Color color);

		void SetModulateColorWhite();

		/// <summary>
		/// Sets the pipeline for this GuiRenderer to use. We won't keep possession of it.
		/// This pipeline must work in certain ways, which can be discerned by inspecting the built-in one
		/// </summary>
		void SetPipeline(IPipeline pipeline);
	}
}
