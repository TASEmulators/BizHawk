//TODO
//we could flag textures as 'actually' render targets (keep a reference to the render target?) which could allow us to convert between them more quickly in some cases

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;
using BizHawk.Client.EmuHawk.FilterManager;
using BizHawk.Bizware.BizwareGL;

using OpenTK;
using BizHawk.Bizware.BizwareGL.Drivers.SlimDX;
using BizHawk.Bizware.BizwareGL.Drivers.GdiPlus;

namespace BizHawk.Client.MultiHawk
{
	/// <summary>
	/// A DisplayManager is destined forevermore to drive the PresentationPanel it gets initialized with.
	/// Its job is to receive OSD and emulator outputs, and produce one single buffer (BitampBuffer? Texture2d?) for display by the PresentationPanel.
	/// Details TBD
	/// </summary>
	public class DisplayManager : IDisposable
	{
		class DisplayManagerRenderTargetProvider : IRenderTargetProvider
		{
			public DisplayManagerRenderTargetProvider(Func<Size, RenderTarget> callback) { Callback = callback; }
			Func<Size, RenderTarget> Callback;
			RenderTarget IRenderTargetProvider.Get(Size size)
			{
				return Callback(size);
			}
		}

		public DisplayManager(PresentationPanel presentationPanel, IGL gl, GLManager glManager)
		{
			GL = gl;
			GLManager = glManager;
			this.presentationPanel = presentationPanel;
			GraphicsControl = this.presentationPanel.GraphicsControl;
			CR_GraphicsControl = GLManager.GetContextForGraphicsControl(GraphicsControl);

			//it's sort of important for these to be initialized to something nonzero
			currEmuWidth = currEmuHeight = 1;

			Renderer = GL.CreateRenderer();

			VideoTextureFrugalizer = new TextureFrugalizer(GL);

			ShaderChainFrugalizers = new RenderTargetFrugalizer[16]; //hacky hardcoded limit.. need some other way to manage these
			for (int i = 0; i < 16; i++)
			{
				ShaderChainFrugalizers[i] = new RenderTargetFrugalizer(GL);
			}

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
			Renderer.Dispose();
		}

		//rendering resources:
		IGL GL;
		GLManager GLManager;
		IGuiRenderer Renderer;

		//layer resources
		PresentationPanel presentationPanel; //well, its the final layer's target, at least
		GraphicsControl GraphicsControl; //well, its the final layer's target, at least
		GLManager.ContextRef CR_GraphicsControl;
		FilterProgram CurrentFilterProgram;

		/// <summary>
		/// these variables will track the dimensions of the last frame's (or the next frame? this is confusing) emulator native output size
		/// THIS IS OLD JUNK. I should get rid of it, I think. complex results from the last filter ingestion should be saved instead.
		/// </summary>
		int currEmuWidth, currEmuHeight;

		/// <summary>
		/// additional pixels added at the unscaled level for the use of lua drawing. essentially increases the input video provider dimensions
		/// </summary>
		public System.Windows.Forms.Padding GameExtraPadding;

		/// <summary>
		/// additional pixels added at the native level for the use of lua drawing. essentially just gets tacked onto the final calculated window sizes.
		/// </summary>
		public System.Windows.Forms.Padding ClientExtraPadding;

		/// <summary>
		/// custom fonts that don't need to be installed on the user side
		/// </summary>
		public System.Drawing.Text.PrivateFontCollection CustomFonts = new System.Drawing.Text.PrivateFontCollection();

		TextureFrugalizer VideoTextureFrugalizer;
		Dictionary<string, TextureFrugalizer> LuaSurfaceFrugalizers = new Dictionary<string, TextureFrugalizer>();
		RenderTargetFrugalizer[] ShaderChainFrugalizers;
		EmuHawk.Filters.RetroShaderChain ShaderChain_user;

