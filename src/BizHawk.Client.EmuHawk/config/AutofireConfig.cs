using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;
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

		private CheckBox cbConsiderLag;

		private NumericUpDown nudPatternOff;

		private NumericUpDown nudPatternOn;

		public AutofireConfig(
			Config config,
			AutofireController autoFireController,
			AutoFireStickyXorAdapter stickyXorAdapter)
		{
			_config = config;
			_autoFireController = autoFireController;
			_stickyXorAdapter = stickyXorAdapter;

			SuspendLayout();
			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(323, 90);
			Icon = Properties.Resources.Lightning_MultiSize;
			MaximizeBox = false;
			base.MinimumSize = new Size(339, 129);
			Name = "AutofireConfig";
			StartPosition = FormStartPosition.CenterParent;
			base.Text = "Autofire Configuration";
			var builderContext = new ControlBuilderContext(isLTR: true);

			Controls.Add(BuildUnparented(builderContext).AddFLPSingleColumn(flpDialog =>
			{
				flpDialog.Position(0, 0);
				flpDialog.FixedSize(323, 55);
				flpDialog.AnchorAll();
				flpDialog.AddFLPSingleRowLTRInLTR(flpPattern =>
				{
					static Blueprint<NUDBuilder> CreateNUD(decimal initValue) => nud =>
					{
						nud.SetValidRange(1.0M.RangeTo(512.0M));
						nud.SetInitialValue(initValue);
						nud.FixedSize(48, 20);
					};
					flpPattern.AddLabel("Pattern:");
					nudPatternOn = flpPattern.AddNUD(CreateNUD(_config.AutofireOn)).GetControlRef();
					flpPattern.AddLabel("on,");
					nudPatternOff = flpPattern.AddNUD(CreateNUD(_config.AutofireOff)).GetControlRef();
					flpPattern.AddLabel("off");
				});
				cbConsiderLag = flpDialog.AddCheckBox(cb =>
				{
					cb.LabelText("Take lag frames into account");
					cb.CheckIf(_config.AutofireLagFrames);
					cb.InnerPaddingInLTR(4, 0, 0, 0);
				}).GetControlRef();
			}).GetControlRef());

			Button btnOKRef = null;
			Button btnCancelRef = null;
			Controls.Add(BuildUnparented(builderContext).AddFLPSingleRowLTRInLTR(flpDialogButtons =>
			{
				flpDialogButtons.Position(161, 61);
				flpDialogButtons.FixedSize(162, 29);
				flpDialogButtons.AnchorBottomRightInLTR();
				btnOKRef = flpDialogButtons.DialogOKButton(btnDialogOK_Click).GetControlRef();
				btnCancelRef = flpDialogButtons.DialogCancelButton(btnDialogCancel_Click).GetControlRef();
			}).GetControlRef());
			AcceptButton = btnOKRef;
			CancelButton = btnCancelRef;

			ResumeLayout();
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
