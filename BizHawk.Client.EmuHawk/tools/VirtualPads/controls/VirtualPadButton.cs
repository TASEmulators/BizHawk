using System;
using System.Windows.Forms;
using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public class VirtualPadButton : CheckBox, IVirtualPadControl
	{
		private bool _rightClicked = false;

		public VirtualPadButton()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);

			Appearance = Appearance.Button;
			AutoSize = true;
			ForeColor = SystemColors.ControlText;
		}

		#region IVirtualPadControl Implementation

		public void Clear()
		{
			RightClicked = false;
			Checked = false;
			Global.AutofireStickyXORAdapter.SetSticky(Name, false);
			Global.StickyXORAdapter.SetSticky(Name, false);
		}

		public void Set(IController controller)
		{
			var newVal = controller.IsPressed(Name);
			var changed = newVal != Checked;

			Checked = newVal;
			if (changed)
			{
				Refresh();
			}
		}

		public bool ReadOnly
		{
			get; set; // TODO
		}

		#endregion

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case 0x0204: // WM_RBUTTONDOWN
					RightClicked = true;
					Checked ^= true;
					return;
				case 0x0205: // WM_RBUTTONUP
					return;
				case 0x0206: // WM_RBUTTONDBLCLK
					return;
			}

			base.WndProc(ref m);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (RightClicked)
			{
				ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
										SystemColors.HotTrack, 1, ButtonBorderStyle.Inset,
										SystemColors.HotTrack, 1, ButtonBorderStyle.Inset,
										SystemColors.HotTrack, 1, ButtonBorderStyle.Inset,
										SystemColors.HotTrack, 1, ButtonBorderStyle.Inset);
			}
		}

		public bool RightClicked
		{
			get
			{
				return _rightClicked;
			}

			set
			{
				_rightClicked = value;
				if (_rightClicked)
				{
					ForeColor = SystemColors.HotTrack;
				}
				else
				{
					ForeColor = SystemColors.ControlText;
				}
			}
		}

		private void SetSticky()
		{
			Global.StickyXORAdapter.SetSticky(Name, Checked);

			if (Checked == false)
			{
				Clear();
			}
		}

		private void SetAutofireSticky()
		{
			Global.AutofireStickyXORAdapter.SetSticky(Name, Checked);

			if (Checked == false)
			{
				Clear();
			}
		}

		protected override void OnCheckedChanged(EventArgs e)
		{
			if (RightClicked)
			{
				SetAutofireSticky();
			}
			else
			{
				SetSticky();
			}

			base.OnCheckedChanged(e);
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				RightClicked = false;
			}

			base.OnMouseClick(e);
		}
	}
}
