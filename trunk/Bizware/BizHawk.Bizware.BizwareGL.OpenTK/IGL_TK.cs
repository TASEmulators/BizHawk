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
			GL.Enable(EnableCap.Texture2D);
		}

		void IDisposable.Dispose()
		{
			//TODO - a lot of analysis here
			OffscreenNativeWindow.Dispose(); OffscreenNativeWindow = null;
			GraphicsContext.Dispose(); GraphicsContext = null;
		}

		void IGL.Clear(ClearBufferMask mask)
		{
			GL.Clear((global::OpenTK.Graphics.OpenGL.ClearBufferMask)mask);
		}
		void IGL.SetClearColor(sd.Color color)
		{
			GL.ClearColor(color);
		}

		IGraphicsControl IGL.Internal_CreateGraphicsControl()
		{
			var glc = new GLControlWrapper(this);
			glc.CreateControl();

			//now the control's context will be current. annoying! fix it.
			MakeDefaultCurrent();


			return glc;
		}

		IntPtr IGL.GenTexture() { return new IntPtr(GL.GenTexture()); }
		void IGL.FreeTexture(IntPtr texHandle) { GL.DeleteTexture(texHandle.ToInt32()); }
		IntPtr IGL.GetEmptyHandle() { return new IntPtr(0); }
		IntPtr IGL.GetEmptyUniformHandle() { return new IntPtr(-1); }

		Shader IGL.CreateFragmentShader(string source)
		{
			int sid = GL.CreateShader(ShaderType.FragmentShader);
			CompileShaderSimple(sid,source);
			return new Shader(this,new IntPtr(sid));
		}
		Shader IGL.CreateVertexShader(string source)
		{
			int sid = GL.CreateShader(ShaderType.VertexShader);
			CompileShaderSimple(sid, source);

			return new Shader(this, new IntPtr(sid));
		}

		void IGL.FreeShader(IntPtr shader) { GL.DeleteShader(shader.ToInt32()); }

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
		IBlendState IGL.CreateBlendState(BlendingFactorSrc colorSource, BlendEquationMode colorEquation, BlendingFactorDest colorDest,
			BlendingFactorSrc alphaSource, BlendEquationMode alphaEquation, BlendingFactorDest alphaDest)
		{
			return new MyBlendState(true, colorSource, colorEquation, colorDest, alphaSource, alphaEquation, alphaDest);
		}

		void IGL.SetBlendState(IBlendState rsBlend)
		{
			var mybs = rsBlend as MyBlendState;
			if (mybs.enabled)
			{
				GL.Enable(EnableCap.Blend);
				GL.BlendEquationSeparate(mybs.colorEquation, mybs.alphaEquation);
				GL.BlendFuncSeparate(mybs.colorSource, mybs.colorDest, mybs.alphaSource, mybs.alphaDest);
			}
			else GL.Disable(EnableCap.Blend);
		}

		IBlendState IGL.BlendNone { get { return _rsBlendNone; } }
		IBlendState IGL.BlendNormal { get { return _rsBlendNormal; } }

		Pipeline IGL.CreatePipeline(Shader vertexShader, Shader fragmentShader)
		{
			ErrorCode errcode;
			int pid = GL.CreateProgram();
			GL.AttachShader(pid, vertexShader.Id.ToInt32());
			errcode = GL.GetError();
			GL.AttachShader(pid, fragmentShader.Id.ToInt32());
			errcode = GL.GetError();
			
			GL.LinkProgram(pid);
			errcode = GL.GetError();

			string resultLog = GL.GetProgramInfoLog(pid);
			
			if (errcode != ErrorCode.NoError)
				throw new InvalidOperationException("Error creating pipeline (error returned from glLinkProgram): " + errcode + "\r\n\r\n" + resultLog);
			
			int linkStatus;
			GL.GetProgram(pid, GetProgramParameterName.LinkStatus, out linkStatus);
			if(linkStatus == 0)
				throw new InvalidOperationException("Error creating pipeline (link status false returned from glLinkProgram): " + "\r\n\r\n" + resultLog);

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

			return new Pipeline(this, new IntPtr(pid), uniforms);
		}

		VertexLayout IGL.CreateVertexLayout() { return new VertexLayout(this,new IntPtr(0)); }

		void IGL.BindTexture2d(Texture2d tex)
		{
			GL.BindTexture(TextureTarget.Texture2D, tex.Id.ToInt32());
		}

		unsafe void IGL.BindVertexLayout(VertexLayout layout)
		{
			StatePendingVertexLayouts[CurrentContext] = layout;
		}

		unsafe void IGL.BindArrayData(void* pData)
		{
			MyBindArrayData(StatePendingVertexLayouts[CurrentContext], pData);
		}

		void IGL.DrawArrays(PrimitiveType mode, int first, int count)
		{
			GL.DrawArrays((global::OpenTK.Graphics.OpenGL.PrimitiveType)mode, first, count);
		}

		void IGL.BindPipeline(Pipeline pipeline)
		{
			GL.UseProgram(pipeline.Id.ToInt32());
		}

		unsafe void IGL.SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4 mat, bool transpose)
		{
			GL.UniformMatrix4(uniform.Id.ToInt32(), 1, transpose, (float*)&mat);
		}

		unsafe void IGL.SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4 mat, bool transpose)
		{
			fixed(Matrix4* pMat = &mat)
				GL.UniformMatrix4(uniform.Id.ToInt32(), 1, transpose, (float*)pMat);
		}

		void IGL.SetPipelineUniform(PipelineUniform uniform, Vector4 value)
		{
			GL.Uniform4(uniform.Id.ToInt32(), value.X, value.Y, value.Z, value.W);
		}

		void IGL.SetPipelineUniformSampler(PipelineUniform uniform, IntPtr texHandle)
		{
			//set the sampler index into the uniform first
			//GL.Uniform1(uniform.Id.ToInt32(), uniform.SamplerIndex);
			//now bind the texture
			GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + uniform.SamplerIndex));
			GL.BindTexture(TextureTarget.Texture2D, texHandle.ToInt32());
		}

		void IGL.TexParameter2d(TextureParameterName pname, int param)
		{
			GL.TexParameter(TextureTarget.Texture2D, (global::OpenTK.Graphics.OpenGL.TextureParameterName)pname, param);
		}

		Texture2d IGL.LoadTexture(sd.Bitmap bitmap)
		{
			using (var bmp = new BitmapBuffer(bitmap, new BitmapLoadOptions()))
				return (this as IGL).LoadTexture(bmp);
		}

		Texture2d IGL.LoadTexture(Stream stream)
		{
			using(var bmp = new BitmapBuffer(stream,new BitmapLoadOptions()))
				return (this as IGL).LoadTexture(bmp);
		}

		Texture2d IGL.CreateTexture(int width, int height)
		{
			IntPtr id = (this as IGL).GenTexture();
			return new Texture2d(this, id, width, height);
		}

		void IGL.LoadTextureData(Texture2d tex, BitmapBuffer bmp)
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

		void IGL.FreeRenderTarget(RenderTarget rt)
		{
			rt.Texture2d.Dispose();
			GL.DeleteFramebuffer(rt.Id.ToInt32());
		}

		unsafe RenderTarget IGL.CreateRenderTarget(int w, int h)
		{
			//create a texture for it
			IntPtr texid = (this as IGL).GenTexture();
			Texture2d tex = new Texture2d(this, texid, w, h);
			GL.BindTexture(TextureTarget.Texture2D,texid.ToInt32());
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w,h,  0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
			tex.SetMagFilter(TextureMagFilter.Nearest);
			tex.SetMinFilter(TextureMinFilter.Nearest);

			//create the FBO
			int fbid = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbid);

			//bind the tex to the FBO
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texid.ToInt32(), 0);

			//do something, I guess say which colorbuffers are used by the framebuffer
			DrawBuffersEnum* buffers = stackalloc DrawBuffersEnum[1];
			buffers[0] = DrawBuffersEnum.ColorAttachment0;
			GL.DrawBuffers(1, buffers);

			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
				throw new InvalidOperationException("Error creating framebuffer (at CheckFramebufferStatus)");

			//since we're done configuring unbind this framebuffer, to return to the default
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			return new RenderTarget(this, new IntPtr(fbid), tex);
		}

		void IGL.BindRenderTarget(RenderTarget rt)
		{
			if(rt == null)
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			else
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, rt.Id.ToInt32());
		}

		Texture2d IGL.LoadTexture(BitmapBuffer bmp)
		{
			Texture2d ret = null;
			IntPtr id = (this as IGL).GenTexture();
			try
			{
				ret = new Texture2d(this, id, bmp.Width, bmp.Height);
				GL.BindTexture(TextureTarget.Texture2D, id.ToInt32());
				//picking a color order that matches doesnt seem to help, any. maybe my driver is accelerating it, or maybe it isnt a big deal. but its something to study on another day
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
				(this as IGL).LoadTextureData(ret, bmp);
			}
			catch
			{
				(this as IGL).FreeTexture(id);
				throw;
			}

			//set default filtering.. its safest to do this always
			ret.SetFilterNearest();

			return ret;
		}

		Texture2d IGL.LoadTexture(string path)
		{
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				return (this as IGL).LoadTexture(fs);
		}

		Matrix4 IGL.CreateGuiProjectionMatrix(int w, int h)
		{
			Matrix4 ret = Matrix4.Identity;
			ret.M11 = 2.0f / (float)w;
			ret.M22 = 2.0f / (float)h;
			return ret;
		}

		Matrix4 IGL.CreateGuiViewMatrix(int w, int h)
		{
			Matrix4 ret = Matrix4.Identity;
			ret.M22 = -1.0f;
			ret.M41 = -w * 0.5f; // -0.5f;
			ret.M42 = h * 0.5f; // +0.5f;
			return ret;
		}

		void IGL.SetViewport(int x, int y, int width, int height)
		{
			GL.Viewport(x, y, width, height);
		}

		void IGL.SetViewport(int width, int height)
		{
			GL.Viewport(0, 0, width, height);
		}

		void IGL.SetViewport(swf.Control control)
		{
			var r = control.ClientRectangle;
			GL.Viewport(r.Left, r.Top, r.Width, r.Height);
		}

		//------------------

		INativeWindow OffscreenNativeWindow;
		IGraphicsContext GraphicsContext;

		//---------------
		//my utility methods

		global::OpenTK.Graphics.IGraphicsContext CurrentContext { get { return global::OpenTK.Graphics.GraphicsContext.CurrentContext; } }

		GLControl CastControl(swf.Control swfControl)
		{
			GLControl glc = swfControl as GLControl;
			if (glc == null)
				throw new ArgumentException("Argument isn't a control created by the IGL interface", "glControl");
			return glc;
		}

		void CompileShaderSimple(int sid, string source)
		{
			ErrorCode errcode;

			GL.ShaderSource(sid, source);
			
			errcode = GL.GetError();
			if (errcode != ErrorCode.NoError)
				throw new InvalidOperationException("Error compiling shader (ShaderSource)" + errcode);

			GL.CompileShader(sid);
			errcode = GL.GetError();

			string resultLog = GL.GetShaderInfoLog(sid);

			if (errcode != ErrorCode.NoError)
				throw new InvalidOperationException("Error compiling shader (CompileShader)" + errcode + "\r\n\r\n" + resultLog);

			int n;
			GL.GetShader(sid, ShaderParameter.CompileStatus, out n);

			if(n==0)
				throw new InvalidOperationException("Error compiling shader (CompileShader)" + "\r\n\r\n" + resultLog);
		}

		Dictionary<IGraphicsContext, VertexLayout> StateCurrentVertexLayouts = new Dictionary<IGraphicsContext, VertexLayout>();
		Dictionary<IGraphicsContext, VertexLayout> StatePendingVertexLayouts = new Dictionary<IGraphicsContext, VertexLayout>();
		WorkingDictionary<IGraphicsContext, HashSet<int>> VertexAttribEnables = new WorkingDictionary<IGraphicsContext, HashSet<int>>();
		void UnbindVertexAttributes()
		{
			//HAMNUTS:
			//its not clear how many bindings we'll have to disable before we can enable the ones we need..
			//so lets just disable the ones we remember we have bound
			var currBindings = VertexAttribEnables[CurrentContext];
			foreach (var index in currBindings)
				GL.DisableVertexAttribArray(index);
			currBindings.Clear();
		}

		unsafe void MyBindArrayData(VertexLayout layout, void* pData)
		{
			UnbindVertexAttributes();

			//HAMNUTS (continued)
			var currBindings = VertexAttribEnables[CurrentContext];
			StateCurrentVertexLayouts[CurrentContext] = StatePendingVertexLayouts[CurrentContext];

			foreach (var kvp in layout.Items)
			{
				GL.VertexAttribPointer(kvp.Key, kvp.Value.Components, (VertexAttribPointerType)kvp.Value.AttribType, kvp.Value.Normalized, kvp.Value.Stride, new IntPtr(pData) + kvp.Value.Offset);
				GL.EnableVertexAttribArray(kvp.Key);
				currBindings.Add(kvp.Key);
			}
		}

		internal void MakeDefaultCurrent()
		{
			MakeContextCurrent(this.GraphicsContext, OffscreenNativeWindow.WindowInfo);
		}

		internal void MakeContextCurrent(IGraphicsContext context, global::OpenTK.Platform.IWindowInfo windowInfo)
		{
			//TODO - if we're churning through contexts quickly, this will sort of be a memory leak, since they'll be memoized forever in here
			//maybe make it a weakptr or something

			//dont do anything if we're already current
			IGraphicsContext currentForThread = null;
			if (ThreadsForContexts.TryGetValue(Thread.CurrentThread, out currentForThread))
				if (currentForThread == context)
					return;

			NativeWindowsForContexts[context] = windowInfo;
			context.MakeCurrent(windowInfo);
			ThreadsForContexts[Thread.CurrentThread] = context;
		}

		Dictionary<Thread, IGraphicsContext> ThreadsForContexts = new Dictionary<Thread, IGraphicsContext>();
		Dictionary<IGraphicsContext, global::OpenTK.Platform.IWindowInfo> NativeWindowsForContexts = new Dictionary<IGraphicsContext, global::OpenTK.Platform.IWindowInfo>();

		void CreateRenderStates()
		{
			_rsBlendNone = new MyBlendState(false, BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero, BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero);
			_rsBlendNormal = new MyBlendState(true,
				BlendingFactorSrc.SrcAlpha, BlendEquationMode.FuncAdd, BlendingFactorDest.OneMinusSrcAlpha,
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero);
		}

		MyBlendState _rsBlendNone, _rsBlendNormal;
	
	} //class IGL_TK

}
