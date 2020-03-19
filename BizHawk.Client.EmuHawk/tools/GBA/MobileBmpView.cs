using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class MobileBmpView : Form
	{
		public MobileBmpView()
		{
			InitializeComponent();
		}

		public BmpView BmpView => bmpView1;

		[Browsable(false)]
		public bool ShouldDraw => Visible;

		public override string ToString() => Text;

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
	}
}
