using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using LuaInterface;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Globalization;

using BizHawk.Client.Common;
using BizHawk.Emulation.Consoles.Nintendo;
using System.Text;

using BizHawk.MultiClient.tools; //TODO: remove me, this is not an intended namespace

namespace BizHawk.MultiClient
{
	public class LuaImplementation
	{
		private Lua _lua = new Lua();
		private readonly LuaConsole Caller;
		private int CurrentMemoryDomain; //Main memory by default
		private Lua currThread;

		public LuaDocumentation docs = new LuaDocumentation();
		public bool isRunning;
		public EventWaitHandle LuaWait;
		public bool FrameAdvanceRequested;

		public LuaImplementation(LuaConsole passed)
		{
			LuaWait = new AutoResetEvent(false);
			docs.Clear();
			Caller = passed.get();
			LuaRegister(_lua);
		}

		public void Close()
		{
			_lua = new Lua();
			foreach (var brush in SolidBrushes.Values) brush.Dispose();
			foreach (var brush in Pens.Values) brush.Dispose();
		}

		#region Initialize Library Definitions

		public static string[] BitwiseFunctions = new[]
		{
			"band",
			"bnot",
			"bor",
			"bxor",
			"lshift",
			"rol",
			"ror",
			"rshift",
		};

		public static string[] MultiClientFunctions = new[]
		{
			"closerom",
			"getwindowsize",
			"opencheats",
			"openhexeditor",
			"openramwatch",
			"openramsearch",
			"openrom",
			"opentasstudio",
			"opentoolbox",
			"opentracelogger",
			"pause_av",
			"reboot_core",
			"screenheight",
			"screenshot",
			"screenshottoclipboard",
			"screenwidth",
			"setscreenshotosd",
			"setwindowsize",
			"unpause_av",
			"xpos",
			"ypos",
		};

		public static string[] ConsoleFunctions = new[]
		{
			"clear",
			"getluafunctionslist",
			"log",
			"output",
		};

		public static string[] GuiFunctions = new[]
		{
			"addmessage",
			"alert",
			"cleartext",
			"drawBezier",
			"drawBox",
			"drawEllipse",
			"drawIcon",
			"drawImage",
			"drawLine",
			"drawPie",
			"drawPixel",
			"drawPolygon",
			"drawRectangle",
			"drawString",
			"drawText",
			"text",
		};

		public static string[] EmuFunctions = new[]
		{
			"displayvsync",
			"enablerewind",
			"frameadvance",
			"framecount",
			"frameskip",
			"getsystemid",
			"islagged",
			"ispaused",		
			"lagcount",
			"limitframerate",
			"minimizeframeskip",
			"on_snoop",
			"pause",
			"setrenderplanes",
			"speedmode",
			"togglepause",
			"unpause",
			"yield",
		};

		public static string[] EventFunctions = new[]
		{
			"onframeend",
			"onframestart",
			"oninputpoll",
			"onloadstate",
			"onmemoryread",
			"onmemorywrite",
			"onsavestate",
			"unregisterbyid",
			"unregisterbyname",
		};

		public static string[] FormsFunctions = new[]
		{
			"addclick",
			"button",
			"clearclicks",
			"destroy",
			"destroyall",
			"getproperty",
			"gettext",
			"label",
			"newform",
			"openfile",
			"setlocation",
			"setproperty",
			"setsize",
			"settext",
			"textbox",
		};

		public static string[] InputFunctions = new[]
		{
			"get",
			"getmouse"
		};

		public static string[] JoypadFunctions = new[]
		{
			"get",
			"getimmediate",
			"set",
			"setanalog"
		};

		public static string[] MainMemoryFunctions = new[]
		{
			"getname",
			"readbyte",
			"readbyterange",
			"readfloat",
			"writebyte",
			"writebyterange",
			"writefloat",

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
		};

		public static string[] MemoryFunctions = new[]
		{
			"getcurrentmemorydomain",
			"getcurrentmemorydomainsize",
			"getmemorydomainlist",
			"readbyte",
			"readfloat",
			"usememorydomain",
			"writebyte",
			"writefloat",

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
		};

		public static string[] MovieFunctions = new[]
		{
			"filename",
			"getinput",
			"getreadonly",
			"getrerecordcounting",
			"isloaded",
			"length",
			"mode",
			"rerecordcount",
			"setreadonly",
			"setrerecordcounting",
			"stop",
		};

		public static string[] NESFunctions = new[]
		{
			"addgamegenie",
			"getallowmorethaneightsprites",
			"getbottomscanline",
			"getclipleftandright",
			"getdispbackground",
			"getdispsprites",
			"gettopscanline",
			"removegamegenie",
			"setallowmorethaneightsprites",
			"setclipleftandright",
			"setdispbackground",
			"setdispsprites",
			"setscanlines",
		};

		public static string[] SaveStateFunctions = new[]
		{
			"load",
			"loadslot",
			"registerload",
			"registersave",
			"save",
			"saveslot",
		};

		public static string[] SNESFunctions = new[]
		{
			"getlayer_bg_1",
			"getlayer_bg_2",
			"getlayer_bg_3",
			"getlayer_bg_4",
			"getlayer_obj_1",
			"getlayer_obj_2",
			"getlayer_obj_3",
			"getlayer_obj_4",
			"setlayer_bg_1",
			"setlayer_bg_2",
			"setlayer_bg_3",
			"setlayer_bg_4",
			"setlayer_obj_1",
			"setlayer_obj_2",
			"setlayer_obj_3",
			"setlayer_obj_4",
		};

