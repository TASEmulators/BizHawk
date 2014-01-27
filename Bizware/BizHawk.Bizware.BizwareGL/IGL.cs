using System;
using System.IO;
using sd=System.Drawing;
using swf=System.Windows.Forms;

using OpenTK;

namespace BizHawk.Bizware.BizwareGL
{

	/// <summary>
	/// This is a wrapper over hopefully any OpenGL bindings..
	/// And possibly, quite possibly, Direct3d.. even though none of your shaders would work. (could use nvidia CG, native dlls in necessary since this would only be for windows)
	/// TODO - This really needs to be split up into an internal and a user interface. so many of the functions are made to support the smart wrappers
	/// Maybe make a method that returns an interface used for advanced methods (and IGL_TK could implement that as well and just "return this:")
	/// </summary>
	public interface IGL : IDisposable
	{
		/// <summary>
		/// Returns an a control optimized for drawing onto the screen.
		/// </summary>
		GraphicsControl CreateGraphicsControl();

		/// <summary>
		/// Clears the specified buffer parts
		/// </summary>
		/// <param name="mask"></param>
		void Clear(ClearBufferMask mask);

		/// <summary>
		/// Sets the current clear color
		/// </summary>
		void ClearColor(sd.Color color);

		/// <summary>
		/// generates a texture handle
		/// </summary>
		IntPtr GenTexture();

		/// <summary>
		/// returns an empty handle
		/// </summary>
		IntPtr GetEmptyHandle();

		/// <summary>
		/// returns an empty uniform handle
		/// </summary>
		IntPtr GetEmptyUniformHandle();

		/// <summary>
		/// compile a fragment shader. This is the simplified method. A more complex method may be added later which will accept multiple sources and preprocessor definitions independently
		/// </summary>
		Shader CreateFragmentShader(string source);

		/// <summary>
		/// compile a vertex shader. This is the simplified method. A more complex method may be added later which will accept multiple sources and preprocessor definitions independently
		/// </summary>
		Shader CreateVertexShader(string source);

		/// <summary>
		/// Creates a complete pipeline from the provided vertex and fragment shader handles
		/// </summary>
		Pipeline CreatePipeline(Shader vertexShader, Shader fragmentShader);

		/// <summary>
		/// Binds this pipeline as the current used for rendering
		/// </summary>
		void BindPipeline(Pipeline pipeline);

		/// <summary>
		/// Sets a uniform sampler to use use the provided texture handle
		/// </summary>
		void SetPipelineUniformSampler(PipelineUniform uniform, IntPtr texHandle);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4 mat, bool transpose);

		/// <summary>
		/// Sets a uniform value
		/// </summary>
		void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4 mat, bool transpose);

		/// <summary>
		/// sets a uniform value
		/// </summary>
		void SetPipelineUniform(PipelineUniform uniform, Vector4 value);

		/// <summary>
		/// Binds this VertexLayout for use in rendering (in OpenGL's case, by glVertexAttribPointer calls)
		/// </summary>
		void BindVertexLayout(VertexLayout layout);

		/// <summary>
		/// Binds array data for use with the currently-bound VertexLayout
		/// </summary>
		unsafe void BindArrayData(void* pData);

		/// <summary>
		/// Draws based on the currently set VertexLayout and ArrayData
		/// </summary>
		void DrawArrays(PrimitiveType mode, int first, int count);

		/// <summary>
		/// Frees the provided shader handle
		/// </summary>
		void FreeShader(IntPtr shader);

		/// <summary>
		/// frees the provided texture handle
		/// </summary>
		void FreeTexture(IntPtr texHandle);

		/// <summary>
		/// Binds this texture as the current texture2d target for parameter-specification
		/// </summary>
		/// <param name="texture"></param>
		void BindTexture2d(Texture2d texture);

		/// <summary>
		/// Sets a 2d texture parameter
		/// </summary>
		void TexParameter2d(TextureParameterName pname, int param);

		/// <summary>
		/// creates a vertex layout resource
		/// </summary>
		VertexLayout CreateVertexLayout();

		/// <summary>
		/// Creates a blending state object
		/// </summary>
		IBlendState CreateBlendState(BlendingFactor colorSource, BlendEquationMode colorEquation, BlendingFactor colorDest,
			BlendingFactor alphaSource, BlendEquationMode alphaEquation, BlendingFactor alphaDest);

		/// <summary>
		/// retrieves a blend state for opaque rendering
		/// Alpha values are copied from the source fragment.
		/// </summary>
		IBlendState BlendNone { get; }

		/// <summary>
		/// retrieves a blend state for normal (non-premultiplied) alpha blending.
		/// Alpha values are copied from the source fragment.
		/// </summary>
		IBlendState BlendNormal { get; }

		/// <summary>
		/// Sets the current blending state object
		/// </summary>
		void SetBlendState(IBlendState rsBlend);

		/// <summary>
		/// Creates a texture with the specified dimensions
		/// TODO - pass in specifications somehow
		/// </summary>
		Texture2d CreateTexture(int width, int height);

		/// <summary>
		/// Loads the texture with new data. This isnt supposed to be especially versatile, it just blasts a bitmap buffer into the texture
		/// </summary>
		void LoadTextureData(Texture2d tex, BitmapBuffer bmp);

		/// <summary>
		/// Loads a texture from disk
		/// </summary>
		Texture2d LoadTexture(string path);

		/// <summary>
		/// Loads a texture from the stream
		/// </summary>
		Texture2d LoadTexture(Stream stream);

		/// <summary>
		/// Loads a texture from the BitmapBuffer
		/// </summary>
		Texture2d LoadTexture(BitmapBuffer buffer);

		/// <summary>
		/// Loads a texture from the System.Drawing.Bitmap
		/// </summary>
		Texture2d LoadTexture(sd.Bitmap bitmap);

		/// <summary>
		/// sets the viewport according to the provided specifications
		/// </summary>
		void SetViewport(int x, int y, int width, int height);

		/// <summary>
		/// sets the viewport according to the client area of the provided control
		/// </summary>
		void SetViewport(swf.Control control);

		/// <summary>
		/// generates a proper 2d othographic projection for the given destination size, suitable for use in a GUI
		/// </summary>
		Matrix4 CreateGuiProjectionMatrix(int w, int h);

		/// <summary>
		/// generates a proper view transform for a standard 2d ortho projection, including half-pixel jitter if necessary and
		/// re-establishing of a normal 2d graphics top-left origin. suitable for use in a GUI
		/// </summary>
		Matrix4 CreateGuiViewMatrix(int w, int h);
	}
}
