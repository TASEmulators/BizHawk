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
			NESController1Panel.LoadSettings(Global.Config.NESController[0]);
			NESController2Panel.LoadSettings(Global.Config.NESController[1]);
			NESController3Panel.LoadSettings(Global.Config.NESController[2]);
			NESController4Panel.LoadSettings(Global.Config.NESController[3]);

			//NESController1Panel.ControllerNumber = 1;
			//NESController1Panel.Autofire = false;
			//NESController1Panel.Load();
			//NESController2Panel.ControllerNumber = 2;
			//NESController2Panel.Autofire = false;
			//NESController2Panel.Load();
			
			//NESController3Panel.ControllerNumber = 3;
			//NESController3Panel.Autofire = false;
			//NESController4Panel.ControllerNumber = 4;
			//NESController4Panel.Autofire = false;
			//NESAutofire1Panel.ControllerNumber = 1;
			//NESAutofire1Panel.Autofire = false;
			//NESAutofire2Panel.ControllerNumber = 2;
			//NESAutofire2Panel.Autofire = false;
			//NESAutofire3Panel.ControllerNumber = 3;
			//NESAutofire3Panel.Autofire = false;
			//NESAutofire4Panel.ControllerNumber = 4;
			//NESAutofire4Panel.Autofire = false;


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
			NESController1Panel.Save();
			NESController2Panel.Save();
			NESController3Panel.Save();
			NESController4Panel.Save();

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
