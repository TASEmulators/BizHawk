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

		public LetterboxingLogic(bool maintainAspect, bool maintainInteger, int targetWidth, int targetHeight, int sourceWidth, int sourceHeight)
		{
			//do maths on the viewport and the native resolution and the user settings to get a display rectangle
			Size sz = new Size(targetWidth, targetHeight);

			float widthScale = (float)sz.Width / sourceWidth;
			float heightScale = (float)sz.Height / sourceHeight;
			if (maintainAspect)
			{
				if (widthScale > heightScale) widthScale = heightScale;
				if (heightScale > widthScale) heightScale = widthScale;
			}
			if (maintainInteger)
			{
				widthScale = (float)Math.Floor(widthScale);
				heightScale = (float)Math.Floor(heightScale);
			}
			vw = (int)(widthScale * sourceWidth);
			vh = (int)(heightScale * sourceHeight);
			vx = (sz.Width - vw) / 2;
			vy = (sz.Height - vh) / 2;
			WidthScale = widthScale;
			HeightScale = heightScale;
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
		public int BackgroundColor;
		public GuiRenderer GuiRenderer;
		public IGL GL;
		bool nop;
		LetterboxingLogic LL;

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

			LL = new LetterboxingLogic(Global.Config.DispFixAspectRatio, Global.Config.DispFixScaleInteger, OutputSize.Width, OutputSize.Height, size.Width, size.Height);

			return size;
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			bool need = false;
			if (state.SurfaceFormat.Size != OutputSize)
				need = true;
			if (FilterOption != eFilterOption.None)
				need = true;

			if (!need)
			{
				nop = true;
				return;
			}

			FindInput().SurfaceDisposition = SurfaceDisposition.Texture;
			DeclareOutput(new SurfaceState(new SurfaceFormat(OutputSize), SurfaceDisposition.RenderTarget));
			InputSize = state.SurfaceFormat.Size;
			LL = new LetterboxingLogic(Global.Config.DispFixAspectRatio, Global.Config.DispFixScaleInteger, OutputSize.Width, OutputSize.Height, InputSize.Width, InputSize.Height);
		}

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

		public override void Run()
		{
			if (nop)
				return;

			GL.SetClearColor(Color.FromArgb(BackgroundColor));
			GL.Clear(OpenTK.Graphics.OpenGL.ClearBufferMask.ColorBufferBit);

			GuiRenderer.Begin(OutputSize.Width, OutputSize.Height);
			GuiRenderer.SetBlendState(GL.BlendNone);
			GuiRenderer.Modelview.Push();
			GuiRenderer.Modelview.Translate(LL.vx, LL.vy);
			GuiRenderer.Modelview.Scale(LL.WidthScale, LL.HeightScale);
			if(FilterOption != eFilterOption.None)
				InputTexture.SetFilterLinear();
			else
				InputTexture.SetFilterNearest();

			if (FilterOption == eFilterOption.Bicubic)
			{
			}


			GuiRenderer.Draw(InputTexture);

			GuiRenderer.End();
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

		Bizware.BizwareGL.Texture2d Texture;

		public void SetTexture(Bizware.BizwareGL.Texture2d tex)
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