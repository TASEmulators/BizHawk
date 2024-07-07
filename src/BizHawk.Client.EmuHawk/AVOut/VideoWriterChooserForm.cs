using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// implements a simple dialog which chooses an IVideoWriter to record with
	/// </summary>
	public partial class VideoWriterChooserForm : Form, IDialogParent
	{
		private readonly int _captureWidth = 640;
		private readonly int _captureHeight = 480;

		public IDialogController DialogController { get; }

		private VideoWriterChooserForm(IMainFormForTools mainForm, IEmulator emulator, Config config)
		{
			DialogController = mainForm;

			InitializeComponent();

			// TODO: do we want to use virtual w/h?
			if (emulator.HasVideoProvider())
			{
				var videoProvider = emulator.AsVideoProvider();
				_captureWidth = videoProvider.BufferWidth;
				_captureHeight = videoProvider.BufferHeight;
			}

			if (config.AviCaptureOsd)
			{
				using var bb = mainForm.CaptureOSD();
				_captureWidth = bb.Width;
				_captureHeight = bb.Height;
			}

			lblSize.Text = $"Size:\r\n{_captureWidth}x{_captureHeight}";

			if (_captureWidth % 4 != 0 || _captureHeight % 4 != 0)
			{
				lblResolutionWarning.Visible = true;
			}
			else
			{
				lblResolutionWarning.Visible = false;
			}
		}

		/// <summary>
		/// chose an IVideoWriter
		/// </summary>
		/// <param name="list">list of IVideoWriters to choose from</param>
		/// <param name="owner">parent window</param>
		/// <param name="emulator">The current emulator</param>
		/// <returns>user choice, or null on Cancel\Close\invalid</returns>
		public static IVideoWriter DoVideoWriterChooserDlg<T>(
			IEnumerable<VideoWriterInfo> list,
			T owner,
			IEmulator emulator,
			Config config)
				where T : IMainFormForTools, IDialogParent
		{
			var dlg = new VideoWriterChooserForm(owner, emulator, config)
			{
				checkBoxASync = { Checked = config.VideoWriterAudioSyncEffective },
				checkBoxPad = { Checked = config.AVWriterPad },
				numericTextBoxH = { Text = Math.Max(0, config.AVWriterResizeHeight).ToString() },
				numericTextBoxW = { Text = Math.Max(0, config.AVWriterResizeWidth).ToString() },
				labelDescriptionBody = { Text = "" }
			};

			int idx = 0;
			int idxSelect = -1;
			dlg.listBox1.BeginUpdate();
			foreach (var vw in list)
			{
				dlg.listBox1.Items.Add(vw);
				if (vw.Attribs.ShortName == config.VideoWriter)
				{
					idxSelect = idx;
				}

				idx++;
			}

			dlg.listBox1.SelectedIndex = idxSelect;
			dlg.listBox1.EndUpdate();

			foreach (Control c in dlg.panelSizeSelect.Controls)
			{
				c.Enabled = false;
			}

			IVideoWriter ret = null;
			if (owner.ShowDialogAsChild(dlg).IsOk()
				&& dlg.listBox1.SelectedIndex is not -1)
			{
				var vwi = (VideoWriterInfo)dlg.listBox1.SelectedItem;
				ret = vwi.Create(owner);
				config.VideoWriter = vwi.Attribs.ShortName;
			}

			if (ret is not null)
			{
				(config.AVWriterResizeWidth, config.AVWriterResizeHeight) = dlg.checkBoxResize.Checked
					? (dlg.numericTextBoxW.IntValue, dlg.numericTextBoxH.IntValue)
					: (-1, -1);
				config.AVWriterPad = dlg.checkBoxPad.Checked;
				config.VideoWriterAudioSyncEffective = config.VideoWriterAudioSync = dlg.checkBoxASync.Checked;
			}
			dlg.Dispose();
			return ret;
		}

		private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			labelDescriptionBody.Text = listBox1.SelectedIndex != -1
				? ((VideoWriterInfo)listBox1.SelectedItem).Attribs.Description
				: "";
		}

		private void CheckBoxResize_CheckedChanged(object sender, EventArgs e)
		{
			foreach (Control c in panelSizeSelect.Controls)
			{
				c.Enabled = checkBoxResize.Checked;
			}
		}

		private void ButtonAuto_Click(object sender, EventArgs e)
		{
			numericTextBoxW.Text = _captureWidth.ToString();
			numericTextBoxH.Text = _captureHeight.ToString();
		}

		private void ButtonOK_Click(object sender, EventArgs e)
		{
			if (checkBoxResize.Checked)
			{
				try
				{
					if (numericTextBoxW.IntValue < 1 || numericTextBoxH.IntValue < 1)
					{
						this.ModalMessageBox("Size must be positive!");
						DialogResult = DialogResult.None;
					}
				}
				catch (FormatException)
				{
					this.ModalMessageBox("Size must be numeric!");
					DialogResult = DialogResult.None;
				}
			}
		}
	}
}
