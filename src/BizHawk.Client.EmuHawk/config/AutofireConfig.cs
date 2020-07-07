using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public class AutofireConfig : Form
	{
		private readonly Config _config;
		private readonly AutofireController _autoFireController;
		private readonly AutoFireStickyXorAdapter _stickyXorAdapter;

		private readonly CheckBox cbConsiderLag;

		private readonly NumericUpDown nudPatternOff;

		private readonly NumericUpDown nudPatternOn;

		public AutofireConfig(
			Config config,
			AutofireController autoFireController,
			AutoFireStickyXorAdapter stickyXorAdapter)
		{
			_config = config;
			_autoFireController = autoFireController;
			_stickyXorAdapter = stickyXorAdapter;
			SuspendLayout();

			cbConsiderLag = new CheckBoxEx { Padding = new Padding(4, 0, 0, 0), Text = "Take lag frames into account" };
			nudPatternOn = new SzNUDEx { Maximum = 512.0M, Minimum = 1.0M, Size = new Size(48, 20), Value = 1.0M };
			nudPatternOff = new SzNUDEx { Maximum = 512.0M, Minimum = 1.0M, Size = new Size(48, 20), Value = 1.0M };
			var flpDialog = new LocSzSingleColumnFLP
			{
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
				Controls =
				{
					new SingleRowFLP
					{
						Controls =
						{
							new LabelEx { Text = "Pattern:" },
							nudPatternOn,
							new LabelEx { Text = "on," },
							nudPatternOff,
							new LabelEx { Text = "off" }
						}
					},
					cbConsiderLag
				},
				Location = new Point(0, 0),
				Size = new Size(323, 55)
			};

			var btnDialogOK = new SzButtonEx { Size = new Size(75, 23), Text = "&OK" };
			btnDialogOK.Click += btnDialogOK_Click;
			var btnDialogCancel = new SzButtonEx { DialogResult = DialogResult.Cancel, Size = new Size(75, 23), Text = "&Cancel" };
			btnDialogCancel.Click += btnDialogCancel_Click;
			var flpDialogButtons = new LocSzSingleRowFLP
			{
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				Controls = { btnDialogOK, btnDialogCancel },
				Location = new Point(161, 61),
				Size = new Size(162, 29)
			};

			AcceptButton = btnDialogOK;
			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = btnDialogCancel;
			ClientSize = new Size(323, 90);
			Controls.Add(flpDialog);
			Controls.Add(flpDialogButtons);
			Icon = Properties.Resources.Lightning_MultiSize;
			MaximizeBox = false;
			base.MinimumSize = new Size(339, 129);
			Name = "AutofireConfig";
			StartPosition = FormStartPosition.CenterParent;
			base.Text = "Autofire Configuration";
			Load += AutofireConfig_Load;
			ResumeLayout(false);
		}

		private void AutofireConfig_Load(object sender, EventArgs e)
		{
			if (_config.AutofireOn < nudPatternOn.Minimum)
			{
				nudPatternOn.Value = nudPatternOn.Minimum;
			}
			else if (_config.AutofireOn > nudPatternOn.Maximum)
			{
				nudPatternOn.Value = nudPatternOn.Maximum;
			}
			else
			{
				nudPatternOn.Value = _config.AutofireOn;
			}

			if (_config.AutofireOff < nudPatternOff.Minimum)
			{
				nudPatternOff.Value = nudPatternOff.Minimum;
			}
			else if (_config.AutofireOff > nudPatternOff.Maximum)
			{
				nudPatternOff.Value = nudPatternOff.Maximum;
			}
			else
			{
				nudPatternOff.Value = _config.AutofireOff;
			}

			cbConsiderLag.Checked = _config.AutofireLagFrames;
		}

		private void btnDialogOK_Click(object sender, EventArgs e)
		{
			_autoFireController.On = _config.AutofireOn = (int)nudPatternOn.Value;
			_autoFireController.Off = _config.AutofireOff = (int)nudPatternOff.Value;
			_config.AutofireLagFrames = cbConsiderLag.Checked;
			_stickyXorAdapter.SetOnOffPatternFromConfig(_config.AutofireOn, _config.AutofireOff);

			Close();
		}

		private void btnDialogCancel_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
