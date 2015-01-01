using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ComponentModel;

using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Common.ReflectionExtensions;

using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public class ToolManager
	{
		// TODO: merge ToolHelper code where logical
		// For instance, add an IToolForm property called UsesCheats, so that a UpdateCheatRelatedTools() method can update all tools of this type
		// Also a UsesRam, and similar method
		private readonly List<IToolForm> _tools = new List<IToolForm>();

		/// <summary>
		/// Loads the tool dialog T, if it does not exist it will be created, if it is already open, it will be focused
		/// </summary>
		public T Load<T>(bool focus = true) where T : IToolForm
		{
			return (T)Load(typeof(T), focus);
		}

		/// <summary>
		/// Loads a tool dialog of type toolType if it does not exist it will be
		/// created, if it is already open, it will be focused.
		/// </summary>
		public IToolForm Load(Type toolType, bool focus = true)
		{
			if (!typeof(IToolForm).IsAssignableFrom(toolType))
				throw new ArgumentException(String.Format("Type {0} does not implement IToolForm.", toolType.Name));

			if (!ServiceInjector.IsAvailable(Global.Emulator.ServiceProvider, toolType))
				return null;

			var existingTool = _tools.FirstOrDefault(x => toolType.IsAssignableFrom(x.GetType()));

			if (existingTool != null)
			{
				if (existingTool.IsDisposed)
				{
					_tools.Remove(existingTool);
				}
				else
				{
					if (focus)
					{
						existingTool.Show();
						existingTool.Focus();
					}
					return existingTool;
				}
			}

			var newTool = CreateInstance(toolType);

			ServiceInjector.UpdateServices(Global.Emulator.ServiceProvider, newTool);

			// auto settings
			if (newTool is IToolFormAutoConfig)
			{
				ToolDialogSettings settings;
				if (!Global.Config.CommonToolSettings.TryGetValue(toolType.ToString(), out settings))
				{
					settings = new ToolDialogSettings();
					Global.Config.CommonToolSettings[toolType.ToString()] = settings;
				}
				AttachSettingHooks(newTool as IToolFormAutoConfig, settings);
			}

			// custom settings
			if (HasCustomConfig(newTool))
			{
				Dictionary<string, object> settings;
				if (!Global.Config.CustomToolSettings.TryGetValue(toolType.ToString(), out settings))
				{
					settings = new Dictionary<string, object>();
					Global.Config.CustomToolSettings[toolType.ToString()] = settings;
				}
				InstallCustomConfig(newTool, settings);
			}

			newTool.Restart();
			newTool.Show();
			return newTool;
		}

		public void AutoLoad()
		{
			foreach (var typename in Global.Config.CommonToolSettings.Where(kvp => kvp.Value.AutoLoad).Select(kvp => kvp.Key))
			{
				// this type resolution might not be sufficient.  more investigation is needed
				Type t = Type.GetType(typename);
				if (t == null)
				{
					Console.WriteLine("BENIGN: Couldn't find type {0}", typename);
				}
				else
				{
					Load(t, false);
				}
			}
		}

		private static void RefreshSettings(Form form, ToolStripItemCollection menu, ToolDialogSettings settings, int idx)
		{
			(menu[idx + 0] as ToolStripMenuItem).Checked = settings.SaveWindowPosition;
			(menu[idx + 1] as ToolStripMenuItem).Checked = settings.TopMost;
			(menu[idx + 2] as ToolStripMenuItem).Checked = settings.FloatingWindow;
			(menu[idx + 3] as ToolStripMenuItem).Checked = settings.AutoLoad;

			form.TopMost = settings.TopMost;

			// do we need to do this OnShown() as well?
			form.Owner = settings.FloatingWindow ? null : GlobalWin.MainForm;
		}

		private static void AttachSettingHooks(IToolFormAutoConfig tool, ToolDialogSettings settings)
		{
			var form = (Form)tool;
			ToolStripItemCollection dest = null;
			var oldsize = form.Size; // this should be the right time to grab this size
			foreach (Control c in form.Controls)
			{
				if (c is MenuStrip)
				{
					var ms = c as MenuStrip;
					foreach (ToolStripMenuItem submenu in ms.Items)
					{
						if (submenu.Text.Contains("Settings"))
						{
							dest = submenu.DropDownItems;
							dest.Add(new ToolStripSeparator());
							break;
						}
					}
					if (dest == null)
					{
						var submenu = new ToolStripMenuItem("&Settings");
						ms.Items.Add(submenu);
						dest = submenu.DropDownItems;
					}
					break;
				}
			}
			if (dest == null)
				throw new InvalidOperationException("IToolFormAutoConfig must have menu to bind to!");

			int idx = dest.Count;

			dest.Add("Save Window &Position");
			dest.Add("Stay on &Top");
			dest.Add("&Float from Parent");
			dest.Add("&Autoload");
			dest.Add("Restore &Defaults");

			RefreshSettings(form, dest, settings, idx);

			if (settings.UseWindowPosition)
			{
				form.Location = settings.WindowPosition;
			}
			if (settings.UseWindowSize)
			{
				if (form.FormBorderStyle == FormBorderStyle.Sizable || form.FormBorderStyle == FormBorderStyle.SizableToolWindow)
					form.Size = settings.WindowSize;
			}

			form.FormClosing += (o, e) =>
			{
				settings.Wndx = form.Location.X;
				settings.Wndy = form.Location.Y;
				settings.Width = form.Right - form.Left; // why not form.Size.Width?
				settings.Height = form.Bottom - form.Top;
			};

			dest[idx + 0].Click += (o, e) =>
			{
				bool val = !(o as ToolStripMenuItem).Checked;
				settings.SaveWindowPosition = val;
				(o as ToolStripMenuItem).Checked = val;
			};
			dest[idx + 1].Click += (o, e) =>
			{
				bool val = !(o as ToolStripMenuItem).Checked;
				settings.TopMost = val;
				(o as ToolStripMenuItem).Checked = val;
				form.TopMost = val;
			};
			dest[idx + 2].Click += (o, e) =>
			{
				bool val = !(o as ToolStripMenuItem).Checked;
				settings.FloatingWindow = val;
				(o as ToolStripMenuItem).Checked = val;
				form.Owner = val ? null : GlobalWin.MainForm;
			};
			dest[idx + 3].Click += (o, e) =>
			{
				bool val = !(o as ToolStripMenuItem).Checked;
				settings.AutoLoad = val;
				(o as ToolStripMenuItem).Checked = val;
			};
			dest[idx + 4].Click += (o, e) =>
			{
				settings.RestoreDefaults();
				RefreshSettings(form, dest, settings, idx);
				form.Size = oldsize;
			};
		}

		private static bool HasCustomConfig(IToolForm tool)
		{
			return tool.GetType().GetPropertiesWithAttrib(typeof(ConfigPersistAttribute)).Any();
		}

		private static void InstallCustomConfig(IToolForm tool, Dictionary<string, object> data)
		{
			Type type = tool.GetType();
			var props = type.GetPropertiesWithAttrib(typeof(ConfigPersistAttribute)).ToList();
			if (props.Count == 0)
				return;

			foreach (var prop in props)
			{
				object val;
				if (data.TryGetValue(prop.Name, out val))
				{
					if (val is string && prop.PropertyType != typeof(string))
					{
						// if a type has a TypeConverter, and that converter can convert to string,
						// that will be used in place of object markup by JSON.NET

						// but that doesn't work with $type metadata, and JSON.NET fails to fall
						// back on regular object serialization when needed.  so try to undo a TypeConverter
						// operation here
						var converter = TypeDescriptor.GetConverter(prop.PropertyType);
						val = converter.ConvertFromString((string)val);
					}
					else if (!(val is bool) && prop.PropertyType.IsPrimitive)
					{
						// numeric constanst are similarly hosed
						val = Convert.ChangeType(val, prop.PropertyType);
					}
					prop.SetValue(tool, val, null);
				}
			}

			(tool as Form).FormClosing += (o, e) => SaveCustomConfig(tool, data, props);
		}

		private static void SaveCustomConfig(IToolForm tool, Dictionary<string, object> data, List<PropertyInfo> props)
		{
			data.Clear();
			foreach (var prop in props)
			{
				data.Add(prop.Name, prop.GetValue(tool, null));
			}
		}

	
		/// <summary>
		/// Determines whether a given IToolForm is already loaded
		/// </summary>
		public bool IsLoaded<T>() where T : IToolForm
		{
			var existingTool = _tools.FirstOrDefault(x => x is T);
			if (existingTool != null)
			{
				return !existingTool.IsDisposed;
			}

			return false;
		}

		/// <summary>
		/// Returns true if an instance of T exists
		/// </summary>
		public bool Has<T>() where T : IToolForm
		{
			return _tools.Any(x => x is T && !x.IsDisposed);
		}

		/// <summary>
		/// Gets the instance of T, or creates and returns a new instance
		/// </summary>
		public IToolForm Get<T>() where T : IToolForm
		{
			return Load<T>(false);
		}

		public void UpdateBefore()
		{
			var beforeList = _tools.Where(x => x.UpdateBefore);
			foreach (var tool in beforeList)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // Ram Watch hack, on screen display should run even if Ram Watch is closed
				{
					tool.UpdateValues();
				}
			}
		}

		public void UpdateAfter()
		{
			var afterList = _tools.Where(x => !x.UpdateBefore);
			foreach (var tool in afterList)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // Ram Watch hack, on screen display should run even if Ram Watch is closed
				{
					tool.UpdateValues();
				}
			}
		}

		/// <summary>
		/// Calls UpdateValues() on an instance of T, if it exists
		/// </summary>
		public void UpdateValues<T>() where T : IToolForm
		{
			CloseIfDisposed<T>();
			var tool = _tools.FirstOrDefault(x => x is T);
			if (tool != null)
			{
				tool.UpdateValues();
			}
		}

		public void Restart()
		{
			// If Cheat tool is loaded, restarting will restart the list too anyway
			if (!GlobalWin.Tools.Has<Cheats>())
			{
				Global.CheatList.NewList(GenerateDefaultCheatFilename(), autosave: true);
			}

			var unavailable = new List<IToolForm>();

			foreach (var tool in _tools)
			{
				if (ServiceInjector.IsAvailable(Global.Emulator.ServiceProvider, tool.GetType()))
				{
					ServiceInjector.UpdateServices(Global.Emulator.ServiceProvider, tool);
					if ((tool.IsHandleCreated && !tool.IsDisposed) || tool is RamWatch) // Hack for Ram Watch - in display watches mode it wants to keep running even closed, it will handle disposed logic
					{
						tool.Restart();
					}
				}
				else
				{
					unavailable.Add(tool);
					ServiceInjector.ClearServices(tool); // the services of the old emulator core are no longer valid on the tool
				}
			}

			foreach (var tool in unavailable)
			{
				tool.Close();
				_tools.Remove(tool);
			}
		}

		/// <summary>
		/// Calls Restart() on an instance of T, if it exists
		/// </summary>
		public void Restart<T>() where T : IToolForm
		{
			CloseIfDisposed<T>();
			var tool = _tools.FirstOrDefault(x => x is T);
			if (tool != null)
			{
				tool.Restart();
			}
		}

		/// <summary>
		/// Runs AskSave on every tool dialog, false is returned if any tool returns false
		/// </summary>
		public bool AskSave()
		{
			if (Global.Config.SupressAskSave) // User has elected to not be nagged
			{
				return true;
			}

			return _tools
				.Select(tool => tool.AskSaveChanges())
				.All(result => result);
		}

		/// <summary>
		/// Calls AskSave() on an instance of T, if it exists, else returns true
		/// The caller should interpret false as cancel and will back out of the action that invokes this call
		/// </summary>
		public bool AskSave<T>() where T : IToolForm
		{
			if (Global.Config.SupressAskSave) // User has elected to not be nagged
			{
				return true;
			}

			var tool = _tools.FirstOrDefault(x => x is T);
			if (tool != null)
			{
				return tool.AskSaveChanges();
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// If T exists, this call will close the tool, and remove it from memory
		/// </summary>
		public void Close<T>() where T : IToolForm
		{
			var tool = _tools.FirstOrDefault(x => x is T);
			if (tool != null)
			{
				tool.Close();
				_tools.Remove(tool);
			}
		}

		public void Close(Type toolType)
		{
			var tool = _tools.FirstOrDefault(x => toolType.IsAssignableFrom(x.GetType()));

			if (tool != null)
			{
				tool.Close();
				_tools.Remove(tool);
			}
		}

		public void Close()
		{
			_tools.ForEach(x => x.Close());
			_tools.Clear();
		}

		private IToolForm CreateInstance<T>()
			where T: IToolForm
		{
			return CreateInstance(typeof(T));
		}

		private IToolForm CreateInstance(Type toolType)
		{
			var tool = (IToolForm)Activator.CreateInstance(toolType);

			// Add to our list of tools
			_tools.Add(tool);
			return tool;
		}

		private void CloseIfDisposed<T>() where T : IToolForm
		{
			var existingTool = _tools.FirstOrDefault(x => x is T);
			if (existingTool != null && existingTool.IsDisposed)
			{
				Close<T>();
			}
		}

		public void UpdateToolsBefore(bool fromLua = false)
		{
			if (Has<LuaConsole>())
			{
				if (!fromLua)
				{
					LuaConsole.StartLuaDrawing();
				}
			}

			UpdateBefore();
		}

		public void UpdateToolsAfter(bool fromLua = false)
		{
			if (!fromLua && Has<LuaConsole>())
			{
				LuaConsole.ResumeScripts(true);
			}

			GlobalWin.Tools.UpdateAfter();

			if (Has<LuaConsole>())
			{
				if (!fromLua)
				{
					LuaConsole.EndLuaDrawing();
				}
			}
		}

		public void FastUpdateBefore()
		{
			var beforeList = _tools.Where(x => x.UpdateBefore);
			foreach (var tool in beforeList)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // Ram Watch hack, on screen display should run even if Ram Watch is closed
				{
					tool.FastUpdate();
				}
			}
		}

		public void FastUpdateAfter()
		{
			var afterList = _tools.Where(x => !x.UpdateBefore);
			foreach (var tool in afterList)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // Ram Watch hack, on screen display should run even if Ram Watch is closed
				{
					tool.FastUpdate();
				}
			}
		}

		public bool IsAvailable<T>()
		{
			return ServiceInjector.IsAvailable(Global.Emulator.ServiceProvider, typeof(T));
		}

		// Note: Referencing these properties creates an instance of the tool and persists it.  They should be referenced by type if this is not desired
		#region Tools

		public RamWatch RamWatch
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is RamWatch);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as RamWatch;
					}
				}
				
				var newTool = new RamWatch();
				_tools.Add(newTool);
				return newTool;
			}
		}

		public RamSearch RamSearch
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is RamSearch);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as RamSearch;
					}
				}

				var newTool = new RamSearch();
				_tools.Add(newTool);
				return newTool;
			}
		}

		public Cheats Cheats
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is Cheats);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as Cheats;
					}
				}

				var newTool = new Cheats();
				_tools.Add(newTool);
				return newTool;
			}
		}

		public HexEditor HexEditor
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is HexEditor);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as HexEditor;
					}
				}

				var newTool = new HexEditor();
				_tools.Add(newTool);
				return newTool;
			}
		}

		public VirtualpadTool VirtualPad
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is VirtualpadTool);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as VirtualpadTool;
					}
				}

				var newTool = new VirtualpadTool();
				_tools.Add(newTool);
				return newTool;
			}
		}

		public SNESGraphicsDebugger SNESGraphicsDebugger
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is SNESGraphicsDebugger);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as SNESGraphicsDebugger;
					}
				}

				var newTool = new SNESGraphicsDebugger();
				_tools.Add(newTool);
				return newTool;
			}
		}

		public LuaConsole LuaConsole
		{
			get
			{
				var tool = _tools.FirstOrDefault(x => x is LuaConsole);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as LuaConsole;
					}
				}

				var newTool = new LuaConsole();
				_tools.Add(newTool);
				return newTool;
			}
		}

		#endregion

		#region Specialized Tool Loading Logic

		public void LoadRamWatch(bool loadDialog)
		{
			if (Global.Emulator.HasMemoryDomains())
			if (!IsLoaded<RamWatch>() && Global.Config.RecentWatches.AutoLoad && !Global.Config.RecentWatches.Empty)
			{
				GlobalWin.Tools.RamWatch.LoadFileFromRecent(Global.Config.RecentWatches.MostRecent);
			}

			if (loadDialog)
			{
				GlobalWin.Tools.Load<RamWatch>();
			}
		}

		public void LoadTraceLogger()
		{
			if (Global.Emulator.CpuTraceAvailable())
			{
				Load<TraceLogger>();
			}
		}

		public void LoadGameGenieEc()
		{
			if (Global.Emulator.SystemId == "NES")
			{
				Load<NESGameGenie>();
			}
			else if (Global.Emulator.SystemId == "SNES")
			{
				Load<SNESGameGenie>();
			}
			else if ((Global.Emulator.SystemId == "GB") || (Global.Game.System == "GG"))
			{
				Load<GBGameGenie>();
			}
			else if (Global.Emulator.SystemId == "GEN" && VersionInfo.DeveloperBuild)
			{
				Load<GenGameGenie>();
			}
		}

		#endregion

		public static string GenerateDefaultCheatFilename()
		{
			var pathEntry = Global.Config.PathEntries[Global.Game.System, "Cheats"]
			                ?? Global.Config.PathEntries[Global.Game.System, "Base"];

			var path = PathManager.MakeAbsolutePath(pathEntry.Path, Global.Game.System);

			var f = new FileInfo(path);
			if (f.Directory != null && f.Directory.Exists == false)
			{
				f.Directory.Create();
			}

			return Path.Combine(path, PathManager.FilesystemSafeName(Global.Game) + ".cht");
		}
	}
}
