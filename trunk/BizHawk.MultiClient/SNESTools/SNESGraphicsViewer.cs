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
			if(!DesignMode)
				this.BackColor = Color.Transparent;
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
