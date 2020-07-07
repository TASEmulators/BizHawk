using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.WinForms.BuilderDSL;

using static BizHawk.WinForms.BuilderDSL.BuilderDSL;

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
			var builderContext = new ControlBuilderContext(isLTR: true);

			cbConsiderLag = BuildUnparented(builderContext)
				.AddCheckBox("Take lag frames into account", blueprint: cb => cb.InnerPaddingInLTR(4, 0, 0, 0))
				.GetControlRef();
			Blueprint<NUDBuilder> createNUD = nud =>
			{
				nud.SetInitialValue(1.0M);
				nud.SetValidRange(1.0M.RangeTo(512.0M));
				nud.FixedSize(48, 20);
			};
			nudPatternOn = BuildUnparented(builderContext).AddNUD(createNUD).GetControlRef();
			nudPatternOff = BuildUnparented(builderContext).AddNUD(createNUD).GetControlRef();
			var flpPatternBuilt = BuildUnparented(builderContext).AddFLPSingleRowLTRInLTR(flpPattern =>
			{
				flpPattern.AddLabel("Pattern:");
			});
			flpPatternBuilt.HackAddChild(nudPatternOn);
			flpPatternBuilt.AddChildren(flpPattern => flpPattern.AddLabel("on,"));
			flpPatternBuilt.HackAddChild(nudPatternOff);
			flpPatternBuilt.AddChildren(flpPattern => flpPattern.AddLabel("off"));
			var flpDialogBuilt = BuildUnparented(builderContext).AddFLPSingleColumn(flpDialog =>
			{
				flpDialog.Position(0, 0);
				flpDialog.FixedSize(323, 55);
				flpDialog.AnchorAll();
			});
			flpDialogBuilt.HackAddChild(flpPatternBuilt.GetControlRef());
			flpDialogBuilt.HackAddChild(cbConsiderLag);

			var btnDialogOK = BuildUnparented(builderContext).AddButton("&OK", button =>
			{
				button.FixedSize(75, 23);
				button.SubToClick(btnDialogOK_Click);
			}).GetControlRef();
			var btnDialogCancel = BuildUnparented(builderContext).AddButton("&Cancel", button =>
			{
				button.SetDialogResult(DialogResult.Cancel);
				button.FixedSize(75, 23);
				button.SubToClick(btnDialogCancel_Click);
			}).GetControlRef();
			var flpDialogButtonsBuilt = BuildUnparented(builderContext).AddFLPSingleRowLTRInLTR(flpDialogButtons =>
			{
				flpDialogButtons.Position(161, 61);
				flpDialogButtons.FixedSize(162, 29);
				flpDialogButtons.AnchorBottomRightInLTR();
			});
			flpDialogButtonsBuilt.HackAddChild(btnDialogOK);
			flpDialogButtonsBuilt.HackAddChild(btnDialogCancel);

			AcceptButton = btnDialogOK;
			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = btnDialogCancel;
			ClientSize = new Size(323, 90);
			Controls.Add(flpDialogBuilt.GetControlRef());
			Controls.Add(flpDialogButtonsBuilt.GetControlRef());
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
