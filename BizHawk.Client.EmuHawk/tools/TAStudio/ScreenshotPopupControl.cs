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

		public const int BorderWidth = 20;
		private TasBranch _branch = null;
		
		public TasBranch Branch
		{
			get { return _branch; }
			set
			{
				_branch = value;
				Size = new Size(Branch.OSDFrameBuffer.Width + (BorderWidth * 2), Branch.OSDFrameBuffer.Height + (BorderWidth * 2));
				Refresh();
			}
		}

		private void ScreenshotPopupControl_Load(object sender, EventArgs e)
		{

		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.DrawRectangle(new Pen(Brushes.Black), 0, 0, Width - 1, Height - 1);
			e.Graphics.DrawImage(Branch.OSDFrameBuffer.ToSysdrawingBitmap(), new Point(BorderWidth, BorderWidth));
			base.OnPaint(e);
		}
	}
}
