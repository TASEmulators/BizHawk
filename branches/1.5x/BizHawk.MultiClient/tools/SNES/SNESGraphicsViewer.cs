using System.Drawing;
using System.Windows.Forms;
using BizHawk.Core;

namespace BizHawk.MultiClient
{
	public class SNESGraphicsViewer : RetainedViewportPanel
	{
		public SNESGraphicsViewer()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			if(!DesignMode)
				BackColor = Color.Transparent;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}
}
