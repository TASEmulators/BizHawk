using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using swf = System.Windows.Forms;
using sd = System.Drawing;
using sdi = System.Drawing.Imaging;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using BizHawk.Bizware.BizwareGL;

//TODO - maybe a layer to cache Graphics parameters (notably, filtering) ?

namespace BizHawk.Bizware.BizwareGL.Drivers.GdiPlus
{
	public class IGL_GdiPlus : IGL
	{
		//rendering state
		RenderTarget _CurrRenderTarget;

		public IGL_GdiPlus()
		{
			MyBufferedGraphicsContext = new BufferedGraphicsContext();
		}

		void IDisposable.Dispose()
		{
		}

		public void Clear(OpenTK.Graphics.OpenGL.ClearBufferMask mask)
		{
			var g = GetCurrentGraphics();
			if((mask & ClearBufferMask.ColorBufferBit) != 0)
			{
				g.Clear(_currentClearColor);
			}
		}

		public string API { get { return "GDIPLUS"; } }

		public IBlendState CreateBlendState(BlendingFactorSrc colorSource, BlendEquationMode colorEquation, BlendingFactorDest colorDest,
					BlendingFactorSrc alphaSource, BlendEquationMode alphaEquation, BlendingFactorDest alphaDest)
		{
			return null;
		}

		private sd.Color _currentClearColor = Color.Transparent;
		public void SetClearColor(sd.Color color)
		{
			_currentClearColor = color;
		}
		
		public unsafe void BindArrayData(void* pData)
		{
		}


		public void FreeTexture(Texture2d tex)
		{
			var tw = tex.Opaque as TextureWrapper;
			tw.Dispose();
		}
		
		public Shader CreateFragmentShader(bool cg, string source, string entry, bool required)
		{
			return null;
		}
		public Shader CreateVertexShader(bool cg, string source, string entry, bool required)
		{
			return null;
		}

		public void FreeShader(IntPtr shader) {  }

		public void SetBlendState(IBlendState rsBlend)
		{
			//TODO for real
		}

		class MyBlendState : IBlendState { }
		static MyBlendState _rsBlendNoneVerbatim = new MyBlendState(), _rsBlendNoneOpaque = new MyBlendState(), _rsBlendNormal = new MyBlendState();

		public IBlendState BlendNoneCopy { get { return _rsBlendNoneVerbatim; } }
		public IBlendState BlendNoneOpaque { get { return _rsBlendNoneOpaque; } }
		public IBlendState BlendNormal { get { return _rsBlendNormal; } }

		public Pipeline CreatePipeline(VertexLayout vertexLayout, Shader vertexShader, Shader fragmentShader, bool required, string memo)
		{
			return null;
		}

		public void FreePipeline(Pipeline pipeline) {}

		public VertexLayout CreateVertexLayout() { return new VertexLayout(this, new IntPtr(0)); }

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

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4 mat, bool transpose)
		{
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4 mat, bool transpose)
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

		public unsafe void SetPipelineUniform(PipelineUniform uniform, Vector4[] values)
		{
		}

		public void SetPipelineUniformSampler(PipelineUniform uniform, Texture2d tex)
		{
	
		}

		public void TexParameter2d(Texture2d tex, TextureParameterName pname, int param)
		{
			var tw = tex.Opaque as TextureWrapper;
			if (pname == TextureParameterName.TextureMinFilter)
				tw.MinFilter = (TextureMinFilter)param;
			if (pname == TextureParameterName.TextureMagFilter)
				tw.MagFilter = (TextureMagFilter)param;
		}

		public Texture2d LoadTexture(sd.Bitmap bitmap)
		{
			var sdbmp = (sd.Bitmap)bitmap.Clone();
			TextureWrapper tw = new TextureWrapper();
			tw.SDBitmap = sdbmp;
			return new Texture2d(this, tw, bitmap.Width, bitmap.Height);
		}

