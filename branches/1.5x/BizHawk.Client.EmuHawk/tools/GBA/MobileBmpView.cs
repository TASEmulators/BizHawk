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

		public BmpView BmpView { get; private set; }

		[Browsable(false)]
		public bool ShouldDraw { get { return Visible; } }

		public override string ToString()
		{
			return Text;
		}

		public void ChangeViewSize(Size size)
		{
			BmpView.Size = ClientSize = size;
		}

		public void ChangeViewSize(int w, int h)
		{
			ChangeViewSize(new Size(w, h));
		}

		public void ChangeAllSizes(int w, int h)
		{
			ChangeViewSize(w, h);
			BmpView.ChangeBitmapSize(w, h);
		}

		public void ChangeAllSizes(Size size)
		{
			ChangeViewSize(size);
			BmpView.ChangeBitmapSize(size);
		}
	}
}
