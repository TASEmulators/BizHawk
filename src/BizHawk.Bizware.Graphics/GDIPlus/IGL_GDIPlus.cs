using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;

using SDGraphics = System.Drawing.Graphics;

namespace BizHawk.Bizware.Graphics
{
	public class IGL_GDIPlus : IGL
	{
		private GDIPlusControlRenderTarget _controlRenderTarget;

		internal GDIPlusRenderTarget CurRenderTarget;

		public EDispMethod DispMethodEnum => EDispMethod.GdiPlus;

		public void Dispose()
		{
		}

		// maximum bitmap size doesn't seem to be well defined... we'll just use D3D11's maximum size
		public int MaxTextureDimension => 16384;

		internal SDGraphics GetCurrentGraphics()
			=> CurRenderTarget?.TextureGraphics ?? _controlRenderTarget.BufferedGraphics.Graphics;

		public void ClearColor(Color color)
			=> GetCurrentGraphics().Clear(color);

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

		public IPipeline CreatePipeline(PipelineCompileArgs compileArgs)
			=> throw new NotSupportedException("GDI+ does not support pipelines");

		public void BindPipeline(IPipeline pipeline)
		{
		}

		public void Draw(int vertexCount)
		{
		}

		public void DrawIndexed(int indexCount, int indexStart, int vertexStart)
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

		public IRenderTarget CreateRenderTarget(int width, int height)
			=> new GDIPlusRenderTarget(this, width, height);

		public void BindDefaultRenderTarget()
			=> CurRenderTarget = null;

		public GDIPlusControlRenderTarget CreateControlRenderTarget(Func<GDIPlusControlRenderContext> getControlRenderContext)
		{
			if (_controlRenderTarget != null)
			{
				throw new InvalidOperationException($"{nameof(IGL_GDIPlus)} can only have one control render target");
			}

			_controlRenderTarget = new(getControlRenderContext);
			return _controlRenderTarget;
		}
	}
}