		public void LuaRegister(Lua lua)
		{
			lua.RegisterFunction("print", this, GetType().GetMethod("print"));

			//Register libraries
			lua.NewTable("console");
			foreach (string t in ConsoleFunctions)
			{
				lua.RegisterFunction("console." + t, this,
									 GetType().GetMethod("console_" + t));
				docs.Add("console", t, GetType().GetMethod("console_" + t));
			}

			lua.NewTable("gui");
			foreach (string t in GuiFunctions)
			{
				lua.RegisterFunction("gui." + t, this, GetType().GetMethod("gui_" + t));
				docs.Add("gui", t, GetType().GetMethod("gui_" + t));
			}

			lua.NewTable("emu");
			foreach (string t in EmuFunctions)
			{
				lua.RegisterFunction("emu." + t, this, GetType().GetMethod("emu_" + t));
				docs.Add("emu", t, GetType().GetMethod("emu_" + t));
			}

			lua.NewTable("memory");
			foreach (string t in MemoryFunctions)
			{
				lua.RegisterFunction("memory." + t, this, GetType().GetMethod("memory_" + t));
				docs.Add("memory", t, GetType().GetMethod("memory_" + t));
			}

			lua.NewTable("mainmemory");
			foreach (string t in MainMemoryFunctions)
			{
				lua.RegisterFunction("mainmemory." + t, this,
									 GetType().GetMethod("mainmemory_" + t));
				docs.Add("mainmemory", t, GetType().GetMethod("mainmemory_" + t));
			}

			lua.NewTable("savestate");
			foreach (string t in SaveStateFunctions)
			{
				lua.RegisterFunction("savestate." + t, this,
									 GetType().GetMethod("savestate_" + t));
				docs.Add("savestate", t, GetType().GetMethod("savestate_" + t));
			}

			lua.NewTable("movie");
			foreach (string t in MovieFunctions)
			{
				lua.RegisterFunction("movie." + t, this, GetType().GetMethod("movie_" + t));
				docs.Add("movie", t, GetType().GetMethod("movie_" + t));
			}

			lua.NewTable("input");
			foreach (string t in InputFunctions)
			{
				lua.RegisterFunction("input." + t, this, GetType().GetMethod("input_" + t));
				docs.Add("input", t, GetType().GetMethod("input_" + t));
			}

			lua.NewTable("joypad");
			foreach (string t in JoypadFunctions)
			{
				lua.RegisterFunction("joypad." + t, this, GetType().GetMethod("joypad_" + t));
				docs.Add("joypad", t, GetType().GetMethod("joypad_" + t));
			}

			lua.NewTable("client");
			foreach (string t in MultiClientFunctions)
			{
				lua.RegisterFunction("client." + t, this,
									 GetType().GetMethod("client_" + t));
				docs.Add("client", t, GetType().GetMethod("client_" + t));
			}

			lua.NewTable("forms");
			foreach (string t in FormsFunctions)
			{
				lua.RegisterFunction("forms." + t, this, GetType().GetMethod("forms_" + t));
				docs.Add("forms", t, GetType().GetMethod("forms_" + t));
			}

			lua.NewTable("bit");
			foreach (string t in BitwiseFunctions)
			{
				lua.RegisterFunction("bit." + t, this, GetType().GetMethod("bit_" + t));
				docs.Add("bit", t, GetType().GetMethod("bit_" + t));
			}

			lua.NewTable("nes");
			foreach (string t in NESFunctions)
			{
				lua.RegisterFunction("nes." + t, this, GetType().GetMethod("nes_" + t));
				docs.Add("nes", t, GetType().GetMethod("nes_" + t));
			}

			lua.NewTable("snes");
			foreach (string t in SNESFunctions)
			{
				lua.RegisterFunction("snes." + t, this, GetType().GetMethod("snes_" + t));
				docs.Add("snes", t, GetType().GetMethod("snes_" + t));
			}

			lua.NewTable("event");
			foreach (string t in EventFunctions)
			{
				lua.RegisterFunction("event." + t, this, GetType().GetMethod("event_" + t));
				docs.Add("event", t, GetType().GetMethod("event_" + t));
			}

			docs.Sort();
		}

		#endregion

		#region Libraries

		#region General Helpers

		public Lua SpawnCoroutine(string File)
		{
			var t = _lua.NewThread();
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

		/// <summary>
		/// LuaInterface requires the exact match of parameter count, except optional parameters. 
		/// So, if you want to support variable arguments, declare them as optional and pass
		/// them to this method.
		/// </summary>
		/// <param name="lua_args"></param>
		/// <returns></returns>
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

		#endregion

		#region Bitwise Library

		public uint bit_band(object val, object amt)
		{
			return (uint)(LuaInt(val) & LuaInt(amt));
		}

		public uint bit_bnot(object val)
		{
			return (uint)(~LuaInt(val));
		}

		public uint bit_bor(object val, object amt)
		{
			return (uint)(LuaInt(val) | LuaInt(amt));
		}

		public uint bit_bxor(object val, object amt)
		{
			return (uint)(LuaInt(val) ^ LuaInt(amt));
		}

		public uint bit_lshift(object val, object amt)
		{
			return (uint)(LuaInt(val) << LuaInt(amt));
		}

		public uint bit_rol(object val, object amt)
		{
			return (uint)((LuaInt(val) << LuaInt(amt)) | (LuaInt(val) >> (32 - LuaInt(amt))));
		}

		public uint bit_ror(object val, object amt)
		{
			return (uint)((LuaInt(val) >> LuaInt(amt)) | (LuaInt(val) << (32 - LuaInt(amt))));
		}

		public uint bit_rshift(object val, object amt)
		{
			return (uint)(LuaInt(val) >> LuaInt(amt));
		}

		#endregion

		#region Client Library

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

		#endregion

		#region Console Library

		public void console_clear()
		{
			GlobalWinF.MainForm.LuaConsole1.ClearOutputWindow();
		}

		public string console_getluafunctionslist()
		{
			string list = "";
			foreach (LuaDocumentation.LibraryFunction l in GlobalWinF.MainForm.LuaConsole1.LuaImp.docs.FunctionList)
			{
				list += l.name + "\n";
			}

			return list;
		}

		public void console_log(object lua_input)
		{
			console_output(lua_input);
		}

		public void console_output(object lua_input)
		{
			if (lua_input == null)
			{
				GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow("NULL");
			}
			else
			{
				if (lua_input is LuaTable)
				{
					StringBuilder sb = new StringBuilder();
					var lti = (lua_input as LuaTable);

					List<string> Keys = new List<string>();
					List<string> Values = new List<string>();
					foreach (var key in lti.Keys) { Keys.Add(key.ToString()); }
					foreach (var value in lti.Values) { Values.Add(value.ToString()); }

					List<KeyValuePair<string, string>> KVPs = new List<KeyValuePair<string, string>>();
					for (int i = 0; i < Keys.Count; i++)
					{
						if (i < Values.Count)
						{
							KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(Keys[i], Values[i]);
							KVPs.Add(kvp);
						}
					}
					KVPs = KVPs.OrderBy(x => x.Key).ToList();
					foreach (var kvp in KVPs)
					{
						sb
							.Append("\"")
							.Append(kvp.Key)
							.Append("\": \"")
							.Append(kvp.Value)
							.Append("\"")
							.AppendLine();
					}

					GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(sb.ToString());
				}
				else
				{
					GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(lua_input.ToString());
				}
			}
		}

		#endregion

		#region Gui Library

		#region Gui Library Helpers

		private readonly Dictionary<Color, SolidBrush> SolidBrushes = new Dictionary<Color, SolidBrush>();
		private readonly Dictionary<Color, Pen> Pens = new Dictionary<Color, Pen>();

		public Color GetColor(object color)
		{
			if (color is double)
			{
				return Color.FromArgb(int.Parse(long.Parse(color.ToString()).ToString("X"), NumberStyles.HexNumber));
			}
			else
			{
				return Color.FromName(color.ToString().ToLower());
			}
		}

		public SolidBrush GetBrush(object color)
		{
			Color c = GetColor(color);
			SolidBrush b;
			if (!SolidBrushes.TryGetValue(c, out b))
			{
				b = new SolidBrush(c);
				SolidBrushes[c] = b;
			}
			return b;
		}

		public Pen GetPen(object color)
		{
			Color c = GetColor(color);
			Pen p;
			if (!Pens.TryGetValue(c, out p))
			{
				p = new Pen(c);
				Pens[c] = p;
			}
			return p;
		}

		public void gui_clearGraphics()
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			luaSurface.Clear();
		}

