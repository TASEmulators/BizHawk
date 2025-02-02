using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Security.AccessControl;
using System.Security.Principal;
using System.IO.Pipes;

using BizHawk.Bizware.Graphics;
using BizHawk.Bizware.Input;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.PathExtensions;
using BizHawk.Common.StringExtensions;

using BizHawk.Client.Common;
using BizHawk.Client.Common.cheats;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.AppleII;
using BizHawk.Emulation.Cores.Computers.Commodore64;
using BizHawk.Emulation.Cores.Computers.DOS;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Consoles.SNK;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;

using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Client.EmuHawk.CoreExtensions;
using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Common.CollectionExtensions;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm : FormBase, IDialogParent, IMainFormForApi, IMainFormForTools, IMainFormForRetroAchievements
	{
		private const string FMT_STR_DUMP_STATUS_MENUITEM_LABEL = "Dump Status Report{0}...";

		private static readonly FilesystemFilterSet EmuHawkSaveStatesFSFilterSet = new(FilesystemFilter.EmuHawkSaveStates);

		private static readonly FilesystemFilterSet LibretroCoresFSFilterSet = new(
			appendAllFilesEntry: false,
			new FilesystemFilter("Libretro Cores", extensions: [ (OSTailoredCode.IsUnixHost ? "so" : "dll") ]));

		private const int WINDOW_SCALE_MAX = 10;

		private readonly ToolStripMenuItemEx DOSSubMenu = new() { Text = "&DOS" };

		private readonly ToolStripMenuItemEx DumpStatusReportMenuItem = new()
		{
			Enabled = false,
			Text = string.Format(FMT_STR_DUMP_STATUS_MENUITEM_LABEL, string.Empty),
		};

		private readonly ToolStripMenuItemEx NullHawkVSysSubmenu = new() { Enabled = false, Text = "—" };

		private readonly ToolStripMenuItemEx RealTimeCounterMenuItem = new() { Enabled = false, Text = "00:00.0" };

		private readonly StatusLabelEx StatusBarMuteIndicator = new();

		private readonly StatusLabelEx StatusBarRewindIndicator = new()
		{
			Image = Properties.Resources.RewindRecord,
			ToolTipText = "Rewinder is capturing states",
		};

		private void MainForm_Load(object sender, EventArgs e)
		{
			UpdateWindowTitle();

			Slot1StatusButton.Tag = SelectSlot1MenuItem.Tag = 1;
			Slot2StatusButton.Tag = SelectSlot2MenuItem.Tag = 2;
			Slot3StatusButton.Tag = SelectSlot3MenuItem.Tag = 3;
			Slot4StatusButton.Tag = SelectSlot4MenuItem.Tag = 4;
			Slot5StatusButton.Tag = SelectSlot5MenuItem.Tag = 5;
			Slot6StatusButton.Tag = SelectSlot6MenuItem.Tag = 6;
			Slot7StatusButton.Tag = SelectSlot7MenuItem.Tag = 7;
			Slot8StatusButton.Tag = SelectSlot8MenuItem.Tag = 8;
			Slot9StatusButton.Tag = SelectSlot9MenuItem.Tag = 9;
			Slot0StatusButton.Tag = SelectSlot0MenuItem.Tag = 10;

			DumpStatusReportMenuItem.Click += DumpStatusButton_Click;
			EmulationSubMenu.DropDownItems.InsertBefore(LoadedCoreNameMenuItem, insert: DumpStatusReportMenuItem);
			EmulationSubMenu.DropDownItems.InsertBefore(LoadedCoreNameMenuItem, insert: RealTimeCounterMenuItem);

			{
				for (int i = 1; i <= WINDOW_SCALE_MAX; i++)
				{
					long quotient = Math.DivRem(i, 10, out long remainder);
					var temp = new ToolStripMenuItemEx
					{
						Tag = i,
						Text = $"{(quotient is not 0L ? quotient.ToString() : string.Empty)}&{remainder}x",
					};
					temp.Click += this.WindowSize_Click;
					WindowSizeSubMenu.DropDownItems.Insert(i - 1, temp);
				}
			}

#if BIZHAWKBUILD_SUPERHAWK
			ToolStripMenuItemEx superHawkThrottleMenuItem = new() { Text = "SUPER·HAWK" };
			superHawkThrottleMenuItem.Click += (_, _) => Config.SuperHawkThrottle = !Config.SuperHawkThrottle;
			_ = SpeedSkipSubMenu.DropDownItems.InsertBefore(MinimizeSkippingMenuItem, insert: superHawkThrottleMenuItem);
			ConfigSubMenu.DropDownOpened += (_, _) => superHawkThrottleMenuItem.Checked = Config.SuperHawkThrottle;
#endif

			foreach (var (appliesTo, coreNames) in Config.CorePickerUIData)
			{
				var submenu = new ToolStripMenuItem { Text = string.Join(" | ", appliesTo) };
				submenu.DropDownItems.AddRange(coreNames.Select(coreName => {
					var entry = new ToolStripMenuItem { Text = coreName };
					entry.Click += (_, _) =>
					{
						string currentCoreName = Emulator.Attributes().CoreName;
						if (coreName != currentCoreName && coreNames.Contains(currentCoreName)) FlagNeedsReboot();
						foreach (string system in appliesTo)
							Config.PreferredCores[system] = coreName;
					};
					return (ToolStripItem) entry;
				}).ToArray());
				submenu.DropDownOpened += (openedSender, _1) =>
				{
					_ = Config.PreferredCores.TryGetValue(appliesTo[0], out var preferred);
					if (!coreNames.Contains(preferred))
					{
						// invalid --> default (doing this here rather than when reading config file to allow for hacked-in values, though I'm not sure if that could do anything at the moment --yoshi)
						var defaultCore = coreNames[0];
						Console.WriteLine($"setting preferred core for {submenu.Text} to {defaultCore} (was {preferred ?? "null"})");
						preferred = defaultCore;
						foreach (var sysID in appliesTo) Config.PreferredCores[sysID] = preferred;
					}
					foreach (ToolStripMenuItem entry in ((ToolStripMenuItem) openedSender).DropDownItems) entry.Checked = entry.Text == preferred;
				};
				CoresSubMenu.DropDownItems.Add(submenu);
			}
			CoresSubMenu.DropDownItems.Add(new ToolStripSeparator { AutoSize = true });
			var GBInSGBMenuItem = new ToolStripMenuItem { Text = "GB in SGB" };
			GBInSGBMenuItem.Click += (_, _) =>
			{
				Config.GbAsSgb = !Config.GbAsSgb;
				if (Emulator.SystemId is VSystemID.Raw.GB or VSystemID.Raw.GBC or VSystemID.Raw.SGB) FlagNeedsReboot();
			};
			CoresSubMenu.DropDownItems.Add(GBInSGBMenuItem);
			var setLibretroCoreToolStripMenuItem = new ToolStripMenuItem { Text = "Set Libretro Core..." };
			setLibretroCoreToolStripMenuItem.Click += (_, _) => RunLibretroCoreChooser();
			CoresSubMenu.DropDownItems.Add(setLibretroCoreToolStripMenuItem);
			CoresSubMenu.DropDownOpened += (_, _) => GBInSGBMenuItem.Checked = Config.GbAsSgb;

			ToolStripMenuItemEx recentCoreSettingsSubmenu = new() { Text = "Recent" };
			recentCoreSettingsSubmenu.DropDownItems.AddRange(CreateCoreSettingsSubmenus().ToArray());
			ToolStripMenuItemEx noRecentsItem = new() { Enabled = false, Text = "(N/A)" };
			recentCoreSettingsSubmenu.DropDownItems.Add(noRecentsItem);
			recentCoreSettingsSubmenu.DropDownOpening += (_, _) =>
			{
				foreach (ToolStripItem submenu in recentCoreSettingsSubmenu.DropDownItems) submenu.Visible = Config.RecentCores.Contains(submenu.Text);
				noRecentsItem.Visible = Config.RecentCores.Count is 0;
			};
			ToolStripMenuItemEx consolesCoreSettingsSubmenu = new() { Text = "For Consoles" };
			ToolStripMenuItemEx handheldsCoreSettingsSubmenu = new() { Text = "For Handhelds" };
			ToolStripMenuItemEx pcsCoreSettingsSubmenu = new() { Text = "For Computers" };
			ToolStripMenuItemEx otherCoreSettingsSubmenu = new() { Text = "Other" };
			foreach (var submenu in CreateCoreSettingsSubmenus(includeDupes: true).OrderBy(submenu => submenu.Text))
			{
				var parentMenu = (VSystemCategory) submenu.Tag switch
				{
					VSystemCategory.Consoles => consolesCoreSettingsSubmenu,
					VSystemCategory.Handhelds => handheldsCoreSettingsSubmenu,
					VSystemCategory.PCs => pcsCoreSettingsSubmenu,
					_ => otherCoreSettingsSubmenu,
				};
				parentMenu.DropDownItems.Add(submenu);
			}
			foreach (var submenu in new[] { consolesCoreSettingsSubmenu, handheldsCoreSettingsSubmenu, pcsCoreSettingsSubmenu, otherCoreSettingsSubmenu })
			{
				if (submenu.DropDownItems.Count is 0)
				{
					submenu.DropDownItems.Add(new ToolStripMenuItemEx { Text = "(none)" });
					submenu.Enabled = false;
				}
			}
			_ = ConfigSubMenu.DropDownItems.InsertAfter(
				CoresSubMenu,
				insert: new ToolStripMenuItemEx
				{
					DropDownItems =
					{
						recentCoreSettingsSubmenu,
						new ToolStripSeparatorEx { AutoSize = true },
						consolesCoreSettingsSubmenu,
						handheldsCoreSettingsSubmenu,
						pcsCoreSettingsSubmenu,
						otherCoreSettingsSubmenu,
					},
					Text = "Core Settings",
				});

			_ = MainformMenu.Items.InsertAfter(ToolsSubMenu, insert: NullHawkVSysSubmenu);

			ToolStripMenuItemEx DOSSettingsToolStripMenuItem = new() { Text = "Settings..." };
			DOSSettingsToolStripMenuItem.Click += (_, _) => _ = OpenDOSBoxSettingsDialog();
			DOSSubMenu.DropDownItems.Add(DOSSettingsToolStripMenuItem);
			ToolStripMenuItemEx DOSExportHDDImageToolStripMenuItem = new() { Text = "Export Hard Disk Drive..." };
			DOSExportHDDImageToolStripMenuItem.Click += DOSSExportHddMenuItem_Click;
			DOSSubMenu.DropDownItems.Add(DOSExportHDDImageToolStripMenuItem);
			DOSSubMenu.DropDownOpened += (_, _) => DOSExportHDDImageToolStripMenuItem.Enabled = Emulator is DOSBox dosbox && dosbox.HasValidHDD();
			_ = MainformMenu.Items.InsertAfter(NullHawkVSysSubmenu, insert: DOSSubMenu);

			// Hide Status bar icons and general StatusBar prep
			MainStatusBar.Padding = new Padding(MainStatusBar.Padding.Left, MainStatusBar.Padding.Top, MainStatusBar.Padding.Left, MainStatusBar.Padding.Bottom); // Workaround to remove extra padding on right
			PlayRecordStatusButton.Visible = false;

			StatusBarRewindIndicator.Click += RewindOptionsMenuItem_Click;
			MainStatusBar.Items.InsertAfter(PlayRecordStatusButton, insert: StatusBarRewindIndicator);
			UpdateStatusBarRewindIndicator();

			AVStatusLabel.Visible = false;
			SetPauseStatusBarIcon();
			Tools.UpdateCheatRelatedTools(null, new(null));
			RebootStatusBarIcon.Visible = false;
			UpdateNotification.Visible = false;
			_statusBarDiskLightOnImage = Properties.Resources.LightOn;
			_statusBarDiskLightOffImage = Properties.Resources.LightOff;
			_linkCableOn = Properties.Resources.Connect16X16;
			_linkCableOff = Properties.Resources.NoConnect16X16;
			UpdateCoreStatusBarButton();
			if (Config.FirstBoot)
			{
				ProfileFirstBootLabel.Visible = true;
				AddOnScreenMessage("Click the blue silhouette below for onboarding", duration: 30);
			}

			HandleToggleLightAndLink();
			SetStatusBar();
			_stateSlots.Update(Emulator, MovieSession.Movie, SaveStatePrefix());

			var quickslotButtons = new[]
			{
				Slot1StatusButton, Slot2StatusButton, Slot3StatusButton, Slot4StatusButton, Slot5StatusButton,
				Slot6StatusButton, Slot7StatusButton, Slot8StatusButton, Slot9StatusButton, Slot0StatusButton,
			};
			for (var i = 0; i < quickslotButtons.Length; i++)
			{
				ref var button = ref quickslotButtons[i];
				button.MouseEnter += SlotStatusButtons_MouseEnter;
				button.MouseLeave += SlotStatusButtons_MouseLeave;
			}

			StatusBarMuteIndicator.Click += (_, _) => ToggleSound();
			MainStatusBar.Items.InsertBefore(KeyPriorityStatusLabel, insert: StatusBarMuteIndicator);
			UpdateStatusBarMuteIndicator();

			if (OSTailoredCode.IsUnixHost)
			{
				// workaround for https://github.com/mono/mono/issues/12644
				MainFormContextMenu.Items.Insert(0, new ToolStripMenuItemEx { Text = "(Dismiss Menu)" }); // don't even need to attach any behaviour, since clicking anything will dismiss the menu first
				MainFormContextMenu.Items.Insert(1, new ToolStripSeparatorEx());
			}

			// New version notification
			UpdateChecker.CheckComplete += (s2, e2) =>
			{
				if (IsDisposed)
				{
					return;
				}

				this.BeginInvoke(() => { UpdateNotification.Visible = UpdateChecker.IsNewVersionAvailable; });
			};
			UpdateChecker.GlobalConfig = Config;
			UpdateChecker.BeginCheck(); // Won't actually check unless enabled by user

			// open requested ext. tool
			var requestedExtToolDll = _argParser.openExtToolDll;
			if (requestedExtToolDll != null)
			{
				var found = ExtToolManager.ToolStripItems.Where(static item => item.Enabled)
					.Select(static item => (ExternalToolManager.MenuItemInfo) item.Tag)
					.FirstOrNull(info => info.AsmFilename == requestedExtToolDll
						|| Path.GetFileName(info.AsmFilename) == requestedExtToolDll
						|| Path.GetFileNameWithoutExtension(info.AsmFilename) == requestedExtToolDll);
				if (found is not null) found.Value.TryLoad();
				else Console.WriteLine($"requested ext. tool dll {requestedExtToolDll} could not be loaded");
			}

#if DEBUG
			AddDebugMenu();
#endif
		}

		static MainForm()
		{
			// If this isn't here, then our assembly resolving hacks wont work due to the check for MainForm.INTERIM
			// its.. weird. don't ask.
		}

		public CoreComm CreateCoreComm()
		{
			var cfp = new CoreFileProvider(
				this,
				FirmwareManager,
				Config.PathEntries,
				Config.FirmwareUserSpecifications);
			var prefs = CoreComm.CorePreferencesFlags.None;
			if (Config.SkipWaterboxIntegrityChecks)
				prefs = CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck;

			// can't pass self as IDialogParent :(
			return new CoreComm(
				message => this.ModalMessageBox(message, "Warning", EMsgBoxIcon.Warning),
				AddOnScreenMessage,
				cfp,
				prefs,
				new OpenGLProvider());
		}

		private void SetImages()
		{
			OpenRomMenuItem.Image = Properties.Resources.OpenFile;
			RecentRomSubMenu.Image = Properties.Resources.Recent;
			CloseRomMenuItem.Image = Properties.Resources.Close;
			PreviousSlotMenuItem.Image = Properties.Resources.MoveLeft;
			NextSlotMenuItem.Image = Properties.Resources.MoveRight;
			ReadonlyMenuItem.Image = Properties.Resources.ReadOnly;
			RecentMovieSubMenu.Image = Properties.Resources.Recent;
			RecordMovieMenuItem.Image = Properties.Resources.Record;
			PlayMovieMenuItem.Image = Properties.Resources.Play;
			StopMovieMenuItem.Image = Properties.Resources.Stop;
			PlayFromBeginningMenuItem.Image = Properties.Resources.Restart;
			ImportMoviesMenuItem.Image = Properties.Resources.Import;
			SaveMovieMenuItem.Image = Properties.Resources.SaveAs;
			SaveMovieAsMenuItem.Image = Properties.Resources.SaveAs;
			StopMovieWithoutSavingMenuItem.Image = Properties.Resources.Stop;
			RecordAVMenuItem.Image = Properties.Resources.Record;
			ConfigAndRecordAVMenuItem.Image = Properties.Resources.Avi;
			StopAVMenuItem.Image = Properties.Resources.Stop;
			ScreenshotMenuItem.Image = Properties.Resources.Camera;
			PauseMenuItem.Image = Properties.Resources.Pause;
			RebootCoreMenuItem.Image = Properties.Resources.Reboot;
			SwitchToFullscreenMenuItem.Image = Properties.Resources.Fullscreen;
			ControllersMenuItem.Image = Properties.Resources.GameController;
			HotkeysMenuItem.Image = Properties.Resources.HotKeys;
			DisplayConfigMenuItem.Image = Properties.Resources.TvIcon;
			SoundMenuItem.Image = Properties.Resources.Audio;
			PathsMenuItem.Image = Properties.Resources.CopyFolder;
			FirmwareMenuItem.Image = Properties.Resources.Pcb;
			MessagesMenuItem.Image = Properties.Resources.MessageConfig;
			AutofireMenuItem.Image = Properties.Resources.Lightning;
			RewindOptionsMenuItem.Image = Properties.Resources.Previous;
			ProfilesMenuItem.Image = Properties.Resources.Profile;
			SaveConfigMenuItem.Image = Properties.Resources.Save;
			LoadConfigMenuItem.Image = Properties.Resources.LoadConfig;
			(ToolBoxMenuItem.Image, /*ToolBoxMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(ToolBox)]
				= (/*ToolBox.ToolIcon.ToBitmap()*/Properties.Resources.ToolBox, "Tool Box");
			(RamWatchMenuItem.Image, /*RamWatchMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(RamWatch)]
				= (/*RamWatch.ToolIcon.ToBitmap()*/Properties.Resources.Watch, "RAM Watch");
			(RamSearchMenuItem.Image, /*RamSearchMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(RamSearch)]
				= (/*RamSearch.ToolIcon.ToBitmap()*/Properties.Resources.Search, "RAM Search");
			(LuaConsoleMenuItem.Image, /*LuaConsoleMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(LuaConsole)]
				= (/*LuaConsole.ToolIcon.ToBitmap()*/Properties.Resources.TextDoc, "Lua Console");
			(TAStudioMenuItem.Image, /*TAStudioMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(TAStudio)]
				= (/*TAStudio.ToolIcon.ToBitmap()*/Properties.Resources.TAStudio, "TAStudio");
			(HexEditorMenuItem.Image, /*HexEditorMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(HexEditor)]
				= (/*HexEditor.ToolIcon.ToBitmap()*/Properties.Resources.Poke, "Hex Editor");
			(TraceLoggerMenuItem.Image, /*TraceLoggerMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(TraceLogger)]
				= (/*TraceLogger.ToolIcon.ToBitmap()*/Properties.Resources.Pencil, "Trace Logger");
			(DebuggerMenuItem.Image, /*DebuggerMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(GenericDebugger)]
				= (/*GenericDebugger.ToolIcon.ToBitmap()*/Properties.Resources.Bug, "Debugger");
			(CodeDataLoggerMenuItem.Image, /*CodeDataLoggerMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(CDL)]
				= (/*CDL.ToolIcon.ToBitmap()*/Properties.Resources.CdLogger, "Code Data Logger");
			(VirtualPadMenuItem.Image, /*VirtualPadMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(VirtualpadTool)]
				= (/*VirtualpadTool.ToolIcon.ToBitmap()*/Properties.Resources.GameController, "Virtual Pads");
			(BasicBotMenuItem.Image, /*BasicBotMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(BasicBot)]
				= (/*BasicBot.ToolIcon.ToBitmap()*/Properties.Resources.BasicBotBit, "Basic Bot");
			(CheatsMenuItem.Image, /*CheatsMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(Cheats)]
				= (/*Cheats.ToolIcon.ToBitmap()*/Properties.Resources.Cheat, "Cheats");
			(GameSharkConverterMenuItem.Image, /*GameSharkConverterMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(GameShark)]
				= (/*GameShark.ToolIcon.ToBitmap()*/Properties.Resources.Shark, "Cheat Code Converter");
			(MultiDiskBundlerFileMenuItem.Image, /*MultiDiskBundlerFileMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(MultiDiskBundler)]
				= (/*MultiDiskBundler.ToolIcon.ToBitmap()*/Properties.Resources.SaveConfig, "Multi-disk Bundler");
			NesControllerSettingsMenuItem.Image = Properties.Resources.GameController;
			NESGraphicSettingsMenuItem.Image = Properties.Resources.TvIcon;
			NESSoundChannelsMenuItem.Image = Properties.Resources.Audio;
			(KeypadMenuItem.Image, /*KeypadMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(TI83KeyPad)]
				= (/*TI83KeyPad.ToolIcon.ToBitmap()*/Properties.Resources.Calculator, "TI-83 Virtual KeyPad");
			PSXControllerSettingsMenuItem.Image = Properties.Resources.GameController;
			SNESControllerConfigurationMenuItem.Image = Properties.Resources.GameController;
			(SnesGfxDebuggerMenuItem.Image, /*SnesGfxDebuggerMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(SNESGraphicsDebugger)]
				= (/*SNESGraphicsDebugger.ToolIcon.ToBitmap()*/Properties.Resources.Bug, "Graphics Debugger");
			ColecoControllerSettingsMenuItem.Image = Properties.Resources.GameController;
			IntVControllerSettingsMenuItem.Image = Properties.Resources.GameController;
			OnlineHelpMenuItem.Image = Properties.Resources.Help;
			ForumsMenuItem.Image = Properties.Resources.TAStudio;
			(FeaturesMenuItem.Image, /*FeaturesMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(CoreFeatureAnalysis)]
				= (/*CoreFeatureAnalysis.ToolIcon.ToBitmap()*/Properties.Resources.KitchenSink, "Core Features");
			AboutMenuItem.Image = Properties.Resources.CorpHawkSmall;
			DumpStatusButton.Image = Properties.Resources.Blank;
			PlayRecordStatusButton.Image = Properties.Resources.Blank;
			PauseStatusButton.Image = Properties.Resources.Blank;
			RebootStatusBarIcon.Image = Properties.Resources.Reboot;
			AVStatusLabel.Image = Properties.Resources.Blank;
			LedLightStatusLabel.Image = Properties.Resources.LightOff;
			KeyPriorityStatusLabel.Image = Properties.Resources.Both;
			CoreNameStatusBarButton.Image = Properties.Resources.CorpHawkSmall;
			ProfileFirstBootLabel.Image = Properties.Resources.Profile;
			LinkConnectStatusBarButton.Image = Properties.Resources.Connect16X16;
			OpenRomContextMenuItem.Image = Properties.Resources.OpenFile;
			LoadLastRomContextMenuItem.Image = Properties.Resources.Recent;
			StopAVContextMenuItem.Image = Properties.Resources.Stop;
			RecordMovieContextMenuItem.Image = Properties.Resources.Record;
			PlayMovieContextMenuItem.Image = Properties.Resources.Play;
			RestartMovieContextMenuItem.Image = Properties.Resources.Restart;
			StopMovieContextMenuItem.Image = Properties.Resources.Stop;
			LoadLastMovieContextMenuItem.Image = Properties.Resources.Recent;
			StopNoSaveContextMenuItem.Image = Properties.Resources.Stop;
			SaveMovieContextMenuItem.Image = Properties.Resources.SaveAs;
			SaveMovieAsContextMenuItem.Image = Properties.Resources.SaveAs;
			UndoSavestateContextMenuItem.Image = Properties.Resources.Undo;
			toolStripMenuItem6.Image = Properties.Resources.GameController;
			toolStripMenuItem7.Image = Properties.Resources.HotKeys;
			toolStripMenuItem8.Image = Properties.Resources.TvIcon;
			toolStripMenuItem9.Image = Properties.Resources.Audio;
			toolStripMenuItem10.Image = Properties.Resources.CopyFolder;
			toolStripMenuItem11.Image = Properties.Resources.Pcb;
			toolStripMenuItem12.Image = Properties.Resources.MessageConfig;
			toolStripMenuItem13.Image = Properties.Resources.Lightning;
			toolStripMenuItem14.Image = Properties.Resources.Previous;
			toolStripMenuItem66.Image = Properties.Resources.Save;
			toolStripMenuItem67.Image = Properties.Resources.LoadConfig;
			ScreenshotContextMenuItem.Image = Properties.Resources.Camera;
			CloseRomContextMenuItem.Image = Properties.Resources.Close;
		}

		public MainForm(
			ParsedCLIFlags cliFlags,
			IGL gl,
			Func<string> getConfigPath,
			Func<Config> getGlobalConfig,
			Action<Sound> updateGlobalSound,
			string[] args,
			out IMovieSession movieSession,
			out bool exitEarly)
		{
			movieSession = null;
			exitEarly = false;

			_getGlobalConfig = getGlobalConfig;

			if (Config.SingleInstanceMode)
			{
				if (SingleInstanceInit(args))
				{
					exitEarly = true;
					return;
				}
			}

			_argParser = cliFlags;
			_getConfigPath = getConfigPath;
			GL = gl;
			_updateGlobalSound = updateGlobalSound;

			InputManager = new InputManager
			{
				GetMainFormMouseInfo = () =>
				{
					var b = Control.MouseButtons;
					return (
						Control.MousePosition,
						MouseWheelTracker,
						(b & MouseButtons.Left) != 0,
						(b & MouseButtons.Middle) != 0,
						(b & MouseButtons.Right) != 0,
						(b & MouseButtons.XButton1) != 0,
						(b & MouseButtons.XButton2) != 0
					);
				},
			};
			FirmwareManager = new FirmwareManager();
			movieSession = MovieSession = new MovieSession(
				Config.Movies,
				Config.PathEntries.MovieBackupsAbsolutePath(),
				this,
				PauseEmulator,
				SetMainformMovieInfo,
				() => Sound.PlayWavFile(Properties.Resources.GetNotHawkCallSFX(), Config.SoundVolume / 100f));

			void MainForm_MouseClick(object sender, MouseEventArgs e)
			{
				AutohideCursor(hide: false);
				if (Config.ShowContextMenu && e.Button == MouseButtons.Right)
				{
					// suppress the context menu if right click has a binding
					// (unless shift is being pressed, similar to double click fullscreening)
					var allowSuppress = ModifierKeys != Keys.Shift;
					if (allowSuppress && InputManager.ActiveController.HasBinding("WMouse R"))
					{
						return;
					}

					MainFormContextMenu.Show(PointToScreen(new Point(e.X, e.Y + MainformMenu.Height)));
				}
			}
			void MainForm_MouseMove(object sender, MouseEventArgs e) => AutohideCursor(hide: false, alwaysUpdate: false);
			void MainForm_MouseWheel(object sender, MouseEventArgs e) => MouseWheelTracker += e.Delta;
			MouseClick += MainForm_MouseClick;
			MouseMove += MainForm_MouseMove;

			InitializeComponent();
			Icon = Properties.Resources.Logo;
			SetImages();
#if !DEBUG
			ZXSpectrumExportSnapshotMenuItemMenuItem.Enabled = false;
			ZXSpectrumExportSnapshotMenuItemMenuItem.Visible = false;
#endif
#if AVI_SUPPORT
			SynclessRecordingMenuItem.Click += (_, _) => new SynclessRecordingTools(Config, Game, this).Run();
#else
			SynclessRecordingMenuItem.Enabled = false;
#endif

			Game = GameInfo.NullInstance;
			_throttle = new Throttle();
			Emulator = new NullEmulator();

			UpdateStatusSlots();
			UpdateKeyPriorityIcon();

			// TODO GL - a lot of disorganized wiring-up here
			_presentationPanel = new(
				Config,
				GL,
				ToggleFullscreen,
				MainForm_MouseClick,
				MainForm_MouseMove,
				MainForm_MouseWheel);

			DisplayManager = new(Config, Emulator, InputManager, MovieSession, GL, _presentationPanel, () => DisableSecondaryThrottling);
			Controls.Add(_presentationPanel);
			Controls.SetChildIndex(_presentationPanel, 0);

			// set up networking before ApiManager (in ToolManager)
			byte[] NetworkingTakeScreenshot()
				=> (byte[]) new ImageConverter().ConvertTo(MakeScreenshotImage().ToSysdrawingBitmap(), typeof(byte[]));
			NetworkingHelpers = (
				_argParser.HTTPAddresses is var (httpGetURL, httpPostURL)
					? new HttpCommunication(NetworkingTakeScreenshot, httpGetURL, httpPostURL)
					: null,
				new MemoryMappedFiles(NetworkingTakeScreenshot, _argParser.MMFFilename),
				_argParser.SocketAddress is var (socketIP, socketPort)
					? new SocketServer(NetworkingTakeScreenshot, _argParser.SocketProtocol, socketIP, socketPort)
					: null
			);

			ExtToolManager = new(
				Config,
				() => (Emulator.SystemId, Game.Hash),
				(toolPath, customFormTypeName, skipExtToolWarning) => Tools!.LoadExternalToolForm(
					toolPath: toolPath,
					customFormTypeName: customFormTypeName,
					skipExtToolWarning: skipExtToolWarning) is not null);
			Tools = new ToolManager(this, Config, DisplayManager, ExtToolManager, InputManager, Emulator, MovieSession, Game);

			// TODO GL - move these event handlers somewhere less obnoxious line in the On* overrides
			Load += (o, e) =>
			{
				AllowDrop = true;
				DragEnter += FormDragEnter;
				DragDrop += FormDragDrop;
			};

			Closing += CheckMayCloseAndCleanup;

			ResizeBegin += (o, e) =>
			{
				_inResizeLoop = true;

				if (!OSTailoredCode.IsUnixHost)
				{
					Sound?.StopSound();
				}
			};

			Resize += (_, _) => UpdateWindowTitle();

			ResizeEnd += (o, e) =>
			{
				_inResizeLoop = false;
				UpdateWindowTitle();

				if (!OSTailoredCode.IsUnixHost)
				{
					Sound?.StartSound();
				}
			};

			_presentationPanel.Control.Move += (_, _) =>
			{
				if (Config.CaptureMouse)
				{
					CaptureMouse(false);
					CaptureMouse(true);
				}
			};

			_presentationPanel.Control.Resize += (_, _) =>
			{
				if (Config.CaptureMouse)
				{
					CaptureMouse(false);
					CaptureMouse(true);
				}
			};

			if (!Config.GCAdapterSupportEnabled)
			{
				// annoyingly this isn't an SDL hint in SDL2, only in SDL3, have to use an environment variables to signal this
				Environment.SetEnvironmentVariable("SDL_HIDAPI_DISABLE_LIBUSB", "1");
			}

			Input.Instance = new Input(
				Handle,
				() => Config,
				() => ActiveForm switch
				{
					null => Config.AcceptBackgroundInput // none of our forms are focused, check the background input config
						? Config.AcceptBackgroundInputControllerOnly
							? AllowInput.OnlyController
							: AllowInput.All
						: AllowInput.None,
					FormBase { BlocksInputWhenFocused: false, MenuIsOpen: false } => AllowInput.All,
					ControllerConfig => AllowInput.All,
					HotkeyConfig => AllowInput.All,
					LuaWinform { BlocksInputWhenFocused: false } => AllowInput.All,
					IExternalToolForm => AllowInput.None,
					_ => Config.AcceptBackgroundInput ? AllowInput.OnlyController : AllowInput.None,
				}
			);
			InitControls();

			var savedOutputMethod = Config.SoundOutputMethod;
			if (savedOutputMethod is ESoundOutputMethod.Dummy) Config.SoundOutputMethod = HostCapabilityDetector.HasXAudio2 ? ESoundOutputMethod.XAudio2 : ESoundOutputMethod.OpenAL;
			try
			{
				Sound = new Sound(Config, () => Emulator.VsyncRate());
			}
			catch
			{
				if (savedOutputMethod is not ESoundOutputMethod.Dummy)
				{
					ShowMessageBox(
						owner: null,
						text: "Couldn't initialize sound device! Try changing the output method in Sound config.",
						caption: "Initialization Error",
						EMsgBoxIcon.Error);
				}
				Config.SoundOutputMethod = ESoundOutputMethod.Dummy;
				Sound = new Sound(Config, () => Emulator.VsyncRate());
			}

			Sound.StartSound();
			InputManager.SyncControls(Emulator, MovieSession, Config);
			CheatList = new CheatCollection(this, Config.Cheats);
			CheatList.Changed += Tools.UpdateCheatRelatedTools;
			RewireSound();

			if (Config.SaveWindowPosition)
			{
				if (Config.MainWindowPosition is Point position)
				{
					Location = position;
				}

				if (Config.MainWindowSize is Size size && !Config.ResizeWithFramebuffer)
				{
					Size = size;
				}

				if (Config.MainWindowMaximized)
				{
					WindowState = FormWindowState.Maximized;
				}
			}

			if (Config.MainFormStayOnTop) TopMost = true;

			if (_argParser.cmdRom != null)
			{
				// Commandline should always override auto-load
				var ioa = OpenAdvancedSerializer.ParseWithLegacy(_argParser.cmdRom);
				_ = LoadRom(ioa.SimplePath, new LoadRomArgs(ioa));
				if (Game.IsNullInstance())
				{
					ShowMessageBox(owner: null, $"Failed to load {_argParser.cmdRom} specified on commandline");
				}
			}
			else if (Config.RecentRoms.AutoLoad && !Config.RecentRoms.Empty)
			{
				LoadMostRecentROM();
			}

			Config.VideoWriterAudioSyncEffective = _argParser.audiosync ?? Config.VideoWriterAudioSync;
			_autoDumpLength = _argParser._autoDumpLength;
			if (_argParser.cmdMovie != null)
			{
				_suppressSyncSettingsWarning = true; // We don't want to be nagged if we are attempting to automate
				if (Game.IsNullInstance())
				{
					OpenRom();
				}

				// If user picked a game, then do the commandline logic
				if (!Game.IsNullInstance())
				{
					var movie = MovieSession.Get(_argParser.cmdMovie, true);
					MovieSession.ReadOnly = true;

					// if user is dumping and didn't supply dump length, make it as long as the loaded movie
					if (_autoDumpLength == 0)
					{
						_autoDumpLength = movie.InputLogLength;
					}

					// Copy pasta from drag & drop
					if (MovieImport.IsValidMovieExtension(Path.GetExtension(_argParser.cmdMovie)))
					{
						ProcessMovieImport(_argParser.cmdMovie, true);
					}
					else
					{
						StartNewMovie(movie, false);
						Config.RecentMovies.Add(_argParser.cmdMovie);
					}

					_suppressSyncSettingsWarning = false;
				}
			}
			else if (Config.RecentMovies.AutoLoad && !Config.RecentMovies.Empty)
			{
				if (Game.IsNullInstance())
				{
					OpenRom();
				}

				// If user picked a game, then do the autoload logic
				if (!Game.IsNullInstance())
				{
					if (File.Exists(Config.RecentMovies.MostRecent))
					{
						StartNewMovie(MovieSession.Get(Config.RecentMovies.MostRecent, true), false);
					}
					else
					{
						Config.RecentMovies.HandleLoadError(this, Config.RecentMovies.MostRecent);
					}
				}
			}

			if (_argParser.startFullscreen || Config.StartFullscreen)
			{
				_needsFullscreenOnLoad = true;
			}

			if (!Game.IsNullInstance())
			{
				if (_argParser.cmdLoadState != null)
				{
					_ = LoadState(
						path: _argParser.cmdLoadState,
						userFriendlyStateName: Path.GetFileName(_argParser.cmdLoadState));
				}
				else if (_argParser.cmdLoadSlot != null)
				{
					_ = LoadQuickSave(_argParser.cmdLoadSlot.Value);
				}
				else if (Config.AutoLoadLastSaveSlot)
				{
					_ = LoadstateCurrentSlot();
				}
			}

			if (_argParser.UserdataUnparsedPairs is {} pairs) foreach (var (k, v) in pairs)
			{
				MovieSession.UserBag[k] = v switch
				{
					"true" => true,
					"false" => false,
					_ when int.TryParse(v, out var i) => i,
					_ when double.TryParse(v, out var d) => d,
					_ => v,
				};
			}

			Shown += (_, _) =>
			{
				//start Lua Console if requested in the command line arguments
				if (_argParser.luaConsole)
				{
					OpenLuaConsole();
				}
				//load Lua Script if requested in the command line arguments
				if (_argParser.luaScript != null)
				{
					Tools.LuaConsole.LoadFromCommandLine(_argParser.luaScript.MakeAbsolute());
				}
			};

			SetStatusBar();

			if (Config.StartPaused)
			{
				PauseEmulator();
			}

			// start dumping, if appropriate
			if (_argParser.cmdDumpType != null && _argParser.cmdDumpName != null)
			{
				RecordAv(_argParser.cmdDumpType, _argParser.cmdDumpName);
			}

			SetMainformMovieInfo();

			SynchChrome();

			if (Config.RAAutostart)
			{
				OpenRetroAchievements();
			}

			_presentationPanel.Control.Paint += (o, e) =>
			{
				// I would like to trigger a repaint here, but this isn't done yet
			};

			if (!Config.SkipOutdatedOsCheck && OSTailoredCode.HostWindowsVersion is not null)
			{
				var (winVersion, win10PlusVersion) = OSTailoredCode.HostWindowsVersion.Value;
				var message = winVersion switch
				{
					OSTailoredCode.WindowsVersion._11 when win10PlusVersion! < new Version(10, 0, 22621) => $"Quick reminder: Your copy of Windows 11 (build {win10PlusVersion.Build}) is no longer supported by Microsoft.\nEmuHawk will probably continue working, but please update to 24H2 for increased security.",
					OSTailoredCode.WindowsVersion._11 => null,
					OSTailoredCode.WindowsVersion._10 when win10PlusVersion! < new Version(10, 0, 19045) => $"Quick reminder: Your copy of Windows 10 (build {win10PlusVersion.Build}) is no longer supported by Microsoft.\nEmuHawk will probably continue working, but please update to 22H2 for increased security.",
					OSTailoredCode.WindowsVersion._10 => null,
					_ => $"Quick reminder: Windows {winVersion.ToString().RemovePrefix('_').Replace('_', '.')} is no longer supported by Microsoft.\nEmuHawk will probably continue working, but please get a new operating system for increased security (either Windows 10+ or a GNU+Linux distro).",
				};
				if (message is not null)
				{
#if DEBUG
				Console.WriteLine(message);
#else
				Load += (_, _) => Config.SkipOutdatedOsCheck = this.ShowMessageBox2($"{message}\n\nSkip this reminder from now on?");
#endif
				}
			}
		}

		private void CheckMayCloseAndCleanup(object/*?*/ closingSender, CancelEventArgs closingArgs)
		{
			if (_currAviWriter is not null)
			{
				if (!this.ModalMessageBox2(
					caption: "Really quit?",
					icon: EMsgBoxIcon.Question,
					text: "You are currently recording A/V.\nChoose \"Yes\" to finalise it and quit EmuHawk.\nChoose \"No\" to cancel shutdown and continue recording."))
				{
					closingArgs.Cancel = true;
					return;
				}
				StopAv();
			}

			if (!Tools.AskSave())
			{
				closingArgs.Cancel = true;
				return;
			}

			Tools.Close();
			MovieSession.StopMovie();
			// zero 03-nov-2015 - close game after other steps. tools might need to unhook themselves from a core.
			CloseGame();
			SaveConfig();
		}

		private readonly bool _suppressSyncSettingsWarning;

		public override bool BlocksInputWhenFocused { get; } = false;

		/// <summary>
		/// Windows does tool stip menu focus things when Alt is released, not pressed.
		/// However, if an alt combination is pressed then those things happen at that time instead.
		/// So we need to know if a key combination was used, so we can skip the alt release logic.
		/// </summary>
		private bool _skipNextAltRelease = true;

		public int ProgramRunLoop()
		{
			// needs to be done late, after the log console snaps on top
			// fullscreen should snap on top even harder!
			if (_needsFullscreenOnLoad)
			{
				_needsFullscreenOnLoad = false;
				ToggleFullscreen();
			}

			CaptureMouse(Config.CaptureMouse);

			// incantation required to get the program reliably on top of the console window
			// we might want it in ToggleFullscreen later, but here, it needs to happen regardless
			BringToFront();
			Activate();
			BringToFront();

			InitializeFpsData();

			for (; ; )
			{
				Input.Instance.Update();

				// handle events and dispatch as a hotkey action, or a hotkey button, or an input button
				// ...but prepare haptics first, those get read in ProcessInput
				var finalHostController = InputManager.ControllerInputCoalescer;
				InputManager.ActiveController.PrepareHapticsForHost(finalHostController);
				Input.Instance.Adapter.SetHaptics(finalHostController.GetHapticsSnapshot());

				InputManager.ProcessInput(Input.Instance, CheckHotkey, Config, (ie, handled) =>
				{
					if (ActiveForm is not FormBase afb) return;

					// Alt key for menu items.
					if (ie.EventType is InputEventType.Press && (ie.LogicalButton.Modifiers & LogicalButton.MASK_ALT) is not 0U)
					{
						// Windows will not focus the menu if any other key was pressed while Alt is held. Regardless of whether that key did anything.
						_skipNextAltRelease = true;
						if (handled) return;

						if (ie.LogicalButton.Button.Length == 1)
						{
							var c = ie.LogicalButton.Button.ToLowerInvariant()[0];
							afb.SendAltCombination(c);
						}
						else if (ie.LogicalButton.Button == "Space")
						{
							afb.SendAltCombination(' ');
						}
					}
					else if (handled) return;
					else if (ie.EventType is InputEventType.Press && ie.LogicalButton.Button == "Alt")
					{
						// We will only do the alt release if the alt press itself was not already handled.
						_skipNextAltRelease = false;
					}
					else if (ie.EventType is InputEventType.Release
						&& !afb.BlocksInputWhenFocused
						&& ie.LogicalButton.Button == "Alt"
						&& !_skipNextAltRelease)
					{
						afb.FocusToolStipMenu();
					}

					// same as right-click
					if (ie.ToString() == "Press:Apps" && Config.ShowContextMenu && ContainsFocus)
					{
						MainFormContextMenu.Show(PointToScreen(new(0, MainformMenu.Height)));
					}
				});

				// translate mouse coordinates
				// NOTE: these must go together, because in the case of screen rotation, X and Y are transformed together
				{
					var p = DisplayManager.UntransformPoint(new Point(
						finalHostController.AxisValue("WMouse X"),
						finalHostController.AxisValue("WMouse Y")));
					var x = p.X / (float)_currentVideoProvider.BufferWidth;
					var y = p.Y / (float)_currentVideoProvider.BufferHeight;
					finalHostController.AcceptNewAxis("WMouse X", (int)((x * 20000) - 10000));
					finalHostController.AcceptNewAxis("WMouse Y", (int)((y * 20000) - 10000));
				}

				InputManager.RunControllerChain(Config);

				// emu.yield()'ing scripts
				if (Tools.Has<LuaConsole>())
				{
					Tools.LuaConsole.ResumeScripts(false);
				}
				// ext. tools don't yield per se, so just send them a GeneralUpdate
				Tools.GeneralUpdateActiveExtTools();

				StepRunLoop_Core();
				Render();
				StepRunLoop_Throttle();

				// HACK: RAIntegration might peek at memory during messages
				// we need this to allow memory access here, otherwise it will deadlock
				var raMemHack = (RA as RAIntegration)?.ThisIsTheRAMemHack();
				raMemHack?.Enter();

				CheckMessages();

				// RA == null possibly due MainForm Dispose disposing RA (which case Exit is not valid anymore)
				// RA != null possibly due to RA object being created (which case raMemHack is null, as RA was null before)
				if (RA is not null) raMemHack?.Exit();

				if (_exitRequestPending)
				{
					_exitRequestPending = false;
					Close();
				}

				if (IsDisposed || _windowClosedAndSafeToExitProcess)
				{
					break;
				}
			}

			Shutdown();
			return _exitCode;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			// NOTE: this gets called twice sometimes. once by using() in Program.cs and once from winforms internals when the form is closed...
			DisplayManager?.Dispose();
			DisplayManager = null;

			RA?.Dispose();
			RA = null;

			if (disposing)
			{
				components?.Dispose();
				_presentationPanel?.Dispose();
				SingleInstanceDispose();
			}

			if (OSTailoredCode.IsUnixHost)
			{
				if (_x11Display != IntPtr.Zero)
				{
					for (var i = 0; i < 4; i++)
					{
						if (_pointerBarriers[i] != IntPtr.Zero)
						{
							XfixesImports.XFixesDestroyPointerBarrier(_x11Display, _pointerBarriers[i]);
							_pointerBarriers[i] = IntPtr.Zero;
						}
					}

					_ = XlibImports.XCloseDisplay(_x11Display);
					_x11Display = IntPtr.Zero;
				}
			}

			base.Dispose(disposing);
		}

		private bool _emulatorPaused;
		public bool EmulatorPaused
		{
			get => _emulatorPaused;

			private set
			{
				_didMenuPause = false; // overwritten where relevant
				if (_emulatorPaused == value) return;
				if (_emulatorPaused && !value) // Unpausing
				{
					InitializeFpsData();
				}

				if (value != _emulatorPaused) Tools.OnPauseToggle(value);
				_emulatorPaused = value;
			}
		}

		public bool BlockFrameAdvance { get; set; }

		public string CurrentlyOpenRom { get; private set; } // todo - delete me and use only args instead
		public LoadRomArgs CurrentlyOpenRomArgs { get; private set; }
		public bool PauseAvi { get; set; }
		public bool PressFrameAdvance { get; set; }
		public bool FrameInch { get; set; }
		public bool HoldFrameAdvance { get; set; } // necessary for tastudio > button
		public bool PressRewind { get; set; } // necessary for tastudio < button
		public bool FastForward { get; set; }

		/// <summary>
		/// Disables updates for video/audio, and enters "turbo" mode.
		/// Can be used to replicate Gens-rr's "latency compensation" that involves:
		/// <list type="bullet">
		/// <item><description>Saving a no-framebuffer state that is stored in RAM</description></item>
		/// <item><description>Emulating forth for some frames with updates disabled</description></item>
		/// <item><description><list type="bullet">
		/// <item><description>Optionally hacking in-game memory
		/// (like camera position, to show off-screen areas)</description></item>
		/// </list></description></item>
		/// <item><description>Updating the screen</description></item>
		/// <item><description>Loading the no-framebuffer state from RAM</description></item>
		/// </list>
		/// The most common use case is CamHack for Sonic games.
		/// Accessing this from Lua allows to keep internal code hacks to minimum.
		/// <list type="bullet">
		/// <item><description><see cref="ClientLuaLibrary.InvisibleEmulation(bool)"/></description></item>
		/// </list>
		/// </summary>
		public bool InvisibleEmulation { get; set; }

		private long MouseWheelTracker;

		private int? _pauseOnFrame;
		public int? PauseOnFrame // If set, upon completion of this frame, the client wil pause
		{
			get => _pauseOnFrame;

			set
			{
				bool wasTurboSeeking = IsTurboSeeking;
				_pauseOnFrame = value;
				SetPauseStatusBarIcon();

				if (wasTurboSeeking && value == null) // TODO: make an Event handler instead, but the logic here is that after turbo seeking, tools will want to do a real update when the emulator finally pauses
				{
					// Tools.UpdateToolsBefore(); // TODO: do we need this?
					Tools.UpdateToolsAfter();
				}
			}
		}

		public bool IsSeeking => PauseOnFrame.HasValue;
		private bool IsTurboSeeking => PauseOnFrame.HasValue && Config.TurboSeek;
		public bool IsTurboing => InputManager.ClientControls["Turbo"] || IsTurboSeeking;
		public bool IsFastForwarding => InputManager.ClientControls["Fast Forward"] || IsTurboing || InvisibleEmulation;
		public bool IsRewinding { get; private set; }

		/// <summary>
		/// Used to disable secondary throttling (e.g. vsync, audio) for unthrottled modes or when the primary (clock) throttle is taking over (e.g. during fast forward/rewind).
		/// </summary>
		public static bool DisableSecondaryThrottling { get; set; }

		public void AddOnScreenMessage(string message, [LiteralExpected] int? duration = null)
		{
#pragma warning disable CS0618 // this is the sanctioned call-site
			OSD.AddMessage(message, duration);
#pragma warning restore CS0618
			if (!this.SafeScreenReaderAnnounce(message)) Util.DebugWriteLine($"{nameof(AddOnScreenMessage)}: {nameof(AccessibleObject.RaiseAutomationNotification)} failed");
		}

		public void ClearHolds()
		{
			if (Tools.Has<VirtualpadTool>())
			{
				Tools.VirtualPad.ClearVirtualPadHolds();
			}
			else
			{
				InputManager.StickyHoldController.ClearStickies();
				InputManager.StickyAutofireController.ClearStickies();
			}
		}

		public void FlagNeedsReboot()
		{
			RebootStatusBarIcon.Visible = true;
			AddOnScreenMessage("Core reboot needed for this setting");
		}

		/// <remarks>don't use this, use <see cref="Emulator"/></remarks>
		private IEmulator _emulator;

		public IEmulator Emulator
		{
			get => _emulator;

			private set
			{
				_emulator = value;
				_currentVideoProvider = value.AsVideoProviderOrDefault();
				_currentSoundProvider = value.AsSoundProviderOrDefault();
			}
		}

		public event BeforeQuickLoadEventHandler QuicksaveLoad;

		public event BeforeQuickSaveEventHandler QuicksaveSave;

		public event EventHandler RomLoaded;

		public event StateLoadedEventHandler SavestateLoaded;

		public event StateSavedEventHandler SavestateSaved;

		private readonly InputManager InputManager;

		private IVideoProvider _currentVideoProvider = NullVideo.Instance;

		private ISoundProvider _currentSoundProvider = new NullSound(44100 / 60); // Reasonable default until we have a core instance

		/// <remarks>don't use this, use <see cref="Config"/></remarks>
		private readonly Func<Config> _getGlobalConfig;

		private new Config Config => _getGlobalConfig();

		public Action<string> LoadGlobalConfigFromFile { get; set; }

		private readonly Func<string> _getConfigPath;

		private readonly IGL GL;

		internal readonly ExternalToolManager ExtToolManager;

		public readonly ToolManager Tools;

		private IControlMainform ToolControllingSavestates => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToControlSavestates);
		private IControlMainform ToolControllingRewind => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToControlRewind);
		private IControlMainform ToolControllingReboot => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToControlReboot);
		private IControlMainform ToolControllingStopMovie => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToControlStopMovie);
		private IControlMainform ToolControllingRestartMovie => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToControlRestartMovie);
		private IControlMainform ToolControllingReadOnly => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToControlReadOnly);
		private IControlMainform ToolBypassingMovieEndAction => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToBypassMovieEndAction);

		private DisplayManager DisplayManager;

		private OSDManager OSD => DisplayManager.OSD;

		public IMovieSession MovieSession { get; }

		public GameInfo Game { get; private set; }

		/// <remarks>don't use this, use <see cref="Sound"/></remarks>
		private Sound _sound;

		private readonly Action<Sound> _updateGlobalSound;

		private Sound Sound
		{
			get => _sound;
			set => _updateGlobalSound(_sound = value);
		}

		public CheatCollection CheatList { get; }

		public IDecodeResult DecodeCheatForAPI(string code, out MemoryDomain/*?*/ domain)
		{
			domain = null;
			if (!Emulator.HasMemoryDomains()) return new InvalidCheatCode($"cheat codes not supported by the current system: {Emulator.SystemId}");
			try
			{
				GameSharkDecoder decoder = new(Emulator.AsMemoryDomains(), Emulator.SystemId);
				var result = decoder.Decode(code);
				if (result.IsValid(out var valid))
				{
					domain = decoder.CheatDomain(valid);
				}

				return result;
			}
			catch (Exception e)
			{
				return new InvalidCheatCode(e.ToString());
			}
		}

		public (HttpCommunication HTTP, MemoryMappedFiles MMF, SocketServer Sockets) NetworkingHelpers { get; }

		public IRewinder Rewinder { get; private set; }

		public void CreateRewinder()
		{
			if (ToolControllingRewind is not null) return;

			Rewinder?.Dispose();
			Rewinder = Emulator.HasSavestates() && Config.Rewind.Enabled && (!Emulator.AsStatable().AvoidRewind || Config.Rewind.AllowSlowStates)
				? Config.Rewind.UseDelta
					? new ZeldaWinder(Emulator.AsStatable(), Config.Rewind)
					: new Zwinder(Emulator.AsStatable(), Config.Rewind)
				: null;
			UpdateStatusBarRewindIndicator();
			AddOnScreenMessage(Rewinder?.Active == true ? "Rewind started" : "Rewind disabled");
		}

		public FirmwareManager FirmwareManager { get; }

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			Input.Instance.ControlInputFocus(this, HostInputType.Mouse, true);

			if (Config.CaptureMouse)
			{
				CaptureMouse(false);
				CaptureMouse(true);
			}
		}

		protected override void OnDeactivate(EventArgs e)
		{
			Input.Instance.ControlInputFocus(this, HostInputType.Mouse, false);

			if (Config.CaptureMouse)
			{
				CaptureMouse(false);
			}

			base.OnDeactivate(e);
		}

		public bool RebootCore()
		{
			if (ToolControllingReboot is { } tool)
			{
				tool.RebootCore();
				return true;
			}
			else
			{
				if (CurrentlyOpenRomArgs == null) return true;
				return LoadRom(
					CurrentlyOpenRomArgs.OpenAdvanced.SimplePath,
					CurrentlyOpenRomArgs with { ForcedSysID = Emulator.SystemId });
			}
		}

		public void PauseEmulator()
		{
			EmulatorPaused = true;
			SetPauseStatusBarIcon();
		}

		public void UnpauseEmulator()
		{
			EmulatorPaused = false;
			SetPauseStatusBarIcon();
		}

		public void TogglePause()
		{
			EmulatorPaused = !EmulatorPaused;
			SetPauseStatusBarIcon();

			// TODO: have tastudio set a pause status change callback, or take control over pause
			if (Tools.Has<TAStudio>())
			{
				Tools.UpdateValues<TAStudio>();
			}
		}

		public void TakeScreenshotToClipboard()
		{
			using var bb = Config.ScreenshotCaptureOsd ? CaptureOSD() : MakeScreenshotImage();
			bb.ToSysdrawingBitmap().ToClipBoard();
			AddOnScreenMessage("Screenshot (raw) saved to clipboard.");
		}

		private void TakeScreenshotClientToClipboard()
		{
			using var bb = DisplayManager.RenderOffscreen(_currentVideoProvider, Config.ScreenshotCaptureOsd);
			bb.ToSysdrawingBitmap().ToClipBoard();
			AddOnScreenMessage("Screenshot (client) saved to clipboard.");
		}

		private string ScreenshotPrefix()
		{
			var screenPath = Config.PathEntries.ScreenshotAbsolutePathFor(Game.System);
			var name = Game.FilesystemSafeName();
			return Path.Combine(screenPath, name);
		}

		public void TakeScreenshot()
		{
			var basename = $"{ScreenshotPrefix()}.{DateTime.Now:yyyy-MM-dd HH.mm.ss}";

			var fnameBare = $"{basename}.png";
			var fname = $"{basename} (0).png";

			// if the (0) filename exists, do nothing. we'll bump up the number later
			// if the bare filename exists, move it to (0)
			// otherwise, no related filename exists, and we can proceed with the bare filename
			if (!File.Exists(fname))
			{
				if (File.Exists(fnameBare)) File.Move(fnameBare, fname);
				else fname = fnameBare;
			}

			for (var seq = 0; File.Exists(fname); seq++)
				fname = $"{basename} ({seq}).png";

			TakeScreenshot(fname);
		}

		public void TakeScreenshot(string path)
		{
			var fi = new FileInfo(path);
			fi.Directory?.Create();
			using (var bb = Config.ScreenshotCaptureOsd ? CaptureOSD() : MakeScreenshotImage())
			{
				using var img = bb.ToSysdrawingBitmap();
				if (".JPG".EqualsIgnoreCase(Path.GetExtension(path)))
				{
					img.Save(fi.FullName, ImageFormat.Jpeg);
				}
				else
				{
					img.Save(fi.FullName, ImageFormat.Png);
				}
			}

			AddOnScreenMessage($"{fi.Name} saved.");
		}

		public void FrameBufferResized(bool forceWindowResize = false)
		{
			if (WindowState is not FormWindowState.Normal)
			{
				// Wait until no longer maximized/minimized to get correct size/location values
				_framebufferResizedPending = true;
				return;
			}
			if (!Config.ResizeWithFramebuffer && !forceWindowResize)
			{
				return;
			}
			// run this entire thing exactly twice, since the first resize may adjust the menu stacking
			void DoPresentationPanelResize()
			{
				int zoom = Config.GetWindowScaleFor(Emulator.SystemId);
				var area = Screen.FromControl(this).WorkingArea;

				int borderWidth = Size.Width - _presentationPanel.Control.Size.Width;
				int borderHeight = Size.Height - _presentationPanel.Control.Size.Height;

				// start at target zoom and work way down until we find acceptable zoom
				Size lastComputedSize = new Size(1, 1);
				for (; zoom >= 1; zoom--)
				{
					lastComputedSize = DisplayManager.CalculateClientSize(_currentVideoProvider, zoom);
					if (lastComputedSize.Width + borderWidth < area.Width
						&& lastComputedSize.Height + borderHeight < area.Height)
					{
						break;
					}
				}

//				Util.DebugWriteLine($"For emulator framebuffer {new Size(_currentVideoProvider.BufferWidth, _currentVideoProvider.BufferHeight)}:");
//				Util.DebugWriteLine($"  For virtual size {new Size(_currentVideoProvider.VirtualWidth, _currentVideoProvider.VirtualHeight)}:");
//				Util.DebugWriteLine($"  Selecting display size {lastComputedSize}");

				// Change size
				Size = new Size(lastComputedSize.Width + borderWidth, lastComputedSize.Height + borderHeight);
				PerformLayout();

				// Is window off the screen at this size?
				if (!area.Contains(Bounds))
				{
					// At large framebuffer sizes/low screen resolutions, the window may be too large to fit the screen even at 1x scale
					// Prioritize that the top-left of the window is on-screen so the title bar and menu stay accessible

					if (Bounds.Right > area.Right) // Window is off the right edge
					{
						Left = Math.Max(area.Right - Size.Width, area.Left);
					}

					if (Bounds.Bottom > area.Bottom) // Window is off the bottom edge
					{
						Top = Math.Max(area.Bottom - Size.Height, area.Top);
					}
				}
			}

			DoPresentationPanelResize();
			DoPresentationPanelResize();
		}

		private void SynchChrome()
		{
			if (_inFullscreen)
			{
				// TODO - maybe apply a hack tracked during fullscreen here to override it
				FormBorderStyle = FormBorderStyle.None;
				MainMenuStrip.Visible = Config.DispChromeMenuFullscreen && !_argParser._chromeless;
				MainStatusBar.Visible = Config.DispChromeStatusBarFullscreen && !_argParser._chromeless;
			}
			else
			{
				MainStatusBar.Visible = Config.DispChromeStatusBarWindowed && !_argParser._chromeless;
				MainMenuStrip.Visible = Config.DispChromeMenuWindowed && !_argParser._chromeless;
				MaximizeBox = MinimizeBox = Config.DispChromeCaptionWindowed && !_argParser._chromeless;
				if (Config.DispChromeFrameWindowed == 0 || _argParser._chromeless)
				{
					FormBorderStyle = FormBorderStyle.None;
				}
				else if (Config.DispChromeFrameWindowed == 1)
				{
					FormBorderStyle = FormBorderStyle.SizableToolWindow;
				}
				else if (Config.DispChromeFrameWindowed == 2)
				{
					FormBorderStyle = FormBorderStyle.Sizable;
				}
			}
		}

		public void ToggleFullscreen(bool allowSuppress = false)
		{
			AutohideCursor(hide: false);

			// prohibit this operation if the current controls include LMouse
			if (allowSuppress)
			{
				if (InputManager.ActiveController.HasBinding("WMouse L"))
				{
					return;
				}
			}

			if (!_inFullscreen)
			{
				SuspendLayout();

				// Work around an AMD driver bug in >= vista:
				// It seems windows will activate opengl fullscreen mode when a GL control is occupying the exact space of a screen (0,0 and dimensions=screensize)
				// AMD cards manifest a problem under these circumstances, flickering other monitors.
				// It isn't clear whether nvidia cards are failing to employ this optimization, or just not flickering.
				// (this could be determined with more work; other side affects of the fullscreen mode include: corrupted TaskBar, no modal boxes on top of GL control, no screenshots)
				// At any rate, we can solve this by adding a 1px black border around the GL control
				// Please note: It is important to do this before resizing things, otherwise momentarily a GL control without WS_BORDER will be at the magic dimensions and cause the flakeout
				if (!OSTailoredCode.IsUnixHost
					&& Config.DispFullscreenHacks
					&& Config.DispMethod == EDispMethod.OpenGL)
				{
					// ATTENTION: this causes the StatusBar to not work well, since the backcolor is now set to black instead of SystemColors.Control.
					// It seems that some StatusBar elements composite with the backcolor.
					// Maybe we could add another control under the StatusBar. with a different backcolor
					Padding = new Padding(1);
					BackColor = Color.Black;

					// FUTURE WORK:
					// re-add this padding back into the display manager (so the image will get cut off a little but, but a few more resolutions will fully fit into the screen)
				}

				_windowedLocation = Location;

				_inFullscreen = true;
				SynchChrome();
				WindowState = FormWindowState.Maximized; // be sure to do this after setting the chrome, otherwise it wont work fully
				ResumeLayout();
			}
			else
			{
				SuspendLayout();

				WindowState = FormWindowState.Normal;

				if (!OSTailoredCode.IsUnixHost)
				{
					// do this even if DispFullscreenHacks aren't enabled, to restore it in case it changed underneath us or something
					Padding = new Padding(0);

					// it's important that we set the form color back to this, because the StatusBar icons blend onto the mainform, not onto the StatusBar--
					// so we need the StatusBar and mainform backdrop color to match
					BackColor = SystemColors.Control;
				}

				_inFullscreen = false;

				SynchChrome();
				Location = _windowedLocation;
				ResumeLayout();

				FrameBufferResized();
			}
		}

		private void OpenLuaConsole()
		{
			if (!LuaLibraries.IsAvailable)
			{
				ShowMessageBox(
					owner: null,
					text: "Native Lua dynamic library was unable to be loaded. " + (OSTailoredCode.IsUnixHost
						? "Make sure Lua is installed with your package manager."
						: "This library is provided in the dll/ folder, try redownloading BizHawk to fix this error."),
					caption: "Lua Load Error",
					EMsgBoxIcon.Error);
				return;
			}

			Tools.Load<LuaConsole>();
		}

		public void ClickSpeedItem(int num)
		{
			if ((ModifierKeys & Keys.Control) != 0)
			{
				SetSpeedPercentAlternate(num);
			}
			else
			{
				SetSpeedPercent(num);
			}
		}

		public void Unthrottle()
		{
			Config.Unthrottled = true;
		}

		public void Throttle()
		{
			Config.Unthrottled = false;
		}

		private void ThrottleMessage()
		{
			string type = ":(none)";
			if (Config.SoundThrottle)
			{
				type = ":Sound";
			}

			if (Config.VSyncThrottle)
			{
				type = $":Vsync{(Config.VSync ? "[ena]" : "[dis]")}";
			}

			if (Config.ClockThrottle)
			{
				type = ":Clock";
			}

			string throttled = Config.Unthrottled ? "Unthrottled" : "Throttled";
			string msg = $"{throttled}{type} ";

			AddOnScreenMessage(msg);
		}

		public void FrameSkipMessage()
		{
			AddOnScreenMessage($"Frameskipping set to {Config.FrameSkip}");
		}

		public void UpdateCheatStatus()
		{
			if (CheatList.AnyActive)
			{
				CheatStatusButton.ToolTipText = "Cheats are currently active";
				CheatStatusButton.Image = Properties.Resources.Cheat;
				CheatStatusButton.Visible = true;
			}
			else
			{
				CheatStatusButton.ToolTipText = "";
				CheatStatusButton.Image = Properties.Resources.Blank;
				CheatStatusButton.Visible = false;
			}
		}

		public bool RunLibretroCoreChooser()
		{
			string initFileName = null;
			string initDir;
			if (Config.LibretroCore is not null)
			{
				(initDir, initFileName) = Config.LibretroCore.SplitPathToDirAndFile();
			}
			else
			{
				initDir = Config.PathEntries.AbsolutePathForType(VSystemID.Raw.Libretro, "Cores");
				Directory.CreateDirectory(initDir);
			}
			var result = this.ShowFileOpenDialog(
				discardCWDChange: true,
				filter: LibretroCoresFSFilterSet,
				initDir: initDir!,
				initFileName: initFileName);
			if (result is null) return false;
			Config.LibretroCore = result;
			return true;
		}

		private Size _lastVideoSize = new Size(-1, -1), _lastVirtualSize = new Size(-1, -1);
		private readonly SaveSlotManager _stateSlots = new SaveSlotManager();

		// AVI/WAV state
		private IVideoWriter _currAviWriter;

		// Sound refactor TODO: we can enforce async mode here with a property that gets/sets this but does an async check
		private ISoundProvider _aviSoundInputAsync; // Note: This sound provider must be in async mode!

		private SimpleSyncSoundProvider _dumpProxy; // an audio proxy used for dumping

		private bool _windowClosedAndSafeToExitProcess;
		private int _exitCode;
		private bool _exitRequestPending;
		private bool _runloopFrameProgress;
		private long _frameAdvanceTimestamp;
		private long _frameRewindTimestamp;
		private bool _frameRewindWasPaused;
		private bool _runloopFrameAdvance;
		private bool _wasRewinding;
		private bool _lastFastForwardingOrRewinding;
		private bool _inResizeLoop;

		private readonly double _fpsUpdatesPerSecond = 4.0;
		private readonly double _fpsSmoothing = 8.0;
		private double _lastFps;
		private int _lastFpsRounded;
		private int _framesSinceLastFpsUpdate;
		private long _timestampLastFpsUpdate;

		public int GetApproxFramerate() => _lastFpsRounded;

		private readonly Throttle _throttle;

		// For handling automatic pausing when entering the menu
		private bool _wasPaused;
		private bool _didMenuPause;

		private bool _cursorHidden;
		private bool _inFullscreen;
		private Point _windowedLocation;
		private bool _needsFullscreenOnLoad;
		private bool _framebufferResizedPending;

		private int _lastOpenRomFilter;

		private readonly ParsedCLIFlags _argParser;

		private int _autoDumpLength;

		// Resources
		private Bitmap _statusBarDiskLightOnImage;
		private Bitmap _statusBarDiskLightOffImage;
		private Bitmap _linkCableOn;
		private Bitmap _linkCableOff;

		private readonly PresentationPanel _presentationPanel;

		// countdown for saveram autoflushing
		public int AutoFlushSaveRamIn { get; set; }

		private void SetStatusBar()
		{
			if (!_inFullscreen)
			{
				MainStatusBar.Visible = Config.DispChromeStatusBarWindowed;
				PerformLayout();
				FrameBufferResized();
			}
		}

		protected override string WindowTitle
		{
			get
			{
				var sb = new StringBuilder();

				if (_inResizeLoop)
				{
					var size = _presentationPanel.NativeSize;
					sb.Append($"({size.Width}x{size.Height})={(float) size.Width / size.Height} - ");
				}

				if (Config.DispSpeedupFeatures == 0)
				{
					// we need to display FPS somewhere, in this case
					sb.Append($"({_lastFps:0} fps) - ");
				}

				if (!Emulator.IsNull())
				{
					sb.Append($"{Game.Name} [{Emulator.GetSystemDisplayName()}] - ");
					var movie = MovieSession.Movie;
					if (movie.IsActive())
					{
						// I think the asterisk is conventionally after the filename, but I worry it would often be cut off there --yoshi
						sb.Append($"{(movie.Changes ? "*" : string.Empty)}{Path.GetFileName(movie.Filename)} - ");
					}
				}

				sb.Append(string.IsNullOrEmpty(VersionInfo.CustomBuildString)
					? "BizHawk"
					: VersionInfo.CustomBuildString);
				if (VersionInfo.DeveloperBuild) sb.Append(" (interim)");

				return sb.ToString();
			}
		}

		protected override string WindowTitleStatic
		{
			get
			{
				var sb = new StringBuilder();
				sb.Append(string.IsNullOrEmpty(VersionInfo.CustomBuildString)
					? "BizHawk"
					: VersionInfo.CustomBuildString);
				if (VersionInfo.DeveloperBuild) sb.Append(" (interim)");
				return sb.ToString();
			}
		}

		private void ClearAutohold()
		{
			ClearHolds();
			AddOnScreenMessage("Autohold keys cleared");
		}

		private void UpdateToolsLoadstate()
		{
			if (Tools.Has<SNESGraphicsDebugger>())
			{
				Tools.SNESGraphicsDebugger.UpdateToolsLoadstate();
			}
		}

		private void UpdateToolsAfter()
		{
			Tools.UpdateToolsAfter();
			HandleToggleLightAndLink();
		}

		public void UpdateDumpInfo(RomStatus? newStatus = null)
		{
			const string DUMP_KIND_BAD = " (bad dump)";
			const string DUMP_KIND_GOOD = " (good dump)";
			const string DUMP_KIND_HACK = " (hack/homebrew)";
			const string DUMP_KIND_MAME_BADEMU = " (unsupported board)";
			const string DUMP_KIND_UNRECOGNIZED = " (unrecognized)";
			var kind = string.Empty;
			var icon = Properties.Resources.Blank;
			var tooltip = string.Empty;
			if (!Game.IsNullInstance() && !Emulator.IsNull())
			{
				if (newStatus is null) newStatus = Game.Status;
				else Game.Status = newStatus.Value;
				(kind, icon, tooltip) = newStatus switch
				{
					RomStatus.GoodDump => (DUMP_KIND_GOOD, Properties.Resources.GreenCheck, "Verified good dump"),
					RomStatus.BadDump => (DUMP_KIND_BAD, Properties.Resources.ExclamationRed, "Warning: Bad ROM Dump"),
					RomStatus.Homebrew => (DUMP_KIND_HACK, Properties.Resources.HomeBrew, "Homebrew ROM"),
					RomStatus.TranslatedRom => (DUMP_KIND_HACK, Properties.Resources.Translation, "Translated ROM"),
					RomStatus.Hack => (DUMP_KIND_HACK, Properties.Resources.Hack, "Hacked ROM"),
					RomStatus.Overdump => (DUMP_KIND_BAD, Properties.Resources.ExclamationRed, "Warning: Overdump"),
					RomStatus.NotInDatabase => (DUMP_KIND_UNRECOGNIZED, Properties.Resources.RetroQuestion, "Warning: Unknown ROM"),
					// 3 from MAME:
					RomStatus.Imperfect => (DUMP_KIND_MAME_BADEMU, Properties.Resources.RetroQuestion, "Warning: Imperfect emulation"),
					RomStatus.Unimplemented => (DUMP_KIND_MAME_BADEMU, Properties.Resources.ExclamationRed, "Warning: Unemulated features"),
					RomStatus.NotWorking => (DUMP_KIND_MAME_BADEMU, Properties.Resources.ExclamationRed, "Warning: The game does not work"),
					/*RomStatus.Unknown or RomStatus.Bios*/_ => (DUMP_KIND_UNRECOGNIZED, Properties.Resources.Hack, "Warning: ROM of Unknown Character"),
				};
				if (_multiDiskMode
					&& kind is not DUMP_KIND_MAME_BADEMU) // don't override the warnings from MAME in this case
				{
					icon = Properties.Resources.RetroQuestion;
					tooltip = "Multi-disk bundler";
				}
			}
			DumpStatusButton.Image = icon;
			DumpStatusButton.ToolTipText = tooltip;
			DumpStatusReportMenuItem.Enabled = kind.Length is not 0;
			DumpStatusReportMenuItem.Image = icon;
			DumpStatusReportMenuItem.Text = string.Format(FMT_STR_DUMP_STATUS_MENUITEM_LABEL, kind);
			DumpStatusReportMenuItem.ToolTipText = tooltip;
		}

		private bool _multiDiskMode;

		// Rom details as decided by MainForm, which shouldn't happen, the RomLoader or Core should be doing this
		// Better is to just keep the game and rom hashes as properties and then generate the rom info from this
		private string _defaultRomDetails = "";

		private void LoadSaveRam()
		{
			if (Emulator.HasSaveRam())
			{
				var saveRam = new FileInfo(Config.PathEntries.SaveRamAbsolutePath(Game, MovieSession.Movie));
				var autoSaveRam = new FileInfo(Config.PathEntries.AutoSaveRamAbsolutePath(Game, MovieSession.Movie));

				FileInfo saveramToLoad;
				if (saveRam.Exists && (!autoSaveRam.Exists || autoSaveRam.LastWriteTimeUtc <= saveRam.LastWriteTimeUtc))
				{
					saveramToLoad = saveRam;
				}
				else if (autoSaveRam.Exists && !saveRam.Exists)
				{
					AddOnScreenMessage("SaveRAM missing! Loading autosaved SaveRAM instead.", 5);
					saveramToLoad = autoSaveRam;
				}
				else if (saveRam.Exists && autoSaveRam.Exists)
				{
					bool result = ShowMessageBox2(
						owner: this,
						"The autosaved SaveRAM is more recent than the normal SaveRAM.\n" +
						"This could happen due to a crash or because files were manually modified.\n" +
						"Do you want to load the autosave instead of the older SaveRAM file?",
						"Load autosaved SaveRAM?",
						EMsgBoxIcon.Error);

					saveramToLoad = result ? autoSaveRam : saveRam;
				}
				else
				{
					// no saveram to load
					return;
				}

				try
				{
					byte[] sram;

					// some cores might not know how big the saveram ought to be, so just send it the whole file
					if (Emulator is AppleII or C64 or DOSBox or MGBAHawk or NeoGeoPort or NES { BoardName: "FDS" })
					{
						sram = File.ReadAllBytes(saveramToLoad.FullName);
					}
					else
					{
						var oldRam = Emulator.AsSaveRam().CloneSaveRam();
						if (oldRam is null)
						{
							// we have a SaveRAM file, but the current core does not have save ram.
							// just skip loading the saveram file in that case
							return;
						}

						// why do we silently truncate\pad here instead of warning\erroring?
						sram = new byte[oldRam.Length];
						using var fs = saveramToLoad.OpenRead();
						_ = fs.Read(sram, 0, sram.Length);
					}

					Emulator.AsSaveRam().StoreSaveRam(sram);
				}
				catch (IOException e)
				{
					AddOnScreenMessage("An error occurred while loading Sram");
					Console.Error.WriteLine(e);
				}
			}
		}

		public bool FlushSaveRAM(bool autosave = false)
		{
			if (Emulator.HasSaveRam())
			{
				string path;
				if (autosave)
				{
					path = Config.PathEntries.AutoSaveRamAbsolutePath(Game, MovieSession.Movie);
				}
				else
				{
					path = Config.PathEntries.SaveRamAbsolutePath(Game, MovieSession.Movie);
				}

				var file = new FileInfo(path);
				var newPath = $"{path}.new";
				var newFile = new FileInfo(newPath);
				var backupPath = $"{path}.bak";
				var backupFile = new FileInfo(backupPath);

				var saveram = Emulator.AsSaveRam().CloneSaveRam();
				if (saveram == null)
					return true;

				try
				{
					Directory.CreateDirectory(file.DirectoryName!);
					using (var fs = File.Create(newPath))
					{
						fs.Write(saveram, 0, saveram.Length);
						fs.Flush(flushToDisk: true);
					}

					if (file.Exists)
					{
						if (Config.BackupSaveram)
						{
							if (backupFile.Exists)
							{
								backupFile.Delete();
							}

							file.MoveTo(backupPath);
						}
						else
						{
							file.Delete();
						}
					}

					newFile.MoveTo(path);
				}
				catch (IOException e)
				{
					AddOnScreenMessage("Failed to flush saveram!");
					Console.Error.WriteLine(e);
					return false;
				}
			}

			return true;
		}

		private void RewireSound()
		{
			if (_dumpProxy != null)
			{
				// we're video dumping, so async mode only and use the DumpProxy.
				// note that the avi dumper has already rewired the emulator itself in this case.
				Sound.SetInputPin(_dumpProxy);
			}
			else
			{
				bool useAsyncMode = _currentSoundProvider.CanProvideAsync && !Config.SoundThrottle;
				_currentSoundProvider.SetSyncMode(useAsyncMode ? SyncSoundMode.Async : SyncSoundMode.Sync);
				Sound.SetInputPin(_currentSoundProvider);
			}
		}

		private static readonly IList<Type> SpecializedTools = ReflectionCache.Types
			.Where(static t => !t.IsAbstract && typeof(IToolForm).IsAssignableFrom(t)
				&& t.GetCustomAttribute<SpecializedToolAttribute>() is not null)
			.ToList();

		private ISet<char> _availableAccelerators;

		private ISet<char> AvailableAccelerators
		{
			get
			{
				if (_availableAccelerators == null)
				{
					_availableAccelerators = new HashSet<char>();
					for (var c = 'A'; c <= 'Z'; c++) _availableAccelerators.Add(c);
					foreach (ToolStripItem item in MainMenuStrip.Items)
					{
						if (!item.Visible) continue;
						var i = item.Text.IndexOf('&');
						if (i == -1 || i == item.Text.Length - 1) continue;
						_availableAccelerators.Remove(char.ToUpperInvariant(item.Text[i + 1]));
					}
				}
				return _availableAccelerators;
			}
		}

		private void DisplayDefaultCoreMenu()
		{
			GenericCoreSubMenu.Visible = true;
			var sysID = Emulator.SystemId;
			for (var i = 0; i < sysID.Length; i++)
			{
				if (AvailableAccelerators.Remove(char.ToUpperInvariant(sysID[i])))
				{
					sysID = sysID.Insert(i, "&");
					break;
				}
			}
			GenericCoreSubMenu.Text = sysID;
			GenericCoreSubMenu.DropDownItems.Clear();

			var settingsMenuItem = new ToolStripMenuItem { Text = "&Settings" };
			settingsMenuItem.Click += GenericCoreSettingsMenuItem_Click;
			GenericCoreSubMenu.DropDownItems.Add(settingsMenuItem);

			var specializedTools = SpecializedTools.Where(Tools.IsAvailable).OrderBy(static t => t.Name).ToList();
			if (specializedTools.Count is 0) return;

			GenericCoreSubMenu.DropDownItems.Add(new ToolStripSeparator());
			foreach (var toolType in specializedTools)
			{
				var (icon, name) = Tools.GetIconAndNameFor(toolType);
				ToolStripMenuItem item = new() { Image = icon, Text = $"&{name}" };
				item.Click += (_, _) => Tools.Load(toolType);
				GenericCoreSubMenu.DropDownItems.Add(item);
			}
		}

		private void InitControls()
		{
			Controller controls = new(new ControllerDefinition("Emulator Frontend Controls")
			{
				BoolButtons = Config.HotkeyBindings.Keys.ToList(),
			}.MakeImmutable());

			foreach (var (k, v) in Config.HotkeyBindings) controls.BindMulti(k, v);

			InputManager.ClientControls = controls;
			InputManager.ControllerInputCoalescer = new();
		}

		private void LoadMoviesFromRecent(string path)
		{
			if (File.Exists(path))
			{
				var movie = MovieSession.Get(path, true);
				MovieSession.ReadOnly = true;
				StartNewMovie(movie, false);
			}
			else
			{
				Config.RecentMovies.HandleLoadError(this, path);
			}
		}

		private void LoadRomFromRecent(string rom)
		{
			var ioa = OpenAdvancedSerializer.ParseWithLegacy(rom);

			// if(ioa is this or that) - for more complex behaviour
			string romPath = ioa.SimplePath;

			if (!LoadRom(romPath, new LoadRomArgs(ioa), out var failureIsFromAskSave))
			{
				if (failureIsFromAskSave) AddOnScreenMessage("ROM loading cancelled; a tool had unsaved changes");
				else if (ioa is OpenAdvanced_LibretroNoGame || File.Exists(romPath)) AddOnScreenMessage("ROM loading failed");
				else Config.RecentRoms.HandleLoadError(this, romPath, rom);
			}
		}

		private void SetPauseStatusBarIcon()
		{
			if (EmulatorPaused)
			{
				PauseStatusButton.Image = Properties.Resources.Pause;
				PauseStatusButton.Visible = true;
				PauseStatusButton.ToolTipText = "Emulator Paused";
			}
			else if (IsTurboSeeking)
			{
				PauseStatusButton.Image = Properties.Resources.Lightning;
				PauseStatusButton.Visible = true;
				// ReSharper disable once PossibleInvalidOperationException
				PauseStatusButton.ToolTipText = $"Emulator is turbo seeking to frame {PauseOnFrame.Value} click to stop seek";
			}
			else if (PauseOnFrame.HasValue)
			{
				PauseStatusButton.Image = Properties.Resources.YellowRight;
				PauseStatusButton.Visible = true;
				PauseStatusButton.ToolTipText = $"Emulator is playing to frame {PauseOnFrame.Value} click to stop seek";
			}
			else
			{
				PauseStatusButton.Image = Properties.Resources.Blank;
				PauseStatusButton.Visible = false;
				PauseStatusButton.ToolTipText = "";
			}
		}

		private void SyncThrottle()
		{
			// "unthrottled" = throttle was turned off with "Toggle Throttle" hotkey
			// "turbo" = throttle is off due to the "Turbo" hotkey being held
			// They are basically the same thing but one is a toggle and the other requires a
			// hotkey to be held. There is however slightly different behavior in that turbo
			// skips outputting the audio. There's also a third way which is when no throttle
			// method is selected, but the clock throttle determines that by itself and
			// everything appears normal here.
			var rewind = Rewinder?.Active == true && (InputManager.ClientControls["Rewind"] || PressRewind);
			var fastForward = InputManager.ClientControls["Fast Forward"] || FastForward;
			var turbo = IsTurboing;

			int speedPercent = fastForward ? Config.SpeedPercentAlternate : Config.SpeedPercent;

			if (rewind)
			{
				speedPercent = Math.Max(speedPercent / Rewinder.RewindFrequency, 5);
			}

			DisableSecondaryThrottling = Config.Unthrottled || turbo || fastForward || rewind;

			// realtime throttle is never going to be so exact that using a double here is wrong
			_throttle.SetCoreFps(Emulator.VsyncRate());
			_throttle.signal_paused = EmulatorPaused;
			_throttle.signal_unthrottle = Config.Unthrottled || turbo;

			// zero 26-mar-2016 - vsync and vsync throttle here both is odd, but see comments elsewhere about triple buffering
			_throttle.signal_overrideSecondaryThrottle = (fastForward || rewind) && (Config.SoundThrottle || Config.VSyncThrottle || Config.VSync);
			_throttle.SetSpeedPercent(speedPercent);
		}

		private void SetSpeedPercentAlternate(int value)
		{
			Config.SpeedPercentAlternate = value;
			SyncThrottle();
			AddOnScreenMessage($"Alternate Speed: {value}%");
		}

		private void SetSpeedPercent(int value)
		{
			Config.SpeedPercent = value;
			SyncThrottle();
			AddOnScreenMessage($"Speed: {value}%");
		}

		private void Shutdown()
		{
			_currAviWriter?.CloseFile();
			_currAviWriter = null;
		}

		private DateTime _lastMessageCheck = DateTime.MinValue;

		private void CheckMessages()
		{
			var currentTime = DateTime.UtcNow;
			// only check window messages a maximum of once per millisecond
			// this check is irrelvant for the 99% of cases where fps are <1k
			// but gives a slight fps boost in those scenarios
			if ((uint)(currentTime - _lastMessageCheck).Milliseconds > 0)
			{
				_lastMessageCheck = currentTime;
				Application.DoEvents();
			}

			if (ActiveForm != null)
			{
				ScreenSaver.ResetTimerPeriodically();
			}

			if (PathsFromDragDrop is not null) this.DoWithTempMute(() =>
			{
				try
				{
					FormDragDrop_internal();
				}
				catch (Exception ex)
				{
					ShowMessageBox(owner: null, $"Exception on drag and drop:\n{ex}");
				}
				PathsFromDragDrop = null;
			});

			string[][] todo = Array.Empty<string[]>();
			lock (_singleInstanceForwardedArgs)
			{
				if (_singleInstanceForwardedArgs.Count > 0)
				{
					todo = _singleInstanceForwardedArgs.ToArray();
					_singleInstanceForwardedArgs.Clear();
				}
			}
			foreach (var args in todo) SingleInstanceProcessArgs(args);
		}

		private Point _lastMouseAutoHidePos;

		private void AutohideCursor(bool hide, bool alwaysUpdate = true)
		{
			var mousePos = MousePosition;
			// avoid sensitive mice unhiding the mouse cursor
			var shouldUpdateCursor = alwaysUpdate
				|| Math.Abs(_lastMouseAutoHidePos.X - mousePos.X) > 5
				|| Math.Abs(_lastMouseAutoHidePos.Y - mousePos.Y) > 5;

			if (!shouldUpdateCursor || Config.CaptureMouse)
			{
				return;
			}

			_lastMouseAutoHidePos = mousePos;
			if (hide && !_cursorHidden)
			{
				// this only works assuming the mouse is perfectly still
				// if the mouse is slightly moving, it will use the "moving" cursor rather
				_presentationPanel.Control.Cursor = Properties.Resources.BlankCursor.Value;

				// This will actually fully hide the cursor
				// However, this is a no-op on Mono, so we need to do both ways
				Cursor.Hide();

				_cursorHidden = true;
			}
			else if (!hide && _cursorHidden)
			{
				_presentationPanel.Control.Cursor = Cursors.Default;
				Cursor.Show();
				timerMouseIdle.Stop();
				timerMouseIdle.Start();
				_cursorHidden = false;
			}
		}

		public BitmapBuffer MakeScreenshotImage()
		{
			var ret = new BitmapBuffer(_currentVideoProvider.BufferWidth, _currentVideoProvider.BufferHeight, _currentVideoProvider.GetVideoBufferCopy());
			ret.DiscardAlpha();
			return ret;
		}

		private void SaveSlotSelectedMessage()
		{
			int slot = Config.SaveSlot;
			string emptyPart = HasSlot(slot) ? "" : " (empty)";
			string message = $"Slot {slot}{emptyPart} selected.";
			AddOnScreenMessage(message);
		}

		/*internal*/public void Render()
		{
			if (Config.DispSpeedupFeatures == 0)
			{
				DisplayManager.DiscardApiHawkSurfaces();
				return;
			}

			var video = _currentVideoProvider;
			Size currVideoSize = new Size(video.BufferWidth, video.BufferHeight);
			Size currVirtualSize = new Size(video.VirtualWidth, video.VirtualHeight);


			bool resizeFramebuffer = currVideoSize != _lastVideoSize || currVirtualSize != _lastVirtualSize;

			bool isZero = currVideoSize.Width == 0 || currVideoSize.Height == 0 || currVirtualSize.Width == 0 || currVirtualSize.Height == 0;

			//don't resize if the new size is 0 somehow; we'll wait until we have a sensible size
			if (isZero)
			{
				resizeFramebuffer = false;
			}

			if (resizeFramebuffer)
			{
				_lastVideoSize = currVideoSize;
				_lastVirtualSize = currVirtualSize;
				FrameBufferResized();
			}

			//rendering flakes out egregiously if we have a zero size
			//can we fix it later not to?
			if (isZero)
				DisplayManager.Blank();
			else
				DisplayManager.UpdateSource(video);
		}

		public static readonly FilesystemFilterSet ConfigFileFSFilterSet = new(
			appendAllFilesEntry: false,
			new FilesystemFilter("Config File", extensions: [ "ini" ]));

		private void OpenRom()
		{
			var result = this.ShowFileOpenDialog(
				filter: RomLoader.RomFilter,
				filterIndex: ref _lastOpenRomFilter,
				initDir: Config.PathEntries.RomAbsolutePath(Emulator.SystemId));
			if (result is null) return;
			var filePath = new FileInfo(result).FullName;
			_ = LoadRom(filePath, new LoadRomArgs(new OpenAdvanced_OpenRom(filePath)));
		}

		private void CoreSyncSettings(object sender, RomLoader.SettingsLoadArgs e)
		{
			if (MovieSession.NewMovieQueued)
			{
				if (!string.IsNullOrWhiteSpace(MovieSession.QueuedSyncSettings))
				{
					e.Settings = ConfigService.LoadWithType(MovieSession.QueuedSyncSettings);
				}
				else
				{
					e.Settings = Config.GetCoreSyncSettings(e.Core, e.SettingsType);

					// Only show this nag if the core actually has sync settings, not all cores do
					if (e.Settings != null && !_suppressSyncSettingsWarning)
					{
						ShowMessageBox(
							owner: null,
							"No sync settings found, using currently configured settings for this core.",
							"No sync settings found",
							EMsgBoxIcon.Warning);
					}
				}
			}
			else
			{
				e.Settings = Config.GetCoreSyncSettings(e.Core, e.SettingsType);
			}
		}

		private void CoreSettings(object sender, RomLoader.SettingsLoadArgs e)
		{
			e.Settings = Config.GetCoreSettings(e.Core, e.SettingsType);
		}

		private void HandlePutCoreSettings(PutSettingsDirtyBits dirty)
		{
			if (dirty.HasFlag(PutSettingsDirtyBits.RebootCore)) FlagNeedsReboot();
			if (dirty.HasFlag(PutSettingsDirtyBits.ScreenLayoutChanged)) FrameBufferResized();
		}

		private bool MayPutCoreSyncSettings()
		{
			if (MovieSession.Movie.IsActive())
			{
				AddOnScreenMessage("Attempt to change sync-relevant settings while recording BLOCKED.");
				return false;
			}
			return true;
		}

		private void HandlePutCoreSyncSettings(PutSettingsDirtyBits dirty)
		{
			if (dirty.HasFlag(PutSettingsDirtyBits.RebootCore)) FlagNeedsReboot();
		}

		public ISettingsAdapter GetSettingsAdapterFor<T>()
			where T : IEmulator
			=> Emulator is T
				? GetSettingsAdapterForLoadedCoreUntyped()
				: new ConfigSettingsAdapter<T>(Config);

		public ISettingsAdapter GetSettingsAdapterForLoadedCore<T>()
			where T : IEmulator
		{
			if (Emulator is not T) throw new InvalidOperationException();
			return GetSettingsAdapterForLoadedCoreUntyped();
		}

		public SettingsAdapter GetSettingsAdapterForLoadedCoreUntyped()
			=> new(Emulator, static () => true, HandlePutCoreSettings, MayPutCoreSyncSettings, HandlePutCoreSyncSettings);

		private void SaveConfig(string path = "")
		{
			if (Config.SaveWindowPosition)
			{
				if (WindowState is FormWindowState.Normal)
				{
					Config.MainWindowPosition = Location;
					Config.MainWindowSize = Size;
				}
				Config.MainWindowMaximized = WindowState is FormWindowState.Maximized && !_inFullscreen;
			}
			else
			{
				Config.MainWindowPosition = null;
				Config.MainWindowSize = null;
			}

			Config.LastWrittenFrom = VersionInfo.MainVersion;
			Config.LastWrittenFromDetailed = VersionInfo.GetEmuVersion();

			if (string.IsNullOrEmpty(path))
			{
				path = _getConfigPath();
			}

			CommitCoreSettingsToConfig();
			ConfigService.Save(path, Config);
		}

		private void ToggleFps()
			=> Config.DisplayFps = !Config.DisplayFps;

		private void ToggleFrameCounter()
			=> Config.DisplayFrameCounter = !Config.DisplayFrameCounter;

		private void ToggleLagCounter()
			=> Config.DisplayLagCounter = !Config.DisplayLagCounter;

		private void ToggleInputDisplay()
			=> Config.DisplayInput = !Config.DisplayInput;

		public void ToggleSound()
		{
			Config.SoundEnabled = !Config.SoundEnabled;
			Sound.StopSound();
			Sound.StartSound();
			UpdateStatusBarMuteIndicator();
		}

		private void VolumeUp()
		{
			Config.SoundVolume += 10;
			if (Config.SoundVolume > 100)
			{
				Config.SoundVolume = 100;
			}

			AddOnScreenMessage($"Volume {Config.SoundVolume}");
		}

		private void VolumeDown()
		{
			Config.SoundVolume -= 10;
			if (Config.SoundVolume < 0)
			{
				Config.SoundVolume = 0;
			}

			AddOnScreenMessage($"Volume {Config.SoundVolume}");
		}

		private void SoftReset()
		{
			// is it enough to run this for one frame? maybe..
			if (Emulator.ControllerDefinition.BoolButtons.Contains("Reset")
				&& !MovieSession.Movie.IsPlaying())
			{
				InputManager.ClickyVirtualPadController.Click("Reset");
				AddOnScreenMessage("Reset button pressed.");
			}
		}

		private void HardReset()
		{
			// is it enough to run this for one frame? maybe..
			if (Emulator.ControllerDefinition.BoolButtons.Contains("Power")
				&& !MovieSession.Movie.IsPlaying())
			{
				InputManager.ClickyVirtualPadController.Click("Power");
				AddOnScreenMessage("Power button pressed.");
			}
		}

		private Color SlotForeColor(int slot)
		{
			return HasSlot(slot)
				? Config.SaveSlot == slot
					? SystemColors.HighlightText
					: SystemColors.WindowText
				: SystemColors.GrayText;
		}

		private Color SlotBackColor(int slot)
		{
			return  Config.SaveSlot == slot
				? SystemColors.Highlight
				: SystemColors.Control;
		}

		public void UpdateStatusSlots()
		{
			_stateSlots.Update(Emulator, MovieSession.Movie, SaveStatePrefix());

			Slot1StatusButton.ForeColor = SlotForeColor(1);
			Slot2StatusButton.ForeColor = SlotForeColor(2);
			Slot3StatusButton.ForeColor = SlotForeColor(3);
			Slot4StatusButton.ForeColor = SlotForeColor(4);
			Slot5StatusButton.ForeColor = SlotForeColor(5);
			Slot6StatusButton.ForeColor = SlotForeColor(6);
			Slot7StatusButton.ForeColor = SlotForeColor(7);
			Slot8StatusButton.ForeColor = SlotForeColor(8);
			Slot9StatusButton.ForeColor = SlotForeColor(9);
			Slot0StatusButton.ForeColor = SlotForeColor(10);

			Slot1StatusButton.BackColor = SlotBackColor(1);
			Slot2StatusButton.BackColor = SlotBackColor(2);
			Slot3StatusButton.BackColor = SlotBackColor(3);
			Slot4StatusButton.BackColor = SlotBackColor(4);
			Slot5StatusButton.BackColor = SlotBackColor(5);
			Slot6StatusButton.BackColor = SlotBackColor(6);
			Slot7StatusButton.BackColor = SlotBackColor(7);
			Slot8StatusButton.BackColor = SlotBackColor(8);
			Slot9StatusButton.BackColor = SlotBackColor(9);
			Slot0StatusButton.BackColor = SlotBackColor(10);

			SaveSlotsStatusLabel.Visible =
				Slot1StatusButton.Visible =
				Slot2StatusButton.Visible =
				Slot3StatusButton.Visible =
				Slot4StatusButton.Visible =
				Slot5StatusButton.Visible =
				Slot6StatusButton.Visible =
				Slot7StatusButton.Visible =
				Slot8StatusButton.Visible =
				Slot9StatusButton.Visible =
				Slot0StatusButton.Visible =
					Emulator.HasSavestates();
		}

		public BitmapBuffer CaptureOSD()
		{
			var bb = DisplayManager.RenderOffscreen(_currentVideoProvider, true);
			bb.DiscardAlpha();
			return bb;
		}
		public BitmapBuffer CaptureLua()
		{
			var bb = DisplayManager.RenderOffscreenLua(_currentVideoProvider);
			bb.DiscardAlpha();
			return bb;
		}

		private void IncreaseWindowSize()
		{
			var windowScale = Config.GetWindowScaleFor(Emulator.SystemId);
			if (windowScale < WINDOW_SCALE_MAX)
			{
				windowScale++;
				Config.SetWindowScaleFor(Emulator.SystemId, windowScale);
			}
			AddOnScreenMessage($"Screensize set to {windowScale}x");
			FrameBufferResized(forceWindowResize: true);
		}

		private void DecreaseWindowSize()
		{
			var windowScale = Config.GetWindowScaleFor(Emulator.SystemId);
			if (windowScale > 1)
			{
				windowScale--;
				Config.SetWindowScaleFor(Emulator.SystemId, windowScale);
			}
			AddOnScreenMessage($"Screensize set to {windowScale}x");
			FrameBufferResized(forceWindowResize: true);
		}

		private static readonly int[] SpeedPercents = { 1, 3, 6, 12, 25, 50, 75, 100, 150, 200, 300, 400, 800, 1600, 3200, 6400 };

		private bool CheckCanSetSpeed()
		{
			if (Config.ClockThrottle)
				return true;

			AddOnScreenMessage("Unable to change speed, please switch to clock throttle");
			return false;
		}

		private void ResetSpeed()
		{
			if (!CheckCanSetSpeed())
				return;

			SetSpeedPercent(100);
		}

		private void IncreaseSpeed()
		{
			if (!CheckCanSetSpeed())
				return;

			var oldPercent = Config.SpeedPercent;
			int newPercent;

			int i = 0;
			do
			{
				i++;
				newPercent = SpeedPercents[i];
			}
			while (newPercent <= oldPercent && i < SpeedPercents.Length - 1);

			SetSpeedPercent(newPercent);
		}

		private void DecreaseSpeed()
		{
			if (!CheckCanSetSpeed())
				return;

			var oldPercent = Config.SpeedPercent;
			int newPercent;

			int i = SpeedPercents.Length - 1;
			do
			{
				i--;
				newPercent = SpeedPercents[i];
			}
			while (newPercent >= oldPercent && i > 0);

			SetSpeedPercent(newPercent);
		}

		private void SaveMovie()
		{
			if (MovieSession.Movie.IsActive())
			{
				MovieSession.Movie.Save();
				AddOnScreenMessage($"{MovieSession.Movie.Filename} saved.");
			}
		}

		private void HandleToggleLightAndLink()
		{
			if (!MainStatusBar.Visible) return;

			if (Emulator.HasDriveLight() && Emulator.AsDriveLight() is { DriveLightEnabled: true } diskLEDCore)
			{
				LedLightStatusLabel.Image = diskLEDCore.DriveLightOn ? _statusBarDiskLightOnImage : _statusBarDiskLightOffImage;
				LedLightStatusLabel.ToolTipText = Emulator.AsDriveLight().DriveLightIconDescription;
				LedLightStatusLabel.Visible = true;
			}
			else
			{
				LedLightStatusLabel.Visible = false;
			}

			if (Emulator.UsesLinkCable())
			{
				var linkableCore = Emulator.AsLinkable();
				LinkConnectStatusBarButton.Image = linkableCore.LinkConnected ? _linkCableOn : _linkCableOff;
				LinkConnectStatusBarButton.ToolTipText = $"Link connection is currently {(linkableCore.LinkConnected ? "enabled" : "disabled")}";
				LinkConnectStatusBarButton.Visible = true;
			}
			else
			{
				LinkConnectStatusBarButton.Visible = false;
			}
		}

		private void UpdateStatusBarMuteIndicator()
			=> (StatusBarMuteIndicator.Image, StatusBarMuteIndicator.ToolTipText) = Config.SoundEnabled
				? (Properties.Resources.Audio, $"Core is producing audio, live playback at {(Config.SoundEnabledNormal ? Config.SoundVolume : 0)}% volume")
				: (Properties.Resources.AudioMuted, "Core is not producing audio");

		private void UpdateStatusBarRewindIndicator()
			=> StatusBarRewindIndicator.Visible = Rewinder?.Active is true;

		private void UpdateKeyPriorityIcon()
		{
			switch (Config.InputHotkeyOverrideOptions)
			{
				default:
				case Config.InputPriority.BOTH:
					KeyPriorityStatusLabel.Image = Properties.Resources.Both;
					KeyPriorityStatusLabel.ToolTipText = "Key priority: Allow both hotkeys and controller buttons";
					break;
				case Config.InputPriority.INPUT:
					KeyPriorityStatusLabel.Image = Properties.Resources.GameController;
					KeyPriorityStatusLabel.ToolTipText = "Key priority: Controller buttons will override hotkeys";
					break;
				case Config.InputPriority.HOTKEY:
					KeyPriorityStatusLabel.Image = Properties.Resources.HotKeys;
					KeyPriorityStatusLabel.ToolTipText = "Key priority: Hotkeys will override controller buttons";
					break;
			}
		}

		private void ToggleBackgroundInput()
		{
			Config.AcceptBackgroundInput = !Config.AcceptBackgroundInput;
			AddOnScreenMessage($"Background Input {(Config.AcceptBackgroundInput ? "enabled" : "disabled")}");
		}

		private void ToggleCaptureMouse()
		{
			if (!InputManager.ActiveController.IsMouseBound)
			{
				AddOnScreenMessage("Nothing bound to mouse, not capturing cursor");
				return;
			}
			Config.CaptureMouse = !Config.CaptureMouse;
			CaptureMouse(Config.CaptureMouse);
			if (Config.CaptureMouse) AddOnScreenMessage($"Mouse cursor captured, press {Config.HotkeyBindings["Capture Mouse"]} to uncapture", duration: 7);
			else AddOnScreenMessage("Mouse cursor uncaptured");
		}

		private void ToggleStayOnTop()
		{
			TopMost = Config.MainFormStayOnTop = !Config.MainFormStayOnTop;
			AddOnScreenMessage($"Stay on Top {(Config.MainFormStayOnTop ? "enabled" : "disabled")}");
		}

		private void VsyncMessage()
		{
			AddOnScreenMessage($"Display Vsync set to {(Config.VSync ? "on" : "off")}");
		}

		private void FdsInsertDiskMenuAdd(string name, string button, string msg)
		{
			FDSControlsMenuItem.DropDownItems.Add(name, null, (sender, e) =>
			{
				if (Emulator.ControllerDefinition.BoolButtons.Contains(button)
					&& !MovieSession.Movie.IsPlaying())
				{
					InputManager.ClickyVirtualPadController.Click(button);
					AddOnScreenMessage(msg);
				}
			});
		}

		private const int WmDeviceChange = 0x0219;

		private void UpdateCoreStatusBarButton()
		{
			var attributes = Emulator.Attributes();
			var coreDispName = attributes.Released ? attributes.CoreName : $"(Experimental) {attributes.CoreName}";
			LoadedCoreNameMenuItem.Text = $"Loaded core: {coreDispName} ({Emulator.SystemId})";
			if (Emulator.IsNull())
			{
				CoreNameStatusBarButton.Visible = false;
				return;
			}

			CoreNameStatusBarButton.Visible = true;

			CoreNameStatusBarButton.Text = coreDispName;
			CoreNameStatusBarButton.Image = Emulator.Icon();
			CoreNameStatusBarButton.ToolTipText = attributes is PortedCoreAttribute ? "(ported) " : "";


			if (Emulator.SystemId == VSystemID.Raw.ZXSpectrum)
			{
				var core = (Emulation.Cores.Computers.SinclairSpectrum.ZXSpectrum)Emulator;
				CoreNameStatusBarButton.ToolTipText = core.GetMachineType();
			}

			if (Emulator.SystemId == VSystemID.Raw.AmstradCPC)
			{
				var core = (Emulation.Cores.Computers.AmstradCPC.AmstradCPC)Emulator;
				CoreNameStatusBarButton.ToolTipText = core.GetMachineType();
			}
		}

		private void ToggleKeyPriority()
		{
			int priority = (int)Config.InputHotkeyOverrideOptions;
			priority++;
			if (priority > 2)
			{
				priority = 0;
			}

			if (Config.NoMixedInputHokeyOverride && priority == 0)
			{
				priority = 1;
			}

			Config.InputHotkeyOverrideOptions = (Config.InputPriority)priority;
			UpdateKeyPriorityIcon();
			switch (Config.InputHotkeyOverrideOptions)
			{
				case Config.InputPriority.BOTH:
					AddOnScreenMessage("Key priority set to Both Hotkey and Input");
					break;
				case Config.InputPriority.INPUT:
					AddOnScreenMessage("Key priority set to Input over Hotkey");
					break;
				case Config.InputPriority.HOTKEY:
					AddOnScreenMessage("Key priority set to Hotkey over Input");
					break;
			}
		}

		private void LoadConfigFile(string iniPath)
		{
			LoadGlobalConfigFromFile(iniPath);
			InitControls(); // rebind hotkeys
			InputManager.SyncControls(Emulator, MovieSession, Config);
			Tools.Restart(Config, Emulator, Game);
			ExtToolManager.Restart(Config);
			Sound.Config = Config;
			DisplayManager.UpdateGlobals(Config, Emulator);
			RA?.Restart();
			AddOnScreenMessage($"Config file loaded: {iniPath}");
		}

		/*internal*/public void StepRunLoop_Throttle()
		{
			SyncThrottle();
			_throttle.signal_frameAdvance = _runloopFrameAdvance;
			_throttle.signal_continuousFrameAdvancing = _runloopFrameProgress;

			_throttle.Step(Config, Sound, allowSleep: true, forceFrameSkip: -1);
		}

		public void FrameAdvance(bool discardApiHawkSurfaces)
		{
			PressFrameAdvance = true;
			StepRunLoop_Core(true);
			if (discardApiHawkSurfaces)
			{
				DisplayManager.DiscardApiHawkSurfaces();
			}
		}

		public void SeekFrameAdvance()
		{
			PressFrameAdvance = true;
			StepRunLoop_Core(true);
			DisplayManager.DiscardApiHawkSurfaces();
			PressFrameAdvance = false;
		}

		private void StepRunLoop_Core(bool force = false)
		{
			var runFrame = false;
			var currentTimestamp = Stopwatch.GetTimestamp();

			double frameAdvanceTimestampDeltaMs = (double)(currentTimestamp - _frameAdvanceTimestamp) / Stopwatch.Frequency * 1000.0;
			bool frameProgressTimeElapsed = frameAdvanceTimestampDeltaMs >= Config.FrameProgressDelayMs;

			// TODO technically this should only force run frames if the frame advance key has been used
			if (Config.SkipLagFrame && Emulator.CanPollInput() && Emulator.AsInputPollable().IsLagFrame && Emulator.Frame > 0)
			{
				runFrame = true;
			}

			RA?.Update();

			bool frameAdvance = InputManager.ClientControls["Frame Advance"] || PressFrameAdvance || HoldFrameAdvance;
			if (FrameInch)
			{
				FrameInch = false;
				if (EmulatorPaused)
				{
					frameAdvance = true;
				}
				else
				{
					PauseEmulator();
				}
			}

			if (frameAdvance)
			{
				if (!_runloopFrameAdvance)
				{
					// handle the initial trigger of a frame advance
					runFrame = true;
					_frameAdvanceTimestamp = currentTimestamp;
					PauseEmulator();
				}
				else if (frameProgressTimeElapsed)
				{
					runFrame = true;
					_runloopFrameProgress = true;
					UnpauseEmulator();
				}
			}
			else
			{
				if (_runloopFrameAdvance)
				{
					// handle release of frame advance
					PauseEmulator();
				}
				_runloopFrameProgress = false;
			}

			_runloopFrameAdvance = frameAdvance;

#if BIZHAWKBUILD_SUPERHAWK
			if (!EmulatorPaused && (!Config.SuperHawkThrottle || InputManager.ClientControls.AnyInputHeld))
#else
			if (!EmulatorPaused)
#endif
			{
				runFrame = true;
			}

			bool isRewinding = Rewind(ref runFrame, currentTimestamp, out var returnToRecording);
			IsRewinding = isRewinding;
			_runloopFrameProgress |= isRewinding;

			float atten = 0;

			// BlockFrameAdvance (true when input it being editted in TAStudio) supercedes all other frame advance conditions
			if ((runFrame || force) && !BlockFrameAdvance)
			{
				var isFastForwarding = IsFastForwarding;
				var isFastForwardingOrRewinding = isFastForwarding || isRewinding || Config.Unthrottled;

				if (isFastForwardingOrRewinding != _lastFastForwardingOrRewinding)
				{
					InitializeFpsData();
				}

				_lastFastForwardingOrRewinding = isFastForwardingOrRewinding;

				// client input-related duties
				OSD.ClearGuiText();

				CheatList.Pulse();

				// zero 03-may-2014 - moved this before call to UpdateToolsBefore(), since it seems to clear the state which a lua event.framestart is going to want to alter
				InputManager.ClickyVirtualPadController.FrameTick();
				InputManager.ButtonOverrideAdapter.FrameTick();

				if (IsTurboing)
				{
					Tools.FastUpdateBefore();
				}
				else
				{
					Tools.UpdateToolsBefore();
				}

				if (!InvisibleEmulation)
				{
					CaptureRewind(isRewinding);
				}

				// Set volume, if enabled
				if (Config.SoundEnabledNormal && !InvisibleEmulation)
				{
					atten = Config.SoundVolume / 100.0f;

					if (isFastForwardingOrRewinding)
					{
						if (Config.SoundEnabledRWFF)
						{
							atten *= Config.SoundVolumeRWFF / 100.0f;
						}
						else
						{
							atten = 0;
						}
					}

					// Mute if using Frame Advance/Frame Progress
					if (_runloopFrameAdvance && Config.MuteFrameAdvance)
					{
						atten = 0;
					}
				}

				MovieSession.HandleFrameBefore();

				RA?.OnFrameAdvance();

				if (Config.AutosaveSaveRAM)
				{
					AutoFlushSaveRamIn--;
					if (AutoFlushSaveRamIn <= 0)
					{
						FlushSaveRAM(true);
						AutoFlushSaveRamIn = Config.FlushSaveRamFrames;
					}
				}
				// why not skip audio if the user doesn't want sound
				bool renderSound = (Config.SoundEnabled && !IsTurboing)
					|| _currAviWriter?.UsesAudio is true;
				if (!renderSound)
				{
					atten = 0;
				}

				bool atTurboSeekEnd = IsTurboSeeking && Emulator.Frame == PauseOnFrame.Value - 1;
				bool render = !InvisibleEmulation && (!_throttle.skipNextFrame || _currAviWriter?.UsesVideo is true || atTurboSeekEnd);
				bool newFrame = Emulator.FrameAdvance(InputManager.ControllerOutput, render, renderSound);

				MovieSession.HandleFrameAfter(ToolBypassingMovieEndAction is not null);

				if (returnToRecording)
				{
					MovieSession.Movie.SwitchToRecord();
				}

				if (isRewinding && ToolControllingRewind is null && MovieSession.Movie.IsRecording())
				{
					MovieSession.Movie.Truncate(Emulator.Frame);
					if (!_wasRewinding)
						MovieSession.Movie.Rerecords++;
				}

				CheatList.Pulse();

				if (Emulator.CanPollInput() && Emulator.AsInputPollable().IsLagFrame && Config.AutofireLagFrames)
				{
					InputManager.AutoFireController.IncrementStarts();
				}

				InputManager.StickyAutofireController.IncrementLoops(Emulator.CanPollInput() && Emulator.AsInputPollable().IsLagFrame);

				PressFrameAdvance = false;

				// Update tools, but not if we're at the end of a turbo seek. In that case, updating will happen later when the seek is ended.
				if (!atTurboSeekEnd)
				{
					if (IsTurboing)
					{
						Tools.FastUpdateAfter();
					}
					else
					{
						UpdateToolsAfter();
					}
				}

				if (!PauseAvi && newFrame && !InvisibleEmulation)
				{
					AvFrameAdvance();
				}

				if (newFrame)
				{
					_framesSinceLastFpsUpdate++;

					CalcFramerateAndUpdateDisplay(currentTimestamp, isRewinding, isFastForwarding);
				}

				if (IsSeeking && PauseOnFrame.Value <= Emulator.Frame)
				{
					if (PauseOnFrame.Value == Emulator.Frame)
					{
						PauseEmulator();
						PauseOnFrame = null;
					}
				}

				_wasRewinding = isRewinding;
			}
			else if (isRewinding)
			{
				// Tools will want to be updated after rewind (load state), but we only need to manually do this if we did not frame advance.
				UpdateToolsAfter();
			}

			Sound.UpdateSound(atten, DisableSecondaryThrottling);
		}

		private void CalcFramerateAndUpdateDisplay(long currentTimestamp, bool isRewinding, bool isFastForwarding)
		{
			double elapsedSeconds = (currentTimestamp - _timestampLastFpsUpdate) / (double)Stopwatch.Frequency;

			if (elapsedSeconds < 1.0 / _fpsUpdatesPerSecond)
			{
				return;
			}

			if (_lastFps == 0) // Initial calculation
			{
				_lastFps = (_framesSinceLastFpsUpdate - 1) / elapsedSeconds;
			}
			else
			{
				_lastFps = (_lastFps + (_framesSinceLastFpsUpdate * _fpsSmoothing)) / (1.0 + (elapsedSeconds * _fpsSmoothing));
			}
			_lastFpsRounded = (int) Math.Round(_lastFps);

			_framesSinceLastFpsUpdate = 0;
			_timestampLastFpsUpdate = currentTimestamp;

			var fpsString = $"{_lastFpsRounded} fps";
			if (isRewinding)
			{
				fpsString += IsTurboing || isFastForwarding ?
					" <<<<" :
					" <<";
			}
			else if (isFastForwarding)
			{
				fpsString += IsTurboing ?
					" >>>>" :
					" >>";
			}

			OSD.Fps = fpsString;

			// need to refresh window caption in this case
			if (Config.DispSpeedupFeatures is 0) UpdateWindowTitle();
		}

		private void InitializeFpsData()
		{
			_lastFps = _lastFpsRounded = 0;
			_timestampLastFpsUpdate = Stopwatch.GetTimestamp();
			_framesSinceLastFpsUpdate = 0;
		}

		/// <summary>
		/// start AVI recording, unattended
		/// </summary>
		/// <param name="videoWriterName">match the short name of an <see cref="IVideoWriter"/></param>
		/// <param name="filename">filename to save to</param>
		private void RecordAv(string videoWriterName, string filename)
		{
			RecordAvBase(videoWriterName, filename, true);
		}

		/// <summary>
		/// start AV recording, asking user for filename and options
		/// </summary>
		private void RecordAv()
		{
			RecordAvBase(null, null, false);
		}

		/// <summary>
		/// start AV recording
		/// </summary>
		private void RecordAvBase(string videoWriterName, string filename, bool unattended)
		{
			if (_currAviWriter != null) return;

			if (Game.IsNullInstance()) throw new InvalidOperationException("how is an A/V recording starting with no game loaded? please report this including as much detail as possible");

			// select IVideoWriter to use
			IVideoWriter aw;

			if (string.IsNullOrEmpty(videoWriterName) && !string.IsNullOrEmpty(Config.VideoWriter))
			{
				videoWriterName = Config.VideoWriter;
			}

			if (unattended && !string.IsNullOrEmpty(videoWriterName))
			{
				aw = VideoWriterInventory.GetVideoWriter(videoWriterName, this);
			}
			else
			{
				aw = VideoWriterChooserForm.DoVideoWriterChooserDlg(
					VideoWriterInventory.GetAllWriters(),
					this,
					Emulator,
					Config);
			}

			if (aw == null)
			{
				AddOnScreenMessage(
					unattended ? $"Couldn't start video writer \"{videoWriterName}\"" : "A/V capture canceled.");

				return;
			}

			try
			{
#if AVI_SUPPORT
				bool usingAvi = aw is AviWriter; // SO GROSS!
#else
				const bool usingAvi = false;
#endif

				aw = Config.VideoWriterAudioSyncEffective ? new VideoStretcher(aw) : new AudioStretcher(aw);
				aw.SetMovieParameters(Emulator.VsyncNumerator(), Emulator.VsyncDenominator());
				(IVideoProvider output, Action dispose) = GetCaptureProvider();
				aw.SetVideoParameters(output.BufferWidth, output.BufferHeight);
				if (dispose != null) dispose();

				aw.SetAudioParameters(44100, 2, 16);

				// select codec token
				// do this before save dialog because ffmpeg won't know what extension it wants until it's been configured
				if (unattended && !string.IsNullOrEmpty(filename))
				{
					aw.SetDefaultVideoCodecToken(Config);
				}
				else
				{
					// THIS IS REALLY SLOPPY!
					// PLEASE REDO ME TO NOT CARE WHICH AVWRITER IS USED!
					if (usingAvi && !string.IsNullOrEmpty(Config.AviCodecToken))
					{
						aw.SetDefaultVideoCodecToken(Config);
					}

					var token = aw.AcquireVideoCodecToken(Config);
					if (token == null)
					{
						AddOnScreenMessage("A/V capture canceled.");
						aw.Dispose();
						return;
					}

					aw.SetVideoCodecToken(token);
				}

				// select file to save to
				if (unattended && !string.IsNullOrEmpty(filename))
				{
					aw.OpenFile(filename);
				}
				else
				{
					string ext = aw.DesiredExtension();
					string pathForOpenFile;

					// handle directories first
					if (ext == "<directory>")
					{
						using var fbd = new FolderBrowserEx();
						if (this.ShowDialogWithTempMute(fbd) is DialogResult.Cancel)
						{
							aw.Dispose();
							return;
						}

						pathForOpenFile = fbd.SelectedPath;
					}
					else
					{
						var result = this.ShowFileSaveDialog(
							filter: new(new FilesystemFilter(ext, new[] { ext })),
							initDir: Config.PathEntries.AvAbsolutePath(),
							initFileName: $"{(MovieSession.Movie.IsActive() ? Path.GetFileNameWithoutExtension(MovieSession.Movie.Filename) : Game.FilesystemSafeName())}.{ext}");
						if (result is null)
						{
							aw.Dispose();
							return;
						}
						pathForOpenFile = result;
					}

					aw.OpenFile(pathForOpenFile);
				}

				// commit the avi writing last, in case there were any errors earlier
				_currAviWriter = aw;
				AddOnScreenMessage("A/V capture started");
				AVStatusLabel.Image = Properties.Resources.Avi;
				AVStatusLabel.ToolTipText = "A/V capture in progress";
				AVStatusLabel.Visible = true;
			}
			catch
			{
				AddOnScreenMessage("A/V capture failed!");
				aw.Dispose();
				throw;
			}

			if (Config.VideoWriterAudioSyncEffective)
			{
				_currentSoundProvider.SetSyncMode(SyncSoundMode.Sync);
			}
			else
			{
				if (_currentSoundProvider.CanProvideAsync)
				{
					_currentSoundProvider.SetSyncMode(SyncSoundMode.Async);
					_aviSoundInputAsync = _currentSoundProvider;
				}
				else
				{
					_currentSoundProvider.SetSyncMode(SyncSoundMode.Sync);
					_aviSoundInputAsync = new SyncToAsyncProvider(() => Emulator.VsyncRate(), _currentSoundProvider);
				}
			}

			_dumpProxy = new SimpleSyncSoundProvider();
			RewireSound();
		}

		private (IVideoProvider Output, Action/*?*/ Dispose) GetCaptureProvider()
		{
			// TODO ZERO - this code is pretty jacked. we'll want to frugalize buffers better for speedier dumping, and we might want to rely on the GL layer for padding
			if (Config.AVWriterResizeWidth > 0 && Config.AVWriterResizeHeight > 0)
			{
				BitmapBuffer bbIn = null;
				Bitmap bmpIn = null;
				try
				{
					bbIn = Config.AviCaptureOsd
						? CaptureOSD()
						: new BitmapBuffer(_currentVideoProvider.BufferWidth, _currentVideoProvider.BufferHeight, _currentVideoProvider.GetVideoBuffer());

					bbIn.DiscardAlpha();

					Bitmap bmpOut = new(width: Config.AVWriterResizeWidth, height: Config.AVWriterResizeHeight, PixelFormat.Format32bppArgb);
					bmpIn = bbIn.ToSysdrawingBitmap();
					using (var g = Graphics.FromImage(bmpOut))
					{
						if (Config.AVWriterPad)
						{
							g.Clear(Color.FromArgb(_currentVideoProvider.BackgroundColor));
							g.DrawImageUnscaled(bmpIn, (bmpOut.Width - bmpIn.Width) / 2, (bmpOut.Height - bmpIn.Height) / 2);
						}
						else
						{
							g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
							g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
							g.DrawImage(bmpIn, new Rectangle(0, 0, bmpOut.Width, bmpOut.Height));
						}
					}

					IVideoProvider output = new BmpVideoProvider(bmpOut, _currentVideoProvider.VsyncNumerator, _currentVideoProvider.VsyncDenominator);
					return (output, bmpOut.Dispose);
				}
				finally
				{
					bbIn?.Dispose();
					bmpIn?.Dispose();
				}
			}
			else
			{
				BitmapBuffer source = null;
				if (Config.AviCaptureOsd)
				{
					source = CaptureOSD();
				}
				else if (Config.AviCaptureLua)
				{
					source = CaptureLua();
				}

				if (source != null)
				{
					return (new BitmapBufferVideoProvider(source), source.Dispose);
				}
				else
				{
					return (_currentVideoProvider, null);
				}
			}
		}

		private void AbortAv()
		{
			if (_currAviWriter == null)
			{
				_dumpProxy = null;
				RewireSound();
				return;
			}

			_currAviWriter.Dispose();
			_currAviWriter = null;
			AddOnScreenMessage("A/V capture aborted");
			AVStatusLabel.Image = Properties.Resources.Blank;
			AVStatusLabel.ToolTipText = "";
			AVStatusLabel.Visible = false;
			_aviSoundInputAsync = null;
			_dumpProxy = null; // return to normal sound output
			RewireSound();
		}

		private void StopAv()
		{
			if (_currAviWriter == null)
			{
				_dumpProxy = null;
				RewireSound();
				return;
			}

			_currAviWriter.CloseFile();
			_currAviWriter.Dispose();
			_currAviWriter = null;
			AddOnScreenMessage("A/V capture stopped");
			AVStatusLabel.Image = Properties.Resources.Blank;
			AVStatusLabel.ToolTipText = "";
			AVStatusLabel.Visible = false;
			_aviSoundInputAsync = null;
			_dumpProxy = null; // return to normal sound output
			RewireSound();
		}

		private void AvFrameAdvance()
		{
			// is this the best time to handle this? or deeper inside?
			if (_argParser._currAviWriterFrameList?.Contains(Emulator.Frame) != false)
			{
				if (_currAviWriter == null) return;
				Action dispose = null;
				try
				{
					_currAviWriter.SetFrame(Emulator.Frame);

					(IVideoProvider output, dispose) = GetCaptureProvider();

					short[] samp;
					int nsamp;
					if (Config.VideoWriterAudioSyncEffective)
					{
						((VideoStretcher) _currAviWriter).DumpAV(output, _currentSoundProvider, out samp, out nsamp);
					}
					else
					{
						((AudioStretcher) _currAviWriter).DumpAV(output, _aviSoundInputAsync, out samp, out nsamp);
					}

					_dumpProxy.PutSamples(samp, nsamp);
				}
				catch (Exception e)
				{
					ShowMessageBox(owner: null, $"Video dumping died:\n\n{e}");
					AbortAv();
				}
				finally
				{
					if (dispose != null) dispose();
				}
			}

			if (_autoDumpLength > 0) //TODO this is probably not necessary because of the call to StopAv --yoshi
			{
				_autoDumpLength--;
				if (_autoDumpLength == 0) // finish
				{
					StopAv();
					if (_argParser._autoCloseOnDump) ScheduleShutdown();
				}
			}
		}

		private int? LoadArchiveChooser(HawkFile file)
		{
			using var ac = new ArchiveChooser(file);
			if (this.ShowDialogAsChild(ac).IsOk())
			{
				return ac.SelectedMemberIndex;
			}

			return null;
		}

		public string SaveStatePrefix()
		{
			var name = Game.FilesystemSafeName();
			name += $".{Emulator.Attributes().CoreName}";

			// Bsnes profiles have incompatible savestates so save the profile name
			if (Emulator is LibsnesCore bsnes)
			{
				name += $".{bsnes.CurrentProfile}";
			}

			if (MovieSession.Movie.IsActive())
			{
				name += $".{Path.GetFileNameWithoutExtension(MovieSession.Movie.Filename)}";
			}

			var pathEntry = Config.PathEntries.SaveStateAbsolutePath(Game.System);

			return Path.Combine(pathEntry, name);
		}

		private void ShowLoadError(object sender, RomLoader.RomErrorArgs e)
		{
			if (e.Type == RomLoader.LoadErrorType.MissingFirmware)
			{
				if (this.ShowMessageBox2(
					caption: "Missing Firmware!",
					icon: EMsgBoxIcon.Error,
					text: $"{e.Message}\n\nOpen the firmware manager now?",
					useOKCancel: true))
				{
					OpenFWConfigRomLoadFailed(e);
					if (e.Retry)
					{
						// Retry loading the ROM here. This leads to recursion, as the original call to LoadRom has not exited yet,
						// but unless the user tries and fails to set his firmware a lot of times, nothing should happen.
						// Refer to how RomLoader implemented its LoadRom method for a potential fix on this.
						_ = LoadRom(e.RomPath, _currentLoadRomArgs);
					}
				}
			}
			else
			{
				string title = "load error";
				if (e.AttemptedCoreLoad != null)
				{
					title = $"{e.AttemptedCoreLoad} load error";
				}

				this.ModalMessageBox(e.Message, title, EMsgBoxIcon.Error);
			}
		}

		private string ChoosePlatformForRom(RomGame rom)
		{
			using var platformChooser = new PlatformChooser(Config)
			{
				RomGame = rom,
			};
			this.ShowDialogWithTempMute(platformChooser);
			return platformChooser.PlatformChoice;
		}

		private LoadRomArgs _currentLoadRomArgs;
		private bool _isLoadingRom;

		public bool LoadRom(string path, LoadRomArgs args) => LoadRom(path, args, out _);

		public bool LoadRom(string path, LoadRomArgs args, out bool failureIsFromAskSave)
		{
			if (!LoadRomInternal(path, args, out failureIsFromAskSave))
				return false;

			// what's the meaning of the last rom path when opening an archive? based on the archive file location
			if (args.OpenAdvanced is OpenAdvanced_OpenRom)
			{
				var physicalPath = path;
				if (HawkFile.SplitArchiveMemberPath(path) is { } split) physicalPath = split.ArchivePath;
				Config.PathEntries.LastRomPath = Path.GetDirectoryName(Path.GetFullPath(physicalPath)) ?? "";
			}

			return true;
		}

		// Still needs a good bit of refactoring
		private bool LoadRomInternal(string path, LoadRomArgs args, out bool failureIsFromAskSave)
		{
			failureIsFromAskSave = false;
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			if (args == null)
				throw new ArgumentNullException(nameof(args));

			_isLoadingRom = true;
			path = EmuHawkUtil.ResolveShortcut(path);

			// if this is the first call to LoadRom (they will come in recursively) then stash the args
			bool firstCall = false;
			if (_currentLoadRomArgs == null)
			{
				firstCall = true;
				_currentLoadRomArgs = args;
			}
			else
			{
				args = _currentLoadRomArgs;
			}

			try
			{
				// movies should require deterministic emulation in ALL cases
				// if the core is managing its own DE through SyncSettings a 'deterministic' bool can be passed into the core's constructor
				// it is then up to the core itself to override its own local DeterministicEmulation setting
				bool deterministic = args.Deterministic ?? MovieSession.NewMovieQueued;

				if (!Tools.AskSave())
				{
					failureIsFromAskSave = true;
					return false;
				}

				var loader = new RomLoader(Config, this)
				{
					ChooseArchive = LoadArchiveChooser,
					ChoosePlatform = romGame => args.ForcedSysID ?? ChoosePlatformForRom(romGame),
					Deterministic = deterministic,
					OpenAdvanced = args.OpenAdvanced,
				};
				FirmwareManager.RecentlyServed.Clear();

				loader.OnLoadError += ShowLoadError;
				loader.OnLoadSettings += CoreSettings;
				loader.OnLoadSyncSettings += CoreSyncSettings;

				// this also happens in CloseGame(). But it needs to happen here since if we're restarting with the same core,
				// any settings changes that we made need to make it back to config before we try to instantiate that core with
				// the new settings objects
				CommitCoreSettingsToConfig(); // adelikat: I Think by reordering things, this isn't necessary anymore
				CloseGame();

				var nextComm = CreateCoreComm();

				IOpenAdvanced ioa = args.OpenAdvanced;
				var oaOpenrom = ioa as OpenAdvanced_OpenRom;
				var ioaRetro = ioa as IOpenAdvancedLibretro;

				var forcedCoreName = MovieSession.QueuedCoreName;

				// we need to inform LoadRom which Libretro core to use...
				if (ioaRetro != null)
				{
					// prepare a core specification
					// if it wasn't already specified, use the current default
					if (ioaRetro.CorePath == null)
					{
						ioaRetro.CorePath = Config.LibretroCore;
					}

					if (ioaRetro.CorePath == null)
					{
						throw new InvalidOperationException("Can't load a file via Libretro until a core is specified");
					}
				}
				else if (forcedCoreName is not null)
				{
					var availCores = CoreInventory.Instance.AllCores.GetValueOrDefault(MovieSession.QueuedSysID, [ ]);
					if (!availCores.Exists(core => core.Name == forcedCoreName))
					{
						const string FMT_STR_NO_SUCH_CORE = "This movie is for the \"{0}\" core,"
							+ " but that's not a valid {1} core. (Was the movie made in this version of EmuHawk?)"
							+ "\nContinue with your preferred core instead?";
						//TODO let the user pick from `availCores`?
						// also use a different message when `availCores` is empty (which might happen if someone makes a movie on a new core and tries to load it in a version without the core)
						if (!this.ModalMessageBox2(
							caption: "No such core",
							icon: EMsgBoxIcon.Error,
							text: string.Format(
								FMT_STR_NO_SUCH_CORE,
								forcedCoreName,
								EmulatorExtensions.SystemIDToDisplayName(MovieSession.QueuedSysID))))
						{
							return false;
						}
						forcedCoreName = null;
					}
				}

				DisplayManager.ActivateOpenGLContext(); // required in case the core wants to create a shared OpenGL context

				var result = loader.LoadRom(
					path: path,
					nextComm,
					launchLibretroCore: ioaRetro?.CorePath,
					forcedCoreName: forcedCoreName);

				// we need to replace the path in the OpenAdvanced with the canonical one the user chose.
				// It can't be done until loader.LoadRom happens (for CanonicalFullPath)
				// i'm not sure this needs to be more abstractly engineered yet until we have more OpenAdvanced examples
				if (ioa is OpenAdvanced_Libretro oaRetro)
				{
					oaRetro.token.Path = loader.CanonicalFullPath;
				}

				if (oaOpenrom != null)
				{
					oaOpenrom.Path = loader.CanonicalFullPath;
				}

				if (ioa is OpenAdvanced_MAME oaMame)
				{
					oaMame.Path = loader.CanonicalFullPath;
				}

				var openAdvancedArgs = $"*{OpenAdvancedSerializer.Serialize(ioa)}";
				Config.RecentRoms.Add(openAdvancedArgs);

				if (result)
				{
					Emulator.Dispose();
					Emulator = loader.LoadedEmulator;
					Game = loader.Game;
					Config.RecentCores.Enqueue(Emulator.Attributes().CoreName);
					while (Config.RecentCores.Count > 5) Config.RecentCores.Dequeue();
					InputManager.SyncControls(Emulator, MovieSession, Config);
					_multiDiskMode = false;

					if (loader.XMLGameInfo is XmlGame xmlGame && Emulator is not LibsnesCore)
					{
						// this is a multi-disk bundler file
						// determine the xml assets and create RomStatusDetails for all of them
						using var xSw = new StringWriter();

						for (int xg = 0; xg < xmlGame.Assets.Count; xg++)
						{
							var (_, filename, data) = xmlGame.Assets[xg];
							// data length is 0 in the case of discs or 3DS roms
							if (data.Length == 0)
							{
								xSw.WriteLine(Path.GetFileNameWithoutExtension(filename));
								xSw.WriteLine("SHA1:N/A");
								xSw.WriteLine("MD5:N/A");
								xSw.WriteLine();
							}
							else
							{
								xSw.WriteLine(filename);
								xSw.WriteLine(SHA1Checksum.ComputePrefixedHex(data));
								xSw.WriteLine(MD5Checksum.ComputePrefixedHex(data));
								xSw.WriteLine();
							}
						}

						_defaultRomDetails = xSw.ToString();
						_multiDiskMode = true;
					}

					if (loader.LoadedEmulator is NES nes)
					{
						if (!string.IsNullOrWhiteSpace(nes.GameName))
						{
							Game.Name = nes.GameName;
						}

						Game.Status = nes.RomStatus;
					}
					else if (loader.LoadedEmulator is QuickNES qns)
					{
						if (!string.IsNullOrWhiteSpace(qns.BootGodName))
						{
							Game.Name = qns.BootGodName;
						}

						if (qns.BootGodStatus.HasValue)
						{
							Game.Status = qns.BootGodStatus.Value;
						}
					}

					var romDetails = Emulator.RomDetails();
					if (string.IsNullOrWhiteSpace(romDetails) && loader.Rom != null)
					{
						_defaultRomDetails = $"{Game.Name}\r\n{SHA1Checksum.ComputePrefixedHex(loader.Rom.RomData)}\r\n{MD5Checksum.ComputePrefixedHex(loader.Rom.RomData)}\r\n";
					}
					else if (string.IsNullOrWhiteSpace(romDetails) && loader.Rom == null)
					{
						// single disc game
						_defaultRomDetails = $"{Game.Name}\r\nSHA1:N/A\r\nMD5:N/A\r\n";
					}

					if (Emulator.HasBoardInfo())
					{
						Console.WriteLine("Core reported BoardID: \"{0}\"", Emulator.AsBoardInfo().BoardName);
					}

					// Don't load Save Ram if a movie is being loaded
					if (!MovieSession.NewMovieQueued)
					{
						LoadSaveRam();
						AutoFlushSaveRamIn = Config.FlushSaveRamFrames;
					}

					var previousRom = CurrentlyOpenRom;
					CurrentlyOpenRom = oaOpenrom?.Path ?? openAdvancedArgs;
					CurrentlyOpenRomArgs = args;

					Tools.Restart(Config, Emulator, Game);

					if (previousRom != CurrentlyOpenRom)
					{
						CheatList.NewList(Tools.GenerateDefaultCheatFilename(), autosave: true);
						if (Config.Cheats.LoadFileByGame && Emulator.HasMemoryDomains())
						{
							if (CheatList.AttemptToLoadCheatFile(Emulator.AsMemoryDomains()))
							{
								AddOnScreenMessage("Cheats file loaded");
							}
						}
					}
					else
					{
						if (Emulator.HasMemoryDomains())
						{
							CheatList.UpdateDomains(Emulator.AsMemoryDomains());
						}
						else
						{
							CheatList.NewList(Tools.GenerateDefaultCheatFilename(), autosave: true);
						}
					}

					OnRomChanged();
					DisplayManager.UpdateGlobals(Config, Emulator);
					DisplayManager.Blank();
					CreateRewinder();

					RewireSound();
					Tools.UpdateCheatRelatedTools(null, new(null));
					if (!MovieSession.NewMovieQueued && Config.AutoLoadLastSaveSlot && HasSlot(Config.SaveSlot))
					{
						_ = LoadstateCurrentSlot();
					}

					if (FirmwareManager.RecentlyServed.Count > 0)
					{
						Console.WriteLine("Active firmware:");
						foreach (var f in FirmwareManager.RecentlyServed)
						{
							Console.WriteLine($"\t{f.ID} : {f.Hash}");
						}
					}

					ExtToolManager.BuildToolStrip();

					RomLoaded?.Invoke(this, EventArgs.Empty);

					// Some window messages like paints may be dispatched during this function call and before it returns.
					// Therefore this needs to be called very late after tools have been restarted
					// to ensure no stale references like disposed cores are being used, see https://github.com/TASEmulators/BizHawk/issues/4436.
					if (!OSTailoredCode.IsUnixHost) JumpLists.AddRecentItem(openAdvancedArgs, ioa.DisplayName);

					return true;
				}
				else if (Emulator.IsNull())
				{
					// This shows up if there's a problem
					Tools.Restart(Config, Emulator, Game);
					DisplayManager.UpdateGlobals(Config, Emulator);
					DisplayManager.Blank();
					ExtToolManager.BuildToolStrip();
					CheatList.NewList("", autosave: true);
					OnRomChanged();
					return false;
				}
				else
				{
					// The ROM has been loaded by a recursive invocation of the LoadROM method.
					RomLoaded?.Invoke(this, EventArgs.Empty);
					return true;
				}
			}
			finally
			{
				if (firstCall)
				{
					_currentLoadRomArgs = null;
				}

				_isLoadingRom = false;
			}
		}

		private void OnRomChanged()
		{
			OSD.Fps = "0 fps";
			UpdateWindowTitle();
			HandlePlatformMenus();
			_stateSlots.ClearRedoList();
			UpdateStatusSlots();
			UpdateCoreStatusBarButton();
			UpdateDumpInfo();
			SetMainformMovieInfo();
			RA?.Restart();
		}

		private void CommitCoreSettingsToConfig()
		{
			// save settings object
			var t = Emulator.GetType();
			var settable = GetSettingsAdapterForLoadedCoreUntyped();

			if (settable.HasSettings)
			{
				Config.PutCoreSettings(settable.GetSettings(), t);
			}

			if (settable.HasSyncSettings && MovieSession.Movie.NotActive())
			{
				// don't trample config with loaded-from-movie settings
				Config.PutCoreSyncSettings(settable.GetSyncSettings(), t);
			}
		}

		// whats the difference between these two methods??
		// its very tricky. rename to be more clear or combine them.
		// This gets called whenever a core related thing is changed.
		// Like reboot core.
		private void CloseGame(bool clearSram = false)
		{
			GameIsClosing = true;
			if (clearSram)
			{
				var path = Config.PathEntries.SaveRamAbsolutePath(Game, MovieSession.Movie);
				if (File.Exists(path))
				{
					File.Delete(path);
					AddOnScreenMessage("SRAM cleared.");
				}
			}
			else if (Emulator.HasSaveRam())
			{
				while (true)
				{
					if (FlushSaveRAM()) break;

					var result = ShowMessageBox3(
						owner: this,
						"Failed flushing the game's Save RAM to your disk.\n" +
						"Do you want to try again?",
						"IOError while writing SaveRAM",
						EMsgBoxIcon.Error);

					if (result is false) break;
					if (result is null) return;
				}
			}

			StopAv();
			AutoSaveStateIfConfigured();

			CommitCoreSettingsToConfig();
			DisableRewind();

			if (MovieSession.Movie.IsActive()) // Note: this must be called after CommitCoreSettingsToConfig()
			{
				StopMovie();
			}

			RA?.Stop();

			CheatList.SaveOnClose();
			Emulator.Dispose();
			Emulator = new NullEmulator();
			Game = GameInfo.NullInstance;
			InputManager.SyncControls(Emulator, MovieSession, Config);
			RewireSound();
			RebootStatusBarIcon.Visible = false;
			GameIsClosing = false;
		}

		private void AutoSaveStateIfConfigured()
		{
			if (Config.AutoSaveLastSaveSlot && Emulator.HasSavestates()) SavestateCurrentSlot();
		}

		public bool GameIsClosing { get; private set; } // Lets tools make better decisions when being called by CloseGame

		public void CloseRom(bool clearSram = false)
		{
			// This gets called after Close Game gets called.
			// Tested with NESHawk and SMB3 (U)
			if (Tools.AskSave())
			{
				CloseGame(clearSram);
				Tools.Restart(Config, Emulator, Game);
				DisplayManager.UpdateGlobals(Config, Emulator);
				ExtToolManager.BuildToolStrip();
				PauseOnFrame = null;
				CurrentlyOpenRom = null;
				CurrentlyOpenRomArgs = null;
				CheatList.NewList("", autosave: true);
				OnRomChanged();
			}
		}

		private void ProcessMovieImport(string fn, bool start)
		{
			var result = MovieImport.ImportFile(this, MovieSession, fn, Config);

			if (result.Errors.Count is not 0)
			{
				ShowMessageBox(owner: null, string.Join("\n", result.Errors), "Conversion error", EMsgBoxIcon.Error);
				return;
			}

			if (result.Warnings.Count is not 0)
			{
				AddOnScreenMessage(result.Warnings[0]); // For now, just show the first warning
			}

			AddOnScreenMessage($"{Path.GetFileName(fn)} imported as {result.Movie.Filename}");

			if (start)
			{
				StartNewMovie(result.Movie, false);
				Config.RecentMovies.Add(result.Movie.Filename);
			}
		}

		public void EnableRewind(bool enabled)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			if (Rewinder == null)
			{
				CreateRewinder();
			}

			// CreateRewinder doesn't necessarily create an instance of rewinder, still need to check null
			if (Rewinder != null)
			{
				if (enabled)
				{
					Rewinder.Resume();
				}
				else
				{
					Rewinder.Suspend();
				}

				AddOnScreenMessage($"Rewind {(enabled ? "enabled" : "suspended")}");
			}
			UpdateStatusBarRewindIndicator();
		}

		public void DisableRewind()
		{
			Rewinder?.Dispose();
			Rewinder = null;
			UpdateStatusBarRewindIndicator();
		}

		public BitmapBuffer/*?*/ ReadScreenshotFromSavestate(int slot)
		{
			if (!Emulator.HasSavestates()) return null;
			var path = $"{SaveStatePrefix()}.QuickSave{slot % 10}.State";
			return File.Exists(path) ? SavestateFile.GetFrameBufferFrom(path) : null;
		}

		public bool LoadState(string path, string userFriendlyStateName, bool suppressOSD = false) // Move to client.common
		{
			if (!Emulator.HasSavestates()) return false;
			if (ToolControllingSavestates is { } tool) return tool.LoadState();

			if (!new SavestateFile(Emulator, MovieSession, MovieSession.UserBag).Load(path, this))
			{
				AddOnScreenMessage("Loadstate error!");
				return false;
			}

			OSD.ClearGuiText();
			if (SavestateLoaded is not null)
			{
				StateLoadedEventArgs args = new(userFriendlyStateName);
				SavestateLoaded(this, args);
			}
			RA?.OnLoadState(path);

			if (Tools.Has<LuaConsole>()) Tools.LuaConsole.CallStateLoadCallbacks(userFriendlyStateName);

			SetMainformMovieInfo();
			UpdateToolsAfter();
			UpdateToolsLoadstate();
			InputManager.AutoFireController.ClearStarts();

			//we don't want to analyze how to intermix movies, rewinding, and states
			//so purge rewind history when loading a state while doing a movie
			if (ToolControllingRewind is null && MovieSession.Movie.IsActive())
			{
				Rewinder?.Clear();
			}

			if (!suppressOSD)
			{
				AddOnScreenMessage($"Loaded state: {userFriendlyStateName}");
			}
			return true;
		}

		public bool LoadQuickSave(int slot, bool suppressOSD = false)
		{
			if (!Emulator.HasSavestates()) return false;

			var quickSlotName = $"QuickSave{slot % 10}";
			var handled = false;
			if (QuicksaveLoad is not null)
			{
				BeforeQuickLoadEventArgs args = new(quickSlotName);
				QuicksaveLoad(this, args);
				handled = args.Handled;
			}
			if (handled) return true; // not sure

			if (ToolControllingSavestates is { } tool) return tool.LoadQuickSave(slot);

			var path = $"{SaveStatePrefix()}.{quickSlotName}.State";
			if (!File.Exists(path))
			{
				AddOnScreenMessage($"Unable to load {quickSlotName}.State");
				return false;
			}

			return LoadState(path: path, userFriendlyStateName: quickSlotName, suppressOSD: suppressOSD);
		}

		public void SaveState(string path, string userFriendlyStateName, bool fromLua = false, bool suppressOSD = false)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			if (ToolControllingSavestates is { } tool)
			{
				tool.SaveState();
				return;
			}

			if (MovieSession.Movie.IsActive() && Emulator.Frame > MovieSession.Movie.FrameCount)
			{
				AddOnScreenMessage("Cannot savestate after movie end!");
				return;
			}

			try
			{
				new SavestateFile(Emulator, MovieSession, MovieSession.UserBag).Create(path, Config.Savestates);

				if (SavestateSaved is not null)
				{
					StateSavedEventArgs args = new(userFriendlyStateName);
					SavestateSaved(this, args);
				}
				RA?.OnSaveState(path);

				if (!suppressOSD)
				{
					AddOnScreenMessage($"Saved state: {userFriendlyStateName}");
				}
			}
			catch (IOException)
			{
				AddOnScreenMessage($"Unable to save state {path}");
			}

			if (!fromLua)
			{
				UpdateStatusSlots();
			}
		}

		// TODO: should backup logic be stuffed in into Client.Common.SaveStateManager?
		public void SaveQuickSave(int slot, bool suppressOSD = false, bool fromLua = false)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}
			var quickSlotName = $"QuickSave{slot % 10}";
			var handled = false;
			if (QuicksaveSave is not null)
			{
				BeforeQuickSaveEventArgs args = new(quickSlotName);
				QuicksaveSave(this, args);
				handled = args.Handled;
			}
			if (handled)
			{
				return;
			}

			if (ToolControllingSavestates is { } tool)
			{
				tool.SaveQuickSave(slot);
				return;
			}

			var path = $"{SaveStatePrefix()}.{quickSlotName}.State";
			new FileInfo(path).Directory?.Create();

			// Make backup first
			if (Config.Savestates.MakeBackups)
			{
				Util.TryMoveBackupFile(path, $"{path}.bak");
			}

			SaveState(path, quickSlotName, fromLua, suppressOSD);

			if (Tools.Has<LuaConsole>()) Tools.LuaConsole.CallStateSaveCallbacks(quickSlotName);
		}

		public bool EnsureCoreIsAccurate()
		{
			bool PromptToSwitchCore(string currentCore, string recommendedCore, Action disableCurrentCore)
			{
				using var box = new MsgBox(
					$"While the {currentCore} core is faster, it is not nearly as accurate as {recommendedCore}.{Environment.NewLine}It is recommended that you switch to the {recommendedCore} core for movie recording.{Environment.NewLine}Switch to {recommendedCore}?",
					"Accuracy Warning",
					MessageBoxIcon.Warning);

				box.SetButtons(
					new[] { "Switch", "Continue" },
					new[] { DialogResult.Yes, DialogResult.Cancel });

				box.MaximumSize = UIHelper.Scale(new Size(575, 175));
				box.SetMessageToAutoSize();
				if (this.ShowDialogWithTempMute(box) is not DialogResult.Yes) return false;

				disableCurrentCore();
				RebootCore();
				return true;
			}

			var currentCoreName = Emulator.Attributes().CoreName;
			var recommendedCore = currentCoreName switch
			{
				CoreNames.Snes9X => CoreNames.Bsnes115,
				CoreNames.TI83Hawk => CoreNames.Emu83,
				CoreNames.QuickNes => CoreNames.NesHawk,
				CoreNames.Atari2600Hawk => CoreNames.Stella,
				CoreNames.HyperNyma => CoreNames.TurboNyma,
				_ => null,
			};
			return recommendedCore is null
				? true
				: PromptToSwitchCore(currentCoreName, recommendedCore, () => Config.PreferredCores[Emulator.SystemId] = recommendedCore);
		}

		private void SaveStateAs()
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			// allow named state export for tastudio, since it's safe, unlike loading one
			// todo: make it not save laglog in that case
			if (Tools.IsLoaded<TAStudio>())
			{
				Tools.TAStudio.NamedStatePending = true;
			}

			if (ToolControllingSavestates is { } tool)
			{
				tool.SaveStateAs();
				return;
			}

			var path = Config.PathEntries.SaveStateAbsolutePath(Game.System);
			new FileInfo(path).Directory?.Create();

			var result = this.ShowFileSaveDialog(
				fileExt: "State",
				filter: EmuHawkSaveStatesFSFilterSet,
				initDir: path,
				initFileName: $"{SaveStatePrefix()}.QuickSave0.State");
			if (result is not null) SaveState(path: result, userFriendlyStateName: result);

			if (Tools.IsLoaded<TAStudio>())
			{
				Tools.TAStudio.NamedStatePending = false;
			}
		}

		private bool LoadStateAs()
		{
			if (!Emulator.HasSavestates()) return false;
			if (ToolControllingSavestates is { } tool) return tool.LoadStateAs();

			var result = this.ShowFileOpenDialog(
				discardCWDChange: true,
				filter: EmuHawkSaveStatesFSFilterSet,
				initDir: Config.PathEntries.SaveStateAbsolutePath(Game.System));
			if (result is null || !File.Exists(result)) return false;
			return LoadState(path: result, userFriendlyStateName: Path.GetFileName(result));
		}

		private void SelectSlot(int slot)
		{
			if (!Emulator.HasSavestates()) return;
			if (ToolControllingSavestates is { } tool)
			{
				bool handled = tool.SelectSlot(slot);
				if (handled) return;
			}
			Config.SaveSlot = slot;
			SaveSlotSelectedMessage();
			UpdateStatusSlots();
		}

		private void PreviousSlot()
		{
			if (!Emulator.HasSavestates()) return;
			if (ToolControllingSavestates is { } tool)
			{
				bool handled = tool.PreviousSlot();
				if (handled) return;
			}
			Config.SaveSlot--;
			if (Config.SaveSlot < 1) Config.SaveSlot = 10;
			SaveSlotSelectedMessage();
			UpdateStatusSlots();
		}

		private void NextSlot()
		{
			if (!Emulator.HasSavestates()) return;
			if (ToolControllingSavestates is { } tool)
			{
				bool handled = tool.NextSlot();
				if (handled) return;
			}
			Config.SaveSlot++;
			if (Config.SaveSlot > 10) Config.SaveSlot = 1;
			SaveSlotSelectedMessage();
			UpdateStatusSlots();
		}

		private void CaptureRewind(bool suppressCaptureRewind)
		{
			if (ToolControllingRewind is { } tool)
			{
				tool.CaptureRewind();
			}
			else if (!suppressCaptureRewind && Rewinder?.Active == true)
			{
				Rewinder.Capture(Emulator.Frame);
			}
		}

		private bool Rewind(ref bool runFrame, long currentTimestamp, out bool returnToRecording)
		{
			var isRewinding = false;

			returnToRecording = false;

			if (ToolControllingRewind is { } rewindTool)
			{
				if (InputManager.ClientControls["Rewind"] || PressRewind)
				{
					if (_frameRewindTimestamp == 0)
					{
						isRewinding = true;
						_frameRewindTimestamp = currentTimestamp;
						_frameRewindWasPaused = EmulatorPaused;
					}
					else
					{
						double timestampDeltaMs = (double)(currentTimestamp - _frameRewindTimestamp) / Stopwatch.Frequency * 1000.0;
						isRewinding = timestampDeltaMs >= Config.FrameProgressDelayMs;

						// clear this flag once we get out of the progress stage
						if (isRewinding)
						{
							_frameRewindWasPaused = false;
						}

						// if we're freely running, there's no need for reverse frame progress semantics (that may be debatable though)
						if (!EmulatorPaused)
						{
							isRewinding = true;
						}

						if (_frameRewindWasPaused)
						{
							if (IsSeeking)
							{
								isRewinding = false;
							}
						}
					}

					if (isRewinding)
					{
						runFrame = rewindTool.Rewind();
					}
				}
				else
				{
					_frameRewindTimestamp = 0;
				}

				return isRewinding;
			}

			if (Rewinder?.Active == true && (InputManager.ClientControls["Rewind"] || PressRewind))
			{
				if (EmulatorPaused)
				{
					if (_frameRewindTimestamp == 0)
					{
						isRewinding = true;
						_frameRewindTimestamp = currentTimestamp;
					}
					else
					{
						double timestampDeltaMs = (double)(currentTimestamp - _frameRewindTimestamp) / Stopwatch.Frequency * 1000.0;
						isRewinding = timestampDeltaMs >= Config.FrameProgressDelayMs;
					}
				}
				else
				{
					isRewinding = true;
				}

				if (isRewinding)
				{
					// Try to avoid the previous frame:  We want to frame advance right after rewinding so we can give a useful
					// framebuffer.
					var frameToAvoid = Emulator.Frame - 1;
					runFrame = Rewinder.Rewind(frameToAvoid);
					if (Emulator.Frame == frameToAvoid)
					{
						// The rewinder was unable to satisfy our request.  Prefer showing a stale framebuffer to
						// advancing in a way that essentially no-ops the entire rewind.
						runFrame = false;
					}

					if (runFrame && MovieSession.Movie.IsRecording())
					{
						MovieSession.Movie.SwitchToPlay();
						returnToRecording = true;
					}
				}
			}
			else
			{
				_frameRewindTimestamp = 0;
			}

			return isRewinding;
		}

		public IDialogController DialogController => this;

		private static string SanitiseForFileDialog(string initDir)
		{
			if (initDir.Length is 0 || Directory.Exists(initDir)) return initDir;
#if DEBUG
			throw new ArgumentException(
				paramName: nameof(initDir),
				message: File.Exists(initDir)
					? $"file picker called with {nameof(initDir)} set to a non-dir"
					: $"file picker called with {nameof(initDir)} set to a nonexistent path");
#else
			return File.Exists(initDir) ? Path.GetDirectoryName(initDir) : string.Empty;
#endif
		}

		public IReadOnlyList<string>/*?*/ ShowFileMultiOpenDialog(
			IDialogParent dialogParent,
			string/*?*/ filterStr,
			ref int filterIndex,
			string initDir,
			bool discardCWDChange = false,
			string/*?*/ initFileName = null,
			bool maySelectMultiple = false,
			string/*?*/ windowTitle = null)
		{
			using OpenFileDialog ofd = new()
			{
				FileName = initFileName ?? string.Empty,
				Filter = filterStr ?? string.Empty,
				FilterIndex = filterIndex,
				InitialDirectory = SanitiseForFileDialog(initDir),
				Multiselect = maySelectMultiple,
				RestoreDirectory = discardCWDChange,
				Title = windowTitle ?? string.Empty,
				ValidateNames = false, // only raises confusing errors, doesn't affect result
			};
			var result = dialogParent.ShowDialogWithTempMute(ofd);
			filterIndex = ofd.FilterIndex;
			return result.IsOk() && ofd.FileNames.Length is not 0 ? ofd.FileNames : null;
		}

		public string/*?*/ ShowFileSaveDialog(
			IDialogParent dialogParent,
			bool discardCWDChange,
			string/*?*/ fileExt,
			string/*?*/ filterStr,
			string initDir,
			string/*?*/ initFileName,
			bool muteOverwriteWarning)
		{
			using SaveFileDialog sfd = new()
			{
				DefaultExt = fileExt ?? string.Empty,
				FileName = initFileName ?? string.Empty,
				Filter = filterStr ?? string.Empty,
				InitialDirectory = SanitiseForFileDialog(initDir),
				OverwritePrompt = !muteOverwriteWarning,
				RestoreDirectory = discardCWDChange,
				ValidateNames = false, // only raises confusing errors, doesn't affect result
			};
			var result = dialogParent.ShowDialogWithTempMute(sfd);
			return result.IsOk() ? sfd.FileName : null;
		}

		public void ShowMessageBox(
			IDialogParent/*?*/ owner,
			string text,
			string/*?*/ caption = null,
			EMsgBoxIcon? icon = null)
				=> this.ShowMessageBox(
					owner: owner,
					text: text,
					caption: caption,
					buttons: MessageBoxButtons.OK,
					icon: icon);

		public bool ShowMessageBox2(
			IDialogParent/*?*/ owner,
			string text,
			string/*?*/ caption = null,
			EMsgBoxIcon? icon = null,
			bool useOKCancel = false)
				=> this.ShowMessageBox(
					owner: owner,
					text: text,
					caption: caption,
					buttons: useOKCancel ? MessageBoxButtons.OKCancel : MessageBoxButtons.YesNo,
					icon: icon) switch
				{
					DialogResult.OK => true,
					DialogResult.Yes => true,
					_ => false,
				};

		public bool? ShowMessageBox3(
			IDialogParent/*?*/ owner,
			string text,
			string/*?*/ caption = null,
			EMsgBoxIcon? icon = null)
				=> this.ShowMessageBox(
					owner: owner,
					text: text,
					caption: caption,
					buttons: MessageBoxButtons.YesNoCancel,
					icon: icon) switch
				{
					DialogResult.Yes => true,
					DialogResult.No => false,
					_ => null,
				};

		public void StartSound() => Sound.StartSound();
		public void StopSound() => Sound.StopSound();

		private Mutex _singleInstanceMutex;
		private NamedPipeServerStream _singleInstanceServer;
		private readonly List<string[]> _singleInstanceForwardedArgs = new();

		private bool SingleInstanceInit(string[] args)
		{
			//note: this isn't 100% reliable, it's just a user convenience
			_singleInstanceMutex = new Mutex(true, "mutex-{84125ACB-F570-4458-9748-321F887FE795}", out bool createdNew);
			if (createdNew)
			{
				StartSingleInstanceServer();
				return false;
			}
			else
			{
				ForwardSingleInstanceStartup(args);
				return true;
			}
		}

		private void SingleInstanceDispose()
		{
			_singleInstanceServer?.Dispose();
		}

		private void ForwardSingleInstanceStartup(string[] args)
		{
			using var namedPipeClientStream = new NamedPipeClientStream(".", "pipe-{84125ACB-F570-4458-9748-321F887FE795}", PipeDirection.Out);
			try
			{
				namedPipeClientStream.Connect(0);
				//do this a bit cryptically to avoid loading up another big assembly (especially ones as frail as http and/or web ones)
				var payloadString = string.Join("|", args.Select(a => Encoding.UTF8.GetBytes(a).BytesToHexString()));
				var payloadBytes = Encoding.ASCII.GetBytes(payloadString);
				namedPipeClientStream.Write(payloadBytes, 0, payloadBytes.Length);
			}
			catch
			{
				Console.WriteLine("Failed forwarding args to already-running single instance");
			}
		}

		private void StartSingleInstanceServer()
		{
			//MIT LICENSE - https://www.autoitconsulting.com/site/development/single-instance-winform-app-csharp-mutex-named-pipes/

			// Create a new pipe accessible by local authenticated users, disallow network
			var sidNetworkService = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null);
			var sidWorld = new SecurityIdentifier(WellKnownSidType.WorldSid, null);

			var pipeSecurity = new PipeSecurity();

			// Deny network access to the pipe
			var accessRule = new PipeAccessRule(sidNetworkService, PipeAccessRights.ReadWrite, AccessControlType.Deny);
			pipeSecurity.AddAccessRule(accessRule);

			// Alow Everyone to read/write
			accessRule = new PipeAccessRule(sidWorld, PipeAccessRights.ReadWrite, AccessControlType.Allow);
			pipeSecurity.AddAccessRule(accessRule);

			// Current user is the owner
			SecurityIdentifier sidOwner = WindowsIdentity.GetCurrent().Owner;
			if (sidOwner != null)
			{
				accessRule = new PipeAccessRule(sidOwner, PipeAccessRights.FullControl, AccessControlType.Allow);
				pipeSecurity.AddAccessRule(accessRule);
			}

			// Create pipe and start the async connection wait
