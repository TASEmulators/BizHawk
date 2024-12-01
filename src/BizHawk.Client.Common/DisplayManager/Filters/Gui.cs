using System.Drawing;
using System.Numerics;

using BizHawk.Bizware.Graphics;
using BizHawk.Client.Common.FilterManager;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;

namespace BizHawk.Client.Common.Filters
{
	/// <summary>
	/// applies letterboxing logic to figure out how to fit the source dimensions into the target dimensions.
	/// In the future this could also apply rules like integer-only scaling, etc.
	/// </summary>
	public class LetterboxingLogic
	{
		/// <summary>
		/// the location within the destination region of the output content (scaled and translated)
		/// </summary>
		public int vx, vy, vw, vh;

		/// <summary>
		/// the scale factor eventually used
		/// </summary>
		public float WidthScale, HeightScale;

		/// <summary>
		/// In case you want to do it yourself
		/// </summary>
		public LetterboxingLogic()
		{
		}

		// do maths on the viewport and the native resolution and the user settings to get a display rectangle
		public LetterboxingLogic(bool maintainAspect, bool maintainInteger, int targetWidth, int targetHeight, int sourceWidth, int sourceHeight, Size textureSize, Size virtualSize)
		{
			var textureWidth = textureSize.Width;
			var textureHeight = textureSize.Height;
			var virtualWidth = virtualSize.Width;
			var virtualHeight = virtualSize.Height;

			// zero 02-jun-2014 - we passed these in, but ignored them. kind of weird..
			var oldSourceWidth = sourceWidth;
			var oldSourceHeight = sourceHeight;
			sourceWidth = virtualWidth;
			sourceHeight = virtualHeight;

			// this doesn't make sense
			if (!maintainAspect)
			{
				maintainInteger = false;
			}

			var widthScale = (float)targetWidth / sourceWidth;
			var heightScale = (float)targetHeight / sourceHeight;

			if (maintainAspect
				// zero 20-jul-2014 - hacks upon hacks, this function needs rewriting
				&& !maintainInteger
				)
			{
				if (widthScale > heightScale) widthScale = heightScale;
				if (heightScale > widthScale) heightScale = widthScale;
			}

			if (maintainInteger)
			{
				// just totally different code
				// apply the zooming algorithm (pasted and reworked, for now)
				// ALERT COPYPASTE LAUNDROMAT

				var AR = new Vector2(virtualWidth / (float) textureWidth, virtualHeight / (float) textureHeight);
				var targetPar = AR.X / AR.Y;
				var PS = Vector2.One; // this would malfunction for AR <= 0.5 or AR >= 2.0

				Span<Vector2> trials = stackalloc Vector2[3];
				Span<bool> trialsLimited = stackalloc bool[3];
				while (true)
				{
					// TODO - would be good not to run this per frame....
					trials[0] = PS + Vector2.UnitX;
					trials[1] = PS + Vector2.UnitY;
					trials[2] = PS + Vector2.One;

					var bestIndex = -1;
					var bestValue = 1000.0f;
					for (var t = 0; t < trials.Length; t++)
					{
						var vTrial = trials[t];
						trialsLimited[t] = false;

						//check whether this is going to exceed our allotted area
						var testVw = (int)(vTrial.X * textureWidth);
						var testVh = (int)(vTrial.Y * textureHeight);
						if (testVw > targetWidth) trialsLimited[t] = true;
						if (testVh > targetHeight) trialsLimited[t] = true;

						// I.
						var testAr = vTrial.X / vTrial.Y;

						// II.
						// var calc = Vector2.Multiply(trials[t], VS);
						// var test_ar = calc.X / calc.Y;

						// not clear which approach is superior

						var deviationLinear = Math.Abs(testAr - targetPar);
						if (deviationLinear < bestValue)
						{
							bestIndex = t;
							bestValue = deviationLinear;
						}
					}

					// last result was best, so bail out
					if (bestIndex == -1)
					{
						break;
					}

					// if the winner ran off the edge, bail out
					if (trialsLimited[bestIndex])
					{
						break;
					}

					PS = trials[bestIndex];
				}

				// "fix problems with gameextrapadding in >1x window scales" (other edits were made, maybe theyre whats important)
				// vw = (int)(PS.X * oldSourceWidth);
				// vh = (int)(PS.Y * oldSourceHeight);
				vw = (int)(PS.X * sourceWidth);
				vh = (int)(PS.Y * sourceHeight);
#if false
				widthScale = PS.X;
				heightScale = PS.Y;
#endif
			}
			else
			{
				vw = (int)(widthScale * sourceWidth);
				vh = (int)(heightScale * sourceHeight);
			}

			// there's only one sensible way to letterbox in case we're shrinking a dimension: "pan & scan" to the center
			// this is unlikely to be what the user wants except in the one case of maybe shrinking off some overscan area
			// instead, since we're more about biz than gaming, lets shrink the view to fit in the small dimension
			if (targetWidth < vw)
			{
				vw = targetWidth;
			}

			if (targetHeight < vh)
			{
				vh = targetHeight;
			}

			// determine letterboxing parameters
			vx = (targetWidth - vw) / 2;
			vy = (targetHeight - vh) / 2;

			// zero 09-oct-2014 - changed this for TransformPoint. scenario: basic 1x (but system-specified AR) NES window.
			// vw would be 293 but WidthScale would be 1.0. I think it should be something different.
			// FinalPresentation doesn't use the LL.WidthScale, so this is unlikely to be breaking anything old that depends on it
#if false
			WidthScale = widthScale;
			HeightScale = heightScale;
#else
			WidthScale = (float)vw / oldSourceWidth;
			HeightScale = (float)vh / oldSourceHeight;
#endif
		}
	}

