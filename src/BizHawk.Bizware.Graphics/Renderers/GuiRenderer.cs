//http://stackoverflow.com/questions/6893302/decode-rgb-value-to-single-float-without-bit-shift-in-glsl

using System;
using System.Drawing;
using System.Numerics;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// A simple renderer useful for rendering GUI stuff.
	/// When doing GUI rendering, run everything through here (if you need a GL feature not done through here, run it through here first)
	/// Call Begin, then draw, then End, and don't use other Renderers or GL calls in the meantime, unless you know what you're doing.
	/// This can perform batching (well.. maybe not yet), which is occasionally necessary for drawing large quantities of things.
	/// </summary>
	public class GuiRenderer : IDisposable, IGuiRenderer
	{
		public GuiRenderer(IGL owner)
		{
			Owner = owner;

			VertexLayout = owner.CreateVertexLayout();
			VertexLayout.DefineVertexAttribute("aPosition", 0, 2, VertexAttribPointerType.Float, AttribUsage.Position, false, 32);
			VertexLayout.DefineVertexAttribute("aTexcoord", 1, 2, VertexAttribPointerType.Float, AttribUsage.Texcoord0, false, 32, 8);
			VertexLayout.DefineVertexAttribute("aColor", 2, 4, VertexAttribPointerType.Float, AttribUsage.Texcoord1, false, 32, 16);
			VertexLayout.Close();

			_Projection = new();
			_Modelview = new();

			string psProgram, vsProgram;

			switch (owner.API)
			{
				case "D3D11":
					vsProgram = DefaultShader_d3d9;
					psProgram = DefaultShader_d3d9;
					break;
				case "OPENGL":
					vsProgram = DefaultVertexShader_gl;
					psProgram = DefaultPixelShader_gl;
					break;
				default:
					throw new InvalidOperationException();
			}

			var vs = Owner.CreateVertexShader(vsProgram, "vsmain", true);
			var ps = Owner.CreateFragmentShader(psProgram, "psmain", true);
			CurrPipeline = DefaultPipeline = Owner.CreatePipeline(VertexLayout, vs, ps, true, "xgui");
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
			Flush(); //don't really need to flush with current implementation. we might as well roll modulate color into it too.
			CornerColors[which] = color;
		}

		/// <exception cref="ArgumentException"><paramref name="colors"/> does not have exactly <c>4</c> elements</exception>
		public void SetCornerColors(Vector4[] colors)
		{
			Flush(); //don't really need to flush with current implementation. we might as well roll modulate color into it too.
			if (colors.Length != 4) throw new ArgumentException("array must be size 4", nameof(colors));
			for (var i = 0; i < 4; i++)
			{
				CornerColors[i] = colors[i];
			}
		}

		public void Dispose()
		{
			DefaultPipeline.Dispose();
			VertexLayout.Release();
		}

		/// <exception cref="InvalidOperationException"><see cref="IsActive"/> is <see langword="true"/></exception>
		public void SetPipeline(Pipeline pipeline)
		{
			if (IsActive)
				throw new InvalidOperationException("Can't change pipeline while renderer is running!");

			Flush();
			CurrPipeline = pipeline;

			// clobber state cache
			sTexture = null;
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
			CurrPipeline["uModulateColor"].Set(new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f));
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

		private MatrixStack _Projection, _Modelview;
		public MatrixStack Projection
		{
			get => _Projection;
			set
			{
				_Projection = value;
				_Projection.IsDirty = true;
			}
		}
		public MatrixStack Modelview
		{
			get => _Modelview;
			set
			{
				_Modelview = value;
				_Modelview.IsDirty = true;
			}
		}

		public void Begin(Size size)
		{
			Begin(size.Width, size.Height);
		}

		public void Begin(int width, int height)
		{
			Begin();

			Projection = Owner.CreateGuiViewMatrix(width, height) * Owner.CreateGuiProjectionMatrix(width, height);
			Modelview.Clear();

			Owner.SetViewport(width, height);
		}

		/// <exception cref="InvalidOperationException">no pipeline set (need to call <see cref="SetPipeline"/>)</exception>
		public void Begin()
		{
			// uhhmmm I want to throw an exception if its already active, but its annoying.

			if (CurrPipeline == null)
			{
				throw new InvalidOperationException("Pipeline hasn't been set!");
			}

			IsActive = true;
			Owner.BindPipeline(CurrPipeline);
			Owner.DisableBlending();

			//clear state cache
			sTexture = null;
			CurrPipeline["uSamplerEnable"].Set(false);
			Modelview.Clear();
			Projection.Clear();
			SetModulateColorWhite();
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

		public void RectFill(float x, float y, float w, float h)
		{
			PrepDrawSubrectInternal(null);
			EmitRectangleInternal(x, y, w, h, 0, 0, 0, 0);
		}


		public void DrawSubrect(Texture2d tex, float x, float y, float w, float h, float u0, float v0, float u1, float v1)
		{
			DrawSubrectInternal(tex, x, y, w, h, u0, v0, u1, v1);
		}

		public void Draw(Art art)
		{
			DrawInternal(art, 0, 0, art.Width, art.Height, false, false);
		}

		public void Draw(Art art, float x, float y)
		{
			DrawInternal(art, x, y, art.Width, art.Height, false, false);
		}

		public void Draw(Art art, float x, float y, float width, float height)
		{
			DrawInternal(art, x, y, width, height, false, false);
		}

		public void Draw(Art art, Vector2 pos)
		{
			DrawInternal(art, pos.X, pos.Y, art.Width, art.Height, false, false);
		}

		public void Draw(Texture2d tex)
		{
			DrawInternal(tex, 0, 0, tex.Width, tex.Height);
		}

		public void Draw(Texture2d tex, float x, float y)
		{
			DrawInternal(tex, x, y, tex.Width, tex.Height);
		}

		public void DrawFlipped(Art art, bool xflip, bool yflip)
		{
			DrawInternal(art, 0, 0, art.Width, art.Height, xflip, yflip);
		}

		public void Draw(Texture2d art, float x, float y, float width, float height)
		{
			DrawInternal(art, x, y, width, height);
		}

		private void DrawInternal(Texture2d tex, float x, float y, float w, float h)
		{
			var art = new Art((ArtManager)null)
			{
				Width = w,
				Height = h
			};

			art.u0 = art.v0 = 0;
			art.u1 = art.v1 = 1;
			art.BaseTexture = tex;

			DrawInternal(art, x, y, w, h, false, tex.IsUpsideDown);
		}

		private unsafe void DrawInternal(Art art, float x, float y, float w, float h, bool fx, bool fy)
		{
			float u0, v0, u1, v1;

			if (fx)
			{
				u0 = art.u1;
				u1 = art.u0;
			}
			else
			{
				u0 = art.u0;
				u1 = art.u1;
			}

			if (fy)
			{
				v0 = art.v1;
				v1 = art.v0;
			}
			else
			{
				v0 = art.v0;
				v1 = art.v1;
			}

			var data = stackalloc float[32]
			{
				x, y, u0, v0,
				CornerColors[0].X, CornerColors[0].Y, CornerColors[0].Z, CornerColors[0].W,
				x + w, y, u1, v0,
				CornerColors[1].X, CornerColors[1].Y, CornerColors[1].Z, CornerColors[1].W,
				x, y + h, u0, v1,
				CornerColors[2].X, CornerColors[2].Y, CornerColors[2].Z, CornerColors[2].W,
				x + w, y + h, u1, v1,
				CornerColors[3].X, CornerColors[3].Y, CornerColors[3].Z, CornerColors[3].W,
			};

			PrepDrawSubrectInternal(art.BaseTexture);
			Owner.Draw(new(data), 4);
		}

		private void PrepDrawSubrectInternal(Texture2d tex)
		{
			if (sTexture != tex)
			{
				sTexture = tex;
				CurrPipeline["uSampler0"].Set(tex);
				CurrPipeline["uSamplerEnable"].Set(tex != null);
			}

			if (_Projection.IsDirty)
			{
				CurrPipeline["um44Projection"].Set(ref _Projection.Top);
				_Projection.IsDirty = false;
			}
			if (_Modelview.IsDirty)
			{
				CurrPipeline["um44Modelview"].Set(ref _Modelview.Top);
				_Modelview.IsDirty = false;
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

			Owner.Draw(new(pData), 4);
		}

		private void DrawSubrectInternal(Texture2d tex, float x, float y, float w, float h, float u0, float v0, float u1, float v1)
		{
			PrepDrawSubrectInternal(tex);
			EmitRectangleInternal(x, y, w, h, u0, v0, u1, v1);
		}

		public bool IsActive { get; private set; }
		public IGL Owner { get; }

		private readonly VertexLayout VertexLayout;
		private Pipeline CurrPipeline;
		private readonly Pipeline DefaultPipeline;

		// state cache
		private Texture2d sTexture;

		// shaders are hand-coded for each platform to make sure they stay as fast as possible

#if false // this doesn't work for reasons unknown (TODO make this work)
		public const string DefaultShader_d3d11 = @"
//vertex shader uniforms
float4x4 um44Modelview, um44Projection;
float4 uModulateColor;

//pixel shader uniforms
bool uSamplerEnable;
Texture2D texture0, texture1;
SamplerState uSampler0 = sampler_state { Texture = (texture0); };

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

struct PS_INPUT
{
	float4 vPosition : SV_POSITION;
	float2 vTexcoord0 : TEXCOORD0;
	float4 vCornerColor : COLOR0;
};

VS_OUTPUT vsmain(VS_INPUT src)
{
	VS_OUTPUT dst;
	dst.vPosition = float4(src.aPosition,0,1);
	dst.vTexcoord0 = src.aTexcoord;
	dst.vCornerColor = src.aColor;
	return dst;
}

float4 psmain(PS_INPUT src) : SV_TARGET
{
	float4 temp = src.vCornerColor;
	temp *= texture0.Sample(uSampler0,src.vTexcoord0);
	return temp;
}
";
#endif

		public const string DefaultShader_d3d9 = @"
//vertex shader uniforms
float4x4 um44Modelview, um44Projection;
float4 uModulateColor;

//pixel shader uniforms
bool uSamplerEnable;
texture2D texture0, texture1;
sampler uSampler0 = sampler_state { Texture = (texture0); };

struct VS_INPUT
{
	float2 aPosition : POSITION;
	float2 aTexcoord : TEXCOORD0;
	float4 aColor : TEXCOORD1;
};

struct VS_OUTPUT
{
	float4 vPosition : POSITION;
	float2 vTexcoord0 : TEXCOORD0;
	float4 vCornerColor : COLOR0;
};

struct PS_INPUT
{
	float4 vPosition : POSITION;
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

float4 psmain(PS_INPUT src) : COLOR
{
	float4 temp = src.vCornerColor;
	if(uSamplerEnable) temp *= tex2D(uSampler0,src.vTexcoord0);
	return temp;
}
";

		public const string DefaultVertexShader_gl = @"
//opengl 2.0 ~ 2004
#version 110
uniform mat4 um44Modelview, um44Projection;
uniform vec4 uModulateColor;

attribute vec2 aPosition;
attribute vec2 aTexcoord;
attribute vec4 aColor;

varying vec2 vTexcoord0;
varying vec4 vCornerColor;

void main()
{
	vec4 temp = vec4(aPosition,0,1);
	gl_Position = um44Projection * (um44Modelview * temp);
	vTexcoord0 = aTexcoord;
	vCornerColor = aColor * uModulateColor;
}";

		public const string DefaultPixelShader_gl = @"
//opengl 2.0 ~ 2004
#version 110
uniform bool uSamplerEnable;
uniform sampler2D uSampler0;

varying vec2 vTexcoord0;
varying vec4 vCornerColor;

void main()
{
	vec4 temp = vCornerColor;
	if(uSamplerEnable) temp *= texture2D(uSampler0,vTexcoord0);
	gl_FragColor = temp;
}";

	}
}