		public void RefreshUserShader()
		{
			if (ShaderChain_user != null)
				ShaderChain_user.Dispose();
			if (File.Exists(Global.Config.DispUserFilterPath))
			{
				var fi = new FileInfo(Global.Config.DispUserFilterPath);
				using (var stream = fi.OpenRead())
					ShaderChain_user = new EmuHawk.Filters.RetroShaderChain(GL, new EmuHawk.Filters.RetroShaderPreset(stream), Path.GetDirectoryName(Global.Config.DispUserFilterPath));
			}
		}

		System.Windows.Forms.Padding CalculateCompleteContentPadding(bool user, bool source)
		{
			var padding = new System.Windows.Forms.Padding();

			if(user)
				padding += GameExtraPadding;

			//an experimental feature
			if(source)
				if (Global.Emulator is BizHawk.Emulation.Cores.Sony.PSX.Octoshock)
				{
					var psx = Global.Emulator as BizHawk.Emulation.Cores.Sony.PSX.Octoshock;
					var core_padding = psx.VideoProvider_Padding;
					padding.Left += core_padding.Width / 2;
					padding.Right += core_padding.Width - core_padding.Width / 2;
					padding.Top += core_padding.Height / 2;
					padding.Bottom += core_padding.Height - core_padding.Height / 2;
				}

			return padding;
		}

		FilterProgram BuildDefaultChain(Size chain_insize, Size chain_outsize, bool includeOSD)
		{
			//select user special FX shader chain
			Dictionary<string, object> selectedChainProperties = new Dictionary<string, object>();
			EmuHawk.Filters.RetroShaderChain selectedChain = null;
			if (Global.Config.TargetDisplayFilter == 3 && ShaderChain_user != null && ShaderChain_user.Available)
				selectedChain = ShaderChain_user;

			EmuHawk.Filters.FinalPresentation fPresent = new EmuHawk.Filters.FinalPresentation(chain_outsize);
			EmuHawk.Filters.SourceImage fInput = new EmuHawk.Filters.SourceImage(chain_insize);
			EmuHawk.Filters.OSD fOSD = new EmuHawk.Filters.OSD();
			fOSD.RenderCallback = () =>
			{
				if (!includeOSD)
					return;
				var size = fOSD.FindInput().SurfaceFormat.Size;
				Renderer.Begin(size.Width, size.Height);
				Renderer.SetBlendState(GL.BlendNormal);
				Renderer.End();
			};

			var chain = new FilterProgram();

			//add the first filter, encompassing output from the emulator core
			chain.AddFilter(fInput, "input");

			//if a non-zero padding is required, add a filter to allow for that
			//note, we have two sources of padding right now.. one can come from the videoprovider and one from the user.
			//we're combining these now and just using black, for sake of being lean, despite the discussion below:
			//keep in mind, the videoprovider design in principle might call for another color.
			//we havent really been using this very hard, but users will probably want black there (they could fill it to another color if needed tho)
			var padding = CalculateCompleteContentPadding(true,true);
			if (padding.Vertical != 0 || padding.Horizontal != 0)
			{
				//TODO - add another filter just for this, its cumbersome to use final presentation... I think. but maybe theres enough similarities to justify it.
				Size size = chain_insize;
				size.Width += padding.Horizontal;
				size.Height += padding.Vertical;
				EmuHawk.Filters.FinalPresentation fPadding = new EmuHawk.Filters.FinalPresentation(size);
				chain.AddFilter(fPadding, "padding");
				fPadding.GuiRenderer = Renderer;
				fPadding.GL = GL;
				fPadding.Config_PadOnly = true;
				fPadding.Padding = padding;
			}

			if (Global.Config.DispPrescale != 1)
			{
				EmuHawk.Filters.PrescaleFilter fPrescale = new EmuHawk.Filters.PrescaleFilter() { Scale = Global.Config.DispPrescale };
				chain.AddFilter(fPrescale, "user_prescale");
			}

			//AutoPrescale makes no sense for a None final filter
			if (Global.Config.DispAutoPrescale && Global.Config.DispFinalFilter != (int)EmuHawk.Filters.FinalPresentation.eFilterOption.None)
			{
				var apf = new EmuHawk.Filters.AutoPrescaleFilter();
				chain.AddFilter(apf, "auto_prescale");
			}

			//choose final filter
			EmuHawk.Filters.FinalPresentation.eFilterOption finalFilter = EmuHawk.Filters.FinalPresentation.eFilterOption.None;
			fPresent.FilterOption = finalFilter;

			//add final presentation
			chain.AddFilter(fPresent, "presentation");

			return chain;
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
				videoProvider = videoProvider,
				simulate = displayNothing,
				chain_outsize = GraphicsControl.Size,
				includeOSD = true,
			};
			UpdateSourceInternal(job);
		}

