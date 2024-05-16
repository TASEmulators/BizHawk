// regarding binding and vertex arrays:
// http://stackoverflow.com/questions/8704801/glvertexattribpointer-clarification
// http://stackoverflow.com/questions/9536973/oes-vertex-array-object-and-client-state
// http://www.opengl.org/wiki/Vertex_Specification

// etc
// glBindAttribLocation (programID, 0, "vertexPosition_modelspace");

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;

using BizHawk.Common;

using Silk.NET.OpenGL.Legacy;

using BizShader = BizHawk.Bizware.Graphics.Shader;

using GLVertexAttribPointerType = Silk.NET.OpenGL.Legacy.VertexAttribPointerType;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// OpenGL implementation of the BizwareGL.IGL interface
	/// </summary>
	public class IGL_OpenGL : IGL
	{
		public EDispMethod DispMethodEnum => EDispMethod.OpenGL;

		private readonly GL GL;

		// rendering state
		private Pipeline _currPipeline;
		private RenderTarget _currRenderTarget;

		// this IGL either requires at least OpenGL 3.0
		public static bool Available => OpenGLVersion.SupportsVersion(3, 0);

		public IGL_OpenGL()
		{
			if (!Available)
			{
				throw new InvalidOperationException("OpenGL 3.0 is required and unavailable");
			}

			GL = GL.GetApi(SDL2OpenGLContext.GetGLProcAddress);
		}

		public void Dispose()
		{
			GL.Dispose();
		}

		public void ClearColor(Color color)
		{
			GL.ClearColor(color);
			GL.Clear(ClearBufferMask.ColorBufferBit);
		}

		public void FreeTexture(Texture2d tex)
		{
			GL.DeleteTexture((uint)tex.Opaque);
		}

		public BizShader CreateFragmentShader(string source, string entry, bool required)
		{
			return CreateShader(ShaderType.FragmentShader, source, required);
		}

		public BizShader CreateVertexShader(string source, string entry, bool required)
		{
			return CreateShader(ShaderType.VertexShader, source, required);
		}

		public void EnableBlending()
		{
			GL.Enable(EnableCap.Blend);
			GL.BlendEquation(GLEnum.FuncAdd);
			GL.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.Zero);
		}

		public void DisableBlending()
			=> GL.Disable(EnableCap.Blend);

		private class ShaderWrapper
		{
			public uint SID;
		}

		private class PipelineWrapper
		{
			public uint PID;
			public BizShader FragmentShader, VertexShader;
			public List<int> SamplerLocs;
		}

		/// <exception cref="InvalidOperationException">
		/// <paramref name="required"/> is <see langword="true"/> and either <paramref name="vertexShader"/> or <paramref name="fragmentShader"/> is unavailable (their <see cref="BizShader.Available"/> property is <see langword="false"/>), or
		/// <c>glLinkProgram</c> call did not produce expected result
		/// </exception>
		public Pipeline CreatePipeline(VertexLayout vertexLayout, BizShader vertexShader, BizShader fragmentShader, bool required, string memo)
		{
			// if the shaders aren't available, the pipeline isn't either
			if (!vertexShader.Available || !fragmentShader.Available)
			{
				var errors = $"Vertex Shader:\r\n {vertexShader.Errors} \r\n-------\r\nFragment Shader:\r\n{fragmentShader.Errors}";
				if (required)
				{
					throw new InvalidOperationException($"Couldn't build required GL pipeline:\r\n{errors}");
				}
 
				return new(this, null, false, null, null, null) { Errors = errors };
			}

			var success = true;

			var vsw = (ShaderWrapper)vertexShader.Opaque;
			var fsw = (ShaderWrapper)fragmentShader.Opaque;

			var pid = GL.CreateProgram();
			GL.AttachShader(pid, vsw.SID);
			GL.AttachShader(pid, fsw.SID);
			_ = GL.GetError();

			GL.LinkProgram(pid);

			var errcode = (ErrorCode)GL.GetError();
			var resultLog = GL.GetProgramInfoLog(pid);

			if (errcode != ErrorCode.NoError)
			{
				if (required)
				{
					throw new InvalidOperationException($"Error creating pipeline (error returned from glLinkProgram): {errcode}\r\n\r\n{resultLog}");
				}

				success = false;
			}

			GL.GetProgram(pid, GLEnum.LinkStatus, out var linkStatus);
			if (linkStatus == 0)
			{
				if (required)
				{
					throw new InvalidOperationException($"Error creating pipeline (link status false returned from glLinkProgram): \r\n\r\n{resultLog}");
				}

				success = false;
#if DEBUG
				resultLog = GL.GetProgramInfoLog(pid);
				Util.DebugWriteLine(resultLog);
#endif
			}

			// need to work on validation. apparently there are some weird caveats to glValidate which make it complicated and possibly excuses (barely) the intel drivers' dysfunctional operation
			// "A sampler points to a texture unit used by fixed function with an incompatible target"
			//
			// info:
			// http://www.opengl.org/sdk/docs/man/xhtml/glValidateProgram.xml
			// This function mimics the validation operation that OpenGL implementations must perform when rendering commands are issued while programmable shaders are part of current state.
			// glValidateProgram checks to see whether the executables contained in program can execute given the current OpenGL state
			// This function is typically useful only during application development.
			//
			// So, this is no big deal. we shouldn't be calling validate right now anyway.
			// conclusion: glValidate is very complicated and is of virtually no use unless your draw calls are returning errors and you want to know why
			// _ = GL.GetError();
			// GL.ValidateProgram(pid);
			// errcode = (ErrorCode)GL.GetError();
			// resultLog = GL.GetProgramInfoLog(pid);
			// if (errcode != ErrorCode.NoError)
			//   throw new InvalidOperationException($"Error creating pipeline (error returned from glValidateProgram): {errcode}\r\n\r\n{resultLog}");
			// GL.GetProgram(pid, GetProgramParameterName.ValidateStatus, out var validateStatus);
			// if (validateStatus == 0)
			//   throw new InvalidOperationException($"Error creating pipeline (validateStatus status false returned from glValidateProgram): \r\n\r\n{resultLog}");

			// set the program to active, in case we need to set sampler uniforms on it
			GL.UseProgram(pid);

			// get all the uniforms
			var uniforms = new List<UniformInfo>();
			GL.GetProgram(pid, GLEnum.ActiveUniforms, out var nUniforms);
			var samplers = new List<int>();

			for (uint i = 0; i < nUniforms; i++)
			{
				GL.GetActiveUniform(pid, i, 1024, out _, out _, out UniformType type, out string name);
				var loc = GL.GetUniformLocation(pid, name);

				var ui = new UniformInfo { Name = name, Opaque = loc };

				if (type == UniformType.Sampler2D)
				{
					ui.IsSampler = true;
					ui.SamplerIndex = samplers.Count;
					ui.Opaque = loc | (samplers.Count << 24);
					samplers.Add(loc);
				}

				uniforms.Add(ui);
			}

			// deactivate the program, so we don't accidentally use it
			GL.UseProgram(0);

			if (!vertexShader.Available) success = false;
			if (!fragmentShader.Available) success = false;

			var pw = new PipelineWrapper { PID = pid, VertexShader = vertexShader, FragmentShader = fragmentShader, SamplerLocs = samplers };

			return new(this, pw, success, vertexLayout, uniforms, memo);
		}

		public void FreePipeline(Pipeline pipeline)
		{
			// unavailable pipelines will have no opaque
			if (pipeline.Opaque is not PipelineWrapper pw)
			{
				return;
			}

			GL.DeleteProgram(pw.PID);

			pw.FragmentShader.Release();
			pw.VertexShader.Release();
		}

		public void Internal_FreeShader(BizShader shader)
		{
			var sw = (ShaderWrapper)shader.Opaque;
			GL.DeleteShader(sw.SID);
		}

		/// <exception cref="InvalidOperationException"><paramref name="pipeline"/>.<see cref="Pipeline.Available"/> is <see langword="false"/></exception>
		public void BindPipeline(Pipeline pipeline)
		{
			_currPipeline = pipeline;

			if (pipeline == null)
			{
				GL.UseProgram(0);
				return;
			}

			if (!pipeline.Available)
			{
				throw new InvalidOperationException("Attempt to bind unavailable pipeline");
			}

			var pw = (PipelineWrapper)pipeline.Opaque;
			GL.UseProgram(pw.PID);

			// this is dumb and confusing, but we have to bind physical sampler numbers to sampler variables.
			for (var i = 0; i < pw.SamplerLocs.Count; i++)
			{
				GL.Uniform1(pw.SamplerLocs[i], i);
			}
		}

		private class VertexLayoutWrapper
		{
			public uint VAO;
			public uint VBO;
			public int BufferLen;
		}

		public VertexLayout CreateVertexLayout()
		{
			var vlw = new VertexLayoutWrapper
			{
				VAO = GL.GenVertexArray(),
				VBO = GL.GenBuffer(),
				BufferLen = 0,
			};

			return new(this, vlw);
		}

		public void Internal_FreeVertexLayout(VertexLayout vl)
		{
			var vlw = (VertexLayoutWrapper)vl.Opaque;
			GL.DeleteVertexArray(vlw.VAO);
			GL.DeleteBuffer(vlw.VBO);
		}

		private void BindTexture2d(Texture2d tex)
		{
			GL.BindTexture(TextureTarget.Texture2D, (uint)tex.Opaque);
		}

		private void LegacyBindArrayData(VertexLayout vertexLayout, IntPtr pData)
		{
			// DEPRECATED CRAP USED, NEEDED FOR ANCIENT SHADERS
			// ALSO THIS IS WHY LEGACY PACKAGE IS USED AS NON-LEGACY DOESN'T HAVE THESE
			// TODO: REMOVE NEED FOR THIS
#pragma warning disable CS0618
#pragma warning disable CS0612

			// disable all the client states.. a lot of overhead right now, to be sure

			GL.DisableClientState(EnableCap.VertexArray);
			GL.DisableClientState(EnableCap.ColorArray);

			for (var i = 1; i >= 0; i--)
			{
				GL.ClientActiveTexture(TextureUnit.Texture0 + i);
				GL.DisableClientState(EnableCap.TextureCoordArray);
			}

			unsafe
			{
				foreach (var (_, item) in vertexLayout.Items)
				{
					switch (item.Usage)
					{
						case AttribUsage.Position:
							GL.EnableClientState(EnableCap.VertexArray);
							GL.VertexPointer(item.Components, VertexPointerType.Float, (uint)item.Stride, (pData + item.Offset).ToPointer());
							break;
						case AttribUsage.Texcoord0:
							GL.ClientActiveTexture(TextureUnit.Texture0);
							GL.EnableClientState(EnableCap.TextureCoordArray);
							GL.TexCoordPointer(item.Components, TexCoordPointerType.Float, (uint)item.Stride, (pData + item.Offset).ToPointer());
							break;
						case AttribUsage.Texcoord1:
							GL.ClientActiveTexture(TextureUnit.Texture1);
							GL.EnableClientState(EnableCap.TextureCoordArray);
							GL.TexCoordPointer(item.Components, TexCoordPointerType.Float, (uint)item.Stride, (pData + item.Offset).ToPointer());
							GL.ClientActiveTexture(TextureUnit.Texture0);
							break;
						case AttribUsage.Color0:
							break;
						case AttribUsage.Unspecified:
						default:
							throw new InvalidOperationException();
					}
				}
			}
#pragma warning restore CS0618
#pragma warning restore CS0612
		}

		public void Draw(IntPtr data, int count)
		{
			if (_currPipeline == null)
			{
				throw new InvalidOperationException($"Tried to {nameof(Draw)} without pipeline!");
			}

			var vertexLayout = _currPipeline.VertexLayout;

			if (_currPipeline.Memo != "xgui")
			{
				LegacyBindArrayData(vertexLayout, data);
				GL.DrawArrays(PrimitiveType.TriangleStrip, 0, (uint)count);
				return;
			}

			var vlw = (VertexLayoutWrapper)vertexLayout.Opaque;
			GL.BindVertexArray(vlw.VAO);
			GL.BindBuffer(GLEnum.ArrayBuffer, vlw.VBO);

			var stride = vertexLayout.Items[0].Stride;
			Debug.Assert(vertexLayout.Items.All(i => i.Value.Stride == stride));

			unsafe
			{
				var vertexes = new ReadOnlySpan<byte>(data.ToPointer(), count * stride);

				// BufferData reallocs and BufferSubData doesn't, so only use the former if we need to grow the buffer
				if (vertexes.Length > vlw.BufferLen)
				{
					GL.BufferData(GLEnum.ArrayBuffer, vertexes, GLEnum.DynamicDraw);
					vlw.BufferLen = vertexes.Length;
				}
				else
				{
					GL.BufferSubData(GLEnum.ArrayBuffer, 0, vertexes);
				}

				foreach (var (i, item) in vertexLayout.Items)
				{
					GL.VertexAttribPointer(
						(uint)i,
						item.Components,
						(GLVertexAttribPointerType)item.AttribType, // these are the same enum
						item.Normalized,
						(uint)item.Stride,
						(void*)item.Offset);
					GL.EnableVertexAttribArray((uint)i);
				}
			}

			GL.DrawArrays(PrimitiveType.TriangleStrip, 0, (uint)count);

			foreach (var (i, _) in vertexLayout.Items)
			{
				GL.DisableVertexAttribArray((uint)i);
			}

			GL.BindBuffer(GLEnum.ArrayBuffer, 0);
			GL.BindVertexArray(0);
		}

		public void SetPipelineUniform(PipelineUniform uniform, bool value)
		{
			GL.Uniform1((int)uniform.Sole.Opaque, value ? 1 : 0);
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, Matrix4x4 mat, bool transpose)
		{
			GL.UniformMatrix4((int)uniform.Sole.Opaque, 1, transpose, (float*)&mat);
		}

		public unsafe void SetPipelineUniformMatrix(PipelineUniform uniform, ref Matrix4x4 mat, bool transpose)
		{
			fixed (Matrix4x4* pMat = &mat)
			{
				GL.UniformMatrix4((int)uniform.Sole.Opaque, 1, transpose, (float*)pMat);
			}
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
			if (uniform.Owner == null)
			{
				return; // uniform was optimized out
            }

			GL.Uniform1((int)uniform.Sole.Opaque, value);
		}

		public unsafe void SetPipelineUniform(PipelineUniform uniform, Vector4[] values)
		{
			fixed (Vector4* pValues = &values[0])
			{
				GL.Uniform4((int)uniform.Sole.Opaque, (uint)values.Length, (float*)pValues);
			}
		}

		public void SetPipelineUniformSampler(PipelineUniform uniform, Texture2d tex)
		{
			var n = (int)uniform.Sole.Opaque >> 24;

			// set the sampler index into the uniform first
			GL.ActiveTexture(TextureUnit.Texture0 + n);

			// now bind the texture
			GL.BindTexture(TextureTarget.Texture2D, (uint)tex.Opaque);
		}

		public void SetTextureFilter(Texture2d texture, bool linear)
		{
			BindTexture2d(texture);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)(linear ? TextureMinFilter.Linear : TextureMinFilter.Nearest));
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)(linear ? TextureMagFilter.Linear : TextureMagFilter.Nearest));
		}

		public Texture2d CreateTexture(int width, int height)
		{
			var texId = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, texId);
			unsafe
			{
				GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, null);
			}

			// sensible defaults
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			return new(this, texId, width, height);
		}

		public Texture2d WrapGLTexture2d(IntPtr glTexId, int width, int height)
			=> new(this, (uint)glTexId.ToInt32(), width, height) { IsUpsideDown = true };

		public unsafe void LoadTextureData(Texture2d tex, BitmapBuffer bmp)
		{
			var bmpData = bmp.LockBits();
			try
			{
				GL.BindTexture(TextureTarget.Texture2D, (uint)tex.Opaque);
				GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)bmp.Width, (uint)bmp.Height, PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0.ToPointer());
			}
			finally
			{
				bmp.UnlockBits(bmpData);
			}
		}

		public void FreeRenderTarget(RenderTarget rt)
		{
			rt.Texture2d.Dispose();
			GL.DeleteFramebuffer((uint)rt.Opaque);
		}

		/// <exception cref="InvalidOperationException">framebuffer creation unsuccessful</exception>
		public RenderTarget CreateRenderTarget(int width, int height)
		{
			// create a texture for it
			var tex = CreateTexture(width, height);
			var texId = (uint)tex.Opaque;

			// create the FBO
			var fbId = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbId);

			// bind the tex to the FBO
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texId, 0);

			// do something, I guess say which color buffers are used by the framebuffer
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

			if ((FramebufferStatus)GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.Complete)
			{
				throw new InvalidOperationException($"Error creating framebuffer (at {nameof(GL.CheckFramebufferStatus)})");
			}

			// since we're done configuring unbind this framebuffer, to return to the default
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			return new(this, fbId, tex);
		}

		public void BindRenderTarget(RenderTarget rt)
		{
			_currRenderTarget = rt;
			if (rt == null)
			{
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			}
			else
			{
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)rt.Opaque);
			}
		}

		public unsafe BitmapBuffer ResolveTexture2d(Texture2d tex)
		{
			// note - this is dangerous since it changes the bound texture. could we save it?
			BindTexture2d(tex);
			var bb = new BitmapBuffer(tex.IntWidth, tex.IntHeight);
			var bmpdata = bb.LockBits();
			GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmpdata.Scan0.ToPointer());
			bb.UnlockBits(bmpdata);
			return bb;
		}

		public Matrix4x4 CreateGuiProjectionMatrix(int width, int height)
		{
			var ret = Matrix4x4.Identity;
			ret.M11 = 2.0f / width;
			ret.M22 = 2.0f / height;
			return ret;
		}

		public Matrix4x4 CreateGuiViewMatrix(int width, int height, bool autoflip)
		{
			var ret = Matrix4x4.Identity;
			ret.M22 = -1.0f;
			ret.M41 = width * -0.5f;
			ret.M42 = height * 0.5f;
			if (autoflip && _currRenderTarget is not null) // flip as long as we're not a final render target
			{
				ret.M22 = 1.0f;
				ret.M42 *= -1;
			}

			return ret;
		}

		public void SetViewport(int x, int y, int width, int height)
		{
			GL.Viewport(x, y, (uint)width, (uint)height);
			GL.Scissor(x, y, (uint)width, (uint)height); // hack for mupen[rice]+intel: at least the rice plugin leaves the scissor rectangle scrambled, and we're trying to run it in the main graphics context for intel
			// BUT ALSO: new specifications.. viewport+scissor make sense together
		}

		private BizShader CreateShader(ShaderType type, string source, bool required)
		{
			var sw = new ShaderWrapper();
			var info = string.Empty;

			_ = GL.GetError();
			var sid = GL.CreateShader(type);
			var ok = CompileShaderSimple(sid, source, required);
			if (!ok)
			{
				GL.GetShaderInfoLog(sid, out info);
				GL.DeleteShader(sid);
				sid = 0;
			}

			var ret = new BizShader(this, sw, ok)
			{
				Errors = info
			};

			sw.SID = sid;

			return ret;
		}

		private bool CompileShaderSimple(uint sid, string source, bool required)
		{
			var success = true;

			var errcode = (ErrorCode)GL.GetError();
			if (errcode != ErrorCode.NoError)
			{
				if (required)
				{
					throw new InvalidOperationException($"Error compiling shader (from previous operation) {errcode}");
				}

				success = false;
			}

			GL.ShaderSource(sid, source);
			
			errcode = (ErrorCode)GL.GetError();
			if (errcode != ErrorCode.NoError)
			{
				if (required)
				{
					throw new InvalidOperationException($"Error compiling shader ({nameof(GL.ShaderSource)}) {errcode}");
				}

				success = false;
			}

			GL.CompileShader(sid);

			errcode = (ErrorCode)GL.GetError();
			var resultLog = GL.GetShaderInfoLog(sid);
			if (errcode != ErrorCode.NoError)
			{
				var message = $"Error compiling shader ({nameof(GL.CompileShader)}) {errcode}\r\n\r\n{resultLog}";
				if (required)
				{
					throw new InvalidOperationException(message);
				}
					
				Console.WriteLine(message);
				success = false;
			}

			GL.GetShader(sid, ShaderParameterName.CompileStatus, out var n);

			if (n == 0)
			{
				if (required)
				{
					throw new InvalidOperationException($"Error compiling shader ({nameof(GL.GetShader)})\r\n\r\n{resultLog}");
				}

				success = false;
			}

			return success;
		}
	}
}
