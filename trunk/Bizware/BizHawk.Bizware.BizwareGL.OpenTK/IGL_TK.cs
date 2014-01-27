//regarding binding and vertex arrays:
//http://stackoverflow.com/questions/8704801/glvertexattribpointer-clarification
//http://stackoverflow.com/questions/9536973/oes-vertex-array-object-and-client-state
//http://www.opengl.org/wiki/Vertex_Specification

//etc
//glBindAttribLocation (programID, 0, "vertexPosition_modelspace");

//for future reference: c# tesselators
//http://www.opentk.com/node/437 (AGG#, codes on Tao forums)

using System;
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
			OffscreenNativeWindow = new NativeWindow();
			OffscreenNativeWindow.ClientSize = new sd.Size(8, 8);
			this.GraphicsContext = new GraphicsContext(GraphicsMode.Default, OffscreenNativeWindow.WindowInfo, 2, 0, GraphicsContextFlags.Default);
			MakeDefaultCurrent();
			this.GraphicsContext.LoadAll(); //this is important for reasons unknown
			CreateRenderStates();
		}

		void IGL.Clear(ClearBufferMask mask)
		{
			GL.Clear((global::OpenTK.Graphics.OpenGL.ClearBufferMask)mask);
		}
		void IGL.ClearColor(sd.Color color)
		{
			GL.ClearColor(color);
		}

		class GLControlWrapper : GraphicsControl
		{
			//Note: In order to work around bugs in OpenTK which sometimes do things to a context without making that context active first...
			//we are going to push and pop the context before doing stuff

			public GLControlWrapper(IGL_TK owner)
			{
				Owner = owner;
			}

			IGL_TK Owner;

			public override swf.Control Control { get { return MyControl; } }

			public override void SetVsync(bool state)
			{
				//IGraphicsContext curr = global::OpenTK.Graphics.GraphicsContext.CurrentContext;
				MyControl.MakeCurrent();
					MyControl.VSync = state;
				//Owner.MakeContextCurrent(curr, Owner.NativeWindowsForContexts[curr]);
			}

			public override void Begin()
			{
				Owner.MakeContextCurrent(MyControl.Context, MyControl.WindowInfo);
			}

			public override void End()
			{
				//this slows things down too much
				//Owner.MakeDefaultCurrent();
			}

			public override void SwapBuffers()
			{
				//IGraphicsContext curr = global::OpenTK.Graphics.GraphicsContext.CurrentContext;
				MyControl.MakeCurrent();
					MyControl.SwapBuffers();
				//Owner.MakeContextCurrent(curr, Owner.NativeWindowsForContexts[curr]);
			}

			public override void Dispose()
			{
				//TODO - what happens if this context was current?
				MyControl.Dispose();
				MyControl = null;
			}

			public GLControl MyControl;
		}

		GraphicsControl IGL.CreateGraphicsControl()
		{
			var glc = new GLControl(GraphicsMode.Default, 2, 0, GraphicsContextFlags.Default);
			glc.CreateControl();

			//now the control's context will be current. annoying! fix it.
			MakeDefaultCurrent();

			GLControlWrapper wrapper = new GLControlWrapper(this);
			wrapper.MyControl = glc;
			return wrapper;
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
			public MyBlendState(bool enabled, BlendingFactor colorSource, BlendEquationMode colorEquation, BlendingFactor colorDest,
				BlendingFactor alphaSource, BlendEquationMode alphaEquation, BlendingFactor alphaDest)
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
		IBlendState IGL.CreateBlendState(BlendingFactor colorSource, BlendEquationMode colorEquation, BlendingFactor colorDest,
			BlendingFactor alphaSource, BlendEquationMode alphaEquation, BlendingFactor alphaDest)
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
				throw new InvalidOperationException("Error creating pipeline (link status false returned from glLinkProgram): " + errcode + "\r\n\r\n" + resultLog);

			GL.ValidateProgram(pid);
			errcode = GL.GetError();

			resultLog = GL.GetProgramInfoLog(pid);

			if (errcode != ErrorCode.NoError)
				throw new InvalidOperationException("Error creating pipeline (error returned from glValidateProgram): " + errcode + "\r\n\r\n" + resultLog);

			int validateStatus;
			GL.GetProgram(pid, GetProgramParameterName.ValidateStatus, out validateStatus);
			if (validateStatus == 0)
				throw new InvalidOperationException("Error creating pipeline (validateStatus status false returned from glValidateProgram): " + errcode + "\r\n\r\n" + resultLog);

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
			GL.Uniform1(uniform.Id.ToInt32(), uniform.SamplerIndex);
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
			ErrorCode errcode;
			GL.Viewport(x, y, width, height);
			errcode = GL.GetError();
		}

		void IGL.SetViewport(swf.Control control)
		{
			ErrorCode errcode;
			var r = control.ClientRectangle;
			errcode = GL.GetError();
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
			GL.ShaderSource(sid, source);
			ErrorCode errcode = GL.GetError();
			GL.CompileShader(sid);
			errcode = GL.GetError();
			int n;
			GL.GetShader(sid, ShaderParameter.CompileStatus, out n);
			string result = GL.GetShaderInfoLog(sid);
			if (result != "")
				throw new InvalidOperationException("Error compiling shader:\r\n\r\n" + result);
			
			//HAX???
			GL.Enable(EnableCap.Texture2D);
			//GL.PolygonMode(MaterialFace.Back, PolygonMode.Line); //??
			//GL.PolygonMode(MaterialFace.Front, PolygonMode.Line); //??
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
			ErrorCode errcode;

			UnbindVertexAttributes();

			//HAMNUTS (continued)
			var currBindings = VertexAttribEnables[CurrentContext];
			StateCurrentVertexLayouts[CurrentContext] = StatePendingVertexLayouts[CurrentContext];

			foreach (var kvp in layout.Items)
			{
				GL.VertexAttribPointer(kvp.Key, kvp.Value.Components, (VertexAttribPointerType)kvp.Value.AttribType, kvp.Value.Normalized, kvp.Value.Stride, new IntPtr(pData) + kvp.Value.Offset);
				errcode = GL.GetError();
				GL.EnableVertexAttribArray(kvp.Key);
				errcode = GL.GetError();
				currBindings.Add(kvp.Key);
			}
		}

		void MakeDefaultCurrent()
		{
			MakeContextCurrent(this.GraphicsContext, OffscreenNativeWindow.WindowInfo);
		}

		void MakeContextCurrent(IGraphicsContext context, global::OpenTK.Platform.IWindowInfo windowInfo)
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
			_rsBlendNone = new MyBlendState(false, BlendingFactor.One, BlendEquationMode.FuncAdd, BlendingFactor.Zero, BlendingFactor.One, BlendEquationMode.FuncAdd, BlendingFactor.Zero);
			_rsBlendNormal = new MyBlendState(true, 
				BlendingFactor.SrcAlpha, BlendEquationMode.FuncAdd, BlendingFactor.OneMinusSrcAlpha,
				BlendingFactor.One, BlendEquationMode.FuncAdd, BlendingFactor.Zero);
		}

		MyBlendState _rsBlendNone, _rsBlendNormal;
	
	} //class IGL_TK

}
