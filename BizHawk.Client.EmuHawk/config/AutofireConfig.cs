using System;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class AutofireConfig : Form
	{
		private readonly Config _config;
		private readonly AutofireController _autoFireController;
		private readonly AutoFireStickyXorAdapter _stickyXorAdapter;

		public AutofireConfig(
			Config config,
			AutofireController autoFireController,
			AutoFireStickyXorAdapter stickyXorAdapter)
		{
			_config = config;
			_autoFireController = autoFireController;
			_stickyXorAdapter = stickyXorAdapter;
			InitializeComponent();
		}

		private void AutofireConfig_Load(object sender, EventArgs e)
		{
			if (_config.AutofireOn < OnNumeric.Minimum)
			{
				OnNumeric.Value = OnNumeric.Minimum;
			}
			else if (_config.AutofireOn > OnNumeric.Maximum)
			{
				OnNumeric.Value = OnNumeric.Maximum;
			}
			else
			{
				OnNumeric.Value = _config.AutofireOn;
			}

			if (_config.AutofireOff < OffNumeric.Minimum)
			{
				OffNumeric.Value = OffNumeric.Minimum;
			}
			else if (_config.AutofireOff > OffNumeric.Maximum)
			{
				OffNumeric.Value = OffNumeric.Maximum;
			}
			else
			{
				OffNumeric.Value = _config.AutofireOff;
			}

			LagFrameCheck.Checked = _config.AutofireLagFrames;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			_autoFireController.On = _config.AutofireOn = (int)OnNumeric.Value;
			_autoFireController.Off = _config.AutofireOff = (int)OffNumeric.Value;
			_config.AutofireLagFrames = LagFrameCheck.Checked;
			_stickyXorAdapter.SetOnOffPatternFromConfig();

			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