#if NET5_0_OR_GREATER
			_singleInstanceServer = NamedPipeServerStreamAcl.Create(
#else
			_singleInstanceServer = new NamedPipeServerStream(
#endif
					"pipe-{84125ACB-F570-4458-9748-321F887FE795}",
					PipeDirection.In,
					1,
					PipeTransmissionMode.Message,
					PipeOptions.Asynchronous,
					0,
					0,
					pipeSecurity);

			// Begin async wait for connections
			_singleInstanceServer.BeginWaitForConnection(SingleInstanceServerPipeCallback, null);
		}

		//Note: This method is called on a non-UI thread.
		//Note: this seems really frail. I don't think it's industrial strength. Pipes are weak compared to sockets.
		//It was probably frail in the first place with the old vbnet impl
		private void SingleInstanceServerPipeCallback(IAsyncResult iAsyncResult)
		{
			try
			{
				_singleInstanceServer.EndWaitForConnection(iAsyncResult);

				//a bit over-engineered in case someone wants to send a script or a rom or something
				//buffer size is set to something tiny so that we are continually testing it
				var payloadBytes = new MemoryStream();
				while (true)
				{
					var bytes = new byte[16];
					int did = _singleInstanceServer.Read(bytes, 0, bytes.Length);
					payloadBytes.Write(bytes, 0, did);
					if (_singleInstanceServer.IsMessageComplete) break;
				}

				var payloadString = Encoding.ASCII.GetString(payloadBytes.GetBuffer(), 0, (int)payloadBytes.Length);
				var args = payloadString.Split('|').Select(a => Encoding.UTF8.GetString(a.HexStringToBytes())).ToArray();

				Console.WriteLine("RECEIVED SINGLE INSTANCE FORWARDED ARGS:");
				lock (_singleInstanceForwardedArgs)
					_singleInstanceForwardedArgs.Add(args);
			}
			catch (ObjectDisposedException)
			{
				// EndWaitForConnection will exception when someone calls closes the pipe before connection made
				// In that case we dont create any more pipes and just return
				// This will happen when app is closing and our pipe is closed/disposed
				return;
			}
			catch (Exception)
			{
				// ignored
			}
			finally
			{
				// Close the original pipe (we will create a new one each time)
				_singleInstanceServer.Dispose();
			}

			// Create a new pipe for next connection
			StartSingleInstanceServer();
		}

		private void SingleInstanceProcessArgs(string[] args)
		{
			//ulp. it's not clear how to handle these.
			//we only have a legacy case where we can tell the form to load a rom, if it's in a sensible condition for that.
			//er.. let's assume it's always in a sensible condition
			//in case this all sounds insanely sketchy to you, remember, the main 99% use case is double clicking roms in explorer

			//BANZAIIIIIIIIIIIIIIIIIIIIIIIIIII
			_ = LoadRom(args[0]);
		}

		private IRetroAchievements RA { get; set; }

		private void OpenRetroAchievements()
		{
			RA = RetroAchievements.CreateImpl(
				this,
				InputManager,
				Tools,
				() => Config,
				wavFile =>
				{
					// TODO: Probably want to have wav volume a different setting?
					var atten = Config.SoundEnabledNormal ? Config.SoundVolume / 100.0f : 0.0f;
					Sound.PlayWavFile(wavFile, atten);
				},
				RetroAchievementsMenuItem.DropDownItems,
				() =>
				{
					RA.Dispose();
					RA = null;
					RetroAchievementsMenuItem.DropDownItems.Clear();
					RetroAchievementsMenuItem.DropDownItems.Add(StartRetroAchievementsMenuItem);
				});

			RA?.Restart();
		}

		private IntPtr _x11Display;
		private bool _hasXFixes;
		private readonly IntPtr[] _pointerBarriers = new IntPtr[4];

