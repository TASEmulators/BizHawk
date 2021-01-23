using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESSyncSettingsForm : Form, IDialogParent
	{
		private readonly IMainFormForConfig _mainForm;
		private readonly DataTableDictionaryBind<string, string> _dataTableDictionary;
		private readonly NES.NESSyncSettings _syncSettings;

		public IDialogController DialogController { get; }

		public NESSyncSettingsForm(
			IMainFormForConfig mainForm,
			NES.NESSyncSettings syncSettings,
			bool hasMapperProperties)
		{
			_mainForm = mainForm;
			_syncSettings = syncSettings;
			DialogController = mainForm.DialogController;
			InitializeComponent();
			HelpBtn.Image = Properties.Resources.Help;

			if (hasMapperProperties)
			{
				_dataTableDictionary = new DataTableDictionaryBind<string, string>(_syncSettings.BoardProperties);
				dataGridView1.DataSource = _dataTableDictionary.Table;
				InfoLabel.Visible = false;
			}
			else
			{
				BoardPropertiesGroupBox.Enabled = false;
				dataGridView1.DataSource = null;
				dataGridView1.Enabled = false;
				InfoLabel.Visible = true;
			}

			RegionComboBox.Items.AddRange(Enum.GetNames(typeof(NES.NESSyncSettings.Region)).Cast<object>().ToArray());
			RegionComboBox.SelectedItem = Enum.GetName(typeof(NES.NESSyncSettings.Region), _syncSettings.RegionOverride);

			if (_syncSettings.InitialWRamStatePattern != null && _syncSettings.InitialWRamStatePattern.Any())
			{
				var sb = new StringBuilder();
				foreach (var b in _syncSettings.InitialWRamStatePattern)
				{
					sb.Append($"{b:X2}");
				}

				RamPatternOverrideBox.Text = sb.ToString();
			}
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var old = _syncSettings.RegionOverride;
			_syncSettings.RegionOverride = (NES.NESSyncSettings.Region)
				Enum.Parse(
				typeof(NES.NESSyncSettings.Region),
				(string)RegionComboBox.SelectedItem);

			var oldRam = _syncSettings.InitialWRamStatePattern ?? new List<byte>();

			if (!string.IsNullOrWhiteSpace(RamPatternOverrideBox.Text))
			{
				_syncSettings.InitialWRamStatePattern = Enumerable.Range(0, RamPatternOverrideBox.Text.Length)
					.Where(x => x % 2 == 0)
					.Select(x => Convert.ToByte(RamPatternOverrideBox.Text.Substring(x, 2), 16))
					.ToList();
			}
			else
			{
				_syncSettings.InitialWRamStatePattern = null;
			}

			bool changed = (_dataTableDictionary != null && _dataTableDictionary.WasModified) ||
				old != _syncSettings.RegionOverride ||
				!(oldRam.SequenceEqual(_syncSettings.InitialWRamStatePattern ?? new List<byte>()));

			DialogResult = DialogResult.OK;
			if (changed)
			{
				_mainForm.PutCoreSyncSettings(_syncSettings);
			}
		}

		private void HelpBtn_Click(object sender, EventArgs e)
		{
			this.ModalMessageBox(
				"Board Properties are special per-mapper system settings.  They are only useful to advanced users creating Tool Assisted Superplays.  No support will be provided if you break something with them.",
				"Help",
				EMsgBoxIcon.Info);
		}
	}
}
