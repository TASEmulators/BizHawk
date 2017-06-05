using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;

// We pretend it's a tooltip kind of thing, so show only the actual contents
// and avoid stealing forcus, while still being topmost
// http://stackoverflow.com/a/25219399/2792852
// This is not an actual tooltip, because they can't reliably fade in and out with trasparency
namespace BizHawk.Client.EmuHawk
{
	public partial class ScreenshotForm : Form
	{
		// but still appear topmost
		private const int WS_EX_TOPMOST = 0x00000008;

		private const int WidthCap = 320;
		private const int HeightCap = 240;
		private const int Interval = 40;
		private const double AlphaStep = 0.125;

		private readonly Timer _showTimer = new Timer();
		private readonly Timer _hideTimer = new Timer();

		private TasBranch _branch;
		private int _drawingHeight;

		new public Font Font;
		new public int Padding;
		new public string Text;

		public ScreenshotForm()
		{
			InitializeComponent();

			Width = WidthCap;
			Height = HeightCap;
			var fontSize = 10;
			var fontStyle = FontStyle.Regular;
			Font = new Font(FontFamily.GenericMonospace, fontSize, fontStyle);
			_drawingHeight = 0;
			Padding = 0;
			Opacity = 0;

			_showTimer.Interval = Interval;
			_showTimer.Tick += (sender, e) =>
			{
				if ((Opacity += AlphaStep) >= 1)
				{
					_showTimer.Stop();
				}
			};

			_hideTimer.Interval = Interval;
			_hideTimer.Tick += (sender, e) =>
			{
				if ((Opacity -= AlphaStep) <= 0)
				{
					_hideTimer.Stop();
					Hide();
				}
			};
		}

		public void UpdateValues(TasBranch branch, Point location , int width, int height, int padding)
		{
			_branch = branch;
			Width = width;
			Padding = padding;
			_drawingHeight = height;
			Text = _branch.UserText;
			Location = location;

			// Set the screenshot to "1x" resolution of the core
			// cores like n64 and psx are going to still have sizes too big for the control, so cap them
			if (Width > WidthCap)
			{
				double ratio = WidthCap / (double)Width;
				Width = WidthCap;
				_drawingHeight = (int)((double)(_drawingHeight) * ratio);
			}

			if (Padding > 0)
			{
				Padding += 2;
			}

			Height = _drawingHeight + Padding;
			Refresh();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			_branch.OSDFrameBuffer.DiscardAlpha();
			var bitmap = _branch.OSDFrameBuffer.ToSysdrawingBitmap();
			e.Graphics.DrawImage(bitmap, new Rectangle(0, 0, Width, _drawingHeight));
			if (Padding > 0)
			{
				e.Graphics.DrawRectangle(new Pen(Brushes.Black), new Rectangle(new Point(0, _drawingHeight), new Size(Width - 1, Padding - 1)));
				e.Graphics.DrawString(Text, Font, Brushes.Black, new Rectangle(2, _drawingHeight, Width - 2, Height));
			}

			base.OnPaint(e);
		}

		public void FadeIn()
		{
			_showTimer.Stop();
			_hideTimer.Stop();
			_showTimer.Start();
			Show();
		}

		public void FadeOut()
		{
			_showTimer.Stop();
			_hideTimer.Stop();
			_hideTimer.Start();
		}

		// avoid stealing focus
		protected override bool ShowWithoutActivation => true;

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.ExStyle |= WS_EX_TOPMOST;
				return createParams;
			}
		}
	}
}
