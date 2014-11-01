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
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericCoreConfig : Form
	{
		object s;
		object ss;
		bool syncsettingschanged = false;

		GenericCoreConfig(bool ignoresettings, bool ignoresyncsettings)
		{
			InitializeComponent();

			var settable = new SettingsAdapter(Global.Emulator);

			if (settable.HasSettings && !ignoresettings)
				s = settable.GetSettings();
			if (settable.HasSyncSettings && !ignoresyncsettings)
				ss = settable.GetSyncSettings();

			if (s != null)
				propertyGrid1.SelectedObject = s;
			else
				tabControl1.TabPages.Remove(tabPage1);
			if (ss != null)
				propertyGrid2.SelectedObject = ss;
			else
				tabControl1.TabPages.Remove(tabPage2);

			if (Global.MovieSession.Movie.IsActive)
				propertyGrid2.Enabled = false; // disable changes to sync setting when movie, so as not to confuse user
		}

		GenericCoreConfig()
			:this(false, false)
		{
		}

		private void button1_Click(object sender, EventArgs e)
		{
			var settable = new SettingsAdapter(Global.Emulator);
			if (s != null && settable.HasSettings)
			{
				settable.PutSettings(s);
			}

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

		public static void DoDialog(IWin32Window owner, string title, bool hidesettings, bool hidesyncsettings)
		{
			using (var dlg = new GenericCoreConfig(hidesettings, hidesyncsettings) { Text = title })
				dlg.ShowDialog(owner);
		}

		private void propertyGrid2_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			syncsettingschanged = true;
		}

		private void GenericCoreConfig_Load(object sender, EventArgs e)
		{

		}

		private void buttonDefaults_Click(object sender, EventArgs e)
		{
			// the new config objects guarantee that the default constructor gives a default-settings object
			if (s != null)
			{
				s = Activator.CreateInstance(s.GetType());
				propertyGrid1.SelectedObject = s;
			}
			if (ss != null)
			{
				ss = Activator.CreateInstance(ss.GetType());
				propertyGrid2.SelectedObject = ss;
				syncsettingschanged = true;
			}

		}
	}
}
