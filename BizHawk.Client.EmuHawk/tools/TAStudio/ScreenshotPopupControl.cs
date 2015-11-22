using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class ScreenshotPopupControl : UserControl
	{
		public ScreenshotPopupControl()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			//SetStyle(ControlStyles.Opaque, true);
			//this.BackColor = Color.Transparent;
			
			InitializeComponent();
		}

		public TasBranch Branch { get; set; }

		private void ScreenshotPopupControl_Load(object sender, EventArgs e)
		{

		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var bitmap = Branch.OSDFrameBuffer.ToSysdrawingBitmap();
			e.Graphics.DrawImage(bitmap, new Rectangle(0, 0, Width, DrawingHeight));
			if (UserPadding > 0)
			{
				e.Graphics.DrawRectangle(new Pen(Brushes.Black), new Rectangle(new Point(0, DrawingHeight), new Size(Width - 1, UserPadding - 1)));
				e.Graphics.DrawString(UserText, _font, Brushes.Black, new Rectangle(1, DrawingHeight, Width - 1, Height));
			}
			base.OnPaint(e);
		}

		private void ScreenshotPopupControl_MouseLeave(object sender, EventArgs e)
		{
			Visible = false;
		}

		private void ScreenshotPopupControl_MouseHover(object sender, EventArgs e)
		{
			// todo: switch screenshots by hotkey
		}

		public void RecalculatePadding()
		{
			UserPadding = (int)Graphics.FromHwnd(this.Handle).MeasureString(UserText, _font, Width).Height;
			if (UserPadding > 0)
				UserPadding += 2;
			Height = DrawingHeight + UserPadding;
		}

		public int DrawingHeight = 0;
		public int UserPadding = 0;
		public string UserText;

		private Font _font = new Font(new FontFamily("Courier New"), 8);
	}
}
