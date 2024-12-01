using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// implements a minimal dialog for configuring JMDWriter
	/// </summary>
	public partial class JmdForm : Form
	{
		public JmdForm()
		{
			InitializeComponent();
		}

		private void ThreadsBar_Scroll(object sender, EventArgs e)
		{
			threadTop.Text = $"Number of compression threads: {threadsBar.Value}";
		}

		private void CompressionBar_Scroll(object sender, EventArgs e)
		{
			compressionTop.Text = compressionBar.Value == compressionBar.Minimum
				? "Compression Level: NONE"
				: $"Compression Level: {compressionBar.Value}";
		}

		/// <summary>
		/// Show a configuration dialog (modal) for JMDWriter
		/// </summary>
		/// <param name="threads">number of threads</param>
		/// <param name="compLevel">compression level</param>
		/// <param name="tMin">minimum possible number of threads</param>
		/// <param name="tMax">maximum possible number of threads</param>
		/// <param name="cMin">minimum compression level, assumed to be "no compression"</param>
		/// <param name="cMax">maximum compression level</param>
		/// <param name="hwnd">hwnd of parent</param>
		/// <returns>false if user canceled; true if user consented</returns>
		public static bool DoCompressionDlg(ref int threads, ref int compLevel, int tMin, int tMax, int cMin, int cMax, IWin32Window hwnd)
		{
			var j = new JmdForm
			{
				threadsBar = { Minimum = tMin, Maximum = tMax },
				compressionBar = { Minimum = cMin, Maximum = cMax }
			};

			j.threadsBar.Value = threads;
			j.compressionBar.Value = compLevel;
			j.ThreadsBar_Scroll(null, null);
			j.CompressionBar_Scroll(null, null);
			j.threadLeft.Text = $"{tMin}";
			j.threadRight.Text = $"{tMax}";
			j.compressionLeft.Text = $"{cMin}";
			j.compressionRight.Text = $"{cMax}";

			DialogResult d = j.ShowDialog(hwnd);

			threads = j.threadsBar.Value;
			compLevel = j.compressionBar.Value;

			j.Dispose();
			return d.IsOk();
		}
	}
}
