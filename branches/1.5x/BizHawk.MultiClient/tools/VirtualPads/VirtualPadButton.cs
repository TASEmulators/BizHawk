using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace BizHawk.MultiClient
{
	public class VirtualPadButton : CheckBox
	{
		public string ControllerButton = "";
		private bool _rightClicked = false;

		public VirtualPadButton()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);

			Appearance = System.Windows.Forms.Appearance.Button;

			ForeColor = Color.Black;
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
			Global.StickyXORAdapter.SetSticky(ControllerButton, Checked);

			if (Checked == false)
			{
				Clear();
			}
		}

		protected void SetAutofireSticky()
		{
			Global.AutofireStickyXORAdapter.SetSticky(ControllerButton, Checked);

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
				ForeColor = Color.Black;
			}
			base.OnMouseClick(e);
		}

		public void Clear()
		{
			_rightClicked = false;
			ForeColor = Color.Black;
			Checked = false;
			Global.AutofireStickyXORAdapter.SetSticky(ControllerButton, false);
			Global.StickyXORAdapter.SetSticky(ControllerButton, false);
		}
	}
}
