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
		//TODO: autoab
		//enable L+R
		
		public ControllerConfig()
		{
			InitializeComponent();
		}

		private void ControllerConfig_Load(object sender, EventArgs e)
		{
			NESController1Panel.LoadSettings(Global.Config.NESController[0]);
			NESController2Panel.LoadSettings(Global.Config.NESController[1]);
			NESController3Panel.LoadSettings(Global.Config.NESController[2]);
			NESController4Panel.LoadSettings(Global.Config.NESController[3]);

			SetAutoTab(true);
		}

		protected override void OnShown(EventArgs e)
		{
			Input.Instance.EnableIgnoreModifiers = true;
			base.OnShown(e);
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Input.Instance.EnableIgnoreModifiers = false;
		}

		private void SetAutoTab(bool setting)
		{
			foreach (Control control1 in tabControl1.TabPages)
			{
				if (control1 is TabControl)
				{
					foreach (Control control2 in (control1 as TabControl).TabPages)
					{
						if (control2 is InputWidget)
						{
							(control2 as InputWidget).AutoTab = setting;
						}
					}
				}
				else
				{
					if (control1 is InputWidget)
					{
						(control1 as InputWidget).AutoTab = setting;
					}
				}
			}
		}

		private void OK_Click(object sender, EventArgs e)
		{
			foreach (Control control1 in tabControl1.TabPages)
			{
				if (control1 is TabControl)
				{
					foreach (Control control2 in (control1 as TabControl).TabPages)
					{
						if (control2 is ControllerConfigPanel)
						{
							(control2 as ControllerConfigPanel).Save();
						}
					}
				}
				else
				{
					if (control1 is ControllerConfigPanel)
					{
						(control1 as ControllerConfigPanel).Save();
					}
				}
			}

			Global.OSD.AddMessage("Controller settings saved");
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Global.OSD.AddMessage("Controller config aborted");
			Close();
		}
	}
}
