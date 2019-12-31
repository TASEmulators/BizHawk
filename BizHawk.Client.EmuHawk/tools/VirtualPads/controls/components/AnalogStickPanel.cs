using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class AnalogStickPanel : Panel
	{
		private int _x;
		private int _y;

		public int X
		{
			get => _x;
			set
			{
				_x = _rangeX.Constrain(value);
				SetAnalog();
			}
		}

		public int Y
		{
			get => _y;
			set
			{
				_y = _rangeY.Constrain(value);
				SetAnalog();
			}
		}

		public bool HasValue;
		public bool ReadOnly { private get; set; }

		public string XName = string.Empty;
		public string YName = string.Empty;

		private IController _previous;

		private sbyte _userRangePercentageX = 100;
		private sbyte _userRangePercentageY = 100;

		public void SetUserRange(decimal rx, decimal ry)
		{
			_userRangePercentageX = (sbyte) rx;
			_userRangePercentageY = (sbyte) ry;

			Rerange();
			Refresh();
		}

		public void SetRangeX(float[] range)
		{
			_actualRangeX.Min = (int) range[0];
			_actualRangeX.Max = (int) range[2];

			Rerange();
		}

		public void SetRangeY(float[] range)
		{
			_actualRangeY.Min = (int) range[0];
			_actualRangeY.Max = (int) range[2];

			Rerange();
		}

		private readonly MutableIntRange _rangeX = new MutableIntRange(-128, 127);
		private readonly MutableIntRange _rangeY = new MutableIntRange(-128, 127);
		private readonly MutableIntRange _actualRangeX = new MutableIntRange(-128, 127);
		private readonly MutableIntRange _actualRangeY = new MutableIntRange(-128, 127);

		private bool _reverseX;
		private bool _reverseY;

		private void Rerange()
		{
			_reverseX = _userRangePercentageX < 0;
			_reverseY = _userRangePercentageY < 0;

			var midX = (_actualRangeX.Min + _actualRangeX.Max) / 2.0;
			var halfRangeX = (_reverseX ? -1 : 1) * (_actualRangeX.Max - _actualRangeX.Min) * _userRangePercentageX / 200.0;
			_rangeX.Min = (int) (midX - halfRangeX);
			_rangeX.Max = (int) (midX + halfRangeX);

			var midY = (_actualRangeY.Min + _actualRangeY.Max) / 2.0;
			var halfRangeY = (_reverseY ? -1 : 1) * (_actualRangeY.Max - _actualRangeY.Min) * _userRangePercentageY / 200.0;
			_rangeY.Min = (int) (midY - halfRangeY);
			_rangeY.Max = (int) (midY + halfRangeY);
			
			// re-constrain after changing ranges
			X = X;
			Y = Y;
		}

		/// <remarks>
		/// never tested, assuming it works --zeromus
		/// </remarks>
		private const float ScaleX = 0.60f;
		/// <inheritdoc cref="ScaleX"/>
		private const float ScaleY = 0.60f;

		/// <remarks>
		/// min + (max - i) == max - (i - min) == min + max - i
		/// </remarks>
		private int MaybeReversedInX(int i) => _reverseX ? _rangeX.Min + _rangeX.Max - i : i;
		/// <inheritdoc cref="MaybeReversedInX"/>
		private int MaybeReversedInY(int i) => _reverseY ? _rangeY.Min + _rangeY.Max - i : i;

		private int PixelSizeX => (int)(_rangeX.GetCount() * ScaleX);
		private int PixelSizeY => (int)(_rangeY.GetCount() * ScaleY);
		private int PixelMinX => (Size.Width - PixelSizeX) / 2;
		private int PixelMinY => (Size.Height - PixelSizeY) / 2;
		private int PixelMidX => PixelMinX + PixelSizeX / 2;
		private int PixelMidY => PixelMinY + PixelSizeY / 2;
		private int PixelMaxX => PixelMinX + PixelSizeX - 1;
		private int PixelMaxY => PixelMinY + PixelSizeY - 1;

		private int RealToGfxX(int val) =>
			PixelMinX + ((MaybeReversedInX(_rangeX.Constrain(val)) - _rangeX.Min) * ScaleX).RoundToInt();

		private int RealToGfxY(int val) =>
			PixelMinY + ((MaybeReversedInY(_rangeY.Constrain(val)) - _rangeY.Min) * ScaleY).RoundToInt();

		private int GfxToRealX(int val) =>
			MaybeReversedInX(_rangeX.Constrain(_rangeX.Min + ((val - PixelMinX) / ScaleX).RoundToInt()));

		private int GfxToRealY(int val) =>
			MaybeReversedInY(_rangeY.Constrain(_rangeY.Min + ((val - PixelMinY) / ScaleY).RoundToInt()));

		private readonly Pen _blackPen = new Pen(Brushes.Black);
		private readonly Pen _bluePen = new Pen(Brushes.Blue, 2);
		private readonly Pen _grayPen = new Pen(Brushes.Gray, 2);

		private readonly Bitmap _dot = new Bitmap(7, 7);
		private readonly Bitmap _grayDot = new Bitmap(7, 7);

		public Action ClearCallback { private get; set; }

		private void DoClearCallback()
		{
			ClearCallback?.Invoke();
		}

		public AnalogStickPanel()
		{
			Size = new Size(PixelSizeX + 1, PixelSizeY + 1);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			BackColor = Color.Gray;
			Paint += AnalogControlPanel_Paint;
			BorderStyle = BorderStyle.Fixed3D;

			// Draw the dot into a bitmap
			using var g = Graphics.FromImage(_dot);
			g.Clear(Color.Transparent);
			var redBrush = Brushes.Red;
			g.FillRectangle(redBrush, 2, 0, 3, 7);
			g.FillRectangle(redBrush, 1, 1, 5, 5);
			g.FillRectangle(redBrush, 0, 2, 7, 3);

			using var gg = Graphics.FromImage(_grayDot);
			gg.Clear(Color.Transparent);
			gg.FillRectangle(Brushes.Gray, 2, 0, 3, 7);
			gg.FillRectangle(Brushes.Gray, 1, 1, 5, 5);
			gg.FillRectangle(Brushes.Gray, 0, 2, 7, 3);
		}

		private void SetAnalog()
		{
			Global.StickyXORAdapter.SetFloat(XName, HasValue ? X : (int?)null);
			Global.StickyXORAdapter.SetFloat(YName, HasValue ? Y : (int?)null);
			Refresh();
		}

		private void AnalogControlPanel_Paint(object sender, PaintEventArgs e)
		{
			unchecked
			{
				// Background
				e.Graphics.Clear(Color.LightGray);

				e.Graphics.FillRectangle(Brushes.LightGray, PixelMinX, PixelMinY, PixelMaxX - PixelMinX, PixelMaxY - PixelMinY);
				e.Graphics.FillEllipse(ReadOnly ? Brushes.Beige : Brushes.White, PixelMinX, PixelMinY, PixelMaxX - PixelMinX - 2, PixelMaxY - PixelMinY - 3);
				e.Graphics.DrawEllipse(_blackPen, PixelMinX, PixelMinY, PixelMaxX - PixelMinX - 2, PixelMaxY - PixelMinY - 3);
				e.Graphics.DrawLine(_blackPen, PixelMidX, 0, PixelMidX, PixelMaxY);
				e.Graphics.DrawLine(_blackPen, 0, PixelMidY, PixelMaxX, PixelMidY);

				// Previous frame
				if (_previous != null)
				{
					var pX = (int)_previous.GetFloat(XName);
					var pY = (int)_previous.GetFloat(YName);
					e.Graphics.DrawLine(_grayPen, PixelMidX, PixelMidY, RealToGfxX(pX), RealToGfxY(pY));
					e.Graphics.DrawImage(_grayDot, RealToGfxX(pX) - 3, RealToGfxY(_rangeY.Max) - RealToGfxY(pY) - 3);
				}

				// Line
				if (HasValue)
				{
					e.Graphics.DrawLine(_bluePen, PixelMidX, PixelMidY, RealToGfxX(X), RealToGfxY(Y));
					e.Graphics.DrawImage(ReadOnly ? _grayDot : _dot, RealToGfxX(X) - 3, RealToGfxY(Y) - 3);
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (ReadOnly) return;
			if (e.Button == MouseButtons.Left)
			{
				X = GfxToRealX(e.X);
				Y = GfxToRealY(e.Y);
				HasValue = true;
				SetAnalog();
			}
			else if (e.Button == MouseButtons.Right)
			{
				Clear();
			}
			Refresh();
			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			Capture = false;
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == 0x007B) // WM_CONTEXTMENU
			{
				// Don't let parent controls get this. We handle the right mouse button ourselves
				return;
			}

			base.WndProc(ref m);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (ReadOnly) return;
			if (e.Button == MouseButtons.Left)
			{
				X = GfxToRealX(e.X);
				Y = GfxToRealY(e.Y);
				HasValue = true;
			}
			if (e.Button == MouseButtons.Right)
			{
				Clear();
			}
			Refresh();
		}

		public void Clear()
		{
			if (!HasValue && X == 0 && Y == 0) return;
			X = Y = 0;
			HasValue = false;
			DoClearCallback();
			Refresh();
		}

		public void Set(IController controller)
		{
			var newX = (int) controller.GetFloat(XName);
			var newY = (int) controller.GetFloat(YName);
			if (newX != X || newY != Y) SetPosition(newX, newY);
		}

		public void SetPrevious(IController previous)
		{
			_previous = previous;
		}

		private void SetPosition(int xval, int yval)
		{
			X = xval;
			Y = yval;
			HasValue = true;
			Refresh();
		}
	}
}
