using System.Collections.Generic;
using System.Threading;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A programmatic PictureBox, really, which will paint itself using the last bitmap that was provided
	/// </summary>
	public class RetainedViewportPanel : Control
	{
		private Thread _threadPaint;
		private EventWaitHandle _ewh;
		private volatile bool _killSignal;

		public Func<Bitmap,bool> ReleaseCallback;

		/// <summary>
		/// Turns this panel into multi-threaded mode.
		/// This will sort of glitch out other gdi things on the system, but at least its fast...
		/// </summary>
		public void ActivateThreaded()
		{
			_ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
			_threadPaint = new Thread(PaintProc) { IsBackground = true };
			_threadPaint.Start();
		}

		public RetainedViewportPanel(bool doubleBuffer = false)
		{
			CreateHandle();
			
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, doubleBuffer);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserMouse, true);

			SetBitmap(new Bitmap(2, 2));
		}


		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (_threadPaint != null)
			{
				_killSignal = true;
				_ewh.Set();
				_ewh.WaitOne();
			}
			CleanupDisposeQueue();
		}

		public bool ScaleImage = true;

		private void DoPaint()
		{
			if (_bmp != null)
			{
				using Graphics g = CreateGraphics();
				g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.CompositingMode = CompositingMode.SourceCopy;
				g.CompositingQuality = CompositingQuality.HighSpeed;
				if (ScaleImage)
				{
					g.InterpolationMode = InterpolationMode.NearestNeighbor;
					g.PixelOffsetMode = PixelOffsetMode.Half;
					g.DrawImage(_bmp, 0, 0, Width, Height);
				}
				else
				{
					{
						var sb = Brushes.Black;
						g.FillRectangle(sb, _bmp.Width, 0, Width - _bmp.Width, Height);
						g.FillRectangle(sb, 0, _bmp.Height, _bmp.Width, Height - _bmp.Height);
					}
					g.DrawImageUnscaled(_bmp, 0, 0);
				}
			}

			CleanupDisposeQueue();
		}

		private void PaintProc()
		{
			for (; ; )
			{
				_ewh.WaitOne();
				if (_killSignal)
				{
					_ewh.Set();
					return;
				}

				DoPaint();
			}
		}

		private void CleanupDisposeQueue()
		{
			lock (this)
			{
				while (_disposeQueue.Count > 0)
				{
					var bmp = _disposeQueue.Dequeue();
					bool dispose = true;
					if(ReleaseCallback != null)
						dispose = ReleaseCallback(bmp);
					if(dispose) bmp.Dispose();
				}
			}
		}

		private readonly Queue<Bitmap> _disposeQueue = new Queue<Bitmap>();

		private void SignalPaint()
		{
			if (_threadPaint == null)
				DoPaint();
			else
				_ewh.Set();
		}

		/// <summary>
		/// Takes ownership of the provided bitmap and will use it for future painting
		/// </summary>
		public void SetBitmap(Bitmap newBmp)
		{
			lock (this)
			{
				if(_bmp != null) _disposeQueue.Enqueue(_bmp);
				_bmp = newBmp;
			}
			SignalPaint();
		}

		private Bitmap _bmp;

		/// <summary>bit of a hack; use at your own risk</summary>
		/// <returns>you probably shouldn't modify this?</returns>
		public Bitmap GetBitmap()
		{
			return _bmp;
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
}