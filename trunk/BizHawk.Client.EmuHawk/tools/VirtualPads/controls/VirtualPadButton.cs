using System;
using System.Windows.Forms;
using System.Drawing;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class VirtualPadButton : CheckBox
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

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case 0x0204://WM_RBUTTONDOWN
					_rightClicked = true;
					ForeColor = Color.Red;
					Checked ^= true;
					return;
				case 0x0205://WM_RBUTTONUP
					return;
				case 0x0206://WM_RBUTTONDBLCLK
					return;
			}

			base.WndProc(ref m);
		}

		protected void SetSticky()
		{
			Global.StickyXORAdapter.SetSticky(Name, Checked);

			if (Checked == false)
			{
				Clear();
			}
		}

		protected void SetAutofireSticky()
		{
			Global.AutofireStickyXORAdapter.SetSticky(Name, Checked);

			if (Checked == false)
			{
				Clear();
			}
		}

		protected override void OnCheckedChanged(EventArgs e)
		{
			if (_rightClicked)
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
				_rightClicked = false;
				ForeColor = SystemColors.ControlText;
			}

			base.OnMouseClick(e);
		}

		public void Clear()
		{
			_rightClicked = false;
			ForeColor = SystemColors.ControlText;
			Checked = false;
			Global.AutofireStickyXORAdapter.SetSticky(Name, false);
			Global.StickyXORAdapter.SetSticky(Name, false);
		}
	}
}
