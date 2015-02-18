using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// implements a simple dialog which chooses an IVideoWriter to record with
	/// </summary>
	public partial class VideoWriterChooserForm : Form
	{
		VideoWriterChooserForm()
		{
			InitializeComponent();

			CaptureWidth = Global.Emulator.CoreComm.NominalWidth;
			CaptureHeight = Global.Emulator.CoreComm.NominalHeight;

			if (Global.Config.AVI_CaptureOSD)
			{
				using (var bb = GlobalWin.MainForm.CaptureOSD())
				{
					CaptureWidth = bb.Width;
					CaptureHeight = bb.Height;
				}
			}

			lblSize.Text = string.Format("Size:\r\n{0}x{1}", CaptureWidth, CaptureHeight);

			if (CaptureWidth % 4 != 0 || CaptureHeight % 4 != 0)
				lblResolutionWarning.Visible = true;
			else lblResolutionWarning.Visible = false;
		}

		int CaptureWidth, CaptureHeight;

		/// <summary>
		/// chose an IVideoWriter
		/// </summary>
		/// <param name="list">list of IVideoWriters to choose from</param>
		/// <param name="owner">parent window</param>
		/// <returns>user choice, or null on Cancel\Close\invalid</returns>
		public static IVideoWriter DoVideoWriterChoserDlg(IEnumerable<VideoWriterInfo> list, IWin32Window owner, out int resizew, out int resizeh, out bool pad, out bool audiosync)
		{
			VideoWriterChooserForm dlg = new VideoWriterChooserForm();

			dlg.labelDescriptionBody.Text = "";

			{
				int idx = 0;
				int idx_select = -1;
				dlg.listBox1.BeginUpdate();
				foreach (var vw in list)
				{
					dlg.listBox1.Items.Add(vw);
					if (vw.Attribs.ShortName == Global.Config.VideoWriter)
						idx_select = idx;
					idx++;
				}
				dlg.listBox1.SelectedIndex = idx_select;
				dlg.listBox1.EndUpdate();
			}

			foreach (Control c in dlg.panelSizeSelect.Controls)
				c.Enabled = false;

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
				resizew = dlg.numericTextBoxW.IntValue;
				resizeh = dlg.numericTextBoxH.IntValue;
			}
			else
			{
				resizew = -1;
				resizeh = -1;
			}

			pad = dlg.checkBoxPad.Checked;
			audiosync = dlg.checkBoxASync.Checked;

			dlg.Dispose();
			return ret;
		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listBox1.SelectedIndex != -1)
				labelDescriptionBody.Text = ((VideoWriterInfo)listBox1.SelectedItem).Attribs.Description;
			else
				labelDescriptionBody.Text = "";
		}

		private void checkBoxResize_CheckedChanged(object sender, EventArgs e)
		{
			foreach (Control c in panelSizeSelect.Controls)
				c.Enabled = checkBoxResize.Checked;
		}

		private void buttonAuto_Click(object sender, EventArgs e)
		{
			numericTextBoxW.Text = CaptureWidth.ToString();
			numericTextBoxH.Text = CaptureHeight.ToString();
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
						return;
					}
				}
				catch (FormatException)
				{
					MessageBox.Show(this, "Size must be numeric!");
					DialogResult = DialogResult.None;
					return;
				}
			}
		}
	}
}
