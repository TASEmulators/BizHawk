using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;

namespace BizHawk.Client.EmuHawk
{
	public sealed class AutofireConfig : Form
	{
		private const decimal nudMaximum = 512.0M;
		private const decimal nudMinimum = 1.0M;

		private readonly NumericUpDown nudPatternOn;
		private readonly NumericUpDown nudPatternOff;
		private readonly CheckBox cbConsiderLagFrames;

		public AutofireConfig()
		{
			static decimal ConstrainToNUDRange(decimal d) => d < nudMinimum ? nudMinimum : nudMaximum < d ? nudMaximum : d;
			var nudSize = new Size(48, 19);
			nudPatternOn = new NumericUpDown
			{
				Maximum = nudMaximum,
				Minimum = nudMinimum,
				Size = nudSize,
				Value = ConstrainToNUDRange(Global.Config.AutofireOn)
			};
			nudPatternOff = new NumericUpDown
			{
				Maximum = nudMaximum,
				Minimum = nudMinimum,
				Size = nudSize,
				Value = ConstrainToNUDRange(Global.Config.AutofireOff)
			};
			cbConsiderLagFrames = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.AutofireLagFrames,
				Text = "Take lag frames into account",
				UseVisualStyleBackColor = true
			};
			var labelAlignment = new Padding(0, 5, 0, 0);
			var flpMain = new SingleColumnFLP
			{
				Anchor = AnchorStyles.Top | AnchorStyles.Left,
				Controls =
				{
					new SingleRowFLP
					{
						Controls =
						{
							new AutosizedLabel("Pattern:") { Margin = labelAlignment },
							nudPatternOn,
							new AutosizedLabel("on,") { Margin = labelAlignment },
							nudPatternOff,
							new AutosizedLabel("off") { Margin = labelAlignment }
						}
					},
					cbConsiderLagFrames
				},
				Location = new Point(12, 12)
			};

			var btnOk = new Button { Text = "&OK", UseVisualStyleBackColor = true };
			btnOk.Click += (sender, e) =>
			{
				SaveControlsTo(Global.Config);
				GlobalWin.OSD.AddMessage("Autofire settings saved");
				Close();
			};

			var btnCancel = new Button { Text = "&Cancel", UseVisualStyleBackColor = true };
			btnCancel.Click += (sender, e) =>
			{
				GlobalWin.OSD.AddMessage("Autofire config aborted");
				Close();
			};

			SuspendLayout();
			AcceptButton = btnOk;
			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = btnCancel;
			ClientSize = new Size(276, 175);
			Controls.AddRange(new Control[]
			{
				new SingleRowFLP
				{
					Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
					Controls = { btnOk, btnCancel },
					Location = new Point(108, 140)
				},
				flpMain
			});
			Icon = (Icon) new ComponentResourceManager(typeof(AutofireConfig)).GetObject("$this.Icon");
			MaximizeBox = false;
			MaximumSize = new Size(512, 512);
			MinimumSize = new Size(218, 179);
			Name = "AutofireConfig";
			StartPosition = FormStartPosition.CenterParent;
			Text = "Autofire Configuration";
			ResumeLayout();
		}

		private void SaveControlsTo(Config config)
		{
			Global.AutoFireController.On = config.AutofireOn = (int) nudPatternOn.Value;
			Global.AutoFireController.Off = config.AutofireOff = (int) nudPatternOff.Value;
			config.AutofireLagFrames = cbConsiderLagFrames.Checked;
			Global.AutofireStickyXORAdapter.SetOnOffPatternFromConfig();
		}
	}
}
