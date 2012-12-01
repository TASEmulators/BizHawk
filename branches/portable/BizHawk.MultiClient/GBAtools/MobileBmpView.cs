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
	public partial class MobileBmpView : Form
	{
		public MobileBmpView()
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

		public void ChangeViewSize(Size size)
		{
			bmpView1.Size = size;
			this.ClientSize = size;
		}
		public void ChangeViewSize(int w, int h)
		{
			ChangeViewSize(new Size(w, h));
		}
		public void ChangeAllSizes(int w, int h)
		{
			ChangeViewSize(w, h);
			bmpView1.ChangeBitmapSize(w, h);
		}
		public void ChangeAllSizes(Size size)
		{
			ChangeViewSize(size);
			bmpView1.ChangeBitmapSize(size);
		}
	}
}
