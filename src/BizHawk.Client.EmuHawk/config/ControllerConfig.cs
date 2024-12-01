using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class ControllerConfig : Form, IDialogParent
	{
		private static readonly Dictionary<string, Lazy<Bitmap>> ControllerImages = new Dictionary<string, Lazy<Bitmap>>();
		private readonly IEmulator _emulator;
		private readonly Config _config;

		public IDialogController DialogController { get; }

		static ControllerConfig()
		{
			ControllerImages.Add("NES Controller", Properties.Resources.NesController);
			ControllerImages.Add("SNES Controller", Properties.Resources.SnesController);
			ControllerImages.Add("Nintendo 64 Controller", Properties.Resources.N64);
			ControllerImages.Add("Gameboy Controller", Properties.Resources.GbController);
			ControllerImages.Add("Gameboy Controller H", Properties.Resources.GbController);
			ControllerImages.Add("Gameboy Controller + Tilt", Properties.Resources.GbController);
			ControllerImages.Add("GBA Controller", Properties.Resources.GbaController);
			ControllerImages.Add("Dual Gameboy Controller", Properties.Resources.GbController);

			ControllerImages.Add("SMS Controller", Properties.Resources.SmsController);
			ControllerImages.Add("GPGX Genesis Controller", Properties.Resources.GenesisController);
			ControllerImages.Add("Saturn Controller", Properties.Resources.SaturnController);

			ControllerImages.Add("Intellivision Controller", Properties.Resources.IntVController);
			ControllerImages.Add("ColecoVision Basic Controller", Properties.Resources.ColecoVisionController);
			ControllerImages.Add("Atari 2600 Basic Controller", Properties.Resources.AtariController);
			ControllerImages.Add("Atari 7800 ProLine Joystick Controller", Properties.Resources.A78Joystick);

			ControllerImages.Add("PC Engine Controller", Properties.Resources.PceController);
			ControllerImages.Add("Commodore 64 Controller", Properties.Resources.C64Joystick);
			ControllerImages.Add("TI83 Controller", Properties.Resources.TI83Controller);

			ControllerImages.Add("WonderSwan Controller", Properties.Resources.WonderSwanColor);
			ControllerImages.Add("Lynx Controller", Properties.Resources.Lynx);
			ControllerImages.Add("PSX Front Panel", Properties.Resources.PsxDualShockController);
			ControllerImages.Add("Apple IIe Keyboard", Properties.Resources.AppleIIKeyboard);
			ControllerImages.Add("VirtualBoy Controller", Properties.Resources.VBoyController);
			ControllerImages.Add("NeoGeo Portable Controller", Properties.Resources.NgpController);
			ControllerImages.Add("MAME Controller", Properties.Resources.ArcadeController);
			ControllerImages.Add("NDS Controller", Properties.Resources.DSController);
			ControllerImages.Add("Amiga Controller", Properties.Resources.AmigaKeyboard);
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			Input.Instance.ControlInputFocus(this, ClientInputFocus.Mouse, true);
		}

		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);
			Input.Instance.ControlInputFocus(this, ClientInputFocus.Mouse, false);
		}

		private void ControllerConfig_Load(object sender, EventArgs e)
		{
			Icon = Properties.Resources.GameControllerIcon;
			Text = $"{_emulator.ControllerDefinition.Name} Configuration";
		}

		private void ControllerConfig_FormClosed(object sender, FormClosedEventArgs e)
		{
			Input.Instance.ClearEvents();
		}

		private delegate Control PanelCreator<TBindValue>(Dictionary<string, TBindValue> settings, List<string> buttons, Size size);

		private Control CreateNormalPanel(Dictionary<string, string> settings, List<string> buttons, Size size)
		{
			ControllerConfigPanel cp = new(_config.ModifierKeysEffective) { Dock = DockStyle.Fill, AutoScroll = true, Tooltip = toolTip1 };
			cp.LoadSettings(settings, checkBoxAutoTab.Checked, buttons, size.Width, size.Height);
			return cp;
		}

		private static Control CreateAnalogPanel(Dictionary<string, AnalogBind> settings, List<string> buttons, Size size)
		{
			return new AnalogBindPanel(settings, buttons) { Dock = DockStyle.Fill, AutoScroll = true };
		}

		private static Control CreateFeedbacksPanel(Dictionary<string, FeedbackBind> settings, List<string> buttons, Size size)
		{
			return new FeedbacksBindPanel(settings, buttons) { Dock = DockStyle.Fill, AutoScroll = true };
		}

		private static readonly Regex ButtonMatchesPlayer = new Regex("^P(\\d+)\\s");

		private void LoadToPanel<TBindValue>(
			Control dest,
			string controllerName,
			IList<string> controllerButtons,
			IDictionary<string, string> categoryLabels,
			IDictionary<string, Dictionary<string, TBindValue>> settingsBlock,
			TBindValue defaultValue,
			PanelCreator<TBindValue> createPanel
		)
		{
			var settings = settingsBlock.GetValueOrPutNew(controllerName);

			// check to make sure that the settings object has all of the appropriate bool buttons
			foreach (var button in controllerButtons)
			{
				if (!settings.ContainsKey(button))
				{
					settings[button] = defaultValue;
				}
			}

			if (controllerButtons.Count == 0)
			{
				return;
			}

			// split the list of all settings into buckets by player number, or supplied category
			// the order that buttons appeared in determines the order of the tabs
			var orderedBuckets = new List<KeyValuePair<string, List<string>>>();
			var buckets = new Dictionary<string, List<string>>();

			// by iterating through only the controller's active buttons, we're silently
			// discarding anything that's not on the controller right now.  due to the way
			// saving works, those entries will still be preserved in the config file, tho
			foreach (var button in controllerButtons)
			{
				if (!categoryLabels.TryGetValue(button, out var categoryLabel))
				{
					var m = ButtonMatchesPlayer.Match(button);
					categoryLabel = m.Success
						? $"Player {m.Groups[1].Value}"
						: "Console"; // anything that wants not console can set it in the categorylabels
				}

				if (!buckets.ContainsKey(categoryLabel))
				{
					var l = new List<string>();
					buckets.Add(categoryLabel, l);
					orderedBuckets.Add(new KeyValuePair<string, List<string>>(categoryLabel, l));
				}

				buckets[categoryLabel].Add(button);
			}

			if (orderedBuckets.Count == 1)
			{
				// everything went into bucket 0, so make no tabs at all
				dest.Controls.Add(createPanel(settings, controllerButtons.ToList(), dest.Size));
			}
			else
			{
				// create multiple tabs
				var tt = new TabControl { Dock = DockStyle.Fill };
				dest.Controls.Add(tt);
				int pageIdx = 0;
				foreach (var (tabName, buttons) in orderedBuckets)
				{
					tt.TabPages.Add(tabName);
					tt.TabPages[pageIdx++].Controls.Add(createPanel(settings, buttons, tt.Size));
				}
			}
		}

		public ControllerConfig(
			IDialogController dialogController,
			IEmulator emulator,
			Config config)
		{
			_emulator = emulator;
			_config = config;
			DialogController = dialogController;
			
			InitializeComponent();

			SuspendLayout();
			LoadPanels(_config);

			switch (_config.OpposingDirPolicy)
			{
				case OpposingDirPolicy.Priority:
					rbUDLRPriority.Checked = true;
					break;
				case OpposingDirPolicy.Forbid:
					rbUDLRForbid.Checked = true;
					break;
				case OpposingDirPolicy.Allow:
					rbUDLRAllow.Checked = true;
					break;
				default:
					throw new Exception();
			}
			checkBoxAutoTab.Checked = _config.InputConfigAutoTab;

			SetControllerPicture(_emulator.ControllerDefinition.Name);
			ResumeLayout();
		}

		private void LoadPanels(
			IDictionary<string, Dictionary<string, string>> normal,
			IDictionary<string, Dictionary<string, string>> autofire,
			IDictionary<string, Dictionary<string, AnalogBind>> analog,
			IDictionary<string, Dictionary<string, FeedbackBind>> haptics)
		{
			LoadToPanel(
				NormalControlsTab,
				_emulator.ControllerDefinition.Name,
				_emulator.ControllerDefinition.BoolButtons,
				_emulator.ControllerDefinition.CategoryLabels,
				normal,
				"",
				CreateNormalPanel
			);
			LoadToPanel(
				AutofireControlsTab,
				_emulator.ControllerDefinition.Name,
				_emulator.ControllerDefinition.BoolButtons,
				_emulator.ControllerDefinition.CategoryLabels,
				autofire,
				"",
				CreateNormalPanel
			);
			LoadToPanel(
				AnalogControlsTab,
				_emulator.ControllerDefinition.Name,
				_emulator.ControllerDefinition.Axes.Keys.ToList(),
				_emulator.ControllerDefinition.CategoryLabels,
				analog,
				new AnalogBind("", 1.0f, 0.1f),
				CreateAnalogPanel
			);
			LoadToPanel(
				FeedbacksTab,
				_emulator.ControllerDefinition.Name,
				_emulator.ControllerDefinition.HapticsChannels,
				_emulator.ControllerDefinition.CategoryLabels,
				haptics,
				new(string.Empty, string.Empty, 1.0f),
				CreateFeedbacksPanel);

			if (AnalogControlsTab.Controls.Count == 0)
			{
				tabControl1.TabPages.Remove(AnalogControlsTab);
			}
			if (FeedbacksTab.Controls.Count == 0) tabControl1.TabPages.Remove(FeedbacksTab);
		}

		private void LoadPanels(DefaultControls cd)
		{
			LoadPanels(cd.AllTrollers, cd.AllTrollersAutoFire, cd.AllTrollersAnalog, cd.AllTrollersFeedbacks);
		}

		private void LoadPanels(Config c)
		{
			LoadPanels(c.AllTrollers, c.AllTrollersAutoFire, c.AllTrollersAnalog, c.AllTrollersFeedbacks);
		}

		private void SetControllerPicture(string controlName)
		{
			_ = ControllerImages.TryGetValue(controlName, out var lazyBmp);
			if (lazyBmp != null)
			{
				var bmp = lazyBmp.Value;
				pictureBox1.Image = bmp;
				pictureBox1.Size = bmp.Size;
				tableLayoutPanel1.ColumnStyles[1].Width = bmp.Width;
			}
			else
			{
				tableLayoutPanel1.ColumnStyles[1].Width = 0;
			}

			// Uberhack
			if (controlName == "Commodore 64 Controller")
			{
				var pictureBox2 = new PictureBox
					{
						Image = Properties.Resources.C64Keyboard.Value,
						Size = Properties.Resources.C64Keyboard.Value.Size
					};
				tableLayoutPanel1.ColumnStyles[1].Width = Properties.Resources.C64Keyboard.Value.Width;
				pictureBox1.Height /= 2;
				pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
				pictureBox1.Dock = DockStyle.Top;
				pictureBox2.Location = new Point(pictureBox1.Location.X, pictureBox1.Location.Y + pictureBox1.Size.Height + 10);
				tableLayoutPanel1.Controls.Add(pictureBox2, 1, 0);

				pictureBox2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
			}

			if (controlName == "ZXSpectrum Controller")
			{
				pictureBox1.Image = Properties.Resources.ZXSpectrumKeyboards.Value;
				pictureBox1.Size = Properties.Resources.ZXSpectrumKeyboards.Value.Size;
				tableLayoutPanel1.ColumnStyles[1].Width = Properties.Resources.ZXSpectrumKeyboards.Value.Width;
			}

			if (controlName == "ChannelF Controller")
			{

			}

			if (controlName == "AmstradCPC Controller")
			{
#if false
				pictureBox1.Image = Properties.Resources.ZXSpectrumKeyboards.Value;
				pictureBox1.Size = Properties.Resources.ZXSpectrumKeyboards.Value.Size;
				tableLayoutPanel1.ColumnStyles[1].Width = Properties.Resources.ZXSpectrumKeyboards.Value.Width;
#endif
			}
		}

		// lazy methods, but they're not called often and actually
		// tracking all of the ControllerConfigPanels wouldn't be simpler
		private static void SetAutoTab(Control c, bool value)
		{
			if (c is ControllerConfigPanel panel)
			{
				panel.SetAutoTab(value);
			}
			else if (c is AnalogBindPanel)
			{
				// TODO
			}
			else if (c.HasChildren)
			{
				foreach (Control cc in c.Controls)
				{
					SetAutoTab(cc, value);
				}
			}
		}

		private void Save()
		{
			ActOnControlCollection<ControllerConfigPanel>(NormalControlsTab, c => c.Save(_config.AllTrollers[_emulator.ControllerDefinition.Name]));
			ActOnControlCollection<ControllerConfigPanel>(AutofireControlsTab, c => c.Save(_config.AllTrollersAutoFire[_emulator.ControllerDefinition.Name]));
			ActOnControlCollection<AnalogBindPanel>(AnalogControlsTab, c => c.Save(_config.AllTrollersAnalog[_emulator.ControllerDefinition.Name]));
			ActOnControlCollection<FeedbacksBindPanel>(FeedbacksTab, c => c.Save(_config.AllTrollersFeedbacks[_emulator.ControllerDefinition.Name]));
		}

		private void SaveToDefaults(DefaultControls cd)
		{
			ActOnControlCollection<ControllerConfigPanel>(NormalControlsTab, c => c.Save(cd.AllTrollers[_emulator.ControllerDefinition.Name]));
			ActOnControlCollection<ControllerConfigPanel>(AutofireControlsTab, c => c.Save(cd.AllTrollersAutoFire[_emulator.ControllerDefinition.Name]));
			ActOnControlCollection<AnalogBindPanel>(AnalogControlsTab, c => c.Save(cd.AllTrollersAnalog[_emulator.ControllerDefinition.Name]));
			ActOnControlCollection<FeedbacksBindPanel>(FeedbacksTab, c => c.Save(cd.AllTrollersFeedbacks[_emulator.ControllerDefinition.Name]));
		}

		private static void ActOnControlCollection<T>(Control c, Action<T> proc)
			where T : Control
		{
			if (c is T control)
			{
				proc(control);
			}
			else if (c.HasChildren)
			{
				foreach (Control cc in c.Controls)
				{
					ActOnControlCollection(cc, proc);
				}
			}
		}

		private void CheckBoxAutoTab_CheckedChanged(object sender, EventArgs e)
		{
			SetAutoTab(this, checkBoxAutoTab.Checked);
		}

		private void ButtonOk_Click(object sender, EventArgs e)
		{
			if (rbUDLRPriority.Checked) _config.OpposingDirPolicy = OpposingDirPolicy.Priority;
			else if (rbUDLRForbid.Checked) _config.OpposingDirPolicy = OpposingDirPolicy.Forbid;
			else if (rbUDLRAllow.Checked) _config.OpposingDirPolicy = OpposingDirPolicy.Allow;
			_config.InputConfigAutoTab = checkBoxAutoTab.Checked;

			Save();

			DialogResult = DialogResult.OK;
			Close();
		}

		private void ButtonCancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private static TabControl GetTabControl(IEnumerable controls)
		{
			return controls?.OfType<TabControl>()
				.Select(c => c)
				.FirstOrDefault();
		}

		private void ButtonLoadDefaults_Click(object sender, EventArgs e)
		{
			tabControl1.SuspendLayout();

			var wasTabbedMain = tabControl1.SelectedTab.Name;
			var tb1 = GetTabControl(NormalControlsTab.Controls);
			var tb2 = GetTabControl(AutofireControlsTab.Controls);
			var tb3 = GetTabControl(AnalogControlsTab.Controls);
			var tb4 = GetTabControl(FeedbacksTab.Controls);
			int? wasTabbedPage1 = null;
			int? wasTabbedPage2 = null;
			int? wasTabbedPage3 = null;
			int? wasTabbedPage4 = null;

			if (tb1?.SelectedTab != null) { wasTabbedPage1 = tb1.SelectedIndex; }
			if (tb2?.SelectedTab != null) { wasTabbedPage2 = tb2.SelectedIndex; }
			if (tb3?.SelectedTab != null) { wasTabbedPage3 = tb3.SelectedIndex; }
			if (tb4?.SelectedTab != null) { wasTabbedPage4 = tb4.SelectedIndex; }

			NormalControlsTab.Controls.Clear();
			AutofireControlsTab.Controls.Clear();
			AnalogControlsTab.Controls.Clear();
			FeedbacksTab.Controls.Clear();

			// load panels directly from the default config.
			// this means that the changes are NOT committed.  so "Cancel" works right and you
			// still have to hit OK at the end.
			var cd = ConfigService.Load<DefaultControls>(Config.ControlDefaultPath);
			LoadPanels(cd);

			tabControl1.SelectTab(wasTabbedMain);

			if (wasTabbedPage1.HasValue)
			{
				var newTb1 = GetTabControl(NormalControlsTab.Controls);
				newTb1?.SelectTab(wasTabbedPage1.Value);
			}

			if (wasTabbedPage2.HasValue)
			{
				var newTb2 = GetTabControl(AutofireControlsTab.Controls);
				newTb2?.SelectTab(wasTabbedPage2.Value);
			}

			if (wasTabbedPage3.HasValue)
			{
				var newTb3 = GetTabControl(AnalogControlsTab.Controls);
				newTb3?.SelectTab(wasTabbedPage3.Value);
			}

			if (wasTabbedPage4.HasValue)
			{
				var newTb4 = GetTabControl(FeedbacksTab.Controls);
				newTb4?.SelectTab(wasTabbedPage4.Value);
			}

			tabControl1.ResumeLayout();
		}

		private void ButtonSaveDefaults_Click(object sender, EventArgs e)
		{
			// this doesn't work anymore, as it stomps out any defaults for buttons that aren't currently active on the console
			// there are various ways to fix it, each with its own semantic problems
			var result = this.ModalMessageBox2("OK to overwrite defaults for current control scheme?", "Save Defaults");
			if (result)
			{
				var cd = ConfigService.Load<DefaultControls>(Config.ControlDefaultPath);
				cd.AllTrollers[_emulator.ControllerDefinition.Name] = new Dictionary<string, string>();
				cd.AllTrollersAutoFire[_emulator.ControllerDefinition.Name] = new Dictionary<string, string>();
				cd.AllTrollersAnalog[_emulator.ControllerDefinition.Name] = new Dictionary<string, AnalogBind>();

				SaveToDefaults(cd);

				ConfigService.Save(Config.ControlDefaultPath, cd);
			}
		}

		private void ClearWidgetAndChildren(Control c)
		{
			switch (c)
			{
				case InputCompositeWidget widget:
					widget.Clear();
					break;
				case InputWidget inputWidget:
					inputWidget.ClearAll();
					break;
				case AnalogBindControl control:
					control.Unbind_Click(null, null);
					break;
			}

			var children = c.Controls().ToList();
			if (children.Count != 0)
			{
				foreach (var child in children) ClearWidgetAndChildren(child);
			}
		}

		private void ClearBtn_Click(object sender, EventArgs e)
		{
			foreach (var c in this.Controls())
			{
				ClearWidgetAndChildren(c);
			}
		}
	}
}
