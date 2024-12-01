using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Silk.NET.OpenGL;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Bizware.Graphics
{
	internal class OpenGLPipeline : IPipeline
	{
		private readonly GL GL;

		public readonly uint VAO;
		public readonly uint VBO;

		public readonly int VertexStride;
		public int VertexBufferLen;

		public readonly uint IBO;
		public int IndexBufferLen;

		public readonly uint VertexSID;
		public readonly uint FragmentSID;

		public readonly uint PID;
		private readonly Dictionary<string, int> _uniforms = new();
		private readonly Dictionary<string, int> _samplers = new();

		private uint CompileShader(string source, ShaderType type)
		{
			var sid = GL.CreateShader(type);

			try
			{
				var errcode = (ErrorCode)GL.GetError();
				if (errcode != ErrorCode.NoError)
				{
					throw new InvalidOperationException($"Error compiling shader (from previous operation) {errcode}");
				}

				GL.ShaderSource(sid, source);

				errcode = (ErrorCode)GL.GetError();
				if (errcode != ErrorCode.NoError)
				{
					throw new InvalidOperationException($"Error compiling shader ({nameof(GL.ShaderSource)}) {errcode}");
				}

				GL.CompileShader(sid);

				errcode = (ErrorCode)GL.GetError();
				var resultLog = GL.GetShaderInfoLog(sid);
				if (errcode != ErrorCode.NoError)
				{
					throw new InvalidOperationException( $"Error compiling shader ({nameof(GL.CompileShader)}) {errcode}\r\n\r\n{resultLog}");
				}

				GL.GetShader(sid, ShaderParameterName.CompileStatus, out var n);

				if (n == 0)
				{
					throw new InvalidOperationException($"Error compiling shader ({nameof(GL.GetShader)})\r\n\r\n{resultLog}");
				}

				return sid;
			}
			catch
			{
				GL.DeleteShader(sid);
				throw;
			}
		}

		public OpenGLPipeline(GL gl, PipelineCompileArgs compileArgs)
		{
			GL = gl;

			try
			{
				VAO = GL.GenVertexArray();
				VBO = GL.GenBuffer();
				VertexStride = compileArgs.VertexLayoutStride;

				IBO = GL.GenBuffer();

				_ = GL.GetError();
				VertexSID = CompileShader(compileArgs.VertexShaderArgs.Source, ShaderType.VertexShader);
				FragmentSID = CompileShader(compileArgs.FragmentShaderArgs.Source, ShaderType.FragmentShader);

				PID = GL.CreateProgram();
				GL.AttachShader(PID, VertexSID);
				GL.AttachShader(PID, FragmentSID);

				GL.BindVertexArray(VAO); // used by EnableVertexAttribArray
				GL.BindBuffer(GLEnum.ArrayBuffer, VBO); // used by VertexAttribPointer
				unsafe
				{
					for (var i = 0; i < compileArgs.VertexLayout.Count; i++)
					{
						var item = compileArgs.VertexLayout[i];
						var attribIndex = (uint)i;
						GL.BindAttribLocation(PID, attribIndex, item.Name);
						GL.EnableVertexAttribArray(attribIndex);
						GL.VertexAttribPointer(
							attribIndex,
							item.Integer ? (int)GLEnum.Bgra : item.Components,
							item.Integer ? VertexAttribPointerType.UnsignedByte : VertexAttribPointerType.Float,
							normalized: item.Integer,
							(uint)VertexStride,
							(void*)item.Offset);
					}
				}

				GL.BindFragDataLocation(PID, 0, compileArgs.FragmentOutputName);

				_ = GL.GetError();
				GL.LinkProgram(PID);

				var errcode = (ErrorCode)GL.GetError();
				var resultLog = GL.GetProgramInfoLog(PID);

				if (errcode != ErrorCode.NoError)
				{
					throw new InvalidOperationException($"Error creating pipeline (error returned from ({nameof(GL.LinkProgram)}): {errcode}\r\n\r\n{resultLog}");
				}

				GL.GetProgram(PID, GLEnum.LinkStatus, out var linkStatus);

				if (linkStatus == 0)
				{
					throw new InvalidOperationException($"Error creating pipeline (link status false returned from {nameof(GL.GetProgram)}): \r\n\r\n{resultLog}");
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
#if false
				_ = GL.GetError();
				GL.ValidateProgram(PID);
				errcode = (ErrorCode)GL.GetError();
				resultLog = GL.GetProgramInfoLog(PID);
				if (errcode != ErrorCode.NoError)
				{
					throw new InvalidOperationException($"Error creating pipeline (error returned from {nameof(GL.ValidateProgram)}): {errcode}\r\n\r\n{resultLog}");
				}

				GL.GetProgram(PID, GLEnum.ValidateStatus, out var validateStatus);
				if (validateStatus == 0)
				{
					throw new InvalidOperationException($"Error creating pipeline (validateStatus status false returned from glValidateProgram): \r\n\r\n{resultLog}");
				}
#endif
				// set the program to active, in case we need to set sampler uniforms on it
				GL.UseProgram(PID);

				// get all the uniforms
				GL.GetProgram(PID, GLEnum.ActiveUniforms, out var numUniforms);

				for (uint i = 0; i < numUniforms; i++)
				{
					GL.GetActiveUniform(PID, i, 1024, out _, out _, out UniformType type, out string name);
					var loc = GL.GetUniformLocation(PID, name);

					// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
					switch (type)
					{
						case UniformType.Bool:
						case UniformType.Float:
						case UniformType.FloatVec2:
						case UniformType.FloatVec3:
						case UniformType.FloatVec4:
						case UniformType.FloatMat4:
							_uniforms.Add(name, loc);
							break;
						case UniformType.Sampler2D:
							// this is dumb and confusing, but we have to bind physical sampler numbers to sampler variables
							var bindPoint = _samplers.Count;
							GL.Uniform1(loc, bindPoint);
							_samplers.Add(name, bindPoint);
							break;
					}
				}
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public void Dispose()
		{
			GL.DeleteProgram(PID);

			GL.DeleteShader(VertexSID);
			GL.DeleteShader(FragmentSID);

			GL.DeleteVertexArray(VAO);
			GL.DeleteBuffer(VBO);
		}

		public void SetVertexData(IntPtr data, int count)
		{
			ReadOnlySpan<byte> vertexes = Util.UnsafeSpanFromPointer(ptr: data, length: count * VertexStride);
			// BufferData reallocs and BufferSubData doesn't, so only use the former if we need to grow the buffer
			if (vertexes.Length > VertexBufferLen)
			{
				GL.BufferData(GLEnum.ArrayBuffer, vertexes, GLEnum.DynamicDraw);
				VertexBufferLen = vertexes.Length;
			}
			else
			{
				GL.BufferSubData(GLEnum.ArrayBuffer, 0, vertexes);
			}
		}

		public void SetIndexData(IntPtr data, int count)
		{
			ReadOnlySpan<byte> indexes = Util.UnsafeSpanFromPointer(ptr: data, length: count * 2);
			if (indexes.Length > IndexBufferLen)
			{
				GL.BufferData(GLEnum.ElementArrayBuffer, indexes, GLEnum.DynamicDraw);
				IndexBufferLen = indexes.Length;
			}
			else
			{
				GL.BufferSubData(GLEnum.ElementArrayBuffer, 0, indexes);
			}
		}

		public bool HasUniformSampler(string name)
			=> _samplers.ContainsKey(name);

		public string GetUniformSamplerName(int index)
		{
			var sampler = _samplers.AsEnumerable().FirstOrNull(s => s.Value == index);
			return sampler?.Key;
		}

		public void SetUniformSampler(string name, ITexture2D tex)
		{
			if (tex == null)
			{
				return;
			}

			if (_samplers.TryGetValue(name, out var sampler))
			{
				var oglTex = (OpenGLTexture2D)tex;
				GL.ActiveTexture(TextureUnit.Texture0 + sampler);
				GL.BindTexture(TextureTarget.Texture2D, oglTex.TexID);
			}
		}

		public void SetUniformMatrix(string name, Matrix4x4 mat, bool transpose)
			=> SetUniformMatrix(name, ref mat, transpose);

		public unsafe void SetUniformMatrix(string name, ref Matrix4x4 mat, bool transpose)
		{
			if (_uniforms.TryGetValue(name, out var uid))
			{
				fixed (Matrix4x4* p = &mat)
				{
					GL.UniformMatrix4(uid, 1, transpose, (float*)p);
				}
			}
		}

		public unsafe void SetUniform(string name, Vector4 value)
		{
			if (_uniforms.TryGetValue(name, out var uid))
			{
				GL.Uniform4(uid, 1, (float*)&value);
			}
		}

		public unsafe void SetUniform(string name, Vector2 value)
		{
			if (_uniforms.TryGetValue(name, out var uid))
			{
				GL.Uniform2(uid, 1, (float*)&value);
			}
		}

		public unsafe void SetUniform(string name, float value)
		{
			if (_uniforms.TryGetValue(name, out var uid))
			{
				GL.Uniform1(uid, 1, &value);
			}
		}

		public unsafe void SetUniform(string name, bool value)
		{
			if (_uniforms.TryGetValue(name, out var uid))
			{
				// note: GLSL bool is 4 bytes large
				var b = value ? 1 : 0;
				GL.Uniform1(uid, 1, &b);
			}
		}
	}
}
