using System.Windows.Forms;
using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public class VirtualPadButton : CheckBox, IVirtualPadControl
	{
		private bool _rightClicked;
		private bool _readonly;

		public InputManager InputManager;

		public VirtualPadButton()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);

			Appearance = Appearance.Button;
			AutoSize = true;
			ForeColor = SystemColors.ControlText;
		}

		public void Clear()
		{
			if (!ReadOnly)
			{
				RightClicked = false;
				Checked = false;
				//HOOMOO
				InputManager.StickyAutofireController.SetButtonAutofire(Name, false);
				InputManager.StickyHoldController.SetButtonHold(Name, false);
			}
		}

		public void UpdateValues()
		{
			// Nothing to do
			// This tool already draws as necessary
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
			get => _readonly;

			set
			{
				if (_readonly != value)
				{
					_readonly = value;
					RightClicked = false;
					if (!value)
					{
						Checked = false;
					}

					Refresh();
				}
			}
		}

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case 0x0204: // WM_RBUTTONDOWN
					if (!ReadOnly)
					{
						RightClicked = true;
						Checked = !Checked;
					}
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
			if (ReadOnly)
			{
				ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
										SystemColors.ControlDark, 1, ButtonBorderStyle.Inset,
										SystemColors.ControlDark, 1, ButtonBorderStyle.Inset,
										SystemColors.ControlDark, 1, ButtonBorderStyle.Inset,
										SystemColors.ControlDark, 1, ButtonBorderStyle.Inset);
			}
			else if (RightClicked)
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
			get => !ReadOnly && _rightClicked;

			set
			{
				if (!ReadOnly)
				{
					_rightClicked = value;
					ForeColor = _rightClicked ? SystemColors.HotTrack : SystemColors.ControlText;
				}
			}
		}

		protected override void OnCheckedChanged(EventArgs e)
		{
			if (RightClicked)
			{
				InputManager.StickyAutofireController.SetButtonAutofire(Name, Checked);
			}
			else
			{
				InputManager.StickyHoldController.SetButtonHold(Name, Checked);
			}

			if (!Checked)
			{
				Clear();
			}

			base.OnCheckedChanged(e);
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			if (!ReadOnly)
			{
				if (e.Button == MouseButtons.Left)
				{
					RightClicked = false;
				}

				base.OnMouseClick(e);
			}
		}

		protected override void OnClick(EventArgs e)
		{
			if (!ReadOnly)
			{
				base.OnClick(e);
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData) => true;
	}
}
