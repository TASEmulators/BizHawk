using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public sealed class PatternViewer : Control
	{
		public Bitmap Pattern { get; set; }
		public int Pal0 { get; set; } = 0; // 0-7 Palette choice
		public int Pal1 { get; set; } = 0;

		public PatternViewer()
		{
			var pSize = new Size(256, 128);
			Pattern = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			Size = pSize;
			BackColor = Color.Transparent;
			Paint += PatternViewer_Paint;
		}

		private void PatternViewer_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawImage(Pattern, 0, 0);
		}
	}
}
