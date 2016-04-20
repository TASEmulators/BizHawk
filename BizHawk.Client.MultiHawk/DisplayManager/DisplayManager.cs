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
			DisplayManagerRenderTargetProvider(Func<Size, RenderTarget> callback) { Callback = callback; }
			Func<Size, RenderTarget> Callback;
			RenderTarget IRenderTargetProvider.Get(Size size)
			{
				return Callback(size);
			}
		}

		public DisplayManager(PresentationPanel presentationPanel, IGL gl, GLManager glManager)
		{
			GL = gl;
			this.presentationPanel = presentationPanel;
			GraphicsControl = this.presentationPanel.GraphicsControl;
			CR_GraphicsControl = glManager.GetContextForGraphicsControl(GraphicsControl);
			_glManager = glManager;

			//it's sort of important for these to be initialized to something nonzero
			currEmuWidth = currEmuHeight = 1;

			if (GL is BizHawk.Bizware.BizwareGL.Drivers.OpenTK.IGL_TK)
				Renderer = new GuiRenderer(GL);
			else if (GL is BizHawk.Bizware.BizwareGL.Drivers.SlimDX.IGL_SlimDX9)
				Renderer = new GuiRenderer(GL);
			else
				Renderer = new GDIPlusGuiRenderer((BizHawk.Bizware.BizwareGL.Drivers.GdiPlus.IGL_GdiPlus)GL);

			VideoTextureFrugalizer = new BizHawk.Client.EmuHawk.TextureFrugalizer(GL);

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
		}

		//rendering resources:
		public IGL GL;
		IGuiRenderer Renderer;
		private GLManager _glManager;

		//layer resources
		PresentationPanel presentationPanel; //well, its the final layer's target, at least
		GraphicsControl GraphicsControl; //well, its the final layer's target, at least
		GLManager.ContextRef CR_GraphicsControl;
		FilterProgram CurrentFilterProgram;

		/// <summary>
		/// these variables will track the dimensions of the last frame's (or the next frame? this is confusing) emulator native output size
		/// </summary>
		int currEmuWidth, currEmuHeight;

		BizHawk.Client.EmuHawk.TextureFrugalizer VideoTextureFrugalizer;
		Dictionary<string, BizHawk.Client.EmuHawk.TextureFrugalizer> LuaSurfaceFrugalizers = new Dictionary<string, BizHawk.Client.EmuHawk.TextureFrugalizer>();
		RenderTargetFrugalizer[] ShaderChainFrugalizers;
		
		BizHawk.Client.EmuHawk.Filters.RetroShaderChain ShaderChain_user;

		public void RefreshUserShader()
		{
			if (ShaderChain_user != null)
				ShaderChain_user.Dispose();
			if (File.Exists(Global.Config.DispUserFilterPath))
			{
				var fi = new FileInfo(Global.Config.DispUserFilterPath);
				using (var stream = fi.OpenRead())
					ShaderChain_user = new BizHawk.Client.EmuHawk.Filters.RetroShaderChain(GL, new BizHawk.Client.EmuHawk.Filters.RetroShaderPreset(stream), Path.GetDirectoryName(Global.Config.DispUserFilterPath));
			}
		}

		FilterProgram BuildDefaultChain(Size chain_insize, Size chain_outsize, bool includeOSD)
		{
			//select user special FX shader chain
			Dictionary<string, object> selectedChainProperties = new Dictionary<string, object>();
			BizHawk.Client.EmuHawk.Filters.RetroShaderChain selectedChain = null;

			if (Global.Config.TargetDisplayFilter == 3 && ShaderChain_user != null && ShaderChain_user.Available)
				selectedChain = ShaderChain_user;

			BizHawk.Client.EmuHawk.Filters.FinalPresentation fPresent = new BizHawk.Client.EmuHawk.Filters.FinalPresentation(chain_outsize);
			BizHawk.Client.EmuHawk.Filters.SourceImage fInput = new BizHawk.Client.EmuHawk.Filters.SourceImage(chain_insize);
			BizHawk.Client.EmuHawk.Filters.OSD fOSD = new BizHawk.Client.EmuHawk.Filters.OSD();
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

			//choose final filter
			BizHawk.Client.EmuHawk.Filters.FinalPresentation.eFilterOption finalFilter = BizHawk.Client.EmuHawk.Filters.FinalPresentation.eFilterOption.None;
			if (Global.Config.DispFinalFilter == 1) finalFilter = BizHawk.Client.EmuHawk.Filters.FinalPresentation.eFilterOption.Bilinear;
			if (Global.Config.DispFinalFilter == 2) finalFilter = BizHawk.Client.EmuHawk.Filters.FinalPresentation.eFilterOption.Bicubic;

			fPresent.FilterOption = finalFilter;

			//add final presentation 
			chain.AddFilter(fPresent, "presentation");

			return chain;
		}

		void AppendRetroShaderChain(FilterProgram program, string name, BizHawk.Client.EmuHawk.Filters.RetroShaderChain retroChain, Dictionary<string, object> properties)
		{
			for (int i = 0; i < retroChain.Passes.Length; i++)
			{
				var pass = retroChain.Passes[i];
				var rsp = new BizHawk.Client.EmuHawk.Filters.RetroShaderPass(retroChain, i);
				string fname = string.Format("{0}[{1}]", name, i);
				program.AddFilter(rsp, fname);
				rsp.Parameters = properties;
			}
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
			var job = new JobInfo
			{
				videoProvider = videoProvider,
				simulate = false,
				chain_outsize = GraphicsControl.Size,
				includeOSD = true
			};
			UpdateSourceInternal(job);
		}

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
		}

		/// <summary>
		/// Attempts to calculate a good client size with the given zoom factor, considering the user's DisplayManager preferences
		/// </summary>
		public Size CalculateClientSize(IVideoProvider videoProvider, int zoom)
		{
			bool ar_active = Global.Config.DispFixAspectRatio;
			bool ar_system = Global.Config.DispManagerAR == Config.EDispManagerAR.System;
			bool ar_custom = Global.Config.DispManagerAR == Config.EDispManagerAR.Custom;
			bool ar_correct = ar_system || ar_custom;
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
							Vector2[] trials = new [] {
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
			_glManager.Activate(CR_GraphicsControl);

			IVideoProvider videoProvider = job.videoProvider;
			bool simulate = job.simulate;
			Size chain_outsize = job.chain_outsize;
			
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
			}

			int[] videoBuffer = videoProvider.GetVideoBuffer();

			int bufferWidth = videoProvider.BufferWidth;
			int bufferHeight = videoProvider.BufferHeight;
			bool isGlTextureId = videoBuffer.Length == 1;

			//TODO - need to do some work here for GDI+ to repair gl texture ID importing
			BitmapBuffer bb = null;
			Texture2d videoTexture = null;
			if (!simulate)
			{
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
					GL.SetTextureWrapMode(videoTexture, true);
				}

				//TEST (to be removed once we have an actual example of bring in a texture ID from opengl emu core):
				//if (!isGlTextureId)
				//{
				//  videoBuffer = new int[1] { videoTexture.Id.ToInt32() };
				//  goto TESTEROO;
				//}
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
			BizHawk.Client.EmuHawk.Filters.SourceImage fInput = filterProgram["input"] as BizHawk.Client.EmuHawk.Filters.SourceImage;
			fInput.Texture = videoTexture;
			
			//setup the final presentation filter
			BizHawk.Client.EmuHawk.Filters.FinalPresentation fPresent = filterProgram["presentation"] as BizHawk.Client.EmuHawk.Filters.FinalPresentation;
			fPresent.VirtualTextureSize = new Size(vw, vh);
			fPresent.TextureSize = new Size(bufferWidth, bufferHeight);
			fPresent.BackgroundColor = videoProvider.BackgroundColor;
			fPresent.GuiRenderer = Renderer;
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
			//begin rendering on this context
			//should this have been done earlier?
			//do i need to check this on an intel video card to see if running excessively is a problem? (it used to be in the FinalTarget command below, shouldnt be a problem)
			//GraphicsControl.Begin();

			//run filter chain
			Texture2d texCurr = null;
			RenderTarget rtCurr = null;
			int rtCounter = 0;
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

			if (job.offscreen)
			{
				job.offscreenBB = rtCurr.Texture2d.Resolve();
			}
			else
			{
				Debug.Assert(inFinalTarget);
				//apply the vsync setting (should probably try to avoid repeating this)
				bool vsync = Global.Config.VSyncThrottle || Global.Config.VSync;

				//ok, now this is a bit undesireable.
				//maybe the user wants vsync, but not vsync throttle.
				//this makes sense... but we dont have the infrastructure to support it now (we'd have to enable triple buffering or something like that)
				//so what we're gonna do is disable vsync no matter what if throttling is off, and maybe nobody will notice.
				if (Global.DisableSecondaryThrottling)
					vsync = false;

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

				//present and conclude drawing
				presentationPanel.GraphicsControl.SwapBuffers();

				//nope. dont do this. workaround for slow context switching on intel GPUs. just switch to another context when necessary before doing anything
				//presentationPanel.GraphicsControl.End();
			}
		}

		bool? LastVsyncSetting;
		GraphicsControl LastVsyncSettingGraphicsControl;
	}

}