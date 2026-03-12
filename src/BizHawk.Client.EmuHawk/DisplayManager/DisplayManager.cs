using System.Diagnostics;
using System.Drawing;

using BizHawk.Bizware.Graphics;
using BizHawk.Bizware.Graphics.Controls;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public class DisplayManager : DisplayManagerBase
	{
		// this function requires at least windows 10
		private static readonly unsafe delegate* unmanaged[Stdcall]<IntPtr, int> _getDpiForWindow;

		static DisplayManager()
		{
			if (OSTailoredCode.IsUnixHost)
			{
				return;
			}

			var lib = OSTailoredCode.LinkedLibManager.LoadOrZero("user32.dll");
			if (lib == IntPtr.Zero)
			{
				return;
			}

			unsafe
			{
				_getDpiForWindow = (delegate* unmanaged[Stdcall]<IntPtr, int>)
					OSTailoredCode.LinkedLibManager.GetProcAddrOrZero(lib, "GetDpiForWindow");
			}
		}

		private readonly Func<bool> _getIsSecondaryThrottlingDisabled;

		private bool? _lastVsyncSetting;

		private GraphicsControl _lastVsyncSettingGraphicsControl;

		// layer resources

		private readonly PresentationPanel _presentationPanel; // well, its the final layer's target, at least

		private GraphicsControl _graphicsControl => _presentationPanel.GraphicsControl;

		public DisplayManager(
			Config config,
			IEmulator emulator,
			InputManager inputManager,
			IMovieSession movieSession,
			IGL gl,
			PresentationPanel presentationPanel,
			Func<bool> getIsSecondaryThrottlingDisabled)
				: base(config, emulator, inputManager, movieSession, gl.DispMethodEnum, gl, gl.CreateGuiRenderer())
		{
			_presentationPanel = presentationPanel;
			_getIsSecondaryThrottlingDisabled = getIsSecondaryThrottlingDisabled;
		}

		public override void ActivateOpenGLContext()
		{
			if (_gl.DispMethodEnum == EDispMethod.OpenGL)
			{
				_graphicsControl.Begin();
			}
		}

		protected override void ActivateGraphicsControlContext() => _graphicsControl.Begin();

		protected override Size GetGraphicsControlSize() => _graphicsControl.Size;

		public override Size GetPanelNativeSize() => _presentationPanel.NativeSize;

		protected override unsafe int GetGraphicsControlDpi()
			=> _getDpiForWindow == null ? DEFAULT_DPI : _getDpiForWindow(_graphicsControl.Handle);

		protected override Point GraphicsControlPointToClient(Point p) => _graphicsControl.PointToClient(p);

		protected override void SwapBuffersOfGraphicsControl() => _graphicsControl.SwapBuffers();

		protected override void UpdateSourceDrawingWork(JobInfo job)
		{
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

				// no cost currently to just always call this...
				_graphicsControl.AllowTearing(GlobalConfig.DispAllowTearing);

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

			//TODO - auto-create and age these (and dispose when old)
			int rtCounter = 0;
			_currentFilterProgram.RenderTargetProvider = new DisplayManagerRenderTargetProvider(size => _shaderChainFrugalizers[rtCounter++].Get(size));

			RunFilterChainSteps(ref rtCounter, out var rtCurr, out var inFinalTarget);

			if (job.Offscreen)
			{
				job.OffscreenBb = rtCurr.Resolve();
				job.OffscreenBb.DiscardAlpha();
				return;
			}

			Debug.Assert(inFinalTarget, "not in final target?");

			// present and conclude drawing
			_graphicsControl.SwapBuffers();

			// nope. don't do this. workaround for slow context switching on intel GPUs. just switch to another context when necessary before doing anything
			// _graphicsControl.End();
		}
	}
}
