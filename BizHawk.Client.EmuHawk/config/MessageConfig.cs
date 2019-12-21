using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MessageConfig : Form
	{
		private int _dispFpSx = Global.Config.DispFPSx;
		private int _dispFpSy = Global.Config.DispFPSy;
		private int _dispFrameCx = Global.Config.DispFrameCx;
		private int _dispFrameCy = Global.Config.DispFrameCy;
		private int _dispLagX = Global.Config.DispLagx;
		private int _dispLagY = Global.Config.DispLagy;
		private int _dispInpX = Global.Config.DispInpx;
		private int _dispInpY = Global.Config.DispInpy;
		private int _dispWatchesX = Global.Config.DispRamWatchx;
		private int _dispWatchesY = Global.Config.DispRamWatchy;
		private int _lastInputColor = Global.Config.LastInputColor;
		private int _dispRecX = Global.Config.DispRecx;
		private int _dispRecY = Global.Config.DispRecy;
		private int _dispMultiX = Global.Config.DispMultix;
		private int _dispMultiY = Global.Config.DispMultiy;
		private int _dispMessageX = Global.Config.DispMessagex;
		private int _dispMessageY = Global.Config.DispMessagey;
		private int _dispAutoholdX = Global.Config.DispAutoholdx;
		private int _dispAutoholdY = Global.Config.DispAutoholdy;

		private int _messageColor = Global.Config.MessagesColor;
		private int _alertColor = Global.Config.AlertMessageColor;
		private int _movieInput = Global.Config.MovieInput;
		
		private int _dispFpsAnchor = Global.Config.DispFPSanchor;
		private int _dispFrameAnchor = Global.Config.DispFrameanchor;
		private int _dispLagAnchor = Global.Config.DispLaganchor;
		private int _dispInputAnchor = Global.Config.DispInpanchor;
		private int _dispWatchesAnchor = Global.Config.DispWatchesanchor;
		private int _dispRecAnchor = Global.Config.DispRecanchor;
		private int _dispMultiAnchor = Global.Config.DispMultianchor;
		private int _dispMessageAnchor = Global.Config.DispMessageanchor;
		private int _dispAutoholdAnchor = Global.Config.DispAutoholdanchor;

		private readonly Brush _brush = Brushes.Black;
		private int _px;
		private int _py;
		private bool _mousedown;

		public MessageConfig()
		{
			InitializeComponent();
		}

		private void MessageConfig_Load(object sender, EventArgs e)
		{
			SetMaxXy();
			MessageColorDialog.Color = Color.FromArgb(_messageColor);
			AlertColorDialog.Color = Color.FromArgb(_alertColor);
			LInputColorDialog.Color = Color.FromArgb(_lastInputColor);
			MovieInputColorDialog.Color = Color.FromArgb(_movieInput);
			SetColorBox();
			SetPositionInfo();
			StackMessagesCheckbox.Checked = Global.Config.StackOSDMessages;
		}

		private void SetMaxXy()
		{
			var video = Global.Emulator.AsVideoProvider(); // TODO: this is objectively wrong, these are core agnostic settings, why is the current core used here? Also this will crash on a core without a video provider
			XNumeric.Maximum = video.BufferWidth - 12;
			YNumeric.Maximum = video.BufferHeight - 12;
			PositionPanel.Size = new Size(video.BufferWidth + 2, video.BufferHeight + 2);

			PositionGroupBox.Size = new Size(
				Math.Max(video.BufferWidth, UIHelper.ScaleX(128)) + UIHelper.ScaleX(44),
				video.BufferHeight + UIHelper.ScaleY(52));
		}

		private void SetColorBox()
		{
			_messageColor = MessageColorDialog.Color.ToArgb();
			ColorPanel.BackColor = MessageColorDialog.Color;
			ColorText.Text = $"{_messageColor:X8}";

			_alertColor = AlertColorDialog.Color.ToArgb();
			AlertColorPanel.BackColor = AlertColorDialog.Color;
			AlertColorText.Text = $"{_alertColor:X8}";

			_lastInputColor = LInputColorDialog.Color.ToArgb();
			LInputColorPanel.BackColor = LInputColorDialog.Color;
			LInputText.Text = $"{_lastInputColor:X8}";

			_movieInput = MovieInputColorDialog.Color.ToArgb();
			MovieInputColor.BackColor = MovieInputColorDialog.Color;
			MovieInputText.Text = $"{_movieInput:X8}";
		}

		private void SetAnchorRadio(int anchor)
		{
			switch (anchor)
			{
				default:
				case 0:
					TL.Checked = true;
					break;
				case 1:
					TR.Checked = true;
					break;
				case 2:
					BL.Checked = true;
					break;
				case 3:
					BR.Checked = true;
					break;
			}
		}

		private void SetPositionInfo()
		{
			if (FPSRadio.Checked)
			{
				XNumeric.Value = _dispFpSx;
				YNumeric.Value = _dispFpSy;
				_px = _dispFpSx;
				_py = _dispFpSy;
				SetAnchorRadio(_dispFpsAnchor);
			}
			else if (FrameCounterRadio.Checked)
			{
				XNumeric.Value = _dispFrameCx;
				YNumeric.Value = _dispFrameCy;
				_px = _dispFrameCx;
				_py = _dispFrameCy;
				SetAnchorRadio(_dispFrameAnchor);
			}
			else if (LagCounterRadio.Checked)
			{
				XNumeric.Value = _dispLagX;
				YNumeric.Value = _dispLagY;
				_px = _dispLagX;
				_py = _dispLagY;
				SetAnchorRadio(_dispLagAnchor);
			}
			else if (InputDisplayRadio.Checked)
			{
				XNumeric.Value = _dispInpX;
				XNumeric.Value = _dispInpY;
				_px = _dispInpX;
				_py = _dispInpY;
				SetAnchorRadio(_dispInputAnchor);
			}
			else if (WatchesRadio.Checked)
			{
				XNumeric.Value = _dispWatchesX;
				XNumeric.Value = _dispWatchesY;
				_px = _dispWatchesX;
				_py = _dispWatchesY;
				SetAnchorRadio(_dispWatchesAnchor);
			}
			else if (MessagesRadio.Checked)
			{
				XNumeric.Value = _dispMessageX;
				YNumeric.Value = _dispMessageY;
				_px = _dispMessageX;
				_py = _dispMessageY;
				SetAnchorRadio(_dispMessageAnchor);
			}
			else if (RerecordsRadio.Checked)
			{
				XNumeric.Value = _dispRecX;
				YNumeric.Value = _dispRecY;
				_px = _dispRecX;
				_py = _dispRecY;
				SetAnchorRadio(_dispRecAnchor);
			}
			else if (MultitrackRadio.Checked)
			{
				XNumeric.Value = _dispMultiX;
				YNumeric.Value = _dispMultiY;
				_px = _dispMultiX;
				_py = _dispMultiY;
				SetAnchorRadio(_dispMultiAnchor);
			}
			else if (AutoholdRadio.Checked)
			{
				XNumeric.Value = _dispAutoholdX;
				YNumeric.Value = _dispAutoholdY;
				_px = _dispAutoholdX;
				_py = _dispAutoholdY;
				SetAnchorRadio(_dispAutoholdAnchor);
			}

			PositionPanel.Refresh();
			XNumeric.Refresh();
			YNumeric.Refresh();
			SetPositionLabels();
		}

		private void SaveSettings()
		{
			Global.Config.DispFPSx = _dispFpSx;
			Global.Config.DispFPSy = _dispFpSy;
			Global.Config.DispFrameCx = _dispFrameCx;
			Global.Config.DispFrameCy = _dispFrameCy;
			Global.Config.DispLagx = _dispLagX;
			Global.Config.DispLagy = _dispLagY;
			Global.Config.DispInpx = _dispInpX;
			Global.Config.DispInpy = _dispInpY;
			Global.Config.DispRamWatchx = _dispWatchesX;
			Global.Config.DispRamWatchy = _dispWatchesY;
			Global.Config.DispRecx = _dispRecX;
			Global.Config.DispRecy = _dispRecY;
			Global.Config.DispMultix = _dispMultiX;
			Global.Config.DispMultiy = _dispMultiY;
			Global.Config.DispMessagex = _dispMessageX;
			Global.Config.DispMessagey = _dispMessageY;
			Global.Config.DispAutoholdx = _dispAutoholdX;
			Global.Config.DispAutoholdy = _dispAutoholdY;

			Global.Config.MessagesColor = _messageColor;
			Global.Config.AlertMessageColor = _alertColor;
			Global.Config.LastInputColor = _lastInputColor;
			Global.Config.MovieInput = _movieInput;
			Global.Config.DispFPSanchor = _dispFpsAnchor;
			Global.Config.DispFrameanchor = _dispFrameAnchor;
			Global.Config.DispLaganchor = _dispLagAnchor;
			Global.Config.DispInpanchor = _dispInputAnchor;
			Global.Config.DispRecanchor = _dispRecAnchor;
			Global.Config.DispMultianchor = _dispMultiAnchor;
			Global.Config.DispMessageanchor = _dispMessageAnchor;
			Global.Config.DispAutoholdanchor = _dispAutoholdAnchor;

			Global.Config.StackOSDMessages = StackMessagesCheckbox.Checked;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			SaveSettings();
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void MessageTypeRadio_CheckedChanged(object sender, EventArgs e)
		{
			SetPositionInfo();
		}

		private void XNumericChange()
		{
			_px = (int)XNumeric.Value;
			SetPositionLabels();
			PositionPanel.Refresh();
		}

		private void YNumericChange()
		{
			_py = (int)YNumeric.Value;
			SetPositionLabels();
			PositionPanel.Refresh();
		}

		private void PositionPanel_MouseEnter(object sender, EventArgs e)
		{
			Cursor = Cursors.Hand;
		}

		private void PositionPanel_MouseLeave(object sender, EventArgs e)
		{
			Cursor = Cursors.Default;
		}

		private void PositionPanel_Paint(object sender, PaintEventArgs e)
		{
			int x = 0;
			int y = 0;

			if (TL.Checked)
			{
				x = _px;
				y = _py;
			}
			else if (TR.Checked)
			{
				x = (int)XNumeric.Maximum - _px;
				y = _py;
			}
			else if (BL.Checked)
			{
				x = _px;
				y = (int)YNumeric.Maximum - _py;
			}
			else if (BR.Checked)
			{
				x = (int)XNumeric.Maximum - _px;
				y = (int)YNumeric.Maximum - _py;
			}

			using var p = new Pen(_brush);
			e.Graphics.DrawLine(p, new Point(x, y), new Point(x + 8, y + 8));
			e.Graphics.DrawLine(p, new Point(x + 8, y), new Point(x, y + 8));
			e.Graphics.DrawRectangle(p, new Rectangle(x, y, 8, 8));
		}

		private void PositionPanel_MouseDown(object sender, MouseEventArgs e)
		{
			Cursor = Cursors.Arrow;
			_mousedown = true;
			SetNewPosition(e.X, e.Y);
		}

		private void PositionPanel_MouseUp(object sender, MouseEventArgs e)
		{
			Cursor = Cursors.Hand;
			_mousedown = false;
		}

		private void SetNewPosition(int mx, int my)
		{
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
			_px = mx;
			_py = my;

			PositionPanel.Refresh();
			SetPositionLabels();
		}

		private void PositionPanel_MouseMove(object sender, MouseEventArgs e)
		{
			if (_mousedown)
			{
				SetNewPosition(e.X, e.Y);
			}
		}

		private void SetPositionLabels()
		{
			if (FPSRadio.Checked)
			{
				_dispFpSx = _px;
				_dispFpSy = _py;
			}
			else if (FrameCounterRadio.Checked)
			{
				_dispFrameCx = _px;
				_dispFrameCy = _py;
			}
			else if (LagCounterRadio.Checked)
			{
				_dispLagX = _px;
				_dispLagY = _py;
			}
			else if (InputDisplayRadio.Checked)
			{
				_dispInpX = _px;
				_dispInpY = _py;
			}
			else if (WatchesRadio.Checked)
			{
				_dispWatchesX = _px;
				_dispWatchesY = _py;
			}
			else if (RerecordsRadio.Checked)
			{
				_dispRecX = _px;
				_dispRecY = _py;
			}
			else if (MultitrackRadio.Checked)
			{
				_dispMultiX = _px;
				_dispMultiY = _py;
			}
			else if (MessagesRadio.Checked)
			{
				_dispMessageX = _px;
				_dispMessageY = _py;
			}
			else if (AutoholdRadio.Checked)
			{
				_dispAutoholdX = _px;
				_dispAutoholdY = _py;
			}

			FpsPosLabel.Text = $"{_dispFpSx}, {_dispFpSy}";
			FCLabel.Text = $"{_dispFrameCx}, {_dispFrameCy}";
			LagLabel.Text = $"{_dispLagX}, {_dispLagY}";
			InpLabel.Text = $"{_dispInpX}, {_dispInpY}";
			WatchesLabel.Text = $"{_dispWatchesX}, {_dispWatchesY}";
			RerecLabel.Text = $"{_dispRecX}, {_dispRecY}";
			MultitrackLabel.Text = $"{_dispMultiX}, {_dispMultiY}";
			MessLabel.Text = $"{_dispMessageX}, {_dispMessageY}";
			AutoholdLabel.Text = $"{_dispAutoholdX}, {_dispAutoholdY}";
		}

		private void ResetDefaultsButton_Click(object sender, EventArgs e)
		{
			Global.Config.DispFPSx = DefaultMessageOptions.DispFPSx;
			Global.Config.DispFPSy = DefaultMessageOptions.DispFPSy;
			Global.Config.DispFrameCx = DefaultMessageOptions.DispFrameCx;
			Global.Config.DispFrameCy = DefaultMessageOptions.DispFrameCy;
			Global.Config.DispLagx = DefaultMessageOptions.DispLagx;
			Global.Config.DispLagy = DefaultMessageOptions.DispLagy;
			Global.Config.DispInpx = DefaultMessageOptions.DispInpx;
			Global.Config.DispInpy = DefaultMessageOptions.DispInpy;
			Global.Config.DispRecx = DefaultMessageOptions.DispRecx;
			Global.Config.DispRecy = DefaultMessageOptions.DispRecy;
			Global.Config.DispMultix = DefaultMessageOptions.DispMultix;
			Global.Config.DispMultiy = DefaultMessageOptions.DispMultiy;
			Global.Config.DispMessagex = DefaultMessageOptions.DispMessagex;
			Global.Config.DispMessagey = DefaultMessageOptions.DispMessagey;
			Global.Config.DispAutoholdx = DefaultMessageOptions.DispAutoholdx;
			Global.Config.DispAutoholdy = DefaultMessageOptions.DispAutoholdy;

			Global.Config.DispFPSanchor = DefaultMessageOptions.DispFPSanchor;
			Global.Config.DispFrameanchor = DefaultMessageOptions.DispFrameanchor;
			Global.Config.DispLaganchor = DefaultMessageOptions.DispLaganchor;
			Global.Config.DispInpanchor = DefaultMessageOptions.DispInpanchor;
			Global.Config.DispRecanchor = DefaultMessageOptions.DispRecanchor;
			Global.Config.DispMultianchor = DefaultMessageOptions.DispMultianchor;
			Global.Config.DispMessageanchor = DefaultMessageOptions.DispMessageanchor;
			Global.Config.DispAutoholdanchor = DefaultMessageOptions.DispAutoholdanchor;

			Global.Config.MessagesColor = DefaultMessageOptions.MessagesColor;
			Global.Config.AlertMessageColor = DefaultMessageOptions.AlertMessageColor;
			Global.Config.LastInputColor = DefaultMessageOptions.LastInputColor;
			Global.Config.MovieInput = DefaultMessageOptions.MovieInput;

			_dispFpSx = Global.Config.DispFPSx;
			_dispFpSy = Global.Config.DispFPSy;
			_dispFrameCx = Global.Config.DispFrameCx;
			_dispFrameCy = Global.Config.DispFrameCy;
			_dispLagX = Global.Config.DispLagx;
			_dispLagY = Global.Config.DispLagy;
			_dispInpX = Global.Config.DispInpx;
			_dispInpY = Global.Config.DispInpy;
			_dispRecX = Global.Config.DispRecx;
			_dispRecY = Global.Config.DispRecy;
			_dispMultiX = Global.Config.DispMultix;
			_dispMultiY = Global.Config.DispMultiy;
			_dispMessageX = Global.Config.DispMessagex;
			_dispMessageY = Global.Config.DispMessagey;
			_dispAutoholdX = Global.Config.DispAutoholdx;
			_dispAutoholdY = Global.Config.DispAutoholdy;

			_dispFpsAnchor = Global.Config.DispFPSanchor;
			_dispFrameAnchor = Global.Config.DispFrameanchor;
			_dispLagAnchor = Global.Config.DispLaganchor;
			_dispInputAnchor = Global.Config.DispInpanchor;
			_dispRecAnchor = Global.Config.DispRecanchor;
			_dispMultiAnchor = Global.Config.DispMultianchor;
			_dispMessageAnchor = Global.Config.DispMessageanchor;
			_dispAutoholdAnchor = Global.Config.DispAutoholdanchor;

			_messageColor = Global.Config.MessagesColor;
			_alertColor = Global.Config.AlertMessageColor;
			_lastInputColor = Global.Config.LastInputColor;
			_movieInput = Global.Config.MovieInput;

			MessageColorDialog.Color = Color.FromArgb(_messageColor);
			AlertColorDialog.Color = Color.FromArgb(_alertColor);
			LInputColorDialog.Color = Color.FromArgb(_lastInputColor);
			MovieInputColorDialog.Color = Color.FromArgb(_movieInput);

			SetMaxXy();
			SetColorBox();
			SetPositionInfo();

			StackMessagesCheckbox.Checked = Global.Config.StackOSDMessages = true;
		}

		private void SetAnchorValue(int value)
		{
			if (FPSRadio.Checked)
			{
				_dispFpsAnchor = value;
			}
			else if (FrameCounterRadio.Checked)
			{
				_dispFrameAnchor = value;
			}
			else if (LagCounterRadio.Checked)
			{
				_dispLagAnchor = value;
			}
			else if (InputDisplayRadio.Checked)
			{
				_dispInputAnchor = value;
			}
			else if (WatchesRadio.Checked)
			{
				_dispWatchesAnchor = value;
			}
			else if (MessagesRadio.Checked)
			{
				_dispMessageAnchor = value;
			}
			else if (RerecordsRadio.Checked)
			{
				_dispRecAnchor = value;
			}
			else if (MultitrackRadio.Checked)
			{
				_dispMultiAnchor = value;
			}
			else if (AutoholdRadio.Checked)
			{
				_dispAutoholdAnchor = value;
			}
		}

		private void TL_CheckedChanged(object sender, EventArgs e)
		{
			if (TL.Checked)
			{
				SetAnchorValue(0);
			}

			PositionPanel.Refresh();
		}

		private void TR_CheckedChanged(object sender, EventArgs e)
		{
			if (TR.Checked)
			{
				SetAnchorValue(1);
			}

			PositionPanel.Refresh();
		}

		private void BL_CheckedChanged(object sender, EventArgs e)
		{
			if (BL.Checked)
			{
				SetAnchorValue(2);
			}
			PositionPanel.Refresh();
		}

		private void BR_CheckedChanged(object sender, EventArgs e)
		{
			if (BR.Checked)
			{
				SetAnchorValue(3);
			}
			PositionPanel.Refresh();
		}

		private void XNumeric_Click(object sender, EventArgs e)
		{
			XNumericChange();
		}

		private void YNumeric_Click(object sender, EventArgs e)
		{
			YNumericChange();
		}

		private void ColorPanel_Click(object sender, EventArgs e)
		{
			if (MessageColorDialog.ShowDialog() == DialogResult.OK)
			{
				SetColorBox();
			}
		}

		private void AlertColorPanel_Click(object sender, EventArgs e)
		{
			if (AlertColorDialog.ShowDialog() == DialogResult.OK)
			{
				SetColorBox();
			}
		}

		private void LInputColorPanel_Click(object sender, EventArgs e)
		{
			if (LInputColorDialog.ShowDialog() == DialogResult.OK)
			{
				SetColorBox();
			}
		}

		private void MovieInputColor_Click(object sender, EventArgs e)
		{
			if (MovieInputColorDialog.ShowDialog() == DialogResult.OK)
			{
				SetColorBox();
			}
		}

		private void XNumeric_KeyUp(object sender, KeyEventArgs e)
		{
			XNumericChange();
		}

		private void YNumeric_KeyUp(object sender, KeyEventArgs e)
		{
			YNumericChange();
		}
	}
}