		/// <summary>
		/// sets the current drawing context to a new surface.
		/// you COULD pass these back to lua to use as a target in rendering jobs, instead of setting it as current here.
		/// could be more powerful.
		/// performance test may reveal that repeatedly calling GetGraphics could be slow.
		/// we may want to make a class here in LuaImplementation that wraps a DisplaySurface and a Graphics which would be created once
		/// </summary>
		public void gui_drawNew()
		{
			luaSurface = GlobalWinF.DisplayManager.GetLuaSurfaceNative();
		}

		public void gui_drawNewEmu()
		{
			luaSurface = GlobalWinF.DisplayManager.GetLuaEmuSurfaceEmu();
		}

		/// <summary>
		/// finishes the current drawing and submits it to the display manager (at native [host] resolution pre-osd)
		/// you would probably want some way to specify which surface to set it to, when there are other surfaces.
		/// most notably, the client output [emulated] resolution 
		/// </summary>
		public void gui_drawFinish()
		{
			GlobalWinF.DisplayManager.SetLuaSurfaceNativePreOSD(luaSurface);
			luaSurface = null;
		}

		public void gui_drawFinishEmu()
		{
			GlobalWinF.DisplayManager.SetLuaSurfaceEmu(luaSurface);
			luaSurface = null;
		}

		Graphics GetGraphics()
		{
			var g = luaSurface.GetGraphics();
			int tx = GlobalWinF.Emulator.CoreComm.ScreenLogicalOffsetX;
			int ty = GlobalWinF.Emulator.CoreComm.ScreenLogicalOffsetY;
			if (tx != 0 || ty != 0)
			{
				var transform = g.Transform;
				transform.Translate(-tx, -ty);
				g.Transform = transform;
			}
			return g;
		}

		public DisplaySurface luaSurface;

		private void do_gui_text(object luaX, object luaY, object luaStr, bool alert, object background = null,
								 object forecolor = null, object anchor = null)
		{
			if (!alert)
			{
				if (forecolor == null)
					forecolor = "white";
				if (background == null)
					background = "black";
			}
			int dx = LuaInt(luaX);
			int dy = LuaInt(luaY);
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
			else
			{
				dx -= GlobalWinF.Emulator.CoreComm.ScreenLogicalOffsetX;
				dy -= GlobalWinF.Emulator.CoreComm.ScreenLogicalOffsetY;
			}
			// blah hacks
			dx *= client_getwindowsize();
			dy *= client_getwindowsize();

			GlobalWinF.OSD.AddGUIText(luaStr.ToString(), dx, dy, alert, GetColor(background), GetColor(forecolor), a);
		}

		#endregion

		public void gui_addmessage(object luaStr)
		{
			GlobalWinF.OSD.AddMessage(luaStr.ToString());
		}

		public void gui_alert(object luaX, object luaY, object luaStr, object anchor = null)
		{
			do_gui_text(luaX, luaY, luaStr, true, null, null, anchor);
		}

		public void gui_cleartext()
		{
			GlobalWinF.OSD.ClearGUIText();
		}

		public void gui_drawBezier(LuaTable points, object color)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					Point[] Points = new Point[4];
					int i = 0;
					foreach (LuaTable point in points.Values)
					{
						Points[i] = new Point(LuaInt(point[1]), LuaInt(point[2]));
						i++;
						if (i >= 4)
							break;
					}

