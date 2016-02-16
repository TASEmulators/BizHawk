using System;
using System.Drawing;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// Handles RetroArch's GLSL shader pass format
	/// </summary>
	public class RetroShader : IDisposable
	{
		//NOTE: we may need to overhaul uniform-setting infrastructure later. 
		//maybe samplers will need to be set by index and not by name (I think the specs dont dictate what the sampler must be named)

		public RetroShader(IGL owner, string source, bool debug = false)
		{
			Owner = owner;

			VertexLayout = owner.CreateVertexLayout();
			VertexLayout.DefineVertexAttribute("position", 0, 4, VertexAttribPointerType.Float, AttributeUsage.Position, false, 40, 0);
			VertexLayout.DefineVertexAttribute("color", 1, 4, VertexAttribPointerType.Float, AttributeUsage.Color0, false, 40, 16); //just dead weight, i have no idea why this is here. but some old HLSL compilers (used in bizhawk for various reasons) will want it to exist here since it exists in the vertex shader
			VertexLayout.DefineVertexAttribute("texCoord1", 2, 2, VertexAttribPointerType.Float, AttributeUsage.Texcoord0, false, 40, 32);
			VertexLayout.Close();

			string defines = "#define TEXCOORD TEXCOORD0\r\n"; //maybe not safe..
			string vsSource = "#define VERTEX\r\n" + defines + source;
			string psSource = "#define FRAGMENT\r\n" + defines + source;
			var vs = owner.CreateVertexShader(true, vsSource, "main_vertex", debug);
			var ps = owner.CreateFragmentShader(true, psSource, "main_fragment", debug);
			Pipeline = Owner.CreatePipeline(VertexLayout, vs, ps, debug, "retro");

			if (!Pipeline.Available)
			{
				Available = false;
				return;
			}

			//retroarch shaders will sometimes not have the right sampler name
			//it's unclear whether we should bind to s_p or sampler0
			//lets bind to sampler0 in case we dont have s_p
			sampler0 = Pipeline.TryGetUniform("s_p");
			if (sampler0 == null)
			{
				//sampler wasn't named correctly. this can happen on some retroarch shaders
				foreach (var u in Pipeline.GetUniforms())
				{
					if (u.Sole.IsSampler && u.Sole.SamplerIndex == 0)
					{
						sampler0 = u;
						break;
					}
				}
			}

			if (sampler0 == null)
				return;

			Available = true;
		}

		public bool Available { get; private set; }
		public string Errors { get { return Pipeline.Errors; } }

		PipelineUniform sampler0;

		public void Dispose()
		{
			Pipeline.Dispose();
		}

		public void Bind()
		{
			//lame...
			Owner.BindPipeline(Pipeline);
		}

		public unsafe void Run(Texture2d tex, Size InputSize, Size OutputSize, bool flip)
		{
			flip = false;
			//test

			//ack! make sure to set the pipeline before setting uniforms
			Bind();

			Pipeline["IN.video_size"].Set(new Vector2(InputSize.Width, InputSize.Height));
			Pipeline["IN.texture_size"].Set(new Vector2(tex.Width, tex.Height));
			Pipeline["IN.output_size"].Set(new Vector2(OutputSize.Width, OutputSize.Height));
			Pipeline["IN.frame_count"].Set(1); //todo
			Pipeline["IN.frame_direction"].Set(1); //todo


			var Projection = Owner.CreateGuiProjectionMatrix(OutputSize);
			var Modelview = Owner.CreateGuiViewMatrix(OutputSize);
			var mat = Modelview * Projection;
			mat.Transpose();
			Pipeline["modelViewProj"].Set(mat, true);

			Owner.SetTextureWrapMode(tex, true);

			sampler0.Set(tex);
			Owner.SetViewport(OutputSize);

			int w = OutputSize.Width;
			int h = OutputSize.Height;
			float v0,v1;
			if (flip) { v0 = 1; v1 = 0; }
			else { v0 = 0; v1 = 1; }
			float* pData = stackalloc float[10*4];
			int i=0;
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
			pData[i++] = 1; pData[i++] = v1;

			Owner.SetBlendState(Owner.BlendNoneCopy);
			Owner.BindArrayData(pData);
			Owner.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
		}


		public IGL Owner { get; private set; }

		VertexLayout VertexLayout;
		public Pipeline Pipeline;
	}
}