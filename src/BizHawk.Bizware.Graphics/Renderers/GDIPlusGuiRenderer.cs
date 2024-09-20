// this is full of bugs probably, related to state from old rendering sessions being all messed up. its only barely good enough to work at all

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;

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
			=> CornerColors[which] = color;

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
			=> CurrentImageAttributes?.Dispose();

		public void SetPipeline(IPipeline pipeline)
		{
		}

		public void SetDefaultPipeline()
		{
		}

		public void SetModulateColorWhite()
			=> SetModulateColor(Color.White);

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

		public void EnableBlending()
			=> Owner.EnableBlending();

		public void DisableBlending()
			=> Owner.DisableBlending();

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

		public void Begin(int width, int height)
		{
			// uhhmmm I want to throw an exception if its already active, but its annoying.
			IsActive = true;
			Owner.DisableBlending();

			CurrentImageAttributes?.Dispose();
			CurrentImageAttributes = new();
			ModelView?.Clear();
			Projection?.Clear();

			Projection = Owner.CreateGuiProjectionMatrix(width, height);
			ModelView = Owner.CreateGuiViewMatrix(width, height);
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

		public void DrawSubrect(ITexture2D tex, float x, float y, float w, float h, float u0, float v0, float u1, float v1)
		{
			var g = _gdi.GetCurrentGraphics();

			var tex2d = (GDIPlusTexture2D)tex;
			// TODO - we can support bicubic for the final presentation...
			g.InterpolationMode = tex2d.LinearFiltering ? InterpolationMode.Bilinear : InterpolationMode.NearestNeighbor;

			SetupMatrix(g);

			PointF[] destPoints =
			{
				new(x, y),
				new(x + w, y),
				new(x, y + h),
			};

			var x0 = u0 * tex.Width;
			var y0 = v0 * tex.Height;
			var x1 = u1 * tex.Width;
			var y1 = v1 * tex.Height;

			g.PixelOffsetMode = PixelOffsetMode.Half;
			g.DrawImage(tex2d.SDBitmap, destPoints, new(x0, y0, x1 - x0, y1 - y0), GraphicsUnit.Pixel, CurrentImageAttributes);
			g.Transform = new(); // .Reset() doesnt work?
		}

		private void SetupMatrix(SDGraphics g)
		{
			// projection is always identity, so who cares i guess
			// var mat = Projection.Top * Modelview.Top;
			var mat = ModelView.Top;
			g.Transform = new(mat.M11, mat.M12, mat.M21, mat.M22, mat.M41, mat.M42);
		}

		public bool IsActive { get; private set; }

		public IGL Owner => _gdi;
	}
}