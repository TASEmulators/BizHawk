using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class SoundConfig : Form
	{
		private readonly CheckBox cbEnableNormal;
		private readonly TrackBar tbNormal;
		private readonly CheckBox cbEnableRWFFW;
		private readonly TrackBar tbRWFFW;
		private readonly CheckBox cbEnableMaster;
		private readonly CheckBox cbMuteFrameAdvance;
		private readonly RadioButton rbOutputMethodDirectSound;
		private readonly RadioButton rbOutputMethodXAudio2;
		private readonly RadioButton rbOutputMethodOpenAL;
		private readonly ListBox lbSoundDevices;
		private readonly NumericUpDown nudBufferSize;

		public SoundConfig()
		{
			cbEnableNormal = new CheckBox { AutoSize = true, Text = "Enable", UseVisualStyleBackColor = true };
			var trackBarSize = new Size(32, 160);
			tbNormal = new TrackBar { LargeChange = 10, Maximum = 100, Orientation = Orientation.Vertical, Size = trackBarSize, TickFrequency = 10 };
			var nudSize = new Size(48, 19);
			var nudNormal = new NumericUpDown { Size = nudSize };
			nudNormal.ValueChanged += (sender, e) =>
			{
				var newValue = (int) ((NumericUpDown) sender).Value;
				tbNormal.Value = newValue;
				cbEnableNormal.Checked = newValue != 0; // mute when set to 0% volume
			};
			tbNormal.Scroll += (sender, e) => nudNormal.Value = ((TrackBar) sender).Value;
			nudNormal.Value = Global.Config.SoundVolume;

			cbEnableRWFFW = new CheckBox { AutoSize = true, Text = "Enable", UseVisualStyleBackColor = true };
			tbRWFFW = new TrackBar { LargeChange = 10, Maximum = 100, Orientation = Orientation.Vertical, Size = trackBarSize, TickFrequency = 10 };
			var nudRWFFW = new NumericUpDown { Size = nudSize };
			nudRWFFW.ValueChanged += (sender, e) =>
			{
				var newValue = (int) ((NumericUpDown) sender).Value;
				tbRWFFW.Value = newValue;
				cbEnableRWFFW.Checked = newValue != 0; // mute when set to 0% volume
			};
			tbRWFFW.Scroll += (sender, e) => nudRWFFW.Value = ((TrackBar) sender).Value;
			nudRWFFW.Value = Global.Config.SoundVolumeRWFF;
			cbEnableRWFFW.Checked = Global.Config.SoundEnabledRWFF;

			var flpRWFFW = new SingleColumnFLP
			{
				Controls = { new AutosizedLabel("RW/FFW"), cbEnableRWFFW, tbRWFFW, nudRWFFW },
				Margin = Padding.Empty
			};
			cbEnableNormal.CheckedChanged += (sender, e) => flpRWFFW.Enabled = ((CheckBox) sender).Checked;
			cbEnableNormal.Checked = Global.Config.SoundEnabledNormal;

			var grpSoundVol = new FLPInGroupBox
			{
				Controls = {
					new SingleColumnFLP
					{
						Controls = { new AutosizedLabel("Normal"), cbEnableNormal, tbNormal, nudNormal },
						Margin = Padding.Empty
					},
					flpRWFFW
				},
				InnerFLP = { FlowDirection = FlowDirection.LeftToRight },
				Size = new Size(124, 248),
				Text = "Volume"
			};

			cbEnableMaster = new CheckBox { AutoSize = true, Text = "Master sound toggle", UseVisualStyleBackColor = true };
			cbMuteFrameAdvance = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.MuteFrameAdvance,
				Text = "Mute Frame Advance",
				UseVisualStyleBackColor = true
			};
			cbEnableMaster.CheckedChanged += (sender, e) => grpSoundVol.Enabled = ((CheckBox) sender).Checked;
			cbEnableMaster.Checked = Global.Config.SoundEnabled;

			var onWindows = OSTailoredCode.CurrentOS == OSTailoredCode.DistinctOS.Windows;
			rbOutputMethodDirectSound = new RadioButton { AutoSize = true, Enabled = onWindows, Text = "DirectSound", UseVisualStyleBackColor = true };
			rbOutputMethodXAudio2 = new RadioButton { AutoSize = true, Enabled = onWindows, Text = "XAudio2", UseVisualStyleBackColor = true };
			rbOutputMethodOpenAL = new RadioButton { AutoSize = true, Text = "OpenAL", UseVisualStyleBackColor = true };
			lbSoundDevices = new ListBox { FormattingEnabled = true, Size = new Size(224, 96) };
			void UpdateDeviceList(object sender, EventArgs e)
			{
				if ((sender as RadioButton)?.Checked == false) return; // only update for the radio button just clicked, or once in the constructor

				IEnumerable<string> deviceNames;
				if (OSTailoredCode.CurrentOS == OSTailoredCode.DistinctOS.Windows)
				{
					if (rbOutputMethodDirectSound.Checked) deviceNames = DirectSoundSoundOutput.GetDeviceNames();
					else if (rbOutputMethodXAudio2.Checked) deviceNames = XAudio2SoundOutput.GetDeviceNames();
					else if (rbOutputMethodOpenAL.Checked) deviceNames = OpenALSoundOutput.GetDeviceNames();
					else deviceNames = Enumerable.Empty<string>(); // never hit
				}
				else
				{
					deviceNames = OpenALSoundOutput.GetDeviceNames();
				}
				lbSoundDevices.Items.Clear();
				lbSoundDevices.Items.Add("<default>");
				var i = 1;
				foreach (var name in deviceNames)
				{
					lbSoundDevices.Items.Add(name);
					if (name == Global.Config.SoundDevice) lbSoundDevices.SelectedIndex = i;
					i++;
				}
			}
			rbOutputMethodDirectSound.CheckedChanged += UpdateDeviceList;
			rbOutputMethodXAudio2.CheckedChanged += UpdateDeviceList;
			rbOutputMethodOpenAL.CheckedChanged += UpdateDeviceList;
			var checkedRadio = Global.Config.SoundOutputMethod switch
			{
				Config.ESoundOutputMethod.DirectSound => rbOutputMethodDirectSound,
				Config.ESoundOutputMethod.XAudio2 => rbOutputMethodXAudio2,
				Config.ESoundOutputMethod.OpenAL => rbOutputMethodOpenAL,
				_ => null
			};
			if (checkedRadio != null) checkedRadio.Checked = true;

			var grpOutputMethod = new FLPInGroupBox
			{
				Controls = { rbOutputMethodDirectSound, rbOutputMethodXAudio2, rbOutputMethodOpenAL },
				Size = new Size(88, 84),
				Text = "Output Method"
			};

			var flpDeviceSelector = new SingleColumnFLP
			{
				AutoSize = false,
				Controls = { new AutosizedLabel("Sound Device:"), lbSoundDevices },
				Size = new Size(254, 112)
			};

			nudBufferSize = new NumericUpDown { Maximum = 250.0M, Minimum = 30.0M, Size = nudSize, Value = Global.Config.SoundBufferSizeMs };

			var labelAlignment = new Padding(0, 5, 0, 0);
			var flpRHS = new FlowLayoutPanel
			{
				AutoSize = true,
				Controls =
				{
					new SingleColumnFLP
					{
						Controls =
						{
							cbEnableMaster,
							new AutosizedLabel("Controls whether cores\neven generate audio.") { Margin = new Padding(24, 0, 0, 16) },
							cbMuteFrameAdvance
						}
					},
					grpOutputMethod,
					flpDeviceSelector,
					new SingleRowFLP
					{
						Controls =
						{
							new AutosizedLabel("Buffer Size:") { Margin = labelAlignment },
							nudBufferSize,
							new AutosizedLabel("milliseconds") { Margin = labelAlignment }
						}
					}
				}
			};
			flpRHS.SetFlowBreak(grpOutputMethod, true);
			flpRHS.SetFlowBreak(flpDeviceSelector, true);

			var btnOk = new Button { Text = "&OK", UseVisualStyleBackColor = true };
			btnOk.Click += (sender, e) =>
			{
				if (rbOutputMethodDirectSound.Checked && nudBufferSize.Value < 60.0M)
				{
					MessageBox.Show("Buffer size must be at least 60 milliseconds for DirectSound.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				var oldOutputMethod = Global.Config.SoundOutputMethod;
				var oldDevice = Global.Config.SoundDevice;

				SaveControlsTo(Global.Config);

				GlobalWin.Sound.StopSound();
				if (Global.Config.SoundOutputMethod != oldOutputMethod || Global.Config.SoundDevice != oldDevice)
				{
					GlobalWin.Sound.Dispose();
					GlobalWin.Sound = new Sound(GlobalWin.MainForm.Handle);
				}
				GlobalWin.Sound.StartSound();

				GlobalWin.OSD.AddMessage("Sound settings saved");
				DialogResult = DialogResult.OK;
			};

			var btnCancel = new Button { Text = "&Cancel", UseVisualStyleBackColor = true };
			btnCancel.Click += (sender, e) =>
			{
				GlobalWin.OSD.AddMessage("Sound config aborted");
				Close();
			};

			SuspendLayout();
			AcceptButton = btnOk;
			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = btnCancel;
			ClientSize = new Size(372, 276);
			Controls.AddRange(new Control[]
			{
				new SingleRowFLP { Controls = { btnOk, btnCancel }, Location = new Point(208, 244) },
				new SingleRowFLP { Controls = { grpSoundVol, flpRHS }, Location = new Point(4, 0) }
			});
			FormBorderStyle = FormBorderStyle.FixedDialog;
			Name = "SoundConfig";
			ShowIcon = false;
			StartPosition = FormStartPosition.CenterParent;
			Text = "Sound Configuration";
			ResumeLayout();
		}

		private void SaveControlsTo(Config config)
		{
			config.SoundEnabledNormal = cbEnableNormal.Checked;
			config.SoundVolume = tbNormal.Value;
			config.SoundEnabledRWFF = cbEnableRWFFW.Checked;
			config.SoundVolumeRWFF = tbRWFFW.Value;
			config.SoundEnabled = cbEnableMaster.Checked;
			config.MuteFrameAdvance = cbMuteFrameAdvance.Checked;
			if (rbOutputMethodDirectSound.Checked) config.SoundOutputMethod = Config.ESoundOutputMethod.DirectSound;
			else if (rbOutputMethodXAudio2.Checked) config.SoundOutputMethod = Config.ESoundOutputMethod.XAudio2;
			else if (rbOutputMethodOpenAL.Checked) config.SoundOutputMethod = Config.ESoundOutputMethod.OpenAL;
			config.SoundDevice = (string) lbSoundDevices.SelectedItem ?? "<default>";
			config.SoundBufferSizeMs = (int) nudBufferSize.Value;
		}
	}
}
