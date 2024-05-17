using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;

using SDGraphics = System.Drawing.Graphics;

//TODO - maybe a layer to cache Graphics parameters (notably, filtering) ?
namespace BizHawk.Bizware.Graphics
{
	public class IGL_GDIPlus : IGL
	{
		public EDispMethod DispMethodEnum => EDispMethod.GdiPlus;

		public void Dispose()
			=> BufferedGraphicsContext.Dispose();

		public void ClearColor(Color color)
			=> GetCurrentGraphics().Clear(color);

		public Shader CreateFragmentShader(string source, string entry, bool required)
			=> null;

		public Shader CreateVertexShader(string source, string entry, bool required)
			=> null;

		public void EnableBlending()
		{
			var g = GetCurrentGraphics();
			g.CompositingMode = CompositingMode.SourceOver;
			g.CompositingQuality = CompositingQuality.Default;
		}

		public void DisableBlending()
		{
			var g = GetCurrentGraphics();
			g.CompositingMode = CompositingMode.SourceCopy;
			g.CompositingQuality = CompositingQuality.HighSpeed;
		}

		public Pipeline CreatePipeline(VertexLayout vertexLayout, Shader vertexShader, Shader fragmentShader, bool required, string memo)
			=> null;

		public void FreePipeline(Pipeline pipeline)
		{
		}

		public VertexLayout CreateVertexLayout()
			=> new(this, null);

		public void Internal_FreeVertexLayout(VertexLayout layout)
		{
		}

		public void Draw(IntPtr data, int count)
		{
		}

		public void BindPipeline(Pipeline pipeline)
		{

		}

		public void Internal_FreeShader(Shader shader)
		{
		}

		public void SetPipelineUniform(PipelineUniform uniform, bool value)
		{
		}

		public void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4x4 mat, bool transpose)
		{
		}

		public void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4x4 mat, bool transpose)
		{
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector4 value)
		{
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector2 value)
		{
		}

		public void SetPipelineUniform(PipelineUniform uniform, float value)
		{
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector4[] values)
		{
		}

		public void SetPipelineUniformSampler(PipelineUniform uniform, ITexture2D tex)
		{
		}

		public ITexture2D CreateTexture(int width, int height)
			=> new GDIPlusTexture2D(width, height);

		// only used for OpenGL
		public ITexture2D WrapGLTexture2D(int glTexId, int width, int height)
			=> null;

		// see CreateGuiViewMatrix for more
		public Matrix4x4 CreateGuiProjectionMatrix(int width, int height)
			=> Matrix4x4.Identity;

		// on account of gdi+ working internally with a default view exactly like we want, we don't need to setup a new one here
		// furthermore, we _cant_, without inverting the GuiView and GuiProjection before drawing, to completely undo it
		// this might be feasible, but its kind of slow and annoying and worse, seemingly numerically unstable
		public Matrix4x4 CreateGuiViewMatrix(int width, int height, bool autoFlip)
			=> Matrix4x4.Identity;

		public void SetViewport(int x, int y, int width, int height)
		{
		}

		public void FreeRenderTarget(RenderTarget rt)
		{
			var grt = (GDIPlusRenderTarget)rt.Opaque;
			grt.Dispose();
		}

		public RenderTarget CreateRenderTarget(int width, int height)
		{
			var tex2d = new GDIPlusTexture2D(width, height);
			var grt = new GDIPlusRenderTarget(() => BufferedGraphicsContext);
			var rt = new RenderTarget(this, grt, tex2d);
			grt.Target = rt;
			return rt;
		}

		public void BindRenderTarget(RenderTarget rt)
		{
			if (_currOffscreenGraphics != null)
			{
				_currOffscreenGraphics.Dispose();
				_currOffscreenGraphics = null;
			}

			if (rt == null)
			{
				// null means to use the default RT for the current control
				CurrentRenderTarget = _controlRenderTarget;
			}
			else
			{
				var gtex = (GDIPlusTexture2D)rt.Texture2D;
				CurrentRenderTarget = (GDIPlusRenderTarget)rt.Opaque;
				_currOffscreenGraphics = SDGraphics.FromImage(gtex.SDBitmap);
			}
		}

		private GDIPlusRenderTarget _controlRenderTarget;

		public GDIPlusRenderTarget CreateControlRenderTarget(Func<(SDGraphics Graphics, Rectangle Rectangle)> getControlRenderContext)
		{
			if (_controlRenderTarget != null)
			{
				throw new InvalidOperationException($"{nameof(IGL_GDIPlus)} can only have one control render target");
			}

			_controlRenderTarget = new(() => BufferedGraphicsContext, getControlRenderContext);
			return _controlRenderTarget;
		}

		private SDGraphics _currOffscreenGraphics;

		public SDGraphics GetCurrentGraphics()
			=> _currOffscreenGraphics ?? CurrentRenderTarget.BufferedGraphics.Graphics;

		public GDIPlusRenderTarget CurrentRenderTarget;

		public readonly BufferedGraphicsContext BufferedGraphicsContext = new();
	}
}
