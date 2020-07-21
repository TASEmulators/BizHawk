using System.Drawing;

using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.UITests
{
	[TestClass]
	public class ProofOfConceptUITest
	{
		[TestMethod]
		public void Test()
		{
			using var app = Application.Launch("../output/EmuHawk.exe");
			using var automation = new UIA3Automation();
			var window = app.GetMainWindow(automation);

			Assert.IsNotNull(window);
			Assert.IsNotNull(window.Title);
			var image = Capture.Screen();
			image.ApplyOverlays(new MouseOverlay(image));
			image.ToFile("/tmp/screen.png");
			Capture.Element(window).ToFile("/tmp/window.png");
			Capture.Rectangle(new Rectangle(0, 0, 500, 300)).ToFile("/tmp/rect.png");
			Capture.ElementRectangle(window, new Rectangle(0, 0, 50, 150)).ToFile("/tmp/elemrect.png");

#if false
			var cf = new ConditionFactory(new UIA3PropertyLibrary());
			var menu = window.FindFirstDescendant(cf.Menu()).AsMenu();
			menu.Items[4].Items[0].Invoke(); // should open the Tool Box
//			menu.Items["Config"].Items["Paths..."].Invoke(); // who knows if this works

			// even less likely to work
//			window.FindFirstDescendant(cf.ByName("PresentationPanel")).RightClick();
//			window.ContextMenu.Items[1].DrawHighlight();
#endif

			app.Close();
		}

#if false
		public void VideoTest()
		{
			Logger.Default = new NUnitProgressLogger();
			Logger.Default.SetLevel(LogLevel.Debug);
			SystemInfo.RefreshAll();
			var recorder = new VideoRecorder(new VideoRecorderSettings { VideoQuality = 26, ffmpegPath = @"C:\Temp\ffmpeg.exe", TargetVideoPath = @"C:\temp\out.mp4" }, r =>
			{
				var img = Capture.Screen(1);
				img.ApplyOverlays(new InfoOverlay(img) { RecordTimeSpan = r.RecordTimeSpan, OverlayStringFormat = @"{rt:hh\:mm\:ss\.fff} / {name} / CPU: {cpu} / RAM: {mem.p.used}/{mem.p.tot} ({mem.p.used.perc})" }, new MouseOverlay(img));
				return img;
			});
			System.Threading.Thread.Sleep(5000);
			recorder.Dispose();
		}
#endif
	}
}
