using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;

//TODO - maybe a layer to cache Graphics parameters (notably, filtering) ?
namespace BizHawk.Bizware.BizwareGL
{
	public class IGL_GdiPlus : IGL
	{
		public EDispMethod DispMethodEnum => EDispMethod.GdiPlus;

#if false
		// rendering state
		private RenderTarget _currRenderTarget;
#endif

		private readonly Func<IGL_GdiPlus, IGraphicsControl> _createGLControlWrapper;

		public IGL_GdiPlus(Func<IGL_GdiPlus, IGraphicsControl> createGLControlWrapper)
			=> _createGLControlWrapper = createGLControlWrapper;

		public void Dispose()
		{
			MyBufferedGraphicsContext.Dispose();
		}

		public void Clear(ClearBufferMask mask)
		{
			var g = GetCurrentGraphics();
			if ((mask & ClearBufferMask.ColorBufferBit) != 0)
			{
				g.Clear(_currentClearColor);
			}
		}

		public string API => "GDIPLUS";

		public IBlendState CreateBlendState(BlendingFactorSrc colorSource, BlendEquationMode colorEquation, BlendingFactorDest colorDest,
					BlendingFactorSrc alphaSource, BlendEquationMode alphaEquation, BlendingFactorDest alphaDest)
		{
			return null;
		}

		private Color _currentClearColor = Color.Transparent;

		public void SetClearColor(Color color)
		{
			_currentClearColor = color;
		}

		public void BindArrayData(IntPtr pData)
		{
		}

		public void FreeTexture(Texture2d tex)
		{
			var tw = (GDIPTextureWrapper)tex.Opaque;
			tw.Dispose();
		}

		public Shader CreateFragmentShader(string source, string entry, bool required)
			=> null;

		public Shader CreateVertexShader(string source, string entry, bool required)
			=> null;

		public void SetBlendState(IBlendState rsBlend)
		{
			// TODO for real
		}

		private class EmptyBlendState : IBlendState
		{
		}

		private static readonly EmptyBlendState _rsBlendNoneVerbatim = new(), _rsBlendNoneOpaque = new(), _rsBlendNormal = new();

		public IBlendState BlendNoneCopy => _rsBlendNoneVerbatim;
		public IBlendState BlendNoneOpaque => _rsBlendNoneOpaque;
		public IBlendState BlendNormal => _rsBlendNormal;

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

		public void DrawArrays(PrimitiveType mode, int first, int count)
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
			=> ((GDIPTextureWrapper) texture.Opaque).MinFilter = minFilter;

		public void SetMagFilter(Texture2d texture, TextureMagFilter magFilter)
			=> ((GDIPTextureWrapper) texture.Opaque).MagFilter = magFilter;

		public Texture2d LoadTexture(Bitmap bitmap)
		{
			var sdBitmap = (Bitmap)bitmap.Clone();
			var tw = new GDIPTextureWrapper { SDBitmap = sdBitmap };
			return new(this, tw, bitmap.Width, bitmap.Height);
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
			var tw = (GDIPTextureWrapper)tex.Opaque;
			bmp.ToSysdrawingBitmap(tw.SDBitmap);
		}

		public Texture2d LoadTexture(BitmapBuffer bmp)
		{
			// definitely needed (by TextureFrugalizer at least)
			var sdBitmap = bmp.ToSysdrawingBitmap();
			var tw = new GDIPTextureWrapper { SDBitmap = sdBitmap };
			return new(this, tw, bmp.Width, bmp.Height);
		}

		public BitmapBuffer ResolveTexture2d(Texture2d tex)
		{
			var tw = (GDIPTextureWrapper)tex.Opaque;
			var blow = new BitmapLoadOptions
			{
				AllowWrap = false // must be an independent resource
			};

			var bb = new BitmapBuffer(tw.SDBitmap, blow);
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
#if false
			if (autoFlip && _currRenderTarget != null)
			{
				Matrix4 ret = Matrix4.Identity;
				ret.M22 = -1;
				ret.M42 = dims.Height;
				return ret;
			}
#endif

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
	
		public void BeginControl(IGraphicsControl control)
		{
			CurrentControl = control;
		}

		public void EndControl()
		{
			CurrentControl = null;
		}

		public void BeginScene()
		{
		}

		public void EndScene()
		{
			//maybe an inconsistent semantic with other implementations..
			//but accomplishes the needed goal of getting the current RT to render
			BindRenderTarget(null);
		}

		public IGraphicsControl Internal_CreateGraphicsControl()
		{
			var ret = _createGLControlWrapper(this);
			// create a render target for this control
			var rtw = new RenderTargetWrapper(() => MyBufferedGraphicsContext, ret);
			ret.RenderTargetWrapper = rtw;
			return ret;
		}

		public void FreeRenderTarget(RenderTarget rt)
		{
			var rtw = (RenderTargetWrapper)rt.Opaque;
			rtw.Dispose();
		}

		public RenderTarget CreateRenderTarget(int w, int h)
		{
			var tw = new GDIPTextureWrapper
			{
				SDBitmap = new(w, h, PixelFormat.Format32bppArgb)
			};
			var tex = new Texture2d(this, tw, w, h);

			var rtw = new RenderTargetWrapper(() => MyBufferedGraphicsContext);
			var rt = new RenderTarget(this, rtw, tex);
			rtw.Target = rt;
			return rt;
		}

		public void BindRenderTarget(RenderTarget rt)
		{
			if (_currOffscreenGraphics != null)
			{
				_currOffscreenGraphics.Dispose();
				_currOffscreenGraphics = null;
			}

#if false
			_currRenderTarget = rt;
			if (CurrentRenderTargetWrapper != null)
			{
				if (CurrentRenderTargetWrapper == CurrentControl.RenderTargetWrapper)
				{
					// don't do anything til swapbuffers
				}
				else
				{
					// CurrentRenderTargetWrapper.MyBufferedGraphics.Render();
				}
			}
#endif

			if (rt == null)
			{
				// null means to use the default RT for the current control
				CurrentRenderTargetWrapper = CurrentControl.RenderTargetWrapper;
			}
			else
			{
				var tw = (GDIPTextureWrapper)rt.Texture2d.Opaque;
				CurrentRenderTargetWrapper = (RenderTargetWrapper)rt.Opaque;
				_currOffscreenGraphics = Graphics.FromImage(tw.SDBitmap);
#if false
				if (CurrentRenderTargetWrapper.MyBufferedGraphics == null)
				{
					CurrentRenderTargetWrapper.CreateGraphics();
				}
#endif
			}
		}

		private Graphics _currOffscreenGraphics;

		public Graphics GetCurrentGraphics()
		{
			if (_currOffscreenGraphics != null)
			{
				return _currOffscreenGraphics;
			}

			var rtw = CurrentRenderTargetWrapper;
			return rtw.MyBufferedGraphics.Graphics;
		}

		public IGraphicsControl CurrentControl;
		public RenderTargetWrapper CurrentRenderTargetWrapper;

		public readonly BufferedGraphicsContext MyBufferedGraphicsContext = new();
	}
}
