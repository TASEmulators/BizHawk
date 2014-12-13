using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using swf = System.Windows.Forms;
using sd = System.Drawing;
using sdi = System.Drawing.Imaging;

using BizHawk.Bizware.BizwareGL;

using SlimDX.Direct3D9;
using d3d9=SlimDX.Direct3D9;
using OpenTK;
using OpenTK.Graphics;
using gl=OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL.Drivers.SlimDX
{

	public class IGL_SlimDX9 : IGL
	{
		static Direct3D d3d;
		private Device dev;
		INativeWindow OffscreenNativeWindow;

		public string API { get { return "D3D9"; } }

		public IGL_SlimDX9()
		{
			if (d3d == null)
			{
				d3d = new Direct3D();
			}

			//make an 'offscreen context' so we can at least do things without having to create a window
			OffscreenNativeWindow = new OpenTK.NativeWindow();
			OffscreenNativeWindow.ClientSize = new sd.Size(8, 8);

			CreateDevice();
		}

		private void DestroyDevice()
		{
			if (dev != null)
			{
				dev.Dispose();
				dev = null;
			}
		}

		public void CreateDevice()
		{
			DestroyDevice();

			//just create some present params so we can get the device created
			var pp = new PresentParameters
				{
					BackBufferWidth = 8,
					BackBufferHeight = 8,
					DeviceWindowHandle = OffscreenNativeWindow.WindowInfo.Handle,
					PresentationInterval = PresentInterval.Immediate			
				};

			var flags = CreateFlags.SoftwareVertexProcessing;
			if ((d3d.GetDeviceCaps(0, DeviceType.Hardware).DeviceCaps & DeviceCaps.HWTransformAndLight) != 0)
			{
				flags = CreateFlags.HardwareVertexProcessing;
			}
			dev = new Device(d3d, 0, DeviceType.Hardware, pp.DeviceWindowHandle, flags, pp);
		}

		void IDisposable.Dispose()
		{
		}

		public void Clear(OpenTK.Graphics.OpenGL.ClearBufferMask mask)
		{



		}

		public IBlendState CreateBlendState(gl.BlendingFactorSrc colorSource, gl.BlendEquationMode colorEquation, gl.BlendingFactorDest colorDest,
					gl.BlendingFactorSrc alphaSource, gl.BlendEquationMode alphaEquation, gl.BlendingFactorDest alphaDest)
		{
			return null;
		}

		public void SetClearColor(sd.Color color)
		{

		}

		public unsafe void BindArrayData(void* pData)
		{
		}



		public void FreeTexture(Texture2d tex) {
			var dtex = tex.Opaque as d3d9.Texture;
			dtex.Dispose();
		}
		public IntPtr GetEmptyHandle() { return new IntPtr(0); }
		public IntPtr GetEmptyUniformHandle() { return new IntPtr(-1); }

		class ShaderWrapper
		{
			public d3d9.ConstantTable ct;
			public d3d9.VertexShader vs;
			public d3d9.PixelShader ps;
		}

		public Shader CreateFragmentShader(string source, bool required)
		{
			ShaderWrapper sw = new ShaderWrapper();
			string errors;
			using (var bytecode = d3d9.ShaderBytecode.Compile(source, null, null, "psmain", "ps_2_0", ShaderFlags.None, out errors))
			{
				sw.ct = bytecode.ConstantTable;
				sw.ps = new PixelShader(dev, bytecode);
			}

			Shader s = new Shader(this, IntPtr.Zero, true);
			s.Opaque = sw;
			return s;
		}

		public Shader CreateVertexShader(string source, bool required)
		{
			ShaderWrapper sw = new ShaderWrapper();
			string errors;
			using (var bytecode = d3d9.ShaderBytecode.Compile(source, null, null, "vsmain", "vs_2_0", ShaderFlags.None, out errors))
			{
				sw.ct = bytecode.ConstantTable;
				sw.vs = new VertexShader(dev, bytecode);
			}

			Shader s = new Shader(this, IntPtr.Zero, true);
			s.Opaque = sw;
			return s;
		}

		public void FreeShader(IntPtr shader) { }

		public void SetBlendState(IBlendState rsBlend)
		{
			//TODO for real
		}

		class MyBlendState : IBlendState { }
		static MyBlendState _rsBlendNoneOpaque = new MyBlendState(), _rsBlendNoneVerbatim = new MyBlendState(), _rsBlendNormal = new MyBlendState();

		public IBlendState BlendNoneCopy { get { return _rsBlendNoneVerbatim; } }
		public IBlendState BlendNoneOpaque { get { return _rsBlendNoneOpaque; } }
		public IBlendState BlendNormal { get { return _rsBlendNormal; } }

		public Pipeline CreatePipeline(VertexLayout vertexLayout, Shader vertexShader, Shader fragmentShader, bool required)
		{
			VertexElement[] ves = new VertexElement[vertexLayout.Items.Count];
			foreach (var kvp in vertexLayout.Items)
			{
				var item = kvp.Value;
				d3d9.DeclarationType decltype = DeclarationType.Float1;
				switch (item.AttribType)
				{
					case gl.VertexAttribPointerType.Float:
						if (item.Components == 1) decltype = DeclarationType.Float1;
						else if (item.Components == 2) decltype = DeclarationType.Float2;
						else if (item.Components == 3) decltype = DeclarationType.Float3;
						else if (item.Components == 4) decltype = DeclarationType.Float4;
						else throw new NotSupportedException();
						break;
					default:
						throw new NotSupportedException();
				}

				d3d9.DeclarationUsage usage = DeclarationUsage.Position;
				byte usageIndex = 0;
				switch(item.Usage)
				{
					case AttributeUsage.Position: 
						usage = DeclarationUsage.Position; 
						break;
					case AttributeUsage.Texcoord0: 
						usage = DeclarationUsage.TextureCoordinate;
						break;
					case AttributeUsage.Texcoord1: 
						usage = DeclarationUsage.TextureCoordinate;
						usageIndex = 1;
						break;
					case AttributeUsage.Color0:
						usage = DeclarationUsage.Color;
						break;
					default:
						throw new NotSupportedException();
				}

				ves[kvp.Key] = new VertexElement(0, (short)item.Offset, decltype, DeclarationMethod.Default, usage, usageIndex);
			}

			var pw = new PipelineWrapper();
			pw.VertexDeclaration = new VertexDeclaration(dev, ves);
			
			Pipeline pipeline = new Pipeline(this,IntPtr.Zero,true, vertexLayout, new List<UniformInfo>());
			pipeline.Opaque = pw;

			return pipeline;
		}

		class PipelineWrapper
		{
			public d3d9.VertexDeclaration VertexDeclaration;
		}

		public VertexLayout CreateVertexLayout() { return new VertexLayout(this, new IntPtr(0)); }

		public void BindTexture2d(Texture2d tex)
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

		public void TexParameter2d(gl.TextureParameterName pname, int param)
		{
			//if (CurrentBoundTexture == null)
			//  return;

			//TextureWrapper tw = TextureWrapperForTexture(CurrentBoundTexture);
			//if (pname == TextureParameterName.TextureMinFilter)
			//  tw.MinFilter = (TextureMinFilter)param;
			//if (pname == TextureParameterName.TextureMagFilter)
			//  tw.MagFilter = (TextureMagFilter)param;
		}

		public Texture2d LoadTexture(sd.Bitmap bitmap)
		{
			using (var bmp = new BitmapBuffer(bitmap, new BitmapLoadOptions()))
				return (this as IGL).LoadTexture(bmp);
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
			sdi.BitmapData bmp_data = bmp.LockBits();
			d3d9.Texture dtex = tex.Opaque as d3d9.Texture;
			var dr = dtex.LockRectangle(0, LockFlags.None);
			
			//TODO - do we need to handle odd sizes, weird pitches here?
			dr.Data.WriteRange(bmp_data.Scan0, bmp.Width * bmp.Height);
			dtex.UnlockRectangle(0);
			bmp.UnlockBits(bmp_data);
		}


		public Texture2d LoadTexture(BitmapBuffer bmp)
		{
			var tex = new d3d9.Texture(dev, bmp.Width, bmp.Height, 1, d3d9.Usage.None, d3d9.Format.A8R8G8B8, d3d9.Pool.Managed);
			var ret = new Texture2d(this, IntPtr.Zero, tex, bmp.Width, bmp.Height);
			LoadTextureData(ret, bmp);
			return ret;
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


		public void BeginControl(GLControlWrapper_SlimDX9 control)
		{
			
		}

		public void EndControl(GLControlWrapper_SlimDX9 control)
		{
		
		}

		public void SwapControl(GLControlWrapper_SlimDX9 control)
		{
		}
		
		public void FreeRenderTarget(RenderTarget rt)
		{
			//int id = rt.Id.ToInt32();
			//var rtw = ResourceIDs.Lookup[id] as RenderTargetWrapper;
			//rtw.Target.Dispose();
			//ResourceIDs.Free(rt.Id);
		}

		public unsafe RenderTarget CreateRenderTarget(int w, int h)
		{
			//Texture2d tex = null;
			//var rt = new RenderTarget(this, ResourceIDs.Alloc(ResourceIdManager.EResourceType.RenderTarget), tex);
			//int id = rt.Id.ToInt32();
			//RenderTargetWrapper rtw = new RenderTargetWrapper(this);
			//rtw.Target = rt;
			//ResourceIDs.Lookup[id] = rtw;
			//return rt;
			return null;
		}

		public void BindRenderTarget(RenderTarget rt)
		{
			//if (rt == null)
			//{
			//  //null means to use the default RT for the current control
			//  CurrentRenderTargetWrapper = CurrentControl.RenderTargetWrapper;
			//}
			//else
			//{
			//  CurrentRenderTargetWrapper = RenderTargetWrapperForRt(rt);
			//}
		}

		public void RefreshControlSwapChain(GLControlWrapper_SlimDX9 control)
		{
			if (control.SwapChain != null)
			{
				control.SwapChain.Dispose();
				control.SwapChain = null;
			}
			
			var pp = new PresentParameters
			{
				BackBufferWidth = Math.Max(8,control.ClientSize.Width),
				BackBufferHeight = Math.Max(8, control.ClientSize.Height),
				BackBufferCount = 2,
				BackBufferFormat = Format.X8R8G8B8,
				DeviceWindowHandle = control.Handle,
				Windowed = true,
				PresentationInterval = control.Vsync ? PresentInterval.One : PresentInterval.Immediate
			};

			control.SwapChain = new SwapChain(dev, pp);
		}

		public IGraphicsControl Internal_CreateGraphicsControl()
		{
			var ret = new GLControlWrapper_SlimDX9(this);
			RefreshControlSwapChain(ret);
			return ret;
		}

		public void DrawArrays(gl.PrimitiveType mode, int first, int count)
		{

		}
	
	} //class IGL_SlimDX

}
