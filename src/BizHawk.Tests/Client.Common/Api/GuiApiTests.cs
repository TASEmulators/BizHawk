using System.Drawing;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests.Client.Common.Api
{
	[TestClass]
	public class GuiApiTests
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		// null values are initialized in the setup method
		private GuiApi guiApi = null;
		private DisplayManagerBase displayManager = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		[TestInitialize]
		public void Setup()
		{
			displayManager = new TestDisplayManager(new NullEmulator());
			guiApi = new GuiApi((s) => { }, displayManager);
		}

		[TestMethod]
		public void TestDrawPixel()
		{
			// Draw
			guiApi.DrawPixel(4, 4, Color.Red, DisplaySurfaceID.Client);
			BitmapBufferVideoProvider vp = new BitmapBufferVideoProvider(new BitmapBuffer(8, 8));
			var buffer = displayManager.RenderOffscreenLua(vp);

			// Validate
			Assert.AreEqual(buffer.GetPixel(4, 4), Color.Red.ToArgb());
		}

		[TestMethod]
		public void TestNewFrameClears()
		{
			guiApi.DrawPixel(2, 2, Color.Red, DisplaySurfaceID.Client);
			BitmapBufferVideoProvider vp = new BitmapBufferVideoProvider(new BitmapBuffer(8, 8));
			var buffer = displayManager.RenderOffscreenLua(vp);
			Assert.AreEqual(buffer.GetPixel(2, 2), Color.Red.ToArgb());

			guiApi.BeginFrame();
			guiApi.EndFrame();

			buffer = displayManager.RenderOffscreenLua(vp);
			Assert.AreEqual(buffer.GetPixel(2, 2), Color.Black.ToArgb());
		}

		[TestMethod]
		public void TestStartFrameDoesNotClear()
		{
			guiApi.DrawPixel(2, 2, Color.Red, DisplaySurfaceID.Client);
			BitmapBufferVideoProvider vp = new BitmapBufferVideoProvider(new BitmapBuffer(8, 8));
			var buffer = displayManager.RenderOffscreenLua(vp);
			Assert.AreEqual(buffer.GetPixel(2, 2), Color.Red.ToArgb());

			guiApi.BeginFrame();
			buffer = displayManager.RenderOffscreenLua(vp);
			Assert.AreEqual(buffer.GetPixel(2, 2), Color.Red.ToArgb());
		}

		[TestMethod]
		public void TestDrawAfterBeginFrameIsVisibleOnlyAfterEndFrame()
		{
			BitmapBufferVideoProvider vp = new BitmapBufferVideoProvider(new BitmapBuffer(8, 8));

			guiApi.BeginFrame();
			guiApi.DrawPixel(2, 2, Color.Red, DisplaySurfaceID.Client);

			var buffer = displayManager.RenderOffscreenLua(vp);
			Assert.AreEqual(buffer.GetPixel(2, 2), Color.Black.ToArgb());

			guiApi.EndFrame();
			buffer = displayManager.RenderOffscreenLua(vp);
			Assert.AreEqual(buffer.GetPixel(2, 2), Color.Red.ToArgb());
		}

		[TestMethod]
		public void TestWithSurfaceClearsSurface()
		{
			BitmapBufferVideoProvider vp = new BitmapBufferVideoProvider(new BitmapBuffer(8, 8));

			guiApi.WithSurface(DisplaySurfaceID.Client, () => guiApi.DrawPixel(2, 2, Color.Blue));
			var buffer = displayManager.RenderOffscreenLua(vp);
			Assert.AreEqual(Color.Blue.ToArgb(), buffer.GetPixel(2, 2));

			guiApi.WithSurface(DisplaySurfaceID.Client, () => { });
			buffer = displayManager.RenderOffscreenLua(vp);
			Assert.AreEqual(Color.Black.ToArgb(), buffer.GetPixel(2, 2));
		}

	}
}
