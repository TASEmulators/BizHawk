using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public sealed class AnalogControlPanel : Control
	{
		public double X;
		public double Y;

		public AnalogControlPanel()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			BackColor = Color.Transparent;
			Paint += AnalogControlPanel_Paint;
		}

		private void AnalogControlPanel_Paint(object sender, PaintEventArgs e)
		{
			
		}
	}
}
