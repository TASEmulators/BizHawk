using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public class ToolManager
	{
		private readonly MainForm _owner;
		private Config _config;
		private readonly DisplayManager _displayManager;
		private readonly InputManager _inputManager;
		private IExternalApiProvider _apiProvider;
		private IEmulator _emulator;
		private readonly IMovieSession _movieSession;
		private IGameInfo _game;

		// TODO: merge ToolHelper code where logical
		// For instance, add an IToolForm property called UsesCheats, so that a UpdateCheatRelatedTools() method can update all tools of this type
		// Also a UsesRam, and similar method
		private readonly List<IToolForm> _tools = new List<IToolForm>();

		private IExternalApiProvider ApiProvider
		{
			get => _apiProvider;
			set => _owner.EmuClient = (EmuClientApi) (_apiProvider = value).GetApi<IEmuClientApi>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolManager"/> class.
		/// </summary>
		public ToolManager(
			MainForm owner,
			Config config,
			DisplayManager displayManager,
			InputManager inputManager,
			IEmulator emulator,
			IMovieSession movieSession,
			IGameInfo game)
		{
			_owner = owner;
			_config = config;
			_displayManager = displayManager;
			_inputManager = inputManager;
			_emulator = emulator;
			_movieSession = movieSession;
			_game = game;
			ApiProvider = ApiManager.Restart(_emulator.ServiceProvider, _owner, _displayManager, _inputManager, _movieSession, this, _config, _emulator, _game);
		}

		/// <summary>
		/// Loads the tool dialog T (T must implements <see cref="IToolForm"/>) , if it does not exist it will be created, if it is already open, it will be focused
		/// This method should be used only if you can't use the generic one
		/// </summary>
		/// <param name="toolType">Type of tool you want to load</param>
		/// <param name="focus">Define if the tool form has to get the focus or not (Default is true)</param>
		/// <returns>An instantiated <see cref="IToolForm"/></returns>
		/// <exception cref="ArgumentException">Raised if <paramref name="toolType"/> can't cast into IToolForm </exception>
		internal IToolForm Load(Type toolType, bool focus = true)
		{
			if (!typeof(IToolForm).IsAssignableFrom(toolType))
			{
				throw new ArgumentException($"Type {toolType.Name} does not implement {nameof(IToolForm)}.");
			}

			return (IToolForm) typeof(ToolManager).GetMethod("Load", new[] { typeof(bool), typeof(string) })
				.MakeGenericMethod(toolType)
				.Invoke(this, new object[] { focus, "" });
		}

		// If the form inherits ToolFormBase, it will set base properties such as Tools, Config, etc
		private void SetBaseProperties(IToolForm form)
		{
			if (!(form is FormBase f)) return;

			f.Config = _config;
			if (!(form is ToolFormBase tool)) return;

			tool.Tools = this;
			tool.DisplayManager = _displayManager;
			tool.InputManager = _inputManager;
			tool.MainForm = _owner;
			tool.MovieSession = _movieSession;
			tool.Game = _game;
		}

		/// <summary>
		/// Loads the tool dialog T (T must implement <see cref="IToolForm"/>) , if it does not exist it will be created, if it is already open, it will be focused
		/// </summary>
		/// <param name="focus">Define if the tool form has to get the focus or not (Default is true)</param>
		/// <param name="toolPath">Path to the .dll of the external tool</param>
		/// <typeparam name="T">Type of tool you want to load</typeparam>
		/// <returns>An instantiated <see cref="IToolForm"/></returns>
		public T Load<T>(bool focus = true, string toolPath = "")
			where T : class, IToolForm
		{
			if (!IsAvailable<T>()) return null;

			var existingTool = _tools.OfType<T>().FirstOrDefault();
			if (existingTool != null)
			{
				if (!existingTool.IsDisposed)
				{
					if (focus)
					{
						existingTool.Show();
						existingTool.Focus();
					}
					return existingTool;
				}
				_tools.Remove(existingTool);
			}

			if (!(CreateInstance<T>(toolPath) is T newTool)) return null;

			if (newTool is Form form) form.Owner = _owner;
			ServiceInjector.UpdateServices(_emulator.ServiceProvider, newTool);
			SetBaseProperties(newTool);
			var toolTypeName = typeof(T).ToString();
			// auto settings
			if (newTool is IToolFormAutoConfig autoConfigTool)
			{
				AttachSettingHooks(
					autoConfigTool,
					_config.CommonToolSettings.TryGetValue(toolTypeName, out var settings)
						? settings
						: (_config.CommonToolSettings[toolTypeName] = new ToolDialogSettings())
				);
			}
			// custom settings
			if (HasCustomConfig(newTool))
			{
				InstallCustomConfig(
					newTool,
					_config.CustomToolSettings.TryGetValue(toolTypeName, out var settings)
						? settings
						: (_config.CustomToolSettings[toolTypeName] = new Dictionary<string, object>())
				);
			}

			newTool.Restart();
			newTool.Show();
			return newTool;
		}

		/// <summary>Loads the external tool's entry form.</summary>
		public IExternalToolForm LoadExternalToolForm(string toolPath, string customFormTypeName, bool focus = true, bool skipExtToolWarning = false)
		{
			var existingTool = _tools.OfType<IExternalToolForm>().FirstOrDefault(t => t.GetType().Assembly.Location == toolPath);
			if (existingTool != null)
			{
				if (!existingTool.IsDisposed)
				{
					if (focus)
					{
						existingTool.Show();
						existingTool.Focus();
					}
					return existingTool;
				}
				_tools.Remove(existingTool);
			}

			var newTool = (IExternalToolForm) CreateInstance(typeof(IExternalToolForm), toolPath, customFormTypeName, skipExtToolWarning: skipExtToolWarning);
			if (newTool == null) return null;
			if (newTool is Form form) form.Owner = _owner;
			ApiInjector.UpdateApis(ApiProvider, newTool);
			ServiceInjector.UpdateServices(_emulator.ServiceProvider, newTool);
			SetBaseProperties(newTool);
			// auto settings
			if (newTool is IToolFormAutoConfig autoConfigTool)
			{
				AttachSettingHooks(
					autoConfigTool,
					_config.CommonToolSettings.TryGetValue(customFormTypeName, out var settings)
						? settings
						: (_config.CommonToolSettings[customFormTypeName] = new ToolDialogSettings())
				);
			}
			// custom settings
			if (HasCustomConfig(newTool))
			{
				InstallCustomConfig(
					newTool,
					_config.CustomToolSettings.TryGetValue(customFormTypeName, out var settings)
						? settings
						: (_config.CustomToolSettings[customFormTypeName] = new Dictionary<string, object>())
				);
			}

			newTool.Restart();
			newTool.Show();
			return newTool;
		}

		public void AutoLoad()
		{
			var genericSettings = _config.CommonToolSettings
				.Where(kvp => kvp.Value.AutoLoad)
				.Select(kvp => kvp.Key);

			var customSettings = _config.CustomToolSettings
				.Where(list => list.Value.Any(kvp => kvp.Value is ToolDialogSettings settings && settings.AutoLoad))
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

		private void RefreshSettings(Form form, ToolStripItemCollection menu, ToolDialogSettings settings, int idx)
		{
			((ToolStripMenuItem)menu[idx + 0]).Checked = settings.SaveWindowPosition;
			var stayOnTopItem = (ToolStripMenuItem)menu[idx + 1];
			stayOnTopItem.Checked = settings.TopMost;
			if (OSTailoredCode.IsUnixHost)
			{
				// This is the job of the WM, and is usually exposed in window decorations or a context menu on them
				stayOnTopItem.Enabled = false;
				stayOnTopItem.Visible = false;
			}
			else
			{
				form.TopMost = settings.TopMost;
			}
			((ToolStripMenuItem)menu[idx + 2]).Checked = settings.FloatingWindow;
			((ToolStripMenuItem)menu[idx + 3]).Checked = settings.AutoLoad;

			// do we need to do this OnShown() as well?
			form.Owner = settings.FloatingWindow ? null : _owner;
		}

		private void AddCloseButton(ToolStripMenuItem subMenu, Form form)
		{
			if (subMenu.DropDownItems.Count > 0)
			{
				subMenu.DropDownItems.Add(new ToolStripSeparatorEx());
			}

			var closeMenuItem = new ToolStripMenuItem
			{
				Name = "CloseBtn", 
				Text = "&Close",
				ShortcutKeyDisplayString = "Alt+F4"
			};

			closeMenuItem.Click += (o, e) => { form.Close(); };
			subMenu.DropDownItems.Add(closeMenuItem);
		}

		private void AttachSettingHooks(IToolFormAutoConfig tool, ToolDialogSettings settings)
		{
			var form = (Form)tool;
			ToolStripItemCollection dest = null;
			var oldSize = form.Size; // this should be the right time to grab this size
			foreach (Control c in form.Controls)
			{
				if (c is MenuStrip ms)
				{
					foreach (ToolStripMenuItem submenu in ms.Items)
					{
						if (submenu.Text.Contains("Settings"))
						{
							dest = submenu.DropDownItems;
							dest.Add(new ToolStripSeparator());
						}
						else if (submenu.Text.Contains("File"))
						{
							AddCloseButton(submenu, form);
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
			{
				throw new InvalidOperationException($"{nameof(IToolFormAutoConfig)} must have menu to bind to!");
			}

			int idx = dest.Count;

			dest.Add("Save Window &Position");
			dest.Add("Stay on &Top");
			dest.Add("&Float from Parent");
			dest.Add("&Autoload with EmuHawk");
			dest.Add("Restore &Defaults");

			RefreshSettings(form, dest, settings, idx);

			if (settings.UseWindowPosition && IsOnScreen(settings.TopLeft))
			{
				form.StartPosition = FormStartPosition.Manual;
				form.Location = settings.WindowPosition;
			}

			if (settings.UseWindowSize)
			{
				if (form.FormBorderStyle == FormBorderStyle.Sizable || form.FormBorderStyle == FormBorderStyle.SizableToolWindow)
				{
					form.Size = settings.WindowSize;
				}
			}

			form.FormClosing += (o, e) =>
			{
				if (form.WindowState == FormWindowState.Normal)
				{
					settings.Wndx = form.Location.X;
					settings.Wndy = form.Location.Y;
					if (settings.Wndx < 0)
					{
						settings.Wndx = 0;
					}

					if (settings.Wndy < 0)
					{
						settings.Wndy = 0;
					}

					settings.Width = form.Right - form.Left; // why not form.Size.Width?
					settings.Height = form.Bottom - form.Top;
				}
			};

			dest[idx + 0].Click += (o, e) =>
			{
				bool val = !((ToolStripMenuItem)o).Checked;
				settings.SaveWindowPosition = val;
				((ToolStripMenuItem)o).Checked = val;
			};
			dest[idx + 1].Click += (o, e) =>
			{
				bool val = !((ToolStripMenuItem)o).Checked;
				settings.TopMost = val;
				((ToolStripMenuItem)o).Checked = val;
				form.TopMost = val;
			};
			dest[idx + 2].Click += (o, e) =>
			{
				bool val = !((ToolStripMenuItem)o).Checked;
				settings.FloatingWindow = val;
				((ToolStripMenuItem)o).Checked = val;
				form.Owner = val ? null : _owner;
			};
			dest[idx + 3].Click += (o, e) =>
			{
				bool val = !((ToolStripMenuItem)o).Checked;
				settings.AutoLoad = val;
				((ToolStripMenuItem)o).Checked = val;
			};
			dest[idx + 4].Click += (o, e) =>
			{
				settings.RestoreDefaults();
				RefreshSettings(form, dest, settings, idx);
				form.Size = oldSize;

				form.GetType()
					.GetMethodsWithAttrib(typeof(RestoreDefaultsAttribute))
					.FirstOrDefault()
					?.Invoke(form, new object[0]);
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
			{
				return;
			}

			foreach (var prop in props)
			{
				if (data.TryGetValue(prop.Name, out var val))
				{
					if (val is string str && prop.PropertyType != typeof(string))
					{
						// if a type has a TypeConverter, and that converter can convert to string,
						// that will be used in place of object markup by JSON.NET

						// but that doesn't work with $type metadata, and JSON.NET fails to fall
						// back on regular object serialization when needed.  so try to undo a TypeConverter
						// operation here
						var converter = TypeDescriptor.GetConverter(prop.PropertyType);
						val = converter.ConvertFromString(null, CultureInfo.InvariantCulture, str);
					}
					else if (!(val is bool) && prop.PropertyType.IsPrimitive)
					{
						// numeric constants are similarly hosed
						val = Convert.ChangeType(val, prop.PropertyType, CultureInfo.InvariantCulture);
					}

					prop.SetValue(tool, val, null);
				}
			}

			((Form)tool).FormClosing += (o, e) => SaveCustomConfig(tool, data, props);
		}

		private static void SaveCustomConfig(IToolForm tool, Dictionary<string, object> data, List<PropertyInfo> props)
		{
			data.Clear();
			foreach (var prop in props)
			{
				data.Add(prop.Name, prop.GetValue(tool, BindingFlags.GetProperty, Type.DefaultBinder, null, CultureInfo.InvariantCulture));
			}
		}

		/// <summary>
		/// Determines whether a given IToolForm is already loaded
		/// </summary>
		/// <typeparam name="T">Type of tool to check</typeparam>
		public bool IsLoaded<T>() where T : IToolForm
		{
			var existingTool = _tools.FirstOrDefault(t => t is T);
			if (existingTool != null)
			{
				return !existingTool.IsDisposed;
			}

			return false;
		}

		public bool IsOnScreen(Point topLeft)
		{
			return Screen.AllScreens.Any(
				screen => screen.WorkingArea.Contains(topLeft));
		}

		/// <summary>
		/// Returns true if an instance of T exists
		/// </summary>
		/// <typeparam name="T">Type of tool to check</typeparam>
		public bool Has<T>() where T : IToolForm
		{
			return _tools.Any(t => t is T && !t.IsDisposed);
		}

		/// <summary>
		/// Gets the instance of T, or creates and returns a new instance
		/// </summary>
		/// <typeparam name="T">Type of tool to get</typeparam>
		public IToolForm Get<T>() where T : class, IToolForm
		{
			return Load<T>(false);
		}

		public IEnumerable<Type> AvailableTools => EmuHawk.ReflectionCache.Types
			.Where(t => typeof(IToolForm).IsAssignableFrom(t))
			.Where(t => !t.IsInterface)
			.Where(IsAvailable);

		/// <summary>
		/// Calls UpdateValues() on an instance of T, if it exists
		/// </summary>
		/// <typeparam name="T">Type of tool to update</typeparam>
		public void UpdateValues<T>() where T : IToolForm
		{
			var tool = _tools.FirstOrDefault(t => t is T);
			if (tool != null)
			{
				if (!tool.IsDisposed ||
					(tool is RamWatch && _config.DisplayRamWatch)) // RAM Watch hack, on screen display should run even if RAM Watch is closed
				{
					tool.UpdateValues(ToolFormUpdateType.General);
				}
			}
		}

		public void Restart(Config config, IEmulator emulator, IGameInfo game)
		{
			_config = config;
			_emulator = emulator;
			_game = game;
			ApiProvider = ApiManager.Restart(_emulator.ServiceProvider, _owner, _displayManager, _inputManager, _movieSession, this, _config, _emulator, _game);
			// If Cheat tool is loaded, restarting will restart the list too anyway
			if (!Has<Cheats>())
			{
				_owner.CheatList.NewList(GenerateDefaultCheatFilename(), autosave: true);
			}

			var unavailable = new List<IToolForm>();

			foreach (var tool in _tools)
			{
				SetBaseProperties(tool);
				if (ServiceInjector.IsAvailable(_emulator.ServiceProvider, tool.GetType()))
				{
					ServiceInjector.UpdateServices(_emulator.ServiceProvider, tool);
					
					if ((tool.IsHandleCreated && !tool.IsDisposed) || tool is RamWatch) // Hack for RAM Watch - in display watches mode it wants to keep running even closed, it will handle disposed logic
					{
						if (tool is IExternalToolForm)
							ApiInjector.UpdateApis(ApiProvider, tool);
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
		/// <typeparam name="T">Type of tool to restart</typeparam>
		public void Restart<T>() where T : IToolForm
		{
			var tool = _tools.FirstOrDefault(t => t is T);
			tool?.Restart();
		}

		/// <summary>
		/// Runs AskSave on every tool dialog, false is returned if any tool returns false
		/// </summary>
		public bool AskSave()
		{
			if (_config.SuppressAskSave) // User has elected to not be nagged
			{
				return true;
			}

			return _tools
				.Select(tool => tool.AskSaveChanges())
				.All(result => result);
		}

		/// <summary>
		/// If T exists, this call will close the tool, and remove it from memory
		/// </summary>
		/// <typeparam name="T">Type of tool to close</typeparam>
		public void Close<T>() where T : IToolForm
		{
			var tool = _tools.FirstOrDefault(t => t is T);
			if (tool != null)
			{
				tool.Close();
				_tools.Remove(tool);
			}
		}

		public void Close(Type toolType)
		{
			var tool = _tools.FirstOrDefault(toolType.IsInstanceOfType);

			if (tool != null)
			{
				tool.Close();
				_tools.Remove(tool);
			}
		}

		public void Close()
		{
			_tools.ForEach(t => t.Close());
			_tools.Clear();
		}

		/// <summary>
		/// Create a new instance of an IToolForm and return it
		/// </summary>
		/// <typeparam name="T">Type of tool you want to create</typeparam>
		/// <param name="dllPath">Path .dll for an external tool</param>
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
		/// <param name="toolTypeName">For external tools, <see cref="Type.FullName"/> of the entry form's type (<paramref name="toolType"/> will be <see cref="IExternalToolForm"/>)</param>
		/// <returns>New instance of an IToolForm</returns>
		private IToolForm CreateInstance(Type toolType, string dllPath, string toolTypeName = null, bool skipExtToolWarning = false)
		{
			IToolForm tool;

			// Specific case for custom tools
			// TODO: Use AppDomain in order to be able to unload the assembly
			// Hard stuff as we need a proxy object that inherit from MarshalByRefObject.
			if (toolType == typeof(IExternalToolForm))
			{
				if (!skipExtToolWarning)
				{
					if (!_owner.ShowMessageBox2(
						"Are you sure want to load this external tool?\r\nAccept ONLY if you trust the source and if you know what you're doing. In any other case, choose no.",
						"Confirm loading",
						EMsgBoxIcon.Question))
					{
						return null;
					}
				}

				try
				{
					tool = Activator.CreateInstanceFrom(dllPath, toolTypeName ?? "BizHawk.Client.EmuHawk.CustomMainForm").Unwrap() as IExternalToolForm;
					if (tool == null)
					{
						_owner.ShowMessageBox($"It seems that the object CustomMainForm does not implement {nameof(IExternalToolForm)}. Please review the code.", "No, no, no. Wrong Way !", EMsgBoxIcon.Warning);
						return null;
					}
				}
				catch (MissingMethodException)
				{
					_owner.ShowMessageBox("It seems that the object CustomMainForm does not have a public default constructor. Please review the code.", "No, no, no. Wrong Way !", EMsgBoxIcon.Warning);
					return null;
				}
				catch (TypeLoadException)
				{
					_owner.ShowMessageBox("It seems that the object CustomMainForm does not exists. Please review the code.", "No, no, no. Wrong Way !", EMsgBoxIcon.Warning);
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

		public void UpdateToolsBefore()
		{
			foreach (var tool in _tools)
			{
				if (!tool.IsDisposed
					|| (tool is RamWatch && _config.DisplayRamWatch)) // RAM Watch hack, on screen display should run even if RAM Watch is closed
				{
					tool.UpdateValues(ToolFormUpdateType.PreFrame);
				}
			}
		}

		public void UpdateToolsAfter()
		{
			foreach (var tool in _tools)
			{
				if (!tool.IsDisposed
					|| (tool is RamWatch && _config.DisplayRamWatch)) // RAM Watch hack, on screen display should run even if RAM Watch is closed
				{
					tool.UpdateValues(ToolFormUpdateType.PostFrame);
				}
			}
		}

		public void FastUpdateBefore()
		{
			foreach (var tool in _tools)
			{
				if (!tool.IsDisposed
					|| (tool is RamWatch && _config.DisplayRamWatch)) // RAM Watch hack, on screen display should run even if RAM Watch is closed
				{
					tool.UpdateValues(ToolFormUpdateType.FastPreFrame);
				}
			}
		}

		public void FastUpdateAfter()
		{
			foreach (var tool in _tools)
			{
				if (!tool.IsDisposed
					|| (tool is RamWatch && _config.DisplayRamWatch)) // RAM Watch hack, on screen display should run even if RAM Watch is closed
				{
					tool.UpdateValues(ToolFormUpdateType.FastPostFrame);
				}
			}
		}

		private static readonly Lazy<List<string>> LazyAsmTypes = new Lazy<List<string>>(() =>
			EmuHawk.ReflectionCache.Types // Confining the search to only EmuHawk, for now at least, we may want to broaden for external tools one day
				.Select(t => t.AssemblyQualifiedName)
				.ToList());

		public bool IsAvailable(Type tool)
		{
			if (!ServiceInjector.IsAvailable(_emulator.ServiceProvider, tool)
				|| !LazyAsmTypes.Value.Contains(tool.AssemblyQualifiedName)) // not a tool
			{
				return false;
			}

			ToolAttribute attr = tool.GetCustomAttributes(false).OfType<ToolAttribute>().SingleOrDefault();
			if (attr == null)
			{
				return true; // no ToolAttribute on given type -> assumed all supported
			}

			return !attr.UnsupportedCores.Contains(_emulator.Attributes().CoreName) // not unsupported
				&& (!attr.SupportedSystems.Any() || attr.SupportedSystems.Contains(_emulator.SystemId)); // supported (no supported list -> assumed all supported)
		}

		public bool IsAvailable<T>() => IsAvailable(typeof(T));

		// Note: Referencing these properties creates an instance of the tool and persists it.  They should be referenced by type if this is not desired

		private T GetTool<T>() where T : class, IToolForm, new()
		{
			T tool = _tools.OfType<T>().FirstOrDefault();
			if (tool != null)
			{
				if (!tool.IsDisposed)
				{
					return tool;
				}
				_tools.Remove(tool);
			}
			tool = new T();
			_tools.Add(tool);
			return tool;
		}

		public RamWatch RamWatch => GetTool<RamWatch>();

		public RamSearch RamSearch => GetTool<RamSearch>();

		public HexEditor HexEditor => GetTool<HexEditor>();

		public VirtualpadTool VirtualPad => GetTool<VirtualpadTool>();

		public SNESGraphicsDebugger SNESGraphicsDebugger => GetTool<SNESGraphicsDebugger>();

		public LuaConsole LuaConsole => GetTool<LuaConsole>();

		public TAStudio TAStudio => GetTool<TAStudio>();

		public void LoadRamWatch(bool loadDialog)
		{
			if (IsLoaded<RamWatch>())
			{
				return;
			}

			Load<RamWatch>();

			if (IsAvailable<RamWatch>()) // Just because we attempted to load it, doesn't mean it was, the current core may not have the correct dependencies
			{
				if (_config.RecentWatches.AutoLoad && !_config.RecentWatches.Empty)
				{
					RamWatch.LoadFileFromRecent(_config.RecentWatches.MostRecent);
				}

				if (!loadDialog)
				{
					Get<RamWatch>().Close();
				}
			}
		}

		public string GenerateDefaultCheatFilename()
		{
			var path = _config.PathEntries.CheatsAbsolutePath(_game.System);

			var f = new FileInfo(path);
			if (f.Directory != null && f.Directory.Exists == false)
			{
				f.Directory.Create();
			}

			return Path.Combine(path, $"{_game.FilesystemSafeName()}.cht");
		}

		public void UpdateCheatRelatedTools(object sender, CheatCollection.CheatListEventArgs e)
		{
			if (!_emulator.HasMemoryDomains())
			{
				return;
			}

			UpdateValues<RamWatch>();
			UpdateValues<RamSearch>();
			UpdateValues<HexEditor>();
			UpdateValues<Cheats>();

			_owner.UpdateCheatStatus();
		}
	}
}
