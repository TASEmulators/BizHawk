using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace BizHawk.Client.EmuHawk
{
	public class MenuButton : Button
	{
		[DefaultValue(null)]
		public ContextMenuStrip Menu { get; set; }

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (Menu != null && e.Button == MouseButtons.Left)
			{
				Menu.Show(this, e.Location);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			int arrowX = ClientRectangle.Width - 14;
			int arrowY = ClientRectangle.Height / 2 - 1;

			Brush brush = Enabled ? SystemBrushes.ControlText : SystemBrushes.ButtonShadow;
			Point[] arrows = { new Point(arrowX, arrowY), new Point(arrowX + 7, arrowY), new Point(arrowX + 3, arrowY + 4) };
			e.Graphics.FillPolygon(brush, arrows);
		}
	}
}