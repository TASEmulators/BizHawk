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
using otkg = OpenTK.Graphics;

namespace BizHawk.Bizware.BizwareGL.Drivers.Vulkan
{
	/// <summary>
	/// OpenTK implementation of the BizwareGL.IGL interface. 
	/// TODO - can we have more than one of these? could be dangerous. such dangerous things to be possibly reconsidered are marked with HAMNUTS
	/// TODO - if we have any way of making contexts, we also need a way of freeing it, and then we can cleanup our dictionaries
	/// </summary>
	public class IGL_Vulkan : IGL
	{
		//rendering state
		Pipeline _CurrPipeline;
		RenderTarget _CurrRenderTarget;

		static IGL_Vulkan()
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

		public IGL_Vulkan(int major_version = 0, int minor_version = 0, bool forward_compatible = false)
		{
			//make an 'offscreen context' so we can at least do things without having to create a window
			OffscreenNativeWindow = new NativeWindow();
			OffscreenNativeWindow.ClientSize = new sd.Size(8, 8);
			this.GraphicsContext = new GraphicsContext(GraphicsMode.Default, OffscreenNativeWindow.WindowInfo, major_version, minor_version, forward_compatible ? GraphicsContextFlags.ForwardCompatible : GraphicsContextFlags.Default);
			MakeDefaultCurrent();

			//this is important for reasons unknown
			this.GraphicsContext.LoadAll(); 

			//misc initialization
			CreateRenderStates();
			PurgeStateCache();
		}

		public void BeginScene()
		{
			//seems not to be needed...
		}

		public void EndScene()
		{
			//seems not to be needed...
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
			var glc = new GLControlWrapper_Vulkan(this);
			glc.CreateControl();

			//now the control's context will be current. annoying! fix it.
			MakeDefaultCurrent();


			return glc;
		}

		public int GenTexture() { return GL.GenTexture(); }
		public void FreeTexture(Texture2d tex)
		{
			GL.DeleteTexture((int)tex.Opaque);
		}

		public Shader CreateFragmentShader(bool cg, string source, string entry, bool required)
		{
			return CreateShader(cg, ShaderType.FragmentShader, source, entry, required);
		}
		public Shader CreateVertexShader(bool cg, string source, string entry, bool required)
		{
			return CreateShader(cg, ShaderType.VertexShader, source, entry, required);
		}

		public IBlendState CreateBlendState(BlendingFactorSrc colorSource, BlendEquationMode colorEquation, BlendingFactorDest colorDest,
			BlendingFactorSrc alphaSource, BlendEquationMode alphaEquation, BlendingFactorDest alphaDest)
		{
			return new CacheBlendState(true, colorSource, colorEquation, colorDest, alphaSource, alphaEquation, alphaDest);
		}

