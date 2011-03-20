using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BizHawk.Core
{
	/// <summary>
	/// A programmatic PictureBox, really, which will paint itself using the last bitmap that was provided
	/// </summary>
	public class RetainedViewportPanel : Control
	{
		public RetainedViewportPanel()
		{
			CreateHandle();
			
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserMouse, true);
		}

		//Size logicalSize;
		////int pitch;
		//public void SetLogicalSize(int w, int h)
		//{
		//    if (bmp != null) bmp.Dispose();
		//    bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
		//    logicalSize = new Size(w, h);
		//}

		/// <summary>
		/// Takes ownership of the provided bitmap and will use it for future painting
		/// </summary>
		public void SetBitmap(Bitmap newbmp)
		{
			if (bmp != null) bmp.Dispose();
			bmp = newbmp;
			Refresh();
		}

		Bitmap bmp;

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (bmp != null)
			{
				e.Graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
				e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
				e.Graphics.CompositingMode = CompositingMode.SourceCopy;
				e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
				e.Graphics.DrawImage(bmp, 0, 0, Width, Height);
			}
		}

	}

	/// <summary>
	/// A dumb panel which functions as a placeholder for framebuffer painting
	/// </summary>
	public class ViewportPanel : Control
	{
		public ViewportPanel()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserMouse, true);
		}
	}

	/// <summary>
	/// A ViewportPanel with a vertical scroll bar
	/// </summary>
	public class ScrollableViewportPanel : UserControl
	{
		TableLayoutPanel table;
		ViewportPanel view;
		VScrollBar scroll;

		public ViewportPanel View { get { return view; } }
		public VScrollBar Scrollbar { get { return scroll; } }

		public int ScrollMax { get { return Scrollbar.Maximum; } set { Scrollbar.Maximum = value; } }
		public int ScrollLargeChange { get { return Scrollbar.LargeChange; } set { Scrollbar.LargeChange = value; } }

		public ScrollableViewportPanel()
		{
			InitializeComponent();
		}

		public void InitializeComponent() 
		{
			table = new TableLayoutPanel();
			view = new ViewportPanel();
			scroll = new VScrollBar();

			scroll.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
			view.Dock = DockStyle.Fill;

			table.Dock = DockStyle.Fill;
			table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
			table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, 0));
			table.RowCount = 1;
			table.ColumnCount = 2;
			table.Controls.Add(view);
			table.Controls.Add(scroll);
			table.SetColumn(view, 0);
			table.SetColumn(scroll, 1);

			scroll.Scroll += (sender, e) => OnScroll(e);
			view.Paint += (sender, e) => OnPaint(e);

			Controls.Add(table);
		}
	}
}