		public BitmapBuffer RenderVideoProvider(IVideoProvider videoProvider)
		{
			//TODO - we might need to gather more Global.Config.DispXXX properties here, so they can be overridden
			var targetSize = new Size(videoProvider.BufferWidth, videoProvider.BufferHeight);
			var padding = CalculateCompleteContentPadding(true,true);
			targetSize.Width += padding.Horizontal;
			targetSize.Height += padding.Vertical;

			var job = new JobInfo
			{
				videoProvider = videoProvider,
				simulate = false,
				chain_outsize = targetSize,
				offscreen = true,
				includeOSD = false
			};
			UpdateSourceInternal(job);
			return job.offscreenBB;
		}

		/// <summary>
		/// Does the entire display process to an offscreen buffer, suitable for a 'client' screenshot.
		/// </summary>
		public BitmapBuffer RenderOffscreen(IVideoProvider videoProvider, bool includeOSD)
		{
			var job = new JobInfo
			{
				videoProvider = videoProvider,
				simulate = false,
				chain_outsize = GraphicsControl.Size,
				offscreen = true,
				includeOSD = includeOSD
			};
			UpdateSourceInternal(job);
			return job.offscreenBB;
		}

		class FakeVideoProvider : IVideoProvider
		{
			public int[] GetVideoBuffer() { return new int[] {}; }

			public int VirtualWidth { get; set; }
			public int VirtualHeight { get; set; }

			public int BufferWidth { get; set; }
			public int BufferHeight { get; set; }
			public int BackgroundColor { get; set; }

			public int VsyncNumerator
			{
				get { throw new InvalidOperationException(); }
			}

			public int VsyncDenominator
			{
				get { throw new InvalidOperationException(); }
			}
		}

		void FixRatio(float x, float y, int inw, int inh, out int outw, out int outh)
		{
			float ratio = x / y;
			if (ratio <= 1)
			{
				//taller. weird. expand height.
				outw = inw;
				outh = (int)((float)inw / ratio);
			}
			else
			{
				//wider. normal. expand width.
				outw = (int)((float)inh * ratio);
				outh = inh;
			}
		}

