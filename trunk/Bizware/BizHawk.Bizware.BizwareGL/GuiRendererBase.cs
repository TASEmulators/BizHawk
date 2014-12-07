//http://stackoverflow.com/questions/6893302/decode-rgb-value-to-single-float-without-bit-shift-in-glsl

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using sd=System.Drawing;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL
{
	public class GDIPlusGuiRenderer : IGuiRenderer
	{
		public GDIPlusGuiRenderer(IGL owner)
		{
			Owner = owner;

			VertexLayout = owner.CreateVertexLayout();
			VertexLayout.DefineVertexAttribute("aPosition", 0, 2, VertexAttribPointerType.Float, false, 32, 0);
			VertexLayout.DefineVertexAttribute("aTexcoord", 1, 2, VertexAttribPointerType.Float, false, 32, 8);
			VertexLayout.DefineVertexAttribute("aColor", 2, 4, VertexAttribPointerType.Float, false, 32, 16);
			VertexLayout.Close();

			_Projection = new MatrixStack();
			_Modelview = new MatrixStack();

			var vs = Owner.CreateVertexShader(DefaultVertexShader,true);
			var ps = Owner.CreateFragmentShader(DefaultPixelShader, true);
			CurrPipeline = DefaultPipeline = Owner.CreatePipeline(VertexLayout, vs, ps, true);
		}

		OpenTK.Graphics.Color4[] CornerColors = new OpenTK.Graphics.Color4[4] {
			new OpenTK.Graphics.Color4(1.0f,1.0f,1.0f,1.0f),new OpenTK.Graphics.Color4(1.0f,1.0f,1.0f,1.0f),new OpenTK.Graphics.Color4(1.0f,1.0f,1.0f,1.0f),new OpenTK.Graphics.Color4(1.0f,1.0f,1.0f,1.0f)
		};

		/// <summary>
		/// Sets the specified corner color (for the gradient effect)
		/// </summary>
		public void SetCornerColor(int which, OpenTK.Graphics.Color4 color)
		{
			Flush(); //dont really need to flush with current implementation. we might as well roll modulate color into it too.
			CornerColors[which] = color;
		}

		/// <summary>
		/// Sets all four corner colors at once
		/// </summary>
		public void SetCornerColors(OpenTK.Graphics.Color4[] colors)
		{
			Flush(); //dont really need to flush with current implementation. we might as well roll modulate color into it too.
			if (colors.Length != 4) throw new ArgumentException("array must be size 4", "colors");
			for (int i = 0; i < 4; i++)
				CornerColors[i] = colors[i];
		}

		public void Dispose()
		{
			VertexLayout.Dispose();
			VertexLayout = null;
			DefaultPipeline.Dispose();
			DefaultPipeline = null;
		}

		/// <summary>
		/// Sets the pipeline for this GuiRenderer to use. We won't keep possession of it.
		/// This pipeline must work in certain ways, which can be discerned by inspecting the built-in one
		/// </summary>
		public void SetPipeline(Pipeline pipeline)
		{
			if (IsActive)
				throw new InvalidOperationException("Can't change pipeline while renderer is running!");

			Flush();
			CurrPipeline = pipeline;

			//clobber state cache
			sTexture = null;
			//save the modulate color? user beware, I guess, for now.
		}

		/// <summary>
		/// Restores the pipeline to the default
		/// </summary>
		public void SetDefaultPipeline()
		{
			SetPipeline(DefaultPipeline);
		}

		public void SetModulateColorWhite()
		{
			SetModulateColor(sd.Color.White);
		}

		public void SetModulateColor(sd.Color color)
		{
			Flush();
			CurrPipeline["uModulateColor"].Set(new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f));
		}

		public void SetBlendState(IBlendState rsBlend)
		{
			#if DEBUG
			BlendStateSet = true;
			#endif
			Flush();
			Owner.SetBlendState(rsBlend);
		}

		MatrixStack _Projection, _Modelview;
		public MatrixStack Projection
		{
			get { return _Projection; }
			set
			{
				_Projection = value;
				_Projection.IsDirty = true;
			}
		}
		public MatrixStack Modelview
		{
			get { return _Modelview; }
			set
			{
				_Modelview = value;
				_Modelview.IsDirty = true;
			}
		}

		public void Begin(sd.Size size) { Begin(size.Width, size.Height); }

		/// <summary>
		/// begin rendering, initializing viewport and projections to the given dimensions
		/// </summary>
		/// <param name="yflipped">Whether the matrices should be Y-flipped, for use with render targets</param>
		public void Begin(int width, int height, bool yflipped = false)
		{
			Begin();

			Projection = Owner.CreateGuiProjectionMatrix(width, height);
			Modelview = Owner.CreateGuiViewMatrix(width, height);

			if (yflipped)
			{
				//not sure this is the best way to do it. could be done in the view matrix creation
				Modelview.Scale(1, -1);
				Modelview.Translate(0, -height);
			}
			Owner.SetViewport(width, height);
		}

		/// <summary>
		/// Begins rendering
		/// </summary>
		public void Begin()
		{
			//uhhmmm I want to throw an exception if its already active, but its annoying.

			if(CurrPipeline == null)
				throw new InvalidOperationException("Pipeline hasn't been set!");
			
			IsActive = true;
			Owner.BindPipeline(CurrPipeline);

			//clear state cache
			sTexture = null;
			CurrPipeline["uSamplerEnable"].Set(false);
			Modelview.Clear();
			Projection.Clear();
			SetModulateColorWhite();

			#if DEBUG
				BlendStateSet = false;
			#endif
		}

		/// <summary>
		/// Use this, if you must do something sneaky to openGL without this GuiRenderer knowing.
		/// It might be faster than End and Beginning again, and certainly prettier
		/// </summary>
		public void Flush()
		{
			//no batching, nothing to do here yet
		}

		/// <summary>
		/// Ends rendering
		/// </summary>
		public void End()
		{
			if (!IsActive)
				throw new InvalidOperationException("GuiRenderer is not active!");
			IsActive = false;
		}

		public void RectFill(float x, float y, float w, float h)
		{
			PrepDrawSubrectInternal(null);
			EmitRectangleInternal(x, y, w, h, 0, 0, 0, 0);
		}

		/// <summary>
		/// Draws a subrectangle from the provided texture. For advanced users only
		/// </summary>
		public void DrawSubrect(Texture2d tex, float x, float y, float w, float h, float u0, float v0, float u1, float v1)
		{
			DrawSubrectInternal(tex, x, y, w, h, u0, v0, u1, v1);
		}

		/// <summary>
		/// draws the specified Art resource
		/// </summary>
		public void Draw(Art art) { DrawInternal(art, 0, 0, art.Width, art.Height, false, false); }

		/// <summary>
		/// draws the specified Art resource with the specified offset. This could be tricky if youve applied other rotate or scale transforms first.
		/// </summary>
		public void Draw(Art art, float x, float y) { DrawInternal(art, x, y, art.Width, art.Height, false, false); }

		/// <summary>
		/// draws the specified Art resource with the specified offset, with the specified size. This could be tricky if youve applied other rotate or scale transforms first.
		/// </summary>
		public void Draw(Art art, float x, float y, float width, float height) { DrawInternal(art, x, y, width, height, false, false); }

		/// <summary>
		/// draws the specified Art resource with the specified offset. This could be tricky if youve applied other rotate or scale transforms first.
		/// </summary>
		public void Draw(Art art, Vector2 pos) { DrawInternal(art, pos.X, pos.Y, art.Width, art.Height, false, false); }

		/// <summary>
		/// draws the specified texture2d resource.
		/// </summary>
		public void Draw(Texture2d tex) { DrawInternal(tex, 0, 0, tex.Width, tex.Height); }

		/// <summary>
		/// draws the specified texture2d resource.
		/// </summary>
		public void Draw(Texture2d tex, float x, float y) { DrawInternal(tex, x, y, tex.Width, tex.Height); }

		/// <summary>
		/// draws the specified Art resource with the given flip flags
		/// </summary>
		public void DrawFlipped(Art art, bool xflip, bool yflip) { DrawInternal(art, 0, 0, art.Width, art.Height, xflip, yflip); }

		public void Draw(Texture2d art, float x, float y, float width, float height)
		{
			DrawInternal(art, x, y, width, height);
		}

		unsafe void DrawInternal(Texture2d tex, float x, float y, float w, float h)
		{
			Art art = new Art(null);
			art.Width = w;
			art.Height = h;
			art.u0 = art.v0 = 0;
			art.u1 = art.v1 = 1;
			art.BaseTexture = tex;
			DrawInternal(art,x,y,w,h,false,tex.IsUpsideDown);
		}

		unsafe void DrawInternal(Art art, float x, float y, float w, float h, bool fx, bool fy)
		{
			float u0,v0,u1,v1;
			if(fx) { u0 = art.u1; u1 = art.u0; }
			else { u0 = art.u0; u1 = art.u1; }
			if(fy) { v0 = art.v1; v1 = art.v0; }
			else { v0 = art.v0; v1 = art.v1; }

			float[] data = new float[32] {
			  x,y, u0,v0, CornerColors[0].R, CornerColors[0].G, CornerColors[0].B, CornerColors[0].A,
			  x+art.Width,y, u1,v0, CornerColors[1].R, CornerColors[1].G, CornerColors[1].B, CornerColors[1].A,
			  x,y+art.Height, u0,v1, CornerColors[2].R, CornerColors[2].G, CornerColors[2].B, CornerColors[2].A,
			  x+art.Width,y+art.Height, u1,v1,  CornerColors[3].R, CornerColors[3].G, CornerColors[3].B, CornerColors[3].A,
			};

			Texture2d tex = art.BaseTexture;
			
			PrepDrawSubrectInternal(tex);

			fixed (float* pData = &data[0])
			{
				Owner.BindArrayData(pData);
				Owner.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
			}
		}

		unsafe void PrepDrawSubrectInternal(Texture2d tex)
		{
			if (sTexture != tex)
			{
				sTexture = tex;
				CurrPipeline["uSampler0"].Set(tex);
				if (sTexture == null)
				{
					CurrPipeline["uSamplerEnable"].Set(false);
				}
				else
				{
					CurrPipeline["uSamplerEnable"].Set(true);
				}
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

		unsafe void EmitRectangleInternal(float x, float y, float w, float h, float u0, float v0, float u1, float v1)
		{
			float* pData = stackalloc float[32];
			pData[0] = x;
			pData[1] = y;
			pData[2] = u0;
			pData[3] = v0;
			pData[4] = CornerColors[0].R;
			pData[5] = CornerColors[0].G;
			pData[6] = CornerColors[0].B;
			pData[7] = CornerColors[0].A;
			pData[8] = x + w;
			pData[9] = y;
			pData[10] = u1;
			pData[11] = v0;
			pData[12] = CornerColors[1].R;
			pData[13] = CornerColors[1].G;
			pData[14] = CornerColors[1].B;
			pData[15] = CornerColors[1].A;
			pData[16] = x;
			pData[17] = y + h;
			pData[18] = u0;
			pData[19] = v1;
			pData[20] = CornerColors[2].R;
			pData[21] = CornerColors[2].G;
			pData[22] = CornerColors[2].B;
			pData[23] = CornerColors[2].A;
			pData[24] = x + w;
			pData[25] = y + h;
			pData[26] = u1;
			pData[27] = v1;
			pData[28] = CornerColors[3].R;
			pData[29] = CornerColors[3].G;
			pData[30] = CornerColors[3].B;
			pData[31] = CornerColors[3].A;

			Owner.BindArrayData(pData);
			Owner.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

			#if DEBUG
			Debug.Assert(BlendStateSet);
			#endif
		}

		unsafe void DrawSubrectInternal(Texture2d tex, float x, float y, float w, float h, float u0, float v0, float u1, float v1)
		{
			PrepDrawSubrectInternal(tex);
			EmitRectangleInternal(x, y, w, h, u0, v0, u1, v1);
		}
		
		public bool IsActive { get; private set; }
		public IGL Owner { get; private set; }

		VertexLayout VertexLayout;
		Pipeline CurrPipeline, DefaultPipeline;

		//state cache
		Texture2d sTexture;
		#if DEBUG
			bool BlendStateSet;
		#endif

		public readonly string DefaultVertexShader = @"
#version 110 //opengl 2.0 ~ 2004
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

		public readonly string DefaultPixelShader = @"
#version 110 //opengl 2.0 ~ 2004
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