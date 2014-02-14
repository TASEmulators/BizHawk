using System;
using System.IO;
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

using OpenTK;
using OpenTK.Graphics;

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

			Video2xFrugalizer = new RenderTargetFrugalizer(GL);
			VideoTextureFrugalizer = new TextureFrugalizer(GL);

			ShaderChainFrugalizers = new RenderTargetFrugalizer[16]; //hacky hardcoded limit.. need some other way to manage these
			for (int i = 0; i < 16; i++)
			{
				ShaderChainFrugalizers[i] = new RenderTargetFrugalizer(GL);
			}

			using (var xml = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Client.EmuHawk.Resources.courier16px.fnt"))
			using (var tex = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Client.EmuHawk.Resources.courier16px_0.png"))
				TheOneFont = new StringRenderer(GL, xml, tex);

			var fiHq2x = new FileInfo(System.IO.Path.Combine(PathManager.GetExeDirectoryAbsolute(),"Shaders/BizHawk/hq2x.cgp"));
			if(fiHq2x.Exists)
				using(var stream = fiHq2x.OpenRead())
					ShaderChain_hq2x = new RetroShaderChain(GL,new RetroShaderPreset(stream), System.IO.Path.Combine(PathManager.GetExeDirectoryAbsolute(),"Shaders/BizHawk"));
			var fiScanlines = new FileInfo(System.IO.Path.Combine(PathManager.GetExeDirectoryAbsolute(), "Shaders/BizHawk/BizScanlines.cgp"));
			if (fiScanlines.Exists)
				using (var stream = fiScanlines.OpenRead())
					ShaderChain_scanlines = new RetroShaderChain(GL,new RetroShaderPreset(stream), System.IO.Path.Combine(PathManager.GetExeDirectoryAbsolute(),"Shaders/BizHawk"));

			LuaSurfaceSets["emu"] = new SwappableDisplaySurfaceSet();
			LuaSurfaceSets["native"] = new SwappableDisplaySurfaceSet();
			LuaSurfaceFrugalizers["emu"] = new TextureFrugalizer(GL);
			LuaSurfaceFrugalizers["native"] = new TextureFrugalizer(GL);
		}

		public bool Disposed { get; private set; }

		public void Dispose()
		{
			if (Disposed) return;
			Disposed = true;
			VideoTextureFrugalizer.Dispose();
			foreach (var f in LuaSurfaceFrugalizers.Values)
				f.Dispose();
			foreach (var f in ShaderChainFrugalizers)
				if (f != null)
					f.Dispose();
		}

		//dont know what to do about this yet
		public bool NeedsToPaint { get; set; }

		//rendering resources:
		IGL GL;
		StringRenderer TheOneFont;
		GuiRenderer Renderer;

		//layer resources
		PresentationPanel presentationPanel; //well, its the final layer's target, at least
		GraphicsControl GraphicsControl; //well, its the final layer's target, at least


		/// <summary>
		/// these variables will track the dimensions of the last frame's (or the next frame? this is confusing) emulator native output size
		/// </summary>
		int currEmuWidth, currEmuHeight;

		TextureFrugalizer VideoTextureFrugalizer;
		Dictionary<string, TextureFrugalizer> LuaSurfaceFrugalizers = new Dictionary<string, TextureFrugalizer>();
		RenderTargetFrugalizer Video2xFrugalizer;
		RenderTargetFrugalizer[] ShaderChainFrugalizers;
		RetroShaderChain ShaderChain_hq2x, ShaderChain_scanlines;

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

			//acquire the lua surfaces as textures
			Texture2d luaEmuTexture = null;
			var luaEmuSurface = LuaSurfaceSets["emu"].GetCurrent();
			if (luaEmuSurface != null)
				luaEmuTexture = LuaSurfaceFrugalizers["emu"].Get(luaEmuSurface);

			Texture2d luaNativeTexture = null;
			var luaNativeSurface = LuaSurfaceSets["native"].GetCurrent();
			if (luaNativeSurface != null)
				luaNativeTexture = LuaSurfaceFrugalizers["native"].Get(luaNativeSurface);

			//select shader chain
			RetroShaderChain selectedChain = null;
			if (Global.Config.TargetDisplayFilter == 1 && ShaderChain_hq2x != null && ShaderChain_hq2x.Available)
				selectedChain = ShaderChain_hq2x;
			if (Global.Config.TargetDisplayFilter == 2 && ShaderChain_scanlines != null && ShaderChain_scanlines.Available)
				selectedChain = ShaderChain_scanlines;

			//run shader chain
			Texture2d currentTexture = videoTexture;
			if (selectedChain != null)
			{
				foreach (var pass in selectedChain.Passes)
				{
					//calculate size of input and output (note, we dont have a distinction between logical size and POW2 buffer size yet, like we should)
					
					Size insize = currentTexture.Size;
					Size outsize = insize;

					//calculate letterboxing scale factors for the current configuration, so that ScaleType.Viewport can do something intelligent
					var LLpass = new LetterboxingLogic(GraphicsControl.Width, GraphicsControl.Height, insize.Width, insize.Height);

					if (pass.ScaleTypeX == RetroShaderPreset.ScaleType.Absolute) { throw new NotImplementedException("ScaleType Absolute"); }
					if (pass.ScaleTypeX == RetroShaderPreset.ScaleType.Viewport) outsize.Width = LLpass.Rectangle.Width;
					if (pass.ScaleTypeX == RetroShaderPreset.ScaleType.Source) outsize.Width = (int)(insize.Width * pass.Scale.X);
					if (pass.ScaleTypeY == RetroShaderPreset.ScaleType.Absolute) { throw new NotImplementedException("ScaleType Absolute"); }
					if (pass.ScaleTypeY == RetroShaderPreset.ScaleType.Viewport) outsize.Height = LLpass.Rectangle.Height;
					if (pass.ScaleTypeY == RetroShaderPreset.ScaleType.Source) outsize.Height = (int)(insize.Height * pass.Scale.Y);

					if (pass.InputFilterLinear)
						videoTexture.SetFilterLinear();
					else
						videoTexture.SetFilterNearest();

					var rt = ShaderChainFrugalizers[pass.Index].Get(outsize.Width, outsize.Height);
					rt.Bind();

					var shader = selectedChain.Shaders[pass.Index];
					shader.Bind();
					if(selectedChain == ShaderChain_scanlines)
						shader.Pipeline["uIntensity"].Set(1.0f - Global.Config.TargetScanlineFilterIntensity / 256.0f);
					shader.Run(currentTexture, insize, outsize, true);
					currentTexture = rt.Texture2d;
				}
			}

			//begin drawing to the PresentationPanel:
			GraphicsControl.Begin();

			//1. clear it with the background color that the emulator specified (could we please only clear the necessary letterbox area, to save some time?)
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

			//5a. draw the native layer content
			//4.b draw the "lua emu surface" which is designed for art matching up exactly with the emulator output
			if (luaNativeTexture != null) Renderer.Draw(luaNativeTexture);

			//5b. draw the native layer OSD
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

		Dictionary<string, DisplaySurface> MapNameToLuaSurface = new Dictionary<string,DisplaySurface>();
		Dictionary<DisplaySurface, string> MapLuaSurfaceToName = new Dictionary<DisplaySurface, string>();
		Dictionary<string, SwappableDisplaySurfaceSet> LuaSurfaceSets = new Dictionary<string, SwappableDisplaySurfaceSet>();
		SwappableDisplaySurfaceSet luaNativeSurfaceSet = new SwappableDisplaySurfaceSet();
		public void SetLuaSurfaceNativePreOSD(DisplaySurface surface) { luaNativeSurfaceSet.SetPending(surface); }

		/// <summary>
		/// Locks the requested lua surface name
		/// </summary>
		public DisplaySurface LockLuaSurface(string name)
		{
			if (MapNameToLuaSurface.ContainsKey(name))
				throw new InvalidOperationException("Lua surface is already locked: " + name);

			SwappableDisplaySurfaceSet sdss;
			if (!LuaSurfaceSets.TryGetValue(name, out sdss))
			{
				sdss = new SwappableDisplaySurfaceSet();
				LuaSurfaceSets.Add(name, sdss);
			}

			//placeholder logic for more abstracted surface definitions from filter chain
			int currNativeWidth = presentationPanel.NativeSize.Width;
			int currNativeHeight = presentationPanel.NativeSize.Height;

			int width,height;
			if(name == "emu") { width = currEmuWidth; height = currEmuHeight; }
			else if(name == "native") { width = currNativeWidth; height = currNativeHeight; }
			else throw new InvalidOperationException("Unknown lua surface name: " +name);

			DisplaySurface ret = sdss.AllocateSurface(width, height);
			MapNameToLuaSurface[name] = ret;
			MapLuaSurfaceToName[ret] = name;
			return ret;
		}

		/// <summary>
		/// Unlocks this DisplaySurface which had better have been locked as a lua surface
		/// </summary>
		public void UnlockLuaSurface(DisplaySurface surface)
		{
			if (!MapLuaSurfaceToName.ContainsKey(surface))
				throw new InvalidOperationException("Surface was not locked as a lua surface");
			string name = MapLuaSurfaceToName[surface];
			MapLuaSurfaceToName.Remove(surface);
			MapNameToLuaSurface.Remove(name);
			LuaSurfaceSets[name].SetPending(surface);
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
				Rectangle = new Rectangle((int)dx, (int)dy, (int)(finalScale * sourceWidth), (int)(finalScale * sourceHeight));
			}

			/// <summary>
			/// scale to be applied to both x and y
			/// </summary>
			public float finalScale;

			/// <summary>
			/// offset
			/// </summary>
			public float dx, dy;

			/// <summary>
			/// The destination rectangle
			/// </summary>
			public Rectangle Rectangle;
		}
	}

}