		/// <summary>
		/// Attempts to calculate a good client size with the given zoom factor, considering the user's DisplayManager preferences
		/// TODO - this needs to be redone with a concept different from zoom factor.
		/// basically, each increment of a 'zoomlike' factor should definitely increase the viewable area somehow, even if it isnt strictly by an entire zoom level.
		/// </summary>
		public Size CalculateClientSize(IVideoProvider videoProvider, int zoom)
		{
			bool ar_active = Global.Config.DispFixAspectRatio;
			bool ar_system = Global.Config.DispManagerAR == Config.EDispManagerAR.System;
			bool ar_custom = Global.Config.DispManagerAR == Config.EDispManagerAR.Custom;
			bool ar_customRatio = Global.Config.DispManagerAR == Config.EDispManagerAR.CustomRatio;
			bool ar_correct = ar_system || ar_custom || ar_customRatio;
			bool ar_unity = !ar_correct;
			bool ar_integer = Global.Config.DispFixScaleInteger;

			int bufferWidth = videoProvider.BufferWidth;
			int bufferHeight = videoProvider.BufferHeight;
			int virtualWidth = videoProvider.VirtualWidth;
			int virtualHeight = videoProvider.VirtualHeight;

			if (ar_custom)
			{
				virtualWidth = Global.Config.DispCustomUserARWidth;
				virtualHeight = Global.Config.DispCustomUserARHeight;
			}

			if (ar_customRatio)
			{
				FixRatio(Global.Config.DispCustomUserARX, Global.Config.DispCustomUserARY, videoProvider.BufferWidth, videoProvider.BufferHeight, out virtualWidth, out virtualHeight);
			}

			var padding = CalculateCompleteContentPadding(true, false);
			virtualWidth += padding.Horizontal;
			virtualHeight += padding.Vertical;

			padding = CalculateCompleteContentPadding(true, true);
			bufferWidth += padding.Horizontal;
			bufferHeight += padding.Vertical;

			//Console.WriteLine("DISPZOOM " + zoom); //test

			//old stuff
			var fvp = new FakeVideoProvider();
			fvp.BufferWidth = bufferWidth;
			fvp.BufferHeight = bufferHeight;
			fvp.VirtualWidth = virtualWidth;
			fvp.VirtualHeight = virtualHeight;

			Size chain_outsize = new Size(fvp.BufferWidth * zoom, fvp.BufferHeight * zoom);

			if (ar_active)
			{
				if (ar_correct)
				{
					if (ar_integer)
					{
						//ALERT COPYPASTE LAUNDROMAT
						Vector2 VS = new Vector2(virtualWidth, virtualHeight);
						Vector2 BS = new Vector2(bufferWidth, bufferHeight);
						Vector2 AR = Vector2.Divide(VS, BS);
						float target_par = (AR.X / AR.Y);

						//this would malfunction for AR <= 0.5 or AR >= 2.0
						//EDIT - in fact, we have AR like that coming from PSX, sometimes, so maybe we should solve this better
						Vector2 PS = new Vector2(1, 1);

						//here's how we define zooming, in this case:
						//make sure each step is an increment of zoom for at least one of the dimensions (or maybe both of them)
						//look for the increment which helps the AR the best
						//TODO - this cant possibly support scale factors like 1.5x
						//TODO - also, this might be messing up zooms and stuff, we might need to run this on the output size of the filter chain
						for (int i = 1; i < zoom;i++)
						{
							//would not be good to run this per frame, but it seems to only run when the resolution changes, etc.
							Vector2[] trials = new[] {
								PS + new Vector2(1, 0),
								PS + new Vector2(0, 1),
								PS + new Vector2(1, 1)
							};
							int bestIndex = -1;
							float bestValue = 1000.0f;
							for (int t = 0; t < trials.Length; t++)
							{
								//I.
								float test_ar = trials[t].X / trials[t].Y;

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
							//is it possible to get here without selecting one? doubtful.
							//EDIT: YES IT IS. it happened with an 0,0 buffer size. of course, that was a mistake, but we shouldnt crash
							if(bestIndex != -1) //so, what now? well, this will result in 0,0 getting picked, so thats probably all we can do
								PS = trials[bestIndex];
						}

						chain_outsize = new Size((int)(bufferWidth * PS.X), (int)(bufferHeight * PS.Y));
					}
					else
					{
						//obey the AR, but allow free scaling: just zoom the virtual size
						chain_outsize = new Size(virtualWidth * zoom, virtualHeight * zoom);
					}
				}
				else
				{
					//ar_unity:
					//just choose to zoom the buffer (make no effort to incorporate AR)
					chain_outsize = new Size(bufferWidth * zoom, bufferHeight * zoom);
				}
			}
			else
			{
				//!ar_active:
				//just choose to zoom the buffer (make no effort to incorporate AR)
				chain_outsize = new Size(bufferWidth * zoom, bufferHeight * zoom);
			}

			chain_outsize.Width += ClientExtraPadding.Horizontal;
			chain_outsize.Height += ClientExtraPadding.Vertical;

			var job = new JobInfo
			{
				videoProvider = fvp,
				simulate = true,
				chain_outsize = chain_outsize,
			};
			var filterProgram = UpdateSourceInternal(job);

			var size = filterProgram.Filters[filterProgram.Filters.Count - 1].FindOutput().SurfaceFormat.Size;

			return size;
		}

		class JobInfo
		{
			public IVideoProvider videoProvider;
			public bool simulate;
			public Size chain_outsize;
			public bool offscreen;
			public BitmapBuffer offscreenBB;
			public bool includeOSD;
		}

		FilterProgram UpdateSourceInternal(JobInfo job)
		{
			if (job.chain_outsize.Width == 0 || job.chain_outsize.Height == 0)
			{
				//this has to be a NOP, because lots of stuff will malfunction on a 0-sized viewport
				return null;
			}

			//no drawing actually happens. it's important not to begin drawing on a control
			if (!job.simulate && !job.offscreen)
			{
				GLManager.Activate(CR_GraphicsControl);
			}

			IVideoProvider videoProvider = job.videoProvider;
			bool simulate = job.simulate;
			Size chain_outsize = job.chain_outsize;

			//simulate = true;

			int vw = videoProvider.BufferWidth;
			int vh = videoProvider.BufferHeight;

			if (Global.Config.DispFixAspectRatio)
			{
				if (Global.Config.DispManagerAR == Config.EDispManagerAR.System)
				{
					vw = videoProvider.VirtualWidth;
					vh = videoProvider.VirtualHeight;
				}
				if (Global.Config.DispManagerAR == Config.EDispManagerAR.Custom)
				{
					vw = Global.Config.DispCustomUserARWidth;
					vh = Global.Config.DispCustomUserARHeight;
				}
				if (Global.Config.DispManagerAR == Config.EDispManagerAR.CustomRatio)
				{
					FixRatio(Global.Config.DispCustomUserARX, Global.Config.DispCustomUserARY, videoProvider.BufferWidth, videoProvider.BufferHeight, out vw, out vh);
				}
			}

			var padding = CalculateCompleteContentPadding(true,false);
			vw += padding.Horizontal;
			vh += padding.Vertical;

			int[] videoBuffer = videoProvider.GetVideoBuffer();

			int bufferWidth = videoProvider.BufferWidth;
			int bufferHeight = videoProvider.BufferHeight;
			bool isGlTextureId = videoBuffer.Length == 1;

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
					//wrap the videoprovider data in a BitmapBuffer (no point to refactoring that many IVideoProviders)
					bb = new BitmapBuffer(bufferWidth, bufferHeight, videoBuffer);
					bb.DiscardAlpha();

					//now, acquire the data sent from the videoProvider into a texture
					videoTexture = VideoTextureFrugalizer.Get(bb);

					//lets not use this. lets define BizwareGL to make clamp by default (TBD: check opengl)
					//GL.SetTextureWrapMode(videoTexture, true);
				}
			}

