using System;
using System.Drawing;
using System.Numerics;

using BizHawk.Bizware.BizwareGL;
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

		// TODO: actually use this
#if false
		private bool Nop = false;
#endif

		// matrices used for transforming screens
		private Matrix4x4 matTop, matBot;
		private Matrix4x4 matTopInvert, matBotInvert;

		// final output area size
		private Size outputSize;

		private static float Round(float f)
			=> (float)Math.Round(f);

		public ScreenControlNDS(NDS nds)
		{
			_nds = nds;
		}

		public override void Initialize()
		{
			//we're going to be blitting the source as pieces to a new render target, so we need our input to be a texture
			DeclareInput(SurfaceDisposition.Texture);
		}

		private void CrunchNumbers()
		{
			MatrixStack top = new(), bot = new();

			// set up transforms for each screen based on screen control values
			// this will be TRICKY depending on how many features we have, but once it's done, everything should be easy

			var settings = _nds.GetSettings();

			switch (settings.ScreenLayout)
			{
				//gap only applies to vertical, I guess
				case NDS.ScreenLayoutKind.Vertical:
					bot.Translate(0, 192);
					bot.Translate(0, settings.ScreenGap);
					break;
				case NDS.ScreenLayoutKind.Horizontal:
					bot.Translate(256, 0);
					break;
				case NDS.ScreenLayoutKind.Top:
				case NDS.ScreenLayoutKind.Bottom:
					// do nothing here, we'll discard the other screen
					break;
				default:
					throw new InvalidOperationException();
			}

			// this doesn't make any sense, it's likely to be too much for a monitor to gracefully handle too
			if (settings.ScreenLayout != NDS.ScreenLayoutKind.Horizontal)
			{
				var rot = settings.ScreenRotation switch
				{
					NDS.ScreenRotationKind.Rotate90 => 90,
					NDS.ScreenRotationKind.Rotate180 => 180,
					NDS.ScreenRotationKind.Rotate270 => 270,
					_ => 0
				};

				top.RotateZ(rot);
				bot.RotateZ(rot);
			}

			// TODO: refactor some of the below into a class that doesn't require having top and bottom replica code

			matTop = top.Top;
			matBot = bot.Top;

			// apply transforms from standard input screen positions to output screen positions
			var top_TL = Vector2.Transform(new(0, 0), matTop);
			var top_TR = Vector2.Transform(new(256, 0), matTop);
			var top_BL = Vector2.Transform(new(0, 192), matTop);
			var top_BR = Vector2.Transform(new(256, 192), matTop);
			var bot_TL = Vector2.Transform(new(0, 0), matBot);
			var bot_TR = Vector2.Transform(new(256, 0), matBot);
			var bot_BL = Vector2.Transform(new(0, 192), matBot);
			var bot_BR = Vector2.Transform(new(256, 192), matBot);

			// in case of math errors in the transforms, we'll round this stuff.. although...
			// we're gonna use matrix transforms for drawing later, so it isn't extremely helpful

			// TODO - need more consideration of numerical precision here, because the typical case should be rock solid
			top_TL.X = Round(top_TL.X); top_TL.Y = Round(top_TL.Y);
			top_TR.X = Round(top_TR.X); top_TR.Y = Round(top_TR.Y);
			top_BL.X = Round(top_BL.X); top_BL.Y = Round(top_BL.Y);
			top_BR.X = Round(top_BR.X); top_BR.Y = Round(top_BR.Y);
			bot_TL.X = Round(bot_TL.X); bot_TL.Y = Round(bot_TL.Y);
			bot_TR.X = Round(bot_TR.X); bot_TR.Y = Round(bot_TR.Y);
			bot_BL.X = Round(bot_BL.X); bot_BL.Y = Round(bot_BL.Y);
			bot_BR.X = Round(bot_BR.X); bot_BR.Y = Round(bot_BR.Y);

#if false
			// precalculate some useful metrics
			top_width = (int)(top_TR.X - top_TL.X);
			top_height = (int)(top_BR.Y - top_TR.Y);
			bot_width = (int)(bot_TR.X - bot_TL.X);
			bot_height = (int)(bot_BR.Y - bot_TR.Y);
#endif

			// the size can now be determined in a kind of fluffily magical way by transforming edges and checking the bounds
			float fxmin = 100000, fymin = 100000, fxmax = -100000, fymax = -100000;
			if (settings.ScreenLayout != NDS.ScreenLayoutKind.Bottom)
			{
				fxmin = Math.Min(Math.Min(Math.Min(Math.Min(top_TL.X, top_TR.X), top_BL.X), top_BR.X), fxmin);
				fymin = Math.Min(Math.Min(Math.Min(Math.Min(top_TL.Y, top_TR.Y), top_BL.Y), top_BR.Y), fymin);
				fxmax = Math.Max(Math.Max(Math.Max(Math.Max(top_TL.X, top_TR.X), top_BL.X), top_BR.X), fxmax);
				fymax = Math.Max(Math.Max(Math.Max(Math.Max(top_TL.Y, top_TR.Y), top_BL.Y), top_BR.Y), fymax);
			}
			if (settings.ScreenLayout != NDS.ScreenLayoutKind.Top)
			{
				fxmin = Math.Min(Math.Min(Math.Min(Math.Min(bot_TL.X, bot_TR.X), bot_BL.X), bot_BR.X), fxmin);
				fymin = Math.Min(Math.Min(Math.Min(Math.Min(bot_TL.Y, bot_TR.Y), bot_BL.Y), bot_BR.Y), fymin);
				fxmax = Math.Max(Math.Max(Math.Max(Math.Max(bot_TL.X, bot_TR.X), bot_BL.X), bot_BR.X), fxmax);
				fymax = Math.Max(Math.Max(Math.Max(Math.Max(bot_TL.Y, bot_TR.Y), bot_BL.Y), bot_BR.Y), fymax);
			}

			// relocate whatever we got back into the viewable area
			top.Translate(-fxmin, -fymin);
			bot.Translate(-fxmin, -fymin);
			matTop = top.Top;
			matBot = bot.Top;

			// do some more rounding
			unsafe
			{
				fixed (Matrix4x4* matTopP = &matTop, matBotP = &matBot)
				{
					float* matTopF = (float*)matTopP, matBotF = (float*)matBotP;
					for (var i = 0; i < 4 * 4; i++)
					{
						if (Math.Abs(matTopF[i]) < 0.0000001f)
						{
							matTopF[i] = 0;
						}

						if (Math.Abs(matBotF[i]) < 0.0000001f)
						{
							matBotF[i] = 0;
						}
					}
				}
			}

			Matrix4x4.Invert(matTop, out matTopInvert);
			Matrix4x4.Invert(matBot, out matBotInvert);

			outputSize = new((int)(fxmax - fxmin), (int)(fymax - fymin));
		}

		public override Size PresizeInput(string channel, Size size)
		{
			CrunchNumbers();
			return outputSize;
		}

		public override Size PresizeOutput(string channel, Size size)
		{
			CrunchNumbers();
			return base.PresizeOutput(channel, outputSize);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			CrunchNumbers();
			var ss = new SurfaceState(new(outputSize), SurfaceDisposition.RenderTarget);
			DeclareOutput(ss, channel);
		}

		public override Vector2 UntransformPoint(string channel, Vector2 point)
		{
			var settings = _nds.GetSettings();
			var invert = settings.ScreenInvert
				&& settings.ScreenLayout != NDS.ScreenLayoutKind.Top
				&& settings.ScreenLayout != NDS.ScreenLayoutKind.Bottom;

			point = Vector2.Transform(point, invert ? matTopInvert : matBotInvert);

			// hack to accomodate input tracking system's float-point sense (based on the core's VideoBuffer height)
			// actually, this is needed for a reason similar to the "TouchScreenStart" that I removed.
			// So, something like that needs readding if we're to get rid of this hack.
			// (should redo it as a mouse coordinate offset or something.. but the key is to pipe it to the point where this is needed.. that is where MainForm does DisplayManager.UntransformPoint()
			point.Y *= 2;

			// in case we're in this layout, we get confused, so fix it
			if (settings.ScreenLayout == NDS.ScreenLayoutKind.Top) point = new(0.0f, 0.0f);

			// TODO: we probably need more subtle logic here.
			// some capability to return -1,-1 perhaps in case the cursor is nowhere.
			// not sure about that

			return point;
		}

		public override Vector2 TransformPoint(string channel, Vector2 point)
		{
			return Vector2.Transform(point, matTop);
		}

		public override void Run()
		{
#if false
			if (Nop)
			{
				return;
			}
#endif

			// TODO: this could be more efficient (draw only in gap)
			FilterProgram.GL.ClearColor(Color.Black);

			FilterProgram.GuiRenderer.Begin(outputSize);
			FilterProgram.GuiRenderer.DisableBlending();

			// TODO: may depend on input, or other factors, not sure yet
			// watch out though... if we filter linear, then screens will bleed into each other.
			// so we will have to break them into render targets first.
			InputTexture.SetFilterNearest();

			//draw screens
			var renderTop = false;
			var renderBottom = false;
			var settings = _nds.GetSettings();
			switch (settings.ScreenLayout)
			{
				case NDS.ScreenLayoutKind.Bottom:
					renderBottom = true;
					break;
				case NDS.ScreenLayoutKind.Top:
					renderTop = true;
					break;
				case NDS.ScreenLayoutKind.Vertical:
				case NDS.ScreenLayoutKind.Horizontal:
					renderTop = renderBottom = true;
					break;
				default:
					throw new InvalidOperationException();
			}

			var invert = settings.ScreenInvert && renderTop && renderBottom;

			if (renderTop)
			{
				FilterProgram.GuiRenderer.Modelview.Push();
				FilterProgram.GuiRenderer.Modelview.PreMultiplyMatrix(invert ? matBot : matTop);
				FilterProgram.GuiRenderer.DrawSubrect(InputTexture, 0, 0, 256, 192, 0.0f, 0.0f, 1.0f, 0.5f);
				FilterProgram.GuiRenderer.Modelview.Pop();
			}

			if (renderBottom)
			{
				FilterProgram.GuiRenderer.Modelview.Push();
				FilterProgram.GuiRenderer.Modelview.PreMultiplyMatrix(invert ? matTop : matBot);
				FilterProgram.GuiRenderer.DrawSubrect(InputTexture, 0, 0, 256, 192, 0.0f, 0.5f, 1.0f, 1.0f);
				FilterProgram.GuiRenderer.Modelview.Pop();
			}

			FilterProgram.GuiRenderer.End();
		}
	}

	/// <summary>
	/// special screen control features for 3DS
	/// in practice, this is only for correcting mouse input
	/// as the core internally handles screen drawing madness anyways
	/// </summary>
	public class ScreenControl3DS : BaseFilter
	{
		private readonly Citra _citra;

		public ScreenControl3DS(Citra citra)
		{
			_citra = citra;
		}

		public override Vector2 UntransformPoint(string channel, Vector2 point)
		{
			if (_citra.TouchScreenEnabled)
			{
				var rect = _citra.TouchScreenRectangle;
				var rotated = _citra.TouchScreenRotated;
				var bufferWidth = (float)_citra.AsVideoProvider().BufferWidth;
				var bufferHeight = (float)_citra.AsVideoProvider().BufferHeight;

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
			if (_citra.TouchScreenEnabled)
			{
				var rect = _citra.TouchScreenRectangle;
				var rotated = _citra.TouchScreenRotated;

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
				Nop = true;
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
			var outputSize = state.SurfaceFormat.Size;
			outputSize.Width *= Scale;
			outputSize.Height *= Scale;
			var ss = new SurfaceState(new(outputSize), SurfaceDisposition.RenderTarget);
			DeclareOutput(ss, channel);
		}

		public override void Run()
		{
			var outSize = FindOutput().SurfaceFormat.Size;
			FilterProgram.GuiRenderer.Begin(outSize);
			FilterProgram.GuiRenderer.DisableBlending();
			FilterProgram.GuiRenderer.Modelview.Scale(Scale);
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
			FilterProgram.GuiRenderer.Modelview.Scale(XIS,YIS);
			FilterProgram.GuiRenderer.Draw(InputTexture);
			FilterProgram.GuiRenderer.End();
		}
	}

	/// <remarks>More accurately, ApiHawkLayer, since the <c>gui</c> Lua library is delegated.</remarks>
	public class LuaLayer : BaseFilter
	{
		public override void Initialize()
		{
			DeclareInput(SurfaceDisposition.RenderTarget);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			DeclareOutput(state);
		}

		private Texture2d _texture;

		public void SetTexture(Texture2d tex)
		{
			_texture = tex;
		}

		public override void Run()
		{
			var outSize = FindOutput().SurfaceFormat.Size;
			FilterProgram.GuiRenderer.Begin(outSize);
			FilterProgram.GuiRenderer.EnableBlending();
			FilterProgram.GuiRenderer.Draw(_texture);
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
			var blitter = new OSDBlitter(_font, FilterProgram.GuiRenderer, new(0, 0, size.Width, size.Height));
			FilterProgram.GuiRenderer.EnableBlending();
			_manager.DrawScreenInfo(blitter);
			_manager.DrawMessages(blitter);
			FilterProgram.GuiRenderer.End();
		}

		private class OSDBlitter : IBlitter
		{
			private readonly StringRenderer _font;
			private readonly IGuiRenderer _renderer;

			public OSDBlitter(StringRenderer font, IGuiRenderer renderer, Rectangle clipBounds)
			{
				_font = font;
				_renderer = renderer;
				ClipBounds = clipBounds;
			}

			public void DrawString(string s, Color color, float x, float y)
			{
				_renderer.SetModulateColor(color);
				_font.RenderString(_renderer, x, y, s);
				_renderer.SetModulateColorWhite();
			}

			public SizeF MeasureString(string s)
				=> _font.Measure(s);

			public Rectangle ClipBounds { get; }
		}
	}
}