using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// implements a simple dialog which chooses an IVideoWriter to record with
	/// </summary>
	public partial class VideoWriterChooserForm : Form
	{
		VideoWriterChooserForm()
		{
			InitializeComponent();
		}

		/// <summary>
		/// chose an IVideoWriter
		/// </summary>
		/// <param name="list">list of IVideoWriters to choose from</param>
		/// <param name="owner">parent window</param>
		/// <returns>user choice, or null on Cancel\Close\invalid</returns>
		public static IVideoWriter DoVideoWriterChoserDlg(IEnumerable<IVideoWriter> list, IWin32Window owner, out int resizew, out int resizeh)
		{
			VideoWriterChooserForm dlg = new VideoWriterChooserForm();

			dlg.labelDescriptionBody.Text = "";

			dlg.listBox1.BeginUpdate();
			foreach (var vw in list)
				dlg.listBox1.Items.Add(vw);
			dlg.listBox1.EndUpdate();

			int i = dlg.listBox1.FindStringExact(Global.Config.VideoWriter);
			if (i != ListBox.NoMatches)
				dlg.listBox1.SelectedIndex = i;

			foreach (Control c in dlg.panelSizeSelect.Controls)
				c.Enabled = false;

			DialogResult result = dlg.ShowDialog(owner);

			IVideoWriter ret;

			if (result == DialogResult.OK && dlg.listBox1.SelectedIndex != -1)
			{
				ret = (IVideoWriter)dlg.listBox1.SelectedItem;
				Global.Config.VideoWriter = ret.ToString();
			}
			else
				ret = null;

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
			dlg.Dispose();
			return ret;
		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listBox1.SelectedIndex != -1)
				labelDescriptionBody.Text = ((IVideoWriter)listBox1.SelectedItem).WriterDescription();
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
			numericTextBoxW.Text = Global.Emulator.CoreComm.NominalWidth.ToString();
			numericTextBoxH.Text = Global.Emulator.CoreComm.NominalHeight.ToString();
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
