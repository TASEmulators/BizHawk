using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;

namespace BizHawk.Client.EmuHawk.config
{
	public partial class GenericCoreConfig : Form
	{
		object s;
		object ss;
		bool syncsettingschanged = false;

		GenericCoreConfig()
		{
			InitializeComponent();

			s = Global.Emulator.GetSettings();
			ss = Global.Emulator.GetSyncSettings();

			if (s != null)
				propertyGrid1.SelectedObject = s;
			else
				tabControl1.TabPages.Remove(tabPage1);
			if (ss != null)
				propertyGrid2.SelectedObject = ss;
			else
				tabControl1.TabPages.Remove(tabPage2);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (s != null)
				Global.Emulator.PutSettings(s);
			if (ss != null && syncsettingschanged)
				GlobalWin.MainForm.PutCoreSyncSettings(ss);

			DialogResult = DialogResult.OK;
			Close();
		}

		public static void DoDialog(IWin32Window owner, string title)
		{
			using (var dlg = new GenericCoreConfig { Text = title })
				dlg.ShowDialog(owner);
		}

		private void propertyGrid2_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			syncsettingschanged = true;
		}
	}
}
