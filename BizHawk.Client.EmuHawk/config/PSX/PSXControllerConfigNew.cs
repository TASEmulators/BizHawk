using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Common.ReflectionExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class PSXControllerConfigNew : Form
	{
		public PSXControllerConfigNew()
		{
			InitializeComponent();
		}

		private void PSXControllerConfigNew_Load(object sender, EventArgs e)
		{
			//populate combo boxes
			foreach(var combo in new[]{combo_1_1,combo_1_2,combo_1_3,combo_1_4,combo_2_1,combo_2_2,combo_2_3,combo_2_4})
			{
				combo.Items.Add("-Nothing-");
				combo.Items.Add("Gamepad");
				combo.Items.Add("Dual Shock");
				combo.Items.Add("Dual Analog");
				combo.SelectedIndex = 0;
			}

			RefreshLabels();
		}

		void RefreshLabels()
		{
			bool multitap_1 = cbMultitap_1.Checked;
			bool multitap_2 = cbMultitap_2.Checked;

			bool b1 = multitap_1;
			lbl_1_1.Visible = b1;
			lbl_1_2.Visible = b1;
			lbl_1_3.Visible = b1;
			lbl_1_4.Visible = b1;
			combo_1_2.Enabled = b1;
			combo_1_3.Enabled = b1;
			combo_1_4.Enabled = b1;
			lbl_p_1_2.Visible = b1;
			lbl_p_1_3.Visible = b1;
			lbl_p_1_4.Visible = b1;

			bool b2 = multitap_2;
			lbl_2_1.Visible = b2;
			lbl_2_2.Visible = b2;
			lbl_2_3.Visible = b2;
			lbl_2_4.Visible = b2;
			combo_2_2.Enabled = b2;
			combo_2_3.Enabled = b2;
			combo_2_4.Enabled = b2;
			lbl_p_2_2.Visible = b2;
			lbl_p_2_3.Visible = b2;
			lbl_p_2_4.Visible = b2;

			OctoshockControlUserConfig uc = new OctoshockControlUserConfig();
			
			uc.Multitaps[0] = multitap_1;
			uc.Multitaps[1] = multitap_2;

			var combos = new[] { combo_1_1, combo_1_2, combo_1_3, combo_1_4, combo_2_1, combo_2_2, combo_2_3, combo_2_4};
			for (int i = 0; i < 8; i++)
			{
				var combo = combos[i];
				if (combo.SelectedIndex == 0) uc.Devices8[i] = OctoshockDll.ePeripheralType.None;
				if (combo.SelectedIndex == 1) uc.Devices8[i] = OctoshockDll.ePeripheralType.DualAnalog;
				if (combo.SelectedIndex == 2) uc.Devices8[i] = OctoshockDll.ePeripheralType.DualShock;
			}

			var LC = uc.ToLogicalConfig();

			var p_labels = new[] { lbl_p_1_1,lbl_p_1_2,lbl_p_1_3,lbl_p_1_4,lbl_p_2_1,lbl_p_2_2,lbl_p_2_3,lbl_p_2_4};
			for (int i = 0; i < 8; i++)
			{
				var lbl = p_labels[i];
				if (LC.PlayerAssignments[i] == -1)
					lbl.Visible = false;
				else
				{
					lbl.Text = "P" + LC.PlayerAssignments[i];
					lbl.Visible = true;
				}
			}

		}

		private void cb_changed(object sender, EventArgs e)
		{
			RefreshLabels();
		}

		private void combo_SelectedIndexChanged(object sender, EventArgs e)
		{
			RefreshLabels();
		}
	}
}