	/// <summary>
	/// special screen control features for NDS
	/// </summary>
	public class ScreenControlNDS : BaseFilter
	{
		private readonly NDS _nds;

		public ScreenControlNDS(NDS nds)
		{
			_nds = nds;
		}

		public override Vector2 UntransformPoint(string channel, Vector2 point)
		{
			var ret = _nds.GetTouchCoords((int)point.X, (int)point.Y);
			var vp = _nds.AsVideoProvider();
			ret.X *= vp.BufferWidth / 256.0f;
			ret.Y *= vp.BufferHeight / 192.0f;
			return ret;
		}

		public override Vector2 TransformPoint(string channel, Vector2 point)
			=> _nds.GetScreenCoords(point.X, point.Y);
	}

	/// <summary>
	/// special screen control features for 3DS
	/// in practice, this is only for correcting mouse input
	/// as the core internally handles screen drawing madness anyways
	/// </summary>
	public class ScreenControl3DS : BaseFilter
	{
		private readonly Encore _encore;

		public ScreenControl3DS(Encore encore)
		{
			_encore = encore;
		}

		public override Vector2 UntransformPoint(string channel, Vector2 point)
		{
			if (_encore.TouchScreenEnabled)
			{
				var rect = _encore.TouchScreenRectangle;
				var rotated = _encore.TouchScreenRotated;
				var bufferWidth = (float)_encore.AsVideoProvider().BufferWidth;
				var bufferHeight = (float)_encore.AsVideoProvider().BufferHeight;

				// reset the point's origin to the top left of the screen
				point.X -= rect.X;
				point.Y -= rect.Y;
				if (rotated)
				{
					// scale our point now
					// X is for height here and Y is for width, due to rotation
					point.X *= bufferHeight / rect.Width;
					point.Y *= bufferWidth / rect.Height;
					// adjust point
					point = new(bufferWidth - point.Y, point.X);
				}
				else
				{
					// scale our point now
					point.X *= bufferWidth / rect.Width;
					point.Y *= bufferHeight / rect.Height;
				}
			}

			return point;
		}

		public override Vector2 TransformPoint(string channel, Vector2 point)
		{
			if (_encore.TouchScreenEnabled)
			{
				var rect = _encore.TouchScreenRectangle;
				var rotated = _encore.TouchScreenRotated;

				if (rotated)
				{
					// adjust point
					point = new(point.Y, rect.Height - point.X);
				}

				// reset the point's origin to the top left of the screen
				point.X += rect.X;
				point.Y += rect.Y;

				// TODO: this doesn't handle "large screen" mode correctly
			}

			return point;
		}
	}

	public class FinalPresentation : BaseFilter
	{
		public enum eFilterOption
		{
			None, Bilinear, Bicubic
		}

		public eFilterOption FilterOption = eFilterOption.None;

		public FinalPresentation(Size size)
		{
			OutputSize = size;
		}

		private Size OutputSize, InputSize;
		public Size TextureSize, VirtualTextureSize;
		public int BackgroundColor;
		private bool Nop;
		private LetterboxingLogic LL;

		public bool Config_FixAspectRatio, Config_FixScaleInteger, Config_PadOnly;

		/// <summary>
		/// only use with Config_PadOnly
		/// </summary>
		public (int Left, int Top, int Right, int Bottom) Padding;

		public override void Initialize()
		{
			DeclareInput();
			Nop = false;
		}

		public override Size PresizeOutput(string channel, Size size)
		{
			if (FilterOption == eFilterOption.Bicubic)
			{
				size.Width = LL.vw;
				size.Height = LL.vh;
				return size;
			}

			return base.PresizeOutput(channel, size);
		}

