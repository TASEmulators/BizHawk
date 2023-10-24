using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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
	public class ToolManager : ToolManagerBase, IToolManager
	{
		private readonly MainForm _ownerForm;

		// TODO: merge ToolHelper code where logical
		// For instance, add an IToolForm property called UsesCheats, so that a UpdateCheatRelatedTools() method can update all tools of this type
		// Also a UsesRam, and similar method
		private readonly List<IToolForm> _tools = new List<IToolForm>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolManager"/> class.
		/// </summary>
		public ToolManager(
			MainForm owner,
			Config config,
			DisplayManagerBase displayManager,
			ExternalToolManager extToolManager,
			InputManager inputManager,
			IEmulator emulator,
			IMovieSession movieSession,
			IGameInfo game) : base(owner, owner, config, displayManager, extToolManager, inputManager, emulator, movieSession, game)
		{
			_ownerForm = owner;
		}

		// If the form inherits ToolFormBase, it will set base properties such as Tools, Config, etc
		protected override void SetBaseProperties(IToolForm form)
		{
			if (form is not FormBase f) return;

			f.Config = _config;
			if (form is not ToolFormBase tool) return;
			tool.SetToolFormBaseProps(_displayManager, _inputManager, _mainFormTools, _movieSession, this, _game);
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
			form.Owner = settings.FloatingWindow ? null : _ownerForm;
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

		protected override void AttachSettingHooks(IToolFormAutoConfig tool, ToolDialogSettings settings)
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
				throw new InvalidOperationException($"{nameof(IToolFormAutoConfig)} must have menu to bind to! (need {nameof(Form.MainMenuStrip)} or other {nameof(MenuStrip)} w/ menu labelled \"Settings\")");
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
				form.Owner = val ? null : _ownerForm;
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

				form.GetType().GetMethodsWithAttrib(typeof(RestoreDefaultsAttribute))
					.FirstOrDefault()?.Invoke(form, Array.Empty<object>());
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
					else if (val is not bool && prop.PropertyType.IsPrimitive)
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

		public override bool IsOnScreen(Point topLeft)
		{
			return Screen.AllScreens.Any(
				screen => screen.WorkingArea.Contains(topLeft));
		}

		internal static readonly IDictionary<Type, (Image/*?*/ Icon, string Name)> IconAndNameCache = new Dictionary<Type, (Image/*?*/ Icon, string Name)>
		{
			[typeof(LogWindow)] = (LogWindow.ToolIcon.ToBitmap(), "Log Window"), // can't do this lazily, see https://github.com/TASEmulators/BizHawk/issues/2741#issuecomment-1421014589
		};

		private static PropertyInfo/*?*/ _PInfo_FormBase_WindowTitleStatic = null;

		private static PropertyInfo PInfo_FormBase_WindowTitleStatic
			=> _PInfo_FormBase_WindowTitleStatic ??= typeof(FormBase).GetProperty("WindowTitleStatic", BindingFlags.NonPublic | BindingFlags.Instance);

		protected override bool CaptureIconAndName(object tool, Type toolType, ref Image/*?*/ icon, ref string/*?*/ name)
		{
			if (IconAndNameCache.ContainsKey(toolType)) return true;
			Form winform = null;
			if (name is null)
			{
				winform = tool as FormBase;
				if (winform is not null)
				{
					// then `tool is Formbase` and this getter call is safe
					name = (string) PInfo_FormBase_WindowTitleStatic.GetValue(tool);
					// could do `tool._windowTitleStatic ??= tool.WindowTitleStatic`, but the getter's only being run 1 extra time here anyway so not worth the LOC
				}
				winform ??= tool as Form;
				if (winform is not null)
				{
					icon = winform.Icon?.ToBitmap();
					name ??= winform.Name;
				}
			}
			if (!string.IsNullOrWhiteSpace(name))
			{
				IconAndNameCache[toolType] = (icon, name);
				return true;
			}
			// else don't cache anything
			name = winform?.Text;
			return false;
		}

		private void CaptureIconAndName(object tool, Type toolType)
		{
			Image/*?*/ icon = null;
			string/*?*/ name = null;
			CaptureIconAndName(tool, toolType, ref icon, ref name);
		}

		public override (Image/*?*/ Icon, string Name) GetIconAndNameFor(Type toolType)
		{
			if (IconAndNameCache.TryGetValue(toolType, out var tuple)) return tuple;
			Image/*?*/ icon = null;
			var name = toolType.GetCustomAttribute<SpecializedToolAttribute>()?.DisplayName; //TODO codegen ToolIcon and WindowTitleStatic from [Tool] or some new attribute -- Bitmap..ctor(Type, string)
			var instance = LazyGet(toolType);
			if (instance is not null)
			{
				if (CaptureIconAndName(instance, toolType, ref icon, ref name)) return (icon, name);
				// else fall through
			}
			return (
				icon ?? (toolType.GetProperty("ToolIcon", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as Icon)?.ToBitmap(),
				string.IsNullOrWhiteSpace(name) ? toolType.Name : name);
		}

		public override IEnumerable<Type> AvailableTools => EmuHawk.ReflectionCache.Types
			.Where(t => !t.IsInterface && typeof(IToolForm).IsAssignableFrom(t) && IsAvailable(t));

		protected override void MaybeClearCheats()
		{
			if (!Has<Cheats>())
			{
				_mainFormTools.CheatList.NewList(GenerateDefaultCheatFilename(), autosave: true);
			}
		}

		protected override IExternalToolForm CreateInstanceFrom(string dllPath, string toolTypeName)
		{
			return Activator.CreateInstanceFrom(dllPath, toolTypeName ?? "BizHawk.Client.EmuHawk.CustomMainForm").Unwrap() as IExternalToolForm;
		}

		protected override IList<string> PossibleToolTypeNames { get; } = EmuHawk.ReflectionCache.Types.Select(t => t.AssemblyQualifiedName).ToList();

		public RamWatch RamWatch => GetTool<RamWatch>();

		public RamSearch RamSearch => GetTool<RamSearch>();

		public HexEditor HexEditor => GetTool<HexEditor>();

		public VirtualpadTool VirtualPad => GetTool<VirtualpadTool>();

		public SNESGraphicsDebugger SNESGraphicsDebugger => GetTool<SNESGraphicsDebugger>();

		public LuaConsole LuaConsole => GetTool<LuaConsole>();

		public TAStudio TAStudio => GetTool<TAStudio>();

		public override void LoadRamWatch(bool loadDialog)
		{
			if (IsLoaded<RamWatch>() && !_config.DisplayRamWatch)
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

		public override void UpdateCheatRelatedTools(object sender, CheatCollection.CheatListEventArgs e)
		{
			if (!_emulator.HasMemoryDomains())
			{
				return;
			}

			UpdateValues<RamWatch>();
			UpdateValues<RamSearch>();
			UpdateValues<HexEditor>();
			UpdateValues<Cheats>();

			_ownerForm.UpdateCheatStatus();
		}

		protected override void SetFormParent(IToolForm form)
		{
			if (form is Form formform) formform.Owner = _ownerForm;
		}

		protected override void SetFormClosingEvent(IToolForm form, Action action)
		{
			((Form)form).FormClosing += (o, e) => action();
		}
	}
}
