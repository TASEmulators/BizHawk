using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace BizHawk.Client.EmuHawk
{
	public class MenuButton : Button
	{
		public MenuButton() { }

		[DefaultValue(null)]
		public ContextMenuStrip Menu { get; set; }

		protected override void OnMouseDown(MouseEventArgs mevent)
		{
			base.OnMouseDown(mevent);

			if (Menu != null && mevent.Button == MouseButtons.Left)
			{
				Menu.Show(this, mevent.Location);
			}
		}

		protected override void OnPaint(PaintEventArgs pevent)
		{
			base.OnPaint(pevent);

			int arrowX = ClientRectangle.Width - 14;
			int arrowY = ClientRectangle.Height / 2 - 1;

			Brush brush = Enabled ? SystemBrushes.ControlText : SystemBrushes.ButtonShadow;
			Point[] arrows = new Point[] { new Point(arrowX, arrowY), new Point(arrowX + 7, arrowY), new Point(arrowX + 3, arrowY + 4) };
			pevent.Graphics.FillPolygon(brush, arrows);
		}
	}
}