					g.DrawBezier(GetPen(color), Points[0], Points[1], Points[2], Points[3]);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawBox(object X, object Y, object X2, object Y2, object line = null, object background = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					int int_x = LuaInt(X);
					int int_y = LuaInt(Y);
					int int_width = LuaInt(X2);
					int int_height = LuaInt(Y2);

					if (int_x < int_width)
					{
						int_width = Math.Abs(int_x - int_width);
					}
					else
					{
						int_width = int_x - int_width;
						int_x -= int_width;
					}

					if (int_y < int_height)
					{
						int_height = Math.Abs(int_y - int_height);
					}
					else
					{
						int_height = int_y - int_height;
						int_y -= int_height;
					}

					g.DrawRectangle(GetPen(line ?? "white"), int_x, int_y, int_width, int_height);
					if (background != null)
					{
						g.FillRectangle(GetBrush(background), int_x, int_y, int_width, int_height);
					}
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		public void gui_drawEllipse(object X, object Y, object width, object height, object line, object background = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawEllipse(GetPen(line ?? "white"), LuaInt(X), LuaInt(Y), LuaInt(width), LuaInt(height));
					if (background != null)
					{
						var brush = GetBrush(background);
						g.FillEllipse(brush, LuaInt(X), LuaInt(Y), LuaInt(width), LuaInt(height));
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
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
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
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawImage(object Path, object x, object y, object width = null, object height = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
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

		public void gui_drawLine(object x1, object y1, object x2, object y2, object color = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawLine(GetPen(color ?? "white"), LuaInt(x1), LuaInt(y1), LuaInt(x2), LuaInt(y2));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawPie(object X, object Y, object width, object height, object startangle, object sweepangle,
								object line, object background = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawPie(GetPen(line), LuaInt(X), LuaInt(Y), LuaInt(width), LuaInt(height), LuaInt(startangle), LuaInt(sweepangle));
					if (background != null)
					{
						var brush = GetBrush(background);
						g.FillPie(brush, LuaInt(X), LuaInt(Y), LuaInt(width), LuaInt(height), LuaInt(startangle), LuaInt(sweepangle));
					}
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}


		public void gui_drawPixel(object X, object Y, object color = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				float x = LuaInt(X) + 0.1F;
				try
				{
					g.DrawLine(GetPen(color ?? "white"), LuaInt(X), LuaInt(Y), x, LuaInt(Y));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawPolygon(LuaTable points, object line, object background = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			//this is a test
			using (var g = GetGraphics())
			{
				try
				{
					Point[] Points = new Point[points.Values.Count];
					int i = 0;
					foreach (LuaTable point in points.Values)
					{
						Points[i] = new Point(LuaInt(point[1]), LuaInt(point[2]));
						i++;
					}

					g.DrawPolygon(GetPen(line), Points);
					if (background != null)
					{
						var brush = GetBrush(background);
						g.FillPolygon(brush, Points);
					}
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawRectangle(object X, object Y, object width, object height, object line, object background = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					int int_x = LuaInt(X);
					int int_y = LuaInt(Y);
					int int_width = LuaInt(width);
					int int_height = LuaInt(height);
					g.DrawRectangle(GetPen(line ?? "white"), int_x, int_y, int_width, int_height);
					if (background != null)
					{
						g.FillRectangle(GetBrush(background), int_x, int_y, int_width, int_height);
					}
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		
		public void gui_drawString(object X, object Y, object message, object color = null, object fontsize = null, object fontfamily = null, object fontstyle = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			gui_drawText(X, Y, message, color, fontsize, fontfamily, fontstyle);
		}

		public void gui_drawText(object X, object Y, object message, object color = null, object fontsize = null, object fontfamily = null, object fontstyle = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					int fsize = 12;
					if (fontsize != null)
					{
						fsize = LuaInt(fontsize);
					}

					FontFamily family = FontFamily.GenericMonospace;
					if (fontfamily != null)
					{
						family = new FontFamily(fontfamily.ToString());
					}

					FontStyle fstyle = FontStyle.Regular;
					if (fontstyle != null)
					{
						string tmp = fontstyle.ToString().ToLower();
						switch (tmp)
						{
							default:
							case "regular":
								break;
							case "bold":
								fstyle = FontStyle.Bold;
								break;
							case "italic":
								fstyle = FontStyle.Italic;
								break;
							case "strikethrough":
								fstyle = FontStyle.Strikeout;
								break;
							case "underline":
								fstyle = FontStyle.Underline;
								break;
						}
					}

					Font font = new Font(family, fsize, fstyle, GraphicsUnit.Pixel);
					g.DrawString(message.ToString(), font, GetBrush(color ?? "white"), LuaInt(X), LuaInt(Y));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_text(object luaX, object luaY, object luaStr, object background = null, object forecolor = null,
							 object anchor = null)
		{
			do_gui_text(luaX, luaY, luaStr, false, background, forecolor, anchor);
		}

		#endregion

		#region Emu Library

		#region Emu Library Helpers

		// TODO: error handling for argument count mismatch
		private void emu_setrenderplanes_do(object[] lua_p)
		{
			if (GlobalWinF.Emulator is NES)
			{
				GlobalWinF.CoreComm.NES_ShowOBJ = Global.Config.NESDispSprites = (bool)lua_p[0];
				GlobalWinF.CoreComm.NES_ShowBG = Global.Config.NESDispBackground = (bool)lua_p[1];
			}
			else if (GlobalWinF.Emulator is Emulation.Consoles.TurboGrafx.PCEngine)
			{
				GlobalWinF.CoreComm.PCE_ShowOBJ1 = Global.Config.PCEDispOBJ1 = (bool)lua_p[0];
				GlobalWinF.CoreComm.PCE_ShowBG1 = Global.Config.PCEDispBG1 = (bool)lua_p[1];
				if (lua_p.Length > 2)
				{
					GlobalWinF.CoreComm.PCE_ShowOBJ2 = Global.Config.PCEDispOBJ2 = (bool)lua_p[2];
					GlobalWinF.CoreComm.PCE_ShowBG2 = Global.Config.PCEDispBG2 = (bool)lua_p[3];
				}
			}
			else if (GlobalWinF.Emulator is Emulation.Consoles.Sega.SMS)
			{
				GlobalWinF.CoreComm.SMS_ShowOBJ = Global.Config.SMSDispOBJ = (bool)lua_p[0];
				GlobalWinF.CoreComm.SMS_ShowBG = Global.Config.SMSDispBG = (bool)lua_p[1];
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
			return GlobalWinF.Emulator.Frame;
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
					console_log("Invalid frame skip value");
				}
			}
			catch
			{
				console_log("Invalid frame skip value");
			}
		}

		public string emu_getsystemid()
		{
			return GlobalWinF.Emulator.SystemId;
		}

		public bool emu_islagged()
		{
			return GlobalWinF.Emulator.IsLagFrame;
		}

		public bool emu_ispaused()
		{
			return GlobalWinF.MainForm.EmulatorPaused;
		}

		public int emu_lagcount()
		{
			return GlobalWinF.Emulator.LagCount;
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
				GlobalWinF.Emulator.CoreComm.InputCallback = delegate()
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
				GlobalWinF.Emulator.CoreComm.InputCallback = null;
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
					console_log("Invalid speed value");
				}
			}
			catch
			{
				console_log("Invalid speed value");
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

		#endregion

		#region Events Library

		#region Events Library Helpers

		private LuaFunctionCollection lua_functions = new LuaFunctionCollection();

		public List<NamedLuaFunction> RegisteredFunctions { get { return lua_functions.Functions; } }

		public void SavestateRegisterSave(string name)
		{
			List<NamedLuaFunction> lfs = lua_functions.Where(x => x.Event == "OnSavestateSave").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (NamedLuaFunction lf in lfs)
					{
						lf.Call(name);
					}
				}
				catch (SystemException e)
				{
					GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
						"error running function attached by lua function savestate.registersave" +
						"\nError message: " + e.Message);
				}
			}
		}

		public void SavestateRegisterLoad(string name)
		{
			List<NamedLuaFunction> lfs = lua_functions.Where(x => x.Event == "OnSavestateLoad").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (NamedLuaFunction lf in lfs)
					{
						lf.Call(name);
					}
				}
				catch (SystemException e)
				{
					GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
						"error running function attached by lua function savestate.registerload" +
						"\nError message: " + e.Message);
				}
			}
		}

		public void FrameRegisterBefore()
		{
			List<NamedLuaFunction> lfs = lua_functions.Where(x => x.Event == "OnFrameStart").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (NamedLuaFunction lf in lfs)
					{
						lf.Call();
					}
				}
				catch (SystemException e)
				{
					GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
						"error running function attached by lua function emu.registerbefore" +
						"\nError message: " + e.Message);
				}
			}
		}

		public void FrameRegisterAfter()
		{
			List<NamedLuaFunction> lfs = lua_functions.Where(x => x.Event == "OnFrameEnd").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (NamedLuaFunction lf in lfs)
					{
						lf.Call();
					}
				}
				catch (SystemException e)
				{
					GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
						"error running function attached by lua function emu.registerafter" +
						"\nError message: " + e.Message);
				}
			}
		}