		public Texture2d LoadTexture(Stream stream)
		{
			using (var bmp = new BitmapBuffer(stream, new BitmapLoadOptions()))
				return (this as IGL).LoadTexture(bmp);
		}

		public Texture2d CreateTexture(int width, int height)
		{
			return null;
		}

		public Texture2d WrapGLTexture2d(IntPtr glTexId, int width, int height)
		{
			//TODO - need to rip the texturedata. we had code for that somewhere...
			return null;
		}

		public void LoadTextureData(Texture2d tex, BitmapBuffer bmp)
		{
			var tw = tex.Opaque as TextureWrapper;
			bmp.ToSysdrawingBitmap(tw.SDBitmap);
		}


		public Texture2d LoadTexture(BitmapBuffer bmp)
		{
			//definitely needed (by TextureFrugalizer at least)
			var sdbmp = bmp.ToSysdrawingBitmap();
			var tw = new TextureWrapper();
			tw.SDBitmap = sdbmp;
			return new Texture2d(this, tw, bmp.Width, bmp.Height);
		}

		public unsafe BitmapBuffer ResolveTexture2d(Texture2d tex)
		{
			var tw = tex.Opaque as TextureWrapper;
			var blow = new BitmapLoadOptions()
			{
				AllowWrap = false //must be an independent resource
			};
			var bb = new BitmapBuffer(tw.SDBitmap,blow); 
			return bb;
		}

		public Texture2d LoadTexture(string path)
		{
			//todo
			//using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			//  return (this as IGL).LoadTexture(fs);
			return null;
		}

		public Matrix4 CreateGuiProjectionMatrix(int w, int h)
		{
			return CreateGuiProjectionMatrix(new sd.Size(w, h));
		}

		public Matrix4 CreateGuiViewMatrix(int w, int h, bool autoflip)
		{
			return CreateGuiViewMatrix(new sd.Size(w, h), autoflip);
		}

		public Matrix4 CreateGuiProjectionMatrix(sd.Size dims)
		{
			//see CreateGuiViewMatrix for more
			return Matrix4.Identity;
		}

		public Matrix4 CreateGuiViewMatrix(sd.Size dims, bool autoflip)
		{
			//on account of gdi+ working internally with a default view exactly like we want, we don't need to setup a new one here
			//furthermore, we _cant_, without inverting the GuiView and GuiProjection before drawing, to completely undo it
			//this might be feasible, but its kind of slow and annoying and worse, seemingly numerically unstable
			//if (autoflip && _CurrRenderTarget != null)
			//{
			//  Matrix4 ret = Matrix4.Identity;
			//  ret.M22 = -1;
			//  ret.M42 = dims.Height;
			//  return ret;
			//}
			//else 
				return Matrix4.Identity;
		}

		public void SetViewport(int x, int y, int width, int height)
		{
		}

		public void SetViewport(int width, int height)
		{
		}

		public void SetViewport(sd.Size size)
		{
			SetViewport(size.Width, size.Height);
		}

		public void SetViewport(swf.Control control)
		{
			
		}

	
		public void BeginControl(GLControlWrapper_GdiPlus control)
		{
			CurrentControl = control;
		}

		public void EndControl(GLControlWrapper_GdiPlus control)
		{
			CurrentControl = null;
		}

		public void SwapControl(GLControlWrapper_GdiPlus control)
		{
		}

		public class RenderTargetWrapper
		{
			public RenderTargetWrapper(IGL_GdiPlus gdi)
			{
				Gdi = gdi;
			}

			public void Dispose()
			{
			}

			IGL_GdiPlus Gdi;

			/// <summary>
			/// the control associated with this render target (if any)
			/// </summary>
			public GLControlWrapper_GdiPlus Control;

			/// <summary>
			/// the offscreen render target, if that's what this is representing
			/// </summary>
			public RenderTarget Target;

			public BufferedGraphics MyBufferedGraphics;

			public Graphics refGraphics; //?? hacky?

