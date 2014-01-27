//http://stackoverflow.com/questions/6893302/decode-rgb-value-to-single-float-without-bit-shift-in-glsl

using System;
using sd=System.Drawing;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// A simple renderer useful for rendering GUI stuff. 
	/// When doing GUI rendering, run everything through here (if you need a GL feature not done through here, run it through here first)
	/// Call Begin, then draw, then End, and dont use other Renderers or GL calls in the meantime, unless you know what youre doing.
	/// This can perform batching (well.. maybe not yet), which is occasionally necessary for drawing large quantities of things.
	/// </summary>
	public class GuiRenderer : IDisposable
	{
		public GuiRenderer(IGL owner)
		{
			Owner = owner;

			VertexLayout = owner.CreateVertexLayout();
			VertexLayout.DefineVertexAttribute(0, 2, VertexAttribPointerType.Float, false, 16, 0);
			VertexLayout.DefineVertexAttribute(1, 2, VertexAttribPointerType.Float, false, 16, 8);
			VertexLayout.Close();

			_Projection = new MatrixStack();
			_Modelview = new MatrixStack();

			var vs = Owner.CreateVertexShader(DefaultVertexShader);
			var ps = Owner.CreateFragmentShader(DefaultPixelShader);
			CurrPipeline = DefaultPipeline = Owner.CreatePipeline(vs, ps);
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
			Owner.BindVertexLayout(VertexLayout);
			Owner.BindPipeline(CurrPipeline);

			//clear state cache
			sTexture = null;
			Modelview.Clear();
			Projection.Clear();
			SetModulateColorWhite();
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

		unsafe void DrawInternal(Texture2d tex, float x, float y, float w, float h)
		{
			Art art = new Art(null);
			art.Width = w;
			art.Height = h;
			art.u0 = art.v0 = 0;
			art.u1 = art.v1 = 1;
			art.BaseTexture = tex;
			DrawInternal(art,x,y,w,h,false,false);
		}

		unsafe void DrawInternal(Art art, float x, float y, float w, float h, bool fx, bool fy)
		{
			float u0,v0,u1,v1;
			if(fx) { u0 = art.u1; u1 = art.u0; }
			else { u0 = art.u0; u1 = art.u1; }
			if(fy) { v0 = art.v1; v1 = art.v0; }
			else { v0 = art.v0; v1 = art.v1; }

			float[] data = new float[16] {
			  x,y, u0,v0, 
			  x+art.Width,y, u1,v0,
			  x,y+art.Height, u0,v1,
			  x+art.Width,y+art.Height, u1,v1
			};

			Texture2d tex = art.BaseTexture;
			if(sTexture != tex)
				CurrPipeline["uSampler0"].Set(sTexture = tex);

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

			fixed (float* pData = &data[0])
			{
				Owner.BindArrayData(pData);
				Owner.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
			}
		}

		unsafe void DrawSubrectInternal(Texture2d tex, float x, float y, float w, float h, float u0, float v0, float u1, float v1)
		{
			float[] data = new float[16] {
			  x,y, u0,v0, 
			  x+w,y, u1,v0,
			  x,y+h, u0,v1,
			  x+w,y+h, u1,v1
			};

			if (sTexture != tex)
				CurrPipeline["uSampler0"].Set(sTexture = tex);

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

			fixed (float* pData = &data[0])
			{
				Owner.BindArrayData(pData);
				Owner.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
			}
		}
		
		public bool IsActive { get; private set; }
		public IGL Owner { get; private set; }

		VertexLayout VertexLayout;
		Pipeline CurrPipeline, DefaultPipeline;

		//state cache
		Texture2d sTexture;

		public readonly string DefaultVertexShader = @"
#version 110 //opengl 2.0 ~ 2004
uniform mat4 um44Modelview, um44Projection;

attribute vec2 aPosition;
attribute vec2 aTexcoord;

varying vec2 vTexcoord0;

void main()
{
    vec4 temp = vec4(aPosition,0,1);
		gl_Position = um44Projection * (um44Modelview * temp);
    vTexcoord0 = aTexcoord;
}";

		public readonly string DefaultPixelShader = @"
#version 110 //opengl 2.0 ~ 2004
uniform sampler2D uSampler0;
uniform vec4 uModulateColor;

varying vec2 vTexcoord0;

void main()
{
	vec4 temp = texture2D(uSampler0,vTexcoord0);
	temp *= uModulateColor;
	gl_FragColor = temp;
}";

	}
}