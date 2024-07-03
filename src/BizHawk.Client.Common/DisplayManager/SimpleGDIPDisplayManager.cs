#nullable enable

using System.Drawing;

using BizHawk.Bizware.Graphics;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class SimpleGDIPDisplayManager : DisplayManagerBase
	{
		private SimpleGDIPDisplayManager(Config config, IEmulator emuCore, IGL_GDIPlus glImpl)
			: base(
				config,
				emuCore,
				inputManager: null,
				movieSession: null,
				EDispMethod.GdiPlus,
				glImpl,
				new GDIPlusGuiRenderer(glImpl))
			{}

		public SimpleGDIPDisplayManager(Config config, IEmulator emuCore, Func<(int Width, int Height)> getVirtualSize)
			: this(config, emuCore, new IGL_GDIPlus()) {}

		protected override void ActivateGraphicsControlContext() {}

		public override void ActivateOpenGLContext() {}

		protected override int GetGraphicsControlDpi()
			=> DisplayManagerBase.DEFAULT_DPI;

		protected override Size GetGraphicsControlSize()
			=> throw new NotImplementedException();

		public override Size GetPanelNativeSize()
			=> throw new NotImplementedException();

		protected override Point GraphicsControlPointToClient(Point p)
			=> throw new NotImplementedException();

		protected override void SwapBuffersOfGraphicsControl() {}
	}
}
