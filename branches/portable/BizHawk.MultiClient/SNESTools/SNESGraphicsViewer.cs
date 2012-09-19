using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.IO;
using System.Drawing.Imaging;
using BizHawk.Core;

namespace BizHawk.MultiClient
{
	public class SNESGraphicsViewer : RetainedViewportPanel
	{
		public SNESGraphicsViewer()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			this.BackColor = Color.Transparent;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Display(e.Graphics);
			base.OnPaint(e);
		}

		void Display(Graphics g)
		{
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
		}

		
		//todo - screenshot?
	}
}