			public void CreateGraphics()
			{
				Rectangle r;
				if (Control != null)
				{
					r = Control.ClientRectangle;
					refGraphics = Control.CreateGraphics();
				}
				else
				{
					var tw = Target.Texture2d.Opaque as TextureWrapper;
					r = Target.Texture2d.Rectangle;
					refGraphics = Graphics.FromImage(tw.SDBitmap);
				}

				if (MyBufferedGraphics != null)
					MyBufferedGraphics.Dispose();
				MyBufferedGraphics = Gdi.MyBufferedGraphicsContext.Allocate(refGraphics, r);
				//MyBufferedGraphics.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;

				////not sure about this stuff...
				////it will wreck alpha blending, for one thing
				//MyBufferedGraphics.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
				//MyBufferedGraphics.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
			}
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
			var ret = new GLControlWrapper_GdiPlus(this);
			
			//create a render target for this control
			RenderTargetWrapper rtw = new RenderTargetWrapper(this);
			rtw.Control = ret;
			ret.RenderTargetWrapper = rtw;
			
			return ret;
		}

		public void FreeRenderTarget(RenderTarget rt)
		{
			var rtw = rt.Opaque as RenderTargetWrapper;
			rtw.Dispose();
		}

		public unsafe RenderTarget CreateRenderTarget(int w, int h)
		{
			TextureWrapper tw = new TextureWrapper();
			tw.SDBitmap = new Bitmap(w,h, sdi.PixelFormat.Format32bppArgb);
			var tex = new Texture2d(this, tw, w, h);

			RenderTargetWrapper rtw = new RenderTargetWrapper(this);
			var rt = new RenderTarget(this, rtw, tex);
			rtw.Target = rt;
			return rt;
		}

		public void BindRenderTarget(RenderTarget rt)
		{
			if (_CurrentOffscreenGraphics != null)
			{
				_CurrentOffscreenGraphics.Dispose();
				_CurrentOffscreenGraphics = null;
			}

			_CurrRenderTarget = rt;
			if (CurrentRenderTargetWrapper != null)
			{
				if (CurrentRenderTargetWrapper == CurrentControl.RenderTargetWrapper)
				{
					//dont do anything til swapbuffers
				}
				else
				{
					//CurrentRenderTargetWrapper.MyBufferedGraphics.Render();
				}
			}

			if (rt == null)
			{
				//null means to use the default RT for the current control
				CurrentRenderTargetWrapper = CurrentControl.RenderTargetWrapper;
			}
			else
			{
				var tw = rt.Texture2d.Opaque as TextureWrapper;
				CurrentRenderTargetWrapper = rt.Opaque as RenderTargetWrapper;
				_CurrentOffscreenGraphics = Graphics.FromImage(tw.SDBitmap);
				//if (CurrentRenderTargetWrapper.MyBufferedGraphics == null)
				//  CurrentRenderTargetWrapper.CreateGraphics();
			}
		}

		Graphics _CurrentOffscreenGraphics;

		public Graphics GetCurrentGraphics()
		{
			if (_CurrentOffscreenGraphics != null)
				return _CurrentOffscreenGraphics;
			var rtw = CurrentRenderTargetWrapper;
			return rtw.MyBufferedGraphics.Graphics;
		}

		public GLControlWrapper_GdiPlus CurrentControl;
		public RenderTargetWrapper CurrentRenderTargetWrapper;

		public BufferedGraphicsContext MyBufferedGraphicsContext;

		public class TextureWrapper : IDisposable
		{
			public sd.Bitmap SDBitmap;
			public TextureMinFilter MinFilter = TextureMinFilter.Nearest;
			public TextureMagFilter MagFilter = TextureMagFilter.Nearest;
			public void Dispose()
			{
				if (SDBitmap != null)
				{
					SDBitmap.Dispose();
					SDBitmap = null;
				}
			}
		}


	} //class IGL_GdiPlus

}