		public override Size PresizeInput(string channel, Size size)
		{
			if (FilterOption != eFilterOption.Bicubic)
				return size;

			if (Config_PadOnly)
			{
				// TODO - redundant fix
				LL = new();
				LL.vx += Padding.Left;
				LL.vy += Padding.Top;
				LL.vw = size.Width;
				LL.vh = size.Height;
			}
			else
			{
				LL = new(Config_FixAspectRatio, Config_FixScaleInteger, OutputSize.Width, OutputSize.Height, size.Width, size.Height, TextureSize, VirtualTextureSize);
				LL.vx += Padding.Left;
				LL.vy += Padding.Top;
			}

			return size;
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			var need = state.SurfaceFormat.Size != OutputSize || FilterOption != eFilterOption.None;
			if (!need)
			{
				Nop = true;
				return;
			}

			FindInput().SurfaceDisposition = SurfaceDisposition.Texture;
			DeclareOutput(new SurfaceState(new(OutputSize), SurfaceDisposition.RenderTarget));
			InputSize = state.SurfaceFormat.Size;
			if (Config_PadOnly)
			{
				// TODO - redundant fix
				LL = new();
				LL.vx += Padding.Left;
				LL.vy += Padding.Top;
				LL.vw = InputSize.Width;
				LL.vh = InputSize.Height;
				LL.WidthScale = 1;
				LL.HeightScale = 1;
			}
			else
			{
				var ow = OutputSize.Width;
				var oh = OutputSize.Height;
				ow -= Padding.Left + Padding.Right;
				oh -= Padding.Top + Padding.Bottom;
				LL = new(Config_FixAspectRatio, Config_FixScaleInteger, ow, oh, InputSize.Width, InputSize.Height, TextureSize, VirtualTextureSize);
				LL.vx += Padding.Left;
				LL.vy += Padding.Top;
			}

			if (InputSize == OutputSize) // any reason we need to check vx and vy?
			{
				// Yes, this is not supposed to be Nop, which has different meaning
				// (yes, this system is a mess, need to clean this up at some point)
				IsNop = true;
			}
		}

		public override Vector2 UntransformPoint(string channel, Vector2 point)
		{
			if (Nop)
			{
				return point;
			}

			point.X -= LL.vx;
			point.Y -= LL.vy;
			point.X /= LL.WidthScale;
			point.Y /= LL.HeightScale;
			return point;
		}

		public override Vector2 TransformPoint(string channel, Vector2 point)
		{
			if (Nop)
			{
				return point;
			}

			point.X *= LL.WidthScale;
			point.Y *= LL.HeightScale;
			point.X += LL.vx;
			point.Y += LL.vy;
			return point;
		}

		public override void Run()
		{
			if (Nop)
			{
				return;
			}

			FilterProgram.GL.ClearColor(Color.FromArgb(BackgroundColor));

			FilterProgram.GuiRenderer.Begin(OutputSize.Width, OutputSize.Height);
			FilterProgram.GuiRenderer.DisableBlending();

			if (FilterOption != eFilterOption.None)
			{
				InputTexture.SetFilterLinear();
			}
			else
			{
				InputTexture.SetFilterNearest();
			}

			if (FilterOption == eFilterOption.Bicubic)
			{
				// this was handled earlier by another filter
			}

			FilterProgram.GuiRenderer.Draw(InputTexture, LL.vx, LL.vy, LL.vw, LL.vh);
			FilterProgram.GuiRenderer.End();
		}
	}

	// TODO - turn this into a NOP at 1x, just in case something accidentally activates it with 1x
	public class PrescaleFilter : BaseFilter
	{
		public int Scale;

		public override void Initialize()
		{
			DeclareInput(SurfaceDisposition.Texture);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			var inputSize = state.SurfaceFormat.Size;
			var outputSize = new Size(inputSize.Width * Scale, inputSize.Height * Scale);
			var maxTexDimension = FilterProgram.GL.MaxTextureDimension;
			while (outputSize.Width > maxTexDimension || outputSize.Height > maxTexDimension)
			{
				outputSize.Width -= inputSize.Width;
				outputSize.Height -= inputSize.Height;
				Scale--;
			}

			// this hopefully never happens
			if (outputSize.Width == 0 || outputSize.Height == 0)
			{
				throw new InvalidOperationException("Prescale input was too large for a texture");
			}

			var ss = new SurfaceState(new(outputSize), SurfaceDisposition.RenderTarget);
			DeclareOutput(ss, channel);
		}

		public override void Run()
		{
			var outSize = FindOutput().SurfaceFormat.Size;
			FilterProgram.GuiRenderer.Begin(outSize);
			FilterProgram.GuiRenderer.DisableBlending();
			FilterProgram.GuiRenderer.ModelView.Scale(Scale);
			FilterProgram.GuiRenderer.Draw(InputTexture);
			FilterProgram.GuiRenderer.End();
		}
	}

