using System;
using System.IO;
using System.Drawing;
using swf=System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL
{

	/// <summary>
	/// This is a wrapper over hopefully any OpenGL bindings..
	/// And possibly, quite possibly, Direct3d.. even though none of your shaders would work. (could use nvidia CG, native dlls in necessary since this would only be for windows)
	/// TODO - This really needs to be split up into an internal and a user interface. so many of the functions are made to support the smart wrappers
	/// Maybe make a method that returns an interface used for advanced methods (and IGL_TK could implement that as well and just "return this:")
	/// 
	/// NOTE: THIS SHOULD NOT BE ASSUMED TO BE THREAD SAFE! Make a new IGL if you want to use it in a new thread. I hope that will work...
	/// </summary>
	public interface IGL : IDisposable
	{
		/// <summary>
		/// Clears the specified buffer parts
		/// </summary>
		void Clear(ClearBufferMask mask);

		/// <summary>
		/// Sets the current clear color
		/// </summary>
		void SetClearColor(Color color);

		/// <summary>
		/// compile a fragment shader. This is the simplified method. A more complex method may be added later which will accept multiple sources and preprocessor definitions independently
		/// </summary>
		Shader CreateFragmentShader(bool cg, string source, string entry, bool required);

		/// <summary>
		/// compile a vertex shader. This is the simplified method. A more complex method may be added later which will accept multiple sources and preprocessor definitions independently
		/// </summary>
		Shader CreateVertexShader(bool cg, string source, string entry, bool required);

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
		void SetPipelineUniformSampler(PipelineUniform uniform, Texture2d tex);

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
		/// sets a uniform value
		/// </summary>
		void SetPipelineUniform(PipelineUniform uniform, Vector2 value);

		/// <summary>
		/// sets a uniform value
		/// </summary>
		void SetPipelineUniform(PipelineUniform uniform, float value);

		/// <summary>
		/// sets uniform values
		/// </summary>
		void SetPipelineUniform(PipelineUniform uniform, Vector4[] values);

		/// <summary>
		/// sets a uniform value
		/// </summary>
		void SetPipelineUniform(PipelineUniform uniform, bool value);

		/// <summary>
		/// Binds array data for use with the currently-bound pipeline's VertexLayout
		/// </summary>
		unsafe void BindArrayData(void* pData);

		/// <summary>
		/// Begins a rendering scene; use before doing any draw calls, as per normal
		/// </summary>
		void BeginScene();

		/// <summary>
		/// Indicates end of scene rendering; use after alldraw calls as per normal
		/// </summary>
		void EndScene();

		/// <summary>
		/// Draws based on the currently set pipeline, VertexLayout and ArrayData.
		/// Count is the VERT COUNT not the primitive count
		/// </summary>
		void DrawArrays(PrimitiveType mode, int first, int count);

		/// <summary>
		/// resolves the texture into a new BitmapBuffer
		/// </summary>
		BitmapBuffer ResolveTexture2d(Texture2d texture);

		/// <summary>
		/// Sets a 2d texture parameter
		/// </summary>
		void TexParameter2d(Texture2d texture, TextureParameterName pname, int param);

		/// <summary>
		/// creates a vertex layout resource
		/// </summary>
		VertexLayout CreateVertexLayout();

		/// <summary>
		/// Creates a blending state object
		/// </summary>
		IBlendState CreateBlendState(BlendingFactorSrc colorSource, BlendEquationMode colorEquation, BlendingFactorDest colorDest,
			BlendingFactorSrc alphaSource, BlendEquationMode alphaEquation, BlendingFactorDest alphaDest);

		/// <summary>
		/// retrieves a blend state for opaque rendering
		/// Alpha values are copied from the source fragment.
		/// </summary>
		IBlendState BlendNoneCopy { get; }

		/// <summary>
		/// retrieves a blend state for opaque rendering
		/// Alpha values are written as opaque
		/// </summary>
		IBlendState BlendNoneOpaque { get; }

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
		/// In case you already have the texture ID (from an opengl emulator gpu core) you can get a Texture2d with it this way.
		/// Otherwise, if this isn't an OpenGL frontend implementation, I guess... try reading the texturedata out of it and making a new texture?
		/// </summary>
		Texture2d WrapGLTexture2d(IntPtr glTexId, int width, int height);

		/// <summary>
		/// Sets the clamp mode (for both uv) for the Texture2d.
		/// The default is clamped=true.
		/// </summary>
		void SetTextureWrapMode(Texture2d tex, bool clamp);

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
		Texture2d LoadTexture(Bitmap bitmap);

		/// <summary>
		/// sets the viewport (and scissor) according to the provided specifications
		/// </summary>
		void SetViewport(int x, int y, int width, int height);

		/// <summary>
		/// sets the viewport (and scissor) according to the provided specifications
		/// </summary>
		void SetViewport(int width, int height);

		/// <summary>
		/// sets the viewport (and scissor) according to the client area of the provided control
		/// </summary>
		void SetViewport(swf.Control control);

		/// <summary>
		/// sets the viewport (and scissor) according to the provided specifications
		/// </summary>
		void SetViewport(Size size);

		/// <summary>
		/// generates a proper 2d othographic projection for the given destination size, suitable for use in a GUI
		/// </summary>
		Matrix4 CreateGuiProjectionMatrix(int w, int h);

		/// <summary>
		/// generates a proper 2d othographic projection for the given destination size, suitable for use in a GUI
		/// </summary>
		Matrix4 CreateGuiProjectionMatrix(Size dims);

		/// <summary>
		/// generates a proper view transform for a standard 2d ortho projection, including half-pixel jitter if necessary and
		/// re-establishing of a normal 2d graphics top-left origin. suitable for use in a GUI
		/// </summary>
		Matrix4 CreateGuiViewMatrix(int w, int h, bool autoflip = true);

		/// <summary>
		/// generates a proper view transform for a standard 2d ortho projection, including half-pixel jitter if necessary and
		/// re-establishing of a normal 2d graphics top-left origin. suitable for use in a GUI
		/// </summary>
		Matrix4 CreateGuiViewMatrix(Size dims, bool autoflip = true);

		/// <summary>
		/// Creates a render target. Only includes a color buffer. Pixel format control TBD
		/// </summary>
		RenderTarget CreateRenderTarget(int w, int h);

		/// <summary>
		/// Binds a RenderTarget for current rendering
		/// </summary>
		void BindRenderTarget(RenderTarget rt);

		/// <summary>
		/// returns a string representing the API employed by this context
		/// </summary>
		string API { get; }

		/// <summary>
		/// frees the provided render target. Same as disposing the resource.
		/// </summary>
		void FreeRenderTarget(RenderTarget rt);

		/// <summary>
		/// frees the provided texture. Same as disposing the resource.
		/// </summary>
		void FreeTexture(Texture2d tex);

		/// <summary>
		/// Frees the provided pipeline. Same as disposing the resource.
		/// </summary>
		void FreePipeline(Pipeline pipeline);

		/// <summary>
		/// Frees the provided texture. For internal use only.
		/// </summary>
		void Internal_FreeShader(Shader shader);

		IGraphicsControl Internal_CreateGraphicsControl();
	}
}
