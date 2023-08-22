using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;

using BizHawk.Bizware.BizwareGL;

using SDGraphics = System.Drawing.Graphics;

//TODO - maybe a layer to cache Graphics parameters (notably, filtering) ?
namespace BizHawk.Bizware.Graphics
{
	public class IGL_GDIPlus : IGL
	{
		public EDispMethod DispMethodEnum => EDispMethod.GdiPlus;

		public void Dispose()
		{
			BufferedGraphicsContext.Dispose();
		}

		public void ClearColor(Color color)
			=> GetCurrentGraphics().Clear(color);

		public string API => "GDIPLUS";

		public void FreeTexture(Texture2d tex)
		{
			var gtex = (GDIPlusTexture)tex.Opaque;
			gtex.Dispose();
		}

		public Shader CreateFragmentShader(string source, string entry, bool required)
			=> null;

		public Shader CreateVertexShader(string source, string entry, bool required)
			=> null;

		public void EnableBlending()
		{
			// TODO for real
		}

		public void DisableBlending()
		{
			// TODO for real
		}

		public Pipeline CreatePipeline(VertexLayout vertexLayout, Shader vertexShader, Shader fragmentShader, bool required, string memo)
		{
			return null;
		}

		public void FreePipeline(Pipeline pipeline)
		{
		}

		public VertexLayout CreateVertexLayout()
			=> new(this, null);

		public void Internal_FreeVertexLayout(VertexLayout layout)
		{
		}

		public void SetTextureWrapMode(Texture2d tex, bool clamp)
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

		public void SetPipelineUniformSampler(PipelineUniform uniform, Texture2d tex)
		{
		}

		public void SetMinFilter(Texture2d texture, TextureMinFilter minFilter)
			=> ((GDIPlusTexture) texture.Opaque).MinFilter = minFilter;

		public void SetMagFilter(Texture2d texture, TextureMagFilter magFilter)
			=> ((GDIPlusTexture) texture.Opaque).MagFilter = magFilter;

		public Texture2d LoadTexture(Bitmap bitmap)
		{
			var sdBitmap = (Bitmap)bitmap.Clone();
			var gtex = new GDIPlusTexture { SDBitmap = sdBitmap };
			return new(this, gtex, bitmap.Width, bitmap.Height);
		}

		public Texture2d LoadTexture(Stream stream)
		{
			using var bmp = new BitmapBuffer(stream, new());
			return LoadTexture(bmp);
		}

		public Texture2d CreateTexture(int width, int height)
			=> null;

		public Texture2d WrapGLTexture2d(IntPtr glTexId, int width, int height)
		{
			// only used for OpenGL
			return null;
		}

		public void LoadTextureData(Texture2d tex, BitmapBuffer bmp)
		{
			var gtex = (GDIPlusTexture)tex.Opaque;
			bmp.ToSysdrawingBitmap(gtex.SDBitmap);
		}

		public Texture2d LoadTexture(BitmapBuffer bmp)
		{
			// definitely needed (by TextureFrugalizer at least)
			var sdBitmap = bmp.ToSysdrawingBitmap();
			var gtex = new GDIPlusTexture { SDBitmap = sdBitmap };
			return new(this, gtex, bmp.Width, bmp.Height);
		}

		public BitmapBuffer ResolveTexture2d(Texture2d tex)
		{
			var gtex = (GDIPlusTexture)tex.Opaque;
			var blow = new BitmapLoadOptions
			{
				AllowWrap = false // must be an independent resource
			};

			var bb = new BitmapBuffer(gtex.SDBitmap, blow);
			return bb;
		}

		public Texture2d LoadTexture(string path)
		{
			using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			return LoadTexture(fs);
		}

		public Matrix4x4 CreateGuiProjectionMatrix(int w, int h)
		{
			return CreateGuiProjectionMatrix(new(w, h));
		}

		public Matrix4x4 CreateGuiViewMatrix(int w, int h, bool autoFlip)
		{
			return CreateGuiViewMatrix(new(w, h), autoFlip);
		}

		public Matrix4x4 CreateGuiProjectionMatrix(Size dims)
		{
			// see CreateGuiViewMatrix for more
			return Matrix4x4.Identity;
		}

		public Matrix4x4 CreateGuiViewMatrix(Size dims, bool autoFlip)
		{
			// on account of gdi+ working internally with a default view exactly like we want, we don't need to setup a new one here
			// furthermore, we _cant_, without inverting the GuiView and GuiProjection before drawing, to completely undo it
			// this might be feasible, but its kind of slow and annoying and worse, seemingly numerically unstable
			return Matrix4x4.Identity;
		}

		public void SetViewport(int x, int y, int width, int height)
		{
		}

		public void SetViewport(int width, int height)
		{
		}

		public void SetViewport(Size size)
		{
			SetViewport(size.Width, size.Height);
		}

		public void BeginScene()
		{
		}

		public void EndScene()
		{
			// maybe an inconsistent semantic with other implementations..
			// but accomplishes the needed goal of getting the current RT to render
			BindRenderTarget(null);
		}

		public void FreeRenderTarget(RenderTarget rt)
		{
			var grt = (GDIPlusRenderTarget)rt.Opaque;
			grt.Dispose();
		}

		public RenderTarget CreateRenderTarget(int w, int h)
		{
			var gtex = new GDIPlusTexture
			{
				SDBitmap = new(w, h, PixelFormat.Format32bppArgb)
			};
			var tex = new Texture2d(this, gtex, w, h);

			var grt = new GDIPlusRenderTarget(() => BufferedGraphicsContext);
			var rt = new RenderTarget(this, grt, tex);
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
				var gtex = (GDIPlusTexture)rt.Texture2d.Opaque;
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
		{
			return _currOffscreenGraphics ?? CurrentRenderTarget.BufferedGraphics.Graphics;
		}

		public GDIPlusRenderTarget CurrentRenderTarget;

		public readonly BufferedGraphicsContext BufferedGraphicsContext = new();
	}
}