#if false
		private delegate void CaptureWithConfineDelegate(Control control, Control confineWindow);

		private static readonly Lazy<CaptureWithConfineDelegate> _captureWithConfine = new(() =>
		{
			var mi = typeof(Control).GetMethod("CaptureWithConfine", BindingFlags.Instance | BindingFlags.NonPublic);
			return (CaptureWithConfineDelegate)Delegate.CreateDelegate(typeof(CaptureWithConfineDelegate), mi!);
		});
#endif

		private void CaptureMouse(bool wantCapture)
		{
			if (wantCapture)
			{
				var fbLocation = Point.Subtract(Bounds.Location, new(PointToClient(Location)));
				fbLocation.Offset(_presentationPanel.Control.Location);
				Cursor.Clip = new(fbLocation, _presentationPanel.Control.Size);
				Cursor.Hide();
				_presentationPanel.Control.Cursor = Properties.Resources.BlankCursor.Value;
				_cursorHidden = true;
				BringToFront();

				if (Config.MainFormMouseCaptureForcesTopmost)
				{
					TopMost = true;
				}
			}
			else
			{
				Cursor.Clip = Rectangle.Empty;
				Cursor.Show();
				_presentationPanel.Control.Cursor = Cursors.Default;
				_cursorHidden = false;

				if (Config.MainFormMouseCaptureForcesTopmost)
				{
					TopMost = Config.MainFormStayOnTop;
				}
			}

			// Cursor.Clip is a no-op on Linux, so we need this too
			if (OSTailoredCode.IsUnixHost)
			{
#if true
				if (_x11Display == IntPtr.Zero)
				{
					_x11Display = XlibImports.XOpenDisplay(null);
					_hasXFixes = XlibImports.XQueryExtension(_x11Display, "XFIXES", out _, out _, out _);
					if (!_hasXFixes)
					{
						Console.Error.WriteLine("XFixes is unsupported, mouse capture will not lock the mouse cursor");
						return;
					}

					try
					{
						if (!XfixesImports.XFixesQueryVersion(_x11Display, out var major, out var minor))
						{
							Console.Error.WriteLine("Failed to query XFixes version, mouse capture will not lock the mouse cursor");
							_hasXFixes = false;
						}
						else if (major * 100 + minor < 500)
						{
							Console.Error.WriteLine($"XFixes version is not at least 5.0 (got {major}.{minor}), mouse capture will not lock the mouse cursor");
							_hasXFixes = false;
						}
					}
					catch
					{
						Console.Error.WriteLine("libXfixes.so.3 is not present, mouse capture will not lock the mouse cursor");
						_hasXFixes = false;
					}
				}

				if (_hasXFixes)
				{
					for (var i = 0; i < 4; i++)
					{
						if (_pointerBarriers[i] != IntPtr.Zero)
						{
							XfixesImports.XFixesDestroyPointerBarrier(_x11Display, _pointerBarriers[i]);
							_pointerBarriers[i] = IntPtr.Zero;
						}
					}

					if (wantCapture)
					{
						var fbLocation = Point.Subtract(Bounds.Location, new(PointToClient(Location)));
						fbLocation.Offset(_presentationPanel.Control.Location);
						var barrierRect = new Rectangle(fbLocation, _presentationPanel.Control.Size);

						// each line of the barrier rect must be a separate barrier object
						// also, the lines should span the entire screen, to avoid the cursor escaping at the corner

						var mfScreen = Screen.FromControl(this);
						var screenRect = mfScreen.Bounds;

						// left barrier
						_pointerBarriers[0] = XfixesImports.XFixesCreatePointerBarrier(
							_x11Display, Handle, barrierRect.X, screenRect.Y, barrierRect.X, screenRect.Bottom,
							XfixesImports.BarrierDirection.BarrierPositiveX, 0, IntPtr.Zero);
						// top barrier
						_pointerBarriers[1] = XfixesImports.XFixesCreatePointerBarrier(
							_x11Display, Handle, screenRect.X, barrierRect.Y, screenRect.Right, barrierRect.Y,
							XfixesImports.BarrierDirection.BarrierPositiveY, 0, IntPtr.Zero);
						// right barrier
						_pointerBarriers[2] = XfixesImports.XFixesCreatePointerBarrier(
							_x11Display, Handle, barrierRect.Right, screenRect.Y, barrierRect.Right, screenRect.Bottom,
							XfixesImports.BarrierDirection.BarrierNegativeX, 0, IntPtr.Zero);
						// bottom barrier
						_pointerBarriers[3] = XfixesImports.XFixesCreatePointerBarrier(
							_x11Display, Handle, screenRect.X, barrierRect.Bottom, screenRect.Right, barrierRect.Bottom,
							XfixesImports.BarrierDirection.BarrierNegativeY, 0, IntPtr.Zero);

						// after creating pointer barriers, warp our cursor over to the presentation panel
						_ = XlibImports.XUngrabPointer(_x11Display, XlibImports.CurrentTime); // just in case someone else has grabbed the pointer
						_ = XlibImports.XGrabPointer(_x11Display, Handle, false, 0,
							XlibImports.GrabMode.Async, XlibImports.GrabMode.Async, _presentationPanel.Control.Handle, IntPtr.Zero, XlibImports.CurrentTime);
						_ = XlibImports.XUngrabPointer(_x11Display, XlibImports.CurrentTime);
					}

					_ = XlibImports.XFlush(_x11Display);
				}
#elif false
				// approach just using XGrabPointer
				// (doesn't work, Mono won't respond to mouse buttons for whatever reason)
				if (_x11Display == IntPtr.Zero)
				{
					_x11Display = XlibImports.XOpenDisplay(null);
				}

				// always returns 1
				_ = XlibImports.XUngrabPointer(_x11Display, XlibImports.CurrentTime);

				if (wantCapture)
				{
					const XlibImports.EventMask eventMask = XlibImports.EventMask.ButtonPressMask | XlibImports.EventMask.ButtonMotionMask
						| XlibImports.EventMask.ButtonReleaseMask | XlibImports.EventMask.PointerMotionMask | XlibImports.EventMask.PointerMotionHintMask
						| XlibImports.EventMask.EnterWindowMask | XlibImports.EventMask.LeaveWindowMask | XlibImports.EventMask.FocusChangeMask;
					_ = XlibImports.XGrabPointer(_x11Display, Handle, false, eventMask, XlibImports.GrabMode.Async,
							XlibImports.GrabMode.Async, _presentationPanel.Control.Handle, IntPtr.Zero, XlibImports.CurrentTime);
				}

				_ = XlibImports.XFlush(_x11Display);
#else
				// approach using internal Mono function that ends up just using XGrabPointer
				// (doesn't work either, while Mono does respond to mouse buttons, it ends up being able to respond to the top menu bar somehow)
				// (also interacting with other windows (e.g. right click menu) cancels the capture)
				if (wantCapture)
				{
					_captureWithConfine.Value(this, _presentationPanel.Control);
				}
				else
				{
					Capture = false;
				}
#endif
			}
		}
	}
}
