using System;
using System.ComponentModel;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericCoreConfig : Form
	{
		private readonly ISettingsAdapter _settable;

		private object _s;
		private object _ss;
		private bool _syncSettingsChanged;
		private bool _settingsChanged;

		private GenericCoreConfig(
			ISettingsAdapter settable,
			bool isMovieActive,
			bool ignoreSettings = false,
			bool ignoreSyncSettings = false)
		{
			InitializeComponent();

			_settable = settable;

			if (_settable.HasSettings && !ignoreSettings) _s = _settable.GetSettings();

			if (_settable.HasSyncSettings && !ignoreSyncSettings) _ss = _settable.GetSyncSettings();

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

			if (isMovieActive)
			{
				propertyGrid2.Enabled = false; // disable changes to sync setting when movie, so as not to confuse user
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			if (_s != null && _settingsChanged)
			{
				_settable.PutCoreSettings(_s);
			}

			if (_ss != null && _syncSettingsChanged)
			{
				_settable.PutCoreSyncSettings(_ss);
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		public static DialogResult DoDialog(IEmulator emulator, IDialogParent owner, string title, bool isMovieActive)
		{
			if (emulator is Emulation.Cores.Waterbox.NymaCore core)
			{
				var desc = new Emulation.Cores.Waterbox.NymaTypeDescriptorProvider(core.SettingsInfo);
				try
				{
					// OH GOD THE HACKS WHY
					TypeDescriptor.AddProvider(desc, typeof(Emulation.Cores.Waterbox.NymaCore.NymaSettings));
					TypeDescriptor.AddProvider(desc, typeof(Emulation.Cores.Waterbox.NymaCore.NymaSyncSettings));
					return DoDialog(owner, "Nyma Core", isMovieActive, !core.SettingsInfo.HasSettings, !core.SettingsInfo.HasSyncSettings);
				}
				finally
				{
					TypeDescriptor.RemoveProvider(desc, typeof(Emulation.Cores.Waterbox.NymaCore.NymaSettings));
					TypeDescriptor.RemoveProvider(desc, typeof(Emulation.Cores.Waterbox.NymaCore.NymaSyncSettings));
				}
			}
			else if (emulator is Emulation.Cores.Arcades.MAME.MAME mame)
			{
				var desc = new Emulation.Cores.Arcades.MAME.MAMETypeDescriptorProvider(mame.CurrentDriverSettings);
				try
				{
					TypeDescriptor.AddProvider(desc, typeof(Emulation.Cores.Arcades.MAME.MAME.MAMESyncSettings));
					return DoDialog(owner, "MAME", isMovieActive, true, false);
				}
				finally
				{
					TypeDescriptor.RemoveProvider(desc, typeof(Emulation.Cores.Arcades.MAME.MAME.MAMESyncSettings));
				}
			}
			else
			{
				return DoDialog(owner, title, isMovieActive, false, false);
			}
		}

		public static DialogResult DoDialog(
			IDialogParent owner,
			string title,
			bool isMovieActive,
			bool hideSettings,
			bool hideSyncSettings)
		{
			using var dlg = new GenericCoreConfig(
				((MainForm) owner).GetSettingsAdapterForLoadedCoreUntyped(), //HACK
				isMovieActive: isMovieActive,
				ignoreSettings: hideSettings,
				ignoreSyncSettings: hideSyncSettings)
			{
				Text = title,
			};
			return owner.ShowDialogAsChild(dlg);
		}

		public static DialogResult DoDialogFor(
			IDialogParent owner,
			ISettingsAdapter settable,
			string title,
			bool isMovieActive)
		{
			using GenericCoreConfig dlg = new(settable, isMovieActive) { Text = title };
			return owner.ShowDialogAsChild(dlg);
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
