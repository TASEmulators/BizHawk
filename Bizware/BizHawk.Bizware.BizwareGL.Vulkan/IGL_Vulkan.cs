using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common.CollectionExtensions;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

using NativeWindow = OpenTK.NativeWindow;
using OTKG = OpenTK.Graphics;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace BizHawk.Bizware.BizwareGL.Drivers.Vulkan
{
	/// <summary>
	/// Vulkan implementation of the BizwareGL.IGL interface.
	/// </summary>
	public class IGL_Vulkan : IGL
	{
		private Pipeline _CurrPipeline;
		private RenderTarget _CurrRenderTarget;

		static IGL_Vulkan()
		{
			var toolkitOptions = ToolkitOptions.Default;
			toolkitOptions.Backend = PlatformBackend.PreferNative;
			Toolkit.Init(toolkitOptions);
		}

		public string API => "Vulkan";

		private GLControlWrapper_Vulkan _glc;

		public IGL_Vulkan()
		{
			_offscreenNativeWindow = new NativeWindow { ClientSize = new Size(8, 8) };
			this._graphicsContext = new OTKG.GraphicsContext(OTKG.GraphicsMode.Default, _offscreenNativeWindow.WindowInfo, 0, 0, OTKG.GraphicsContextFlags.Default);
			MakeDefaultCurrent();
			this._graphicsContext.LoadAll();
			CreateRenderStates();
			PurgeStateCache();
		}

		public void BeginScene()
		{
		}

		public void EndScene()
		{
		}

		void IDisposable.Dispose()
		{
			_offscreenNativeWindow.Dispose();
			_offscreenNativeWindow = null;
			_graphicsContext.Dispose();
			_graphicsContext = null;
		}

		public void Clear(ClearBufferMask mask)
		{
			GL.Clear(mask);
		}

		public void SetClearColor(Color color)
		{
			GL.ClearColor(color);
		}

		public IGraphicsControl Internal_CreateGraphicsControl()
		{
			_glc = new GLControlWrapper_Vulkan(this);
			_glc.CreateControl();
			MakeDefaultCurrent();
			return _glc;
		}

		private static int GenTexture() => GL.GenTexture();

		public void FreeTexture(Texture2d tex)
		{
			GL.DeleteTexture((int)tex.Opaque);
		}

		public Shader CreateFragmentShader(bool cg, string source, string entry, bool required) =>
			CreateShader(cg, ShaderType.FragmentShader, source, entry, required);

		public Shader CreateVertexShader(bool cg, string source, string entry, bool required) =>
			CreateShader(cg, ShaderType.VertexShader, source, entry, required);

		public IBlendState CreateBlendState(BlendingFactorSrc colorSource, BlendEquationMode colorEquation, BlendingFactorDest colorDest, BlendingFactorSrc alphaSource, BlendEquationMode alphaEquation, BlendingFactorDest alphaDest) =>
			new CacheBlendState( true, colorSource, colorEquation, colorDest, alphaSource, alphaEquation, alphaDest);

		public void SetBlendState(IBlendState rsBlend)
		{
			var mybs = rsBlend as CacheBlendState;
			if (mybs == null) throw new InvalidCastException("could not set state: rsBlend is not a CacheBlendState");
			if (mybs.Enabled)
			{
				GL.Enable(EnableCap.Blend);
				GL.BlendEquationSeparate(mybs.colorEquation, mybs.alphaEquation);
				GL.BlendFuncSeparate(mybs.colorSource, mybs.colorDest, mybs.alphaSource, mybs.alphaDest);
			}
			else GL.Disable(EnableCap.Blend);

			if (rsBlend == _rsBlendNoneOpaque)
			{
				GL.BlendColor(new OTKG.Color4(255, 255, 255, 255));
			}
		}

		public IBlendState BlendNoneCopy => _rsBlendNoneVerbatim;

		public IBlendState BlendNoneOpaque => _rsBlendNoneOpaque;

		public IBlendState BlendNormal => _rsBlendNormal;

		private class ShaderWrapper
		{
			public int sid;
			public Dictionary<string, string> MapCodeToNative;
		}

		private class PipelineWrapper
		{
			public int pid;
			public Shader FragmentShader, VertexShader;
			public List<int> SamplerLocs;
		}

		public Pipeline CreatePipeline(VertexLayout vertexLayout, Shader vertexShader, Shader fragmentShader, bool required, string memo)
		{
			if (!vertexShader.Available || !fragmentShader.Available)
			{
				var errors = $"Vertex Shader:\r\n {vertexShader.Errors} \r\n-------\r\nFragment Shader:\r\n{fragmentShader.Errors}";
				if (required) throw new InvalidOperationException($"Couldn't build required GL pipeline:\r\n{errors}");
				return new Pipeline(this, null, false, null, null, null) { Errors = errors };
			}

			var success = true;
			var vsw = vertexShader.Opaque as ShaderWrapper;
			if (vsw == null) throw new InvalidCastException("could not wrap vertex shader");
			var fsw = fragmentShader.Opaque as ShaderWrapper;
			if (fsw == null) throw new InvalidCastException("could not wrap fragment shader");
			var pid = GL.CreateProgram();
			GL.AttachShader(pid, vsw.sid);
			GL.GetError();
			GL.AttachShader(pid, fsw.sid);
			GL.GetError();
			GL.LinkProgram(pid);
			var errcode = GL.GetError();
			var resultLog = GL.GetProgramInfoLog(pid);
			if (errcode != ErrorCode.NoError)
			{
				if (required) throw new InvalidOperationException( $"Error creating pipeline (error returned from glLinkProgram): {errcode}\r\n\r\n{resultLog}");
				success = false;
			}

			int linkStatus;
			GL.GetProgram(pid, GetProgramParameterName.LinkStatus, out linkStatus);
			if (linkStatus == 0)
			{
				if (required) throw new InvalidOperationException( $"Error creating pipeline (link status false returned from glLinkProgram): \r\n\r\n{resultLog}");
				success = false;
			}

			GL.UseProgram(pid);
			int nAttributes;
			GL.GetProgram(pid, GetProgramParameterName.ActiveAttributes, out nAttributes);
			for (var i = 0; i < nAttributes; i++)
			{
				int size, length;
				string name;
				ActiveAttribType type;
				GL.GetActiveAttrib(pid, i, 1024, out length, out size, out type, out name);
			}

			var uniforms = new List<UniformInfo>();
			int nUniforms;
			GL.GetProgram(pid, GetProgramParameterName.ActiveUniforms, out nUniforms);
			var samplers = new List<int>();
			for (var i = 0; i < nUniforms; i++)
			{
				int size, length;
				ActiveUniformType type;
				string name;
				GL.GetActiveUniform(pid, i, 1024, out length, out size, out type, out name);
				GL.GetError();
				var loc = GL.GetUniformLocation(pid, name);
				var ui = new UniformInfo {
					Name = fsw.MapCodeToNative?.GetOrDefault(name)
						?? vsw.MapCodeToNative?.GetOrDefault(name)
						?? name,
					Opaque = loc
				};
				if (type == ActiveUniformType.Sampler2D)
				{
					ui.IsSampler = true;
					ui.SamplerIndex = samplers.Count;
					ui.Opaque = loc | (samplers.Count << 24);
					samplers.Add(loc);
				}

				uniforms.Add(ui);
			}

			GL.UseProgram(0);
			if (!vertexShader.Available) success = false;
			if (!fragmentShader.Available) success = false;
			var pw = new PipelineWrapper { pid = pid, VertexShader = vertexShader, FragmentShader = fragmentShader, SamplerLocs = samplers };
			return new Pipeline(this, pw, success, vertexLayout, uniforms, memo);
		}

		public void FreePipeline(Pipeline pipeline)
		{
			var pw = pipeline.Opaque as PipelineWrapper;
			if (pw == null)
				return;
			GL.DeleteProgram(pw.pid);
			pw.FragmentShader.Release();
			pw.VertexShader.Release();
		}

		public void Internal_FreeShader(Shader shader)
		{
			var sw = shader.Opaque as ShaderWrapper;
			if (sw == null) throw new InvalidCastException("could not wrap shader");
			GL.DeleteShader(sw.sid);
		}

		public void BindPipeline(Pipeline pipeline)
		{
			_CurrPipeline = pipeline;
			if (pipeline == null)
			{
				_sStatePendingVertexLayout = null;
				GL.UseProgram(0);
				return;
			}

			if (!pipeline.Available) throw new InvalidOperationException("Attempt to bind unavailable pipeline");
			_sStatePendingVertexLayout = pipeline.VertexLayout;
			var pw = pipeline.Opaque as PipelineWrapper;
			if (pw == null) throw new InvalidCastException("could not wrap pipeline");
			GL.UseProgram(pw.pid);
			for (int i = 0; i < pw.SamplerLocs.Count; i++) GL.Uniform1(pw.SamplerLocs[i], i);
		}

		public VertexLayout CreateVertexLayout() => new VertexLayout(this, null);

		private void BindTexture2d(Texture2d tex)
		{
			GL.BindTexture(TextureTarget.Texture2D, (int)tex.Opaque);
		}

		public void SetTextureWrapMode(Texture2d tex, bool clamp)
		{
			BindTexture2d(tex);
			int mode;
			if (clamp)
			{
				mode = (int)TextureWrapMode.ClampToEdge;
			}
			else
				mode = (int)TextureWrapMode.Repeat;

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, mode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, mode);
		}

		public unsafe void BindArrayData(void* pData)
		{
			MyBindArrayData(_sStatePendingVertexLayout, pData);
		}

		public void DrawArrays(PrimitiveType mode, int first, int count)
		{
			GL.DrawArrays(mode, first, count);
		}

		public void SetPipelineUniform(PipelineUniform uniform, bool value)
		{
			GL.Uniform1((int)uniform.Sole.Opaque, value ? 1 : 0);
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4 mat, bool transpose)
		{
			GL.Uniform4((int)uniform.Sole.Opaque + 0, 1, (float*)&mat.Row0);
			GL.Uniform4((int)uniform.Sole.Opaque + 1, 1, (float*)&mat.Row1);
			GL.Uniform4((int)uniform.Sole.Opaque + 2, 1, (float*)&mat.Row2);
			GL.Uniform4((int)uniform.Sole.Opaque + 3, 1, (float*)&mat.Row3);
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4 mat, bool transpose)
		{
			fixed (Matrix4* pMat = &mat)
				GL.UniformMatrix4((int)uniform.Sole.Opaque, 1, transpose, (float*)pMat);
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector4 value)
		{
			GL.Uniform4((int)uniform.Sole.Opaque, value.X, value.Y, value.Z, value.W);
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector2 value)
		{
			GL.Uniform2((int)uniform.Sole.Opaque, value.X, value.Y);
		}

		public void SetPipelineUniform(PipelineUniform uniform, float value)
		{
			if (uniform.Owner == null) return;
			GL.Uniform1((int)uniform.Sole.Opaque, value);
		}

		public unsafe void SetPipelineUniform(PipelineUniform uniform, Vector4[] values)
		{
			fixed (Vector4* pValues = &values[0])
				GL.Uniform4((int)uniform.Sole.Opaque, values.Length, (float*)pValues);
		}

		public void SetPipelineUniformSampler(PipelineUniform uniform, Texture2d tex)
		{
			int n = ((int)uniform.Sole.Opaque) >> 24;
			if (_sActiveTexture != n)
			{
				_sActiveTexture = n;
				var selectedUnit = (TextureUnit)((int)TextureUnit.Texture0 + n);
				GL.ActiveTexture(selectedUnit);
			}

			GL.BindTexture(TextureTarget.Texture2D, (int)tex.Opaque);
		}

		public void TexParameter2d(Texture2d tex, TextureParameterName pname, int param)
		{
			BindTexture2d(tex);
			GL.TexParameter(TextureTarget.Texture2D, pname, param);
		}

		public Texture2d LoadTexture(Bitmap bitmap)
		{
			using (var bmp = new BitmapBuffer(bitmap, new BitmapLoadOptions()))
				return (this as IGL).LoadTexture(bmp);
		}

		public Texture2d LoadTexture(Stream stream)
		{
			using (var bmp = new BitmapBuffer(stream, new BitmapLoadOptions()))
				return (this as IGL).LoadTexture(bmp);
		}

		public Texture2d CreateTexture(int width, int height) => new Texture2d(this, GenTexture(), width, height);

		public Texture2d WrapGLTexture2d(IntPtr glTexId, int width, int height) =>
			new Texture2d(this, glTexId.ToInt32(), width, height);

		public void LoadTextureData(Texture2d tex, BitmapBuffer bmp)
		{
			BitmapData bmpData = bmp.LockBits();
			try
			{
				GL.BindTexture(TextureTarget.Texture2D, (int)tex.Opaque);
				GL.TexSubImage2D(
					TextureTarget.Texture2D,
					0,
					0,
					0,
					bmp.Width,
					bmp.Height,
					PixelFormat.Bgra,
					PixelType.UnsignedByte,
					bmpData.Scan0);
			}
			finally
			{
				bmp.UnlockBits(bmpData);
			}
		}

		public void FreeRenderTarget(RenderTarget rt)
		{
			rt.Texture2d.Dispose();
			GL.Ext.DeleteFramebuffer((int)rt.Opaque);
		}

		public unsafe RenderTarget CreateRenderTarget(int w, int h)
		{
			int texid = GenTexture();
			Texture2d tex = new Texture2d(this, texid, w, h);
			GL.BindTexture(TextureTarget.Texture2D, texid);
			GL.TexImage2D(
				TextureTarget.Texture2D,
				0,
				PixelInternalFormat.Rgba8,
				w,
				h,
				0,
				PixelFormat.Bgra,
				PixelType.UnsignedByte,
				IntPtr.Zero);
			tex.SetMagFilter(TextureMagFilter.Nearest);
			tex.SetMinFilter(TextureMinFilter.Nearest);
			int fbid = GL.Ext.GenFramebuffer();
			GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, fbid);
			GL.Ext.FramebufferTexture2D(
				FramebufferTarget.Framebuffer,
				FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D,
				texid,
				0);
			DrawBuffersEnum* buffers = stackalloc DrawBuffersEnum[1];
			buffers[0] = DrawBuffersEnum.ColorAttachment0;
			GL.DrawBuffers(1, buffers);
			if (GL.Ext.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
				throw new InvalidOperationException("Error creating framebuffer (at CheckFramebufferStatus)");
			GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			return new RenderTarget(this, fbid, tex);
		}

		public void BindRenderTarget(RenderTarget rt)
		{
			_CurrRenderTarget = rt;
			if (rt == null)
				GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			else
				GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, (int)rt.Opaque);
		}

		public Texture2d LoadTexture(BitmapBuffer bmp)
		{
			Texture2d ret;
			int id = GenTexture();
			try
			{
				ret = new Texture2d(this, id, bmp.Width, bmp.Height);
				GL.BindTexture(TextureTarget.Texture2D, id);
				GL.TexImage2D(
					TextureTarget.Texture2D,
					0,
					PixelInternalFormat.Rgba,
					bmp.Width,
					bmp.Height,
					0,
					PixelFormat.Bgra,
					PixelType.UnsignedByte,
					IntPtr.Zero);
				(this as IGL).LoadTextureData(ret, bmp);
			}
			catch
			{
				GL.DeleteTexture(id);
				throw;
			}

			ret.SetFilterNearest();
			return ret;
		}

		public BitmapBuffer ResolveTexture2d(Texture2d tex)
		{
			BindTexture2d(tex);
			var bb = new BitmapBuffer(tex.IntWidth, tex.IntHeight);
			var bmpdata = bb.LockBits();
			GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmpdata.Scan0);
			GL.GetError();
			bb.UnlockBits(bmpdata);
			return bb;
		}

		public Texture2d LoadTexture(string path)
		{
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				return (this as IGL).LoadTexture(fs);
		}

		public Matrix4 CreateGuiProjectionMatrix(int w, int h) => CreateGuiProjectionMatrix(new Size(w, h));

		public Matrix4 CreateGuiViewMatrix(int w, int h, bool autoflip) => CreateGuiViewMatrix(new Size(w, h), autoflip);

		public Matrix4 CreateGuiProjectionMatrix(Size dims)
		{
			Matrix4 ret = Matrix4.Identity;
			ret.M11 = 2.0f / dims.Width;
			ret.M22 = 2.0f / dims.Height;
			return ret;
		}

		public Matrix4 CreateGuiViewMatrix(Size dims, bool autoflip)
		{
			Matrix4 ret = Matrix4.Identity;
			ret.M22 = -1.0f;
			ret.M41 = -(float)dims.Width * 0.5f;
			ret.M42 = dims.Height * 0.5f;
			if (autoflip)
			{
				if (_CurrRenderTarget == null)
				{
				}
				else
				{
					ret.M22 = 1.0f;
					ret.M42 *= -1;
				}
			}

			return ret;
		}

		public void SetViewport(int x, int y, int width, int height)
		{
			GL.Viewport(x, y, width, height);
			GL.Scissor(x, y, width, height);
		}

		public void SetViewport(int width, int height)
		{
			SetViewport(0, 0, width, height);
		}

		public void SetViewport(Size size)
		{
			SetViewport(size.Width, size.Height);
		}

		public void SetViewport(Control control)
		{
			var r = control.ClientRectangle;
			SetViewport(r.Left, r.Top, r.Width, r.Height);
		}

		INativeWindow _offscreenNativeWindow;
		OTKG.IGraphicsContext _graphicsContext;

		Shader CreateShader(bool cg, ShaderType type, string source, string entry, bool required)
		{
			var sw = new ShaderWrapper();
			if (cg)
			{
				var cgc = new CGC();
				var results = cgc.Run(source, entry, type == ShaderType.FragmentShader ? "glslf" : "glslv", false);
				if (!results.Succeeded)
				{
					Console.WriteLine("CGC failed");
					Console.WriteLine(results.Errors);
					return new Shader(this, null, false);
				}

				source = results.Code;
				sw.MapCodeToNative = results.MapCodeToNative;
			}

			int sid = GL.CreateShader(type);
			bool ok = CompileShaderSimple(sid, source, required);
			if (!ok)
			{
				GL.DeleteShader(sid);
				sid = 0;
			}

			sw.sid = sid;
			return new Shader(this, sw, ok);
		}

		bool CompileShaderSimple(int sid, string source, bool required)
		{
			bool success = true;
			var errcode = GL.GetError();
			if (errcode != ErrorCode.NoError)
			{
				if (required) throw new InvalidOperationException($"Error compiling shader (from previous operation) {errcode}");
				success = false;
			}

			GL.ShaderSource(sid, source);

			errcode = GL.GetError();
			if (errcode != ErrorCode.NoError)
			{
				if (required) throw new InvalidOperationException($"Error compiling shader (ShaderSource) {errcode}");
				success = false;
			}

			GL.CompileShader(sid);
			errcode = GL.GetError();
			string resultLog = GL.GetShaderInfoLog(sid);
			if (errcode != ErrorCode.NoError)
			{
				string message = $"Error compiling shader (CompileShader) {errcode}\r\n\r\n{resultLog}";
				if (required) throw new InvalidOperationException(message);
				Console.WriteLine(message);
				success = false;
			}

			int n;
			GL.GetShader(sid, ShaderParameter.CompileStatus, out n);
			if (n == 0)
			{
				if (required) throw new InvalidOperationException( $"Error compiling shader (CompileShader )\r\n\r\n{resultLog}");
				success = false;
			}

			return success;
		}

		void UnbindVertexAttributes()
		{
			var currBindings = _sVertexAttribEnables;
			foreach (var index in currBindings) GL.DisableVertexAttribArray(index);
			currBindings.Clear();
		}

		unsafe void MyBindArrayData(VertexLayout layout, void* pData)
		{
			UnbindVertexAttributes();
			var currBindings = _sVertexAttribEnables;
			if (layout == null) return;
			GL.DisableClientState(ArrayCap.VertexArray);
			GL.DisableClientState(ArrayCap.ColorArray);
			for (int i = 0; i < 8; i++) GL.DisableVertexAttribArray(i);
			for (int i = 0; i < 8; i++)
			{
				GL.ClientActiveTexture(TextureUnit.Texture0 + i);
				GL.DisableClientState(ArrayCap.TextureCoordArray);
			}

			GL.ClientActiveTexture(TextureUnit.Texture0);
			foreach (var kvp in layout.Items)
			{
				if (_CurrPipeline.Memo == "gui")
				{
					GL.VertexAttribPointer(
						kvp.Key,
						kvp.Value.Components,
						kvp.Value.AttribType,
						kvp.Value.Normalized,
						kvp.Value.Stride,
						new IntPtr(pData) + kvp.Value.Offset);
					GL.EnableVertexAttribArray(kvp.Key);
					currBindings.Add(kvp.Key);
				}
				else
				{
					switch (kvp.Value.Usage)
					{
						case AttributeUsage.Position:
							GL.EnableClientState(ArrayCap.VertexArray);
							GL.VertexPointer(
								kvp.Value.Components,
								VertexPointerType.Float,
								kvp.Value.Stride,
								new IntPtr(pData) + kvp.Value.Offset);
							break;
						case AttributeUsage.Texcoord0:
							GL.ClientActiveTexture(TextureUnit.Texture0);
							GL.EnableClientState(ArrayCap.TextureCoordArray);
							GL.TexCoordPointer(
								kvp.Value.Components,
								TexCoordPointerType.Float,
								kvp.Value.Stride,
								new IntPtr(pData) + kvp.Value.Offset);
							break;
						case AttributeUsage.Texcoord1:
							GL.ClientActiveTexture(TextureUnit.Texture1);
							GL.EnableClientState(ArrayCap.TextureCoordArray);
							GL.TexCoordPointer(
								kvp.Value.Components,
								TexCoordPointerType.Float,
								kvp.Value.Stride,
								new IntPtr(pData) + kvp.Value.Offset);
							GL.ClientActiveTexture(TextureUnit.Texture0);
							break;
						case AttributeUsage.Color0:
							break;
					}
				}
			}
		}

		public void MakeDefaultCurrent()
		{
			MakeContextCurrent(this._graphicsContext, _offscreenNativeWindow.WindowInfo);
		}

		private void MakeContextCurrent(OTKG.IGraphicsContext context, IWindowInfo windowInfo)
		{
			context.MakeCurrent(windowInfo);
			PurgeStateCache();
		}

		private void CreateRenderStates()
		{
			_rsBlendNoneVerbatim = new CacheBlendState(
				false,
				BlendingFactorSrc.One,
				BlendEquationMode.FuncAdd,
				BlendingFactorDest.Zero,
				BlendingFactorSrc.One,
				BlendEquationMode.FuncAdd,
				BlendingFactorDest.Zero);
			_rsBlendNoneOpaque = new CacheBlendState(
				false,
				BlendingFactorSrc.One,
				BlendEquationMode.FuncAdd,
				BlendingFactorDest.Zero,
				BlendingFactorSrc.ConstantAlpha,
				BlendEquationMode.FuncAdd,
				BlendingFactorDest.Zero);
			_rsBlendNormal = new CacheBlendState(
				true,
				BlendingFactorSrc.SrcAlpha,
				BlendEquationMode.FuncAdd,
				BlendingFactorDest.OneMinusSrcAlpha,
				BlendingFactorSrc.One,
				BlendEquationMode.FuncAdd,
				BlendingFactorDest.Zero);
		}

		private CacheBlendState _rsBlendNoneVerbatim, _rsBlendNoneOpaque, _rsBlendNormal;
		private int _sActiveTexture;
		private VertexLayout _sStatePendingVertexLayout;
		private readonly HashSet<int> _sVertexAttribEnables = new HashSet<int>();

		private void PurgeStateCache()
		{
			_sStatePendingVertexLayout = null;
			_sVertexAttribEnables.Clear();
			_sActiveTexture = -1;
		}
	}
}