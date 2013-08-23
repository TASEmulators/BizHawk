using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient.GBAtools
{
	public partial class MobileDetailView : Form
	{
		public MobileDetailView()
		{
			InitializeComponent();
		}

		public GBtools.BmpView bmpView { get { return bmpView1; } }

		[Browsable(false)]
		public bool ShouldDraw { get { return this.Visible; } }

		public override string ToString()
		{
			return Text;
		}

		public void SetDetails(IList<Tuple<string, string>> details)
		{
			listView1.Items.Clear();
			foreach (var t in details)
			{
				listView1.Items.Add(new ListViewItem(new string[] { t.Item1, t.Item2 }));
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
