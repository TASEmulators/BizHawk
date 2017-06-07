using System;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericCoreConfig : Form
	{
		private object _s;
		private object _ss;
		private bool _syncsettingschanged;

		private GenericCoreConfig(bool ignoresettings, bool ignoresyncsettings)
		{
			InitializeComponent();

			var settable = new SettingsAdapter(Global.Emulator);

			if (settable.HasSettings && !ignoresettings)
			{
				_s = settable.GetSettings();
			}

			if (settable.HasSyncSettings && !ignoresyncsettings)
			{
				_ss = settable.GetSyncSettings();
			}

			if (_s != null)
			{
				propertyGrid1.SelectedObject = _s;
			}
			else
			{
				tabControl1.TabPages.Remove(tabPage1);
			}

			if (_ss != null)
			{
				propertyGrid2.SelectedObject = _ss;
			}
			else
			{
				tabControl1.TabPages.Remove(tabPage2);
			}

			if (Global.MovieSession.Movie.IsActive)
			{
				propertyGrid2.Enabled = false; // disable changes to sync setting when movie, so as not to confuse user
			}
		}

		private GenericCoreConfig()
			: this(false, false)
		{
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var settable = new SettingsAdapter(Global.Emulator);
			if (_s != null && settable.HasSettings)
			{
				settable.PutSettings(_s);
			}

			if (_ss != null && _syncsettingschanged)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(_ss);
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		public static void DoDialog(IWin32Window owner, string title)
		{
			using (var dlg = new GenericCoreConfig { Text = title })
			{
				dlg.ShowDialog(owner);
			}
		}

		public static void DoDialog(IWin32Window owner, string title, bool hidesettings, bool hidesyncsettings)
		{
			using (var dlg = new GenericCoreConfig(hidesettings, hidesyncsettings) { Text = title })
			{
				dlg.ShowDialog(owner);
			}
		}

		private void PropertyGrid2_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			_syncsettingschanged = true;
		}

		private void GenericCoreConfig_Load(object sender, EventArgs e)
		{
		}

		private void DefaultsBtn_Click(object sender, EventArgs e)
		{
			// the new config objects guarantee that the default constructor gives a default-settings object
			if (_s != null)
			{
				_s = Activator.CreateInstance(_s.GetType());
				propertyGrid1.SelectedObject = _s;
			}

			if (_ss != null)
			{
				_ss = Activator.CreateInstance(_ss.GetType());
				propertyGrid2.SelectedObject = _ss;
				_syncsettingschanged = true;
			}
		}
	}
}