		#endregion

		public string event_onframeend(LuaFunction luaf, string name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnFrameEnd", name != null ? name.ToString() : null);
			lua_functions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public string event_onframestart(LuaFunction luaf, object name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnFrameStart", name != null ? name.ToString() : null);
			lua_functions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public void event_oninputpoll(LuaFunction luaf)
		{
			emu_on_snoop(luaf);
		}

		public string event_onloadstate(LuaFunction luaf, object name = null)
		{
			return savestate_registerload(luaf, name);
		}

		public void event_onmemoryread(LuaFunction luaf, object address = null)
		{
			//TODO: allow a list of addresses
			if (luaf != null)
			{
				int? _addr;
				if (address == null)
				{
					_addr = null;
				}
				else
				{
					_addr = LuaInt(address);
				}

				GlobalWinF.Emulator.CoreComm.MemoryCallbackSystem.ReadAddr = _addr;
				GlobalWinF.Emulator.CoreComm.MemoryCallbackSystem.SetReadCallback(delegate(uint addr)
				{
					try
					{
						luaf.Call(addr);
					}
					catch (SystemException e)
					{
						GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
							"error running function attached by lua function event.onmemoryread" +
							"\nError message: " + e.Message);
					}
				});

			}
			else
			{
				GlobalWinF.Emulator.CoreComm.MemoryCallbackSystem.SetReadCallback(null);
			}
		}

		public void event_onmemorywrite(LuaFunction luaf, object address = null)
		{
			//TODO: allow a list of addresses
			if (luaf != null)
			{
				int? _addr;
				if (address == null)
				{
					_addr = null;
				}
				else
				{
					_addr = LuaInt(address);
				}

				GlobalWinF.Emulator.CoreComm.MemoryCallbackSystem.WriteAddr = _addr;
				GlobalWinF.Emulator.CoreComm.MemoryCallbackSystem.SetWriteCallback(delegate(uint addr)
				{
					try
					{
						luaf.Call(addr);
					}
					catch (SystemException e)
					{
						GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
							"error running function attached by lua function event.onmemoryread" +
							"\nError message: " + e.Message);
					}
				});
			}
			else
			{
				GlobalWinF.Emulator.CoreComm.MemoryCallbackSystem.SetWriteCallback(null);
			}
		}

		public string event_onsavestate(LuaFunction luaf, object name = null)
		{
			return savestate_registersave(luaf, name);
		}

		public bool event_unregisterbyid(object guid)
		{
			//Iterating every possible event type is not very scalable
			foreach (NamedLuaFunction nlf in lua_functions)
			{
				if (nlf.GUID.ToString() == guid.ToString())
				{
					lua_functions.Remove(nlf);
					return true;
				}
			}

			return false;
		}

		public bool event_unregisterbyname(object name)
		{
			//Horribly redundant to the function above!
			foreach (NamedLuaFunction nlf in lua_functions)
			{
				if (nlf.Name == name.ToString())
				{
					lua_functions.Remove(nlf);
					return true;
				}
			}

			return false;
		}

		#endregion

		#region Forms Library

		#region Forms Library Helpers

		private List<LuaWinform> LuaForms = new List<LuaWinform>();

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

		private LuaWinform GetForm(object form_handle)
		{
			IntPtr ptr = new IntPtr(LuaInt(form_handle));
			return LuaForms.FirstOrDefault(form => form.Handle == ptr);
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

		#endregion

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

		public string forms_getproperty(object handle, object property)
		{
			try
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
			}
			catch (Exception ex)
			{
				console_output(ex.Message);
			}

			return "";
		}

		public string forms_gettext(object handle)
		{
			try
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
			}
			catch (Exception ex)
			{
				console_output(ex.Message);
			}

			return "";
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

