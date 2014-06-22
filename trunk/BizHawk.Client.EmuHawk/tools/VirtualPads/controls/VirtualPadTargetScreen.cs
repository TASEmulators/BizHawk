using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadTargetScreen : UserControl, IVirtualPadControl
	{
		private readonly Pen BlackPen = new Pen(Brushes.Black, 2);
		private readonly Pen WhitePen = new Pen(Brushes.White, 2);

		private bool _isProgrammicallyChangingNumerics = false;
		private bool _isDragging = false;

		public VirtualPadTargetScreen()
		{
			InitializeComponent();
		}

		private void VirtualPadTargetScreen_Load(object sender, EventArgs e)
		{
			XNumeric.Maximum = TargetPanel.Width;
			YNumeric.Maximum = TargetPanel.Height;
		}

		public string XName { get; set; }
		public string YName { get; set; }
		public string FireButton { get; set; } // Fire, Press, etc

		public int X
		{
			get
			{
				return (int)Global.StickyXORAdapter.GetFloat(XName);
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

				Global.StickyXORAdapter.SetFloat(XName, (int)XNumeric.Value);
			}
		}
		public int Y
		{
			get
			{
				return (int)Global.StickyXORAdapter.GetFloat(YName);
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

				Global.StickyXORAdapter.SetFloat(YName, (int)YNumeric.Value);
			}
		}

		public bool Fire
		{
			get
			{
				return Global.StickyXORAdapter.IsPressed(FireButton);
			}
		}

		public void Clear()
		{
			X = 0;
			Y = 0;
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
				Fire ? WhitePen : BlackPen,
				X - 10,
				Y - 10,
				21,
				21);

			e.Graphics.DrawLine(
				Fire ? WhitePen : BlackPen,
				new Point(X, Y - 10),
				new Point(X, Y + 10));

			e.Graphics.DrawLine(
				Fire ? WhitePen : BlackPen,
				new Point(X - 10, Y),
				new Point(X + 10, Y));

			e.Graphics.FillEllipse(
				Brushes.Red,
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
			if (e.Button == MouseButtons.Left)
			{
				_isDragging = true;
				X = e.X;
				Y = e.Y;
				TargetPanel.Refresh();
			}
		}

		private void TargetPanel_MouseMove(object sender, MouseEventArgs e)
		{
			if (_isDragging)
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
				_isProgrammicallyChangingNumerics = false;
				Global.StickyXORAdapter.SetSticky(XName, false);
				Global.StickyXORAdapter.SetSticky(YName, false);
				TargetPanel.Refresh();
			}
		}

		public override void Refresh()
		{
			X = X;
			Y = Y;

			base.Refresh();
		}
	}
}
