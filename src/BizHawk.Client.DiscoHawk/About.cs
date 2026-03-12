using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.Client.DiscoHawk
{
	public partial class About : Form
	{
		public About()
		{
			InitializeComponent();
			lblVersion.Text = $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
		}

		private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			Util.OpenUrlExternal(e.LinkText);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