			//record the size of what we received, since lua and stuff is gonna want to draw onto it
			currEmuWidth = bufferWidth;
			currEmuHeight = bufferHeight;

			//build the default filter chain and set it up with services filters will need
			Size chain_insize = new Size(bufferWidth, bufferHeight);

			var filterProgram = BuildDefaultChain(chain_insize, chain_outsize, job.includeOSD);
			filterProgram.GuiRenderer = Renderer;
			filterProgram.GL = GL;

			//setup the source image filter
			EmuHawk.Filters.SourceImage fInput = filterProgram["input"] as EmuHawk.Filters.SourceImage;
			fInput.Texture = videoTexture;

			//setup the final presentation filter
			EmuHawk.Filters.FinalPresentation fPresent = filterProgram["presentation"] as EmuHawk.Filters.FinalPresentation;
			fPresent.VirtualTextureSize = new Size(vw, vh);
			fPresent.TextureSize = new Size(bufferWidth, bufferHeight);
			fPresent.BackgroundColor = videoProvider.BackgroundColor;
			fPresent.GuiRenderer = Renderer;
			fPresent.Flip = isGlTextureId;
			fPresent.Config_FixAspectRatio = Global.Config.DispFixAspectRatio;
			fPresent.Config_FixScaleInteger = Global.Config.DispFixScaleInteger;
			fPresent.Padding = ClientExtraPadding;
			fPresent.AutoPrescale = Global.Config.DispAutoPrescale;

