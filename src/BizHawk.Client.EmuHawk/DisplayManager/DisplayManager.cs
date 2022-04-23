using System;
using System.Diagnostics;
using System.Drawing;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public class DisplayManager : DisplayManagerBase
	{
		private readonly Func<bool> _getIsSecondaryThrottlingDisabled;

		private bool? _lastVsyncSetting;

		private GraphicsControl _lastVsyncSettingGraphicsControl;

		// layer resources

		private readonly PresentationPanel _presentationPanel; // well, its the final layer's target, at least

		private readonly GLManager.ContextRef _crGraphicsControl;

		private GraphicsControl _graphicsControl => _presentationPanel.GraphicsControl;

		public DisplayManager(
			Config config,
			IEmulator emulator,
			InputManager inputManager,
			IMovieSession movieSession,
			IGL gl,
			PresentationPanel presentationPanel,
			Func<bool> getIsSecondaryThrottlingDisabled)
				: base(config, emulator, inputManager, movieSession, gl.DispMethodEnum, gl, gl.CreateRenderer())
		{
			_presentationPanel = presentationPanel;
			_getIsSecondaryThrottlingDisabled = getIsSecondaryThrottlingDisabled;

			// setup the GL context manager, needed for coping with multiple opengl cores vs opengl display method
			// but is it tho? --yoshi
			// turns out it was, calling Instance getter here initialises it, and the encapsulated Activate call is necessary too --yoshi
			_crGraphicsControl = GLManager.Instance.GetContextForGraphicsControl(_graphicsControl);
		}

		protected override void ActivateGLContext() => GLManager.Instance.Activate(_crGraphicsControl);

		protected override Size GetGraphicsControlSize() => _graphicsControl.Size;

		public override Size GetPanelNativeSize() => _presentationPanel.NativeSize;

		protected override Point GraphicsControlPointToClient(Point p) => _graphicsControl.PointToClient(p);

		protected override void SwapBuffersOfGraphicsControl() => _graphicsControl.SwapBuffers();

		protected override void UpdateSourceDrawingWork(JobInfo job)
		{
			bool alternateVsync = false;

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
				if (vsync && GlobalConfig.DispAlternateVsync && GlobalConfig.VSyncThrottle && _gl.DispMethodEnum is EDispMethod.SlimDX9)
				{
					alternateVsync = true;
					//unset normal vsync if we've chosen the alternate vsync
					vsync = false;
				}

				//TODO - whats so hard about triple buffering anyway? just enable it always, and change api to SetVsync(enable,throttle)
				//maybe even SetVsync(enable,throttlemethod) or just SetVsync(enable,throttle,advanced)

				if (_lastVsyncSetting != vsync || _lastVsyncSettingGraphicsControl != _graphicsControl)
				{
					if (_lastVsyncSetting == null && vsync)
					{
						// Workaround for vsync not taking effect at startup (Intel graphics related?)
						_graphicsControl.SetVsync(false);
					}
					_graphicsControl.SetVsync(vsync);
					_lastVsyncSettingGraphicsControl = _graphicsControl;
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
			RunFilterChainSteps(ref rtCounter, out var rtCurr, out var inFinalTarget);
			_gl.EndScene();

			if (job.Offscreen)
			{
				job.OffscreenBb = rtCurr.Texture2d.Resolve();
				job.OffscreenBb.DiscardAlpha();
				return;
			}

			Debug.Assert(inFinalTarget);

			// wait for vsync to begin
			if (alternateVsync) ((dynamic) _gl).AlternateVsyncPass(0);

			// present and conclude drawing
			_graphicsControl.SwapBuffers();

			// wait for vsync to end
			if (alternateVsync) ((dynamic) _gl).AlternateVsyncPass(1);

			// nope. don't do this. workaround for slow context switching on intel GPUs. just switch to another context when necessary before doing anything
			// presentationPanel.GraphicsControl.End();
		}
	}
}
