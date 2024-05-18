using System;
using System.Drawing;
using System.Numerics;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// This is a wrapper over OpenGL and direct3d to give a uniform interface
	/// TODO - This really needs to be split up into an internal and a user interface. so many of the functions are made to support the smart wrappers
	/// Maybe make a method that returns an interface used for advanced methods (and IGL_OpenGL could implement that as well and just "return this:")
	///
	/// NOTE: THIS SHOULD NOT BE ASSUMED TO BE THREAD SAFE! Make a new IGL if you want to use it in a new thread. I hope that will work...
	/// </summary>
	public interface IGL : IDisposable
	{
		/// <summary>
		/// Returns the display method represented by this IGL
		/// </summary>
		EDispMethod DispMethodEnum { get; }

		/// <summary>
		/// Clears the currently bound render target with the specified color
		/// </summary>
		void ClearColor(Color color);

		/// <summary>
		/// Compile a fragment shader. This is the simplified method. A more complex method may be added later which will accept multiple sources and preprocessor definitions independently
		/// </summary>
		Shader CreateFragmentShader(string source, string entry, bool required);

		/// <summary>
		/// Compile a vertex shader. This is the simplified method. A more complex method may be added later which will accept multiple sources and preprocessor definitions independently
		/// </summary>
		Shader CreateVertexShader(string source, string entry, bool required);

		/// <summary>
		/// Creates a complete pipeline from the provided vertex and fragment shader handles
		/// </summary>
		Pipeline CreatePipeline(VertexLayout vertexLayout, Shader vertexShader, Shader fragmentShader, bool required, string memo);

		/// <summary>
		/// Binds this pipeline as the current used for rendering
		/// </summary>
		void BindPipeline(Pipeline pipeline);

		/// <summary>
		/// Sets a uniform sampler to use use the provided texture
		/// </summary>
		void SetPipelineUniformSampler(PipelineUniform uniform, ITexture2D tex);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4x4 mat, bool transpose);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4x4 mat, bool transpose);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetPipelineUniform(PipelineUniform uniform, Vector4 value);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetPipelineUniform(PipelineUniform uniform, Vector2 value);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetPipelineUniform(PipelineUniform uniform, float value);

		/// <summary>
		/// Sets uniform values
		/// </summary>
		void SetPipelineUniform(PipelineUniform uniform, Vector4[] values);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetPipelineUniform(PipelineUniform uniform, bool value);

		/// <summary>
		/// Draws based on the currently set pipeline
		/// Data contains vertexes based on the pipeline's VertexLayout
		/// Count is the vertex count
		/// Vertexes must form triangle strips
		/// </summary>
		void Draw(IntPtr data, int count);

		/// <summary>
		/// Creates a vertex layout resource
		/// </summary>
		VertexLayout CreateVertexLayout();

		/// <summary>
		/// Enables normal (non-premultiplied) alpha blending.
		/// </summary>
		void EnableBlending();

		/// <summary>
		/// Disables blending (alpha values are copied from the source fragment)
		/// </summary>
		void DisableBlending();

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
		/// Sets the viewport (and scissor) according to the provided specifications
		/// </summary>
		void SetViewport(int x, int y, int width, int height);

		/// <summary>
		/// Generates a proper 2D othographic projection for the given destination size, suitable for use in a GUI
		/// </summary>
		Matrix4x4 CreateGuiProjectionMatrix(int width, int height);

		/// <summary>
		/// Generates a proper view transform for a standard 2D othographic projection, including half-pixel jitter if necessary
		/// and re-establishing of a normal 2D graphics top-left origin. Suitable for use in a GUI
		/// </summary>
		Matrix4x4 CreateGuiViewMatrix(int width, int height, bool autoflip = true);

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
		/// Frees the provided pipeline. Same as disposing the resource.
		/// </summary>
		void FreePipeline(Pipeline pipeline);

		/// <summary>
		/// Frees the provided shader. For internal use only.
		/// </summary>
		void Internal_FreeShader(Shader shader);

		/// <summary>
		/// Frees the provided vertex layout. For internal use only.
		/// </summary>
		void Internal_FreeVertexLayout(VertexLayout vertexLayout);
	}
}
