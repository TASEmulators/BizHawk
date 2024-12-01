//http://stackoverflow.com/questions/6893302/decode-rgb-value-to-single-float-without-bit-shift-in-glsl

using System.Drawing;
using System.Numerics;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// A simple renderer useful for rendering GUI stuff.
	/// When doing GUI rendering, run everything through here (if you need a GL feature not done through here, run it through here first)
	/// Call Begin, then Draw, then End, and don't use other Renderers or GL calls in the meantime, unless you know what you're doing.
	/// This can perform batching (well.. maybe not yet), which is occasionally necessary for drawing large quantities of things.
	/// </summary>
	public class GuiRenderer : IGuiRenderer
	{
		public GuiRenderer(IGL owner)
		{
			Owner = owner;

			_projection = new();
			_modelView = new();

			string psProgram, vsProgram;

			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch (owner.DispMethodEnum)
			{
				case EDispMethod.D3D11:
					vsProgram = DefaultVertexShader_d3d11;
					psProgram = DefaultPixelShader_d3d11;
					break;
				case EDispMethod.OpenGL:
					vsProgram = DefaultVertexShader_gl;
					psProgram = DefaultPixelShader_gl;
					break;
				default:
					throw new InvalidOperationException();
			}

			var vertexLayoutItems = new PipelineCompileArgs.VertexLayoutItem[3];
			vertexLayoutItems[0] = new("aPosition", 2, 0, AttribUsage.Position);
			vertexLayoutItems[1] = new("aTexcoord", 2, 8, AttribUsage.Texcoord0);
			vertexLayoutItems[2] = new("aColor", 4, 16, AttribUsage.Texcoord1);

			var compileArgs = new PipelineCompileArgs(
				vertexLayoutItems,
				vertexShaderArgs: new(vsProgram, "vsmain"),
				fragmentShaderArgs: new(psProgram, "psmain"),
				fragmentOutputName: "FragColor");
			CurrPipeline = DefaultPipeline = Owner.CreatePipeline(compileArgs);
		}

		private readonly Vector4[] CornerColors =
		{
			new(1.0f, 1.0f, 1.0f, 1.0f),
			new(1.0f, 1.0f, 1.0f, 1.0f),
			new(1.0f, 1.0f, 1.0f, 1.0f),
			new(1.0f, 1.0f, 1.0f, 1.0f)
		};

		public void SetCornerColor(int which, Vector4 color)
		{
			Flush(); // don't really need to flush with current implementation. we might as well roll modulate color into it too.
			CornerColors[which] = color;
		}

		/// <exception cref="ArgumentException"><paramref name="colors"/> does not have exactly <c>4</c> elements</exception>
		public void SetCornerColors(Vector4[] colors)
		{
			Flush(); // don't really need to flush with current implementation. we might as well roll modulate color into it too.

			if (colors.Length != 4)
			{
				throw new ArgumentException("array must be size 4", nameof(colors));
			}

			for (var i = 0; i < 4; i++)
			{
				CornerColors[i] = colors[i];
			}
		}

		public void Dispose()
			=> DefaultPipeline.Dispose();

		/// <exception cref="InvalidOperationException"><see cref="IsActive"/> is <see langword="true"/></exception>
		public void SetPipeline(IPipeline pipeline)
		{
			if (IsActive)
			{
				throw new InvalidOperationException("Can't change pipeline while renderer is running!");
			}

			Flush();
			CurrPipeline = pipeline;

			// save the modulate color? user beware, I guess, for now.
		}

		public void SetDefaultPipeline()
		{
			SetPipeline(DefaultPipeline);
		}

		public void SetModulateColorWhite()
		{
			SetModulateColor(Color.White);
		}

		public void SetModulateColor(Color color)
		{
			Flush();
			CurrPipeline.SetUniform("uModulateColor", new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f));
		}

		public void EnableBlending()
		{
			Flush();
			Owner.EnableBlending();
		}

		public void DisableBlending()
		{
			Flush();
			Owner.DisableBlending();
		}

		private MatrixStack _projection, _modelView;

		public MatrixStack Projection
		{
			get => _projection;
			set
			{
				_projection = value;
				_projection.IsDirty = true;
			}
		}

		public MatrixStack ModelView
		{
			get => _modelView;
			set
			{
				_modelView = value;
				_modelView.IsDirty = true;
			}
		}

		/// <exception cref="InvalidOperationException">no pipeline set (need to call <see cref="SetPipeline"/>)</exception>
		public void Begin(int width, int height)
		{
			// uhhmmm I want to throw an exception if its already active, but its annoying.

			if (CurrPipeline == null)
			{
				throw new InvalidOperationException("Pipeline hasn't been set!");
			}

			IsActive = true;
			Owner.BindPipeline(CurrPipeline);
			Owner.DisableBlending();

			// clear state cache
			CurrPipeline.SetUniform("uSamplerEnable", false);
			ModelView.Clear();
			Projection.Clear();
			SetModulateColorWhite();

			Projection = Owner.CreateGuiViewMatrix(width, height) * Owner.CreateGuiProjectionMatrix(width, height);
			ModelView.Clear();

			Owner.SetViewport(width, height);
		}

		public void Flush()
		{
			// no batching, nothing to do here yet
		}

		/// <exception cref="InvalidOperationException"><see cref="IsActive"/> is <see langword="false"/></exception>
		public void End()
		{
			if (!IsActive)
			{
				throw new InvalidOperationException($"{nameof(GuiRenderer)} is not active!");
			}

			IsActive = false;
		}

		public void DrawSubrect(ITexture2D tex, float x, float y, float w, float h, float u0, float v0, float u1, float v1)
		{
			PrepDrawSubrectInternal(tex);
			EmitRectangleInternal(x, y, w, h, u0, v0, u1, v1);
		}

		private void PrepDrawSubrectInternal(ITexture2D tex)
		{
			CurrPipeline.SetUniformSampler("uSampler0", tex);
			CurrPipeline.SetUniform("uSamplerEnable", tex != null);

			if (_projection.IsDirty)
			{
				CurrPipeline.SetUniformMatrix("um44Projection", ref _projection.Top);
				_projection.IsDirty = false;
			}

			if (_modelView.IsDirty)
			{
				CurrPipeline.SetUniformMatrix("um44Modelview", ref _modelView.Top);
				_modelView.IsDirty = false;
			}
		}

		private unsafe void EmitRectangleInternal(float x, float y, float w, float h, float u0, float v0, float u1, float v1)
		{
			var pData = stackalloc float[32];
			pData[0] = x;
			pData[1] = y;
			pData[2] = u0;
			pData[3] = v0;
			pData[4] = CornerColors[0].X;
			pData[5] = CornerColors[0].Y;
			pData[6] = CornerColors[0].Z;
			pData[7] = CornerColors[0].W;
			pData[8] = x + w;
			pData[9] = y;
			pData[10] = u1;
			pData[11] = v0;
			pData[12] = CornerColors[1].X;
			pData[13] = CornerColors[1].Y;
			pData[14] = CornerColors[1].Z;
			pData[15] = CornerColors[1].W;
			pData[16] = x;
			pData[17] = y + h;
			pData[18] = u0;
			pData[19] = v1;
			pData[20] = CornerColors[2].X;
			pData[21] = CornerColors[2].Y;
			pData[22] = CornerColors[2].Z;
			pData[23] = CornerColors[2].W;
			pData[24] = x + w;
			pData[25] = y + h;
			pData[26] = u1;
			pData[27] = v1;
			pData[28] = CornerColors[3].X;
			pData[29] = CornerColors[3].Y;
			pData[30] = CornerColors[3].Z;
			pData[31] = CornerColors[3].W;

			CurrPipeline.SetVertexData(new(pData), 4);
			Owner.Draw(4);
		}

		public bool IsActive { get; private set; }
		public IGL Owner { get; }

		private IPipeline CurrPipeline;
		private readonly IPipeline DefaultPipeline;

		// shaders are hand-coded for each platform to make sure they stay as fast as possible

		public const string DefaultVertexShader_d3d11 = @"
