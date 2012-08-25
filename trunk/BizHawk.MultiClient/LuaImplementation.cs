using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LuaInterface;
using System.Windows.Forms;
using System.Drawing;
using BizHawk.MultiClient.tools;
using System.Threading;


namespace BizHawk.MultiClient
{
	public class LuaImplementation
	{
		public LuaDocumentation docs = new LuaDocumentation();
		Lua lua = new Lua();
		LuaConsole Caller;
		public EventWaitHandle LuaWait;
		public bool isRunning;
		private int CurrentMemoryDomain = 0; //Main memory by default
		public bool FrameAdvanceRequested;
		Lua currThread;
		LuaFunction savestate_registersavefunc;
		LuaFunction savestate_registerloadfunc;

		public void SavestateRegisterSave(string name)
		{
			if (savestate_registersavefunc != null)
			{
				try
				{
					savestate_registersavefunc.Call(name);
				}
				catch (SystemException e)
				{
					Global.MainForm.LuaConsole1.WriteToOutputWindow("error running function attached by lua function savestate.registersave" +
						"\nError message: " + e.Message);
				}
			}
		}

		public void SavestateRegisterLoad(string name)
		{
			if (savestate_registerloadfunc != null)
			{
				try
				{
					savestate_registerloadfunc.Call(name);
				}
				catch (SystemException e)
				{
					Global.MainForm.LuaConsole1.WriteToOutputWindow("error running function attached by lua function savestate.registerload" +
						"\nError message: " + e.Message);
				}
			}
		}

		public LuaImplementation(LuaConsole passed)
		{
			LuaWait = new AutoResetEvent(false);
			docs.Clear();
			Caller = passed.get();
			LuaRegister(lua);
		}

		public void Close()
		{
			lua = new Lua();
		}

		public void LuaRegister(Lua lua)
		{
			lua.RegisterFunction("print", this, this.GetType().GetMethod("print"));

			//Register libraries
			lua.NewTable("console");
			for (int i = 0; i < ConsoleFunctions.Length; i++)
			{
				lua.RegisterFunction("console." + ConsoleFunctions[i], this, this.GetType().GetMethod("console_" + ConsoleFunctions[i]));
				docs.Add("console", ConsoleFunctions[i], this.GetType().GetMethod("console_" + ConsoleFunctions[i]));
			}

			lua.NewTable("gui");
			for (int i = 0; i < GuiFunctions.Length; i++)
			{
				lua.RegisterFunction("gui." + GuiFunctions[i], this, this.GetType().GetMethod("gui_" + GuiFunctions[i]));
				docs.Add("gui", GuiFunctions[i], this.GetType().GetMethod("gui_" + GuiFunctions[i]));
			}

			lua.NewTable("emu");
			for (int i = 0; i < EmuFunctions.Length; i++)
			{
				lua.RegisterFunction("emu." + EmuFunctions[i], this, this.GetType().GetMethod("emu_" + EmuFunctions[i]));
				docs.Add("emu", EmuFunctions[i], this.GetType().GetMethod("emu_" + EmuFunctions[i]));
			}

			lua.NewTable("memory");
			for (int i = 0; i < MemoryFunctions.Length; i++)
			{
				lua.RegisterFunction("memory." + MemoryFunctions[i], this, this.GetType().GetMethod("memory_" + MemoryFunctions[i]));
				docs.Add("memory", MemoryFunctions[i], this.GetType().GetMethod("memory_" + MemoryFunctions[i]));
			}

			lua.NewTable("mainmemory");
			for (int i = 0; i < MainMemoryFunctions.Length; i++)
			{
				lua.RegisterFunction("mainmemory." + MainMemoryFunctions[i], this, this.GetType().GetMethod("mainmemory_" + MainMemoryFunctions[i]));
				docs.Add("mainmemory", MainMemoryFunctions[i], this.GetType().GetMethod("mainmemory_" + MainMemoryFunctions[i]));
			}

			lua.NewTable("savestate");
			for (int i = 0; i < SaveStateFunctions.Length; i++)
			{
				lua.RegisterFunction("savestate." + SaveStateFunctions[i], this, this.GetType().GetMethod("savestate_" + SaveStateFunctions[i]));
				docs.Add("savestate", SaveStateFunctions[i], this.GetType().GetMethod("savestate_" + SaveStateFunctions[i]));
			}

			lua.NewTable("movie");
			for (int i = 0; i < MovieFunctions.Length; i++)
			{
				lua.RegisterFunction("movie." + MovieFunctions[i], this, this.GetType().GetMethod("movie_" + MovieFunctions[i]));
				docs.Add("movie", MovieFunctions[i], this.GetType().GetMethod("movie_" + MovieFunctions[i]));
			}

			lua.NewTable("input");
			for (int i = 0; i < InputFunctions.Length; i++)
			{
				lua.RegisterFunction("input." + InputFunctions[i], this, this.GetType().GetMethod("input_" + InputFunctions[i]));
				docs.Add("input", InputFunctions[i], this.GetType().GetMethod("input_" + InputFunctions[i]));
			}

			lua.NewTable("joypad");
			for (int i = 0; i < JoypadFunctions.Length; i++)
			{
				lua.RegisterFunction("joypad." + JoypadFunctions[i], this, this.GetType().GetMethod("joypad_" + JoypadFunctions[i]));
				docs.Add("joypad", JoypadFunctions[i], this.GetType().GetMethod("joypad_" + JoypadFunctions[i]));
			}

			lua.NewTable("client");
			for (int i = 0; i < MultiClientFunctions.Length; i++)
			{
				lua.RegisterFunction("client." + MultiClientFunctions[i], this, this.GetType().GetMethod("client_" + MultiClientFunctions[i]));
				docs.Add("client", MultiClientFunctions[i], this.GetType().GetMethod("client_" + MultiClientFunctions[i]));
			}

			lua.NewTable("forms");
			for (int i = 0; i < FormsFunctions.Length; i++)
			{
				lua.RegisterFunction("forms." + FormsFunctions[i], this, this.GetType().GetMethod("forms_" + FormsFunctions[i]));
				docs.Add("forms", FormsFunctions[i], this.GetType().GetMethod("forms_" + FormsFunctions[i]));
			}

			docs.Sort();
		}

