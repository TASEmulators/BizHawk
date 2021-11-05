using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;

namespace BizHawk.Client.EmuHawk
{
	public partial class DualNDSCoreConfig : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private NDS.NDSSettings _singleS;
		private NDS.NDSSyncSettings _singleSs;
		private DualNDS.DualNDSSettings _dualS;
		private DualNDS.DualNDSSyncSettings _dualSs;
		private bool _syncSettingsChanged;
		private bool _settingsChanged;
		private bool _right;

		private DualNDSCoreConfig(IMainFormForConfig mainForm, DualNDS.DualNDSSettings settings, DualNDS.DualNDSSyncSettings syncSettings, bool right)
		{
			InitializeComponent();
			_mainForm = mainForm;

			_dualS = settings;
			_dualSs = syncSettings;
			_singleS = right ? settings.R : settings.L;
			_singleSs = right ? syncSettings.R : syncSettings.L;
			_right = right;

			propertyGrid1.SelectedObject = _singleS;
			propertyGrid1.AdjustDescriptionHeightToFit();

			propertyGrid2.SelectedObject = _singleSs;
			propertyGrid2.AdjustDescriptionHeightToFit();

			if (_mainForm.MovieSession.Movie.IsActive())
			{
				propertyGrid2.Enabled = false; // disable changes to sync setting when movie, so as not to confuse user
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			if (_settingsChanged)
			{
				if (_right)
				{
					_dualS.R = _singleS;
				}
				else
				{
					_dualS.L = _singleS;
				}
				_mainForm.PutCoreSettings(_dualS);
			}

			if (_syncSettingsChanged)
			{
				if (_right)
				{
					_dualSs.R = _singleSs;
				}
				else
				{
					_dualSs.L = _singleSs;
				}
				_mainForm.PutCoreSyncSettings(_dualSs);
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		public static void DoDialog(IMainFormForConfig owner, DualNDS.DualNDSSettings settings, DualNDS.DualNDSSyncSettings syncSettings, bool right)
		{
			using var dlg = new DualNDSCoreConfig(owner, settings, syncSettings, right) { Text = (right ? "Right " : "Left ") + "Dual NDS Settings" };
			owner.ShowDialogAsChild(dlg);
		}

		private void PropertyGrid2_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			_syncSettingsChanged = true;
		}

		private void DefaultsBtn_Click(object sender, EventArgs e)
		{
			// the new config objects guarantee that the default constructor gives a default-settings object
			_singleS = new NDS.NDSSettings();
			propertyGrid1.SelectedObject = _singleS;
			_settingsChanged = true;

			_singleSs = new NDS.NDSSyncSettings();
			propertyGrid2.SelectedObject = _singleSs;
			_syncSettingsChanged = true;
		}

		private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			_settingsChanged = true;
		}
	}
}
