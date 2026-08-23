using Silk.NET.OpenGL;

namespace BizHawk.Bizware.Graphics
{
	internal sealed class OpenGLRenderTarget : OpenGLTexture2D, IRenderTarget
	{
		private readonly IGL_OpenGL _openGL;
		private readonly GL GL;

		public readonly uint FBO;

		public OpenGLRenderTarget(IGL_OpenGL openGL, GL gl, int width, int height)
			: base(gl, width, height)
		{
			_openGL = openGL;
			GL = gl;

			// create the FBO
			FBO = GL.GenFramebuffer();
			Bind();

			// bind the tex to the FBO
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, TexID, 0);

			// do something, I guess say which color buffers are used by the framebuffer
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

			if ((FramebufferStatus)GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.Complete)
			{
				Dispose();
				throw new InvalidOperationException($"Error creating framebuffer (at {nameof(GL.CheckFramebufferStatus)})");
			}
		}

		public override void Dispose()
		{
			GL.DeleteFramebuffer(FBO);
			base.Dispose();
		}

		public void Bind()
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
			_openGL.DefaultRenderTargetBound = false;
		}

		public override string ToString()
			=> $"OpenGL RenderTarget: {Width}x{Height}";
	}

	public sealed class OpenGLFBOWithTexture : OpenGLTexture2D, IFBOWithTexture
	{
		private readonly GL GL;
		private readonly uint _depthBuffer;

		public uint FBO { get; }

		public OpenGLFBOWithTexture(int width, int height, bool depth) : this(GL.GetApi(SDL2OpenGLContext.GetGLProcAddress), width, height, depth) {}
		private OpenGLFBOWithTexture(GL gl, int width, int height, bool depth) : base(gl, width, height)
		{
			GL = gl;

			// create the FBO
			FBO = GL.GenFramebuffer();
			Bind();

			// bind the tex to the FBO
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, TexID, 0);

			// do something, I guess say which color buffers are used by the framebuffer
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

			if (depth)
			{
				_depthBuffer = GL.GenRenderbuffer();

				GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthBuffer);
				GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, (uint) width, (uint) height);
				GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _depthBuffer);
			}

			if ((FramebufferStatus)GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.Complete)
			{
				Dispose();
				throw new InvalidOperationException($"Error creating framebuffer (at {nameof(GL.CheckFramebufferStatus)})");
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			if (_depthBuffer != 0) GL.DeleteRenderbuffer(_depthBuffer);
			GL.DeleteFramebuffer(FBO);
			GL.Dispose();
		}

		public void Bind()
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
		}
	}
}
