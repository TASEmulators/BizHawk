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
		public TasBranch Branch { get; set; }
		public int DrawingHeight = 0;
		public int UserPadding = 0;
		public int UserFontSize = 10; // because why not?
		public FontStyle UserFontStyle = FontStyle.Regular;
		public string UserText;
		private Font UserFont;

		public ScreenshotPopupControl()
		{
			//SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			//SetStyle(ControlStyles.Opaque, true);
			//this.BackColor = Color.Transparent;
			
			InitializeComponent();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Branch.OSDFrameBuffer.DiscardAlpha();
			var bitmap = Branch.OSDFrameBuffer.ToSysdrawingBitmap();
			e.Graphics.DrawImage(bitmap, new Rectangle(0, 0, Width, DrawingHeight));
			if (UserPadding > 0)
			{
				e.Graphics.DrawRectangle(new Pen(Brushes.Black), new Rectangle(new Point(0, DrawingHeight), new Size(Width - 1, UserPadding - 1)));
				e.Graphics.DrawString(UserText, UserFont, Brushes.Black, new Rectangle(2, DrawingHeight, Width - 2, Height));
			}
			base.OnPaint(e);
		}

		public void SetFontStyle(FontStyle style)
		{
			UserFontStyle = style;
		}

		public void SetFontSize(int size)
		{
			UserFontSize = size;
		}

		public void RecalculateHeight()
		{
			UserFont = new Font(FontFamily.GenericMonospace, UserFontSize, UserFontStyle);
			UserText = Branch.UserText;
			UserPadding = (int)Graphics.FromHwnd(this.Handle).MeasureString(UserText, UserFont, Width).Height;
			if (UserPadding > 0)
				UserPadding += 2;
			Height = DrawingHeight + UserPadding;
		}

		private void ScreenshotPopupControl_MouseLeave(object sender, EventArgs e)
		{
			Visible = false;
		}

		private void ScreenshotPopupControl_MouseHover(object sender, EventArgs e)
		{
			// todo: switch screenshots by hotkey
		}

		private void ScreenshotPopupControl_Load(object sender, EventArgs e)
		{

		}
	}
}
