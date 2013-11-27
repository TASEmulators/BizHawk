using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class MobileDetailView : Form
	{
		public MobileDetailView()
		{
			InitializeComponent();
		}

		public BmpView BmpView { get; private set; }

		[Browsable(false)]
		public bool ShouldDraw { get { return Visible; } }

		public override string ToString()
		{
			return Text;
		}

		public void SetDetails(IList<Tuple<string, string>> details)
		{
			listView1.Items.Clear();
			foreach (var t in details)
			{
				listView1.Items.Add(new ListViewItem(new[] { t.Item1, t.Item2 }));
			}
		}

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
