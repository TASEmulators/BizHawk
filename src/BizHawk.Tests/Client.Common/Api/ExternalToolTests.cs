using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Tests.Client.Common.Lua;
using BizHawk.Tests.Implementations;
using BizHawk.Tests.Mocks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests.Client.Common.Api
{
	[TestClass]
	public class ExternalToolTests
	{
		private Config config = new();

		[ClassInitialize]
		public static void TestInitialize(TestContext context)
		{
			// Move our .dll to a directory by itself, so that the ExternalToolManager will only find us.
			string asmName = Assembly.GetExecutingAssembly().Location;
			Directory.CreateDirectory("extTools");
			File.Copy(asmName, "extTools/ExternalToolTests.dll", true);
		}

		[TestInitialize]
		public void TestSetup()
		{
			config.PathEntries.Paths.Find(
				static (e) => string.Equals(e.Type, "External Tools", System.StringComparison.Ordinal)
				)!.Path = "./extTools";
		}

		private TestExternalAPI LoadExternalTool(ToolManagerBase toolManager)
		{
			var info = toolManager.ExternalToolInfos.First(static (i) => i.Text == "TEST");
			return (toolManager.LoadExternalToolForm(info) as TestExternalAPI)!;
		}

		[TestMethod]
		public void TestExternalToolIsFound()
		{
			ExternalToolManager manager = new ExternalToolManager(config, () => ("", ""));

			Assert.IsTrue(manager.ToolStripItems.Count != 0);
			var item = manager.ToolStripItems.First(static (info) => info.Text == "TEST");
			Assert.AreEqual("TEST", item.Text);
		}

		[TestMethod]
		public void TestExternalToolIsCalled()
		{
			IMainFormForApi mainFormApi = new MockMainFormForApi(new NullEmulator());
			DisplayManagerBase displayManager = new TestDisplayManager(mainFormApi.Emulator);
			TestToolManager toolManager = new TestToolManager(mainFormApi, config, displayManager);

			TestExternalAPI externalApi = LoadExternalTool(toolManager);
			Assert.AreEqual(0, externalApi.frameCount);
			toolManager.UpdateToolsBefore();
			toolManager.UpdateToolsAfter();
			Assert.AreEqual(1, externalApi.frameCount);
		}

		[TestMethod]
		public void TestExternalToolCanUseApi()
		{
			IMainFormForApi mainFormApi = new MockMainFormForApi(new NullEmulator());
			DisplayManagerBase displayManager = new TestDisplayManager(mainFormApi.Emulator);
			TestToolManager toolManager = new TestToolManager(mainFormApi, config, displayManager);

			TestExternalAPI externalApi = LoadExternalTool(toolManager);
			Assert.AreEqual(mainFormApi.Emulator.AsVideoProviderOrDefault().BufferHeight, externalApi.APIs.EmuClient.BufferHeight());
		}

		[TestMethod]
		public void TestExternalToolCanDraw()
		{
			IMainFormForApi mainFormApi = new MockMainFormForApi(new NullEmulator());
			DisplayManagerBase displayManager = new TestDisplayManager(mainFormApi.Emulator);
			TestToolManager toolManager = new TestToolManager(mainFormApi, config, displayManager);

			TestExternalAPI externalApi = LoadExternalTool(toolManager);
			IGuiApi guiApi = externalApi.APIs.Gui;

			guiApi.DrawPixel(2, 2, Color.Blue, DisplaySurfaceID.Client);
			BitmapBufferVideoProvider vp = new BitmapBufferVideoProvider(new BitmapBuffer(8, 8));
			var buffer = displayManager.RenderOffscreenLua(vp);
			Assert.AreEqual(buffer.GetPixel(2, 2), Color.Blue.ToArgb());
		}

		[TestMethod]
		public void TestExternalToolCanDrawWhileLuaIsRunning()
		{
			IMainFormForApi mainFormApi = new MockMainFormForApi(new NullEmulator());
			DisplayManagerBase displayManager = new TestDisplayManager(mainFormApi.Emulator);
			TestToolManager toolManager = new TestToolManager(mainFormApi, config, displayManager);

			TestExternalAPI externalApi = LoadExternalTool(toolManager);
			IGuiApi guiApi = externalApi.APIs.Gui;
			TestLuaDrawing luaTester = new TestLuaDrawing(mainFormApi, config, displayManager); // not the default constructor; we need to share a DisplayManager at least

			// trigger some frames with both to ensure we can cycle through buffers
			guiApi.WithSurface(DisplaySurfaceID.Client, () => { });
			luaTester.luaLibraries.CallFrameBeforeEvent();
			luaTester.luaLibraries.CallFrameAfterEvent();

			// draw
			guiApi.DrawPixel(2, 2, Color.Blue, DisplaySurfaceID.Client);
			BitmapBufferVideoProvider vp = new BitmapBufferVideoProvider(new BitmapBuffer(8, 8));
			var buffer = displayManager.RenderOffscreenLua(vp);
			Assert.AreEqual(Color.Blue.ToArgb(), buffer.GetPixel(2, 2));
		}
	}
}
