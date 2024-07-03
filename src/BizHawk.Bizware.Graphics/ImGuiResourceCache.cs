using System.Collections.Generic;
using System.Drawing;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Shared resource cache for the ImGui 2D renderer
	/// This allows multiple ImGui renderers to share the same cache
	/// </summary>
	public sealed class ImGuiResourceCache
	{
		private readonly IGL _igl;
		private ITexture2D _lastTexture;

		internal readonly IPipeline Pipeline;
		internal readonly Dictionary<Bitmap, ITexture2D> TextureCache = new();
		internal readonly Dictionary<Color, SolidBrush> BrushCache = new();

		public ImGuiResourceCache(IGL igl)
		{
			_igl = igl;
			if (igl.DispMethodEnum is EDispMethod.OpenGL or EDispMethod.D3D11)
			{
				string psProgram, vsProgram;

				// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
				switch (igl.DispMethodEnum)
				{
					case EDispMethod.D3D11:
						vsProgram = ImGuiVertexShader_d3d11;
						psProgram = ImGuiPixelShader_d3d11;
						break;
					case EDispMethod.OpenGL:
						vsProgram = ImGuiVertexShader_gl;
						psProgram = ImGuiPixelShader_gl;
						break;
					default:
						throw new InvalidOperationException();
				}

				var vertexLayoutItems = new PipelineCompileArgs.VertexLayoutItem[3];
				vertexLayoutItems[0] = new("aPosition", 2, 0, AttribUsage.Position);
				vertexLayoutItems[1] = new("aTexcoord", 2, 8, AttribUsage.Texcoord0);
				vertexLayoutItems[2] = new("aColor", 4, 16, AttribUsage.Color0, Integer: true);

				var compileArgs = new PipelineCompileArgs(
					vertexLayoutItems,
					vertexShaderArgs: new(vsProgram, "vsmain"),
					fragmentShaderArgs: new(psProgram, "psmain"),
					fragmentOutputName: "FragColor");
				Pipeline = igl.CreatePipeline(compileArgs);

				igl.BindPipeline(Pipeline);
				Pipeline.SetUniform("uSamplerEnable", false);
				Pipeline.SetUniformSampler("uSampler0", null);
				igl.BindPipeline(null);
			}
		}

		internal void SetProjection(int width, int height)
		{
			var projection = _igl.CreateGuiViewMatrix(width, height) * _igl.CreateGuiProjectionMatrix(width, height);
			Pipeline.SetUniformMatrix("um44Projection", projection);
		}

		internal void SetTexture(ITexture2D texture2D)
		{
			if (_lastTexture != texture2D)
			{
				Pipeline.SetUniform("uSamplerEnable", texture2D != null);
				Pipeline.SetUniformSampler("uSampler0", texture2D);
				_lastTexture = texture2D;
			}
		}

		public void Dispose()
		{
			foreach (var cache in TextureCache)
			{
				cache.Key.Dispose();
				cache.Value.Dispose();
			}

			foreach (var cachedBrush in BrushCache.Values)
			{
				cachedBrush.Dispose();
			}

			TextureCache.Clear();
			BrushCache.Clear();
			Pipeline?.Dispose();
		}

		public const string ImGuiVertexShader_d3d11 = @"
//vertex shader uniforms
float4x4 um44Projection;

struct VS_INPUT
{
	float2 aPosition : POSITION;
	float2 aTexcoord : TEXCOORD0;
	float4 aColor : COLOR0;
};

struct VS_OUTPUT
{
	float4 vPosition : SV_POSITION;
	float2 vTexcoord0 : TEXCOORD0;
	float4 vColor0 : COLOR0;
};

VS_OUTPUT vsmain(VS_INPUT src)
{
	VS_OUTPUT dst;
	float4 temp = float4(src.aPosition,0,1);
	dst.vPosition = mul(um44Projection, temp);
	dst.vTexcoord0 = src.aTexcoord;
	dst.vColor0 = src.aColor;
	return dst;
}
";

		public const string ImGuiPixelShader_d3d11 = @"
//pixel shader uniforms
bool uSamplerEnable;
Texture2D uTexture0;
sampler uSampler0;

struct PS_INPUT
{
	float4 vPosition : SV_POSITION;
	float2 vTexcoord0 : TEXCOORD0;
	float4 vColor0 : COLOR0;
};

float4 psmain(PS_INPUT src) : SV_Target
{
	float4 temp = src.vColor0;
	if(uSamplerEnable) temp *= uTexture0.Sample(uSampler0,src.vTexcoord0);
	return temp;
}
";

		public const string ImGuiVertexShader_gl = @"
//opengl 3.2
#version 150
uniform mat4 um44Projection;

in vec2 aPosition;
in vec2 aTexcoord;
in vec4 aColor;

out vec2 vTexcoord0;
out vec4 vColor0;

void main()
{
	vec4 temp = vec4(aPosition,0,1);
	gl_Position = um44Projection * temp;
	vTexcoord0 = aTexcoord;
	vColor0 = aColor;
}";

		public const string ImGuiPixelShader_gl = @"
//opengl 3.2
#version 150
uniform bool uSamplerEnable;
uniform sampler2D uSampler0;

in vec2 vTexcoord0;
in vec4 vColor0;

out vec4 FragColor;

void main()
{
	vec4 temp = vColor0;
	if(uSamplerEnable) temp *= texture(uSampler0, vTexcoord0);
	FragColor = temp;
}";
	}
}
