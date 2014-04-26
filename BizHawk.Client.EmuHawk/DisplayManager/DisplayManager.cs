//TODO
//we could flag textures as 'actually' render targets (keep a reference to the render target?) which could allow us to convert between them more quickly in some cases

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.FilterManager;
using BizHawk.Client.EmuHawk;

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
		class DisplayManagerRenderTargetProvider : FilterManager.IRenderTargetProvider
		{
			DisplayManagerRenderTargetProvider(Func<Size, RenderTarget> callback) { Callback = callback; }
			Func<Size, RenderTarget> Callback;
			RenderTarget FilterManager.IRenderTargetProvider.Get(Size size)
			{
				return Callback(size);
			}
		}

		public DisplayManager(PresentationPanel presentationPanel)
		{
			GL = GlobalWin.GL;
			this.presentationPanel = presentationPanel;
			GraphicsControl = this.presentationPanel.GraphicsControl;

			//it's sort of important for these to be initialized to something nonzero
			currEmuWidth = currEmuHeight = 1;

			Renderer = new GuiRenderer(GL);

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
					ShaderChain_hq2x = new Filters.RetroShaderChain(GL, new Filters.RetroShaderPreset(stream), System.IO.Path.Combine(PathManager.GetExeDirectoryAbsolute(), "Shaders/BizHawk"));
			var fiScanlines = new FileInfo(System.IO.Path.Combine(PathManager.GetExeDirectoryAbsolute(), "Shaders/BizHawk/BizScanlines.cgp"));
			if (fiScanlines.Exists)
				using (var stream = fiScanlines.OpenRead())
					ShaderChain_scanlines = new Filters.RetroShaderChain(GL, new Filters.RetroShaderPreset(stream), System.IO.Path.Combine(PathManager.GetExeDirectoryAbsolute(), "Shaders/BizHawk"));
			var fiBicubic = new FileInfo(System.IO.Path.Combine(PathManager.GetExeDirectoryAbsolute(), "Shaders/BizHawk/bicubic-fast.cgp"));
			if (fiBicubic.Exists)
				using (var stream = fiBicubic.OpenRead())
					ShaderChain_bicubic = new Filters.RetroShaderChain(GL, new Filters.RetroShaderPreset(stream), System.IO.Path.Combine(PathManager.GetExeDirectoryAbsolute(), "Shaders/BizHawk"));

			LuaSurfaceSets["emu"] = new SwappableDisplaySurfaceSet();
			LuaSurfaceSets["native"] = new SwappableDisplaySurfaceSet();
			LuaSurfaceFrugalizers["emu"] = new TextureFrugalizer(GL);
			LuaSurfaceFrugalizers["native"] = new TextureFrugalizer(GL);

			RefreshUserShader();
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
		FilterManager.FilterProgram CurrentFilterProgram;

		/// <summary>
		/// these variables will track the dimensions of the last frame's (or the next frame? this is confusing) emulator native output size
		/// </summary>
		int currEmuWidth, currEmuHeight;

		TextureFrugalizer VideoTextureFrugalizer;
		Dictionary<string, TextureFrugalizer> LuaSurfaceFrugalizers = new Dictionary<string, TextureFrugalizer>();
		RenderTargetFrugalizer[] ShaderChainFrugalizers;
		Filters.RetroShaderChain ShaderChain_hq2x, ShaderChain_scanlines, ShaderChain_bicubic;
		Filters.RetroShaderChain ShaderChain_user;

		public void RefreshUserShader()
		{
			if (ShaderChain_user != null)
				ShaderChain_user.Dispose();
			if (File.Exists(Global.Config.DispUserFilterPath))
			{
				var fi = new FileInfo(Global.Config.DispUserFilterPath);
				using (var stream = fi.OpenRead())
					ShaderChain_user = new Filters.RetroShaderChain(GL, new Filters.RetroShaderPreset(stream), Path.GetDirectoryName(Global.Config.DispUserFilterPath));
			}
		}

		FilterManager.FilterProgram BuildDefaultChain(Size chain_insize, Size chain_outsize)
		{
			//select user special FX shader chain
			Dictionary<string, object> selectedChainProperties = new Dictionary<string, object>();
			Filters.RetroShaderChain selectedChain = null;
			if (Global.Config.TargetDisplayFilter == 1 && ShaderChain_hq2x != null && ShaderChain_hq2x.Available)
				selectedChain = ShaderChain_hq2x;
			if (Global.Config.TargetDisplayFilter == 2 && ShaderChain_scanlines != null && ShaderChain_scanlines.Available)
			{
				//shader.Pipeline["uIntensity"].Set(1.0f - Global.Config.TargetScanlineFilterIntensity / 256.0f);
				selectedChain = ShaderChain_scanlines;
				selectedChainProperties["uIntensity"] = 1.0f - Global.Config.TargetScanlineFilterIntensity / 256.0f;
			}
			if (Global.Config.TargetDisplayFilter == 3 && ShaderChain_user != null && ShaderChain_user.Available)
				selectedChain = ShaderChain_user;

			Filters.FinalPresentation fPresent = new Filters.FinalPresentation(chain_outsize);
			Filters.SourceImage fInput = new Filters.SourceImage(chain_insize);
			Filters.OSD fOSD = new Filters.OSD();
			fOSD.RenderCallback = () =>
			{
				var size = fOSD.FindInput().SurfaceFormat.Size;
				Renderer.Begin(size.Width, size.Height);
				MyBlitter myBlitter = new MyBlitter(this);
				myBlitter.ClipBounds = new Rectangle(0, 0, size.Width, size.Height);
				Renderer.SetBlendState(GL.BlendNormal);
				GlobalWin.OSD.Begin(myBlitter);
				GlobalWin.OSD.DrawScreenInfo(myBlitter);
				GlobalWin.OSD.DrawMessages(myBlitter);
				Renderer.End();
			};

			FilterManager.FilterProgram chain = new FilterManager.FilterProgram();

			//add the first filter, encompassing output from the emulator core
			chain.AddFilter(fInput, "input");

			//add lua layer 'emu'
			AppendLuaLayer(chain, "emu");

			//add user-selected retro shader
			if (selectedChain != null)
				AppendRetroShaderChain(chain, "retroShader", selectedChain, selectedChainProperties);

			//choose final filter
			Filters.FinalPresentation.eFilterOption finalFilter = Filters.FinalPresentation.eFilterOption.None;
			if (Global.Config.DispFinalFilter == 1) finalFilter = Filters.FinalPresentation.eFilterOption.Bilinear;
			if (Global.Config.DispFinalFilter == 2) finalFilter = Filters.FinalPresentation.eFilterOption.Bicubic;
			//if bicubic is selected and unavailable, dont use it
			if (!ShaderChain_bicubic.Available && fPresent.FilterOption == Filters.FinalPresentation.eFilterOption.Bicubic)
			{
				finalFilter = Filters.FinalPresentation.eFilterOption.None;
			}
			fPresent.FilterOption = finalFilter;

			//now if bicubic is chosen, insert it
			if (finalFilter == Filters.FinalPresentation.eFilterOption.Bicubic)
				AppendRetroShaderChain(chain, "bicubic", ShaderChain_bicubic, null);

			//add final presentation 
			chain.AddFilter(fPresent, "presentation");

			//add lua layer 'native'
			AppendLuaLayer(chain, "native");

			//and OSD goes on top of that
			chain.AddFilter(fOSD, "osd");

			return chain;
		}

		void AppendRetroShaderChain(FilterManager.FilterProgram program, string name, Filters.RetroShaderChain retroChain, Dictionary<string, object> properties)
		{
			for (int i = 0; i < retroChain.Passes.Length; i++)
			{
				var pass = retroChain.Passes[i];
				var rsp = new Filters.RetroShaderPass(retroChain, i);
				string fname = string.Format("{0}[{1}]", name, i);
				program.AddFilter(rsp, fname);
				rsp.Parameters = properties;
			}
		}

		void AppendLuaLayer(FilterManager.FilterProgram chain, string name)
		{
			Texture2d luaNativeTexture = null;
			var luaNativeSurface = LuaSurfaceSets[name].GetCurrent();
			if (luaNativeSurface == null)
				return;
			luaNativeTexture = LuaSurfaceFrugalizers[name].Get(luaNativeSurface);
			var fLuaLayer = new Filters.LuaLayer();
			fLuaLayer.SetTexture(luaNativeTexture);
			chain.AddFilter(fLuaLayer, name);
		}

		/// <summary>
		/// Using the current filter program, turn a mouse coordinate from window space to the original emulator screen space.
		/// </summary>
		public Point UntransformPoint(Point p)
		{
			//first, turn it into a window coordinate
			p = presentationPanel.Control.PointToClient(p);
			
			//now, if theres no filter program active, just give up
			if (CurrentFilterProgram == null) return p;
			
			//otherwise, have the filter program untransform it
			Vector2 v = new Vector2(p.X, p.Y);
			v = CurrentFilterProgram.UntransformPoint("default",v);
			return new Point((int)v.X, (int)v.Y);
		}

		/// <summary>
		/// This will receive an emulated output frame from an IVideoProvider and run it through the complete frame processing pipeline
		/// Then it will stuff it into the bound PresentationPanel.
		/// ---
		/// If the int[] is size=1, then it contains an openGL texture ID (and the size should be as specified from videoProvider)
		/// Don't worry about the case where the frontend isnt using opengl; it isnt supported yet, and it will be my responsibility to deal with anyway
		/// </summary>
		public void UpdateSource(IVideoProvider videoProvider)
		{
			int[] videoBuffer = videoProvider.GetVideoBuffer();
			
TESTEROO:
			int bufferWidth = videoProvider.BufferWidth;
			int bufferHeight = videoProvider.BufferHeight;
			bool isGlTextureId = videoBuffer.Length == 1;


			BitmapBuffer bb = null;
			Texture2d videoTexture;
			if (isGlTextureId)
			{
				videoTexture = GL.WrapGLTexture2d(new IntPtr(videoBuffer[0]), bufferWidth, bufferHeight);
			}
			else
			{
				//wrap the videoprovider data in a BitmapBuffer (no point to refactoring that many IVideoProviders)
				bb = new BitmapBuffer(bufferWidth, bufferHeight, videoBuffer);

				//now, acquire the data sent from the videoProvider into a texture
				videoTexture = VideoTextureFrugalizer.Get(bb);
			}

			//TEST (to be removed once we have an actual example of bring in a texture ID from opengl emu core):
			if (!isGlTextureId)
			{
				videoBuffer = new int[1] { videoTexture.Id.ToInt32() };
				goto TESTEROO;
			}

			//record the size of what we received, since lua and stuff is gonna want to draw onto it
			currEmuWidth = bufferWidth;
			currEmuHeight = bufferHeight;

			//build the default filter chain and set it up with services filters will need
			Size chain_insize = new Size(bufferWidth, bufferHeight);
			Size chain_outsize = GraphicsControl.Size;
			CurrentFilterProgram = BuildDefaultChain(chain_insize, chain_outsize);
			CurrentFilterProgram.GuiRenderer = Renderer;
			CurrentFilterProgram.GL = GL;
			//chain.RenderTargetProvider = new DisplayManagerRenderTargetProvider((size) => ShaderChainFrugalizers);

			//setup the source image filter
			Filters.SourceImage fInput = CurrentFilterProgram["input"] as Filters.SourceImage;
			fInput.Texture = videoTexture;
			
			//setup the final presentation filter
			Filters.FinalPresentation fPresent = CurrentFilterProgram["presentation"] as Filters.FinalPresentation;
			fPresent.BackgroundColor = videoProvider.BackgroundColor;
			fPresent.GuiRenderer = Renderer;
			fPresent.GL = GL;

			CurrentFilterProgram.Compile("default", chain_insize, chain_outsize);	

			//run filter chain
			Texture2d texCurr = null;
			RenderTarget rtCurr = null;
			int rtCounter = 0;
			bool inFinalTarget = false;
			foreach (var step in CurrentFilterProgram.Program)
			{
				switch (step.Type)
				{
					case FilterManager.FilterProgram.ProgramStepType.Run:
					{
						int fi = (int)step.Args;
						var f = CurrentFilterProgram.Filters[fi];
						f.SetInput(texCurr);
						f.Run();
						var orec = f.FindOutput();
						if (orec != null)
						{
							if (orec.SurfaceDisposition == FilterManager.SurfaceDisposition.Texture)
							{
								texCurr = f.GetOutput();
								rtCurr = null;
							}
						}
						break;
					}
					case FilterManager.FilterProgram.ProgramStepType.NewTarget:
					{
						var size = (Size)step.Args;
						rtCurr = ShaderChainFrugalizers[rtCounter++].Get(size);
						rtCurr.Bind();
						CurrentFilterProgram.CurrRenderTarget = rtCurr;
						break;
					}
					case FilterManager.FilterProgram.ProgramStepType.FinalTarget:
						inFinalTarget = true;
						rtCurr = null;
						CurrentFilterProgram.CurrRenderTarget = null;
						GraphicsControl.Begin();
						break;
				}
			}
			Debug.Assert(inFinalTarget);

			//apply the vsync setting (should probably try to avoid repeating this)
			bool vsync = Global.Config.VSyncThrottle || Global.Config.VSync;
			//presentationPanel.GraphicsControl.SetVsync(vsync);

			//present and conclude drawing
			presentationPanel.GraphicsControl.SwapBuffers();

			//nope. dont do this. workaround for slow context switching on intel GPUs. just switch to another context when necessary before doing anything
			//presentationPanel.GraphicsControl.End();

			//cleanup:
			if(bb != null) bb.Dispose();
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

		public void ClearLuaSurfaces()
		{
			foreach (var kvp in LuaSurfaceSets)
			{
				var surf = LockLuaSurface(kvp.Key);
				surf.Clear();
				UnlockLuaSurface(surf);
			}
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

		
	}

}