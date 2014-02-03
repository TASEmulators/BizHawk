using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A DisplayManager is destined forevermore to drive the PresentationPanel it gets initialized with.
	/// Its job is to receive OSD and emulator outputs, and produce one single buffer (BitampBuffer? Texture2d?) for display by the PresentationPanel.
	/// Details TBD
	/// </summary>
	public class DisplayManager : IDisposable
	{

		public DisplayManager(PresentationPanel presentationPanel)
		{
			GL = GlobalWin.GL;
			this.presentationPanel = presentationPanel;
			GraphicsControl = this.presentationPanel.GraphicsControl;

			//it's sort of important for these to be initialized to something nonzero
			currEmuWidth = currEmuHeight = 1;

			Renderer = new GuiRenderer(GL);
			VideoTextureFrugalizer = new TextureFrugalizer(GL);
			LuaEmuTextureFrugalizer = new TextureFrugalizer(GL);
			Video2xFrugalizer = new RenderTargetFrugalizer(GL);

			using (var xml = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Client.EmuHawk.Resources.courier16px.fnt"))
			using (var tex = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Client.EmuHawk.Resources.courier16px_0.png"))
				TheOneFont = new StringRenderer(GL, xml, tex);

			using (var stream = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Client.EmuHawk.DisplayManager.Filters.hq2x.glsl"))
			{
				var str = new System.IO.StreamReader(stream).ReadToEnd();
				RetroShader_Hq2x = new Bizware.BizwareGL.Drivers.OpenTK.RetroShader(GL, str);
			}

			using (var stream = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Client.EmuHawk.DisplayManager.Filters.BizScanlines.glsl"))
			{
				var str = new System.IO.StreamReader(stream).ReadToEnd();
				RetroShader_BizScanlines = new Bizware.BizwareGL.Drivers.OpenTK.RetroShader(GL, str);
			}

		}

		Bizware.BizwareGL.Drivers.OpenTK.RetroShader RetroShader_Hq2x, RetroShader_BizScanlines;

		public bool Disposed { get; private set; }

		public void Dispose()
		{
			if (Disposed) return;
			Disposed = true;
			VideoTextureFrugalizer.Dispose();
			LuaEmuTextureFrugalizer.Dispose();
		}

		//rendering resources:
		IGL GL;
		StringRenderer TheOneFont;
		GuiRenderer Renderer;


		//layer resources
		DisplaySurface luaEmuSurface = null;
		PresentationPanel presentationPanel; //well, its the final layer's target, at least
		GraphicsControl GraphicsControl; //well, its the final layer's target, at least


		public bool NeedsToPaint { get; set; }

		public void PreFrameUpdateLuaSource()
		{
			luaEmuSurface = luaEmuSurfaceSet.GetCurrent();
		}

		/// <summary>
		/// these variables will track the dimensions of the last frame's (or the next frame? this is confusing) emulator native output size
		/// </summary>
		int currEmuWidth, currEmuHeight;

		TextureFrugalizer VideoTextureFrugalizer, LuaEmuTextureFrugalizer;
		RenderTargetFrugalizer Video2xFrugalizer;

		/// <summary>
		/// This will receive an emulated output frame from an IVideoProvider and run it through the complete frame processing pipeline
		/// Then it will stuff it into the bound PresentationPanel
		/// </summary>
		public void UpdateSource(IVideoProvider videoProvider)
		{
			//wrap the videoprovider data in a BitmapBuffer (no point to refactoring that many IVidepProviders)
			BitmapBuffer bb = new BitmapBuffer(videoProvider.BufferWidth, videoProvider.BufferHeight, videoProvider.GetVideoBuffer());

			//record the size of what we received, since lua and stuff is gonna want to draw onto it
			currEmuWidth = bb.Width;
			currEmuHeight = bb.Height;

			//now, acquire the data sent from the videoProvider into a texture
			var videoTexture = VideoTextureFrugalizer.Get(bb);

			//acquire the lua emu surface as a texture
			Texture2d luaEmuTexture = null;
			if (luaEmuSurface != null)
				luaEmuTexture = LuaEmuTextureFrugalizer.Get(luaEmuSurface);

	
			//TargetScanlineFilterIntensity
			//apply filter chain (currently, over-simplified)
			Texture2d currentTexture = videoTexture;
			if (Global.Config.TargetDisplayFilter == 1 && RetroShader_Hq2x.Pipeline.Available)
			{
				var rt = Video2xFrugalizer.Get(videoTexture.IntWidth*2,videoTexture.IntHeight*2);
				rt.Bind();
				Size outSize = new Size(videoTexture.IntWidth * 2, videoTexture.IntHeight * 2);
				RetroShader_Hq2x.Run(videoTexture, videoTexture.Size, outSize, true);
				currentTexture = rt.Texture2d;
			}
			if (Global.Config.TargetDisplayFilter == 2 && RetroShader_BizScanlines.Pipeline.Available)
			{
				var rt = Video2xFrugalizer.Get(videoTexture.IntWidth*2,videoTexture.IntHeight*2);
				rt.Bind();
				Size outSize = new Size(videoTexture.IntWidth * 2, videoTexture.IntHeight * 2);
				RetroShader_BizScanlines.Bind();
				RetroShader_BizScanlines.Pipeline["uIntensity"].Set(1.0f - Global.Config.TargetScanlineFilterIntensity / 256.0f);
				RetroShader_BizScanlines.Run(videoTexture, videoTexture.Size, outSize, true);
				currentTexture = rt.Texture2d;
			}

			//begin drawing to the PresentationPanel:
			GraphicsControl.Begin();

			//1. clear it with the background color that the emulator specified
			GL.SetClearColor(Color.FromArgb(videoProvider.BackgroundColor));
			GL.Clear(OpenTK.Graphics.OpenGL.ClearBufferMask.ColorBufferBit);

			
			//2. begin 2d rendering
			Renderer.Begin(GraphicsControl.Width, GraphicsControl.Height);

			//3. figure out how to draw the emulator output content
			var LL = new LetterboxingLogic(GraphicsControl.Width, GraphicsControl.Height, currentTexture.IntWidth, currentTexture.IntHeight);


			//4. draw the emulator content
			Renderer.SetBlendState(GL.BlendNone);
			Renderer.Modelview.Push();
			Renderer.Modelview.Translate(LL.dx, LL.dy);
			Renderer.Modelview.Scale(LL.finalScale);
			if (Global.Config.DispBlurry)
				videoTexture.SetFilterLinear();
			else
				videoTexture.SetFilterNearest();
			Renderer.Draw(currentTexture);
			//4.b draw the "lua emu surface" which is designed for art matching up exactly with the emulator output
			Renderer.SetBlendState(GL.BlendNormal);
			if(luaEmuTexture != null) Renderer.Draw(luaEmuTexture);
			Renderer.Modelview.Pop();

			//(should we draw native layer lua here? thats broken right now)

			//5. draw the native layer OSD
			MyBlitter myBlitter = new MyBlitter(this);
			myBlitter.ClipBounds = new Rectangle(0, 0, GraphicsControl.Width, GraphicsControl.Height);
			GlobalWin.OSD.Begin(myBlitter);
			GlobalWin.OSD.DrawScreenInfo(myBlitter);
			GlobalWin.OSD.DrawMessages(myBlitter);

			//6. finished drawing
			Renderer.End();

			//7. apply the vsync setting (should probably try to avoid repeating this)
			bool vsync = Global.Config.VSyncThrottle || Global.Config.VSync;
			presentationPanel.GraphicsControl.SetVsync(vsync);

			//7. present and conclude drawing
			presentationPanel.GraphicsControl.SwapBuffers();
			presentationPanel.GraphicsControl.End();

			//cleanup:
			bb.Dispose();
			NeedsToPaint = false; //??
		}

		SwappableDisplaySurfaceSet luaNativeSurfaceSet = new SwappableDisplaySurfaceSet();
		public void SetLuaSurfaceNativePreOSD(DisplaySurface surface) { luaNativeSurfaceSet.SetPending(surface); }
		public DisplaySurface GetLuaSurfaceNative()
		{
			int currNativeWidth = presentationPanel.NativeSize.Width;
			int currNativeHeight = presentationPanel.NativeSize.Height;
			return luaNativeSurfaceSet.AllocateSurface(currNativeWidth, currNativeHeight);
		}

		SwappableDisplaySurfaceSet luaEmuSurfaceSet = new SwappableDisplaySurfaceSet();
		public void SetLuaSurfaceEmu(DisplaySurface surface) { luaEmuSurfaceSet.SetPending(surface); }
		public DisplaySurface GetLuaEmuSurfaceEmu()
		{
			return luaEmuSurfaceSet.AllocateSurface(currEmuWidth, currEmuHeight);
		}

		//helper classes:

		class MyBlitter : IBlitter
		{
			DisplayManager Owner;
			public MyBlitter(DisplayManager dispManager)
			{
				Owner = dispManager;
			}

			class FontWrapper : IBlitterFont
			{
				public FontWrapper(StringRenderer font)
				{
					this.font = font;
				}

				public readonly StringRenderer font;
			}

	
			IBlitterFont IBlitter.GetFontType(string fontType) { return new FontWrapper(Owner.TheOneFont); }
			void IBlitter.DrawString(string s, IBlitterFont font, Color color, float x, float y)
			{
				var stringRenderer = ((FontWrapper)font).font;
				Owner.Renderer.SetModulateColor(color);
				stringRenderer.RenderString(Owner.Renderer, x, y, s);
				Owner.Renderer.SetModulateColorWhite();
			}
			SizeF IBlitter.MeasureString(string s, IBlitterFont font)
			{
				var stringRenderer = ((FontWrapper)font).font;
				return stringRenderer.Measure(s);
			}
			public Rectangle ClipBounds { get; set; }
		}

		/// <summary>
		/// applies letterboxing logic to figure out how to fit the source dimensions into the target dimensions.
		/// In the future this could also apply rules like integer-only scaling, etc.
		/// TODO - make this work with a output rect instead of float and dx/dy
		/// </summary>
		class LetterboxingLogic
		{
			public LetterboxingLogic(int targetWidth, int targetHeight, int sourceWidth, int sourceHeight)
			{
				float vw = (float)targetWidth;
				float vh = (float)targetHeight;
				float widthScale = vw / sourceWidth;
				float heightScale = vh / sourceHeight;
				finalScale = Math.Min(widthScale, heightScale);
				dx = (int)((vw - finalScale * sourceWidth) / 2);
				dy = (int)((vh - finalScale * sourceHeight) / 2);
			}

			/// <summary>
			/// scale to be applied to both x and y
			/// </summary>
			public float finalScale;

			/// <summary>
			/// offset
			/// </summary>
			public float dx, dy;
		}
	}

}