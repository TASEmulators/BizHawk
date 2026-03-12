using System.Drawing;
using System.Numerics;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// This is a wrapper over OpenGL and Direct3D11 to give a uniform interface
	/// NOTE: THIS SHOULD NOT BE ASSUMED TO BE THREAD SAFE! Make a new IGL if you want to use it in a new thread. I hope that will work...
	/// </summary>
	public interface IGL : IDisposable
	{
		/// <summary>
		/// Returns the display method represented by this IGL
		/// </summary>
		EDispMethod DispMethodEnum { get; }

		/// <summary>
		/// Returns the maximum size any dimension of a texture may have
		/// This should be set on init, and therefore shouldn't need a graphics context active...
		/// </summary>
		int MaxTextureDimension { get; }

		/// <summary>
		/// Creates a texture with the specified dimensions
		/// The texture will use a clamping address mode and nearest neighbor filtering by default
		/// </summary>
		ITexture2D CreateTexture(int width, int height);

		/// <summary>
		/// In case you already have the texture ID (from an OpenGL emulator gpu core) you can get an ITexture2D with it this way.
		/// Otherwise, if this isn't an OpenGL frontend implementation, the core is expected to readback the texture for GetVideoBuffer()
		/// </summary>
		ITexture2D WrapGLTexture2D(int glTexId, int width, int height);

		/// <summary>
		/// Creates a render target. Only includes a color buffer, and will always be in byte order BGRA (i.e. little endian ARGB)
		/// This may unbind a previously bound render target
		/// </summary>
		IRenderTarget CreateRenderTarget(int width, int height);

		/// <summary>
		/// Binds the IGL's default render target (i.e. to the IGL's control)
		/// This implicitly unbinds any previously bound IRenderTarget
		/// </summary>
		void BindDefaultRenderTarget();

		/// <summary>
		/// Clears the currently bound render target with the specified color
		/// </summary>
		void ClearColor(Color color);

		/// <summary>
		/// Creates a complete pipeline
		/// </summary>
		IPipeline CreatePipeline(PipelineCompileArgs compileArgs);

		/// <summary>
		/// Binds this pipeline as the current used for rendering
		/// </summary>
		void BindPipeline(IPipeline pipeline);

		/// <summary>
		/// Enables normal (non-premultiplied) alpha blending.
		/// </summary>
		void EnableBlending();

		/// <summary>
		/// Disables blending (alpha values are copied from the source fragment)
		/// </summary>
		void DisableBlending();

		/// <summary>
		/// Sets the viewport (and scissor) according to the provided specifications
		/// </summary>
		void SetViewport(int x, int y, int width, int height);

		/// <summary>
		/// Non-indexed drawing for the currently set pipeline/render target
		/// Vertexes must form triangle strips
		/// </summary>
		void Draw(int vertexCount);

		/// <summary>
		/// Indexed drawing for the currently set pipeline/render target
		/// Indexes must be 16 bits each
		/// Vertexes must form triangle lists
		/// </summary>
		void DrawIndexed(int indexCount, int indexStart, int vertexStart);

		/// <summary>
		/// Generates a proper 2D othographic projection for the given destination size, suitable for use in a GUI
		/// </summary>
		Matrix4x4 CreateGuiProjectionMatrix(int width, int height);

		/// <summary>
		/// Generates a proper view transform for a standard 2D othographic projection, including half-pixel jitter if necessary
		/// and re-establishing of a normal 2D graphics top-left origin. Suitable for use in a GUI
		/// </summary>
		Matrix4x4 CreateGuiViewMatrix(int width, int height, bool autoflip = true);
	}
}
