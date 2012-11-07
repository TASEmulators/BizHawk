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
			NESAutofire1Panel.LoadSettings(Global.Config.NESAutoController[0]);
			NESAutofire2Panel.LoadSettings(Global.Config.NESAutoController[1]);
			NESAutofire3Panel.LoadSettings(Global.Config.NESAutoController[2]);
			NESAutofire4Panel.LoadSettings(Global.Config.NESAutoController[3]);

			SNESController1Panel.LoadSettings(Global.Config.SNESController[0]);
			SNESController2Panel.LoadSettings(Global.Config.SNESController[1]);
			SNESController3Panel.LoadSettings(Global.Config.SNESController[2]);
			SNESController4Panel.LoadSettings(Global.Config.SNESController[3]);
			SNESAutofire1Panel.LoadSettings(Global.Config.SNESAutoController[0]);
			SNESAutofire2Panel.LoadSettings(Global.Config.SNESAutoController[1]);
			SNESAutofire3Panel.LoadSettings(Global.Config.SNESAutoController[2]);
			SNESAutofire4Panel.LoadSettings(Global.Config.SNESAutoController[3]);

			GBController1Panel.LoadSettings(Global.Config.GBController[0]);
			GBAutofire1Panel.LoadSettings(Global.Config.GBAutoController[0]);

			GenesisController1Panel.LoadSettings(Global.Config.GenesisController[0]);
			GenesisAutofire1Panel.LoadSettings(Global.Config.GenesisAutoController[0]);

			SMSController1Panel.LoadSettings(Global.Config.SMSController[0]);
			SMSController2Panel.LoadSettings(Global.Config.SMSController[1]);
			SMSAutofire1Panel.LoadSettings(Global.Config.SMSAutoController[0]);
			SMSAutofire2Panel.LoadSettings(Global.Config.SMSAutoController[1]);

			PCEController1Panel.LoadSettings(Global.Config.PCEController[0]);
			PCEController2Panel.LoadSettings(Global.Config.PCEController[1]);
			PCEController3Panel.LoadSettings(Global.Config.PCEController[2]);
			PCEController4Panel.LoadSettings(Global.Config.PCEController[3]);
			PCEController5Panel.LoadSettings(Global.Config.PCEController[4]);
			PCEAutofire1Panel.LoadSettings(Global.Config.PCEAutoController[0]);
			PCEAutofire2Panel.LoadSettings(Global.Config.PCEAutoController[1]);
			PCEAutofire3Panel.LoadSettings(Global.Config.PCEAutoController[2]);
			PCEAutofire4Panel.LoadSettings(Global.Config.PCEAutoController[3]);
			PCEAutofire5Panel.LoadSettings(Global.Config.PCEAutoController[4]);


			Atari2600Controller1Panel.LoadSettings(Global.Config.Atari2600Controller[0]);
			Atari2600Controller2Panel.LoadSettings(Global.Config.Atari2600Controller[1]);
			Atari2600Autofire1Panel.LoadSettings(Global.Config.Atari2600AutoController[0]);
			Atari2600Autofire2Panel.LoadSettings(Global.Config.Atari2600AutoController[1]);

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
				if (control1 is TabPage)
				{
					foreach (Control control2 in control1.Controls)
					{
						if (control2 is ControllerConfigPanel)
						{
							(control2 as ControllerConfigPanel).Save();
						}
						else if (control2 is TabControl)
						{
							foreach (Control control3 in (control2 as TabControl).TabPages)
							{
								if (control3 is TabPage)
								{
									foreach (Control control4 in control3.Controls)
									{
										if (control4 is ControllerConfigPanel)
										{
											(control4 as ControllerConfigPanel).Save();
										}
									}
								}
								else if (control3 is ControllerConfigPanel)
								{
									(control3 as ControllerConfigPanel).Save();
								}
							}
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
