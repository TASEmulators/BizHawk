using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class HotkeyConfig : Form
	{
		public HotkeyConfig()
		{
			InitializeComponent();
		}

		private void NewHotkeyWindow_Load(object sender, EventArgs e)
		{
			var source = new AutoCompleteStringCollection();
			source.AddRange(Global.Config.HotkeyBindings.Select(x => x.DisplayName).ToArray());

			SearchBox.AutoCompleteCustomSource = source;
			SearchBox.AutoCompleteSource = AutoCompleteSource.CustomSource;

			AutoTabCheckBox.Checked = Global.Config.HotkeyConfigAutoTab;
			DoTabs();
			DoFocus();
		}

		private void IDB_CANCEL_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("Hotkey config aborted");
			Close();
		}

		private void IDB_SAVE_Click(object sender, EventArgs e)
		{
			Save();
			GlobalWin.OSD.AddMessage("Hotkey settings saved");
			DialogResult = DialogResult.OK;
			Close();
		}

		private void RestoreDefaults_Click(object sender, EventArgs e)
		{
			Defaults();
		}

		private void AutoTabCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SetAutoTab();
		}

		private void Save()
		{
			Global.Config.HotkeyConfigAutoTab = AutoTabCheckBox.Checked;

			foreach (var w in InputWidgets)
			{
				var b = Global.Config.HotkeyBindings.FirstOrDefault(x => x.DisplayName == w.WidgetName);
				b.Bindings = w.Text;
			}
		}

		private IEnumerable<InputWidget> InputWidgets
		{
			get
			{
				var widgets = new List<InputWidget>();
				for (var x = 0; x < HotkeyTabControl.TabPages.Count; x++)
				{
					for (var y = 0; y < HotkeyTabControl.TabPages[x].Controls.Count; y++)
					{
						if (HotkeyTabControl.TabPages[x].Controls[y] is InputWidget)
						{
							widgets.Add(HotkeyTabControl.TabPages[x].Controls[y] as InputWidget);
						}
					}
				}
				return widgets;
			}
		}

		private void DoTabs()
		{
			HotkeyTabControl.TabPages.Clear();

			//Buckets
			var Tabs = Global.Config.HotkeyBindings.Select(x => x.TabGroup).Distinct().ToList();

			foreach (var tab in Tabs)
			{
				var _y = 14;
				var _x = 6;

				var tb = new TabPage {Name = tab, Text = tab};

				var bindings = Global.Config.HotkeyBindings.Where(x => x.TabGroup == tab).OrderBy(x => x.Ordinal).ThenBy(x => x.DisplayName).ToList();

				const int iwOffsetX = 110;
				const int iwOffsetY = -4;
				const int iwWidth = 120;
				foreach (var b in bindings)
				{
					var l = new Label
						{
						Text = b.DisplayName,
						Location = new Point(_x, _y),
						Width = iwOffsetX - 2,
					};

					var w = new InputWidget
						{
						Bindings = b.Bindings,
						Location = new Point(_x + iwOffsetX, _y + iwOffsetY),
						AutoTab = AutoTabCheckBox.Checked,
						Width = iwWidth,
						WidgetName = b.DisplayName,
					};

					tb.Controls.Add(l);
					tb.Controls.Add(w);

					_y += 24;
					if (_y > HotkeyTabControl.Height - 35)
					{
						_x += iwOffsetX + iwWidth + 10;
						_y = 14;
					}
				}

				HotkeyTabControl.TabPages.Add(tb);
			}
		}

		private void Defaults()
		{
			foreach (var w in InputWidgets)
			{
				var b = Global.Config.HotkeyBindings.FirstOrDefault(x => x.DisplayName == w.WidgetName);
				if (b != null) w.Text = b.DefaultBinding;
			}
		}

		private void SetAutoTab()
		{
			foreach (var w in InputWidgets)
			{
				w.AutoTab = AutoTabCheckBox.Checked;
			}
		}

		private void HotkeyTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			DoFocus();
		}

		private void DoFocus()
		{
			if (HotkeyTabControl.SelectedTab != null)
			{
				foreach (var c in HotkeyTabControl.SelectedTab.Controls.OfType<InputWidget>())
				{
					c.Focus();
					return;
				}
			}
		}

		private void SearchBox_KeyDown(object sender, KeyEventArgs e)
		{
			//Tab or Enter
			if (!e.Control && !e.Alt && !e.Shift &&
				(e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab))
			{
				var b = Global.Config.HotkeyBindings.FirstOrDefault(x => x.DisplayName == SearchBox.Text);

				//Found
				if (b != null)
				{
					var w = InputWidgets.FirstOrDefault(x => x.WidgetName == b.DisplayName);
					if (w != null)
					{
						HotkeyTabControl.SelectTab((w.Parent as TabPage));
						w.Focus();
					}
				}

				e.Handled = true;
			}
		}
	}
}