		public void SetBlendState(IBlendState rsBlend)
		{
			var mybs = rsBlend as CacheBlendState;
			if (mybs.Enabled)
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

		class ShaderWrapper
		{
			public int sid;
			public Dictionary<string, string> MapCodeToNative;
			public Dictionary<string, string> MapNativeToCode;
		}

		class PipelineWrapper
		{
			public int pid;
			public Shader FragmentShader, VertexShader;
			public List<int> SamplerLocs;
		}

		public Pipeline CreatePipeline(VertexLayout vertexLayout, Shader vertexShader, Shader fragmentShader, bool required, string memo)
		{
			//if the shaders arent available, the pipeline isn't either
			if (!vertexShader.Available || !fragmentShader.Available)
			{
				string errors = string.Format("Vertex Shader:\r\n {0} \r\n-------\r\nFragment Shader:\r\n{1}", vertexShader.Errors, fragmentShader.Errors);
				if (required)
					throw new InvalidOperationException("Couldn't build required GL pipeline:\r\n" + errors);
				var pipeline = new Pipeline(this, null, false, null, null, null);
				pipeline.Errors = errors;
				return pipeline;
			}

			bool success = true;

			var vsw = vertexShader.Opaque as ShaderWrapper;
			var fsw = fragmentShader.Opaque as ShaderWrapper;
			var sws = new[] { vsw,fsw };

			bool mapVariables = vsw.MapCodeToNative != null || fsw.MapCodeToNative != null;

			ErrorCode errcode;
			int pid = GL.CreateProgram();
			GL.AttachShader(pid, vsw.sid);
			errcode = GL.GetError();
			GL.AttachShader(pid, fsw.sid);
			errcode = GL.GetError();

			//NOT BEING USED NOW: USING SEMANTICS INSTEAD
			////bind the attribute locations from the vertex layout
			////as we go, look for attribute mappings (CGC will happily reorder and rename our attribute mappings)
			////what's more it will _RESIZE_ them but this seems benign..somehow..
			////WELLLLLLL we wish we could do that by names
			////but the shaders dont seem to be adequate quality (oddly named attributes.. texCoord vs texCoord1). need to use semantics instead.
			//foreach (var kvp in vertexLayout.Items)
			//{
			//  string name = kvp.Value.Name;
			//  //if (mapVariables)
			//  //{
			//  //  foreach (var sw in sws)
			//  //  {
			//  //    if (sw.MapNativeToCode.ContainsKey(name))
			//  //    {
			//  //      name = sw.MapNativeToCode[name];
			//  //      break;
			//  //    }
			//  //  }
			//  //}

			//  if(mapVariables) {
			//    ////proxy for came-from-cgc
			//    //switch (kvp.Value.Usage)
			//    //{
			//    //  case AttributeUsage.Position: 
			//    //}
			//  }
				
			//  //GL.BindAttribLocation(pid, kvp.Key, name);
			//}

			
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

			//get all the attributes (not needed)
			List<AttributeInfo> attributes = new List<AttributeInfo>();
			int nAttributes;
			GL.GetProgram(pid, GetProgramParameterName.ActiveAttributes, out nAttributes);
			for (int i = 0; i < nAttributes; i++)
			{
				int size, length;
				string name = new System.Text.StringBuilder(1024).ToString();
				ActiveAttribType type;
				GL.GetActiveAttrib(pid, i, 1024, out length, out size, out type, out name);
				attributes.Add(new AttributeInfo() { Handle = new IntPtr(i), Name = name });
			}

			//get all the uniforms
			List<UniformInfo> uniforms = new List<UniformInfo>();
			int nUniforms;
			GL.GetProgram(pid,GetProgramParameterName.ActiveUniforms,out nUniforms);
			List<int> samplers = new List<int>();

			for (int i = 0; i < nUniforms; i++)
			{
				int size, length;
				ActiveUniformType type;
				string name = new System.Text.StringBuilder(1024).ToString().ToString();
				GL.GetActiveUniform(pid, i, 1024, out length, out size, out type, out name);
				errcode = GL.GetError();
				int loc = GL.GetUniformLocation(pid, name);

				//translate name if appropriate
				//not sure how effective this approach will be, due to confusion of vertex and fragment uniforms
				if (mapVariables)
				{
					if (vsw.MapCodeToNative.ContainsKey(name)) name = vsw.MapCodeToNative[name];
					if (fsw.MapCodeToNative.ContainsKey(name)) name = fsw.MapCodeToNative[name];
				}

				var ui = new UniformInfo();
				ui.Name = name;
				ui.Opaque = loc;

				if (type == ActiveUniformType.Sampler2D)
				{
					ui.IsSampler = true;
					ui.SamplerIndex = samplers.Count;
					ui.Opaque = loc | (samplers.Count << 24);
					samplers.Add(loc);
				}

				uniforms.Add(ui);
			}

			//deactivate the program, so we dont accidentally use it
			GL.UseProgram(0);

			if (!vertexShader.Available) success = false;
			if (!fragmentShader.Available) success = false;

			var pw = new PipelineWrapper() { pid = pid, VertexShader = vertexShader, FragmentShader = fragmentShader, SamplerLocs = samplers };

			return new Pipeline(this, pw, success, vertexLayout, uniforms, memo);
		}

		public void FreePipeline(Pipeline pipeline)
		{
			var pw = pipeline.Opaque as PipelineWrapper;

			//unavailable pipelines will have no opaque
			if (pw == null)
				return;

			GL.DeleteProgram(pw.pid);

			pw.FragmentShader.Release();
			pw.VertexShader.Release();
		}

		public void Internal_FreeShader(Shader shader)
		{
			var sw = shader.Opaque as ShaderWrapper;
			GL.DeleteShader(sw.sid);
		}

		public void BindPipeline(Pipeline pipeline)
		{
			_CurrPipeline = pipeline;

			if (pipeline == null)
			{
				sStatePendingVertexLayout = null;
				GL.UseProgram(0);
				return;
			}

			if (!pipeline.Available) throw new InvalidOperationException("Attempt to bind unavailable pipeline");
			sStatePendingVertexLayout = pipeline.VertexLayout;

			var pw = pipeline.Opaque as PipelineWrapper;
			GL.UseProgram(pw.pid);

			//this is dumb and confusing, but we have to bind physical sampler numbers to sampler variables.
			for (int i = 0; i < pw.SamplerLocs.Count; i++)
			{
				GL.Uniform1(pw.SamplerLocs[i], i);
			}
		}


		public VertexLayout CreateVertexLayout() { return new VertexLayout(this, null); }

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

		public void SetPipelineUniform(PipelineUniform uniform, bool value)
		{
			GL.Uniform1((int)uniform.Sole.Opaque, value ? 1 : 0); 
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4 mat, bool transpose)
		{
			//GL.UniformMatrix4((int)uniform.Opaque, 1, transpose, (float*)&mat);
			GL.Uniform4((int)uniform.Sole.Opaque + 0, 1, (float*)&mat.Row0);
			GL.Uniform4((int)uniform.Sole.Opaque + 1, 1, (float*)&mat.Row1);
			GL.Uniform4((int)uniform.Sole.Opaque + 2, 1, (float*)&mat.Row2);
			GL.Uniform4((int)uniform.Sole.Opaque + 3, 1, (float*)&mat.Row3);
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4 mat, bool transpose)
		{
			fixed(Matrix4* pMat = &mat)
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
			if (uniform.Owner == null) return; //uniform was optimized out
			GL.Uniform1((int)uniform.Sole.Opaque, value);
		}

		public unsafe void SetPipelineUniform(PipelineUniform uniform, Vector4[] values)
		{
			fixed (Vector4* pValues = &values[0])
				GL.Uniform4((int)uniform.Sole.Opaque, values.Length, (float*)pValues);
		}

		public void SetPipelineUniformSampler(PipelineUniform uniform, Texture2d tex)
		{
			int n = ((int)uniform.Sole.Opaque)>>24;

			//set the sampler index into the uniform first
			if (sActiveTexture != n)
			{
				sActiveTexture = n;
				var selectedUnit = (TextureUnit)((int)TextureUnit.Texture0 + n);
				GL.ActiveTexture(selectedUnit);
			}

			//now bind the texture
			GL.BindTexture(TextureTarget.Texture2D, (int)tex.Opaque);
		}

		public void TexParameter2d(Texture2d tex, TextureParameterName pname, int param)
		{
			BindTexture2d(tex);
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
			int id = GenTexture();
			return new Texture2d(this, id, width, height);
		}

		public Texture2d WrapGLTexture2d(IntPtr glTexId, int width, int height)
		{
			return new Texture2d(this as IGL, glTexId.ToInt32(), width, height);
		}

		public void LoadTextureData(Texture2d tex, BitmapBuffer bmp)
		{
			sdi.BitmapData bmp_data = bmp.LockBits();
			try
			{
				GL.BindTexture(TextureTarget.Texture2D, (int)tex.Opaque);
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
			GL.Ext.DeleteFramebuffer((int)rt.Opaque);
		}

		public unsafe RenderTarget CreateRenderTarget(int w, int h)
		{
			//create a texture for it
			int texid = GenTexture();
			Texture2d tex = new Texture2d(this, texid, w, h);
			GL.BindTexture(TextureTarget.Texture2D,texid);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, w, h, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
			tex.SetMagFilter(TextureMagFilter.Nearest);
			tex.SetMinFilter(TextureMinFilter.Nearest);

			//create the FBO
			int fbid = GL.Ext.GenFramebuffer();
			GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, fbid);

			//bind the tex to the FBO
			GL.Ext.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texid, 0);

			//do something, I guess say which colorbuffers are used by the framebuffer
			DrawBuffersEnum* buffers = stackalloc DrawBuffersEnum[1];
			buffers[0] = DrawBuffersEnum.ColorAttachment0;
			GL.DrawBuffers(1, buffers);

			if (GL.Ext.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
				throw new InvalidOperationException("Error creating framebuffer (at CheckFramebufferStatus)");

			//since we're done configuring unbind this framebuffer, to return to the default
			GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			return new RenderTarget(this, fbid, tex);
		}

		public void BindRenderTarget(RenderTarget rt)
		{
			_CurrRenderTarget = rt;
			if(rt == null)
				GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			else
				GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, (int)rt.Opaque);
		}

