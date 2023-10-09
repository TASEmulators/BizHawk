using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public abstract class ToolManagerBase : IToolManager
	{
		protected readonly IMainFormForTools _mainFormTools;
		private readonly IMainFormForApi _mainFormApi;
		protected Config _config;
		protected readonly DisplayManagerBase _displayManager;
		private readonly ExternalToolManager _extToolManager;
		protected readonly InputManager _inputManager;
		private IExternalApiProvider _apiProvider;
		protected IEmulator _emulator;
		protected readonly IMovieSession _movieSession;
		protected IGameInfo _game;

		// TODO: merge ToolHelper code where logical
		// For instance, add an IToolForm property called UsesCheats, so that a UpdateCheatRelatedTools() method can update all tools of this type
		// Also a UsesRam, and similar method
		private readonly List<IToolForm> _tools = new List<IToolForm>();

		private IExternalApiProvider ApiProvider
		{
			get => _apiProvider;
			set => _mainFormTools.EmuClient = (EmuClientApi)(_apiProvider = value).GetApi<IEmuClientApi>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolManagerBase"/> class.
		/// </summary>
		public ToolManagerBase(
			IMainFormForTools owner,
			IMainFormForApi mainFormApi,
			Config config,
			DisplayManagerBase displayManager,
			ExternalToolManager extToolManager,
			InputManager inputManager,
			IEmulator emulator,
			IMovieSession movieSession,
			IGameInfo game)
		{
			_mainFormTools = owner;
			_mainFormApi = mainFormApi;
			_config = config;
			_displayManager = displayManager;
			_extToolManager = extToolManager;
			_inputManager = inputManager;
			_emulator = emulator;
			_movieSession = movieSession;
			_game = game;
			ApiProvider = ApiManager.Restart(_emulator.ServiceProvider, _mainFormApi, _displayManager, _inputManager, _movieSession, this, _config, _emulator, _game);
		}

		/// <summary>
		/// Loads the tool dialog T (T must implements <see cref="IToolForm"/>) , if it does not exist it will be created, if it is already open, it will be focused
		/// This method should be used only if you can't use the generic one
		/// </summary>
		/// <param name="toolType">Type of tool you want to load</param>
		/// <param name="focus">Define if the tool form has to get the focus or not (Default is true)</param>
		/// <returns>An instantiated <see cref="IToolForm"/></returns>
		/// <exception cref="ArgumentException">Raised if <paramref name="toolType"/> can't cast into IToolForm </exception>
		public IToolForm Load(Type toolType, bool focus = true)
		{
			if (!typeof(IToolForm).IsAssignableFrom(toolType))
			{
				throw new ArgumentException(message: $"Type {toolType.Name} does not implement {nameof(IToolForm)}.", paramName: nameof(toolType));
			}
			var mi = typeof(ToolManagerBase).GetMethod(nameof(Load), new[] { typeof(bool), typeof(string) })!.MakeGenericMethod(toolType);
			return (IToolForm)mi.Invoke(this, new object[] { focus, "" });
		}

		// If the form inherits ToolFormBase, it will set base properties such as Tools, Config, etc
		protected abstract void SetBaseProperties(IToolForm form);


		protected abstract void SetFormParent(IToolForm form);

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
				if (existingTool.IsLoaded)
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

			if (CreateInstance<T>(toolPath) is not T newTool) return null;

			SetFormParent(newTool);
			if (!ServiceInjector.UpdateServices(_emulator.ServiceProvider, newTool)) return null; //TODO pass `true` for `mayCache` when from EmuHawk assembly
			SetBaseProperties(newTool);
			var toolTypeName = typeof(T).FullName!;
			// auto settings
			if (newTool is IToolFormAutoConfig autoConfigTool)
			{
				AttachSettingHooks(autoConfigTool, _config.CommonToolSettings.GetValueOrPutNew(toolTypeName));
			}
			// custom settings
			if (HasCustomConfig(newTool))
			{
				InstallCustomConfig(newTool, _config.CustomToolSettings.GetValueOrPutNew(toolTypeName));
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
				if (existingTool.IsActive)
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

			var newTool = (IExternalToolForm)CreateInstance(typeof(IExternalToolForm), toolPath, customFormTypeName, skipExtToolWarning: skipExtToolWarning);
			if (newTool == null) return null;
			SetFormParent(newTool);
			if (!(ServiceInjector.UpdateServices(_emulator.ServiceProvider, newTool) && ApiInjector.UpdateApis(ApiProvider, newTool))) return null;
			SetBaseProperties(newTool);
			// auto settings
			if (newTool is IToolFormAutoConfig autoConfigTool)
			{
				AttachSettingHooks(autoConfigTool, _config.CommonToolSettings.GetValueOrPutNew(customFormTypeName));
			}
			// custom settings
			if (HasCustomConfig(newTool))
			{
				InstallCustomConfig(newTool, _config.CustomToolSettings.GetValueOrPutNew(customFormTypeName));
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
					if (!IsLoaded(t))
					{
						Load(t, false);
					}
				}
			}
		}

		protected abstract void AttachSettingHooks(IToolFormAutoConfig tool, ToolDialogSettings settings);

		private static bool HasCustomConfig(IToolForm tool)
		{
			return tool.GetType().GetPropertiesWithAttrib(typeof(ConfigPersistAttribute)).Any();
		}

		protected abstract void SetFormClosingEvent(IToolForm form, Action action);

		private void InstallCustomConfig(IToolForm tool, Dictionary<string, object> data)
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
					else if (val is not bool && prop.PropertyType.IsPrimitive)
					{
						// numeric constants are similarly hosed
						val = Convert.ChangeType(val, prop.PropertyType, CultureInfo.InvariantCulture);
					}

					prop.SetValue(tool, val, null);
				}
			}

			SetFormClosingEvent(tool, () => SaveCustomConfig(tool, data, props));
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
		/// <remarks>yo why do we have 4 versions of this, each with slightly different behaviour in edge cases --yoshi</remarks>
		public bool IsLoaded<T>() where T : IToolForm
			=> _tools.OfType<T>().FirstOrDefault()?.IsActive is true;

		public bool IsLoaded(Type toolType)
			=> _tools.Find(t => t.GetType() == toolType)?.IsActive is true;

		public abstract bool IsOnScreen(Point topLeft);

		/// <summary>
		/// Returns true if an instance of T exists
		/// </summary>
		/// <typeparam name="T">Type of tool to check</typeparam>
		public bool Has<T>() where T : IToolForm
			=> _tools.Exists(static t => t is T && t.IsActive);

		/// <returns><see langword="true"/> iff a tool of the given <paramref name="toolType"/> is <see cref="IToolForm.IsActive">active</see></returns>
		public bool Has(Type toolType)
			=> typeof(IToolForm).IsAssignableFrom(toolType)
				&& _tools.Exists(t => toolType.IsInstanceOfType(t) && t.IsActive);

		/// <summary>
		/// Gets the instance of T, or creates and returns a new instance
		/// </summary>
		/// <typeparam name="T">Type of tool to get</typeparam>
		public IToolForm Get<T>() where T : class, IToolForm
		{
			return Load<T>(false);
		}

		/// <summary>
		/// returns the instance of <paramref name="toolType"/>, regardless of whether it's loaded,<br/>
		/// but doesn't create and load a new instance if it's not found
		/// </summary>
		/// <remarks>
		/// does not check <paramref name="toolType"/> is a class implementing <see cref="IToolForm"/>;<br/>
		/// you may pass any class or interface
		/// </remarks>
		public IToolForm/*?*/ LazyGet(Type toolType)
			=> _tools.Find(t => toolType.IsAssignableFrom(t.GetType()));

		private static PropertyInfo/*?*/ _PInfo_FormBase_WindowTitleStatic = null;

		protected abstract bool CaptureIconAndName(object tool, Type toolType, ref Image/*?*/ icon, ref string/*?*/ name);

		private void CaptureIconAndName(object tool, Type toolType)
		{
			Image/*?*/ icon = null;
			string/*?*/ name = null;
			CaptureIconAndName(tool, toolType, ref icon, ref name);
		}

		public abstract (Image/*?*/ Icon, string Name) GetIconAndNameFor(Type toolType);

		public abstract IEnumerable<Type> AvailableTools { get; }

		/// <summary>
		/// Calls UpdateValues() on an instance of T, if it exists
		/// </summary>
		/// <typeparam name="T">Type of tool to update</typeparam>
		public void UpdateValues<T>() where T : IToolForm
		{
			var tool = _tools.OfType<T>().FirstOrDefault();
			if (tool?.IsActive is true)
			{
				tool.UpdateValues(ToolFormUpdateType.General);
			}
		}

		protected abstract void MaybeClearCheats();

		public void Restart(Config config, IEmulator emulator, IGameInfo game)
		{
			_config = config;
			_emulator = emulator;
			_game = game;
			ApiProvider = ApiManager.Restart(_emulator.ServiceProvider, _mainFormApi, _displayManager, _inputManager, _movieSession, this, _config, _emulator, _game);

			MaybeClearCheats();

			var unavailable = new List<IToolForm>();

			foreach (var tool in _tools)
			{
				SetBaseProperties(tool);
				if (ServiceInjector.UpdateServices(_emulator.ServiceProvider, tool)
					&& (tool is not IExternalToolForm || ApiInjector.UpdateApis(ApiProvider, tool)))
				{
					if (tool.IsActive) tool.Restart();
				}
				else
				{
					unavailable.Add(tool);
					if (tool is IExternalToolForm) ApiInjector.ClearApis(tool);
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
			=> _tools.OfType<T>().FirstOrDefault()?.Restart();

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
			var tool = _tools.OfType<T>().FirstOrDefault();
			if (tool != null)
			{
				tool.Close();
				_tools.Remove(tool);
			}
		}

		public void Close(Type toolType)
		{
			var tool = _tools.Find(toolType.IsInstanceOfType);
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

		protected abstract IExternalToolForm CreateInstanceFrom(string dllPath, string toolTypeName);

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
					if (!_mainFormTools.ShowMessageBox2(
						"Are you sure want to load this external tool?\r\nAccept ONLY if you trust the source and if you know what you're doing. In any other case, choose no.",
						"Confirm loading",
						EMsgBoxIcon.Question))
					{
						return null;
					}
				}

				try
				{
					//tool = Activator.CreateInstanceFrom(dllPath, toolTypeName ?? "BizHawk.Client.EmuHawk.CustomMainForm").Unwrap() as IExternalToolForm;
					tool = CreateInstanceFrom(dllPath, toolTypeName);
					if (tool == null)
					{
						_mainFormTools.ShowMessageBox($"It seems that the object CustomMainForm does not implement {nameof(IExternalToolForm)}. Please review the code.", "No, no, no. Wrong Way !", EMsgBoxIcon.Warning);
						return null;
					}
				}
				catch (MissingMethodException)
				{
					_mainFormTools.ShowMessageBox("It seems that the object CustomMainForm does not have a public default constructor. Please review the code.", "No, no, no. Wrong Way !", EMsgBoxIcon.Warning);
					return null;
				}
				catch (TypeLoadException)
				{
					_mainFormTools.ShowMessageBox("It seems that the object CustomMainForm does not exists. Please review the code.", "No, no, no. Wrong Way !", EMsgBoxIcon.Warning);
					return null;
				}
			}
			else
			{
				tool = (IToolForm)Activator.CreateInstance(toolType);
			}
			CaptureIconAndName(tool, toolType);
			// Add to our list of tools
			_tools.Add(tool);
			return tool;
		}

		public void UpdateToolsBefore()
		{
			foreach (var tool in _tools)
			{
				if (tool.IsActive)
				{
					tool.UpdateValues(ToolFormUpdateType.PreFrame);
				}
			}
		}

		public void UpdateToolsAfter()
		{
			foreach (var tool in _tools)
			{
				if (tool.IsActive)
				{
					tool.UpdateValues(ToolFormUpdateType.PostFrame);
				}
			}
		}

		public void FastUpdateBefore()
		{
			foreach (var tool in _tools)
			{
				if (tool.IsActive)
				{
					tool.UpdateValues(ToolFormUpdateType.FastPreFrame);
				}
			}
		}

		public void FastUpdateAfter()
		{
			foreach (var tool in _tools)
			{
				if (tool.IsActive)
				{
					tool.UpdateValues(ToolFormUpdateType.FastPostFrame);
				}
			}
		}

		protected abstract IList<string> PossibleToolTypeNames { get; }

		public bool IsAvailable(Type tool)
		{
			if (!ServiceInjector.IsAvailable(_emulator.ServiceProvider, tool)) return false;
			if (typeof(IExternalToolForm).IsAssignableFrom(tool) && !ApiInjector.IsAvailable(ApiProvider, tool)) return false;
			if (!PossibleToolTypeNames.Contains(tool.AssemblyQualifiedName) && !_extToolManager.PossibleExtToolTypeNames.Contains(tool.AssemblyQualifiedName)) return false; // not a tool

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

		protected T GetTool<T>() where T : class, IToolForm, new()
		{
			T tool = _tools.OfType<T>().FirstOrDefault();
			if (tool != null)
			{
				if (tool.IsActive)
				{
					return tool;
				}

				_tools.Remove(tool);
			}
			tool = new T();
			CaptureIconAndName(tool, typeof(T));
			_tools.Add(tool);
			return tool;
		}

		public abstract void LoadRamWatch(bool loadDialog);

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

		public abstract void UpdateCheatRelatedTools(object sender, CheatCollection.CheatListEventArgs e);
	}
}
