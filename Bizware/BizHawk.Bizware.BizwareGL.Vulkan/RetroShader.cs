using System;
using System.Drawing;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL.Drivers.Vulkan
{
	/// <summary>
	/// Handles RetroArch's GLSL shader pass format
	/// This isnt implemented in BizwareGL abstract layer because it relies too much on GLSL peculiarities
	/// </summary>
	public class RetroShader : IDisposable
	{
		public RetroShader(IGL owner, string source, bool debug = false)
		{
			Owner = owner as IGL_TK;

			VertexLayout = owner.CreateVertexLayout();
			VertexLayout.DefineVertexAttribute("VertexCoord", 0, 4, VertexAttribPointerType.Float, AttributeUsage.Unspecified, false, 40, 0); //VertexCoord
			VertexLayout.DefineVertexAttribute("ColorShit", 1, 4, VertexAttribPointerType.Float, AttributeUsage.Unspecified, false, 40, 16); //COLOR
			VertexLayout.DefineVertexAttribute("TexCoord", 2, 2, VertexAttribPointerType.Float, AttributeUsage.Unspecified, false, 40, 32); //TexCoord (is this vec2 or vec4? the glsl converted from cg had vec4 but the cg had vec2...)
			VertexLayout.Close();

			string vsSource = "#define VERTEX\r\n" + source;
			string psSource = "#define FRAGMENT\r\n" + source;
			var vs = Owner.CreateVertexShader(vsSource, debug);
			var ps = Owner.CreateFragmentShader(psSource, debug);
			Pipeline = Owner.CreatePipeline(VertexLayout, vs, ps, debug);
		}

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
			//ack! make sure to set the pipeline before setting
			Bind();

			Pipeline["InputSize"].Set(new Vector2(InputSize.Width,InputSize.Height));
			Pipeline["TextureSize"].Set(new Vector2(InputSize.Width, InputSize.Height));
			Pipeline["OutputSize"].Set(new Vector2(OutputSize.Width, OutputSize.Height));
			Pipeline["FrameCount"].Set(0); //todo
			Pipeline["FrameDirection"].Set(1); //todo

			var Projection = Owner.CreateGuiProjectionMatrix(OutputSize);
			var Modelview = Owner.CreateGuiViewMatrix(OutputSize);
			Pipeline["MVPMatrix"].Set(Modelview * Projection, false);

			Owner.SetTextureWrapMode(tex, true);

			Pipeline["Texture"].Set(tex);
			Owner.SetViewport(OutputSize);

			int w = OutputSize.Width;
			int h = OutputSize.Height;
			float v0,v1;
			if (flip) { v0 = 1; v1 = 0; }
			else { v0 = 0; v1 = 1; }
			float* pData = stackalloc float[10*4];
			int i=0;
			pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; pData[i++] = 1; //topleft vert
			pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; //junk
			pData[i++] = 0; pData[i++] = v0; //texcoord
			pData[i++] = w; pData[i++] = 0; pData[i++] = 0; pData[i++] = 1; //topright vert
			pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; //junk
			pData[i++] = 1; pData[i++] = v0; //texcoord
			pData[i++] = 0; pData[i++] = h; pData[i++] = 0; pData[i++] = 1; //bottomleft vert
			pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; //junk
			pData[i++] = 0; pData[i++] = v1; //texcoord
			pData[i++] = w; pData[i++] = h; pData[i++] = 0; pData[i++] = 1; //bottomright vert
			pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; pData[i++] = 0; //junk
			pData[i++] = 1; pData[i++] = v1; //texcoord

			Owner.SetBlendState(Owner.BlendNoneCopy);
			Owner.BindArrayData(pData);
			Owner.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
		}


		public IGL_TK Owner { get; private set; }

		VertexLayout VertexLayout;
		public Pipeline Pipeline;
	}
}
