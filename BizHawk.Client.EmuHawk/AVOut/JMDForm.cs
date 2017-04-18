using System;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// implements a minimal dialog for configuring JMDWriter
	/// </summary>
	public partial class JMDForm : Form
	{
		public JMDForm()
		{
			InitializeComponent();
		}

		private void okButton_Click(object sender, EventArgs e)
		{

		}

		private void cancelButton_Click(object sender, EventArgs e)
		{

		}

		private void threadsBar_Scroll(object sender, EventArgs e)
		{
			threadTop.Text = $"Number of compression threads: {threadsBar.Value}";
		}

		private void compressionBar_Scroll(object sender, EventArgs e)
		{
			compressionTop.Text = compressionBar.Value == compressionBar.Minimum
				? "Compression Level: NONE"
				: $"Compression Level: {compressionBar.Value}";
		}

		/// <summary>
		/// Show a configuration dialog (modal) for JMDWriter
		/// </summary>
		/// <param name="threads">number of threads</param>
		/// <param name="complevel">compression level</param>
		/// <param name="tmin">minimum possible number of threads</param>
		/// <param name="tmax">maximum possible number of threads</param>
		/// <param name="cmin">minimum compression level, assumed to be "no compression"</param>
		/// <param name="cmax">maximum compression level</param>
		/// <param name="hwnd">hwnd of parent</param>
		/// <returns>false if user canceled; true if user consented</returns>
		public static bool DoCompressionDlg(ref int threads, ref int complevel, int tmin, int tmax, int cmin, int cmax, IWin32Window hwnd)
		{
			JMDForm j = new JMDForm();
			j.threadsBar.Minimum = tmin;
			j.threadsBar.Maximum = tmax;
			j.compressionBar.Minimum = cmin;
			j.compressionBar.Maximum = cmax;
			j.threadsBar.Value = threads;
			j.compressionBar.Value = complevel;
			j.threadsBar_Scroll(null, null);
			j.compressionBar_Scroll(null, null);
			j.threadLeft.Text = $"{tmin}";
			j.threadRight.Text = $"{tmax}";
			j.compressionLeft.Text = $"{cmin}";
			j.compressionRight.Text = $"{cmax}";

			DialogResult d = j.ShowDialog(hwnd);

			threads = j.threadsBar.Value;
			complevel = j.compressionBar.Value;

			j.Dispose();
			if (d == DialogResult.OK)
			{
				return true;
			}
			
			return false;
		}


	}
}
