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
	public class ResourceIdManager
	{
		int Last = 1;
		Queue<int> Available = new Queue<int>();

		public Dictionary<int, object> Lookup = new Dictionary<int, object>();

		public enum EResourceType
		{
			Texture,
			RenderTarget
		}

		public IntPtr Alloc(EResourceType type)
		{
			if (Available.Count == 0)
			{
				return new IntPtr(Last++);
			}
			else return new IntPtr(Available.Dequeue());
		}

		public void Free(IntPtr handle)
		{
			int n = handle.ToInt32();
			object o;
			if (Lookup.TryGetValue(n, out o))
			{
				if (o is IDisposable)
				{
					((IDisposable)o).Dispose();
				}
				Lookup.Remove(n);
			}
			Available.Enqueue(n);
		}
	}

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

	public class IGL_GdiPlus : IGL
	{
		public IGL_GdiPlus()
		{
			MyBufferedGraphicsContext = new BufferedGraphicsContext();
		}

		void IDisposable.Dispose()
		{
		}

		public void Clear(OpenTK.Graphics.OpenGL.ClearBufferMask mask)
		{
		}

		public string API { get { return "GDIPLUS"; } }

		public IBlendState CreateBlendState(BlendingFactorSrc colorSource, BlendEquationMode colorEquation, BlendingFactorDest colorDest,
					BlendingFactorSrc alphaSource, BlendEquationMode alphaEquation, BlendingFactorDest alphaDest)
		{
			return null;
		}

		public void SetClearColor(sd.Color color)
		{
	
		}
		
		public unsafe void BindArrayData(void* pData)
		{
		}


		public IntPtr GenTexture() { return ResourceIDs.Alloc(ResourceIdManager.EResourceType.Texture); }
		public void FreeTexture(Texture2d tex)
		{
			ResourceIDs.Free(tex.Id);
		}
		public IntPtr GetEmptyHandle() { return new IntPtr(0); }
		public IntPtr GetEmptyUniformHandle() { return new IntPtr(-1); }

		
		public Shader CreateFragmentShader(string source, bool required)
		{
			return null;
		}
		public Shader CreateVertexShader(string source, bool required)
		{
			return null;
		}

		public void FreeShader(IntPtr shader) {  }

		public void SetBlendState(IBlendState rsBlend)
		{
			//TODO for real
		}

		class MyBlendState : IBlendState { }
		static MyBlendState _rsBlendNone = new MyBlendState(), _rsBlendNormal = new MyBlendState();

		public IBlendState BlendNone { get { return _rsBlendNone; } }
		public IBlendState BlendNormal { get { return _rsBlendNormal; } }

		public Pipeline CreatePipeline(VertexLayout vertexLayout, Shader vertexShader, Shader fragmentShader, bool required)
		{
			return null;
		}

		public VertexLayout CreateVertexLayout() { return new VertexLayout(this, new IntPtr(0)); }

		public void BindTexture2d(Texture2d tex)
		{
			CurrentBoundTexture = tex;
		}

		public void SetTextureWrapMode(Texture2d tex, bool clamp)
		{
			if (CurrentBoundTexture == null)
				throw new InvalidOperationException();
		}

		public void DrawArrays(PrimitiveType mode, int first, int count)
		{
			
		}

		public void BindPipeline(Pipeline pipeline)
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

		public void SetPipelineUniformSampler(PipelineUniform uniform, IntPtr texHandle)
		{
	
		}

		public void TexParameter2d(TextureParameterName pname, int param)
		{
			if (CurrentBoundTexture == null)
				return;

			TextureWrapper tw = TextureWrapperForTexture(CurrentBoundTexture);
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
			IntPtr id = GenTexture();
			ResourceIDs.Lookup[id.ToInt32()] = tw;
			return new Texture2d(this, id, null, bitmap.Width, bitmap.Height);
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
			bmp.ToSysdrawingBitmap(BitmapForTexture(tex));
		}


		public Texture2d LoadTexture(BitmapBuffer bmp)
		{
			//definitely needed (by TextureFrugalizer at least)
			var sdbmp = bmp.ToSysdrawingBitmap();
			IntPtr id = GenTexture();
			var tw = new TextureWrapper();
			tw.SDBitmap = sdbmp;
			ResourceIDs.Lookup[id.ToInt32()] = tw;
			return new Texture2d(this, id, null, bmp.Width, bmp.Height);
		}

		public unsafe BitmapBuffer ResolveTexture2d(Texture2d tex)
		{
			//todo
			return null;
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

		public Matrix4 CreateGuiViewMatrix(int w, int h)
		{
			return CreateGuiViewMatrix(new sd.Size(w, h));
		}

		public Matrix4 CreateGuiProjectionMatrix(sd.Size dims)
		{
			Matrix4 ret = Matrix4.Identity;
			ret.M11 = 2.0f / (float)dims.Width;
			ret.M22 = 2.0f / (float)dims.Height;
			return ret;
		}

		public Matrix4 CreateGuiViewMatrix(sd.Size dims)
		{
			Matrix4 ret = Matrix4.Identity;
			ret.M22 = -1.0f;
			ret.M41 = -(float)dims.Width * 0.5f; // -0.5f;
			ret.M42 = (float)dims.Height * 0.5f; // +0.5f;
			return ret;
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

			public void CreateGraphics()
			{
				Graphics refGraphics;
				Rectangle r;
				if (Control != null)
				{
					r = Control.ClientRectangle;
					refGraphics = Control.CreateGraphics();
				}
				else
				{
					r = Target.Texture2d.Rectangle;
					refGraphics = Graphics.FromImage(Gdi.BitmapForTexture(Target.Texture2d));
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
			int id = rt.Id.ToInt32();
			var rtw = ResourceIDs.Lookup[id] as RenderTargetWrapper;
			rtw.Target.Dispose();
			ResourceIDs.Free(rt.Id);
		}

		public unsafe RenderTarget CreateRenderTarget(int w, int h)
		{
			Texture2d tex = null;
			var rt = new RenderTarget(this, ResourceIDs.Alloc(ResourceIdManager.EResourceType.RenderTarget), tex);
			int id = rt.Id.ToInt32();
			RenderTargetWrapper rtw = new RenderTargetWrapper(this);
			rtw.Target = rt;
			ResourceIDs.Lookup[id] = rtw;
			return rt;
		}

		public void BindRenderTarget(RenderTarget rt)
		{
			if (rt == null)
			{
				//null means to use the default RT for the current control
				CurrentRenderTargetWrapper = CurrentControl.RenderTargetWrapper;
			}
			else
			{
				CurrentRenderTargetWrapper = RenderTargetWrapperForRt(rt);
			}
		}

		public sd.Bitmap BitmapForTexture(Texture2d tex)
		{
			return TextureWrapperForTexture(tex).SDBitmap;
		}

		public TextureWrapper TextureWrapperForTexture(Texture2d tex)
		{
			return ResourceIDs.Lookup[tex.Id.ToInt32()] as TextureWrapper;
		}

		public RenderTargetWrapper RenderTargetWrapperForRt(RenderTarget rt)
		{
			return ResourceIDs.Lookup[rt.Id.ToInt32()] as RenderTargetWrapper;
		}

		public Graphics GetCurrentGraphics()
		{
			var rtw = CurrentRenderTargetWrapper;
			return rtw.MyBufferedGraphics.Graphics;
		}

		public GLControlWrapper_GdiPlus CurrentControl;
		public RenderTargetWrapper CurrentRenderTargetWrapper;
		Texture2d CurrentBoundTexture;

		//todo - not thread safe
		public static ResourceIdManager ResourceIDs = new ResourceIdManager();

		public BufferedGraphicsContext MyBufferedGraphicsContext;


	} //class IGL_GdiPlus

}
