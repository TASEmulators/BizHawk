using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class MobileDetailView : Form
	{
		public MobileDetailView()
		{
			InitializeComponent();
		}

		public BmpView BmpView => bmpView1;

		public override string ToString() => Text;

		private void MobileDetailView_SizeChanged(object sender, EventArgs e)
		{
			// bmp view is always square
			tableLayoutPanel1.RowStyles[0].Height = ClientSize.Width;
		}

		private void listView1_SizeChanged(object sender, EventArgs e)
		{
			listView1.Columns[0].Width = listView1.Width / 2;
			listView1.Columns[1].Width = listView1.Width / 2;
		}
	}
}
