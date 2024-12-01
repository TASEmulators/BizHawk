using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class AnalogRangeConfig : Panel
	{
		private const int ScaleFactor = 4;
		private const int _3DPadding = 5;

		private readonly Pen _blackPen = Pens.Black;

		private readonly Pen _bluePen = Pens.Cyan;

		private int _maxX = 127;
		private int _maxY = 127;
		private bool _radial;

		public int MaxX
		{
			get => _maxX;
			set
			{
				_maxX = value;
				Refresh();
				Changed();
			}
		}

		public int MaxY
		{
			get => _maxY;
			set
			{
				_maxY = value;
				Refresh();
				Changed();
			}
		}

		public bool Radial
		{
			get => _radial;
			set
			{
				_radial = value;
				Refresh();
				Changed();
			}
		}

		private int ScaledX => MaxX / ScaleFactor;

		private int ScaledY => MaxY / ScaleFactor;

		private Point TopLeft
		{
			get
			{
				var centerX = Size.Width / 2;
				var centerY = Size.Height / 2;

				return new Point(centerX - ScaledX, centerY - ScaledY);
			}
		}

		public AnalogRangeConfig()
		{
			MaxX = 127;
			MaxY = 127;
			Size = new Size(65, 65);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			BackColor = Color.Gray;
			BorderStyle = BorderStyle.Fixed3D;

			InitializeComponent();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.FillRectangle(SystemBrushes.Control, 0, 0, Width, Height);
			e.Graphics.FillEllipse(Brushes.White, 0, 0, Width - _3DPadding, Height - _3DPadding);
			e.Graphics.DrawEllipse(_blackPen, 0, 0, Width - _3DPadding, Height - _3DPadding);

			if (Radial)
			{
				e.Graphics.DrawEllipse(
					_bluePen,
					TopLeft.X,
					TopLeft.Y,
					ScaledX * 2 - 4,
					ScaledY * 2 - 4);
			}
			else
			{
				e.Graphics.DrawRectangle(
					_bluePen,
					TopLeft.X,
					TopLeft.Y,
					ScaledX * 2 - 3,
					ScaledY * 2 - 3);
			}
			
			base.OnPaint(e);
		}

		private bool _isDragging;

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				_isDragging = true;
				DoDrag(e.X, e.Y);
			}
			else if (e.Button == MouseButtons.Right)
			{
				Radial = !Radial;
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				_isDragging = false;
			}

			base.OnMouseUp(e);
		}

		private void DoDrag(int x, int y)
		{
			if (_isDragging)
			{
				var centerX = Size.Width / 2;
				var centerY = Size.Height / 2;

				var offsetX = Math.Abs(centerX - x) * ScaleFactor;
				var offsetY = Math.Abs(centerY - y) * ScaleFactor;

				MaxX = Math.Min(offsetX, sbyte.MaxValue);
				MaxY = Math.Min(offsetY, sbyte.MaxValue);
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			DoDrag(e.X, e.Y);
			base.OnMouseMove(e);
		}

		public Action ChangeCallback { get; set; }

		private void Changed()
		{
			ChangeCallback?.Invoke();
		}
	}
}