		public Lua SpawnCoroutine(string File)
		{
			var t = lua.NewThread();
			//LuaRegister(t); //adelikat: Not sure why this was here but it was causing the entire luaimplmeentaiton to be duplicated each time, eventually resulting in crashes
			var main = t.LoadFile(File);
			t.Push(main); //push main function on to stack for subsequent resuming
			return t;
		}

		private int LuaInt(object lua_arg)
		{
			return Convert.ToInt32((double)lua_arg);
		}

		private uint LuaUInt(object lua_arg)
		{
			return Convert.ToUInt32((double)lua_arg);
		}


		public Color GetColor(object color)
		{
			if (color.GetType() == typeof(Double))
			{
				return System.Drawing.Color.FromArgb(int.Parse(long.Parse(color.ToString()).ToString("X"), System.Globalization.NumberStyles.HexNumber));
			}
			else
			{
				return System.Drawing.Color.FromName(color.ToString().ToLower());
			}
		}

		public SolidBrush GetBrush(object color)
		{
			return new System.Drawing.SolidBrush(GetColor(color));
		}
		public Pen GetPen(object color)
		{
			return new System.Drawing.Pen(GetColor(color));
		}


		/**
		 * LuaInterface requires the exact match of parameter count,
		 * except optional parameters. So, if you want to support
		 * variable arguments, declare them as optional and pass
		 * them to this method.
		 */
		private object[] LuaVarArgs(params object[] lua_args)
		{
			int n = lua_args.Length;
			int trim = 0;
			for (int i = n - 1; i >= 0; --i)
				if (lua_args[i] == null) ++trim;
			object[] lua_result = new object[n - trim];
			Array.Copy(lua_args, lua_result, n - trim);
			return lua_result;
		}

		public class ResumeResult
		{
			public bool WaitForFrame;
			public bool Terminated;
		}

		public ResumeResult ResumeScript(Lua script)
		{
			currThread = script;
			int execResult = script.Resume(0);
			currThread = null;
			var result = new ResumeResult();
			if (execResult == 0)
			{
				//terminated
				result.Terminated = true;
			}
			else
			{
				//yielded
				result.WaitForFrame = FrameAdvanceRequested;
			}
			FrameAdvanceRequested = false;
			return result;
		}

		public void print(string s)
		{
			Caller.AddText(s);
			Caller.AddText("\n");
		}

		/****************************************************/
		/*************library definitions********************/
		/****************************************************/
		public static string[] ConsoleFunctions = new string[]
		{
			"output",
			"log",
			"clear",
			"getluafunctionslist",
		};

		public static string[] GuiFunctions = new string[]
		{
			"text",
			"alert",
			"cleartext",
			"drawPixel",
			"drawLine",
			"drawRectangle",
			"drawEllipse",
			"drawPolygon",
			"drawBezier",
			"drawPie",
			"drawIcon",
			"drawImage",
		};

		public static string[] EmuFunctions = new string[]
		{
			"frameadvance",
			"yield",
			"pause",
			"unpause",
			"togglepause",
			"ispaused",
			"speedmode",
			"framecount",
			"lagcount",
			"islagged",
			"getsystemid",
			"setrenderplanes",
			"frameskip",
			"minimizeframeskip",
			"limitframerate",
			"displayvsync",
			"enablerewind"
		};

		public static string[] MemoryFunctions = new string[]
		{
			"usememorydomain",
			"getmemorydomainlist",
			"getcurrentmemorydomain",
			"read_s8",
			"read_u8",
			"read_s16_le",
			"read_s24_le",
			"read_s32_le",
			"read_u16_le",
			"read_u24_le",
			"read_u32_le",
			"read_s16_be",
			"read_s24_be",
			"read_s32_be",
			"read_u16_be",
			"read_u24_be",
			"read_u32_be",
			"write_s8",
			"write_u8",
			"write_s16_le",
			"write_s24_le",
			"write_s32_le",
			"write_u16_le",
			"write_u24_le",
			"write_u32_le",
			"write_s16_be",
			"write_s24_be",
			"write_s32_be",
			"write_u16_be",
			"write_u24_be",
			"write_u32_be",
			"readbyte",
			"writebyte",
			//"registerwrite",
			//"registerread",
		};

		public static string[] MainMemoryFunctions = new string[]
		{
			"read_s8",
			"read_u8",
			"read_s16_le",
			"read_s24_le",
			"read_s32_le",
			"read_u16_le",
			"read_u24_le",
			"read_u32_le",
			"read_s16_be",
			"read_s24_be",
			"read_s32_be",
			"read_u16_be",
			"read_u24_be",
			"read_u32_be",
			"write_s8",
			"write_u8",
			"write_s16_le",
			"write_s24_le",
			"write_s32_le",
			"write_u16_le",
			"write_u24_le",
			"write_u32_le",
			"write_s16_be",
			"write_s24_be",
			"write_s32_be",
			"write_u16_be",
			"write_u24_be",
			"write_u32_be",
			//"registerwrite",
			//"registerread",
		};

		public static string[] SaveStateFunctions = new string[] {
			"saveslot",
			"loadslot",
			"save",
			"load",
			"registersave",
			"registerload",
		};

		public static string[] MovieFunctions = new string[] {
			"mode",
			"isloaded",
			"rerecordcount",
			"length",
			"stop",
			"filename",
			"getreadonly",
			"setreadonly",
			"getrerecordcounting",
			"setrerecordcounting",
			"getinput",
		};

		public static string[] InputFunctions = new string[] {
			"get",
			"getmouse",
		};

		public static string[] JoypadFunctions = new string[] {
			"set",
			"get",
			"getimmediate"
		};

		public static string[] MultiClientFunctions = new string[] {
			"setwindowsize",
			"openrom",
			"closerom",
			"opentoolbox",
			"openramwatch",
			"openramsearch",
			"openrampoke",
			"openhexeditor",
			"opentasstudio",
			"opencheats",
		};

		public static string[] FormsFunctions = new string[] {
				"newform",
				"destroy",
				"destroyall",
				"button",
				"label",
				"textbox",
				"setlocation",
				"setsize",
				"settext",
				"addclick",
				"clearclicks",
				"gettext",
		};

