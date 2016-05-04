using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Drawing;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;
using BizHawk.Client.EmuHawk.FilterManager;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.BizwareGL.Drivers.OpenTK;

using OpenTK;
using OpenTK.Graphics;

namespace BizHawk.Client.EmuHawk.Filters
{
	/// <summary>
	/// applies letterboxing logic to figure out how to fit the source dimensions into the target dimensions.
	/// In the future this could also apply rules like integer-only scaling, etc.
	/// </summary>
	class LetterboxingLogic
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

		//do maths on the viewport and the native resolution and the user settings to get a display rectangle
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

			//this doesnt make sense
			if (!maintainAspect)
				maintainInteger = false;

			float widthScale = (float)targetWidth / sourceWidth;
			float heightScale = (float)targetHeight / sourceHeight;

			if (maintainAspect 
				//zero 20-jul-2014 - hacks upon hacks, this function needs rewriting
				&& !maintainInteger
				)
			{
				if (widthScale > heightScale) widthScale = heightScale;
				if (heightScale > widthScale) heightScale = widthScale;
			}

			if (maintainInteger)
			{
				//just totally different code
				//apply the zooming algorithm (pasted and reworked, for now)
				//ALERT COPYPASTE LAUNDROMAT

				Vector2 VS = new Vector2(virtualWidth, virtualHeight);
				Vector2 BS = new Vector2(textureWidth, textureHeight);
				Vector2 AR = Vector2.Divide(VS, BS);
				float target_par = (AR.X / AR.Y);
				Vector2 PS = new Vector2(1, 1); //this would malfunction for AR <= 0.5 or AR >= 2.0

				for(;;)
				{
					//TODO - would be good not to run this per frame....
					Vector2[] trials = new[] {
								PS + new Vector2(1, 0),
								PS + new Vector2(0, 1),
								PS + new Vector2(1, 1)
							};
					bool[] trials_limited = new bool[3] { false,false,false};
					int bestIndex = -1;
					float bestValue = 1000.0f;
					for (int t = 0; t < trials.Length; t++)
					{
						Vector2 vTrial = trials[t];
						trials_limited[t] = false;

						//check whether this is going to exceed our allotted area
						int test_vw = (int)(vTrial.X * textureWidth);
						int test_vh = (int)(vTrial.Y * textureHeight);
						if (test_vw > targetWidth) trials_limited[t] = true;
						if (test_vh > targetHeight) trials_limited[t] = true;

						//I.
						float test_ar = vTrial.X / vTrial.Y;

						//II.
						//Vector2 calc = Vector2.Multiply(trials[t], VS);
						//float test_ar = calc.X / calc.Y;

						//not clear which approach is superior
						float deviation_linear = Math.Abs(test_ar - target_par);
						float deviation_geom = test_ar / target_par;
						if (deviation_geom < 1) deviation_geom = 1.0f / deviation_geom;

						float value = deviation_linear;
						if (value < bestValue)
						{
							bestIndex = t;
							bestValue = value;
						}
					}

					//last result was best, so bail out
					if (bestIndex == -1)
						break;

					//if the winner ran off the edge, bail out
					if (trials_limited[bestIndex])
						break;

					PS = trials[bestIndex];
				}

				//"fix problems with gameextrapadding in >1x window scales" (other edits were made, maybe theyre whats important)
				//vw = (int)(PS.X * oldSourceWidth);
				//vh = (int)(PS.Y * oldSourceHeight);
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

			//theres only one sensible way to letterbox in case we're shrinking a dimension: "pan & scan" to the center
			//this is unlikely to be what the user wants except in the one case of maybe shrinking off some overscan area
			//instead, since we're more about biz than gaming, lets shrink the view to fit in the small dimension
			if (targetWidth < vw)
				vw = targetWidth;
			if (targetHeight < vh)
				vh = targetHeight;

			//determine letterboxing parameters
			vx = (targetWidth - vw) / 2;
			vy = (targetHeight - vh) / 2;

			//zero 09-oct-2014 - changed this for TransformPoint. scenario: basic 1x (but system-specified AR) NES window.
			//vw would be 293 but WidthScale would be 1.0. I think it should be something different.
			//FinalPresentation doesnt use the LL.WidthScale, so this is unlikely to be breaking anything old that depends on it
			//WidthScale = widthScale;
			//HeightScale = heightScale;
			WidthScale = (float)vw / oldSourceWidth;
			HeightScale = (float)vh / oldSourceHeight;
		}

	}

	public class FinalPresentation : BaseFilter
	{
		public enum eFilterOption
		{
			None, Bilinear, Bicubic
		}

		public eFilterOption FilterOption = eFilterOption.None;
		public RetroShaderChain BicubicFilter;

		public FinalPresentation(Size size)
		{
			this.OutputSize = size;
		}

		Size OutputSize, InputSize;
		public Size TextureSize, VirtualTextureSize;
		public int BackgroundColor;
		public bool AutoPrescale;
		public IGuiRenderer GuiRenderer;
		public bool Flip;
		public IGL GL;
		bool nop;
		LetterboxingLogic LL;
		Size ContentSize;

		public bool Config_FixAspectRatio, Config_FixScaleInteger, Config_PadOnly;

		/// <summary>
		/// only use with Config_PadOnly
		/// </summary>
		public System.Windows.Forms.Padding Padding;

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
				ow -= Padding.Horizontal;
				oh -= Padding.Vertical;
				LL = new LetterboxingLogic(Config_FixAspectRatio, Config_FixScaleInteger, ow, oh, InputSize.Width, InputSize.Height, TextureSize, VirtualTextureSize);
				LL.vx += Padding.Left;
				LL.vy += Padding.Top;
			}
			ContentSize = new Size(LL.vw,LL.vh);

			if (InputSize == OutputSize) //any reason we need to check vx and vy?
				IsNOP = true;
		}

		public Size GetContentSize() { return ContentSize; }

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
			var OutputSize = state.SurfaceFormat.Size;
			OutputSize.Width *= Scale;
			OutputSize.Height *= Scale;
			var ss = new SurfaceState(new SurfaceFormat(OutputSize), SurfaceDisposition.RenderTarget);
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
		Size OutputSize, InputSize;
		int XIS, YIS;

		public override void Initialize()
		{
			DeclareInput(SurfaceDisposition.Texture);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			//calculate integer scaling factors
			XIS = OutputSize.Width / state.SurfaceFormat.Size.Width;
			YIS = OutputSize.Height / state.SurfaceFormat.Size.Height;

			OutputSize = state.SurfaceFormat.Size;

			if (XIS <= 1 && YIS <= 1)
			{
				IsNOP = true;
			}
			else
			{
				OutputSize.Width *= XIS;
				OutputSize.Height *= YIS;
			}

			var outState = new SurfaceState();
			outState.SurfaceFormat = new SurfaceFormat(OutputSize);
			outState.SurfaceDisposition = SurfaceDisposition.RenderTarget;
			DeclareOutput(outState);
		}

		public override Size PresizeOutput(string channel, Size size)
		{
			OutputSize = size;
			return base.PresizeOutput(channel, size);
		}

		public override Size PresizeInput(string channel, Size insize)
		{
			InputSize = insize;
			return insize;
		}
		public override void Run()
		{
			FilterProgram.GuiRenderer.Begin(OutputSize); //hope this didnt change
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

		Texture2d Texture;

		public void SetTexture(Texture2d tex)
		{
			Texture = tex;
		}

		public override void Run()
		{
			var outSize = FindOutput().SurfaceFormat.Size;
			FilterProgram.GuiRenderer.Begin(outSize);
			FilterProgram.GuiRenderer.SetBlendState(FilterProgram.GL.BlendNormal);
			FilterProgram.GuiRenderer.Draw(Texture);
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
			if (RenderCallback == null) return;
			RenderCallback();
		}
	}
}