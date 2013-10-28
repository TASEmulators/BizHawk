using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		public void client_closerom()
		{
			GlobalWinF.MainForm.CloseROM();
		}

		public int client_getwindowsize()
		{
			return Global.Config.TargetZoomFactor;
		}

		public void client_opencheats()
		{
			GlobalWinF.MainForm.LoadCheatsWindow();
		}

		public void client_openhexeditor()
		{
			GlobalWinF.MainForm.LoadHexEditor();
		}

		public void client_openramwatch()
		{
			GlobalWinF.MainForm.LoadRamWatch(true);
		}

		public void client_openramsearch()
		{
			GlobalWinF.MainForm.LoadRamSearch();
		}

		public void client_openrom(object lua_input)
		{
			GlobalWinF.MainForm.LoadRom(lua_input.ToString());
		}

		public void client_opentasstudio()
		{
			GlobalWinF.MainForm.LoadTAStudio();
		}

		public void client_opentoolbox()
		{
			GlobalWinF.MainForm.LoadToolBox();
		}

		public void client_opentracelogger()
		{
			GlobalWinF.MainForm.LoadTraceLogger();
		}

		public void client_pause_av()
		{
			GlobalWinF.MainForm.PauseAVI = true;
		}

		public void client_reboot_core()
		{
			GlobalWinF.MainForm.RebootCore();
		}

		public int client_screenheight()
		{
			return GlobalWinF.RenderPanel.NativeSize.Height;
		}

		public void client_screenshot(object path = null)
		{
			if (path == null)
			{
				GlobalWinF.MainForm.TakeScreenshot();
			}
			else
			{
				GlobalWinF.MainForm.TakeScreenshot(path.ToString());
			}
		}

		public void client_screenshottoclipboard()
		{
			GlobalWinF.MainForm.TakeScreenshotToClipboard();
		}

		public void client_setscreenshotosd(bool value)
		{
			Global.Config.Screenshot_CaptureOSD = value;
		}

		public int client_screenwidth()
		{
			return GlobalWinF.RenderPanel.NativeSize.Width;
		}

		public void client_setwindowsize(object window_size)
		{
			try
			{
				string temp = window_size.ToString();
				int size = Convert.ToInt32(temp);
				if (size == 1 || size == 2 || size == 3 || size == 4 || size == 5 || size == 10)
				{
					Global.Config.TargetZoomFactor = size;
					GlobalWinF.MainForm.FrameBufferResized();
					GlobalWinF.OSD.AddMessage("Window size set to " + size.ToString() + "x");
				}
				else
				{
					console_log("Invalid window size");
				}
			}
			catch
			{
				console_log("Invalid window size");
			}

		}

		public void client_unpause_av()
		{
			GlobalWinF.MainForm.PauseAVI = false;
		}

		public int client_xpos()
		{
			return GlobalWinF.MainForm.DesktopLocation.X;
		}

		public int client_ypos()
		{
			return GlobalWinF.MainForm.DesktopLocation.Y;
		}
	}
}