		/****************************************************/
		/*************function definitions********************/
		/****************************************************/

		//----------------------------------------------------
		//Console library
		//----------------------------------------------------

		public void console_output(object lua_input)
		{
			if (lua_input == null)
			{
				Global.MainForm.LuaConsole1.WriteToOutputWindow("NULL");
			}
			else
			{
				Global.MainForm.LuaConsole1.WriteToOutputWindow(lua_input.ToString());
			}
		}

		public void console_log(object lua_input)
		{
			console_output(lua_input);
		}

		public void console_clear()
		{
			Global.MainForm.LuaConsole1.ClearOutputWindow();
		}

		public string console_getluafunctionslist()
		{
			string list = "";
			foreach (LuaDocumentation.LibraryFunction l in Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList)
			{
				list += l.name + "\n";
			}
			
			return list;
		}

		//----------------------------------------------------
		//Gui library
		//----------------------------------------------------
		private void do_gui_text(object luaX, object luaY, object luaStr, bool alert, object background = null, object forecolor = null, object anchor = null)
		{
			if (!alert)
			{
				if (forecolor == null)
					forecolor = "white";
				if (background == null)
					background = "black";
			}
			int a = 0;
			if (anchor != null)
			{
				int x;
				if (int.TryParse(anchor.ToString(), out x) == false)
				{
					if (anchor.ToString().ToLower() == "topleft")
						a = 0;
					else if (anchor.ToString().ToLower() == "topright")
						a = 1;
					else if (anchor.ToString().ToLower() == "bottomleft")
						a = 2;
					else if (anchor.ToString().ToLower() == "bottomright")
						a = 3;
				}
				else
				{
					a = LuaInt(anchor);
				}
			}
			Global.OSD.AddGUIText(luaStr.ToString(), LuaInt(luaX), LuaInt(luaY), alert, GetColor(background), GetColor(forecolor), a);
		}

		public void gui_text(object luaX, object luaY, object luaStr, object background = null, object forecolor = null, object anchor = null)
		{
			do_gui_text(luaX, luaY, luaStr, false, background, forecolor, anchor);
		}

		public void gui_alert(object luaX, object luaY, object luaStr, object anchor = null)
		{
			do_gui_text(luaX, luaY, luaStr, true, null, null, anchor);
		}

		public void gui_cleartext()
		{
			Global.OSD.ClearGUIText();
		}

		public DisplaySurface luaSurface;

		/// <summary>
		/// sets the current drawing context to a new surface.
		/// you COULD pass these back to lua to use as a target in rendering jobs, instead of setting it as current here.
		/// could be more powerful.
		/// performance test may reveal that repeatedly calling GetGraphics could be slow.
		/// we may want to make a class here in LuaImplementation that wraps a DisplaySurface and a Graphics which would be created once
		/// </summary>
		public void gui_drawNew()
		{
			luaSurface = Global.DisplayManager.GetLuaSurfaceNative();
		}

		public void gui_drawNewEmu()
		{
			luaSurface = Global.DisplayManager.GetLuaEmuSurfaceEmu();
		}

		/// <summary>
		/// finishes the current drawing and submits it to the display manager (at native [host] resolution pre-osd)
		/// you would probably want some way to specify which surface to set it to, when there are other surfaces.
		/// most notably, the client output [emulated] resolution 
		/// </summary>
		public void gui_drawFinish()
		{
			Global.DisplayManager.SetLuaSurfaceNativePreOSD(luaSurface);
			luaSurface = null;
		}

		public void gui_drawFinishEmu()
		{
			Global.DisplayManager.SetLuaSurfaceEmu(luaSurface);
			luaSurface = null;
		}

