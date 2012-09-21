using System;
using System.Collections.Generic;
using System.Threading;
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
		Thread threadPaint;
		EventWaitHandle ewh;
		volatile bool killSignal;

		public Func<Bitmap,bool> ReleaseCallback;

		/// <summary>
		/// Turns this panel into multi-threaded mode.
		/// This will sort of glitch out other gdi things on the system, but at least its fast...
		/// </summary>
		public void ActivateThreaded()
		{
			ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
			threadPaint = new Thread(PaintProc);
			threadPaint.IsBackground = true;
			threadPaint.Start();
		}

		public RetainedViewportPanel()
		{
			CreateHandle();
			
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, false);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserMouse, true);

			SetBitmap(new Bitmap(2, 2));
		}


		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (threadPaint != null)
			{
				killSignal = true;
				ewh.Set();
				ewh.WaitOne();
			}
			CleanupDisposeQueue();
		}

		void DoPaint()
		{
			if (bmp != null)
			{
				using (Graphics g = CreateGraphics())
				{
					g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
					g.InterpolationMode = InterpolationMode.NearestNeighbor;
					g.CompositingMode = CompositingMode.SourceCopy;
					g.CompositingQuality = CompositingQuality.HighSpeed;
					g.DrawImage(bmp, 0, 0, Width, Height);
				}
			}

			CleanupDisposeQueue();
		}

		void PaintProc()
		{
			for (; ; )
			{
				ewh.WaitOne();
				if (killSignal)
				{
					ewh.Set();
					return;
				}

				DoPaint();
			}
		}

		void CleanupDisposeQueue()
		{
			lock (this)
			{
				while (DisposeQueue.Count > 0)
				{
					var bmp = DisposeQueue.Dequeue();
					bool dispose = true;
					if(ReleaseCallback != null)
						dispose = ReleaseCallback(bmp);
					if(dispose) bmp.Dispose();
				}
			}
		}

		Queue<Bitmap> DisposeQueue = new Queue<Bitmap>();

		void SignalPaint()
		{
			if (threadPaint == null)
				DoPaint();
			else
				ewh.Set();
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
			lock (this)
			{
				if(bmp != null) DisposeQueue.Enqueue(bmp);
				bmp = newbmp;
			}
			SignalPaint();
		}

		Bitmap bmp;

		/// <summary>bit of a hack; use at your own risk</summary>
		/// <returns>you probably shouldn't modify this?</returns>
		public Bitmap GetBitmap()
		{
			return bmp;
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{

		}


		protected override void OnPaint(PaintEventArgs e)
		{
			SignalPaint();
			base.OnPaint(e);
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