			fPresent.GL = GL;

			filterProgram.Compile("default", chain_insize, chain_outsize, !job.offscreen);

			if (simulate)
			{
			}
			else
			{
				CurrentFilterProgram = filterProgram;
				UpdateSourceDrawingWork(job);
			}

			//cleanup:
			if (bb != null) bb.Dispose();

			return filterProgram;
		}

		void UpdateSourceDrawingWork(JobInfo job)
		{
			bool vsync = false;
			bool alternateVsync = false;
			//only used by alternate vsync
			IGL_SlimDX9 dx9 = null;

			if (!job.offscreen)
			{
				//apply the vsync setting (should probably try to avoid repeating this)
				vsync = Global.Config.VSyncThrottle || Global.Config.VSync;

				//ok, now this is a bit undesireable.
				//maybe the user wants vsync, but not vsync throttle.
				//this makes sense... but we dont have the infrastructure to support it now (we'd have to enable triple buffering or something like that)
				//so what we're gonna do is disable vsync no matter what if throttling is off, and maybe nobody will notice.
				//update 26-mar-2016: this upsets me. When fastforwarding and skipping frames, vsync should still work. But I'm not changing it yet
				if (Global.DisableSecondaryThrottling)
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

			//begin rendering on this context
			//should this have been done earlier?
			//do i need to check this on an intel video card to see if running excessively is a problem? (it used to be in the FinalTarget command below, shouldnt be a problem)
			//GraphicsControl.Begin(); //CRITICAL POINT for yabause+GL

			//TODO - auto-create and age these (and dispose when old)
			int rtCounter = 0;

			CurrentFilterProgram.RenderTargetProvider = new DisplayManagerRenderTargetProvider((size) => ShaderChainFrugalizers[rtCounter++].Get(size));

			GL.BeginScene();

			//run filter chain
			Texture2d texCurr = null;
			RenderTarget rtCurr = null;
			bool inFinalTarget = false;
			foreach (var step in CurrentFilterProgram.Program)
			{
				switch (step.Type)
				{
					case FilterProgram.ProgramStepType.Run:
						{
							int fi = (int)step.Args;
							var f = CurrentFilterProgram.Filters[fi];
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
							CurrentFilterProgram.CurrRenderTarget = rtCurr;
							break;
						}
					case FilterProgram.ProgramStepType.FinalTarget:
						{
							var size = (Size)step.Args;
							inFinalTarget = true;
							rtCurr = null;
							CurrentFilterProgram.CurrRenderTarget = null;
							GL.BindRenderTarget(null);
							break;
						}
				}
			}

			GL.EndScene();

			if (job.offscreen)
			{
				job.offscreenBB = rtCurr.Texture2d.Resolve();
				job.offscreenBB.DiscardAlpha();
			}
			else
			{
				Debug.Assert(inFinalTarget);

				//wait for vsync to begin
				if (alternateVsync) dx9.AlternateVsyncPass(0);

				//present and conclude drawing
				presentationPanel.GraphicsControl.SwapBuffers();

				//wait for vsync to end
				if (alternateVsync) dx9.AlternateVsyncPass(1);

				//nope. dont do this. workaround for slow context switching on intel GPUs. just switch to another context when necessary before doing anything
				//presentationPanel.GraphicsControl.End();
			}
		}

		bool? LastVsyncSetting;
		GraphicsControl LastVsyncSettingGraphicsControl;
	}
}