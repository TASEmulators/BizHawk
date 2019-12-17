using System;
using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// implements a simple dialog which chooses an IVideoWriter to record with
	/// </summary>
	public partial class VideoWriterChooserForm : Form
	{
		private VideoWriterChooserForm()
		{
			InitializeComponent();

			_captureWidth = Global.Emulator.CoreComm.NominalWidth;
			_captureHeight = Global.Emulator.CoreComm.NominalHeight;

			if (Global.Config.AVI_CaptureOSD)
			{
				using var bb = GlobalWin.MainForm.CaptureOSD();
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

		private readonly int _captureWidth, _captureHeight;

		/// <summary>
		/// chose an IVideoWriter
		/// </summary>
		/// <param name="list">list of IVideoWriters to choose from</param>
		/// <param name="owner">parent window</param>
		/// <returns>user choice, or null on Cancel\Close\invalid</returns>
		public static IVideoWriter DoVideoWriterChooserDlg(IEnumerable<VideoWriterInfo> list, IWin32Window owner, out int resizeW, out int resizeH, out bool pad, ref bool audioSync)
		{
			var dlg = new VideoWriterChooserForm
			{
				labelDescriptionBody = { Text = "" }
			};

			int idx = 0;
			int idxSelect = -1;
			dlg.listBox1.BeginUpdate();
			foreach (var vw in list)
			{
				dlg.listBox1.Items.Add(vw);
				if (vw.Attribs.ShortName == Global.Config.VideoWriter)
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

			dlg.checkBoxASync.Checked = audioSync;
			DialogResult result = dlg.ShowDialog(owner);

			IVideoWriter ret;

			if (result == DialogResult.OK && dlg.listBox1.SelectedIndex != -1)
			{
				var vwi = (VideoWriterInfo)dlg.listBox1.SelectedItem;
				ret = vwi.Create();
				Global.Config.VideoWriter = vwi.Attribs.ShortName;
			}
			else
			{
				ret = null;
			}

			if (ret != null && dlg.checkBoxResize.Checked)
			{
				resizeW = dlg.numericTextBoxW.IntValue;
				resizeH = dlg.numericTextBoxH.IntValue;
			}
			else
			{
				resizeW = -1;
				resizeH = -1;
			}

			pad = dlg.checkBoxPad.Checked;
			audioSync = dlg.checkBoxASync.Checked;

			dlg.Dispose();
			return ret;
		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			labelDescriptionBody.Text = listBox1.SelectedIndex != -1
				? ((VideoWriterInfo)listBox1.SelectedItem).Attribs.Description
				: "";
		}

		private void checkBoxResize_CheckedChanged(object sender, EventArgs e)
		{
			foreach (Control c in panelSizeSelect.Controls)
			{
				c.Enabled = checkBoxResize.Checked;
			}
		}

		private void buttonAuto_Click(object sender, EventArgs e)
		{
			numericTextBoxW.Text = _captureWidth.ToString();
			numericTextBoxH.Text = _captureHeight.ToString();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			if (checkBoxResize.Checked)
			{
				try
				{
					if (numericTextBoxW.IntValue < 1 || numericTextBoxH.IntValue < 1)
					{
						MessageBox.Show(this, "Size must be positive!");
						DialogResult = DialogResult.None;
					}
				}
				catch (FormatException)
				{
					MessageBox.Show(this, "Size must be numeric!");
					DialogResult = DialogResult.None;
				}
			}
		}
	}
}
