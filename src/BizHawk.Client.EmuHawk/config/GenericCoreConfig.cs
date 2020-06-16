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
		private readonly MainForm _mainForm;
		private object _s;
		private object _ss;
		private bool _syncSettingsChanged;
		private bool _settingsChanged;

		private GenericCoreConfig(MainForm mainForm, bool ignoreSettings = false, bool ignoreSyncSettings = false)
		{
			InitializeComponent();
			_mainForm = mainForm;

			var settable = new SettingsAdapter(GlobalWin.Emulator);

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
				ChangeDescriptionHeight(propertyGrid1);
			}
			else
			{
				tabControl1.TabPages.Remove(tabPage1);
			}

			if (_ss != null)
			{
				propertyGrid2.SelectedObject = _ss;
				ChangeDescriptionHeight(propertyGrid2);
			}
			else
			{
				tabControl1.TabPages.Remove(tabPage2);
			}

			if (GlobalWin.MovieSession.Movie.IsActive())
			{
				propertyGrid2.Enabled = false; // disable changes to sync setting when movie, so as not to confuse user
			}
		}

		private static void ChangeDescriptionHeight(PropertyGrid grid)
		{
			if (grid == null)
			{
				throw new ArgumentNullException(nameof(grid));
			}

			int maxLength = 0;
			string desc = "";

			foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(grid.SelectedObject))
			{
				if (property.Description?.Length > maxLength)
				{
					maxLength = property.Description.Length;
					desc = property.Description;
				}
			}

			foreach (Control control in grid.Controls)
			{
				if (control.GetType().Name == "DocComment")
				{
					FieldInfo field = control.GetType().GetField("userSized", BindingFlags.Instance | BindingFlags.NonPublic);
					field?.SetValue(control, true);
					int height = (int)System.Drawing.Graphics.FromHwnd(control.Handle).MeasureString(desc, control.Font, grid.Width).Height;
					control.Height = Math.Max(20, height) + 16; // magic for now
					return;
				}
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

		public static void DoDialog(MainForm owner, string title)
		{
			if (GlobalWin.Emulator is Emulation.Cores.Waterbox.NymaCore core)
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

		public static void DoDialog(MainForm owner, string title, bool hideSettings, bool hideSyncSettings)
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
