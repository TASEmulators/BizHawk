using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class NewHotkeyWindow : Form
	{
		public NewHotkeyWindow()
		{
			InitializeComponent();
		}

		private void NewHotkeyWindow_Load(object sender, EventArgs e)
		{
			AutoTabCheckBox.Checked = Global.Config.HotkeyConfigAutoTab;
			DoTabs();
			DoFocus();
		}

		private void IDB_CANCEL_Click(object sender, EventArgs e)
		{
			Global.OSD.AddMessage("Hotkey config aborted");
			Close();
		}

		private void IDB_SAVE_Click(object sender, EventArgs e)
		{
			Save();
			Global.OSD.AddMessage("Hotkey settings saved");
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

			foreach(InputWidget w in _inputWidgets)
			{
				Binding b = Global.Config.HotkeyBindings.FirstOrDefault(x => x.DisplayName == w.WidgetName);
				b.Bindings = w.Text;
			}
		}

		private List<InputWidget> _inputWidgets
		{
			get
			{
				List<InputWidget> widgets = new List<InputWidget>();
				for (int x = 0; x < HotkeyTabControl.TabPages.Count; x++)
				{
					for (int y = 0; y < HotkeyTabControl.TabPages[x].Controls.Count; y++)
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
			List<string> Tabs = Global.Config.HotkeyBindings.Select(x => x.TabGroup).Distinct().ToList();
			foreach (string tab in Tabs)
			{
				TabPage tb = new TabPage();
				tb.Name = tab;
				tb.Text = tab;

				List<Binding> bindings = Global.Config.HotkeyBindings.Where(x => x.TabGroup == tab).OrderBy(x => x.Ordinal).ThenBy(x => x.DisplayName).ToList();

				int _x = 6;
				int _y = 14;
				int iw_offset_x = 110;
				int iw_offset_y = -4;
				int iw_width = 120;
				foreach (Binding b in bindings)
				{
					Label l = new Label()
					{
						Text = b.DisplayName,
						Location = new Point(_x, _y),
						Width = iw_offset_x - 2,
					};

					InputWidget w = new InputWidget()
					{
						Bindings = b.Bindings,
						Location = new Point(_x + iw_offset_x , _y + iw_offset_y),
						AutoTab = AutoTabCheckBox.Checked,
						Width = iw_width,
						WidgetName = b.DisplayName,
					};

					tb.Controls.Add(l);
					tb.Controls.Add(w);

					_y += 24;
					if (_y > HotkeyTabControl.Height - 35)
					{
						_x += iw_offset_x + iw_width + 10;
						_y = 14;
					}
				}

				HotkeyTabControl.TabPages.Add(tb);
			}
		}

		private void Defaults()
		{
			foreach (InputWidget w in _inputWidgets)
			{
				Binding b = Global.Config.HotkeyBindings.FirstOrDefault(x => x.DisplayName == w.WidgetName);
				w.Text = b.DefaultBinding;
			}
		}

		private void SetAutoTab()
		{
			foreach (InputWidget w in _inputWidgets)
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
				foreach (Control c in HotkeyTabControl.SelectedTab.Controls)
				{
					if (c is InputWidget)
					{
						(c as InputWidget).Focus();
						return;
					}
				}
			}
		}

		private void HotkeyTabControl_Enter(object sender, EventArgs e)
		{
			DoFocus();
		}
	}
}
