using System.Drawing;
using System.Numerics;

using Silk.NET.OpenGL;

#pragma warning disable BHI1007 // target-typed Exception TODO don't

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// OpenGL implementation of the IGL interface
	/// </summary>
	public class IGL_OpenGL : IGL
	{
		public EDispMethod DispMethodEnum => EDispMethod.OpenGL;

		private readonly GL GL;

		// rendering state
		private OpenGLPipeline _curPipeline;
		internal bool DefaultRenderTargetBound;

		// this IGL either requires at least OpenGL 3.2
		public static bool Available => OpenGLVersion.SupportsVersion(3, 2);

		public IGL_OpenGL()
		{
			if (!Available)
			{
				throw new InvalidOperationException("OpenGL 3.2 is required and unavailable");
			}

			GL = GL.GetApi(SDL2OpenGLContext.GetGLProcAddress);
		}

		public void Dispose()
			=> GL.Dispose();

		/// <summary>
		/// Should be called once the GL context is created
		/// </summary>
		public void InitGLState()
		{
			GL.GetInteger(GetPName.MaxTextureSize, out var maxTextureDimension);
			if (maxTextureDimension == 0)
			{
				throw new($"Failed to get max texture size, GL error: {GL.GetError()}");
			}

			MaxTextureDimension = maxTextureDimension;
		}

		public int MaxTextureDimension { get; private set; }

		public void ClearColor(Color color)
		{
			GL.ClearColor(color);
			GL.Clear(ClearBufferMask.ColorBufferBit);
		}

		public void EnableBlending()
		{
			GL.Enable(EnableCap.Blend);
			GL.BlendEquation(GLEnum.FuncAdd);
			GL.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
		}

		public void DisableBlending()
			=> GL.Disable(EnableCap.Blend);

		public IPipeline CreatePipeline(PipelineCompileArgs compileArgs)
		{
			try
			{
				return new OpenGLPipeline(GL, compileArgs);
			}
			finally
			{
				BindPipeline(null);
			}
		}

		public void BindPipeline(IPipeline pipeline)
		{
			_curPipeline = (OpenGLPipeline)pipeline;

			if (_curPipeline == null)
			{
				GL.BindVertexArray(0);
				GL.BindBuffer(GLEnum.ArrayBuffer, 0);
				GL.BindBuffer(GLEnum.ElementArrayBuffer, 0);
				GL.UseProgram(0);
				_curPipeline = null;
				return;
			}

			GL.BindVertexArray(_curPipeline.VAO);
			GL.BindBuffer(GLEnum.ArrayBuffer, _curPipeline.VBO);
			GL.BindBuffer(GLEnum.ElementArrayBuffer, _curPipeline.IBO);
			GL.UseProgram(_curPipeline.PID);
		}

		public void Draw(int vertexCount)
			=> GL.DrawArrays(PrimitiveType.TriangleStrip, 0, (uint)vertexCount);

		public unsafe void DrawIndexed(int indexCount, int indexStart, int vertexStart)
			=> GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (uint)indexCount, DrawElementsType.UnsignedShort, (void*)(indexStart * 2), vertexStart);

		public ITexture2D CreateTexture(int width, int height)
			=> new OpenGLTexture2D(GL, width, height);

		public ITexture2D WrapGLTexture2D(int glTexId, int width, int height)
			=> new OpenGLTexture2D(GL, (uint)glTexId, width, height);

		/// <exception cref="InvalidOperationException">framebuffer creation unsuccessful</exception>
		public IRenderTarget CreateRenderTarget(int width, int height)
			=> new OpenGLRenderTarget(this, GL, width, height);

		public void BindDefaultRenderTarget()
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			DefaultRenderTargetBound = true;
		}

		public Matrix4x4 CreateGuiProjectionMatrix(int width, int height)
		{
			var ret = Matrix4x4.Identity;
			ret.M11 = 2.0f / width;
			ret.M22 = 2.0f / height;
			return ret;
		}

		public Matrix4x4 CreateGuiViewMatrix(int width, int height, bool autoflip)
		{
			var ret = Matrix4x4.Identity;
			ret.M22 = -1.0f;
			ret.M41 = width * -0.5f;
			ret.M42 = height * 0.5f;
			if (autoflip && !DefaultRenderTargetBound) // flip as long as we're not a final render target
			{
				ret.M22 = 1.0f;
				ret.M42 *= -1;
			}

			return ret;
		}

		public void SetViewport(int x, int y, int width, int height)
		{
			GL.Viewport(x, y, (uint)width, (uint)height);
			GL.Scissor(x, y, (uint)width, (uint)height); // hack for mupen[rice]+intel: at least the rice plugin leaves the scissor rectangle scrambled, and we're trying to run it in the main graphics context for intel
			// BUT ALSO: new specifications.. viewport+scissor make sense together
		}
	}
}
