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
	public partial class ControllerConfig : Form
	{
		public ControllerConfig()
		{
			InitializeComponent();
		}

		private void ControllerConfig_Load(object sender, EventArgs e)
		{
			LoadInputSettings();
		}

		private void AutotabCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			
		}

		private void OK_Click(object sender, EventArgs e)
		{
			SaveInputSettings();
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void SaveDialogSettings()
		{
			Global.Config.HotkeyConfigAutoTab = AutotabCheckbox.Checked; //TODO: use its own variable not hotkey dialog!
		}

		private void LoadDialogSettings()
		{
			AutotabCheckbox.Checked = Global.Config.HotkeyConfigAutoTab;
			SetAutoTab();
		}

		private void SetAutoTab()
		{
			for (int i = 0; i < ControllerTabs.Controls.Count; i++)
			{
				if (ControllerTabs.Controls[i] is TabControl)
				{
					TabControl tc = ControllerTabs.Controls[i] as TabControl;
					for (int j = 0; j < tc.TabPages[i].Controls.Count; j++)
					{
						if (tc.Controls[i].Controls[j] is InputWidget)
						{
							InputWidget w = tc.Controls[i].Controls[j] as InputWidget;
							w.AutoTab = AutotabCheckbox.Checked;
						}
					}
				}
			}
		}

		private void LoadInputSettings()
		{
			NESC1UpBox.SetBindings(Global.Config.NESController[0].Up);
			NESC1DownBox.SetBindings(Global.Config.NESController[0].Down);
			NESC1LeftBox.SetBindings(Global.Config.NESController[0].Left);
			NESC1RightBox.SetBindings(Global.Config.NESController[0].Right);
			NESC1ABox.SetBindings(Global.Config.NESController[0].A);
			NESC1BBox.SetBindings(Global.Config.NESController[0].B);
			NESC1SelectBox.SetBindings(Global.Config.NESController[0].Select);
			NESC1StartBox.SetBindings(Global.Config.NESController[0].Start);
		}

		private void SaveInputSettings()
		{
			Global.Config.NESController[0].Up = NESC1UpBox.Text;
			Global.Config.NESController[0].Down = NESC1DownBox.Text;
			Global.Config.NESController[0].Left = NESC1LeftBox.Text;
			Global.Config.NESController[0].Right = NESC1RightBox.Text;
			Global.Config.NESController[0].A = NESC1ABox.Text;
			Global.Config.NESController[0].B = NESC1BBox.Text;
			Global.Config.NESController[0].Select = NESC1SelectBox.Text;
			Global.Config.NESController[0].Start = NESC1StartBox.Text;
		}
	}
}
