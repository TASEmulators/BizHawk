using System;
using LuaInterface;
using BizHawk.Client.Common;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		#region Emu Library Helpers

		// TODO: error handling for argument count mismatch
		private void emu_setrenderplanes_do(object[] lua_p)
		{
			if (Global.Emulator is NES)
			{
				Global.CoreComm.NES_ShowOBJ = Global.Config.NESDispSprites = (bool)lua_p[0];
				Global.CoreComm.NES_ShowBG = Global.Config.NESDispBackground = (bool)lua_p[1];
			}
			else if (Global.Emulator is Emulation.Consoles.TurboGrafx.PCEngine)
			{
				Global.CoreComm.PCE_ShowOBJ1 = Global.Config.PCEDispOBJ1 = (bool)lua_p[0];
				Global.CoreComm.PCE_ShowBG1 = Global.Config.PCEDispBG1 = (bool)lua_p[1];
				if (lua_p.Length > 2)
				{
					Global.CoreComm.PCE_ShowOBJ2 = Global.Config.PCEDispOBJ2 = (bool)lua_p[2];
					Global.CoreComm.PCE_ShowBG2 = Global.Config.PCEDispBG2 = (bool)lua_p[3];
				}
			}
			else if (Global.Emulator is Emulation.Consoles.Sega.SMS)
			{
				Global.CoreComm.SMS_ShowOBJ = Global.Config.SMSDispOBJ = (bool)lua_p[0];
				Global.CoreComm.SMS_ShowBG = Global.Config.SMSDispBG = (bool)lua_p[1];
			}
		}

		#endregion

		public void emu_displayvsync(object boolean)
		{
			string temp = boolean.ToString();
			if (!String.IsNullOrWhiteSpace(temp))
			{
				if (temp == "0" || temp.ToLower() == "false")
				{
					Global.Config.VSyncThrottle = false;
				}
				else
				{
					Global.Config.VSyncThrottle = true;
				}
				GlobalWinF.MainForm.VsyncMessage();
			}
		}

		public void emu_enablerewind(object boolean)
		{
			string temp = boolean.ToString();
			if (!String.IsNullOrWhiteSpace(temp))
			{
				if (temp == "0" || temp.ToLower() == "false")
				{
					GlobalWinF.MainForm.RewindActive = false;
					GlobalWinF.OSD.AddMessage("Rewind suspended");
				}
				else
				{
					GlobalWinF.MainForm.RewindActive = true;
					GlobalWinF.OSD.AddMessage("Rewind enabled");
				}
			}
		}

		public void emu_frameadvance()
		{
			FrameAdvanceRequested = true;
			currThread.Yield(0);
		}

		public int emu_framecount()
		{
			return Global.Emulator.Frame;
		}

		public void emu_frameskip(object num_frames)
		{
			try
			{
				string temp = num_frames.ToString();
				int frames = Convert.ToInt32(temp);
				if (frames > 0)
				{
					Global.Config.FrameSkip = frames;
					GlobalWinF.MainForm.FrameSkipMessage();
				}
				else
				{
					ConsoleLuaLibrary.console_log("Invalid frame skip value");
				}
			}
			catch
			{
				ConsoleLuaLibrary.console_log("Invalid frame skip value");
			}
		}

		public string emu_getsystemid()
		{
			return Global.Emulator.SystemId;
		}

		public bool emu_islagged()
		{
			return Global.Emulator.IsLagFrame;
		}

		public bool emu_ispaused()
		{
			return GlobalWinF.MainForm.EmulatorPaused;
		}

		public int emu_lagcount()
		{
			return Global.Emulator.LagCount;
		}

		public void emu_limitframerate(object boolean)
		{
			string temp = boolean.ToString();
			if (!String.IsNullOrWhiteSpace(temp))
			{
				if (temp == "0" || temp.ToLower() == "false")
				{
					Global.Config.ClockThrottle = false;
				}
				else
				{
					Global.Config.ClockThrottle = true;
				}
				GlobalWinF.MainForm.LimitFrameRateMessage();
			}
		}

		public void emu_minimizeframeskip(object boolean)
		{
			string temp = boolean.ToString();
			if (!String.IsNullOrWhiteSpace(temp))
			{
				if (temp == "0" || temp.ToLower() == "false")
				{
					Global.Config.AutoMinimizeSkipping = false;
				}
				else
				{
					Global.Config.AutoMinimizeSkipping = true;
				}
				GlobalWinF.MainForm.MinimizeFrameskipMessage();
			}
		}

		public void emu_on_snoop(LuaFunction luaf)
		{
			if (luaf != null)
			{
				Global.Emulator.CoreComm.InputCallback = delegate
					{
					try
					{
						luaf.Call();
					}
					catch (SystemException e)
					{
						GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
							"error running function attached by lua function emu.on_snoop" +
							"\nError message: " + e.Message);
					}
				};
			}
			else
				Global.Emulator.CoreComm.InputCallback = null;
		}

		public void emu_pause()
		{
			GlobalWinF.MainForm.PauseEmulator();
		}

		public void emu_setrenderplanes( // For now, it accepts arguments up to 5.
			object lua_p0, object lua_p1 = null, object lua_p2 = null,
			object lua_p3 = null, object lua_p4 = null)
		{
			emu_setrenderplanes_do(LuaVarArgs(lua_p0, lua_p1, lua_p2, lua_p3, lua_p4));
		}

		public void emu_speedmode(object percent)
		{
			try
			{
				string temp = percent.ToString();
				int speed = Convert.ToInt32(temp);
				if (speed > 0 && speed < 1000) //arbituarily capping it at 1000%
				{
					GlobalWinF.MainForm.ClickSpeedItem(speed);
				}
				else
				{
					ConsoleLuaLibrary.console_log("Invalid speed value");
				}
			}
			catch
			{
				ConsoleLuaLibrary.console_log("Invalid speed value");
			}
		}

		public void emu_togglepause()
		{
			GlobalWinF.MainForm.TogglePause();
		}

		public void emu_unpause()
		{
			GlobalWinF.MainForm.UnpauseEmulator();
		}

		public void emu_yield()
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			currThread.Yield(0);
		}
	}
}
