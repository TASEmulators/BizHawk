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
		private object _s;
		private object _ss;
		private bool _syncsettingschanged;
		bool settingschanged = false;

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

			if (Global.MovieSession.Movie.IsActive)
			{
				propertyGrid2.Enabled = false; // disable changes to sync setting when movie, so as not to confuse user
			}
		}

		private GenericCoreConfig()
			: this(false, false)
		{
		}

		private static void ChangeDescriptionHeight(PropertyGrid grid)
		{
			if (grid == null)
				throw new ArgumentNullException("grid");

			int maxlen = 0;
			string desc = "";

			foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(grid.SelectedObject))
			{
				if (property.Description.Length > maxlen)
				{
					maxlen = property.Description.Length;
					desc = property.Description;
				}
			}

			foreach (Control control in grid.Controls)
			{
				if (control.GetType().Name == "DocComment")
				{
					FieldInfo field = control.GetType().GetField("userSized", BindingFlags.Instance | BindingFlags.NonPublic);
					field.SetValue(control, true);
					int height = (int)System.Drawing.Graphics.FromHwnd(control.Handle).MeasureString(desc, control.Font, grid.Width).Height;
					control.Height = Math.Max(20, height) + 16; // magic for now
					return;
				}
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			if (_s != null && settingschanged)
			{
				GlobalWin.MainForm.PutCoreSettings(_s);
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

		private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			settingschanged = true;
		}
	}
}
