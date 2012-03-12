using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace BizHawk.MultiClient
{
	public partial class PCEBGCanvas : Control
	{
		const int BAT_WIDTH = 1024;
		const int BAT_HEIGHT = 512;
		public Bitmap bat;

		public PCEBGCanvas()
		{
			bat = new Bitmap(BAT_WIDTH, BAT_HEIGHT, PixelFormat.Format32bppArgb);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			//SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			this.Size = new Size(BAT_WIDTH, BAT_HEIGHT);
			//this.BackColor = Color.Transparent;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.BGViewer_Paint);
		}

		private void BGViewer_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawImageUnscaled(bat, 0, 0);
		}

		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// PCEBGCanvas
			// 
			this.Name = "PCEBGCanvas";
			this.ResumeLayout(false);

		}
	}
}
