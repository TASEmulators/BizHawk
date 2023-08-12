// this is full of bugs probably, related to state from old rendering sessions being all messed up. its only barely good enough to work at all

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;

using BizHawk.Bizware.BizwareGL;

using SDGraphics = System.Drawing.Graphics;

namespace BizHawk.Bizware.Graphics
{
	public class GDIPlusGuiRenderer : IGuiRenderer
	{
		private readonly IGL_GDIPlus _gdi;

		public GDIPlusGuiRenderer(IGL_GDIPlus gdi)
			=> _gdi = gdi;

		private readonly Vector4[] CornerColors =
		{
			new(1.0f, 1.0f, 1.0f, 1.0f),
			new(1.0f, 1.0f, 1.0f, 1.0f),
			new(1.0f, 1.0f, 1.0f, 1.0f),
			new(1.0f, 1.0f, 1.0f, 1.0f)
		};

		public void SetCornerColor(int which, Vector4 color)
		{
			CornerColors[which] = color;
		}

		/// <exception cref="ArgumentException"><paramref name="colors"/> does not have exactly <c>4</c> elements</exception>
		public void SetCornerColors(Vector4[] colors)
		{
			Flush(); //don't really need to flush with current implementation. we might as well roll modulate color into it too.

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
		{
			CurrentImageAttributes?.Dispose();
		}

		public void SetPipeline(Pipeline pipeline)
		{
		}

		public void SetDefaultPipeline()
		{
		}

		public void SetModulateColorWhite()
		{
			SetModulateColor(Color.White);
		}

		private ImageAttributes CurrentImageAttributes;

		public void SetModulateColor(Color color)
		{
			// white is really no color at all
			if (color.ToArgb() == Color.White.ToArgb())
			{
				CurrentImageAttributes.ClearColorMatrix(ColorAdjustType.Bitmap);
				return;
			}

			var r = color.R / 255.0f;
			var g = color.G / 255.0f;
			var b = color.B / 255.0f;
			var a = color.A / 255.0f;

			float[][] colorMatrixElements =
			{
				new[] { r, 0, 0, 0, 0 },
				new[] { 0, g, 0, 0, 0 },
				new[] { 0, 0, b, 0, 0 },
				new[] { 0, 0, 0, a, 0 },
				new float[] { 0, 0, 0, 0, 1 },
			};

			var colorMatrix = new ColorMatrix(colorMatrixElements);
			CurrentImageAttributes.SetColorMatrix(colorMatrix,ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
		}

		private IBlendState CurrentBlendState;

		public void SetBlendState(IBlendState rsBlend)
		{
			CurrentBlendState = rsBlend;
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

			CurrentBlendState = _gdi.BlendNormal;

			Projection = Owner.CreateGuiProjectionMatrix(width, height);
			Modelview = Owner.CreateGuiViewMatrix(width, height);
		}

		public void Begin()
		{
			// uhhmmm I want to throw an exception if its already active, but its annoying.
			IsActive = true;
			CurrentImageAttributes = new();
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
				throw new InvalidOperationException($"{nameof(GDIPlusGuiRenderer)} is not active!");
			}

			IsActive = false;
			CurrentImageAttributes?.Dispose();
			CurrentImageAttributes = null;
		}

		public void RectFill(float x, float y, float w, float h)
		{
		}

		public void DrawSubrect(Texture2d tex, float x, float y, float w, float h, float u0, float v0, float u1, float v1)
		{
			var gtex = (GDIPlusTexture)tex.Opaque;
			var g = _gdi.GetCurrentGraphics();

			PrepDraw(g, tex);
			SetupMatrix(g);

			var x0 = u0 * tex.Width;
			var y0 = v0 * tex.Height;
			var x1 = u1 * tex.Width;
			var y1 = v1 * tex.Height;

			PointF[] destPoints =
			{
				new(x, y),
				new(x+w, y),
				new(x, y+h),
			};

			g.DrawImage(gtex.SDBitmap, destPoints, new(x0, y0, x1 - x0, y1 - y0), GraphicsUnit.Pixel, CurrentImageAttributes);
			g.Transform = new(); // .Reset() doesnt work?
		}

		public void Draw(Art art) { DrawInternal(art, 0, 0, art.Width, art.Height, false, false); }
		public void Draw(Art art, float x, float y) { DrawInternal(art, x, y, art.Width, art.Height, false, false); }
		public void Draw(Art art, float x, float y, float width, float height) { DrawInternal(art, x, y, width, height, false, false); }
		public void Draw(Art art, Vector2 pos) { DrawInternal(art, pos.X, pos.Y, art.Width, art.Height, false, false); }
		public void Draw(Texture2d tex) { DrawInternal(tex, 0, 0, tex.Width, tex.Height); }
		public void Draw(Texture2d tex, float x, float y) { DrawInternal(tex, x, y, tex.Width, tex.Height); }
		public void DrawFlipped(Art art, bool xflip, bool yflip) { DrawInternal(art, 0, 0, art.Width, art.Height, xflip, yflip); }

		public void Draw(Texture2d art, float x, float y, float width, float height)
		{
			DrawInternal(art, x, y, width, height);
		}

		private void PrepDraw(SDGraphics g, Texture2d tex)
		{
			var tw = (GDIPlusTexture)tex.Opaque;

			// TODO - we can support bicubic for the final presentation...
			if ((int)tw.MagFilter != (int)tw.MinFilter)
			{
				throw new InvalidOperationException($"{nameof(tw)}.{nameof(tw.MagFilter)} != {nameof(tw)}.{nameof(tw.MinFilter)}");
			}

			g.InterpolationMode = tw.MagFilter switch
			{
				TextureMagFilter.Linear => InterpolationMode.Bilinear,
				TextureMagFilter.Nearest => InterpolationMode.NearestNeighbor,
				_ => g.InterpolationMode
			};

			if (CurrentBlendState == _gdi.BlendNormal)
			{
				g.CompositingMode = CompositingMode.SourceOver;
				g.CompositingQuality = CompositingQuality.Default; // ?
			}
			else
			// if (CurrentBlendState == Gdi.BlendNoneCopy)
			// if (CurrentBlendState == Gdi.BlendNoneOpaque)
			{
				g.CompositingMode = CompositingMode.SourceCopy;
				g.CompositingQuality = CompositingQuality.HighSpeed;

				// WARNING : DO NOT USE COLOR MATRIX TO WIPE THE ALPHA
				// ITS SOOOOOOOOOOOOOOOOOOOOOOOOOOOO SLOW
				// instead, we added kind of hacky support for 24bpp images
			}
		}

		private void SetupMatrix(SDGraphics g)
		{
			// projection is always identity, so who cares i guess
			// var mat = Projection.Top * Modelview.Top;
			var mat = Modelview.Top;
			g.Transform = new(mat.M11, mat.M12, mat.M21, mat.M22, mat.M41, mat.M42);
		}

		private void DrawInternal(Art art, float x, float y, float w, float h)
		{
			DrawInternal(art.BaseTexture, x, y, w, h, art.u0, art.v0, art.u1, art.v1);
		}

		private void DrawInternal(Texture2d tex, float x, float y, float w, float h)
		{
			DrawInternal(tex, x, y, w, h, 0, 0, 1, 1);
		}

		private void DrawInternal(Texture2d tex, float x, float y, float w, float h, float u0, float v0, float u1, float v1)
		{
			var g = _gdi.GetCurrentGraphics();
			PrepDraw(g, tex);

			SetupMatrix(g);

			PointF[] destPoints =
			{
				new(x, y),
				new(x+w, y),
				new(x, y+h),
			};

			var sx = tex.Width * u0;
			var sy = tex.Height * v0;
			var sx2 = tex.Width * u1;
			var sy2 = tex.Height * v1;
			var sw = sx2 - sx;
			var sh = sy2 - sy;

			var gtex = (GDIPlusTexture)tex.Opaque;
			g.PixelOffsetMode = PixelOffsetMode.Half;
			g.DrawImage(gtex.SDBitmap, destPoints, new(sx, sy, sw, sh), GraphicsUnit.Pixel, CurrentImageAttributes);
			g.Transform = new(); // .Reset() doesn't work ? ?
		}

		private static void DrawInternal(Art art, float x, float y, float w, float h, bool fx, bool fy)
		{
		}

		public bool IsActive { get; private set; }

		public IGL Owner => _gdi;
	}
}