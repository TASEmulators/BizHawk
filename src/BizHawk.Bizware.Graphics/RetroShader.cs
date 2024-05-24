using System;
using System.Drawing;
using System.Numerics;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Handles RetroArch's GLSL shader pass format
	/// </summary>
	public class RetroShader : IDisposable
	{
		// NOTE: we may need to overhaul uniform-setting infrastructure later.
		// maybe samplers will need to be set by index and not by name (I think the specs don't dictate what the sampler must be named)
		public RetroShader(IGL owner, string source, bool debug = false)
		{
			Owner = owner;

			var vertexLayoutItems = new PipelineCompileArgs.VertexLayoutItem[3];
			vertexLayoutItems[0] = new("position", 4, 0, AttribUsage.Position);
			vertexLayoutItems[1] = new("color", 4, 16, AttribUsage.Color0); // just dead weight, i have no idea why this is here. but some old HLSL compilers (used in bizhawk for various reasons) will want it to exist here since it exists in the vertex shader
			vertexLayoutItems[2] = new("tex", 2, 32, AttribUsage.Texcoord0);

			string vsSource, psSource;
			if (owner.DispMethodEnum == EDispMethod.OpenGL)
			{
				vsSource = "#version 130\r\n";
				psSource = "#version 130\r\n";
			}
			else
			{
				vsSource = "";
				psSource = "";
			}

			vsSource += $"#define VERTEX\r\n{source}";
			psSource += $"#define FRAGMENT\r\n{source}";

			var compileArgs = new PipelineCompileArgs(
				vertexLayoutItems,
				vertexShaderArgs: new(vsSource, "main_vertex"),
				fragmentShaderArgs: new(psSource, "main_fragment"),
				fragmentOutputName: "oColor");

			try
			{
				Pipeline = Owner.CreatePipeline(compileArgs);
			}
			catch (Exception ex)
			{
				if (!debug)
				{
					Errors = ex.Message;
					return;
				}

				throw;
			}

			// retroarch shaders will sometimes not have the right sampler name
			// it's unclear whether we should bind to s_p or sampler0
			// lets bind to sampler0 in case we don't have s_p
			sampler0 = Pipeline.HasUniformSampler("s_p") ? "s_p" : Pipeline.GetUniformSamplerName(0);

			// if a sampler isn't available, we can't do much, although this does interfere with debugging (shaders just returning colors will malfunction)
			Available = sampler0 != null;
		}

		public bool Available { get; }
		public string Errors { get; }

		private readonly string sampler0;

		public void Dispose()
		{
			Pipeline.Dispose();
		}

		public void Bind()
		{
			// lame...
			Owner.BindPipeline(Pipeline);
		}

		public unsafe void Run(ITexture2D tex, Size InputSize, Size OutputSize, bool flip)
		{
			// ack! make sure to set the pipeline before setting uniforms
			Bind();

			Pipeline.SetUniform("IN.video_size", new Vector2(InputSize.Width, InputSize.Height));
			Pipeline.SetUniform("IN.texture_size", new Vector2(tex.Width, tex.Height));
			Pipeline.SetUniform("IN.output_size", new Vector2(OutputSize.Width, OutputSize.Height));
			Pipeline.SetUniform("IN.frame_count", 1); //todo
			Pipeline.SetUniform("IN.frame_direction", 1); //todo

			var Projection = Owner.CreateGuiProjectionMatrix(OutputSize);
			var Modelview = Owner.CreateGuiViewMatrix(OutputSize);
			var mat = Modelview * Projection;
			Pipeline.SetUniformMatrix("modelViewProj", mat);

			Pipeline.SetUniformSampler(sampler0, tex);
			Owner.SetViewport(OutputSize);

			var time = DateTime.Now.Second + (float)DateTime.Now.Millisecond / 1000;
			Pipeline.SetUniform("Time", time);

			var w = OutputSize.Width;
			var h = OutputSize.Height;

			float v0, v1;
			if (flip)
			{
				v0 = 1;
				v1 = 0;
			}
			else
			{
				v0 = 0;
				v1 = 1;
			}

			var pData = stackalloc float[10 * 4];
			var i = 0;
			pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; pData[i++] = 1; //topleft vert
			pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; //useless color
			pData[i++] = 0; pData[i++] = v0;
			pData[i++] = w; pData[i++] = 0; pData[i++] = 0; pData[i++] = 1; //topright vert
			pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; //useless color
			pData[i++] = 1; pData[i++] = v0;
			pData[i++] = 0; pData[i++] = h; pData[i++] = 0; pData[i++] = 1; //bottomleft vert
			pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; //useless color
			pData[i++] = 0; pData[i++] = v1;
			pData[i++] = w; pData[i++] = h; pData[i++] = 0; pData[i++] = 1; //bottomright vert
			pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; //useless color
			pData[i++] = 1; pData[i] = v1;

			Pipeline.SetVertexData(new(pData), 4);

			Owner.DisableBlending();
			Owner.Draw(4);
		}

		public IGL Owner { get; }

		public readonly IPipeline Pipeline;
	}
}