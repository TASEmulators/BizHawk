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
		public VideoWriterChooserForm()
		{
			InitializeComponent();
		}

		/// <summary>
		/// chose an IVideoWriter
		/// </summary>
		/// <param name="list">list of IVideoWriters to choose from</param>
		/// <param name="owner">parent window</param>
		/// <returns>user choice, or null on Cancel\Close\invalid</returns>
		public static IVideoWriter DoVideoWriterChoserDlg(IEnumerable<IVideoWriter> list, IWin32Window owner)
		{
			VideoWriterChooserForm dlg = new VideoWriterChooserForm();

			dlg.label1.Text = "Description:";
			dlg.label2.Text = "";

			dlg.listBox1.BeginUpdate();
			foreach (var vw in list)
				dlg.listBox1.Items.Add(vw);
			dlg.listBox1.EndUpdate();

			int i = dlg.listBox1.FindStringExact(Global.Config.VideoWriter);
			if (i != ListBox.NoMatches)
				dlg.listBox1.SelectedIndex = i;

			DialogResult result = dlg.ShowDialog(owner);

			IVideoWriter ret;

			if (result == DialogResult.OK && dlg.listBox1.SelectedIndex != -1)
			{
				ret = (IVideoWriter)dlg.listBox1.SelectedItem;
				Global.Config.VideoWriter = ret.ToString();
			}
			else
				ret = null;

			dlg.Dispose();
			return ret;
		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listBox1.SelectedIndex != -1)
				label2.Text = ((IVideoWriter)listBox1.SelectedItem).WriterDescription();
			else
				label2.Text = "";
		}
	}
}