	public class AutoPrescaleFilter : BaseFilter
	{
		private Size OutputSize;
		private int XIS, YIS;

		public override void Initialize()
		{
			DeclareInput(SurfaceDisposition.Texture);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			// calculate integer scaling factors
			XIS = OutputSize.Width / state.SurfaceFormat.Size.Width;
			YIS = OutputSize.Height / state.SurfaceFormat.Size.Height;

			if (XIS == 0) XIS = 1;
			if (YIS == 0) YIS = 1;

			OutputSize = state.SurfaceFormat.Size;

			if (XIS <= 1 && YIS <= 1)
			{
				IsNop = true;
			}
			else
			{
				OutputSize.Width *= XIS;
				OutputSize.Height *= YIS;
			}

			DeclareOutput(new SurfaceState(new(OutputSize), SurfaceDisposition.RenderTarget));
		}

		public override Size PresizeOutput(string channel, Size size)
		{
			OutputSize = size;
			return base.PresizeOutput(channel, size);
		}

		public override Size PresizeInput(string channel, Size inSize)
		{
			return inSize;
		}

		public override void Run()
		{
			FilterProgram.GuiRenderer.Begin(OutputSize); // hope this didn't change
			FilterProgram.GuiRenderer.DisableBlending();
			FilterProgram.GuiRenderer.ModelView.Scale(XIS,YIS);
			FilterProgram.GuiRenderer.Draw(InputTexture);
			FilterProgram.GuiRenderer.End();
		}
	}

	public class ApiHawkLayer : BaseFilter
	{
		private readonly I2DRenderer _renderer;

		public ApiHawkLayer(I2DRenderer renderer)
			=> _renderer = renderer;

		public override void Initialize()
		{
			DeclareInput(SurfaceDisposition.RenderTarget);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			DeclareOutput(state);
		}

		public override void Run()
		{
			var outSize = FindOutput().SurfaceFormat.Size;

			var output = _renderer.Render(outSize.Width, outSize.Height);

			// render target might have changed when rendering, rebind the filter chain's target
			var rt = FilterProgram.CurrRenderTarget;
			if (rt == null)
			{
				FilterProgram.GL.BindDefaultRenderTarget();
			}
			else
			{
				rt.Bind();
			}

			FilterProgram.GuiRenderer.Begin(outSize);
			FilterProgram.GuiRenderer.EnableBlending();
			FilterProgram.GuiRenderer.Draw(output);
			FilterProgram.GuiRenderer.End();
		}
	}

	public class OSD : BaseFilter
	{
		// This class has the ability to disable its operations for higher performance _drawOsd is false, while still allowing it to be in the chain
		// (although its presence in the chain may slow down performance itself due to added resolves/renders)

		private readonly bool _drawOsd;
		private readonly OSDManager _manager;
		private readonly StringRenderer _font;

		public OSD(bool drawOsd, OSDManager manager, StringRenderer font)
		{
			_drawOsd = drawOsd;
			_manager = manager;
			_font = font;
		}

		public override void Initialize()
		{
			DeclareInput(SurfaceDisposition.RenderTarget);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			DeclareOutput(state);
		}

		public override void Run()
		{
			if (!_drawOsd)
			{
				return;
			}

			var size = FindInput().SurfaceFormat.Size;

			FilterProgram.GuiRenderer.Begin(size.Width, size.Height);
			var scale = FilterProgram.ControlDpi / 96.0F;
			var blitter = new OSDBlitter(_font, FilterProgram.GuiRenderer, new(0, 0, size.Width, size.Height), scale);
			FilterProgram.GuiRenderer.EnableBlending();
			_manager.DrawScreenInfo(blitter);
			_manager.DrawMessages(blitter);
			FilterProgram.GuiRenderer.End();
		}

		private class OSDBlitter : IBlitter
		{
			private readonly StringRenderer _font;
			private readonly IGuiRenderer _renderer;

			public OSDBlitter(StringRenderer font, IGuiRenderer renderer, Rectangle clipBounds, float scale)
			{
				_font = font;
				_renderer = renderer;
				ClipBounds = clipBounds;
				Scale = scale;
			}

			public void DrawString(string s, Color color, float x, float y)
			{
				_renderer.SetModulateColor(color);
				_font.RenderString(_renderer, x, y, s, Scale);
				_renderer.SetModulateColorWhite();
			}

			public SizeF MeasureString(string s)
				=> _font.Measure(s, Scale);

			public Rectangle ClipBounds { get; }

			public float Scale { get; }
		}
	}
}