//vertex shader uniforms
float4x4 um44Modelview, um44Projection;
float4 uModulateColor;

struct VS_INPUT
{
	float2 aPosition : POSITION;
	float2 aTexcoord : TEXCOORD0;
	float4 aColor : TEXCOORD1;
};

struct VS_OUTPUT
{
	float4 vPosition : SV_POSITION;
	float2 vTexcoord0 : TEXCOORD0;
	float4 vCornerColor : COLOR0;
};

VS_OUTPUT vsmain(VS_INPUT src)
{
	VS_OUTPUT dst;
	float4 temp = float4(src.aPosition,0,1);
	dst.vPosition = mul(um44Projection,mul(um44Modelview,temp));
	dst.vTexcoord0 = src.aTexcoord;
	dst.vCornerColor = src.aColor * uModulateColor;
	return dst;
}
";

		public const string DefaultPixelShader_d3d11 = @"
//pixel shader uniforms
bool uSamplerEnable;
Texture2D texture0;
sampler uSampler0;

struct PS_INPUT
{
	float4 vPosition : SV_POSITION;
	float2 vTexcoord0 : TEXCOORD0;
	float4 vCornerColor : COLOR0;
};

float4 psmain(PS_INPUT src) : SV_Target
{
	float4 temp = src.vCornerColor;
	if(uSamplerEnable) temp *= texture0.Sample(uSampler0,src.vTexcoord0);
	return temp;
}
";

		public const string DefaultVertexShader_gl = @"
//opengl 3.2
#version 150
uniform mat4 um44Modelview, um44Projection;
uniform vec4 uModulateColor;

in vec2 aPosition;
in vec2 aTexcoord;
in vec4 aColor;

out vec2 vTexcoord0;
out vec4 vCornerColor;

void main()
{
	vec4 temp = vec4(aPosition,0,1);
	gl_Position = um44Projection * (um44Modelview * temp);
	vTexcoord0 = aTexcoord;
	vCornerColor = aColor * uModulateColor;
}";

		public const string DefaultPixelShader_gl = @"
//opengl 3.2
#version 150
uniform bool uSamplerEnable;
uniform sampler2D uSampler0;

in vec2 vTexcoord0;
in vec4 vCornerColor;

out vec4 FragColor;

void main()
{
	vec4 temp = vCornerColor;
	if(uSamplerEnable) temp *= texture(uSampler0,vTexcoord0);
	FragColor = temp;
}";

	}
}