		public Texture2d LoadTexture(BitmapBuffer bmp)
		{
			Texture2d ret = null;
			int id = GenTexture();
			try
			{
				ret = new Texture2d(this, id, bmp.Width, bmp.Height);
				GL.BindTexture(TextureTarget.Texture2D, id);
				//picking a color order that matches doesnt seem to help, any. maybe my driver is accelerating it, or maybe it isnt a big deal. but its something to study on another day
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
				(this as IGL).LoadTextureData(ret, bmp);
			}
			catch
			{
				GL.DeleteTexture(id);
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
			var err = GL.GetError();
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

		public Matrix4 CreateGuiViewMatrix(int w, int h, bool autoflip)
		{
			return CreateGuiViewMatrix(new sd.Size(w, h), autoflip);
		}

		public Matrix4 CreateGuiProjectionMatrix(sd.Size dims)
		{
			Matrix4 ret = Matrix4.Identity;
			ret.M11 = 2.0f / (float)dims.Width;
			ret.M22 = 2.0f / (float)dims.Height;
			return ret;
		}

		public Matrix4 CreateGuiViewMatrix(sd.Size dims, bool autoflip)
		{
			Matrix4 ret = Matrix4.Identity;
			ret.M22 = -1.0f;
			ret.M41 = -(float)dims.Width * 0.5f;
			ret.M42 = (float)dims.Height * 0.5f;
			if (autoflip)
			{
				if (_CurrRenderTarget == null) { }
				else
				{
					//flip as long as we're not a final render target
					ret.M22 = 1.0f;
					ret.M42 *= -1;
				}
			}
			return ret;
		}

		public void SetViewport(int x, int y, int width, int height)
		{
			GL.Viewport(x, y, width, height);
			GL.Scissor(x, y, width, height); //hack for mupen[rice]+intel: at least the rice plugin leaves the scissor rectangle scrambled, and we're trying to run it in the main graphics context for intel
			//BUT ALSO: new specifications.. viewport+scissor make sense together
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
				sw.MapNativeToCode = results.MapNativeToCode;
			}

			int sid = GL.CreateShader(type);
			bool ok = CompileShaderSimple(sid, source, required);
			if(!ok)
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
			{
				string message = "Error compiling shader (CompileShader) " + errcode + "\r\n\r\n" + resultLog;
				if (required)
					throw new InvalidOperationException(message);
				else
				{
					Console.WriteLine(message);
					success = false;
				}
			}

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

			//disable all the client states.. a lot of overhead right now, to be sure
			GL.DisableClientState(ArrayCap.VertexArray);
			GL.DisableClientState(ArrayCap.ColorArray);
			for(int i=0;i<8;i++)
				GL.DisableVertexAttribArray(i);
			for (int i = 0; i < 8; i++)
			{
				GL.ClientActiveTexture(TextureUnit.Texture0 + i);
				GL.DisableClientState(ArrayCap.TextureCoordArray);
			}
			GL.ClientActiveTexture(TextureUnit.Texture0);

			foreach (var kvp in layout.Items)
			{
				if(_CurrPipeline.Memo == "gui")
				{
					GL.VertexAttribPointer(kvp.Key, kvp.Value.Components, (VertexAttribPointerType)kvp.Value.AttribType, kvp.Value.Normalized, kvp.Value.Stride, new IntPtr(pData) + kvp.Value.Offset);
					GL.EnableVertexAttribArray(kvp.Key);
					currBindings.Add(kvp.Key);
				}
				else
				{

					var pw = _CurrPipeline.Opaque as PipelineWrapper;

					//comment SNACKPANTS
					switch (kvp.Value.Usage)
					{
						case AttributeUsage.Position:
							GL.EnableClientState(ArrayCap.VertexArray);
							GL.VertexPointer(kvp.Value.Components,VertexPointerType.Float,kvp.Value.Stride,new IntPtr(pData) + kvp.Value.Offset);
							break;
						case AttributeUsage.Texcoord0:
							GL.ClientActiveTexture(TextureUnit.Texture0);
							GL.EnableClientState(ArrayCap.TextureCoordArray);
							GL.TexCoordPointer(kvp.Value.Components, TexCoordPointerType.Float, kvp.Value.Stride, new IntPtr(pData) + kvp.Value.Offset);
							break;
						case AttributeUsage.Texcoord1:
							GL.ClientActiveTexture(TextureUnit.Texture1);
							GL.EnableClientState(ArrayCap.TextureCoordArray);
							GL.TexCoordPointer(kvp.Value.Components, TexCoordPointerType.Float, kvp.Value.Stride, new IntPtr(pData) + kvp.Value.Offset);
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
			MakeContextCurrent(this.GraphicsContext,OffscreenNativeWindow.WindowInfo);
		}

		internal void MakeContextCurrent(IGraphicsContext context, global::OpenTK.Platform.IWindowInfo windowInfo)
		{
			context.MakeCurrent(windowInfo);
			PurgeStateCache();
		}

		void CreateRenderStates()
		{
			_rsBlendNoneVerbatim = new CacheBlendState(
				false, 
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero, 
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero);

			_rsBlendNoneOpaque = new CacheBlendState(
				false, 
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero, 
				BlendingFactorSrc.ConstantAlpha, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero);

			_rsBlendNormal = new CacheBlendState(
				true,
				BlendingFactorSrc.SrcAlpha, BlendEquationMode.FuncAdd, BlendingFactorDest.OneMinusSrcAlpha,
				BlendingFactorSrc.One, BlendEquationMode.FuncAdd, BlendingFactorDest.Zero);
		}

		CacheBlendState _rsBlendNoneVerbatim, _rsBlendNoneOpaque, _rsBlendNormal;

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
	
	} //class IGL_Vulkan

}
