using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public class SNESGraphicsViewer : RetainedViewportPanel
	{
		public SNESGraphicsViewer()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			if (!DesignMode)
			{
				BackColor = Color.Transparent;
			}
		}
	}
}
