//regarding binding and vertex arrays:
//http://stackoverflow.com/questions/8704801/glvertexattribpointer-clarification
//http://stackoverflow.com/questions/9536973/oes-vertex-array-object-and-client-state
//http://www.opengl.org/wiki/Vertex_Specification

//etc
//glBindAttribLocation (programID, 0, "vertexPosition_modelspace");

//for future reference: c# tesselators
//http://www.opentk.com/node/437 (AGG#, codes on Tao forums)

using System;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using sd = System.Drawing;
using sdi = System.Drawing.Imaging;
using swf=System.Windows.Forms;

using BizHawk.Bizware.BizwareGL;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL.Drivers.OpenTK
{
	/// <summary>
	/// OpenTK implementation of the BizwareGL.IGL interface. 
	/// TODO - can we have more than one of these? could be dangerous. such dangerous things to be possibly reconsidered are marked with HAMNUTS
	/// TODO - if we have any way of making contexts, we also need a way of freeing it, and then we can cleanup our dictionaries
	/// </summary>
	public class IGL_TK : IGL
	{
		static IGL_TK()
		{
			//make sure OpenTK initializes without getting wrecked on the SDL check and throwing an exception to annoy our MDA's
			var toolkitOptions = global::OpenTK.ToolkitOptions.Default;
			toolkitOptions.Backend = PlatformBackend.PreferNative;
			global::OpenTK.Toolkit.Init(toolkitOptions);
			//NOTE: this throws EGL exceptions anyway. I'm going to ignore it and whine about it later
		}

		public string API { get { return "OPENGL"; } }

		public int Version
		{
			get
			{
				//doesnt work on older than gl3 maybe
				//int major, minor;
				////other overloads may not exist...
				//GL.GetInteger(GetPName.MajorVersion, out major);
				//GL.GetInteger(GetPName.MinorVersion, out minor);

				//supposedly the standard dictates that whatever junk is in the version string, some kind of version is at the beginning
				string version_string = GL.GetString(StringName.Version);
				var version_parts = version_string.Split('.');
				int major = int.Parse(version_parts[0]);
				//getting a minor version out is too hard and not needed now
				return major * 100;
			}
		}

		public IGL_TK()
		{
			//make an 'offscreen context' so we can at least do things without having to create a window
			OffscreenNativeWindow = new NativeWindow();
			OffscreenNativeWindow.ClientSize = new sd.Size(8, 8);
			this.GraphicsContext = new GraphicsContext(GraphicsMode.Default, OffscreenNativeWindow.WindowInfo, 2, 0, GraphicsContextFlags.Default);
			MakeDefaultCurrent();

			//this is important for reasons unknown
			this.GraphicsContext.LoadAll(); 

			//misc initialization
			CreateRenderStates();
			PurgeStateCache();
		}

		void IDisposable.Dispose()
		{
			//TODO - a lot of analysis here
			OffscreenNativeWindow.Dispose(); OffscreenNativeWindow = null;
			GraphicsContext.Dispose(); GraphicsContext = null;
		}

		public void Clear(ClearBufferMask mask)
		{
			GL.Clear((global::OpenTK.Graphics.OpenGL.ClearBufferMask)mask);
		}
		public void SetClearColor(sd.Color color)
		{
			GL.ClearColor(color);
		}

		public IGraphicsControl Internal_CreateGraphicsControl()
		{
			var glc = new GLControlWrapper(this);
			glc.CreateControl();

			//now the control's context will be current. annoying! fix it.
			MakeDefaultCurrent();


			return glc;
		}

		public IntPtr GenTexture() { return new IntPtr(GL.GenTexture()); }
		public void FreeTexture(Texture2d tex)
		{
			GL.DeleteTexture(tex.Id.ToInt32());
		}
		public IntPtr GetEmptyHandle() { return new IntPtr(0); }
		public IntPtr GetEmptyUniformHandle() { return new IntPtr(-1); }

		
		public Shader CreateFragmentShader(string source, bool required)
		{
			return CreateShader(ShaderType.FragmentShader, source, required);
		}
		public Shader CreateVertexShader(string source, bool required)
		{
			return CreateShader(ShaderType.VertexShader, source, required);
		}

		public void FreeShader(IntPtr shader) { GL.DeleteShader(shader.ToInt32()); }

		class MyBlendState : IBlendState
		{
			public bool enabled;
			public global::OpenTK.Graphics.OpenGL.BlendingFactorSrc colorSource;
			public global::OpenTK.Graphics.OpenGL.BlendEquationMode colorEquation;
			public global::OpenTK.Graphics.OpenGL.BlendingFactorDest colorDest;
			public global::OpenTK.Graphics.OpenGL.BlendingFactorSrc alphaSource;
			public global::OpenTK.Graphics.OpenGL.BlendEquationMode alphaEquation;
			public global::OpenTK.Graphics.OpenGL.BlendingFactorDest alphaDest;
			public MyBlendState(bool enabled, BlendingFactorSrc colorSource, BlendEquationMode colorEquation, BlendingFactorDest colorDest,
				BlendingFactorSrc alphaSource, BlendEquationMode alphaEquation, BlendingFactorDest alphaDest)
			{
				this.enabled = enabled;
				this.colorSource = (global::OpenTK.Graphics.OpenGL.BlendingFactorSrc)colorSource;
				this.colorEquation = (global::OpenTK.Graphics.OpenGL.BlendEquationMode)colorEquation;
				this.colorDest = (global::OpenTK.Graphics.OpenGL.BlendingFactorDest)colorDest;
				this.alphaSource = (global::OpenTK.Graphics.OpenGL.BlendingFactorSrc)alphaSource;
				this.alphaEquation = (global::OpenTK.Graphics.OpenGL.BlendEquationMode)alphaEquation;
				this.alphaDest = (global::OpenTK.Graphics.OpenGL.BlendingFactorDest)alphaDest;
			}
		}
		public IBlendState CreateBlendState(BlendingFactorSrc colorSource, BlendEquationMode colorEquation, BlendingFactorDest colorDest,
			BlendingFactorSrc alphaSource, BlendEquationMode alphaEquation, BlendingFactorDest alphaDest)
		{
			return new MyBlendState(true, colorSource, colorEquation, colorDest, alphaSource, alphaEquation, alphaDest);
		}

		public void SetBlendState(IBlendState rsBlend)
		{
			var mybs = rsBlend as MyBlendState;
			if (mybs.enabled)
			{
				GL.Enable(EnableCap.Blend);
				GL.BlendEquationSeparate(mybs.colorEquation, mybs.alphaEquation);
				GL.BlendFuncSeparate(mybs.colorSource, mybs.colorDest, mybs.alphaSource, mybs.alphaDest);
			}
			else GL.Disable(EnableCap.Blend);
			if (rsBlend == _rsBlendNoneOpaque)
			{
				//make sure constant color is set correctly
				GL.BlendColor(new Color4(255, 255, 255, 255));
			}
		}

		public IBlendState BlendNoneCopy { get { return _rsBlendNoneVerbatim; } }
		public IBlendState BlendNoneOpaque { get { return _rsBlendNoneOpaque; } }
		public IBlendState BlendNormal { get { return _rsBlendNormal; } }

		public Pipeline CreatePipeline(VertexLayout vertexLayout, Shader vertexShader, Shader fragmentShader, bool required)
		{
			bool success = true;

			ErrorCode errcode;
			int pid = GL.CreateProgram();
			GL.AttachShader(pid, vertexShader.Id.ToInt32());
			errcode = GL.GetError();
			GL.AttachShader(pid, fragmentShader.Id.ToInt32());
			errcode = GL.GetError();

			//bind the attribute locations from the vertex layout
			foreach (var kvp in vertexLayout.Items)
				GL.BindAttribLocation(pid, kvp.Key, kvp.Value.Name);
			
			GL.LinkProgram(pid);
			errcode = GL.GetError();

			string resultLog = GL.GetProgramInfoLog(pid);

			if (errcode != ErrorCode.NoError)
				if (required)
					throw new InvalidOperationException("Error creating pipeline (error returned from glLinkProgram): " + errcode + "\r\n\r\n" + resultLog);
				else success = false;
			
			int linkStatus;
			GL.GetProgram(pid, GetProgramParameterName.LinkStatus, out linkStatus);
			if (linkStatus == 0)
				if (required)
					throw new InvalidOperationException("Error creating pipeline (link status false returned from glLinkProgram): " + "\r\n\r\n" + resultLog);
				else success = false;

			//need to work on validation. apparently there are some weird caveats to glValidate which make it complicated and possibly excuses (barely) the intel drivers' dysfunctional operation
			//"A sampler points to a texture unit used by fixed function with an incompatible target"
			//
			//info:
			//http://www.opengl.org/sdk/docs/man/xhtml/glValidateProgram.xml
			//This function mimics the validation operation that OpenGL implementations must perform when rendering commands are issued while programmable shaders are part of current state.
			//glValidateProgram checks to see whether the executables contained in program can execute given the current OpenGL state
			//This function is typically useful only during application development.
			//
			//So, this is no big deal. we shouldnt be calling validate right now anyway.
			//conclusion: glValidate is very complicated and is of virtually no use unless your draw calls are returning errors and you want to know why
			//GL.ValidateProgram(pid);
			//errcode = GL.GetError();
			//resultLog = GL.GetProgramInfoLog(pid);
			//if (errcode != ErrorCode.NoError)
			//  throw new InvalidOperationException("Error creating pipeline (error returned from glValidateProgram): " + errcode + "\r\n\r\n" + resultLog);
			//int validateStatus;
			//GL.GetProgram(pid, GetProgramParameterName.ValidateStatus, out validateStatus);
			//if (validateStatus == 0)
			//  throw new InvalidOperationException("Error creating pipeline (validateStatus status false returned from glValidateProgram): " + "\r\n\r\n" + resultLog);

			//set the program to active, in case we need to set sampler uniforms on it
			GL.UseProgram(pid);

			////get all the attributes (not needed)
			//List<AttributeInfo> attributes = new List<AttributeInfo>();
			//int nAttributes;
			//GL.GetProgram(pid, GetProgramParameterName.ActiveAttributes, out nAttributes);
			//for (int i = 0; i < nAttributes; i++)
			//{
			//  int size, length;
			//  var sbName = new System.Text.StringBuilder();
			//  ActiveAttribType type;
			//  GL.GetActiveAttrib(pid, i, 1024, out length, out size, out type, sbName);
			//  attributes.Add(new AttributeInfo() { Handle = new IntPtr(i), Name = sbName.ToString() });
			//}

			//get all the uniforms
			List<UniformInfo> uniforms = new List<UniformInfo>();
			int nUniforms;
			int nSamplers = 0;
			GL.GetProgram(pid,GetProgramParameterName.ActiveUniforms,out nUniforms);

			for (int i = 0; i < nUniforms; i++)
			{
				int size, length;
				ActiveUniformType type;
				var sbName = new System.Text.StringBuilder();
				GL.GetActiveUniform(pid, i, 1024, out length, out size, out type, sbName);
				errcode = GL.GetError();
				string name = sbName.ToString();
				int loc = GL.GetUniformLocation(pid, name);
				var ui = new UniformInfo();
				ui.Name = name;
				ui.Handle = new IntPtr(loc);

				//automatically assign sampler uniforms to texture units (and bind them)
				bool isSampler = (type == ActiveUniformType.Sampler2D);
				if (isSampler)
				{
					ui.SamplerIndex = nSamplers;
					GL.Uniform1(loc, nSamplers);
					nSamplers++;
				}

				uniforms.Add(ui);
			}

			//deactivate the program, so we dont accidentally use it
			GL.UseProgram(0);

			if (!vertexShader.Available) success = false;
			if (!fragmentShader.Available) success = false;

			return new Pipeline(this, new IntPtr(pid), success, vertexLayout, uniforms);
		}

		public VertexLayout CreateVertexLayout() { return new VertexLayout(this, new IntPtr(0)); }

		public void BindTexture2d(Texture2d tex)
		{
			GL.BindTexture(TextureTarget.Texture2D, tex.Id.ToInt32());
		}

		public void SetTextureWrapMode(Texture2d tex, bool clamp)
		{
			BindTexture2d(tex);
			int mode;
			if (clamp)
			{
				mode = (int)global::OpenTK.Graphics.OpenGL.TextureWrapMode.ClampToEdge;
			}
			else
				mode = (int)global::OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat;
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, mode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, mode);
		}

		public unsafe void BindArrayData(void* pData)
		{
			MyBindArrayData(sStatePendingVertexLayout, pData);
		}

		public void DrawArrays(PrimitiveType mode, int first, int count)
		{
			GL.DrawArrays((global::OpenTK.Graphics.OpenGL.PrimitiveType)mode, first, count);
		}

		public void BindPipeline(Pipeline pipeline)
		{
			if (pipeline == null)
			{
				sStatePendingVertexLayout = null;
				GL.UseProgram(0);
				return;
			}
			if (!pipeline.Available) throw new InvalidOperationException("Attempt to bind unavailable pipeline");
			sStatePendingVertexLayout = pipeline.VertexLayout;
			GL.UseProgram(pipeline.Id.ToInt32());
		}

		public void SetPipelineUniform(PipelineUniform uniform, bool value)
		{
			GL.Uniform1(uniform.Id.ToInt32(), value ? 1 : 0); 
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4 mat, bool transpose)
		{
			GL.UniformMatrix4(uniform.Id.ToInt32(), 1, transpose, (float*)&mat);
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4 mat, bool transpose)
		{
			fixed(Matrix4* pMat = &mat)
				GL.UniformMatrix4(uniform.Id.ToInt32(), 1, transpose, (float*)pMat);
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector4 value)
		{
			GL.Uniform4(uniform.Id.ToInt32(), value.X, value.Y, value.Z, value.W);
		}

		public void SetPipelineUniform(PipelineUniform uniform, Vector2 value)
		{
			GL.Uniform2(uniform.Id.ToInt32(), value.X, value.Y);
		}

		public void SetPipelineUniform(PipelineUniform uniform, float value)
		{
			GL.Uniform1(uniform.Id.ToInt32(), value);
		}

		public unsafe void SetPipelineUniform(PipelineUniform uniform, Vector4[] values)
		{
			fixed (Vector4* pValues = &values[0])
				GL.Uniform4(uniform.Id.ToInt32(), values.Length, (float*)pValues);
		}

		public void SetPipelineUniformSampler(PipelineUniform uniform, IntPtr texHandle)
		{
			//set the sampler index into the uniform first
			//now bind the texture
			if(sActiveTexture != uniform.SamplerIndex)
			{
				sActiveTexture = uniform.SamplerIndex;
				var selectedUnit = (TextureUnit)((int)TextureUnit.Texture0 + uniform.SamplerIndex);
				GL.ActiveTexture(selectedUnit);
			}
			GL.BindTexture(TextureTarget.Texture2D, texHandle.ToInt32());
		}

		public void TexParameter2d(TextureParameterName pname, int param)
		{
			GL.TexParameter(TextureTarget.Texture2D, (global::OpenTK.Graphics.OpenGL.TextureParameterName)pname, param);
		}

		public Texture2d LoadTexture(sd.Bitmap bitmap)
		{
			using (var bmp = new BitmapBuffer(bitmap, new BitmapLoadOptions()))
				return (this as IGL).LoadTexture(bmp);
		}

		public Texture2d LoadTexture(Stream stream)
		{
			using(var bmp = new BitmapBuffer(stream,new BitmapLoadOptions()))
				return (this as IGL).LoadTexture(bmp);
		}

		public Texture2d CreateTexture(int width, int height)
		{
			IntPtr id = GenTexture();
			return new Texture2d(this, id, null, width, height);
		}

		public Texture2d WrapGLTexture2d(IntPtr glTexId, int width, int height)
		{
			return new Texture2d(this as IGL, glTexId, null, width, height);
		}

		public void LoadTextureData(Texture2d tex, BitmapBuffer bmp)
		{
			sdi.BitmapData bmp_data = bmp.LockBits();
			try
			{
				GL.BindTexture(TextureTarget.Texture2D, tex.Id.ToInt32());
				GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, bmp.Width, bmp.Height, PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);
			}
			finally
			{
				bmp.UnlockBits(bmp_data);
			}
		}

		public void FreeRenderTarget(RenderTarget rt)
		{
			rt.Texture2d.Dispose();
			GL.Ext.DeleteFramebuffer(rt.Id.ToInt32());
		}

		public unsafe RenderTarget CreateRenderTarget(int w, int h)
		{
			//create a texture for it
			IntPtr texid = GenTexture();
			Texture2d tex = new Texture2d(this, texid, null, w, h);
			GL.BindTexture(TextureTarget.Texture2D,texid.ToInt32());
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, w, h, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
			tex.SetMagFilter(TextureMagFilter.Nearest);
			tex.SetMinFilter(TextureMinFilter.Nearest);

			//create the FBO
			int fbid = GL.Ext.GenFramebuffer();
			GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, fbid);

			//bind the tex to the FBO
			GL.Ext.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texid.ToInt32(), 0);

			//do something, I guess say which colorbuffers are used by the framebuffer
			DrawBuffersEnum* buffers = stackalloc DrawBuffersEnum[1];
			buffers[0] = DrawBuffersEnum.ColorAttachment0;
			GL.DrawBuffers(1, buffers);

			if (GL.Ext.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
				throw new InvalidOperationException("Error creating framebuffer (at CheckFramebufferStatus)");

			//since we're done configuring unbind this framebuffer, to return to the default
			GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			return new RenderTarget(this, new IntPtr(fbid), tex);
		}

		public void BindRenderTarget(RenderTarget rt)
		{
			if(rt == null)
				GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			else
				GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, rt.Id.ToInt32());
		}

		public Texture2d LoadTexture(BitmapBuffer bmp)
		{
			Texture2d ret = null;
			IntPtr id = GenTexture();
			try
			{
				ret = new Texture2d(this, id, null, bmp.Width, bmp.Height);
				GL.BindTexture(TextureTarget.Texture2D, id.ToInt32());
				//picking a color order that matches doesnt seem to help, any. maybe my driver is accelerating it, or maybe it isnt a big deal. but its something to study on another day
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
				(this as IGL).LoadTextureData(ret, bmp);
			}
			catch
			{
				GL.DeleteTexture(id.ToInt32());
				throw;
			}

			//set default filtering.. its safest to do this always
			ret.SetFilterNearest();

			return ret;
		}

		public unsafe BitmapBuffer ResolveTexture2d(Texture2d tex)
		{
			//note - this is dangerous since it changes the bound texture. could we save it?
			BindTexture2d(tex);
			var bb = new BitmapBuffer(tex.IntWidth, tex.IntHeight);
			var bmpdata = bb.LockBits();
			GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmpdata.Scan0);
			bb.UnlockBits(bmpdata);
			return bb;
		}

		public Texture2d LoadTexture(string path)
		{
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				return (this as IGL).LoadTexture(fs);
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
			GL.Viewport(x, y, width, height);
			GL.Scissor(x, y, width, height); //hack for mupen[rice]+intel: at least the rice plugin leaves the scissor rectangle scrambled, and we're trying to run it in the main graphics context for intel
		}

		public void SetViewport(int width, int height)
		{
			SetViewport(0, 0, width, height);
		}

		public void SetViewport(sd.Size size)
		{
			SetViewport(size.Width, size.Height);
		}

		public void SetViewport(swf.Control control)
		{
			var r = control.ClientRectangle;
			SetViewport(r.Left, r.Top, r.Width, r.Height);
		}

		//------------------

		INativeWindow OffscreenNativeWindow;
		IGraphicsContext GraphicsContext;

		//---------------
		//my utility methods

		GLControl CastControl(swf.Control swfControl)
		{
			GLControl glc = swfControl as GLControl;
			if (glc == null)
				throw new ArgumentException("Argument isn't a control created by the IGL interface", "glControl");
			return glc;
		}

		Shader CreateShader(ShaderType type, string source, bool required)
		{
			int sid = GL.CreateShader(type);
			bool ok = CompileShaderSimple(sid,source, required);
			if(!ok)
			{
				GL.DeleteShader(sid);
				sid = 0;
			}

			return new Shader(this, new IntPtr(sid), ok);
		}

		bool CompileShaderSimple(int sid, string source, bool required)
		{
			bool success = true;
			ErrorCode errcode;

			errcode = GL.GetError();
			if (errcode != ErrorCode.NoError)
				if (required)
					throw new InvalidOperationException("Error compiling shader (from previous operation) " + errcode);
				else success = false;

			GL.ShaderSource(sid, source);
			
			errcode = GL.GetError();
			if (errcode != ErrorCode.NoError)
				if (required)
					throw new InvalidOperationException("Error compiling shader (ShaderSource) " + errcode);
				else success = false;

			GL.CompileShader(sid);
			errcode = GL.GetError();

			string resultLog = GL.GetShaderInfoLog(sid);

			if (errcode != ErrorCode.NoError)
				if (required)
					throw new InvalidOperationException("Error compiling shader (CompileShader) " + errcode + "\r\n\r\n" + resultLog);
				else success = false;

			int n;
			GL.GetShader(sid, ShaderParameter.CompileStatus, out n);

			if (n == 0)
				if (required)
					throw new InvalidOperationException("Error compiling shader (CompileShader )" + "\r\n\r\n" + resultLog);
				else success = false;

			return success;
		}
	
		void UnbindVertexAttributes()
		{
			//HAMNUTS:
			//its not clear how many bindings we'll have to disable before we can enable the ones we need..
			//so lets just disable the ones we remember we have bound
			var currBindings = sVertexAttribEnables;
			foreach (var index in currBindings)
				GL.DisableVertexAttribArray(index);
			currBindings.Clear();
		}

		unsafe void MyBindArrayData(VertexLayout layout, void* pData)
		{
			UnbindVertexAttributes();

			//HAMNUTS (continued)
			var currBindings = sVertexAttribEnables;
			sStateCurrentVertexLayout = sStatePendingVertexLayout;

			if (layout == null) return;

			foreach (var kvp in layout.Items)
			{
				GL.VertexAttribPointer(kvp.Key, kvp.Value.Components, (VertexAttribPointerType)kvp.Value.AttribType, kvp.Value.Normalized, kvp.Value.Stride, new IntPtr(pData) + kvp.Value.Offset);
				GL.EnableVertexAttribArray(kvp.Key);
				currBindings.Add(kvp.Key);
			}
		}

		public void MakeDefaultCurrent()
		{
			MakeContextCurrent(this.GraphicsContext,OffscreenNativeWindow.WindowInfo);
		}

		internal void MakeContextCurrent(IGraphicsContext context, global::OpenTK.Platform.IWindowInfo windowInfo)
		{
			context.MakeCurrent(windowInfo);
			PurgeStateCache();
		}

		void CreateRenderStates()
		{
			_rsBlendNoneVerbatim = new MyBlendState(
				false, 
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero, 
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero);
			
			_rsBlendNoneOpaque = new MyBlendState(
				false, 
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero, 
				BlendingFactorSrc.ConstantAlpha, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero);
			
			_rsBlendNormal = new MyBlendState(
				true,
				BlendingFactorSrc.SrcAlpha, BlendEquationMode.FuncAdd, BlendingFactorDest.OneMinusSrcAlpha,
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero);
		}

		MyBlendState _rsBlendNoneVerbatim, _rsBlendNoneOpaque, _rsBlendNormal;

		//state caches
		int sActiveTexture;
		VertexLayout sStateCurrentVertexLayout;
		VertexLayout sStatePendingVertexLayout;
		HashSet<int> sVertexAttribEnables = new HashSet<int>();
		void PurgeStateCache()
		{
			sStateCurrentVertexLayout = null;
			sStatePendingVertexLayout = null;
			sVertexAttribEnables.Clear();
			sActiveTexture = -1;
		}
	
	} //class IGL_TK

}