		/// <summary>
		/// draws a random rectangle for testing purposes
		/// </summary>
		public void gui_drawRectangle(object X, object Y, object width, object height, object line, object background = null)
		{
			using (var g = luaSurface.GetGraphics())
			{
				try
				{
					int int_x = LuaInt(X);
					int int_y = LuaInt(Y);
					int int_width = LuaInt(width);
					int int_height = LuaInt(height);
					using (var pen = GetPen(line))
					{
						g.DrawRectangle(pen, int_x, int_y, int_width, int_height);
						if (background != null)
							using (var brush = GetBrush(background))
								g.FillRectangle(brush, int_x, int_y, int_width, int_height);
					}
				}
				catch(Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		public void gui_drawPixel(object X, object Y, object color = null)
		{
			using (var g = luaSurface.GetGraphics())
			{
				float x = LuaInt(X) + 0.1F;
				try
				{
					if (color == null)
						color = "black";
					using(var pen = GetPen(color))
						g.DrawLine(pen, LuaInt(X), LuaInt(Y), x, LuaInt(Y));
				}
				catch (Exception)
				{
					return;
				}
			}
		}
		public void gui_drawLine(object x1, object y1, object x2, object y2, object color = null)
		{
			using (var g = luaSurface.GetGraphics())
			{
				try
				{
					if (color == null)
						color = "black";
					using(var pen = GetPen(color))
						g.DrawLine(pen, LuaInt(x1), LuaInt(y1), LuaInt(x2), LuaInt(y2));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawEllipse(object X, object Y, object width, object height, object line, object background = null)
		{
			using (var g = luaSurface.GetGraphics())
			{
				try
				{
					using (var pen = GetPen(line))
					{
						g.DrawEllipse(pen, LuaInt(X), LuaInt(Y), LuaInt(width), LuaInt(height));
						if (background != null)
						{
							using (var brush = GetBrush(background))
								g.FillEllipse(brush, LuaInt(X), LuaInt(Y), LuaInt(width), LuaInt(height));
						}
					}

				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		public void gui_drawPolygon(LuaTable points, object line, object background = null)
		{
			//this is a test
			using (var g = luaSurface.GetGraphics())
			{
				try
				{
					System.Drawing.Point[] Points = new System.Drawing.Point[points.Values.Count];
					int i = 0;
					foreach (LuaTable point in points.Values)
					{
						Points[i] = new System.Drawing.Point(LuaInt(point[1]), LuaInt(point[2]));
						i++;
					}

					using (var pen = GetPen(line))
					{
						g.DrawPolygon(pen, Points);
						if (background != null)
						{
							using (var brush = GetBrush(background))
								g.FillPolygon(brush, Points);
						}
					}
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawBezier(LuaTable points, object color)
		{
			using (var g = luaSurface.GetGraphics())
			{
				try
				{
					System.Drawing.Point[] Points = new System.Drawing.Point[4];
					int i = 0;
					foreach (LuaTable point in points.Values)
					{
						Points[i] = new System.Drawing.Point(LuaInt(point[1]), LuaInt(point[2]));
						i++;
						if (i >= 4)
							break;
					}
					using(var pen = GetPen(color))
						g.DrawBezier(pen, Points[0], Points[1], Points[2], Points[3]);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawPie(object X, object Y, object width, object height, object startangle, object sweepangle, object line, object background = null)
		{
			using (var g = luaSurface.GetGraphics())
			{
				try
				{
					using(var pen = GetPen(line))
					{
						g.DrawPie(pen, LuaInt(X), LuaInt(Y), LuaInt(width), LuaInt(height), LuaInt(startangle), LuaInt(sweepangle));
						if (background != null)
						{
							using(var brush = GetBrush(background))
								g.FillPie(brush, LuaInt(X), LuaInt(Y), LuaInt(width), LuaInt(height), LuaInt(startangle), LuaInt(sweepangle));
						}
					}

				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		public void gui_drawIcon(object Path, object x, object y, object width = null, object height = null)
		{
			using (var g = luaSurface.GetGraphics())
			{
				try
				{
					Icon icon;
					if (width != null && height != null)
					{
						icon = new Icon(Path.ToString(), LuaInt(width), LuaInt(height));
					}
					else
					{
						icon = new Icon(Path.ToString());
					}
						
					g.DrawIcon(icon, LuaInt(x), LuaInt(y));
				}
				catch(Exception)
				{
					return;
				}
			}
		}

		public void gui_drawImage(object Path, object x, object y, object width = null, object height = null)
		{
			using (var g = luaSurface.GetGraphics())
			{
				try
				{
					Image img = Image.FromFile(Path.ToString());

					if (width == null || width.GetType() != typeof(int))
						width = img.Width.ToString();
					if (height == null || height.GetType() != typeof(int))
						height = img.Height.ToString();

					g.DrawImage(img, LuaInt(x), LuaInt(y), int.Parse(width.ToString()), int.Parse(height.ToString()));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_clearGraphics()
		{
			luaSurface.Clear();
		}

		//----------------------------------------------------
		//Emu library
		//----------------------------------------------------
		public void emu_frameadvance()
		{
			FrameAdvanceRequested = true;
			currThread.Yield(0);
		}

		public void emu_yield()
		{
			currThread.Yield(0);
		}

		public void emu_pause()
		{
			Global.MainForm.PauseEmulator();
		}

		public void emu_unpause()
		{
			Global.MainForm.UnpauseEmulator();
		}

		public void emu_togglepause()
		{
			Global.MainForm.TogglePause();
		}

		public bool emu_ispaused()
		{
			return Global.MainForm.EmulatorPaused;
		}

		public int emu_framecount()
		{
			return Global.Emulator.Frame;
		}

		public int emu_lagcount()
		{
			return Global.Emulator.LagCount;
		}

		public bool emu_islagged()
		{
			return Global.Emulator.IsLagFrame;
		}

		public string emu_getsystemid()
		{
			return Global.Emulator.SystemId;
		}

		public void emu_speedmode(object percent)
		{
			try
			{
				string temp = percent.ToString();
				int speed = Convert.ToInt32(temp);
				if (speed > 0 && speed < 1000) //arbituarily capping it at 1000%
				{
					Global.MainForm.ClickSpeedItem(speed);
				}
				else
				{
					console_log("Invalid speed value");
				}
			}
			catch
			{
				console_log("Invalid speed value");
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
				Global.MainForm.MinimizeFrameskipMessage();
			}
		}

		public void emu_limitframerate(object boolean)
		{
			string temp = boolean.ToString();
			if (!String.IsNullOrWhiteSpace(temp))
			{
				if (temp == "0" || temp.ToLower() == "false")
				{
					Global.Config.LimitFramerate = false;
				}
				else
				{
					Global.Config.LimitFramerate = true;
				}
				Global.MainForm.LimitFrameRateMessage();
			}
		}

		public void emu_displayvsync(object boolean)
		{
			string temp = boolean.ToString();
			if (!String.IsNullOrWhiteSpace(temp))
			{
				if (temp == "0" || temp.ToLower() == "false")
				{
					Global.Config.DisplayVSync = false;
				}
				else
				{
					Global.Config.DisplayVSync = true;
				}
				Global.MainForm.VsyncMessage();
			}
		}

		public void emu_enablerewind(object boolean)
		{
			string temp = boolean.ToString();
			if (!String.IsNullOrWhiteSpace(temp))
			{
				if (temp == "0" || temp.ToLower() == "false")
				{
					Global.Config.RewindEnabled = false;
				}
				else
				{
					Global.Config.RewindEnabled = true;
				}
				Global.MainForm.RewindMessage();
			}
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
					Global.MainForm.FrameSkipMessage();
				}
				else
				{
					console_log("Invalid frame skip value");
				}
			}
			catch
			{
				console_log("Invalid frame skip value");
			}
		}

		// For now, it accepts arguments up to 5.
		public void emu_setrenderplanes(
			object lua_p0, object lua_p1 = null, object lua_p2 = null,
			object lua_p3 = null, object lua_p4 = null)
		{
			emu_setrenderplanes_do(LuaVarArgs(lua_p0, lua_p1, lua_p2, lua_p3, lua_p4));
		}

		// TODO: error handling for argument count mismatch
		private void emu_setrenderplanes_do(object[] lua_p)
		{
			if (Global.Emulator is BizHawk.Emulation.Consoles.Nintendo.NES)
			{
				Global.CoreInputComm.NES_ShowOBJ = Global.Config.NESDispSprites = (bool)lua_p[0];
				Global.CoreInputComm.NES_ShowBG = Global.Config.NESDispBackground = (bool)lua_p[1];
			}
			else if (Global.Emulator is BizHawk.Emulation.Consoles.TurboGrafx.PCEngine)
			{
				Global.CoreInputComm.PCE_ShowOBJ1 = Global.Config.PCEDispOBJ1 = (bool)lua_p[0];
				Global.CoreInputComm.PCE_ShowBG1 = Global.Config.PCEDispBG1 = (bool)lua_p[1];
				if (lua_p.Length > 2)
				{
					Global.CoreInputComm.PCE_ShowOBJ2 = Global.Config.PCEDispOBJ2 = (bool)lua_p[2];
					Global.CoreInputComm.PCE_ShowBG2 = Global.Config.PCEDispBG2 = (bool)lua_p[3];
				}
			}
			else if (Global.Emulator is BizHawk.Emulation.Consoles.Sega.SMS)
			{
				Global.CoreInputComm.SMS_ShowOBJ = Global.Config.SMSDispOBJ = (bool)lua_p[0];
				Global.CoreInputComm.SMS_ShowBG = Global.Config.SMSDispBG = (bool)lua_p[1];
			}
		}

		//----------------------------------------------------
		//Memory library
		//----------------------------------------------------

		public bool memory_usememorydomain(object lua_input)
		{
			if (lua_input.GetType() != typeof(string))
				return false;

			for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
			{
				if (Global.Emulator.MemoryDomains[x].Name == lua_input.ToString())
				{
					CurrentMemoryDomain = x;
					return true;
				}
			}

			return false;
		}

		public string memory_getmemorydomainlist()
		{
			string list = "";
			for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
			{
				list += Global.Emulator.MemoryDomains[x].Name + '\n';
			}
			return list;
		}

		public string memory_getcurrentmemorydomain()
		{
			return Global.Emulator.MemoryDomains[CurrentMemoryDomain].Name;
		}

		public uint memory_readbyte(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U8(addr);
		}

		public void memory_writebyte(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U8(addr, v);
		}

		public int memory_read_s8(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return (sbyte)M_R_U8(addr);
		}

		public uint memory_read_u8(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U8(addr);
		}

		public int memory_read_s16_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_S_LE(addr, 2);
		}

		public int memory_read_s24_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_S_LE(addr, 3);
		}

		public int memory_read_s32_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_S_LE(addr, 4);
		}

		public uint memory_read_u16_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U_LE(addr, 2);
		}

		public uint memory_read_u24_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U_LE(addr, 3);
		}

		public uint memory_read_u32_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U_LE(addr, 4);
		}

		public int memory_read_s16_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_S_BE(addr, 2);
		}

		public int memory_read_s24_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_S_BE(addr, 3);
		}

		public int memory_read_s32_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_S_BE(addr, 4);
		}

		public uint memory_read_u16_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U_BE(addr, 2);
		}

		public uint memory_read_u24_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U_BE(addr, 3);
		}

		public uint memory_read_u32_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U_BE(addr, 4);
		}

		public void memory_write_s8(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_U8(addr, (uint)v);
		}

		public void memory_write_u8(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U8(addr, v);
		}

		public void memory_write_s16_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_S_LE(addr, v, 2);
		}

		public void memory_write_s24_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_S_LE(addr, v, 3);
		}

		public void memory_write_s32_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_S_LE(addr, v, 4);
		}

		public void memory_write_u16_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U_LE(addr, v, 2);
		}

		public void memory_write_u24_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U_LE(addr, v, 3);
		}

		public void memory_write_u32_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U_LE(addr, v, 4);
		}

		public void memory_write_s16_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_S_BE(addr, v, 2);
		}

