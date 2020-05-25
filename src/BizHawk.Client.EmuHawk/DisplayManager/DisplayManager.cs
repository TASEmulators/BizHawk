// TODO
// we could flag textures as 'actually' render targets (keep a reference to the render target?) which could allow us to convert between them more quickly in some cases

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.FilterManager;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;

using OpenTK;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A DisplayManager is destined forevermore to drive the PresentationPanel it gets initialized with.
	/// Its job is to receive OSD and emulator outputs, and produce one single buffer (BitmapBuffer? Texture2d?) for display by the PresentationPanel.
	/// Details TBD
	/// </summary>
	public class DisplayManager : IDisposable
	{
		private class DisplayManagerRenderTargetProvider : IRenderTargetProvider
		{
			private readonly Func<Size, RenderTarget> _callback;

			RenderTarget IRenderTargetProvider.Get(Size size)
			{
				return _callback(size);
			}

			public DisplayManagerRenderTargetProvider(Func<Size, RenderTarget> callback)
			{
				_callback = callback;
			}
		}

		public DisplayManager(PresentationPanel presentationPanel)
		{
			GL = GlobalWin.GL;
			GLManager = GlobalWin.GLManager;
			this.presentationPanel = presentationPanel;
			GraphicsControl = this.presentationPanel.GraphicsControl;
			CR_GraphicsControl = GLManager.GetContextForGraphicsControl(GraphicsControl);

			// it's sort of important for these to be initialized to something nonzero
			currEmuWidth = currEmuHeight = 1;

			Renderer = GL.CreateRenderer();

			VideoTextureFrugalizer = new TextureFrugalizer(GL);

			ShaderChainFrugalizers = new RenderTargetFrugalizer[16]; // hacky hardcoded limit.. need some other way to manage these
			for (int i = 0; i < 16; i++)
			{
				ShaderChainFrugalizers[i] = new RenderTargetFrugalizer(GL);
			}

			using (var xml = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Client.EmuHawk.Resources.courier16px.fnt"))
			{
				using var tex = typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Client.EmuHawk.Resources.courier16px_0.png");
				TheOneFont = new StringRenderer(GL, xml, tex);
			}

			using (var gens =
				typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Client.EmuHawk.Resources.gens.ttf"))
			{
				LoadCustomFont(gens);
			}

			using (var fceux =
				typeof(Program).Assembly.GetManifestResourceStream("BizHawk.Client.EmuHawk.Resources.fceux.ttf"))
			{
				LoadCustomFont(fceux);
			}

			if (GL is IGL_TK || GL is IGL_SlimDX9)
			{
				var fiHq2x = new FileInfo(Path.Combine(PathUtils.ExeDirectoryPath, "Shaders/BizHawk/hq2x.cgp"));
				if (fiHq2x.Exists)
				{
					using var stream = fiHq2x.OpenRead();
					ShaderChain_hq2x = new Filters.RetroShaderChain(GL, new Filters.RetroShaderPreset(stream), Path.Combine(PathUtils.ExeDirectoryPath, "Shaders/BizHawk"));
				}
				var fiScanlines = new FileInfo(Path.Combine(PathUtils.ExeDirectoryPath, "Shaders/BizHawk/BizScanlines.cgp"));
				if (fiScanlines.Exists)
				{
					using var stream = fiScanlines.OpenRead();
					ShaderChain_scanlines = new Filters.RetroShaderChain(GL, new Filters.RetroShaderPreset(stream), Path.Combine(PathUtils.ExeDirectoryPath, "Shaders/BizHawk"));
				}

				string bicubicPath = "Shaders/BizHawk/bicubic-fast.cgp";
				if (GL is IGL_SlimDX9)
				{
					bicubicPath = "Shaders/BizHawk/bicubic-normal.cgp";
				}
				var fiBicubic = new FileInfo(Path.Combine(PathUtils.ExeDirectoryPath, bicubicPath));
				if (fiBicubic.Exists)
				{
					using var stream = fiBicubic.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
					ShaderChain_bicubic = new Filters.RetroShaderChain(GL, new Filters.RetroShaderPreset(stream), Path.Combine(PathUtils.ExeDirectoryPath, "Shaders/BizHawk"));
				}
			}

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
			{
				f.Dispose();
			}

			foreach (var f in ShaderChainFrugalizers)
			{
				f?.Dispose();
			}

			foreach (var s in new[] { ShaderChain_hq2x, ShaderChain_scanlines, ShaderChain_bicubic, ShaderChain_user })
			{
				s?.Dispose();
			}

			TheOneFont.Dispose();
			Renderer.Dispose();
		}

		// rendering resources:
		private readonly IGL GL;
		private readonly GLManager GLManager;
		private readonly StringRenderer TheOneFont;
		private readonly IGuiRenderer Renderer;

		// layer resources
		private readonly PresentationPanel presentationPanel; // well, its the final layer's target, at least
		private readonly GraphicsControl GraphicsControl; // well, its the final layer's target, at least
		private readonly GLManager.ContextRef CR_GraphicsControl;
		private FilterProgram _currentFilterProgram;

		/// <summary>
		/// these variables will track the dimensions of the last frame's (or the next frame? this is confusing) emulator native output size
		/// THIS IS OLD JUNK. I should get rid of it, I think. complex results from the last filter ingestion should be saved instead.
		/// </summary>
		private int currEmuWidth, currEmuHeight;

		/// <summary>
		/// additional pixels added at the unscaled level for the use of lua drawing. essentially increases the input video provider dimensions
		/// </summary>
		public Padding GameExtraPadding { get; set; }

		/// <summary>
		/// additional pixels added at the native level for the use of lua drawing. essentially just gets tacked onto the final calculated window sizes.
		/// </summary>
		public Padding ClientExtraPadding { get; set; }

		/// <summary>
		/// custom fonts that don't need to be installed on the user side
		/// </summary>
		public PrivateFontCollection CustomFonts = new PrivateFontCollection();

		private readonly TextureFrugalizer VideoTextureFrugalizer;
		private readonly Dictionary<string, TextureFrugalizer> LuaSurfaceFrugalizers = new Dictionary<string, TextureFrugalizer>();
		private readonly RenderTargetFrugalizer[] ShaderChainFrugalizers;
		private readonly Filters.RetroShaderChain ShaderChain_hq2x, ShaderChain_scanlines, ShaderChain_bicubic;
		private Filters.RetroShaderChain ShaderChain_user;

		public void RefreshUserShader()
		{
			ShaderChain_user?.Dispose();
			if (File.Exists(Global.Config.DispUserFilterPath))
			{
				var fi = new FileInfo(Global.Config.DispUserFilterPath);
				using var stream = fi.OpenRead();
				ShaderChain_user = new Filters.RetroShaderChain(GL, new Filters.RetroShaderPreset(stream), Path.GetDirectoryName(Global.Config.DispUserFilterPath));
			}
		}

		private Padding CalculateCompleteContentPadding(bool user, bool source)
		{
			var padding = new Padding();

			if (user)
			{
				padding += GameExtraPadding;
			}

			// an experimental feature
			if (source && GlobalWin.Emulator is Octoshock psx)
			{
				var corePadding = psx.VideoProvider_Padding;
				padding.Left += corePadding.Width / 2;
				padding.Right += corePadding.Width - corePadding.Width / 2;
				padding.Top += corePadding.Height / 2;
				padding.Bottom += corePadding.Height - corePadding.Height / 2;
			}

			// apply user's crop selections as a negative padding (believe it or not, this largely works)
			// is there an issue with the aspect ratio? I don't know--but if there is, there would be with the padding too
			padding.Left -= Global.Config.DispCropLeft;
			padding.Right -= Global.Config.DispCropRight;
			padding.Top -= Global.Config.DispCropTop;
			padding.Bottom -= Global.Config.DispCropBottom;

			return padding;
		}

		private FilterProgram BuildDefaultChain(Size chainInSize, Size chainOutSize, bool includeOSD, bool includeUserFilters)
		{
			// select user special FX shader chain
			var selectedChainProperties = new Dictionary<string, object>();
			Filters.RetroShaderChain selectedChain = null;
			if (Global.Config.TargetDisplayFilter == 1 && ShaderChain_hq2x != null && ShaderChain_hq2x.Available)
			{
				selectedChain = ShaderChain_hq2x;
			}

			if (Global.Config.TargetDisplayFilter == 2 && ShaderChain_scanlines != null && ShaderChain_scanlines.Available)
			{
				selectedChain = ShaderChain_scanlines;
				selectedChainProperties["uIntensity"] = 1.0f - Global.Config.TargetScanlineFilterIntensity / 256.0f;
			}

			if (Global.Config.TargetDisplayFilter == 3 && ShaderChain_user != null && ShaderChain_user.Available)
			{
				selectedChain = ShaderChain_user;
			}

			if (!includeUserFilters)
				selectedChain = null;

			Filters.BaseFilter fCoreScreenControl = CreateCoreScreenControl();

			var fPresent = new Filters.FinalPresentation(chainOutSize);
			var fInput = new Filters.SourceImage(chainInSize);
			var fOSD = new Filters.OSD();
			fOSD.RenderCallback = () =>
			{
				if (!includeOSD)
				{
					return;
				}

				var size = fOSD.FindInput().SurfaceFormat.Size;
				Renderer.Begin(size.Width, size.Height);
				var myBlitter = new MyBlitter(this)
				{
					ClipBounds = new Rectangle(0, 0, size.Width, size.Height)
				};
				Renderer.SetBlendState(GL.BlendNormal);
				GlobalWin.OSD.Begin(myBlitter);
				GlobalWin.OSD.DrawScreenInfo(myBlitter);
				GlobalWin.OSD.DrawMessages(myBlitter);
				Renderer.End();
			};

			var chain = new FilterProgram();

			//add the first filter, encompassing output from the emulator core
			chain.AddFilter(fInput, "input");

			if (fCoreScreenControl != null)
				chain.AddFilter(fCoreScreenControl, "CoreScreenControl");

			// if a non-zero padding is required, add a filter to allow for that
			// note, we have two sources of padding right now.. one can come from the VideoProvider and one from the user.
			// we're combining these now and just using black, for sake of being lean, despite the discussion below:
			// keep in mind, the VideoProvider design in principle might call for another color.
			// we haven't really been using this very hard, but users will probably want black there (they could fill it to another color if needed tho)
			var padding = CalculateCompleteContentPadding(true, true);
			if (padding.Vertical != 0 || padding.Horizontal != 0)
			{
				// TODO - add another filter just for this, its cumbersome to use final presentation... I think. but maybe there's enough similarities to justify it.
				Size size = chainInSize;
				size.Width += padding.Horizontal;
				size.Height += padding.Vertical;
				Filters.FinalPresentation fPadding = new Filters.FinalPresentation(size);
				chain.AddFilter(fPadding, "padding");
				fPadding.GuiRenderer = Renderer;
				fPadding.GL = GL;
				fPadding.Config_PadOnly = true;
				fPadding.Padding = padding;
			}

			//add lua layer 'emu'
			AppendLuaLayer(chain, "emu");

			if(includeUserFilters)
				if (Global.Config.DispPrescale != 1)
				{
					var fPrescale = new Filters.PrescaleFilter() { Scale = Global.Config.DispPrescale };
					chain.AddFilter(fPrescale, "user_prescale");
				}

			// add user-selected retro shader
			if (selectedChain != null)
				AppendRetroShaderChain(chain, "retroShader", selectedChain, selectedChainProperties);

			// AutoPrescale makes no sense for a None final filter
			if (Global.Config.DispAutoPrescale && Global.Config.DispFinalFilter != (int)Filters.FinalPresentation.eFilterOption.None)
			{
				var apf = new Filters.AutoPrescaleFilter();
				chain.AddFilter(apf, "auto_prescale");
			}

			//choose final filter
			var finalFilter = Filters.FinalPresentation.eFilterOption.None;
			if (Global.Config.DispFinalFilter == 1)
			{
				finalFilter = Filters.FinalPresentation.eFilterOption.Bilinear;
			}

			if (Global.Config.DispFinalFilter == 2)
			{
				finalFilter = Filters.FinalPresentation.eFilterOption.Bicubic;
			}

			//if bicubic is selected and unavailable, don't use it. use bilinear instead I guess
			if (finalFilter == Filters.FinalPresentation.eFilterOption.Bicubic)
			{
				if (ShaderChain_bicubic == null || !ShaderChain_bicubic.Available)
				{
					finalFilter = Filters.FinalPresentation.eFilterOption.Bilinear;
				}
			}

			fPresent.FilterOption = finalFilter;

			// now if bicubic is chosen, insert it
			if (finalFilter == Filters.FinalPresentation.eFilterOption.Bicubic)
			{
				AppendRetroShaderChain(chain, "bicubic", ShaderChain_bicubic, null);
			}

			// add final presentation
			if (includeUserFilters)
				chain.AddFilter(fPresent, "presentation");

			//add lua layer 'native'
			AppendLuaLayer(chain, "native");

			// and OSD goes on top of that
			// TODO - things break if this isn't present (the final presentation filter gets messed up when used with prescaling)
			// so, always include it (we'll handle this flag in the callback to do no rendering)
			//if (includeOSD)
				chain.AddFilter(fOSD, "osd");

			return chain;
		}

		private void AppendRetroShaderChain(FilterProgram program, string name, Filters.RetroShaderChain retroChain, Dictionary<string, object> properties)
		{
			for (int i = 0; i < retroChain.Passes.Length; i++)
			{
				var pass = retroChain.Passes[i];
				var rsp = new Filters.RetroShaderPass(retroChain, i);
				string fname = $"{name}[{i}]";
				program.AddFilter(rsp, fname);
				rsp.Parameters = properties;
			}
		}

		private void AppendLuaLayer(FilterProgram chain, string name)
		{
			var luaNativeSurface = LuaSurfaceSets[name].GetCurrent();
			if (luaNativeSurface == null)
			{
				return;
			}

			Texture2d luaNativeTexture = LuaSurfaceFrugalizers[name].Get(luaNativeSurface);
			var fLuaLayer = new Filters.LuaLayer();
			fLuaLayer.SetTexture(luaNativeTexture);
			chain.AddFilter(fLuaLayer, name);
		}

		/// <summary>
		/// Using the current filter program, turn a mouse coordinate from window space to the original emulator screen space.
		/// </summary>
		public Point UntransformPoint(Point p)
		{
			// first, turn it into a window coordinate
			p = presentationPanel.Control.PointToClient(p);

			// now, if there's no filter program active, just give up
			if (_currentFilterProgram == null) return p;

			// otherwise, have the filter program untransform it
			Vector2 v = new Vector2(p.X, p.Y);
			v = _currentFilterProgram.UntransformPoint("default", v);

			// Poop
			//if (Global.Emulator is MelonDS ds && ds.TouchScreenStart.HasValue)
			//{
			//	Point touchLocation = ds.TouchScreenStart.Value;
			//	v.Y = (int)((double)ds.BufferHeight / MelonDS.NativeHeight * (v.Y - touchLocation.Y));
			//	v.X = (int)((double)ds.BufferWidth / MelonDS.NativeWidth * (v.X - touchLocation.X));
			//}

			return new Point((int)v.X, (int)v.Y);
		}

		/// <summary>
		/// Using the current filter program, turn a emulator screen space coordinate to a window coordinate (suitable for lua layer drawing)
		/// </summary>
		public Point TransformPoint(Point p)
		{
			//now, if there's no filter program active, just give up
			if (_currentFilterProgram == null)
			{
				return p;
			}

			// otherwise, have the filter program untransform it
			Vector2 v = new Vector2(p.X, p.Y);
			v = _currentFilterProgram.TransformPoint("default", v);
			return new Point((int)v.X, (int)v.Y);
		}

		/// <summary>
		/// This will receive an emulated output frame from an IVideoProvider and run it through the complete frame processing pipeline
		/// Then it will stuff it into the bound PresentationPanel.
		/// ---
		/// If the int[] is size=1, then it contains an openGL texture ID (and the size should be as specified from videoProvider)
		/// Don't worry about the case where the frontend isnt using opengl; DisplayManager deals with it
		/// </summary>
		public void UpdateSource(IVideoProvider videoProvider)
		{
			bool displayNothing = Global.Config.DispSpeedupFeatures == 0;
			var job = new JobInfo
			{
				VideoProvider = videoProvider,
				Simulate = displayNothing,
				ChainOutsize = GraphicsControl.Size,
				IncludeOSD = true,
				IncludeUserFilters = true
			};
			UpdateSourceInternal(job);
		}

		Filters.BaseFilter CreateCoreScreenControl()
		{
			if (GlobalWin.Emulator is MelonDS nds)
			{
				//TODO: need to pipe layout settings into here now
				var filter = new Filters.ScreenControlNDS(nds);
				return filter;
			}

			return null;
		}

		public BitmapBuffer RenderVideoProvider(IVideoProvider videoProvider)
		{
			// TODO - we might need to gather more Global.Config.DispXXX properties here, so they can be overridden
			var targetSize = new Size(videoProvider.BufferWidth, videoProvider.BufferHeight);
			var padding = CalculateCompleteContentPadding(true,true);
			targetSize.Width += padding.Horizontal;
			targetSize.Height += padding.Vertical;

			var job = new JobInfo
			{
				VideoProvider = videoProvider,
				Simulate = false,
				ChainOutsize = targetSize,
				Offscreen = true,
				IncludeOSD = false,
				IncludeUserFilters = false
			};
			UpdateSourceInternal(job);
			return job.OffscreenBb;
		}

		/// <summary>
		/// Does the entire display process to an offscreen buffer, suitable for a 'client' screenshot.
		/// </summary>
		public BitmapBuffer RenderOffscreen(IVideoProvider videoProvider, bool includeOSD)
		{
			var job = new JobInfo
			{
				VideoProvider = videoProvider,
				Simulate = false,
				ChainOutsize = GraphicsControl.Size,
				Offscreen = true,
				IncludeOSD = includeOSD,
				IncludeUserFilters = true,
			};
			UpdateSourceInternal(job);
			return job.OffscreenBb;
		}

		private class FakeVideoProvider : IVideoProvider
		{
			public int[] GetVideoBuffer() => new int[] {};

			public int VirtualWidth { get; set; }
			public int VirtualHeight { get; set; }

			public int BufferWidth { get; set; }
			public int BufferHeight { get; set; }
			public int BackgroundColor { get; set; }

			/// <exception cref="InvalidOperationException">always</exception>
			public int VsyncNumerator => throw new InvalidOperationException();

			/// <exception cref="InvalidOperationException">always</exception>
			public int VsyncDenominator => throw new InvalidOperationException();
		}

		void FixRatio(float x, float y, int inw, int inh, out int outW, out int outH)
		{
			float ratio = x / y;
			if (ratio <= 1)
			{
				// taller. weird. expand height.
				outW = inw;
				outH = (int)(inw / ratio);
			}
			else
			{
				// wider. normal. expand width.
				outW = (int)(inh * ratio);
				outH = inh;
			}
		}

		/// <summary>
		/// Attempts to calculate a good client size with the given zoom factor, considering the user's DisplayManager preferences
		/// TODO - this needs to be redone with a concept different from zoom factor.
		/// basically, each increment of a 'zoom-like' factor should definitely increase the viewable area somehow, even if it isnt strictly by an entire zoom level.
		/// </summary>
		public Size CalculateClientSize(IVideoProvider videoProvider, int zoom)
		{
			bool arActive = Global.Config.DispFixAspectRatio;
			bool arSystem = Global.Config.DispManagerAR == EDispManagerAR.System;
			bool arCustom = Global.Config.DispManagerAR == EDispManagerAR.Custom;
			bool arCustomRatio = Global.Config.DispManagerAR == EDispManagerAR.CustomRatio;
			bool arCorrect = arSystem || arCustom || arCustomRatio;
			bool arInteger = Global.Config.DispFixScaleInteger;

			int bufferWidth = videoProvider.BufferWidth;
			int bufferHeight = videoProvider.BufferHeight;
			int virtualWidth = videoProvider.VirtualWidth;
			int virtualHeight = videoProvider.VirtualHeight;

			if (arCustom)
			{
				virtualWidth = Global.Config.DispCustomUserARWidth;
				virtualHeight = Global.Config.DispCustomUserARHeight;
			}

			if (arCustomRatio)
			{
				FixRatio(Global.Config.DispCustomUserArx, Global.Config.DispCustomUserAry, videoProvider.BufferWidth, videoProvider.BufferHeight, out virtualWidth, out virtualHeight);
			}

			//TODO: it is bad that this is happening outside the filter chain
			//the filter chain has the ability to add padding...
			//for now, we have to have some hacks. this could be improved by refactoring the filter setup hacks to be in one place only though
			//could the PADDING be done as filters too? that would be nice.
			var fCoreScreenControl = CreateCoreScreenControl();
			if(fCoreScreenControl != null)
			{
				var sz = fCoreScreenControl.PresizeInput("default", new Size(bufferWidth, bufferHeight));
				virtualWidth = bufferWidth = sz.Width;
				virtualHeight = bufferHeight = sz.Height;
			}

			var padding = CalculateCompleteContentPadding(true, false);
			virtualWidth += padding.Horizontal;
			virtualHeight += padding.Vertical;

			padding = CalculateCompleteContentPadding(true, true);
			bufferWidth += padding.Horizontal;
			bufferHeight += padding.Vertical;


			// old stuff
			var fvp = new FakeVideoProvider
			{
				BufferWidth = bufferWidth,
				BufferHeight = bufferHeight,
				VirtualWidth = virtualWidth,
				VirtualHeight = virtualHeight
			};

			Size chainOutsize;

			if (arActive)
			{
				if (arCorrect)
				{
					if (arInteger)
					{
						// ALERT COPYPASTE LAUNDROMAT
						Vector2 VS = new Vector2(virtualWidth, virtualHeight);
						Vector2 BS = new Vector2(bufferWidth, bufferHeight);
						Vector2 AR = Vector2.Divide(VS, BS);
						float targetPar = AR.X / AR.Y;

						// this would malfunction for AR <= 0.5 or AR >= 2.0
						// EDIT - in fact, we have AR like that coming from PSX, sometimes, so maybe we should solve this better
						Vector2 PS = new Vector2(1, 1);

						// here's how we define zooming, in this case:
						// make sure each step is an increment of zoom for at least one of the dimensions (or maybe both of them)
						// look for the increment which helps the AR the best
						//TODO - this cant possibly support scale factors like 1.5x
						//TODO - also, this might be messing up zooms and stuff, we might need to run this on the output size of the filter chain
						for (int i = 1; i < zoom;i++)
						{
							//would not be good to run this per frame, but it seems to only run when the resolution changes, etc.
							Vector2[] trials = 
							{
								PS + new Vector2(1, 0),
								PS + new Vector2(0, 1),
								PS + new Vector2(1, 1)
							};
							int bestIndex = -1;
							float bestValue = 1000.0f;
							for (int t = 0; t < trials.Length; t++)
							{
								//I.
								float testAr = trials[t].X / trials[t].Y;

								// II.
								//Vector2 calc = Vector2.Multiply(trials[t], VS);
								//float test_ar = calc.X / calc.Y;

								// not clear which approach is superior
								float deviationLinear = Math.Abs(testAr - targetPar);
								float deviationGeom = testAr / targetPar;
								if (deviationGeom < 1)
								{
									deviationGeom = 1.0f / deviationGeom;
								}

								float value = deviationLinear;
								if (value < bestValue)
								{
									bestIndex = t;
									bestValue = value;
								}
							}

							// is it possible to get here without selecting one? doubtful.
							// EDIT: YES IT IS. it happened with an 0,0 buffer size. of course, that was a mistake, but we shouldn't crash
							if (bestIndex != -1) // so, what now? well, this will result in 0,0 getting picked, so that's probably all we can do
							{
								PS = trials[bestIndex];
							}
						}

						chainOutsize = new Size((int)(bufferWidth * PS.X), (int)(bufferHeight * PS.Y));
					}
					else
					{
						// obey the AR, but allow free scaling: just zoom the virtual size
						chainOutsize = new Size(virtualWidth * zoom, virtualHeight * zoom);
					}
				}
				else
				{
					// ar_unity:
					// just choose to zoom the buffer (make no effort to incorporate AR)
					chainOutsize = new Size(bufferWidth * zoom, bufferHeight * zoom);
				}
			}
			else
			{
				// !ar_active:
				// just choose to zoom the buffer (make no effort to incorporate AR)
				chainOutsize = new Size(bufferWidth * zoom, bufferHeight * zoom);
			}

			chainOutsize.Width += ClientExtraPadding.Horizontal;
			chainOutsize.Height += ClientExtraPadding.Vertical;

			var job = new JobInfo
			{
				VideoProvider = fvp,
				Simulate = true,
				ChainOutsize = chainOutsize,
				IncludeUserFilters = true,
				IncludeOSD = true,
			};
			var filterProgram = UpdateSourceInternal(job);

			// this only happens when we're forcing the client to size itself with autoload and the core says 0x0....
			// we need some other more sensible client size.
			if (filterProgram == null)
			{
				return new Size(256, 192);
			}

			var size = filterProgram.Filters.Last().FindOutput().SurfaceFormat.Size;

			return size;
		}

		private class JobInfo
		{
			public IVideoProvider VideoProvider;
			public bool Simulate;
			public Size ChainOutsize;
			public bool Offscreen;
			public BitmapBuffer OffscreenBb;
			public bool IncludeOSD;

			/// <summary>
			/// This has been changed a bit to mean "not raw".
			/// Someone needs to rename it, but the sense needs to be inverted and some method args need renaming too
			/// Suggested: IsRaw (with inverted sense)
			/// </summary>
			public bool IncludeUserFilters;
		}

		private FilterProgram UpdateSourceInternal(JobInfo job)
		{
			//no drawing actually happens. it's important not to begin drawing on a control
			if (!job.Simulate && !job.Offscreen)
			{
				GLManager.Activate(CR_GraphicsControl);

				if (job.ChainOutsize.Width == 0 || job.ChainOutsize.Height == 0)
				{
					// this has to be a NOP, because lots of stuff will malfunction on a 0-sized viewport
					if (_currentFilterProgram != null)
					{
						UpdateSourceDrawingWork(job); //but we still need to do this, because of vsync
					}

					return null;
				}
			}

			IVideoProvider videoProvider = job.VideoProvider;
			bool simulate = job.Simulate;
			Size chainOutsize = job.ChainOutsize;

			//simulate = true;

			int[] videoBuffer = videoProvider.GetVideoBuffer();
			int bufferWidth = videoProvider.BufferWidth;
			int bufferHeight = videoProvider.BufferHeight;
			int presenterTextureWidth = bufferWidth;
			int presenterTextureHeight = bufferHeight;
			bool isGlTextureId = videoBuffer.Length == 1;

			int vw = videoProvider.VirtualWidth;
			int vh = videoProvider.VirtualHeight;

			//TODO: it is bad that this is happening outside the filter chain
			//the filter chain has the ability to add padding...
			//for now, we have to have some hacks. this could be improved by refactoring the filter setup hacks to be in one place only though
			//could the PADDING be done as filters too? that would be nice.
			var fCoreScreenControl = CreateCoreScreenControl();
			if(fCoreScreenControl != null)
			{
				var sz = fCoreScreenControl.PresizeInput("default", new Size(bufferWidth, bufferHeight));
				presenterTextureWidth = vw = sz.Width;
				presenterTextureHeight = vh = sz.Height;
			}

			if (Global.Config.DispFixAspectRatio)
			{
				if (Global.Config.DispManagerAR == EDispManagerAR.System)
				{
					//Already set
				}
				if (Global.Config.DispManagerAR == EDispManagerAR.Custom)
				{
					//not clear what any of these other options mean for "screen controlled" systems
					vw = Global.Config.DispCustomUserARWidth;
					vh = Global.Config.DispCustomUserARHeight;
				}
				if (Global.Config.DispManagerAR == EDispManagerAR.CustomRatio)
				{
					//not clear what any of these other options mean for "screen controlled" systems
					FixRatio(Global.Config.DispCustomUserArx, Global.Config.DispCustomUserAry, videoProvider.BufferWidth, videoProvider.BufferHeight, out vw, out vh);
				}
			}

			var padding = CalculateCompleteContentPadding(true,false);
			vw += padding.Horizontal;
			vh += padding.Vertical;

			BitmapBuffer bb = null;
			Texture2d videoTexture = null;
			if (!simulate)
			{
				if (isGlTextureId)
				{
					//FYI: this is a million years from happening on n64, since it's all geriatric non-FBO code
					//is it workable for saturn?
					videoTexture = GL.WrapGLTexture2d(new IntPtr(videoBuffer[0]), bufferWidth, bufferHeight);
				}
				else
				{
					//wrap the VideoProvider data in a BitmapBuffer (no point to refactoring that many IVideoProviders)
					bb = new BitmapBuffer(bufferWidth, bufferHeight, videoBuffer);
					bb.DiscardAlpha();

					//now, acquire the data sent from the videoProvider into a texture
					videoTexture = VideoTextureFrugalizer.Get(bb);

					// lets not use this. lets define BizwareGL to make clamp by default (TBD: check opengl)
					//GL.SetTextureWrapMode(videoTexture, true);
				}
			}

			// record the size of what we received, since lua and stuff is gonna want to draw onto it
			currEmuWidth = bufferWidth;
			currEmuHeight = bufferHeight;

			//build the default filter chain and set it up with services filters will need
			Size chainInsize = new Size(bufferWidth, bufferHeight);

			var filterProgram = BuildDefaultChain(chainInsize, chainOutsize, job.IncludeOSD, job.IncludeUserFilters);
			filterProgram.GuiRenderer = Renderer;
			filterProgram.GL = GL;

			//setup the source image filter
			Filters.SourceImage fInput = filterProgram["input"] as Filters.SourceImage;
			fInput.Texture = videoTexture;

			//setup the final presentation filter
			Filters.FinalPresentation fPresent = filterProgram["presentation"] as Filters.FinalPresentation;
			if (fPresent != null)
			{
				fPresent.VirtualTextureSize = new Size(vw, vh);
				fPresent.TextureSize = new Size(presenterTextureWidth, presenterTextureHeight);
				fPresent.BackgroundColor = videoProvider.BackgroundColor;
				fPresent.GuiRenderer = Renderer;
				fPresent.Flip = isGlTextureId;
				fPresent.Config_FixAspectRatio = Global.Config.DispFixAspectRatio;
				fPresent.Config_FixScaleInteger = Global.Config.DispFixScaleInteger;
				fPresent.Padding = ClientExtraPadding;
				fPresent.AutoPrescale = Global.Config.DispAutoPrescale;

				fPresent.GL = GL;
			}

			//POOPY. why are we delivering the GL context this way? such bad
			Filters.ScreenControlNDS fNDS = filterProgram["CoreScreenControl"] as Filters.ScreenControlNDS;
			if (fNDS != null)
			{
				fNDS.GuiRenderer = Renderer;
				fNDS.GL = GL;
			}

			filterProgram.Compile("default", chainInsize, chainOutsize, !job.Offscreen);

			if (simulate)
			{
			}
			else
			{
				_currentFilterProgram = filterProgram;
				UpdateSourceDrawingWork(job);
			}

			// cleanup:
			bb?.Dispose();

			return filterProgram;
		}

		public void Blank()
		{
			GLManager.Activate(CR_GraphicsControl);
			GL.BeginScene();
			GL.BindRenderTarget(null);
			GL.SetClearColor(Color.Black);
			GL.Clear(OpenTK.Graphics.OpenGL.ClearBufferMask.ColorBufferBit);
			GL.EndScene();
			presentationPanel.GraphicsControl.SwapBuffers();
		}

		private void UpdateSourceDrawingWork(JobInfo job)
		{
			bool alternateVsync = false;

			// only used by alternate vsync
			IGL_SlimDX9 dx9 = null;

			if (!job.Offscreen)
			{
				//apply the vsync setting (should probably try to avoid repeating this)
				var vsync = Global.Config.VSyncThrottle || Global.Config.VSync;

				//ok, now this is a bit undesirable.
				//maybe the user wants vsync, but not vsync throttle.
				//this makes sense... but we don't have the infrastructure to support it now (we'd have to enable triple buffering or something like that)
				//so what we're gonna do is disable vsync no matter what if throttling is off, and maybe nobody will notice.
				//update 26-mar-2016: this upsets me. When fast-forwarding and skipping frames, vsync should still work. But I'm not changing it yet
				if (GlobalWin.DisableSecondaryThrottling)
					vsync = false;

				//for now, it's assumed that the presentation panel is the main window, but that may not always be true
				if (vsync && Global.Config.DispAlternateVsync && Global.Config.VSyncThrottle)
				{
					dx9 = GL as IGL_SlimDX9;
					if (dx9 != null)
					{
						alternateVsync = true;
						//unset normal vsync if we've chosen the alternate vsync
						vsync = false;
					}
				}

				//TODO - whats so hard about triple buffering anyway? just enable it always, and change api to SetVsync(enable,throttle)
				//maybe even SetVsync(enable,throttlemethod) or just SetVsync(enable,throttle,advanced)

				if (LastVsyncSetting != vsync || LastVsyncSettingGraphicsControl != presentationPanel.GraphicsControl)
				{
					if (LastVsyncSetting == null && vsync)
					{
						// Workaround for vsync not taking effect at startup (Intel graphics related?)
						presentationPanel.GraphicsControl.SetVsync(false);
					}
					presentationPanel.GraphicsControl.SetVsync(vsync);
					LastVsyncSettingGraphicsControl = presentationPanel.GraphicsControl;
					LastVsyncSetting = vsync;
				}
			}

			// begin rendering on this context
			// should this have been done earlier?
			// do i need to check this on an intel video card to see if running excessively is a problem? (it used to be in the FinalTarget command below, shouldn't be a problem)
			//GraphicsControl.Begin(); // CRITICAL POINT for yabause+GL

			//TODO - auto-create and age these (and dispose when old)
			int rtCounter = 0;

			_currentFilterProgram.RenderTargetProvider = new DisplayManagerRenderTargetProvider(size => ShaderChainFrugalizers[rtCounter++].Get(size));

			GL.BeginScene();

			// run filter chain
			Texture2d texCurr = null;
			RenderTarget rtCurr = null;
			bool inFinalTarget = false;
			foreach (var step in _currentFilterProgram.Program)
			{
				switch (step.Type)
				{
					case FilterProgram.ProgramStepType.Run:
						{
							int fi = (int)step.Args;
							var f = _currentFilterProgram.Filters[fi];
							f.SetInput(texCurr);
							f.Run();
							var orec = f.FindOutput();
							if (orec != null)
							{
								if (orec.SurfaceDisposition == SurfaceDisposition.Texture)
								{
									texCurr = f.GetOutput();
									rtCurr = null;
								}
							}
							break;
						}
					case FilterProgram.ProgramStepType.NewTarget:
						{
							var size = (Size)step.Args;
							rtCurr = ShaderChainFrugalizers[rtCounter++].Get(size);
							rtCurr.Bind();
							_currentFilterProgram.CurrRenderTarget = rtCurr;
							break;
						}
					case FilterProgram.ProgramStepType.FinalTarget:
						{
							inFinalTarget = true;
							rtCurr = null;
							_currentFilterProgram.CurrRenderTarget = null;
							GL.BindRenderTarget(null);
							break;
						}
				}
			}

			GL.EndScene();

			if (job.Offscreen)
			{
				job.OffscreenBb = rtCurr.Texture2d.Resolve();
				job.OffscreenBb.DiscardAlpha();
			}
			else
			{
				Debug.Assert(inFinalTarget);

				// wait for vsync to begin
				if (alternateVsync) dx9.AlternateVsyncPass(0);

				// present and conclude drawing
				presentationPanel.GraphicsControl.SwapBuffers();

				// wait for vsync to end
				if (alternateVsync) dx9.AlternateVsyncPass(1);

				// nope. don't do this. workaround for slow context switching on intel GPUs. just switch to another context when necessary before doing anything
				// presentationPanel.GraphicsControl.End();
			}
		}

		private void LoadCustomFont(Stream fontStream)
		{
			IntPtr data = Marshal.AllocCoTaskMem((int)fontStream.Length);
			byte[] fontData = new byte[fontStream.Length];
			fontStream.Read(fontData, 0, (int)fontStream.Length);
			Marshal.Copy(fontData, 0, data, (int)fontStream.Length);
			CustomFonts.AddMemoryFont(data, fontData.Length);
			fontStream.Close();
			Marshal.FreeCoTaskMem(data);
		}

		private bool? LastVsyncSetting;
		private GraphicsControl LastVsyncSettingGraphicsControl;

		private readonly Dictionary<string, DisplaySurface> MapNameToLuaSurface = new Dictionary<string,DisplaySurface>();
		private readonly Dictionary<DisplaySurface, string> MapLuaSurfaceToName = new Dictionary<DisplaySurface, string>();
		private readonly Dictionary<string, SwappableDisplaySurfaceSet> LuaSurfaceSets = new Dictionary<string, SwappableDisplaySurfaceSet>();

		/// <summary>
		/// Peeks a locked lua surface, or returns null if it isn't locked
		/// </summary>
		public DisplaySurface PeekLockedLuaSurface(string name)
		{
			if (MapNameToLuaSurface.ContainsKey(name))
				return MapNameToLuaSurface[name];
			return null;
		}

		/// <summary>locks the lua surface called <paramref name="name"/></summary>
		/// <exception cref="InvalidOperationException">already locked, or unknown surface</exception>
		public DisplaySurface LockLuaSurface(string name, bool clear=true)
		{
			if (MapNameToLuaSurface.ContainsKey(name))
			{
				throw new InvalidOperationException($"Lua surface is already locked: {name}");
			}

			if (!LuaSurfaceSets.TryGetValue(name, out var sdss))
			{
				sdss = new SwappableDisplaySurfaceSet();
				LuaSurfaceSets.Add(name, sdss);
			}

			// placeholder logic for more abstracted surface definitions from filter chain
			int currNativeWidth = presentationPanel.NativeSize.Width;
			int currNativeHeight = presentationPanel.NativeSize.Height;

			currNativeWidth += ClientExtraPadding.Horizontal;
			currNativeHeight += ClientExtraPadding.Vertical;

			int width,height;
			if (name == "emu")
			{
				width = currEmuWidth;
				height = currEmuHeight;
				width += GameExtraPadding.Horizontal;
				height += GameExtraPadding.Vertical;
			}
			else if (name == "native")
			{
				width = currNativeWidth; height = currNativeHeight;
			}
			else throw new InvalidOperationException($"Unknown lua surface name: {name}");

			DisplaySurface ret = sdss.AllocateSurface(width, height, clear);
			MapNameToLuaSurface[name] = ret;
			MapLuaSurfaceToName[ret] = name;
			return ret;
		}

		public void ClearLuaSurfaces()
		{
			foreach (var kvp in LuaSurfaceSets)
			{
				try
				{
					var surf = PeekLockedLuaSurface(kvp.Key);
					DisplaySurface surfLocked = null;
					if (surf == null)
					{
						surfLocked = LockLuaSurface(kvp.Key, true);
					}

					if (surfLocked != null)
					{
						UnlockLuaSurface(surfLocked);
					}

					LuaSurfaceSets[kvp.Key].SetPending(null);
				}
				catch (InvalidOperationException)
				{
				}
			}
		}

		/// <summary>unlocks this DisplaySurface which had better have been locked as a lua surface</summary>
		/// <exception cref="InvalidOperationException">already unlocked</exception>
		public void UnlockLuaSurface(DisplaySurface surface)
		{
			if (!MapLuaSurfaceToName.ContainsKey(surface))
			{
				throw new InvalidOperationException("Surface was not locked as a lua surface");
			}

			string name = MapLuaSurfaceToName[surface];
			MapLuaSurfaceToName.Remove(surface);
			MapNameToLuaSurface.Remove(name);
			LuaSurfaceSets[name].SetPending(surface);
		}

		// helper classes:
		private class MyBlitter : IBlitter
		{
			private readonly DisplayManager _owner;

			public MyBlitter(DisplayManager dispManager)
			{
				_owner = dispManager;
			}

			private class FontWrapper : IBlitterFont
			{
				public FontWrapper(StringRenderer font)
				{
					Font = font;
				}

				public readonly StringRenderer Font;
			}

			IBlitterFont IBlitter.GetFontType(string fontType)
			{
				return new FontWrapper(_owner.TheOneFont);
			}

			void IBlitter.DrawString(string s, IBlitterFont font, Color color, float x, float y)
			{
				var stringRenderer = ((FontWrapper)font).Font;
				_owner.Renderer.SetModulateColor(color);
				stringRenderer.RenderString(_owner.Renderer, x, y, s);
				_owner.Renderer.SetModulateColorWhite();
			}

			SizeF IBlitter.MeasureString(string s, IBlitterFont font)
			{
				var stringRenderer = ((FontWrapper)font).Font;
				return stringRenderer.Measure(s);
			}

			public Rectangle ClipBounds { get; set; }
		}
	}
}