		public string forms_openfile(string FileName = null, string InitialDirectory = null, string Filter = "All files (*.*)|*.*")
		{
			// filterext format ex: "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*"
			OpenFileDialog openFileDialog1 = new OpenFileDialog();
			if (InitialDirectory != null)
			{
				openFileDialog1.InitialDirectory = InitialDirectory;
			}
			if (FileName != null)
			{
				openFileDialog1.FileName = FileName;
			}
			if (Filter != null)
			{
				openFileDialog1.AddExtension = true;
				openFileDialog1.Filter = Filter;
			}
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
				return openFileDialog1.FileName;
			else
				return "";
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

		public void forms_setproperty(object handle, object property, object value)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in LuaForms)
			{
				if (form.Handle == ptr)
				{
					form.GetType().GetProperty(property.ToString()).SetValue(form, Convert.ChangeType(value, form.GetType().GetProperty(property.ToString()).PropertyType), null);
				}
				else
				{
					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							control.GetType().GetProperty(property.ToString()).SetValue(control, Convert.ChangeType(value, form.GetType().GetProperty(property.ToString()).PropertyType), null);
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

		public int forms_textbox(object form_handle, object caption = null, object width = null, object height = null, object boxtype = null, object X = null, object Y = null)
		{
			LuaWinform form = GetForm(form_handle);
			if (form == null)
			{
				return 0;
			}

			LuaTextBox textbox = new LuaTextBox();
			SetText(textbox, caption);

			if (X != null && Y != null)
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

		#endregion

		#region Input Library

		public LuaTable input_get()
		{
			LuaTable buttons = _lua.NewTable();
			foreach (var kvp in GlobalWinF.ControllerInputCoalescer.BoolButtons())
				if (kvp.Value)
					buttons[kvp.Key] = true;
			return buttons;
		}

		public LuaTable input_getmouse()
		{
			LuaTable buttons = _lua.NewTable();
			Point p = GlobalWinF.RenderPanel.ScreenToScreen(Control.MousePosition);
			buttons["X"] = p.X;
			buttons["Y"] = p.Y;
			buttons[MouseButtons.Left.ToString()] = (Control.MouseButtons & MouseButtons.Left) != 0;
			buttons[MouseButtons.Middle.ToString()] = (Control.MouseButtons & MouseButtons.Middle) != 0;
			buttons[MouseButtons.Right.ToString()] = (Control.MouseButtons & MouseButtons.Right) != 0;
			buttons[MouseButtons.XButton1.ToString()] = (Control.MouseButtons & MouseButtons.XButton1) != 0;
			buttons[MouseButtons.XButton2.ToString()] = (Control.MouseButtons & MouseButtons.XButton2) != 0;
			return buttons;
		}

		#endregion

		#region Joypad Library

		//Currently sends all controllers, needs to control which ones it sends
		public LuaTable joypad_get(object controller = null)
		{
			LuaTable buttons = _lua.NewTable();
			foreach (string button in GlobalWinF.ControllerOutput.Source.Type.BoolButtons)
			{
				if (controller == null)
				{
					buttons[button] = GlobalWinF.ControllerOutput[button];
				}
				else if (button.Length >= 3 && button.Substring(0, 2) == "P" + LuaInt(controller).ToString())
				{
					buttons[button.Substring(3)] = GlobalWinF.ControllerOutput["P" + LuaInt(controller) + " " + button.Substring(3)];
				}
			}

			foreach (string button in GlobalWinF.ControllerOutput.Source.Type.FloatControls)
			{
				if (controller == null)
				{
					buttons[button] = GlobalWinF.ControllerOutput.GetFloat(button);
				}
				else if (button.Length >= 3 && button.Substring(0, 2) == "P" + LuaInt(controller).ToString())
				{
					buttons[button.Substring(3)] = GlobalWinF.ControllerOutput.GetFloat("P" + LuaInt(controller) + " " + button.Substring(3));
				}
			}

			buttons["clear"] = null;
			buttons["getluafunctionslist"] = null;
			buttons["output"] = null;

			return buttons;
		}

		public LuaTable joypad_getimmediate()
		{
			LuaTable buttons = _lua.NewTable();
			foreach (string button in GlobalWinF.ActiveController.Type.BoolButtons)
				buttons[button] = GlobalWinF.ActiveController[button];
			return buttons;
		}

		public void joypad_set(LuaTable buttons, object controller = null)
		{
			try
			{
				foreach (var button in buttons.Keys)
				{
					bool invert = false;
					bool? theValue;
					string theValueStr = buttons[button].ToString();

					if (!String.IsNullOrWhiteSpace(theValueStr))
					{
						if (theValueStr.ToLower() == "false")
						{
							theValue = false;
						}
						else if (theValueStr.ToLower() == "true")
						{
							theValue = true;
						}
						else
						{
							invert = true;
							theValue = null;
						}
					}
					else
					{
						theValue = null;
					}


					if (!invert)
					{
						if (theValue == true)
						{
							if (controller == null) //Force On
							{
								GlobalWinF.ClickyVirtualPadController.Click(button.ToString());
								GlobalWinF.ForceOffAdaptor.SetSticky(button.ToString(), false);
							}
							else
							{
								GlobalWinF.ClickyVirtualPadController.Click("P" + controller + " " + button);
								GlobalWinF.ForceOffAdaptor.SetSticky("P" + controller + " " + button, false);
							}
						}
						else if (theValue == false) //Force off
						{
							if (controller == null)
							{
								GlobalWinF.ForceOffAdaptor.SetSticky(button.ToString(), true);
							}
							else
							{
								GlobalWinF.ForceOffAdaptor.SetSticky("P" + controller + " " + button, true);
							}
						}
						else
						{
							//Turn everything off
							if (controller == null)
							{
								GlobalWinF.ForceOffAdaptor.SetSticky(button.ToString(), false);
							}
							else
							{
								GlobalWinF.ForceOffAdaptor.SetSticky("P" + controller + " " + button, false);
							}
						}
					}
					else //Inverse
					{
						if (controller == null)
						{
							GlobalWinF.StickyXORAdapter.SetSticky(button.ToString(), true);
							GlobalWinF.ForceOffAdaptor.SetSticky(button.ToString(), false);
						}
						else
						{
							GlobalWinF.StickyXORAdapter.SetSticky("P" + controller + " " + button, true);
							GlobalWinF.ForceOffAdaptor.SetSticky("P" + controller + " " + button, false);
						}
					}
				}
			}
			catch { /*Eat it*/ }
		}

		public void joypad_setanalog(LuaTable controls, object controller = null)
		{
			try
			{
				foreach (var name in controls.Keys)
				{
					string theValueStr = controls[name].ToString();

					if (!String.IsNullOrWhiteSpace(theValueStr))
					{
						try
						{
							float theValue = float.Parse(theValueStr);
							if (controller == null)
							{
								GlobalWinF.StickyXORAdapter.SetFloat(name.ToString(), theValue);
							}
							else
							{
								GlobalWinF.StickyXORAdapter.SetFloat("P" + controller + " " + name, theValue);
							}
						}
						catch { }
					}
				}
			}
			catch { /*Eat it*/ }
		}

		#endregion

		#region Main Memory Library

		#region Main Memory Library Helpers

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
			return GlobalWinF.Emulator.MainMemory.PeekByte(addr);
		}

		private void MM_W_U8(int addr, uint v)
		{
			GlobalWinF.Emulator.MainMemory.PokeByte(addr, (byte)v);
		}

		private int U2S(uint u, int size)
		{
			int s = (int)u;
			s <<= 8 * (4 - size);
			s >>= 8 * (4 - size);
			return s;
		}

		#endregion

		public string mainmemory_getname()
		{
			return GlobalWinF.Emulator.MainMemory.Name;
		}

		public uint mainmemory_readbyte(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return MM_R_U8(addr);
		}

		public LuaTable mainmemory_readbyterange(object address, object length)
		{
			int l = LuaInt(length);
			int addr = LuaInt(address);
			int last_addr = l + addr;
			LuaTable table = _lua.NewTable();
			for (int i = addr; i <= last_addr; i++)
			{
				string a = String.Format("{0:X2}", i);
				byte v = GlobalWinF.Emulator.MainMemory.PeekByte(i);
				string vs = String.Format("{0:X2}", (int)v);
				table[a] = vs;
			}
			return table;
		}

		public float mainmemory_readfloat(object lua_addr, bool bigendian)
		{
			int addr = LuaInt(lua_addr);
			uint val = GlobalWinF.Emulator.MainMemory.PeekDWord(addr, bigendian ? Endian.Big : Endian.Little);

			byte[] bytes = BitConverter.GetBytes(val);
			float _float = BitConverter.ToSingle(bytes, 0);
			return _float;
		}

		public void mainmemory_writebyte(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			MM_W_U8(addr, v);
		}

		public void mainmemory_writebyterange(LuaTable memoryblock)
		{
			foreach (var address in memoryblock.Keys)
			{
				int a = LuaInt(address);
				int v = LuaInt(memoryblock[address]);

				GlobalWinF.Emulator.MainMemory.PokeByte(a, (byte)v);
			}
		}

		public void mainmemory_writefloat(object lua_addr, object lua_v, bool bigendian)
		{
			int addr = LuaInt(lua_addr);
			float dv = (float)(double)lua_v;
			byte[] bytes = BitConverter.GetBytes(dv);
			uint v = BitConverter.ToUInt32(bytes, 0);
			GlobalWinF.Emulator.MainMemory.PokeDWord(addr, v, bigendian ? Endian.Big : Endian.Little);
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

		#endregion

		#region Memory Library

		#region Memory Library Helpers

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
			return GlobalWinF.Emulator.MemoryDomains[CurrentMemoryDomain].PeekByte(addr);
		}

		private void M_W_U8(int addr, uint v)
		{
			GlobalWinF.Emulator.MemoryDomains[CurrentMemoryDomain].PokeByte(addr, (byte)v);
		}

		#endregion

		public string memory_getmemorydomainlist()
		{
			return GlobalWinF.Emulator.MemoryDomains.Aggregate("", (current, t) => current + (t.Name + '\n'));
		}

		public string memory_getcurrentmemorydomain()
		{
			return GlobalWinF.Emulator.MemoryDomains[CurrentMemoryDomain].Name;
		}

		public int memory_getcurrentmemorydomainsize()
		{
			return GlobalWinF.Emulator.MemoryDomains[CurrentMemoryDomain].Size;
		}

		public uint memory_readbyte(object lua_addr)
		{
			int addr = LuaInt(lua_addr);
			return M_R_U8(addr);
		}

		public float memory_readfloat(object lua_addr, bool bigendian)
		{
			int addr = LuaInt(lua_addr);
			uint val = GlobalWinF.Emulator.MemoryDomains[CurrentMemoryDomain].PeekDWord(addr, bigendian ? Endian.Big : Endian.Little);

			byte[] bytes = BitConverter.GetBytes(val);
			float _float = BitConverter.ToSingle(bytes, 0);
			return _float;
		}

		public void memory_writebyte(object lua_addr, object lua_v)
		{
			int addr = LuaInt(lua_addr);
			uint v = LuaUInt(lua_v);
			M_W_U8(addr, v);
		}

		public void memory_writefloat(object lua_addr, object lua_v, bool bigendian)
		{
			int addr = LuaInt(lua_addr);
			float dv = (float)(double)lua_v;
			byte[] bytes = BitConverter.GetBytes(dv);
			uint v = BitConverter.ToUInt32(bytes, 0);
			GlobalWinF.Emulator.MemoryDomains[CurrentMemoryDomain].PokeDWord(addr, v, bigendian ? Endian.Big : Endian.Little);
		}

		public bool memory_usememorydomain(object lua_input)
		{
			if (lua_input.GetType() != typeof(string))
				return false;

			for (int x = 0; x < GlobalWinF.Emulator.MemoryDomains.Count; x++)
			{
				if (GlobalWinF.Emulator.MemoryDomains[x].Name == lua_input.ToString())
				{
					CurrentMemoryDomain = x;
					return true;
				}
			}

			return false;
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

		#endregion

		#region Movie Library

		public string movie_filename()
		{
			return GlobalWinF.MovieSession.Movie.Filename;
		}

		public LuaTable movie_getinput(object frame)
		{
			LuaTable input = _lua.NewTable();

			string s = GlobalWinF.MovieSession.Movie.GetInput(LuaInt(frame));
			MovieControllerAdapter m = new MovieControllerAdapter { Type = GlobalWinF.MovieSession.MovieControllerAdapter.Type };
			m.SetControllersAsMnemonic(s);
			foreach (string button in m.Type.BoolButtons)
				input[button] = m[button];

			return input;
		}

		public bool movie_getreadonly()
		{
			return GlobalWinF.MainForm.ReadOnly;
		}

		public bool movie_getrerecordcounting()
		{
			return GlobalWinF.MovieSession.Movie.IsCountingRerecords;
		}

		public bool movie_isloaded()
		{
			if (GlobalWinF.MovieSession.Movie.IsActive)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public int movie_length()
		{
			if (GlobalWinF.MovieSession.Movie.Frames.HasValue)
			{
				return GlobalWinF.MovieSession.Movie.Frames.Value;
			}
			else
			{
				return -1;
			}
		}

		public string movie_mode()
		{
			if (GlobalWinF.MovieSession.Movie.IsFinished)
			{
				return "FINISHED";
			}
			else if (GlobalWinF.MovieSession.Movie.IsPlaying)
			{
				return "PLAY";
			}
			else if (GlobalWinF.MovieSession.Movie.IsRecording)
			{
				return "RECORD";
			}
			else
			{
				return "INACTIVE";
			}
		}

		public string movie_rerecordcount()
		{
			return GlobalWinF.MovieSession.Movie.Rerecords.ToString();
		}

		public void movie_setreadonly(object lua_input)
		{
			if (lua_input.ToString().ToUpper() == "TRUE" || lua_input.ToString() == "1")
				GlobalWinF.MainForm.SetReadOnly(true);
			else
				GlobalWinF.MainForm.SetReadOnly(false);
		}

		public void movie_setrerecordcounting(object lua_input)
		{
			if (lua_input.ToString().ToUpper() == "TRUE" || lua_input.ToString() == "1")
				GlobalWinF.MovieSession.Movie.IsCountingRerecords = true;
			else
				GlobalWinF.MovieSession.Movie.IsCountingRerecords = false;
		}

		public void movie_stop()
		{
			GlobalWinF.MovieSession.Movie.Stop();
		}

		#endregion

		#region NES Library

		public void nes_addgamegenie(string code)
		{
			if (GlobalWinF.Emulator is NES)
			{
				NESGameGenie gg = new NESGameGenie();
				gg.DecodeGameGenieCode(code);
				if (gg.Address.HasValue && gg.Value.HasValue)
				{
					Watch watch = Watch.GenerateWatch(
						GlobalWinF.Emulator.MemoryDomains[1],
						gg.Address.Value,
						Watch.WatchSize.Byte,
						Watch.DisplayType.Hex,
						code,
						false);

					GlobalWinF.CheatList.Add(new Cheat(
						watch,
						gg.Value.Value,
						gg.Compare,
						enabled: true));
				}
			}
		}

		public bool nes_getallowmorethaneightsprites()
		{
			return Global.Config.NESAllowMoreThanEightSprites;
		}

		public int nes_getbottomscanline(bool pal = false)
		{
			if (pal)
			{
				return Global.Config.PAL_NESBottomLine;
			}
			else
			{
				return Global.Config.NTSC_NESBottomLine;
			}
		}

		public bool nes_getclipleftandright()
		{
			return Global.Config.NESClipLeftAndRight;
		}

		public bool nes_getdispbackground()
		{
			return Global.Config.NESDispBackground;
		}

		public bool nes_getdispsprites()
		{
			return Global.Config.NESDispSprites;
		}

		public int nes_gettopscanline(bool pal = false)
		{
			if (pal)
			{
				return Global.Config.PAL_NESTopLine;
			}
			else
			{
				return Global.Config.NTSC_NESTopLine;
			}
		}

		public void nes_removegamegenie(string code)
		{
			if (GlobalWinF.Emulator is NES)
			{
				NESGameGenie gg = new NESGameGenie();
				gg.DecodeGameGenieCode(code);
				if (gg.Address.HasValue && gg.Value.HasValue)
				{
					var cheats = GlobalWinF.CheatList.Where(x => x.Address == gg.Address);
					GlobalWinF.CheatList.RemoveRange(cheats);
				}
			}
		}

		public void nes_setallowmorethaneightsprites(bool allow)
		{
			Global.Config.NESAllowMoreThanEightSprites = allow;
			if (GlobalWinF.Emulator is NES)
			{
				(GlobalWinF.Emulator as NES).CoreComm.NES_UnlimitedSprites = allow;
			}
		}

		public void nes_setclipleftandright(bool leftandright)
		{
			Global.Config.NESClipLeftAndRight = leftandright;
			if (GlobalWinF.Emulator is NES)
			{
				(GlobalWinF.Emulator as NES).SetClipLeftAndRight(leftandright);
			}
		}

		public void nes_setdispbackground(bool show)
		{
			Global.Config.NESDispBackground = show;
			GlobalWinF.MainForm.SyncCoreCommInputSignals();
		}

		public void nes_setdispsprites(bool show)
		{
			Global.Config.NESDispSprites = show;
			GlobalWinF.MainForm.SyncCoreCommInputSignals();
		}

		public void nes_setscanlines(object top, object bottom, bool pal = false)
		{

			int first = LuaInt(top);
			int last = LuaInt(bottom);
			if (first > 127)
			{
				first = 127;
			}
			else if (first < 0)
			{
				first = 0;
			}

			if (last > 239)
			{
				last = 239;
			}
			else if (last < 128)
			{
				last = 128;
			}

			if (pal)
			{
				Global.Config.PAL_NESTopLine = first;
				Global.Config.PAL_NESBottomLine = last;
			}
			else
			{
				Global.Config.NTSC_NESTopLine = first;
				Global.Config.NTSC_NESBottomLine = last;
			}

			if (GlobalWinF.Emulator is NES)
			{
				if (pal)
				{
					(GlobalWinF.Emulator as NES).PAL_FirstDrawLine = first;
					(GlobalWinF.Emulator as NES).PAL_LastDrawLine = last;
				}
				else
				{
					(GlobalWinF.Emulator as NES).NTSC_FirstDrawLine = first;
					(GlobalWinF.Emulator as NES).NTSC_LastDrawLine = last;
				}
			}
		}

		#endregion

		#region Savestate Library

		public void savestate_load(object lua_input)
		{
			if (lua_input is string)
			{
				GlobalWinF.MainForm.LoadStateFile(lua_input.ToString(), Path.GetFileName(lua_input.ToString()), true);
			}
		}

		public void savestate_loadslot(object lua_input)
		{
			int x;

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

			GlobalWinF.MainForm.LoadState("QuickSave" + x.ToString(), true);
		}

		public string savestate_registerload(LuaFunction luaf, object name)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnSavestateLoad", name != null ? name.ToString() : null);
			lua_functions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public string savestate_registersave(LuaFunction luaf, object name)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnSavestateSave", name != null ? name.ToString() : null);
			lua_functions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public void savestate_save(object lua_input)
		{
			if (lua_input is string)
			{
				string path = lua_input.ToString();
				GlobalWinF.MainForm.SaveStateFile(path, path, true);
			}
		}

		public void savestate_saveslot(object lua_input)
		{
			int x;

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

			GlobalWinF.MainForm.SaveState("QuickSave" + x.ToString());
		}

		#endregion

		#region SNES Library

		public bool snes_getlayer_bg_1()
		{
			return Global.Config.SNES_ShowBG1_1;
		}

		public bool snes_getlayer_bg_2()
		{
			return Global.Config.SNES_ShowBG2_1;
		}

		public bool snes_getlayer_bg_3()
		{
			return Global.Config.SNES_ShowBG3_1;
		}

		public bool snes_getlayer_bg_4()
		{
			return Global.Config.SNES_ShowBG4_1;
		}

		public bool snes_getlayer_obj_1()
		{
			return Global.Config.SNES_ShowOBJ1;
		}

		public bool snes_getlayer_obj_2()
		{
			return Global.Config.SNES_ShowOBJ2;
		}

		public bool snes_getlayer_obj_3()
		{
			return Global.Config.SNES_ShowOBJ3;
		}

		public bool snes_getlayer_obj_4()
		{
			return Global.Config.SNES_ShowOBJ4;
		}

		public void snes_setlayer_bg_1(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleBG1(value);
		}

		public void snes_setlayer_bg_2(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleBG2(value);
		}

		public void snes_setlayer_bg_3(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleBG3(value);
		}

		public void snes_setlayer_bg_4(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleBG4(value);
		}

		public void snes_setlayer_obj_1(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleOBJ1(value);
		}

		public void snes_setlayer_obj_2(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleOBJ2(value);
		}

		public void snes_setlayer_obj_3(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleOBJ3(value);
		}

		public void snes_setlayer_obj_4(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleOBJ4(value);
		}

		#endregion

		#endregion
	}

	#region Classes

	public class NamedLuaFunction
	{
		private LuaFunction _function;
		private string _name;
		private string _event;

		public Guid GUID { get; private set; }

		public NamedLuaFunction(LuaFunction function, string theevent, string name = null)
		{
			_function = function;
			_name = name ?? "Anonymous Function";
			_event = theevent;
			GUID = Guid.NewGuid();
		}

		public void Call(string name = null)
		{
			_function.Call(name);
		}

		public string Name
		{
			get
			{
				return _name;
			}
		}

		public string Event
		{
			get
			{
				return _event;
			}
		}
	}

	public class LuaFunctionCollection : IEnumerable<NamedLuaFunction>
	{
		public List<NamedLuaFunction> Functions { get; private set; }

		public LuaFunctionCollection()
		{
			Functions = new List<NamedLuaFunction>();
		}

		public void Add(NamedLuaFunction nlf)
		{
			Functions.Add(nlf);
		}

		public void Remove(NamedLuaFunction nlf)
		{
			Functions.Remove(nlf);
		}

		public IEnumerator<NamedLuaFunction> GetEnumerator()
		{
			return Functions.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public NamedLuaFunction this[int index]
		{
			get
			{
				return Functions[index];
			}
		}

		public NamedLuaFunction this[string guid]
		{
			get
			{
				return Functions.FirstOrDefault(x => x.GUID.ToString() == guid) ?? null;
			}
		}
	}

	#endregion
}
