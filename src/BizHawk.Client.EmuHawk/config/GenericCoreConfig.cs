using System;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericCoreConfig : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private object _s;
		private object _ss;
		private bool _syncSettingsChanged;
		private bool _settingsChanged;

		private GenericCoreConfig(IMainFormForConfig mainForm, bool ignoreSettings = false, bool ignoreSyncSettings = false)
		{
			InitializeComponent();
			_mainForm = mainForm;

			var settable = new SettingsAdapter(_mainForm.Emulator);

			if (settable.HasSettings && !ignoreSettings)
			{
				_s = settable.GetSettings();
			}

			if (settable.HasSyncSettings && !ignoreSyncSettings)
			{
				_ss = settable.GetSyncSettings();
			}

			if (_s != null)
			{
				propertyGrid1.SelectedObject = _s;
				propertyGrid1.AdjustDescriptionHeightToFit();
			}
			else
			{
				tabControl1.TabPages.Remove(tabPage1);
			}

			if (_ss != null)
			{
				propertyGrid2.SelectedObject = _ss;
				propertyGrid2.AdjustDescriptionHeightToFit();
			}
			else
			{
				tabControl1.TabPages.Remove(tabPage2);
			}

			if (_mainForm.MovieSession.Movie.IsActive())
			{
				propertyGrid2.Enabled = false; // disable changes to sync setting when movie, so as not to confuse user
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			if (_s != null && _settingsChanged)
			{
				_mainForm.PutCoreSettings(_s);
			}

			if (_ss != null && _syncSettingsChanged)
			{
				_mainForm.PutCoreSyncSettings(_ss);
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		public static void DoDialog(IMainFormForConfig owner, string title)
		{
			if (owner.Emulator is Emulation.Cores.Waterbox.NymaCore core)
			{
				var desc = new Emulation.Cores.Waterbox.NymaTypeDescriptorProvider(core.SettingsInfo);
				try
				{
					// OH GOD THE HACKS WHY
					TypeDescriptor.AddProvider(desc, typeof(Emulation.Cores.Waterbox.NymaCore.NymaSettings));
					TypeDescriptor.AddProvider(desc, typeof(Emulation.Cores.Waterbox.NymaCore.NymaSyncSettings));
					DoDialog(owner, "Nyma Core", !core.SettingsInfo.HasSettings, !core.SettingsInfo.HasSyncSettings);
				}
				finally
				{
					TypeDescriptor.RemoveProvider(desc, typeof(Emulation.Cores.Waterbox.NymaCore.NymaSettings));
					TypeDescriptor.RemoveProvider(desc, typeof(Emulation.Cores.Waterbox.NymaCore.NymaSyncSettings));
				}
			}
			else
			{
				using var dlg = new GenericCoreConfig(owner) { Text = title };
				owner.ShowDialogAsChild(dlg);
			}
		}

		public static void DoDialog(IMainFormForConfig owner, string title, bool hideSettings, bool hideSyncSettings)
		{
			using var dlg = new GenericCoreConfig(owner, hideSettings, hideSyncSettings) { Text = title };
			owner.ShowDialogAsChild(dlg);
		}
		private void PropertyGrid2_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			_syncSettingsChanged = true;
		}

		private void DefaultsBtn_Click(object sender, EventArgs e)
		{
			// the new config objects guarantee that the default constructor gives a default-settings object
			if (_s != null)
			{
				_s = Activator.CreateInstance(_s.GetType());
				propertyGrid1.SelectedObject = _s;
				_settingsChanged = true;
			}

			if (_ss != null)
			{
				_ss = Activator.CreateInstance(_ss.GetType());
				propertyGrid2.SelectedObject = _ss;
				_syncSettingsChanged = true;
			}
		}

		private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			_settingsChanged = true;
		}
	}
}
