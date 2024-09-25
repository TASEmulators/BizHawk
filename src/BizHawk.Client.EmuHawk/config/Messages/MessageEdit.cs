using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MessageEdit : UserControl
	{
		private MessagePosition _messagePosition = new MessagePosition();
		private Action _changeCallback;
		private bool _programmaticallyChangingValues;
		private bool _mousedown;

		public MessageEdit()
		{
			InitializeComponent();
		}

		public void Bind(MessagePosition position, Action changeCallback)
		{
			_messagePosition = position;
			_changeCallback = changeCallback;

			_programmaticallyChangingValues = true;
			XNumeric.Value = position.X;
			YNumeric.Value = position.Y;

			switch (position.Anchor)
			{
				default:
				case MessagePosition.AnchorType.TopLeft:
					TL.Checked = true;
					break;
				case MessagePosition.AnchorType.TopRight:
					TR.Checked = true;
					break;
				case MessagePosition.AnchorType.BottomLeft:
					BL.Checked = true;
					break;
				case MessagePosition.AnchorType.BottomRight:
					BR.Checked = true;
					break;
			}

			_programmaticallyChangingValues = false;
			PositionGroupBox.Refresh();
		}

		private void TL_CheckedChanged(object sender, EventArgs e)
		{
			if (TL.Checked)
			{
				SetAnchor(MessagePosition.AnchorType.TopLeft);
			}

			PositionPanel.Refresh();
		}

		private void BL_CheckedChanged(object sender, EventArgs e)
		{
			if (BL.Checked)
			{
				SetAnchor(MessagePosition.AnchorType.BottomLeft);
			}

			PositionPanel.Refresh();
		}

		private void TR_CheckedChanged(object sender, EventArgs e)
		{
			if (TR.Checked)
			{
				SetAnchor(MessagePosition.AnchorType.TopRight);
			}

			PositionPanel.Refresh();
		}

		private void BR_CheckedChanged(object sender, EventArgs e)
		{
			if (BR.Checked)
			{
				SetAnchor(MessagePosition.AnchorType.BottomRight);
			}

			PositionPanel.Refresh();
		}

		private void XNumeric_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValues)
			{
				_messagePosition.X = (int)XNumeric.Value;
				PositionPanel.Refresh();
			}
		}

		private void YNumeric_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValues)
			{
				_messagePosition.Y = (int)YNumeric.Value;
				PositionPanel.Refresh();
			}
		}

		private void PositionPanel_MouseDown(object sender, MouseEventArgs e)
		{
			Cursor = Cursors.Arrow;
			_mousedown = true;
			SetPosition(e.X, e.Y);
		}

		private void PositionPanel_MouseEnter(object sender, EventArgs e)
		{
			Cursor = Cursors.Hand;
		}

		private void PositionPanel_MouseLeave(object sender, EventArgs e)
		{
			Cursor = Cursors.Default;
		}

		private void PositionPanel_MouseMove(object sender, MouseEventArgs e)
		{
			if (_mousedown)
			{
				SetPosition(e.X, e.Y);
			}
		}

		private void PositionPanel_MouseUp(object sender, MouseEventArgs e)
		{
			Cursor = Cursors.Hand;
			_mousedown = false;
		}

		private void PositionPanel_Paint(object sender, PaintEventArgs e)
		{
			int x = 0;
			int y = 0;

			switch (_messagePosition.Anchor)
			{
				case MessagePosition.AnchorType.TopLeft:
					x = _messagePosition.X;
					y = _messagePosition.Y;
					break;
				case MessagePosition.AnchorType.TopRight:
					x = (int)XNumeric.Maximum - _messagePosition.X;
					y = _messagePosition.Y;
					break;
				case MessagePosition.AnchorType.BottomLeft:
					x = _messagePosition.X;
					y = (int)YNumeric.Maximum - _messagePosition.Y;
					break;
				case MessagePosition.AnchorType.BottomRight:
					x = (int)XNumeric.Maximum - _messagePosition.X;
					y = (int)YNumeric.Maximum - _messagePosition.Y;
					break;
			}

			var p = Pens.Black;
			e.Graphics.DrawLine(p, new Point(x, y), new Point(x + 8, y + 8));
			e.Graphics.DrawLine(p, new Point(x + 8, y), new Point(x, y + 8));
			e.Graphics.DrawRectangle(p, new Rectangle(x, y, 8, 8));
		}

		private void SetAnchor(MessagePosition.AnchorType value)
		{
			_messagePosition.Anchor = value;
			_changeCallback?.Invoke();
		}

		private void SetPosition(int mx, int my)
		{
			_programmaticallyChangingValues = true;
			if (mx < 0) mx = 0;
			if (my < 0) my = 0;
			if (mx > XNumeric.Maximum) mx = (int)XNumeric.Maximum;
			if (my > YNumeric.Maximum) my = (int)YNumeric.Maximum;

			if (TL.Checked)
			{
				// Do nothing
			}
			else if (TR.Checked)
			{
				mx = (int)XNumeric.Maximum - mx;
			}
			else if (BL.Checked)
			{
				my = (int)YNumeric.Maximum - my;
			}
			else if (BR.Checked)
			{
				mx = (int)XNumeric.Maximum - mx;
				my = (int)YNumeric.Maximum - my;
			}

			XNumeric.Value = mx;
			YNumeric.Value = my;
			_messagePosition.X = mx;
			_messagePosition.Y = my;

			_changeCallback?.Invoke();
			PositionPanel.Refresh();
			_programmaticallyChangingValues = false;
		}
	}
}
