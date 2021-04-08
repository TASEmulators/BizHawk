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
using BizHawk.Bizware.DirectX;
using BizHawk.Bizware.OpenTK3;
using BizHawk.Client.Common;
using BizHawk.Client.Common.Filters;
using BizHawk.Client.Common.FilterManager;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A DisplayManager is destined forevermore to drive the PresentationPanel it gets initialized with.
	/// Its job is to receive OSD and emulator outputs, and produce one single buffer (BitmapBuffer? Texture2d?) for display by the PresentationPanel.
	/// Details TBD
	/// </summary>
	public class DisplayManager : IDisplayManagerForApi, IWindowCoordsTransformer, IDisposable
	{
		private static DisplaySurface CreateDisplaySurface(int w, int h) => new(w, h);

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

		private readonly Func<bool> _getIsSecondaryThrottlingDisabled;

		public OSDManager OSD { get; }

		private Config GlobalConfig;

		private IEmulator GlobalEmulator;

		public DisplayManager(Config config, IEmulator emulator, InputManager inputManager, IMovieSession movieSession, IGL gl, PresentationPanel presentationPanel, Func<bool> getIsSecondaryThrottlingDisabled)
		{
			GlobalConfig = config;
			GlobalEmulator = emulator;
			OSD = new OSDManager(config, emulator, inputManager, movieSession);
			_getIsSecondaryThrottlingDisabled = getIsSecondaryThrottlingDisabled;
			_gl = gl;

			// setup the GL context manager, needed for coping with multiple opengl cores vs opengl display method
			// but is it tho? --yoshi
			_glManager = GLManager.Instance;

			this._presentationPanel = presentationPanel;
			_graphicsControl = this._presentationPanel.GraphicsControl;
			_crGraphicsControl = _glManager.GetContextForGraphicsControl(_graphicsControl);

			// it's sort of important for these to be initialized to something nonzero
			_currEmuWidth = _currEmuHeight = 1;

			_renderer = _gl.CreateRenderer();

			_videoTextureFrugalizer = new TextureFrugalizer(_gl);

			_shaderChainFrugalizers = new RenderTargetFrugalizer[16]; // hacky hardcoded limit.. need some other way to manage these
			for (int i = 0; i < 16; i++)
			{
				_shaderChainFrugalizers[i] = new RenderTargetFrugalizer(_gl);
			}

			using (var xml = EmuHawk.ReflectionCache.EmbeddedResourceStream("Resources.courier16px.fnt"))
			{
				using var tex = EmuHawk.ReflectionCache.EmbeddedResourceStream("Resources.courier16px_0.png");
				_theOneFont = new StringRenderer(_gl, xml, tex);
			}

			using (var gens =
				EmuHawk.ReflectionCache.EmbeddedResourceStream("Resources.gens.ttf"))
			{
				LoadCustomFont(gens);
			}

			using (var fceux =
				EmuHawk.ReflectionCache.EmbeddedResourceStream("Resources.fceux.ttf"))
			{
				LoadCustomFont(fceux);
			}

			if (_gl is IGL_TK || _gl is IGL_SlimDX9)
			{
				var fiHq2x = new FileInfo(Path.Combine(PathUtils.ExeDirectoryPath, "Shaders/BizHawk/hq2x.cgp"));
				if (fiHq2x.Exists)
				{
					using var stream = fiHq2x.OpenRead();
					_shaderChainHq2X = new RetroShaderChain(_gl, new RetroShaderPreset(stream), Path.Combine(PathUtils.ExeDirectoryPath, "Shaders/BizHawk"));
				}
				var fiScanlines = new FileInfo(Path.Combine(PathUtils.ExeDirectoryPath, "Shaders/BizHawk/BizScanlines.cgp"));
				if (fiScanlines.Exists)
				{
					using var stream = fiScanlines.OpenRead();
					_shaderChainScanlines = new RetroShaderChain(_gl, new RetroShaderPreset(stream), Path.Combine(PathUtils.ExeDirectoryPath, "Shaders/BizHawk"));
				}

				string bicubicPath = "Shaders/BizHawk/bicubic-fast.cgp";
				if (_gl is IGL_SlimDX9)
				{
					bicubicPath = "Shaders/BizHawk/bicubic-normal.cgp";
				}
				var fiBicubic = new FileInfo(Path.Combine(PathUtils.ExeDirectoryPath, bicubicPath));
				if (fiBicubic.Exists)
				{
					using var stream = fiBicubic.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
					_shaderChainBicubic = new RetroShaderChain(_gl, new RetroShaderPreset(stream), Path.Combine(PathUtils.ExeDirectoryPath, "Shaders/BizHawk"));
				}
			}

			_apiHawkSurfaceSets[DisplaySurfaceID.EmuCore] = new(CreateDisplaySurface);
			_apiHawkSurfaceSets[DisplaySurfaceID.Client] = new(CreateDisplaySurface);
			_apiHawkSurfaceFrugalizers[DisplaySurfaceID.EmuCore] = new TextureFrugalizer(_gl);
			_apiHawkSurfaceFrugalizers[DisplaySurfaceID.Client] = new TextureFrugalizer(_gl);

			RefreshUserShader();
		}

		public void UpdateGlobals(Config config, IEmulator emulator)
		{
			GlobalConfig = config;
			GlobalEmulator = emulator;
			OSD.UpdateGlobals(config, emulator);
		}

		public bool Disposed { get; private set; }

		public void Dispose()
		{
			if (Disposed) return;
			Disposed = true;
			_videoTextureFrugalizer.Dispose();
			foreach (var f in _apiHawkSurfaceFrugalizers.Values)
			{
				f.Dispose();
			}

			foreach (var f in _shaderChainFrugalizers)
			{
				f?.Dispose();
			}

			foreach (var s in new[] { _shaderChainHq2X, _shaderChainScanlines, _shaderChainBicubic, _shaderChainUser })
			{
				s?.Dispose();
			}

			_theOneFont.Dispose();
			_renderer.Dispose();
		}

		// rendering resources:
		private readonly IGL _gl;
		private readonly GLManager _glManager;
		private readonly StringRenderer _theOneFont;
		private readonly IGuiRenderer _renderer;

		// layer resources
		private readonly PresentationPanel _presentationPanel; // well, its the final layer's target, at least
		private readonly GraphicsControl _graphicsControl; // well, its the final layer's target, at least
		private readonly GLManager.ContextRef _crGraphicsControl;
		private FilterProgram _currentFilterProgram;

		/// <summary>
		/// these variables will track the dimensions of the last frame's (or the next frame? this is confusing) emulator native output size
		/// THIS IS OLD JUNK. I should get rid of it, I think. complex results from the last filter ingestion should be saved instead.
		/// </summary>
		private int _currEmuWidth, _currEmuHeight;

		private Padding _clientExtraPadding;

		private Padding _gameExtraPadding;

		/// <summary>
		/// additional pixels added at the unscaled level for the use of lua drawing. essentially increases the input video provider dimensions
		/// </summary>
		public (int Left, int Top, int Right, int Bottom) GameExtraPadding
		{
			get => (_gameExtraPadding.Left, _gameExtraPadding.Top, _gameExtraPadding.Right, _gameExtraPadding.Bottom);
			set => _gameExtraPadding = new Padding(value.Left, value.Top, value.Right, value.Bottom);
		}

		/// <summary>
		/// additional pixels added at the native level for the use of lua drawing. essentially just gets tacked onto the final calculated window sizes.
		/// </summary>
		public (int Left, int Top, int Right, int Bottom) ClientExtraPadding
		{
			get => (_clientExtraPadding.Left, _clientExtraPadding.Top, _clientExtraPadding.Right, _clientExtraPadding.Bottom);
			set => _clientExtraPadding = new Padding(value.Left, value.Top, value.Right, value.Bottom);
		}

		/// <summary>
		/// custom fonts that don't need to be installed on the user side
		/// </summary>
		public PrivateFontCollection CustomFonts { get; } = new PrivateFontCollection();

		private readonly TextureFrugalizer _videoTextureFrugalizer;
		private readonly Dictionary<DisplaySurfaceID, TextureFrugalizer> _apiHawkSurfaceFrugalizers = new();
		private readonly RenderTargetFrugalizer[] _shaderChainFrugalizers;
		private readonly RetroShaderChain _shaderChainHq2X, _shaderChainScanlines, _shaderChainBicubic;
		private RetroShaderChain _shaderChainUser;

		public void RefreshUserShader()
		{
			_shaderChainUser?.Dispose();
			if (File.Exists(GlobalConfig.DispUserFilterPath))
			{
				var fi = new FileInfo(GlobalConfig.DispUserFilterPath);
				using var stream = fi.OpenRead();
				_shaderChainUser = new RetroShaderChain(_gl, new RetroShaderPreset(stream), Path.GetDirectoryName(GlobalConfig.DispUserFilterPath));
			}
		}

		private Padding CalculateCompleteContentPadding(bool user, bool source)
		{
			var padding = new Padding();

			if (user)
			{
				padding += _gameExtraPadding;
			}

			// an experimental feature
			if (source && GlobalEmulator is Octoshock psx)
			{
				var corePadding = psx.VideoProvider_Padding;
				padding.Left += corePadding.Width / 2;
				padding.Right += corePadding.Width - corePadding.Width / 2;
				padding.Top += corePadding.Height / 2;
				padding.Bottom += corePadding.Height - corePadding.Height / 2;
			}

			// apply user's crop selections as a negative padding (believe it or not, this largely works)
			// is there an issue with the aspect ratio? I don't know--but if there is, there would be with the padding too
			padding.Left -= GlobalConfig.DispCropLeft;
			padding.Right -= GlobalConfig.DispCropRight;
			padding.Top -= GlobalConfig.DispCropTop;
			padding.Bottom -= GlobalConfig.DispCropBottom;

			return padding;
		}

		private FilterProgram BuildDefaultChain(Size chainInSize, Size chainOutSize, bool includeOSD, bool includeUserFilters)
		{
			// select user special FX shader chain
			var selectedChainProperties = new Dictionary<string, object>();
			RetroShaderChain selectedChain = null;
			if (GlobalConfig.TargetDisplayFilter == 1 && _shaderChainHq2X != null && _shaderChainHq2X.Available)
			{
				selectedChain = _shaderChainHq2X;
			}

			if (GlobalConfig.TargetDisplayFilter == 2 && _shaderChainScanlines != null && _shaderChainScanlines.Available)
			{
				selectedChain = _shaderChainScanlines;
				selectedChainProperties["uIntensity"] = 1.0f - GlobalConfig.TargetScanlineFilterIntensity / 256.0f;
			}

			if (GlobalConfig.TargetDisplayFilter == 3 && _shaderChainUser != null && _shaderChainUser.Available)
			{
				selectedChain = _shaderChainUser;
			}

			if (!includeUserFilters)
				selectedChain = null;

			BaseFilter fCoreScreenControl = CreateCoreScreenControl();

			var fPresent = new FinalPresentation(chainOutSize);
			var fInput = new SourceImage(chainInSize);
			var fOSD = new OSD();
			fOSD.RenderCallback = () =>
			{
				if (!includeOSD)
				{
					return;
				}

				var size = fOSD.FindInput().SurfaceFormat.Size;
				_renderer.Begin(size.Width, size.Height);
				var myBlitter = new MyBlitter(this)
				{
					ClipBounds = new Rectangle(0, 0, size.Width, size.Height)
				};
				_renderer.SetBlendState(_gl.BlendNormal);
				OSD.Begin(myBlitter);
				OSD.DrawScreenInfo(myBlitter);
				OSD.DrawMessages(myBlitter);
				_renderer.End();
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
				FinalPresentation fPadding = new FinalPresentation(size);
				chain.AddFilter(fPadding, "padding");
				fPadding.GuiRenderer = _renderer;
				fPadding.GL = _gl;
				fPadding.Config_PadOnly = true;
				fPadding.Padding = (padding.Left, padding.Top, padding.Right, padding.Bottom);
			}

			//add lua layer 'emu'
			AppendApiHawkLayer(chain, DisplaySurfaceID.EmuCore);

			if(includeUserFilters)
				if (GlobalConfig.DispPrescale != 1)
				{
					var fPrescale = new PrescaleFilter() { Scale = GlobalConfig.DispPrescale };
					chain.AddFilter(fPrescale, "user_prescale");
				}

			// add user-selected retro shader
			if (selectedChain != null)
				AppendRetroShaderChain(chain, "retroShader", selectedChain, selectedChainProperties);

			// AutoPrescale makes no sense for a None final filter
			if (GlobalConfig.DispAutoPrescale && GlobalConfig.DispFinalFilter != (int)FinalPresentation.eFilterOption.None)
			{
				var apf = new AutoPrescaleFilter();
				chain.AddFilter(apf, "auto_prescale");
			}

			//choose final filter
			var finalFilter = FinalPresentation.eFilterOption.None;
			if (GlobalConfig.DispFinalFilter == 1)
			{
				finalFilter = FinalPresentation.eFilterOption.Bilinear;
			}

			if (GlobalConfig.DispFinalFilter == 2)
			{
				finalFilter = FinalPresentation.eFilterOption.Bicubic;
			}

			//if bicubic is selected and unavailable, don't use it. use bilinear instead I guess
			if (finalFilter == FinalPresentation.eFilterOption.Bicubic)
			{
				if (_shaderChainBicubic == null || !_shaderChainBicubic.Available)
				{
					finalFilter = FinalPresentation.eFilterOption.Bilinear;
				}
			}

			fPresent.FilterOption = finalFilter;

			// now if bicubic is chosen, insert it
			if (finalFilter == FinalPresentation.eFilterOption.Bicubic)
			{
				AppendRetroShaderChain(chain, "bicubic", _shaderChainBicubic, null);
			}

			// add final presentation
			if (includeUserFilters)
				chain.AddFilter(fPresent, "presentation");

			//add lua layer 'native'
			AppendApiHawkLayer(chain, DisplaySurfaceID.Client);

			// and OSD goes on top of that
			// TODO - things break if this isn't present (the final presentation filter gets messed up when used with prescaling)
			// so, always include it (we'll handle this flag in the callback to do no rendering)
			if (true /*includeOSD*/) chain.AddFilter(fOSD, "osd");

			return chain;
		}

		private void AppendRetroShaderChain(FilterProgram program, string name, RetroShaderChain retroChain, Dictionary<string, object> properties)
		{
			for (int i = 0; i < retroChain.Passes.Length; i++)
			{
				var pass = retroChain.Passes[i];
				var rsp = new RetroShaderPass(retroChain, i);
				string fname = $"{name}[{i}]";
				program.AddFilter(rsp, fname);
				rsp.Parameters = properties;
			}
		}

		private void AppendApiHawkLayer(FilterProgram chain, DisplaySurfaceID surfaceID)
		{
			var luaNativeSurface = _apiHawkSurfaceSets[surfaceID].GetCurrent();
			if (luaNativeSurface == null)
			{
				return;
			}

			Texture2d luaNativeTexture = _apiHawkSurfaceFrugalizers[surfaceID].Get(luaNativeSurface);
			var fLuaLayer = new LuaLayer();
			fLuaLayer.SetTexture(luaNativeTexture);
			chain.AddFilter(fLuaLayer, surfaceID.GetName());
		}

		/// <summary>
		/// Using the current filter program, turn a mouse coordinate from window space to the original emulator screen space.
		/// </summary>
		public Point UntransformPoint(Point p)
		{
			// first, turn it into a window coordinate
			p = _presentationPanel.Control.PointToClient(p);

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

		public Size GetPanelNativeSize() => _presentationPanel.NativeSize;

		/// <summary>
		/// This will receive an emulated output frame from an IVideoProvider and run it through the complete frame processing pipeline
		/// Then it will stuff it into the bound PresentationPanel.
		/// ---
		/// If the int[] is size=1, then it contains an openGL texture ID (and the size should be as specified from videoProvider)
		/// Don't worry about the case where the frontend isnt using opengl; DisplayManager deals with it
		/// </summary>
		public void UpdateSource(IVideoProvider videoProvider)
		{
			bool displayNothing = GlobalConfig.DispSpeedupFeatures == 0;
			var job = new JobInfo
			{
				VideoProvider = videoProvider,
				Simulate = displayNothing,
				ChainOutsize = _graphicsControl.Size,
				IncludeOSD = true,
				IncludeUserFilters = true
			};
			UpdateSourceInternal(job);
		}

		private BaseFilter CreateCoreScreenControl()
		{
			if (GlobalEmulator is MelonDS nds)
			{
				//TODO: need to pipe layout settings into here now
				var filter = new ScreenControlNDS(nds);
				return filter;
			}

			return null;
		}

		public BitmapBuffer RenderVideoProvider(IVideoProvider videoProvider)
		{
			// TODO - we might need to gather more Config.DispXXX properties here, so they can be overridden
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
				ChainOutsize = _graphicsControl.Size,
				Offscreen = true,
				IncludeOSD = includeOSD,
				IncludeUserFilters = true,
			};
			UpdateSourceInternal(job);
			return job.OffscreenBb;
		}
		/// <summary>
		/// Does the display process to an offscreen buffer, suitable for a Lua-inclusive movie.
		/// </summary>
		public BitmapBuffer RenderOffscreenLua(IVideoProvider videoProvider)
		{
			var job = new JobInfo
			{
				VideoProvider = videoProvider,
				Simulate = false,
				ChainOutsize = new Size(videoProvider.BufferWidth, videoProvider.BufferHeight),
				Offscreen = true,
				IncludeOSD = false,
				IncludeUserFilters = false,
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

		private void FixRatio(float x, float y, int inw, int inh, out int outW, out int outH)
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
			bool arActive = GlobalConfig.DispFixAspectRatio;
			bool arSystem = GlobalConfig.DispManagerAR == EDispManagerAR.System;
			bool arCustom = GlobalConfig.DispManagerAR == EDispManagerAR.Custom;
			bool arCustomRatio = GlobalConfig.DispManagerAR == EDispManagerAR.CustomRatio;
			bool arCorrect = arSystem || arCustom || arCustomRatio;
			bool arInteger = GlobalConfig.DispFixScaleInteger;

			int bufferWidth = videoProvider.BufferWidth;
			int bufferHeight = videoProvider.BufferHeight;
			int virtualWidth = videoProvider.VirtualWidth;
			int virtualHeight = videoProvider.VirtualHeight;

			if (arCustom)
			{
				virtualWidth = GlobalConfig.DispCustomUserARWidth;
				virtualHeight = GlobalConfig.DispCustomUserARHeight;
			}

			if (arCustomRatio)
			{
				FixRatio(GlobalConfig.DispCustomUserArx, GlobalConfig.DispCustomUserAry, videoProvider.BufferWidth, videoProvider.BufferHeight, out virtualWidth, out virtualHeight);
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
						Vector2 AR = new(virtualWidth / (float) bufferWidth, virtualHeight / (float) bufferHeight);
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

			chainOutsize.Width += _clientExtraPadding.Horizontal;
			chainOutsize.Height += _clientExtraPadding.Vertical;

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
				_glManager.Activate(_crGraphicsControl);

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

			if (GlobalConfig.DispFixAspectRatio)
			{
				if (GlobalConfig.DispManagerAR == EDispManagerAR.None)
				{
					vw = bufferWidth;
					vh = bufferHeight;
				}
				if (GlobalConfig.DispManagerAR == EDispManagerAR.System)
				{
					//Already set
				}
				if (GlobalConfig.DispManagerAR == EDispManagerAR.Custom)
				{
					//not clear what any of these other options mean for "screen controlled" systems
					vw = GlobalConfig.DispCustomUserARWidth;
					vh = GlobalConfig.DispCustomUserARHeight;
				}
				if (GlobalConfig.DispManagerAR == EDispManagerAR.CustomRatio)
				{
					//not clear what any of these other options mean for "screen controlled" systems
					FixRatio(GlobalConfig.DispCustomUserArx, GlobalConfig.DispCustomUserAry, videoProvider.BufferWidth, videoProvider.BufferHeight, out vw, out vh);
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
					videoTexture = _gl.WrapGLTexture2d(new IntPtr(videoBuffer[0]), bufferWidth, bufferHeight);
				}
				else
				{
					//wrap the VideoProvider data in a BitmapBuffer (no point to refactoring that many IVideoProviders)
					bb = new BitmapBuffer(bufferWidth, bufferHeight, videoBuffer);
					bb.DiscardAlpha();

					//now, acquire the data sent from the videoProvider into a texture
					videoTexture = _videoTextureFrugalizer.Get(bb);

					// lets not use this. lets define BizwareGL to make clamp by default (TBD: check opengl)
					//GL.SetTextureWrapMode(videoTexture, true);
				}
			}

			// record the size of what we received, since lua and stuff is gonna want to draw onto it
			_currEmuWidth = bufferWidth;
			_currEmuHeight = bufferHeight;

			//build the default filter chain and set it up with services filters will need
			Size chainInsize = new Size(bufferWidth, bufferHeight);

			var filterProgram = BuildDefaultChain(chainInsize, chainOutsize, job.IncludeOSD, job.IncludeUserFilters);
			filterProgram.GuiRenderer = _renderer;
			filterProgram.GL = _gl;

			//setup the source image filter
			SourceImage fInput = filterProgram["input"] as SourceImage;
			fInput.Texture = videoTexture;

			//setup the final presentation filter
			FinalPresentation fPresent = filterProgram["presentation"] as FinalPresentation;
			if (fPresent != null)
			{
				fPresent.VirtualTextureSize = new Size(vw, vh);
				fPresent.TextureSize = new Size(presenterTextureWidth, presenterTextureHeight);
				fPresent.BackgroundColor = videoProvider.BackgroundColor;
				fPresent.GuiRenderer = _renderer;
				fPresent.Flip = isGlTextureId;
				fPresent.Config_FixAspectRatio = GlobalConfig.DispFixAspectRatio;
				fPresent.Config_FixScaleInteger = GlobalConfig.DispFixScaleInteger;
				fPresent.Padding = (ClientExtraPadding.Left, ClientExtraPadding.Top, ClientExtraPadding.Right, ClientExtraPadding.Bottom);
				fPresent.AutoPrescale = GlobalConfig.DispAutoPrescale;

				fPresent.GL = _gl;
			}

			//POOPY. why are we delivering the GL context this way? such bad
			ScreenControlNDS fNDS = filterProgram["CoreScreenControl"] as ScreenControlNDS;
			if (fNDS != null)
			{
				fNDS.GuiRenderer = _renderer;
				fNDS.GL = _gl;
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
			_glManager.Activate(_crGraphicsControl);
			_gl.BeginScene();
			_gl.BindRenderTarget(null);
			_gl.SetClearColor(Color.Black);
			_gl.Clear(ClearBufferMask.ColorBufferBit);
			_gl.EndScene();
			_presentationPanel.GraphicsControl.SwapBuffers();
		}

		private void UpdateSourceDrawingWork(JobInfo job)
		{
			bool alternateVsync = false;

			// only used by alternate vsync
			IGL_SlimDX9 dx9 = null;

			if (!job.Offscreen)
			{
				//apply the vsync setting (should probably try to avoid repeating this)
				var vsync = GlobalConfig.VSyncThrottle || GlobalConfig.VSync;

				//ok, now this is a bit undesirable.
				//maybe the user wants vsync, but not vsync throttle.
				//this makes sense... but we don't have the infrastructure to support it now (we'd have to enable triple buffering or something like that)
				//so what we're gonna do is disable vsync no matter what if throttling is off, and maybe nobody will notice.
				//update 26-mar-2016: this upsets me. When fast-forwarding and skipping frames, vsync should still work. But I'm not changing it yet
				if (_getIsSecondaryThrottlingDisabled())
					vsync = false;

				//for now, it's assumed that the presentation panel is the main window, but that may not always be true
				if (vsync && GlobalConfig.DispAlternateVsync && GlobalConfig.VSyncThrottle)
				{
					dx9 = _gl as IGL_SlimDX9;
					if (dx9 != null)
					{
						alternateVsync = true;
						//unset normal vsync if we've chosen the alternate vsync
						vsync = false;
					}
				}

				//TODO - whats so hard about triple buffering anyway? just enable it always, and change api to SetVsync(enable,throttle)
				//maybe even SetVsync(enable,throttlemethod) or just SetVsync(enable,throttle,advanced)

				if (_lastVsyncSetting != vsync || _lastVsyncSettingGraphicsControl != _presentationPanel.GraphicsControl)
				{
					if (_lastVsyncSetting == null && vsync)
					{
						// Workaround for vsync not taking effect at startup (Intel graphics related?)
						_presentationPanel.GraphicsControl.SetVsync(false);
					}
					_presentationPanel.GraphicsControl.SetVsync(vsync);
					_lastVsyncSettingGraphicsControl = _presentationPanel.GraphicsControl;
					_lastVsyncSetting = vsync;
				}
			}

			// begin rendering on this context
			// should this have been done earlier?
			// do i need to check this on an intel video card to see if running excessively is a problem? (it used to be in the FinalTarget command below, shouldn't be a problem)
			//GraphicsControl.Begin(); // CRITICAL POINT for yabause+GL

			//TODO - auto-create and age these (and dispose when old)
			int rtCounter = 0;

			_currentFilterProgram.RenderTargetProvider = new DisplayManagerRenderTargetProvider(size => _shaderChainFrugalizers[rtCounter++].Get(size));

			_gl.BeginScene();

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
							rtCurr = _shaderChainFrugalizers[rtCounter++].Get(size);
							rtCurr.Bind();
							_currentFilterProgram.CurrRenderTarget = rtCurr;
							break;
						}
					case FilterProgram.ProgramStepType.FinalTarget:
						{
							inFinalTarget = true;
							rtCurr = null;
							_currentFilterProgram.CurrRenderTarget = null;
							_gl.BindRenderTarget(null);
							break;
						}
				}
			}

			_gl.EndScene();

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
				_presentationPanel.GraphicsControl.SwapBuffers();

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

		private bool? _lastVsyncSetting;
		private GraphicsControl _lastVsyncSettingGraphicsControl;

		private readonly Dictionary<DisplaySurfaceID, IDisplaySurface> _apiHawkIDToSurface = new();

		/// <remarks>Can't this just be a prop of <see cref="IDisplaySurface"/>? --yoshi</remarks>
		private readonly Dictionary<IDisplaySurface, DisplaySurfaceID> _apiHawkSurfaceToID = new();

		private readonly Dictionary<DisplaySurfaceID, SwappableDisplaySurfaceSet<DisplaySurface>> _apiHawkSurfaceSets = new();

		/// <summary>
		/// Peeks a locked lua surface, or returns null if it isn't locked
		/// </summary>
		public IDisplaySurface PeekApiHawkLockedSurface(DisplaySurfaceID surfaceID)
		{
			if (_apiHawkIDToSurface.ContainsKey(surfaceID))
				return _apiHawkIDToSurface[surfaceID];
			return null;
		}

		public IDisplaySurface LockApiHawkSurface(DisplaySurfaceID surfaceID, bool clear)
		{
			if (_apiHawkIDToSurface.ContainsKey(surfaceID))
			{
				throw new InvalidOperationException($"ApiHawk/Lua surface is already locked: {surfaceID.GetName()}");
			}

			if (!_apiHawkSurfaceSets.TryGetValue(surfaceID, out var sdss))
			{
				sdss = new(CreateDisplaySurface);
				_apiHawkSurfaceSets.Add(surfaceID, sdss);
			}

			// placeholder logic for more abstracted surface definitions from filter chain
			int currNativeWidth = _presentationPanel.NativeSize.Width;
			int currNativeHeight = _presentationPanel.NativeSize.Height;

			currNativeWidth += _clientExtraPadding.Horizontal;
			currNativeHeight += _clientExtraPadding.Vertical;

			var (width, height) = surfaceID switch
			{
				DisplaySurfaceID.EmuCore => (_currEmuWidth + _gameExtraPadding.Horizontal, _currEmuHeight + _gameExtraPadding.Vertical),
				DisplaySurfaceID.Client => (currNativeWidth, currNativeHeight),
				_ => throw new ArgumentException(message: "not a valid enum member", paramName: nameof(surfaceID))
			};

			IDisplaySurface ret = sdss.AllocateSurface(width, height, clear);
			_apiHawkIDToSurface[surfaceID] = ret;
			_apiHawkSurfaceToID[ret] = surfaceID;
			return ret;
		}

		public void ClearApiHawkSurfaces()
		{
			foreach (var kvp in _apiHawkSurfaceSets)
			{
				try
				{
					if (PeekApiHawkLockedSurface(kvp.Key) == null)
					{
						var surfLocked = LockApiHawkSurface(kvp.Key, true);
						if (surfLocked != null) UnlockApiHawkSurface(surfLocked);
					}
					_apiHawkSurfaceSets[kvp.Key].SetPending(null);
				}
				catch (InvalidOperationException)
				{
					// ignored
				}
			}
		}

		/// <summary>unlocks this IDisplaySurface which had better have been locked as a lua surface</summary>
		/// <exception cref="InvalidOperationException">already unlocked</exception>
		public void UnlockApiHawkSurface(IDisplaySurface surface)
		{
			if (surface is not DisplaySurface dispSurfaceImpl) throw new ArgumentException("don't mix " + nameof(IDisplaySurface) + " implementations!", nameof(surface));
			if (!_apiHawkSurfaceToID.ContainsKey(dispSurfaceImpl))
			{
				throw new InvalidOperationException("Surface was not locked as a lua surface");
			}

			var surfaceID = _apiHawkSurfaceToID[dispSurfaceImpl];
			_apiHawkSurfaceToID.Remove(dispSurfaceImpl);
			_apiHawkIDToSurface.Remove(surfaceID);
			_apiHawkSurfaceSets[surfaceID].SetPending(dispSurfaceImpl);
		}

		// helper classes:
		private class MyBlitter : IBlitter
		{
			private readonly DisplayManager _owner;

			public MyBlitter(DisplayManager dispManager)
			{
				_owner = dispManager;
			}

			public StringRenderer GetFontType(string fontType) => _owner._theOneFont;

			public void DrawString(string s, StringRenderer font, Color color, float x, float y)
			{
				_owner._renderer.SetModulateColor(color);
				font.RenderString(_owner._renderer, x, y, s);
				_owner._renderer.SetModulateColorWhite();
			}

			public SizeF MeasureString(string s, StringRenderer font) => font.Measure(s);

			public Rectangle ClipBounds { get; set; }
		}
	}
}