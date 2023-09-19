using System.Drawing;

using BizHawk.Bizware.Graphics;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests
{
	internal class TestDisplayManager : DisplayManagerBase
	{
		private Size _screenSize;

		private TestDisplayManager(Config config, IEmulator emulator, InputManager inputManager, IGL_GDIPlus gl)
			: base(config, emulator, inputManager, null, gl.DispMethodEnum, gl, new GDIPlusGuiRenderer(gl))
		{
			var vp = emulator.AsVideoProviderOrDefault();
			_screenSize = new Size(vp.BufferWidth, vp.BufferHeight);

		}
		public TestDisplayManager(IEmulator emulator)
			: this(new Config(), emulator, new InputManager(), new IGL_GDIPlus())
		{ }

		public override void ActivateOpenGLContext() { } // Nothing. We only use GDIPlus here.
		public override Size GetPanelNativeSize() => _screenSize;
		protected override void ActivateGraphicsControlContext() { }
		protected override Size GetGraphicsControlSize() => _screenSize;
		protected override Point GraphicsControlPointToClient(Point p) => p;
		protected override void SwapBuffersOfGraphicsControl() { }
	}
}
