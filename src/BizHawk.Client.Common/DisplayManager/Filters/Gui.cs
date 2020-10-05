using System;
using System.Drawing;
using BizHawk.Client.Common.FilterManager;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;

using BizHawk.Bizware.BizwareGL;
using OpenTK;

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
		public LetterboxingLogic() { }

		// do maths on the viewport and the native resolution and the user settings to get a display rectangle
		public LetterboxingLogic(bool maintainAspect, bool maintainInteger, int targetWidth, int targetHeight, int sourceWidth, int sourceHeight, Size textureSize, Size virtualSize)
		{
			int textureWidth = textureSize.Width;
			int textureHeight = textureSize.Height;
			int virtualWidth = virtualSize.Width;
			int virtualHeight = virtualSize.Height;

			//zero 02-jun-2014 - we passed these in, but ignored them. kind of weird..
			int oldSourceWidth = sourceWidth;
			int oldSourceHeight = sourceHeight;
			sourceWidth = (int)virtualWidth;
			sourceHeight = (int)virtualHeight;

			// this doesn't make sense
			if (!maintainAspect)
			{
				maintainInteger = false;
			}

			float widthScale = (float)targetWidth / sourceWidth;
			float heightScale = (float)targetHeight / sourceHeight;

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

				Vector2 VS = new Vector2(virtualWidth, virtualHeight);
				Vector2 BS = new Vector2(textureWidth, textureHeight);
				Vector2 AR = Vector2.Divide(VS, BS);
				float targetPar = (AR.X / AR.Y);
				Vector2 PS = new Vector2(1, 1); //this would malfunction for AR <= 0.5 or AR >= 2.0

				for(;;)
				{
					// TODO - would be good not to run this per frame....
					Vector2[] trials =
					{
						PS + new Vector2(1, 0),
						PS + new Vector2(0, 1),
						PS + new Vector2(1, 1)
					};

					bool[] trialsLimited = new bool[3] { false,false,false};
					int bestIndex = -1;
					float bestValue = 1000.0f;
					for (int t = 0; t < trials.Length; t++)
					{
						Vector2 vTrial = trials[t];
						trialsLimited[t] = false;

						//check whether this is going to exceed our allotted area
						int testVw = (int)(vTrial.X * textureWidth);
						int testVh = (int)(vTrial.Y * textureHeight);
						if (testVw > targetWidth) trialsLimited[t] = true;
						if (testVh > targetHeight) trialsLimited[t] = true;

						// I.
						float testAr = vTrial.X / vTrial.Y;

						// II.
						// Vector2 calc = Vector2.Multiply(trials[t], VS);
						// float test_ar = calc.X / calc.Y;

						// not clear which approach is superior
						float deviationLinear = Math.Abs(testAr - targetPar);
						float deviationGeom = testAr / targetPar;
						if (deviationGeom < 1) deviationGeom = 1.0f / deviationGeom;

						float value = deviationLinear;
						if (value < bestValue)
						{
							bestIndex = t;
							bestValue = value;
						}
					}

					// last result was best, so bail out
					if (bestIndex == -1)
						break;

					// if the winner ran off the edge, bail out
					if (trialsLimited[bestIndex])
						break;

					PS = trials[bestIndex];
				}

				// "fix problems with gameextrapadding in >1x window scales" (other edits were made, maybe theyre whats important)
				// vw = (int)(PS.X * oldSourceWidth);
				// vh = (int)(PS.Y * oldSourceHeight);
				vw = (int)(PS.X * sourceWidth);
				vh = (int)(PS.Y * sourceHeight);
				widthScale = PS.X;
				heightScale = PS.Y;
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

			//determine letterboxing parameters
			vx = (targetWidth - vw) / 2;
			vy = (targetHeight - vh) / 2;

			// zero 09-oct-2014 - changed this for TransformPoint. scenario: basic 1x (but system-specified AR) NES window.
			// vw would be 293 but WidthScale would be 1.0. I think it should be something different.
			// FinalPresentation doesn't use the LL.WidthScale, so this is unlikely to be breaking anything old that depends on it
			// WidthScale = widthScale;
			// HeightScale = heightScale;
			WidthScale = (float)vw / oldSourceWidth;
			HeightScale = (float)vh / oldSourceHeight;
		}
	}

	/// <summary>
	/// special screen control features for NDS
	/// </summary>
	public class ScreenControlNDS : BaseFilter
	{
		public IGL GL;
		public IGuiRenderer GuiRenderer;

		private MelonDS nds;

		//TODO: actually use this
		private bool nop = false;

		//matrices used for transforming screens
		private Matrix4 matTop, matBot;
		private Matrix4 matTopInvert, matBotInvert;
		
		//final output area size
		private Size outputSize;

		private static float Round(float f) { return (float)Math.Round(f); }

		//TODO: put somewhere in extension methods useful for fixing deficiencies in opentk matrix types
		private static Vector2 Transform(Matrix4 m, Vector2 v)
		{
			var r = new Vector4(v.X,v.Y,0,1) * m;
			return new Vector2(r.X, r.Y);
		}

		public ScreenControlNDS(MelonDS nds)
		{
			//not sure if we actually need this nds instance yet
			this.nds = nds;
		}

		public override void Initialize()
		{
			//we're going to be blitting the source as pieces to a new render target, so we need our input to be a texture
			DeclareInput(SurfaceDisposition.Texture);
		}

		private void CrunchNumbers()
		{
			MatrixStack top = new MatrixStack(), bot = new MatrixStack();

			//-----------------------------------
			//set up transforms for each screen based on screen control values
			//this will be TRICKY depending on how many features we have, but once it's done, everything should be easy

			var settings = nds.GetSettings();

			//gap only applies to vertical, I guess
			if (settings.ScreenLayout == MelonDS.ScreenLayoutKind.Vertical)
			{
				bot.Translate(0, 192);
				bot.Translate(0, settings.ScreenGap);
			}
			else if (settings.ScreenLayout == MelonDS.ScreenLayoutKind.Horizontal)
			{
				bot.Translate(256, 0);
			}
			else if (settings.ScreenLayout == MelonDS.ScreenLayoutKind.Top)
			{
				//do nothing here, we'll discard bottom screen
			}

			//this doesn't make any sense, it's likely to be too much for a monitor to gracefully handle too
			if (settings.ScreenLayout != MelonDS.ScreenLayoutKind.Horizontal)
			{
				int rot = 0;
				if (settings.ScreenRotation == MelonDS.ScreenRotationKind.Rotate90) rot = 90;
				if (settings.ScreenRotation == MelonDS.ScreenRotationKind.Rotate180) rot = 180;
				if (settings.ScreenRotation == MelonDS.ScreenRotationKind.Rotate270) rot = 270;
				top.RotateZ(rot);
				bot.RotateZ(rot);
			}

			//-----------------------------------

			//TODO: refactor some of the below into a class that doesn't require having top and bottom replica code

			matTop = top.Top;
			matBot = bot.Top;
			matTopInvert = matTop.Inverted();
			matBotInvert = matBot.Inverted();

			//apply transforms from standard input screen positions to output screen positions
			Vector2 top_TL = Transform(matTop, new Vector2(0, 0));
			Vector2 top_TR = Transform(matTop, new Vector2(256, 0));
			Vector2 top_BL = Transform(matTop, new Vector2(0, 192));
			Vector2 top_BR = Transform(matTop, new Vector2(256, 192));
			Vector2 bot_TL = Transform(matBot, new Vector2(0, 0));
			Vector2 bot_TR = Transform(matBot, new Vector2(256, 0));
			Vector2 bot_BL = Transform(matBot, new Vector2(0, 192));
			Vector2 bot_BR = Transform(matBot, new Vector2(256, 192));

			//in case of math errors in the transforms, we'll round this stuff.. although...
			//we're gonna use matrix transforms for drawing later, so it isn't extremely helpful

			//TODO - need more consideration of numerical precision here, because the typical case should be rock solid
			top_TL.X = Round(top_TL.X); top_TL.Y = Round(top_TL.Y);
			top_TR.X = Round(top_TR.X); top_TR.Y = Round(top_TR.Y);
			top_BL.X = Round(top_BL.X); top_BL.Y = Round(top_BL.Y);
			top_BR.X = Round(top_BR.X); top_BR.Y = Round(top_BR.Y);
			bot_TL.X = Round(bot_TL.X); bot_TL.Y = Round(bot_TL.Y);
			bot_TR.X = Round(bot_TR.X); bot_TR.Y = Round(bot_TR.Y);
			bot_BL.X = Round(bot_BL.X); bot_BL.Y = Round(bot_BL.Y);
			bot_BR.X = Round(bot_BR.X); bot_BR.Y = Round(bot_BR.Y);

			////precalculate some useful metrics
			//top_width = (int)(top_TR.X - top_TL.X);
			//top_height = (int)(top_BR.Y - top_TR.Y);
			//bot_width = (int)(bot_TR.X - bot_TL.X);
			//bot_height = (int)(bot_BR.Y - bot_TR.Y);

			//the size can now be determined in a kind of fluffily magical way by transforming edges and checking the bounds
			float fxmin = 100000, fymin = 100000, fxmax = -100000, fymax = -100000;
			if (settings.ScreenLayout != MelonDS.ScreenLayoutKind.Bottom)
			{
				fxmin = Math.Min(Math.Min(Math.Min(Math.Min(top_TL.X, top_TR.X), top_BL.X), top_BR.X), fxmin);
				fymin = Math.Min(Math.Min(Math.Min(Math.Min(top_TL.Y, top_TR.Y), top_BL.Y), top_BR.Y), fymin);
				fxmax = Math.Max(Math.Max(Math.Max(Math.Max(top_TL.X, top_TR.X), top_BL.X), top_BR.X), fxmax);
				fymax = Math.Max(Math.Max(Math.Max(Math.Max(top_TL.Y, top_TR.Y), top_BL.Y), top_BR.Y), fymax);
			}
			if (settings.ScreenLayout != MelonDS.ScreenLayoutKind.Top)
			{
				fxmin = Math.Min(Math.Min(Math.Min(Math.Min(bot_TL.X, bot_TR.X), bot_BL.X), bot_BR.X), fxmin);
				fymin = Math.Min(Math.Min(Math.Min(Math.Min(bot_TL.Y, bot_TR.Y), bot_BL.Y), bot_BR.Y), fymin);
				fxmax = Math.Max(Math.Max(Math.Max(Math.Max(bot_TL.X, bot_TR.X), bot_BL.X), bot_BR.X), fxmax);
				fymax = Math.Max(Math.Max(Math.Max(Math.Max(bot_TL.Y, bot_TR.Y), bot_BL.Y), bot_BR.Y), fymax);
			}

			//relocate whatever we got back into the viewable area
			top.Translate(-fxmin, -fymin);
			bot.Translate(-fxmin, -fymin);
			matTop = top.Top;
			matBot = bot.Top;

			//do some more rounding
			for (int r = 0; r < 4; r++)
				for (int c = 0; c < 4; c++)
				{
					if (Math.Abs(matTop[r, c]) < 0.0000001f) matTop[r, c] = 0;
					if (Math.Abs(matBot[r, c]) < 0.0000001f) matBot[r, c] = 0;
				}

			matTopInvert = matTop.Inverted();
			matBotInvert = matBot.Inverted();

			outputSize = new Size((int)(fxmax-fxmin), (int)(fymax-fymin));
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
			var ss = new SurfaceState(new SurfaceFormat(outputSize), SurfaceDisposition.RenderTarget);
			DeclareOutput(ss, channel);
		}

		public override Vector2 UntransformPoint(string channel, Vector2 point)
		{
			point = Transform(matBotInvert, point);

			//hack to accomodate input tracking system's float-point sense (based on the core's videobuffer height)
			//actually, this is needed for a reason similar to the "TouchScreenStart" that I removed.
			//So, something like that needs readding if we're to get rid of this hack.
			//(should redo it as a mouse coordinate offset or something.. but the key is to pipe it to the point where this is needed.. that is where MainForm does DisplayManager.UntransformPoint()
			point.Y *= 2;

			//in case we're in this layout, we get confused, so fix it
			var settings = nds.GetSettings();
			if (settings.ScreenLayout == MelonDS.ScreenLayoutKind.Top)
				point = Vector2.Zero;

			//TODO: we probably need more subtle logic here.
			//some capability to return -1,-1 perhaps in case the cursor is nowhere.
			//not sure about that

			return point;
		}

		public override Vector2 TransformPoint(string channel, Vector2 point)
		{
			point = Transform(matBot, point);
			return point;
		}

		public override void Run()
		{
			if (nop)
				return;

			//TODO: this could be more efficient (draw only in gap)
			GL.SetClearColor(Color.Black);
			GL.Clear(OpenTK.Graphics.OpenGL.ClearBufferMask.ColorBufferBit);

			FilterProgram.GuiRenderer.Begin(outputSize);
			GuiRenderer.SetBlendState(GL.BlendNoneCopy);

			//TODO: may depend on input, or other factors, not sure yet
			//watch out though... if we filter linear, then screens will bleed into each other.
			//so we will have to break them into render targets first.
			InputTexture.SetFilterNearest();

			//draw screens
			bool renderTop = false;
			bool renderBottom = false;
			var settings = nds.GetSettings();
			if (settings.ScreenLayout == MelonDS.ScreenLayoutKind.Bottom) renderBottom = true;
			if (settings.ScreenLayout == MelonDS.ScreenLayoutKind.Top) renderTop = true;
			if (settings.ScreenLayout == MelonDS.ScreenLayoutKind.Vertical) renderTop = renderBottom = true;
			if (settings.ScreenLayout == MelonDS.ScreenLayoutKind.Horizontal) renderTop = renderBottom = true;

			if (renderTop)
			{
				GuiRenderer.Modelview.Push();
				GuiRenderer.Modelview.PreMultiplyMatrix(matTop);
				GuiRenderer.DrawSubrect(InputTexture, 0, 0, 256, 192, 0.0f, 0.0f, 1.0f, 0.5f);
				GuiRenderer.Modelview.Pop();
			}

			if (renderBottom)
			{
				GuiRenderer.Modelview.Push();
				GuiRenderer.Modelview.PreMultiplyMatrix(matBot);
				GuiRenderer.DrawSubrect(InputTexture, 0, 0, 256, 192, 0.0f, 0.5f, 1.0f, 1.0f);
				GuiRenderer.Modelview.Pop();
			}

			GuiRenderer.End();
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
			this.OutputSize = size;
		}

		private Size OutputSize, InputSize;
		public Size TextureSize, VirtualTextureSize;
		public int BackgroundColor;
		public bool AutoPrescale;
		public IGuiRenderer GuiRenderer;
		public bool Flip;
		public IGL GL;
		private bool nop;
		private LetterboxingLogic LL;
		private Size ContentSize;

		public bool Config_FixAspectRatio, Config_FixScaleInteger, Config_PadOnly;

		/// <summary>
		/// only use with Config_PadOnly
		/// </summary>
		public (int Left, int Top, int Right, int Bottom) Padding;

		public override void Initialize()
		{
			DeclareInput();
			nop = false;
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
				//TODO - redundant fix
				LL = new LetterboxingLogic();
				LL.vx += Padding.Left;
				LL.vy += Padding.Top;
				LL.vw = size.Width;
				LL.vh = size.Height;
			}
			else
			{
				LL = new LetterboxingLogic(Config_FixAspectRatio, Config_FixScaleInteger, OutputSize.Width, OutputSize.Height, size.Width, size.Height, TextureSize, VirtualTextureSize);
				LL.vx += Padding.Left;
				LL.vy += Padding.Top;
			}

			return size;
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			bool need = false;
			if (state.SurfaceFormat.Size != OutputSize)
				need = true;
			if (FilterOption != eFilterOption.None)
				need = true;
			if (Flip)
				need = true;

			if (!need)
			{
				nop = true;
				ContentSize = state.SurfaceFormat.Size;
				return;
			}

			FindInput().SurfaceDisposition = SurfaceDisposition.Texture;
			DeclareOutput(new SurfaceState(new SurfaceFormat(OutputSize), SurfaceDisposition.RenderTarget));
			InputSize = state.SurfaceFormat.Size;
			if (Config_PadOnly)
			{
				//TODO - redundant fix
				LL = new LetterboxingLogic();
				LL.vx += Padding.Left;
				LL.vy += Padding.Top;
				LL.vw = InputSize.Width;
				LL.vh = InputSize.Height;
				LL.WidthScale = 1;
				LL.HeightScale = 1;
			}
			else
			{
				int ow = OutputSize.Width;
				int oh = OutputSize.Height;
				ow -= Padding.Left + Padding.Right;
				oh -= Padding.Top + Padding.Bottom;
				LL = new LetterboxingLogic(Config_FixAspectRatio, Config_FixScaleInteger, ow, oh, InputSize.Width, InputSize.Height, TextureSize, VirtualTextureSize);
				LL.vx += Padding.Left;
				LL.vy += Padding.Top;
			}
			ContentSize = new Size(LL.vw,LL.vh);

			if (InputSize == OutputSize) //any reason we need to check vx and vy?
				IsNop = true;
		}

		public Size GetContentSize() => ContentSize;

		public override Vector2 UntransformPoint(string channel, Vector2 point)
		{
			if (nop)
				return point;
			point.X -= LL.vx;
			point.Y -= LL.vy;
			point.X /= LL.WidthScale;
			point.Y /= LL.HeightScale;
			return point;
		}

		public override Vector2 TransformPoint(string channel, Vector2 point)
		{
			if (nop)
				return point;
			point.X *= LL.WidthScale;
			point.Y *= LL.HeightScale;
			point.X += LL.vx;
			point.Y += LL.vy;
			return point;
		}

		public override void Run()
		{
			if (nop)
				return;

			GL.SetClearColor(Color.FromArgb(BackgroundColor));
			GL.Clear(OpenTK.Graphics.OpenGL.ClearBufferMask.ColorBufferBit);

			GuiRenderer.Begin(OutputSize.Width, OutputSize.Height);
			GuiRenderer.SetBlendState(GL.BlendNoneCopy);

			if(FilterOption != eFilterOption.None)
				InputTexture.SetFilterLinear();
			else
				InputTexture.SetFilterNearest();

			if (FilterOption == eFilterOption.Bicubic)
			{
				//this was handled earlier by another filter
			}

			GuiRenderer.Modelview.Translate(LL.vx, LL.vy);
			if (Flip)
			{
				GuiRenderer.Modelview.Scale(1, -1);
				GuiRenderer.Modelview.Translate(0, -LL.vh);
			}
			GuiRenderer.Draw(InputTexture,0,0,LL.vw,LL.vh);

			GuiRenderer.End();
		}
	}

	//TODO - turn this into a NOP at 1x, just in case something accidentally activates it with 1x
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
			var ss = new SurfaceState(new SurfaceFormat(outputSize), SurfaceDisposition.RenderTarget);
			DeclareOutput(ss, channel);
		}

		public override void Run()
		{
			var outSize = FindOutput().SurfaceFormat.Size;
			FilterProgram.GuiRenderer.Begin(outSize);
			FilterProgram.GuiRenderer.SetBlendState(FilterProgram.GL.BlendNoneCopy);
			FilterProgram.GuiRenderer.Modelview.Scale(Scale);
			FilterProgram.GuiRenderer.Draw(InputTexture);
			FilterProgram.GuiRenderer.End();
		}
	}

	public class AutoPrescaleFilter : BaseFilter
	{
		private Size OutputSize, InputSize;
		private int XIS, YIS;

		public override void Initialize()
		{
			DeclareInput(SurfaceDisposition.Texture);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			//calculate integer scaling factors
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

			DeclareOutput(new SurfaceState
			{
				SurfaceFormat = new SurfaceFormat(OutputSize),
				SurfaceDisposition = SurfaceDisposition.RenderTarget
			});
		}

		public override Size PresizeOutput(string channel, Size size)
		{
			OutputSize = size;
			return base.PresizeOutput(channel, size);
		}

		public override Size PresizeInput(string channel, Size inSize)
		{
			InputSize = inSize;
			return inSize;
		}

		public override void Run()
		{
			FilterProgram.GuiRenderer.Begin(OutputSize); // hope this didn't change
			FilterProgram.GuiRenderer.SetBlendState(FilterProgram.GL.BlendNoneCopy);
			FilterProgram.GuiRenderer.Modelview.Scale(XIS,YIS);
			FilterProgram.GuiRenderer.Draw(InputTexture);
			FilterProgram.GuiRenderer.End();
		}
	}

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
			FilterProgram.GuiRenderer.SetBlendState(FilterProgram.GL.BlendNormal);
			FilterProgram.GuiRenderer.Draw(_texture);
			FilterProgram.GuiRenderer.End();
		}
	}

	public class OSD : BaseFilter
	{
		//this class has the ability to disable its operations for higher performance when the callback is removed,
		//without having to take it out of the chain. although, its presence in the chain may slow down performance due to added resolves/renders
		//so, we should probably rebuild the chain.

		public override void Initialize()
		{
			if (RenderCallback == null) return;
			DeclareInput(SurfaceDisposition.RenderTarget);
		}
		public override void SetInputFormat(string channel, SurfaceState state)
		{
			if (RenderCallback == null) return;
			DeclareOutput(state);
		}

		public Action RenderCallback;

		public override void Run()
		{
			RenderCallback?.Invoke();
		}
	}
}