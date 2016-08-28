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
		#region Fields

		private readonly Form _owner;

		// TODO: merge ToolHelper code where logical
		// For instance, add an IToolForm property called UsesCheats, so that a UpdateCheatRelatedTools() method can update all tools of this type
		// Also a UsesRam, and similar method
		private readonly List<IToolForm> _tools = new List<IToolForm>();

		#endregion

		#region cTor(s)

		/// <summary>
		/// Initialize an new ToolManager instance 
		/// </summary>
		/// <param name="owner">Form that handle the ToolManager</param>
		public ToolManager(Form owner)
		{
			_owner = owner;
		}

		#endregion

		/// <summary>
		/// Loads the tool dialog T (T must implemants <see cref="IToolForm"/>) , if it does not exist it will be created, if it is already open, it will be focused
		/// This method should be used only if you can't use the generic one
		/// </summary>
		/// <param name="toolType">Type of tool you want to load</param>
		/// <param name="focus">Define if the tool form has to get the focus or not (Default is true)</param>
		/// <returns>An instanciated <see cref="IToolForm"/></returns>
		/// <exception cref="ArgumentException">Raised if <paramref name="toolType"/> can't be casted into IToolForm </exception>
		internal IToolForm Load(Type toolType, bool focus = true)
		{
			if (!typeof(IToolForm).IsAssignableFrom(toolType))
			{
				throw new ArgumentException(string.Format("Type {0} does not implement IToolForm.", toolType.Name));
			}
			else
			{
				//The type[] in parameter is used to avoid an ambigous name exception
				MethodInfo method = GetType().GetMethod("Load", new Type[] { typeof(bool) }).MakeGenericMethod(toolType);
				return (IToolForm)method.Invoke(this, new object[] { focus });
			}
		}

		/// <summary>
		/// Loads the tool dialog T (T must implement <see cref="IToolForm"/>) , if it does not exist it will be created, if it is already open, it will be focused
		/// </summary>
		/// <typeparam name="T">Type of tool you want to load</typeparam>
		/// <param name="focus">Define if the tool form has to get the focus or not (Default is true)</param>
		/// <returns>An instanciated <see cref="IToolForm"/></returns>
		public T Load<T>(bool focus = true)
			where T : class, IToolForm
		{
			return Load<T>(string.Empty, focus);
		}

		/// <summary>
		/// Loads the tool dialog T (T must implement <see cref="IToolForm"/>) , if it does not exist it will be created, if it is already open, it will be focused
		/// </summary>
		/// <typeparam name="T">Type of tool you want to load</typeparam>
		/// <param name="focus">Define if the tool form has to get the focus or not (Default is true)</param>
		/// <param name="toolPath">Path to the dll of the external tool</param>
		/// <returns>An instanciated <see cref="IToolForm"/></returns>
		public T Load<T>(string toolPath, bool focus = true)
			where T : class, IToolForm
		{
			bool isExternal = typeof(T) == typeof(IExternalToolForm);

			if (!IsAvailable<T>() && !isExternal)
			{
				return null;
			}

			T existingTool;
			if (isExternal)
			{
				existingTool = (T)_tools.FirstOrDefault(x => x is T && x.GetType().Assembly.Location == toolPath);
			}
			else
			{
				existingTool = (T)_tools.FirstOrDefault(x => x is T);
			}

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

			IToolForm newTool = CreateInstance<T>(toolPath);

			if (newTool == null)
			{
				return null;
			}

			if (newTool is Form)
			{
				(newTool as Form).Owner = GlobalWin.MainForm;
			}

			ServiceInjector.UpdateServices(Global.Emulator.ServiceProvider, newTool);
			string toolType = typeof(T).ToString();

			// auto settings
			if (newTool is IToolFormAutoConfig)
			{
				ToolDialogSettings settings;
				if (!Global.Config.CommonToolSettings.TryGetValue(toolType, out settings))
				{
					settings = new ToolDialogSettings();
					Global.Config.CommonToolSettings[toolType] = settings;
				}
				AttachSettingHooks(newTool as IToolFormAutoConfig, settings);
			}

			// custom settings
			if (HasCustomConfig(newTool))
			{
				Dictionary<string, object> settings;
				if (!Global.Config.CustomToolSettings.TryGetValue(toolType, out settings))
				{
					settings = new Dictionary<string, object>();
					Global.Config.CustomToolSettings[toolType] = settings;
				}
				InstallCustomConfig(newTool, settings);
			}

			newTool.Restart();
			newTool.Show();
			return (T)newTool;
		}

		public void AutoLoad()
		{
			var genericSettings = Global.Config.CommonToolSettings
				.Where(kvp => kvp.Value.AutoLoad)
				.Select(kvp => kvp.Key);

			var customSettings = Global.Config.CustomToolSettings
				.Where(list => list.Value.Any(kvp => typeof(ToolDialogSettings).IsAssignableFrom(kvp.Value.GetType()) && (kvp.Value as ToolDialogSettings).AutoLoad))
				.Select(kvp => kvp.Key);

			var typeNames = genericSettings.Concat(customSettings);

			foreach (var typename in typeNames)
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

		private void AttachSettingHooks(IToolFormAutoConfig tool, ToolDialogSettings settings)
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
				form.StartPosition = FormStartPosition.Manual;
				form.Location = settings.WindowPosition;
			}
			if (settings.UseWindowSize)
			{
				if (form.FormBorderStyle == FormBorderStyle.Sizable || form.FormBorderStyle == FormBorderStyle.SizableToolWindow)
					form.Size = settings.WindowSize;
			}

			form.FormClosing += (o, e) =>
			{
				if (form.WindowState == FormWindowState.Normal)
				{
					settings.Wndx = form.Location.X;
					settings.Wndy = form.Location.Y;
					settings.Width = form.Right - form.Left; // why not form.Size.Width?
					settings.Height = form.Bottom - form.Top;
				}
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
				form.Owner = val ? null : _owner;
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
						val = converter.ConvertFromString(null,System.Globalization.CultureInfo.InvariantCulture,((string)val));
					}
					else if (!(val is bool) && prop.PropertyType.IsPrimitive)
					{
						// numeric constanst are similarly hosed
						val = Convert.ChangeType(val, prop.PropertyType, System.Globalization.CultureInfo.InvariantCulture);
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
				data.Add(prop.Name, prop.GetValue(tool, BindingFlags.GetProperty, Type.DefaultBinder, null, System.Globalization.CultureInfo.InvariantCulture));
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
		public IToolForm Get<T>() where T : class, IToolForm
		{
			return Load<T>(false);
		}

		public IEnumerable<Type> AvailableTools
		{
			get
			{
				//return _tools.Where(t => !t.IsDisposed);
				return Assembly
					.GetAssembly(typeof(IToolForm))
					.GetTypes()
					.Where(t => typeof(IToolForm).IsAssignableFrom(t))
					.Where(t => !t.IsInterface)
					.Where(t => IsAvailable(t));
			}
		}

		public void UpdateBefore()
		{
			var beforeList = _tools.Where(x => x.UpdateBefore);
			foreach (var tool in beforeList)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // RAM Watch hack, on screen display should run even if RAM Watch is closed
				{
					tool.UpdateValues();
				}
			}
			foreach (var tool in _tools)
				tool.NewUpdate(ToolFormUpdateType.PreFrame);
		}

		public void UpdateAfter()
		{
			var afterList = _tools.Where(x => !x.UpdateBefore);
			foreach (var tool in afterList)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // RAM Watch hack, on screen display should run even if RAM Watch is closed
				{
					tool.UpdateValues();
				}
			}

			foreach (var tool in _tools)
				tool.NewUpdate(ToolFormUpdateType.PostFrame);
		}

		/// <summary>
		/// Calls UpdateValues() on an instance of T, if it exists
		/// </summary>
		public void UpdateValues<T>() where T : IToolForm
		{
			var tool = _tools.FirstOrDefault(x => x is T);
			if (tool != null)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // RAM Watch hack, on screen display should run even if RAM Watch is closed
				{
					tool.UpdateValues();
				}
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
					
					if ((tool.IsHandleCreated && !tool.IsDisposed) || tool is RamWatch) // Hack for RAM Watch - in display watches mode it wants to keep running even closed, it will handle disposed logic
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

		/// <summary>
		/// Create a new instance of an IToolForm and return it
		/// </summary>
		/// <typeparam name="T">Type of tool you want to create</typeparam>
		/// <param name="dllPath">Path dll for an external tool</param>
		/// <returns>New instance of an IToolForm</returns>
		private IToolForm CreateInstance<T>(string dllPath)
			where T : IToolForm
		{
			return CreateInstance(typeof(T), dllPath);
		}

		/// <summary>
		/// Create a new instance of an IToolForm and return it
		/// </summary>
		/// <param name="toolType">Type of tool you want to create</param>
		/// <param name="dllPath">Path dll for an external tool</param>
		/// <returns>New instance of an IToolForm</returns>
		private IToolForm CreateInstance(Type toolType, string dllPath)
		{
			IToolForm tool;

			//Specific case for custom tools
			//TODO: Use AppDomain in order to be able to unload the assembly
			//Hard stuff as we need a proxy object that inherit from MarshalByRefObject.			
			if (toolType == typeof(IExternalToolForm))
			{
				if (MessageBox.Show("Are you sure want to load this external tool?\r\nAccept ONLY if you trust the source and if you know what you're doing. In any other case, choose no."
				, "Confirmm loading", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					try
					{
						tool = Activator.CreateInstanceFrom(dllPath, "BizHawk.Client.EmuHawk.CustomMainForm").Unwrap() as IExternalToolForm;
						if (tool == null)
						{
							MessageBox.Show("It seems that the object CustomMainForm does not implement IExternalToolForm. Please review the code.", "No, no, no. Wrong Way !", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							return null;
						}
					}
					catch (MissingMethodException)
					{
						MessageBox.Show("It seems that the object CustomMainForm does not have a public default constructor. Please review the code.", "No, no, no. Wrong Way !", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						return null;
					}
					catch (TypeLoadException)
					{
						MessageBox.Show("It seems that the object CustomMainForm does not exists. Please review the code.", "No, no, no. Wrong Way !", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						return null;
					}
				}
				else
				{
					return null;
				}
			}
			else
			{
				tool = (IToolForm)Activator.CreateInstance(toolType);
			}

			// Add to our list of tools
			_tools.Add(tool);
			return tool;
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
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // RAM Watch hack, on screen display should run even if RAM Watch is closed
				{
					tool.FastUpdate();
				}
			}
		}

		public void FastUpdateAfter()
		{
			if (Global.Config.RunLuaDuringTurbo && Has<LuaConsole>())
			{
				LuaConsole.ResumeScripts(true);
			}

			var afterList = _tools.Where(x => !x.UpdateBefore);
			foreach (var tool in afterList)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && Global.Config.DisplayRamWatch)) // RAM Watch hack, on screen display should run even if RAM Watch is closed
				{
					tool.FastUpdate();
				}
			}

			if (Global.Config.RunLuaDuringTurbo && Has<LuaConsole>())
			{
				LuaConsole.EndLuaDrawing();
			}
		}

		public bool IsAvailable<T>()
		{
			return IsAvailable(typeof(T));
		}

		public bool IsAvailable(Type t)
		{
			if (!ServiceInjector.IsAvailable(Global.Emulator.ServiceProvider, t))
			{
				return false;
			}

			var tool = Assembly
					.GetExecutingAssembly()
					.GetTypes()
					.FirstOrDefault(type => type == t);

			if (tool == null) // This isn't a tool, must not be available
			{
				return false;
			}

			var attr = tool.GetCustomAttributes(false)
				.OfType<ToolAttributes>()
				.FirstOrDefault();

			if (attr == null) // If no attributes there is no supported systems documented so assume all
			{
				return true;
			}

			// If no supported systems mentioned assume all
			if (attr.SupportedSystems != null && attr.SupportedSystems.Any())
			{
				return attr.SupportedSystems.Contains(Global.Emulator.SystemId);
			}

			return true;
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

		public TAStudio TAStudio
		{
			get
			{
				// prevent nasty silent corruption
				if (!GlobalWin.Tools.IsLoaded<TAStudio>())
					System.Diagnostics.Debug.Fail("TAStudio does not exist!");

				var tool = _tools.FirstOrDefault(x => x is TAStudio);
				if (tool != null)
				{
					if (tool.IsDisposed)
					{
						_tools.Remove(tool);
					}
					else
					{
						return tool as TAStudio;
					}
				}

				var newTool = new TAStudio();
				_tools.Add(newTool);
				return newTool;
			}
		}

		#endregion

		#region Specialized Tool Loading Logic

		public void LoadRamWatch(bool loadDialog)
		{
			if (!IsLoaded<RamWatch>())
			{
				Load<RamWatch>();
			}

			if (IsAvailable<RamWatch>()) // Just because we attempted to load it, doesn't mean it was, the current core may not have the correct dependencies
			{
				if (Global.Config.RecentWatches.AutoLoad && !Global.Config.RecentWatches.Empty)
				{
					RamWatch.LoadFileFromRecent(Global.Config.RecentWatches.MostRecent);
				}

				if (!loadDialog)
				{
					Get<RamWatch>().Close();
				}
			}
		}

		public void LoadGameGenieEc()
		{
			if (GlobalWin.Tools.IsAvailable<GameShark>())
			{
				GlobalWin.Tools.Load<GameShark>();
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