		public void memory_write_s24_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_S_BE(addr, v, 3);
		}

		public void memory_write_s32_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			M_W_S_BE(addr, v, 4);
		}

		public void memory_write_u16_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U_BE(addr, v, 2);
		}

		public void memory_write_u24_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U_BE(addr, v, 3);
		}

		public void memory_write_u32_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U_BE(addr, v, 4);
		}

		private int M_R_S_LE(int addr, int size)
		{
			return U2S(M_R_U_LE(addr, size), size);
		}

		private uint M_R_U_LE(int addr, int size)
		{
			uint v = 0;
			for (int i = 0; i < size; ++i)
				v |= M_R_U8(addr + i) << 8 * i;
			return v;
		}

		private int M_R_S_BE(int addr, int size)
		{
			return U2S(M_R_U_BE(addr, size), size);
		}

		private uint M_R_U_BE(int addr, int size)
		{
			uint v = 0;
			for (int i = 0; i < size; ++i)
				v |= M_R_U8(addr + i) << 8 * (size - 1 - i);
			return v;
		}

		private void M_W_S_LE(int addr, int v, int size)
		{
			M_W_U_LE(addr, (uint)v, size);
		}

		private void M_W_U_LE(int addr, uint v, int size)
		{
			for (int i = 0; i < size; ++i)
				M_W_U8(addr + i, (v >> (8 * i)) & 0xFF);
		}

		private void M_W_S_BE(int addr, int v, int size)
		{
			M_W_U_BE(addr, (uint)v, size);
		}

		private void M_W_U_BE(int addr, uint v, int size)
		{
			for (int i = 0; i < size; ++i)
				M_W_U8(addr + i, (v >> (8 * (size - 1 - i))) & 0xFF);
		}

		private uint M_R_U8(int addr)
		{
			return Global.Emulator.MemoryDomains[CurrentMemoryDomain].PeekByte(addr);
		}

		private void M_W_U8(int addr, uint v)
		{
			Global.Emulator.MemoryDomains[CurrentMemoryDomain].PokeByte(addr, (byte)v);
		}

		//----------------------------------------------------
		//Main Memory library
		//----------------------------------------------------

		public uint mainmemory_readbyte(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_U8(addr);
		}

		public void mainmemory_writebyte(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			MM_W_U8(addr, v);
		}

		public int mainmemory_read_s8(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return (sbyte)MM_R_U8(addr);
		}

		public uint mainmemory_read_u8(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_U8(addr);
		}

		public int mainmemory_read_s16_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_S_LE(addr, 2);
		}

		public int mainmemory_read_s24_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_S_LE(addr, 3);
		}

		public int mainmemory_read_s32_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_S_LE(addr, 4);
		}

		public uint mainmemory_read_u16_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_U_LE(addr, 2);
		}

		public uint mainmemory_read_u24_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_U_LE(addr, 3);
		}

		public uint mainmemory_read_u32_le(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_U_LE(addr, 4);
		}

		public int mainmemory_read_s16_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_S_BE(addr, 2);
		}

		public int mainmemory_read_s24_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_S_BE(addr, 3);
		}

		public int mainmemory_read_s32_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_S_BE(addr, 4);
		}

		public uint mainmemory_read_u16_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_U_BE(addr, 2);
		}

		public uint mainmemory_read_u24_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_U_BE(addr, 3);
		}

		public uint mainmemory_read_u32_be(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_U_BE(addr, 4);
		}

		public void mainmemory_write_s8(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			MM_W_U8(addr, (uint)v);
		}

		public void mainmemory_write_u8(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			MM_W_U8(addr, v);
		}

		public void mainmemory_write_s16_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			MM_W_S_LE(addr, v, 2);
		}

		public void mainmemory_write_s24_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			MM_W_S_LE(addr, v, 3);
		}

		public void mainmemory_write_s32_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			MM_W_S_LE(addr, v, 4);
		}

		public void mainmemory_write_u16_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			MM_W_U_LE(addr, v, 2);
		}

		public void mainmemory_write_u24_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			MM_W_U_LE(addr, v, 3);
		}

		public void mainmemory_write_u32_le(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			MM_W_U_LE(addr, v, 4);
		}

		public void mainmemory_write_s16_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			MM_W_S_BE(addr, v, 2);
		}

		public void mainmemory_write_s24_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			MM_W_S_BE(addr, v, 3);
		}

		public void mainmemory_write_s32_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			int v = LuaInt(lua_v);
			MM_W_S_BE(addr, v, 4);
		}

		public void mainmemory_write_u16_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			MM_W_U_BE(addr, v, 2);
		}

		public void mainmemory_write_u24_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			MM_W_U_BE(addr, v, 3);
		}

		public void mainmemory_write_u32_be(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			MM_W_U_BE(addr, v, 4);
		}

		private int MM_R_S_LE(int addr, int size)
		{
			return U2S(MM_R_U_LE(addr, size), size);
		}

		private uint MM_R_U_LE(int addr, int size)
		{
			uint v = 0;
			for (int i = 0; i < size; ++i)
				v |= MM_R_U8(addr + i) << 8 * i;
			return v;
		}

		private int MM_R_S_BE(int addr, int size)
		{
			return U2S(MM_R_U_BE(addr, size), size);
		}

		private uint MM_R_U_BE(int addr, int size)
		{
			uint v = 0;
			for (int i = 0; i < size; ++i)
				v |= MM_R_U8(addr + i) << 8 * (size - 1 - i);
			return v;
		}

		private void MM_W_S_LE(int addr, int v, int size)
		{
			MM_W_U_LE(addr, (uint)v, size);
		}

		private void MM_W_U_LE(int addr, uint v, int size)
		{
			for (int i = 0; i < size; ++i)
				MM_W_U8(addr + i, (v >> (8 * i)) & 0xFF);
		}

		private void MM_W_S_BE(int addr, int v, int size)
		{
			MM_W_U_BE(addr, (uint)v, size);
		}

		private void MM_W_U_BE(int addr, uint v, int size)
		{
			for (int i = 0; i < size; ++i)
				MM_W_U8(addr + i, (v >> (8 * (size - 1 - i))) & 0xFF);
		}

		private uint MM_R_U8(int addr)
		{
			return Global.Emulator.MainMemory.PeekByte(addr);
		}

		private void MM_W_U8(int addr, uint v)
		{
			Global.Emulator.MainMemory.PokeByte(addr, (byte)v);
		}

		private int U2S(uint u, int size)
		{
			int s = (int)u;
			s <<= 8 * (4 - size);
			s >>= 8 * (4 - size);
			return s;
		}

		//----------------------------------------------------
		//Savestate library
		//----------------------------------------------------
		public void savestate_saveslot(object lua_input)
		{
			int x = 0;

			try //adelikat:  This crap might not be necessary, need to test for a more elegant solution
			{
				x = int.Parse(lua_input.ToString());
			}
			catch
			{
				return;
			}

			if (x < 0 || x > 9)
				return;

			Global.MainForm.SaveState("QuickSave" + x.ToString());
		}

		public void savestate_loadslot(object lua_input)
		{
			int x = 0;

			try //adelikat:  This crap might not be necessary, need to test for a more elegant solution
			{
				x = int.Parse(lua_input.ToString());
			}
			catch
			{
				return;
			}

			if (x < 0 || x > 9)
				return;

			Global.MainForm.LoadState("QuickLoad" + x.ToString());
		}

		public void savestate_save(object lua_input)
		{
			if (lua_input.GetType() == typeof(string))
			{
				string path = lua_input.ToString();
				var writer = new StreamWriter(path);
				Global.MainForm.SaveStateFile(writer, path, true);
			}
		}

		public void savestate_load(object lua_input)
		{
			if (lua_input.GetType() == typeof(string))
			{
				Global.MainForm.LoadStateFile(lua_input.ToString(), Path.GetFileName(lua_input.ToString()));
			}
		}

		public void savestate_registersave(LuaFunction luaf)
		{
			savestate_registersavefunc = luaf;
		}

		public void savestate_registerload(LuaFunction luaf)
		{
			savestate_registerloadfunc = luaf;
		}

		//----------------------------------------------------
		//Movie library
		//----------------------------------------------------
		public string movie_mode()
		{
			return Global.MovieSession.Movie.Mode.ToString();
		}

		public string movie_rerecordcount()
		{
			return Global.MovieSession.Movie.Rerecords.ToString();
		}

		public void movie_stop()
		{
			Global.MovieSession.Movie.StopMovie();
		}

		public bool movie_isloaded()
		{
			if (Global.MovieSession.Movie.Mode == MOVIEMODE.INACTIVE)
				return false;
			else
				return true;
		}

		public int movie_length()
		{
			return Global.MovieSession.Movie.LogLength();
		}

		public string movie_filename()
		{
			return Global.MovieSession.Movie.Filename;
		}

		public bool movie_getreadonly()
		{
			return Global.MainForm.ReadOnly;
		}

		public void movie_setreadonly(object lua_input)
		{
			if (lua_input.ToString().ToUpper() == "TRUE" || lua_input.ToString() == "1")
				Global.MainForm.SetReadOnly(true);
			else
				Global.MainForm.SetReadOnly(false);
		}

		public LuaTable movie_getinput(object frame)
		{
			LuaTable input = lua.NewTable();

			string s = Global.MovieSession.Movie.GetInputFrame(LuaInt(frame));
			MovieControllerAdapter m = new MovieControllerAdapter();
			m.Type = Global.MovieSession.MovieControllerAdapter.Type;
			m.SetControllersAsMnemonic(s);
			foreach (string button in m.Type.BoolButtons)
				input[button] = m[button];

			return input;
		}

		public bool movie_getrerecordcounting()
		{
			return Global.MovieSession.Movie.RerecordCounting;
		}

		public void movie_setrerecordcounting(object lua_input)
		{
			if (lua_input.ToString().ToUpper() == "TRUE" || lua_input.ToString() == "1")
				Global.MovieSession.Movie.RerecordCounting = true;
			else
				Global.MovieSession.Movie.RerecordCounting = false;
		}
		//----------------------------------------------------
		//Input library
		//----------------------------------------------------
		public LuaTable input_get()
		{
			LuaTable buttons = lua.NewTable();
			foreach (var kvp in Global.ControllerInputCoalescer.BoolButtons())
				if (kvp.Value)
					buttons[kvp.Key] = true;
			return buttons;
		}

		//----------------------------------------------------
		//Joypad library
		//----------------------------------------------------

		//Currently sends all controllers, needs to control which ones it sends
		public LuaTable joypad_get(object controller = null)
		{
			LuaTable buttons = lua.NewTable();
			foreach (string button in Global.ControllerOutput.Source.Type.BoolButtons)
				if (controller == null)
					buttons[button] = Global.ControllerOutput[button];
				else if (button.Length >= 3 && button.Substring(0, 2) == "P" + LuaInt(controller).ToString())
					buttons[button.Substring(3)] = Global.ControllerOutput["P" + LuaInt(controller) + " " + button.Substring(3)];

			buttons["clear"] = null;
			buttons["getluafunctionslist"] = null;
			buttons["output"] = null;

			return buttons;
		}

		public LuaTable joypad_getimmediate()
		{
			LuaTable buttons = lua.NewTable();
			foreach (string button in Global.ActiveController.Type.BoolButtons)
				buttons[button] = Global.ActiveController[button];
			return buttons;
		}

		public void joypad_set(LuaTable buttons, object controller = null)
		{
            foreach (var button in buttons.Keys)
            {
                if (Convert.ToBoolean(buttons[button]) == true)
					if (controller == null)
                        Global.ClickyVirtualPadController.Click(button.ToString());
                    else
						Global.ClickyVirtualPadController.Click("P" + controller.ToString() + " " + button.ToString());
            }
		}

		//----------------------------------------------------
		//Client library
		//----------------------------------------------------
		public void client_openrom(object lua_input)
		{
			Global.MainForm.LoadRom(lua_input.ToString());
		}

		public void client_closerom()
		{
			Global.MainForm.CloseROM();
		}

		public void client_opentoolbox()
		{
			Global.MainForm.LoadToolBox();
		}

		public void client_openramwatch()
		{
			Global.MainForm.LoadRamWatch(true);
		}

		public void client_openramsearch()
		{
			Global.MainForm.LoadRamSearch();
		}

		public void client_openrampoke()
		{
			Global.MainForm.LoadRamPoke();
		}

		public void client_openhexeditor()
		{
			Global.MainForm.LoadHexEditor();
		}

		public void client_opentasstudio()
		{
			Global.MainForm.LoadTAStudio();
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
					Global.MainForm.FrameBufferResized();
					Global.OSD.AddMessage("Window size set to " + size.ToString() + "x");
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

		public void client_opencheats()
		{
			Global.MainForm.LoadCheatsWindow();
		}

		//----------------------------------------------------
		//Winforms library
		//----------------------------------------------------
		public List<LuaWinform> LuaForms = new List<LuaWinform>();

		public int forms_newform(object Width = null, object Height = null, object title = null)
		{
			
			LuaWinform theForm = new LuaWinform();
			LuaForms.Add(theForm);
			if (Width != null && Height != null)
			{
				theForm.Size = new Size(LuaInt(Width), LuaInt(Height));
			}

			if (title != null)
			{
				theForm.Text = title.ToString();
			}

			theForm.Show();
			return (int)theForm.Handle;
		}

		public void WindowClosed(IntPtr handle)
		{
			foreach (LuaWinform form in LuaForms)
			{
				if (form.Handle == handle)
				{
					LuaForms.Remove(form);
					return;
				}
			}
		}

		public bool forms_destroy(object handle)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in LuaForms)
			{
				if (form.Handle == ptr)
				{
					form.Close();
					LuaForms.Remove(form);
					return true;
				}
			}
			return false;
		}

		public void forms_destroyall()
		{
			foreach (LuaWinform form in LuaForms)
			{
				form.Close();
				LuaForms.Remove(form);
			}
		}

		private LuaWinform GetForm(object form_handle)
		{
			IntPtr ptr = new IntPtr(LuaInt(form_handle));
			foreach (LuaWinform form in LuaForms)
			{
				if (form.Handle == ptr)
				{
					return form;
				}
			}
			return null;
		}

		private void SetLocation(Control control, object X, object Y)
		{
			try
			{
				if (X != null && Y != null)
				{
					int x = LuaInt(X);
					int y = LuaInt(Y);
					control.Location = new Point(x, y);
				}
			}
			catch
			{
				//Do nothing
			}
		}

		private void SetSize(Control control, object Width, object Height)
		{
			try
			{
				if (Width != null && Height != null)
				{
					int width = LuaInt(Width);
					int height = LuaInt(Height);
					control.Size = new Size(width, height);
				}
			}
			catch
			{
				//Do nothing
			}
		}

		private void SetText(Control control, object caption)
		{
			if (caption != null)
			{
				control.Text = caption.ToString();
			}
		}

		public int forms_button(object form_handle, object caption, LuaFunction lua_event, object X = null, object Y = null, object width = null, object height = null)
		{
			LuaWinform form = GetForm(form_handle);
			if (form == null)
			{
				return 0;
			}
			
			LuaButton button = new LuaButton();
			SetText(button, caption);
			form.Controls.Add(button);
			form.Control_Events.Add(new LuaWinform.Lua_Event(button.Handle, lua_event));

			if (X != null && Y != null)
				SetLocation(button, X, Y);

			if (width != null & height != null)
				SetSize(button, width, height);

			return (int)button.Handle;
		}

		public int forms_label(object form_handle, object caption, object X = null, object Y = null, object width = null, object height = null)
		{
			LuaWinform form = GetForm(form_handle);
			if (form == null)
			{
				return 0;
			}

			Label label = new Label();
			SetText(label, caption);
			form.Controls.Add(label);
			if (X != null && Y != null)
				SetLocation(label, X, Y);

			if (width != null & height != null)
				SetSize(label, width, height);

			return (int)label.Handle;
		}

		public int forms_textbox(object form_handle, object caption = null, object width = null, object height = null, object boxtype = null, object X = null, object Y = null)
		{
			LuaWinform form = GetForm(form_handle);
			if (form == null)
			{
				return 0;
			}

			LuaTextBox textbox = new LuaTextBox();
			SetText(textbox, caption);

            if(X != null && Y != null)
			    SetLocation(textbox, X, Y);

            if (width != null & height != null)
                SetSize(textbox, width, height);

			if (boxtype != null)
			{
				switch (boxtype.ToString().ToUpper())
				{
					case "HEX":
					case "HEXADECIMAL":
						textbox.SetType(BoxType.HEX);
						break;
					case "UNSIGNED":
					case "UINT":
						textbox.SetType(BoxType.UNSIGNED);
						break;
					case "NUMBER":
					case "NUM":
					case "SIGNED":
					case "INT":
						textbox.SetType(BoxType.SIGNED);
						break;
				}
			}
			form.Controls.Add(textbox);
			return (int)textbox.Handle;
		}

		public void forms_setlocation(object handle, object X, object Y)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in LuaForms)
			{
				if (form.Handle == ptr)
				{
					SetLocation(form, X, Y);
				}
				else
				{
					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							SetLocation(control, X, Y);
						}
					}
				}
			}
		}

		public void forms_setsize(object handle, object Width, object Height)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in LuaForms)
			{
				if (form.Handle == ptr)
				{
					SetSize(form, Width, Height);
				}
				else
				{
					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							SetSize(control, Width, Height);
						}
					}
				}
			}
		}

		public void forms_settext(object handle, object caption)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in LuaForms)
			{
				if (form.Handle == ptr)
				{
					SetText(form, caption);
				}
				else
				{
					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							SetText(control, caption);
						}
					}
				}
			}
		}

        public void forms_setproperty(object handle, object property, object value)
        {
            IntPtr ptr = new IntPtr(LuaInt(handle));
            foreach (LuaWinform form in LuaForms)
            {
                if (form.Handle == ptr)
                {
                    form.GetType().GetProperty(property.ToString()).SetValue(form, value, null);
                }
                else
                {
                    foreach (Control control in form.Controls)
                    {
                        if (control.Handle == ptr)
                        {
                            control.GetType().GetProperty(property.ToString()).SetValue(control, value, null);
                        }
                    }
                }
            }
        }

		public void forms_addclick(object handle, LuaFunction lua_event)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in LuaForms)
			{
				foreach (Control control in form.Controls)
				{
					if (control.Handle == ptr)
					{
						form.Control_Events.Add(new LuaWinform.Lua_Event(control.Handle, lua_event));
					}
				}
			}
		}

		public void forms_clearclicks(object handle)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in LuaForms)
			{
				foreach (Control control in form.Controls)
				{
					if (control.Handle == ptr)
					{
						List<LuaWinform.Lua_Event> lua_events = form.Control_Events.Where(x => x.Control == ptr).ToList();
						foreach (LuaWinform.Lua_Event levent in lua_events)
						{
							form.Control_Events.Remove(levent);
						}
					}
				}
			}
		}

		public string forms_gettext(object handle)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in LuaForms)
			{
				if (form.Handle == ptr)
				{
					return form.Text;
				}
				else
				{
					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							return control.Text;
						}
					}
				}
			}

			return "";
		}

        public string forms_getproperty(object handle, object property)
        {
            IntPtr ptr = new IntPtr(LuaInt(handle));
            foreach (LuaWinform form in LuaForms)
            {
                if (form.Handle == ptr)
                {
                    return form.GetType().GetProperty(property.ToString()).GetValue(form, null).ToString();
                }
                else
                {
                    foreach (Control control in form.Controls)
                    {
                        if (control.Handle == ptr)
                        {
                            return control.GetType().GetProperty(property.ToString()).GetValue(control, null).ToString();
                        }
                    }
                }
            }

            return "";
        }

		public LuaTable input_getmouse()
		{
			LuaTable buttons = lua.NewTable();
			buttons["X"] = Control.MousePosition.X;
			buttons["Y"] = Control.MousePosition.Y;
			buttons[MouseButtons.Left.ToString()] = Control.MouseButtons & MouseButtons.Left;
			buttons[MouseButtons.Middle.ToString()] = Control.MouseButtons & MouseButtons.Middle;
			buttons[MouseButtons.Right.ToString()] = Control.MouseButtons & MouseButtons.Right;
			buttons[MouseButtons.XButton1.ToString()] = Control.MouseButtons & MouseButtons.XButton1;
			buttons[MouseButtons.XButton2.ToString()] = Control.MouseButtons & MouseButtons.XButton2;
			return buttons;

		}
	}
}
