using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadTargetScreen : UserControl, IVirtualPadControl
	{
		private readonly Pen BlackPen = new Pen(Brushes.Black, 2);
		private readonly Pen WhitePen = new Pen(Brushes.White, 2);
		private readonly Pen GrayPen = new Pen(Brushes.Gray, 2);
		private readonly Pen RedPen = new Pen(Brushes.Red, 2);

		private bool _isProgrammicallyChangingNumerics;
		private bool _isDragging;
		private bool _readonly;
		private bool _isSet; // The tool has to keep track of this because there is currently no way to know if a float button is being autoheld or just held
		
		private int? _overrideX = null;
		private int? _overrideY = null;

		public VirtualPadTargetScreen()
		{
			InitializeComponent();
		}

		private void VirtualPadTargetScreen_Load(object sender, EventArgs e)
		{
			XNumeric.Maximum = TargetPanel.Width - 1;
			YNumeric.Maximum = TargetPanel.Height - 1;
		}

		#region IVirtualPadControl Implementation

		public void UpdateValues()
		{
			TargetPanel.Refresh();
		}

		public void Clear()
		{
			Global.StickyXORAdapter.Unset(XName);
			Global.StickyXORAdapter.Unset(YName);
			_overrideX = null;
			_overrideY = null;
			_isSet = false;
			Refresh();
		}

		public void Set(IController controller)
		{
			var newX = controller.GetFloat(XName) / MultiplierX;
			var newY = controller.GetFloat(YName) / MultiplierY;

			var oldX = X / MultiplierX;
			var oldY = Y / MultiplierY;

			var changed = newX != oldX && newY != oldY;

			XNumeric.Value = (int)newX;
			YNumeric.Value = (int)newY;

			_overrideX = (int)newX;
			_overrideY = (int)newY;

			if (changed)
			{
				Refresh();
			}

			_isSet = true;
		}

		public bool ReadOnly
		{
			get
			{
				return _readonly;
			}

			set
			{
				if (_readonly != value)
				{
					XNumeric.Enabled =
						XLabel.Enabled =
						YNumeric.Enabled =
						YLabel.Enabled =
						!value;

					_readonly = value;
					Refresh();
				}

				if (!_readonly)
				{
					_overrideX = null;
					_overrideY = null;
				}
			}
		}

		#endregion

		// Size of the extra controls to the right / bottom of the target panel at 96 DPI
		private Size PaddingSize
		{
			get { return new Size(0, 30); }
		}

		public Size TargetSize
		{
			get { return TargetPanel.Size; }
			set
			{
				TargetPanel.Size = value;
				Size = UIHelper.Scale(value + PaddingSize);
			}
		}

		// These are the value that a maximum x or y actually represent, used to translate from control X,Y to values the core expects
		public int RangeX { get; set; }
		public int RangeY { get; set; }

		public float MultiplierX
		{
			get
			{
				if (RangeX > 0)
				{
					return RangeX / (float)TargetPanel.Width;
				}

				return 1;
			}
		}

		public float MultiplierY
		{
			get
			{
				if (RangeY > 0)
				{
					return RangeY / (float)TargetPanel.Height;
				}

				return 1;
			}
		}

		public void Bump(int? x, int? y)
		{
			if (x.HasValue)
			{
				X = X + x.Value;
			}

			if (y.HasValue)
			{
				Y = Y + y.Value;
			}

			Refresh();
		}

		public string XName { get; set; }
		public string YName { get; set; }

		public int X
		{
			get
			{
				return _overrideX ?? (int)(Global.StickyXORAdapter.GetFloat(XName) / MultiplierX);
			}

			set
			{
				if (value < 0)
				{
					XNumeric.Value = 0;
					
				}
				else if (value <= XNumeric.Maximum)
				{
					XNumeric.Value = value;
				}
				else
				{
					XNumeric.Value = XNumeric.Maximum;
				}

				Global.StickyXORAdapter.SetFloat(XName, (int)((float)XNumeric.Value * MultiplierX));
				_isSet = true;
			}
		}
		public int Y
		{
			get
			{
				return _overrideY ?? (int)(Global.StickyXORAdapter.GetFloat(YName) / MultiplierY);
			}

			set
			{
				if (value < 0)
				{
					YNumeric.Value = 0;
				}
				else if (value <= YNumeric.Maximum)
				{
					YNumeric.Value = value;
				}
				else
				{
					YNumeric.Value = YNumeric.Maximum;
				}

				Global.StickyXORAdapter.SetFloat(YName, (int)((float)YNumeric.Value * MultiplierY));
				_isSet = true;
			}
		}

		private void UpdatePanelFromNumeric()
		{
			if (!_isProgrammicallyChangingNumerics)
			{
				TargetPanel.Refresh();
			}
		}

		private void TargetPanel_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawEllipse(
				ReadOnly ? GrayPen : _isSet ? RedPen : BlackPen,
				X - 10,
				Y - 10,
				21,
				21);

			e.Graphics.DrawLine(
				ReadOnly ? GrayPen : BlackPen,
				new Point(X, Y - 10),
				new Point(X, Y + 10));

			e.Graphics.DrawLine(
				ReadOnly ? GrayPen : BlackPen,
				new Point(X - 10, Y),
				new Point(X + 10, Y));

			e.Graphics.FillEllipse(
				ReadOnly ? Brushes.Gray : Brushes.Red,
				new Rectangle(X - 2, Y - 2, 4, 4));
		}

		private void XNumeric_ValueChanged(object sender, EventArgs e)
		{
			UpdatePanelFromNumeric();
		}

		private void XNumeric_KeyUp(object sender, KeyEventArgs e)
		{
			UpdatePanelFromNumeric();
		}

		private void YNumeric_ValueChanged(object sender, EventArgs e)
		{
			UpdatePanelFromNumeric();
		}

		private void YNumeric_KeyUp(object sender, KeyEventArgs e)
		{
			UpdatePanelFromNumeric();
		}

		private void TargetPanel_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && !ReadOnly)
			{
				_isDragging = true;
				X = e.X;
				Y = e.Y;
				TargetPanel.Refresh();
			}
		}

		private void TargetPanel_MouseMove(object sender, MouseEventArgs e)
		{
			if (_isDragging && !ReadOnly)
			{
				_isProgrammicallyChangingNumerics = true;
				X = e.X;
				Y = e.Y;
				_isProgrammicallyChangingNumerics = false;
				TargetPanel.Refresh();
			}
		}

		private void TargetPanel_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				_isDragging = false;
				TargetPanel.Refresh();
			}
		}
	}
}
