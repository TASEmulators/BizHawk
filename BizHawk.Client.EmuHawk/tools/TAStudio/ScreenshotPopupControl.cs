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
			SetStyle(ControlStyles.Opaque, true);
			this.BackColor = Color.Transparent;
			
			InitializeComponent();
		}

		public TasBranch Branch { get; set; }

		private void ScreenshotPopupControl_Load(object sender, EventArgs e)
		{

		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var bitmap = Branch.OSDFrameBuffer.ToSysdrawingBitmap();
			e.Graphics.DrawImage(bitmap, new Rectangle(0, 0, Width, Height));
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
	}
}
