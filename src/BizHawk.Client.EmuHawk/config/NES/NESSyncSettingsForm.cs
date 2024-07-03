using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

using static BizHawk.Common.StringExtensions.NumericStringExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESSyncSettingsForm : Form, IDialogParent
	{
		private readonly DataTableDictionaryBind<string, string> _dataTableDictionary;

		private readonly ISettingsAdapter _settable;

		private readonly NES.NESSyncSettings _syncSettings;

		public IDialogController DialogController { get; }

		public NESSyncSettingsForm(
			IDialogController dialogController,
			ISettingsAdapter settable,
			bool hasMapperProperties)
		{
			_settable = settable;
			_syncSettings = (NES.NESSyncSettings) _settable.GetSyncSettings();
			DialogController = dialogController;
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

			var initWRAMPattern = _syncSettings.InitialWRamStatePattern;
			if (initWRAMPattern.Length is not 0) RamPatternOverrideBox.Text = initWRAMPattern.BytesToHexString();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			static byte[] ParseInitRAMPattern(string/*?*/ ss)
			{
				if (string.IsNullOrWhiteSpace(ss)) return Array.Empty<byte>();
				if (!ss.All(NumericStringExtensions.IsHex))
				{
					//TODO warn
					return Array.Empty<byte>();
				}
				var s = ss.AsSpan();
				var a = new byte[(s.Length + 1) / 2];
				var iArr = 0;
				var iStr = 0;
				if (s.Length % 2 is 1) a[iArr++] = ParseU8FromHex(s.Slice(start: iStr++, length: 1));
				while (iStr < s.Length)
				{
					a[iArr++] = ParseU8FromHex(s.Slice(start: iStr, length: 2));
					iStr += 2;
				}
				return a;
			}

			var newInitRAMPattern = ParseInitRAMPattern(RamPatternOverrideBox.Text);
			var newRegionOverride = (NES.NESSyncSettings.Region) Enum.Parse(
				typeof(NES.NESSyncSettings.Region),
				(string)RegionComboBox.SelectedItem);

			var changed = _dataTableDictionary?.WasModified is true
				|| newRegionOverride != _syncSettings.RegionOverride
				|| !newInitRAMPattern.SequenceEqual(_syncSettings.InitialWRamStatePattern);
			_syncSettings.InitialWRamStatePattern = newInitRAMPattern;
			_syncSettings.RegionOverride = newRegionOverride;
			DialogResult = DialogResult.OK;
			if (changed)
			{
				_settable.PutCoreSyncSettings(_syncSettings);
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
