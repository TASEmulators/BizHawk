using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class MessageConfig : Form
	{
		private MessageOption _fps = Global.Config.Fps.Clone();
		private MessageOption _frameCounter = Global.Config.FrameCounter.Clone();
		private MessageOption _lagCounter = Global.Config.LagCounter.Clone();
		private MessageOption _inputDisplay = Global.Config.InputDisplay.Clone();
		private MessageOption _reRecordCounter = Global.Config.ReRecordCounter.Clone();
		private MessageOption _multitrackRecorder = Global.Config.MultitrackRecorder.Clone();
		private MessageOption _messages = Global.Config.Messages.Clone();
		private MessageOption _autohold = Global.Config.Autohold.Clone();
		private MessageOption _ramWatches = Global.Config.RamWatches.Clone();

		private int _messageColor = Global.Config.MessagesColor;
		private int _alertColor = Global.Config.AlertMessageColor;
		private int _lastInputColor = Global.Config.LastInputColor;
		private int _movieInput = Global.Config.MovieInput;
		
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

		private void SetFromOption(MessageOption option)
		{
			XNumeric.Value = option.X;
			YNumeric.Value = option.Y;
			_px = option.X;
			_py = option.Y;
			SetAnchorRadio(option.Anchor);
		}

		private void SetPositionInfo()
		{
			if (FPSRadio.Checked)
			{
				SetFromOption(_fps);
			}
			else if (FrameCounterRadio.Checked)
			{
				SetFromOption(_frameCounter);
			}
			else if (LagCounterRadio.Checked)
			{
				SetFromOption(_lagCounter);
			}
			else if (InputDisplayRadio.Checked)
			{
				SetFromOption(_inputDisplay);
			}
			else if (WatchesRadio.Checked)
			{
				SetFromOption(_ramWatches);
			}
			else if (MessagesRadio.Checked)
			{
				SetFromOption(_messages);
			}
			else if (RerecordsRadio.Checked)
			{
				SetFromOption(_reRecordCounter);
			}
			else if (MultitrackRadio.Checked)
			{
				SetFromOption(_multitrackRecorder);
			}
			else if (AutoholdRadio.Checked)
			{
				SetFromOption(_autohold);
			}

			PositionPanel.Refresh();
			XNumeric.Refresh();
			YNumeric.Refresh();
			SetPositionLabels();
		}

		private void SaveSettings()
		{
			Global.Config.Fps = _fps;
			Global.Config.FrameCounter = _frameCounter;
			Global.Config.LagCounter = _lagCounter;
			Global.Config.InputDisplay = _inputDisplay;
			Global.Config.ReRecordCounter = _reRecordCounter;
			Global.Config.MultitrackRecorder = _multitrackRecorder;
			Global.Config.Messages = _messages;
			Global.Config.Autohold = _autohold;
			Global.Config.RamWatches = _ramWatches;

			Global.Config.MessagesColor = _messageColor;
			Global.Config.AlertMessageColor = _alertColor;
			Global.Config.LastInputColor = _lastInputColor;
			Global.Config.MovieInput = _movieInput;

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

		private void SetOptionPosition(MessageOption option)
		{
			option.X = _px;
			option.Y = _py;
		}

		private void SetPositionLabels()
		{
			if (FPSRadio.Checked)
			{
				SetOptionPosition(_fps);
			}
			else if (FrameCounterRadio.Checked)
			{
				SetOptionPosition(_frameCounter);
			}
			else if (LagCounterRadio.Checked)
			{
				SetOptionPosition(_lagCounter);
			}
			else if (InputDisplayRadio.Checked)
			{
				SetOptionPosition(_inputDisplay);
			}
			else if (WatchesRadio.Checked)
			{
				SetOptionPosition(_ramWatches);
			}
			else if (RerecordsRadio.Checked)
			{
				SetOptionPosition(_reRecordCounter);
			}
			else if (MultitrackRadio.Checked)
			{
				SetOptionPosition(_multitrackRecorder);
			}
			else if (MessagesRadio.Checked)
			{
				SetOptionPosition(_messages);
			}
			else if (AutoholdRadio.Checked)
			{
				SetOptionPosition(_autohold);
			}

			FpsPosLabel.Text = ToCoordinateStr(_fps);
			FCLabel.Text =  ToCoordinateStr(_frameCounter);
			LagLabel.Text =  ToCoordinateStr(_lagCounter);
			InpLabel.Text =  ToCoordinateStr(_inputDisplay);
			WatchesLabel.Text =  ToCoordinateStr(_ramWatches);
			RerecLabel.Text =  ToCoordinateStr(_reRecordCounter);
			MultitrackLabel.Text =  ToCoordinateStr(_multitrackRecorder);
			MessLabel.Text = ToCoordinateStr(_messages);
			AutoholdLabel.Text = ToCoordinateStr(_autohold);
		}

		private string ToCoordinateStr(MessageOption option)
		{
			return $"{option.X}, {option.Y}";
		}

		private void ResetDefaultsButton_Click(object sender, EventArgs e)
		{
			_fps = Global.Config.Fps = DefaultMessageOptions.Fps;
			_frameCounter = Global.Config.FrameCounter = DefaultMessageOptions.FrameCounter;
			_lagCounter = Global.Config.LagCounter = DefaultMessageOptions.LagCounter;
			_inputDisplay = Global.Config.InputDisplay = DefaultMessageOptions.InputDisplay;
			_reRecordCounter = Global.Config.ReRecordCounter = DefaultMessageOptions.ReRecordCounter;
			_multitrackRecorder = Global.Config.MultitrackRecorder = DefaultMessageOptions.MultitrackRecorder;
			_messages = Global.Config.Messages = DefaultMessageOptions.Messages;
			_autohold = Global.Config.Autohold = DefaultMessageOptions.Autohold;
			_ramWatches = Global.Config.RamWatches = DefaultMessageOptions.RamWatches;

			_messageColor = Global.Config.MessagesColor = DefaultMessageOptions.MessagesColor;
			_alertColor = Global.Config.AlertMessageColor = DefaultMessageOptions.AlertMessageColor;
			_lastInputColor = Global.Config.LastInputColor = DefaultMessageOptions.LastInputColor;
			_movieInput = Global.Config.MovieInput = DefaultMessageOptions.MovieInput;

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
				_fps.Anchor = value;
			}
			else if (FrameCounterRadio.Checked)
			{
				_frameCounter.Anchor = value;
			}
			else if (LagCounterRadio.Checked)
			{
				_lagCounter.Anchor = value;
			}
			else if (InputDisplayRadio.Checked)
			{
				_inputDisplay.Anchor = value;
			}
			else if (WatchesRadio.Checked)
			{
				_ramWatches.Anchor = value;
			}
			else if (MessagesRadio.Checked)
			{
				_messages.Anchor = value;
			}
			else if (RerecordsRadio.Checked)
			{
				_reRecordCounter.Anchor = value;
			}
			else if (MultitrackRadio.Checked)
			{
				_multitrackRecorder.Anchor = value;
			}
			else if (AutoholdRadio.Checked)
			{
				_autohold.Anchor = value;
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
			if (MessageColorDialog.ShowDialog().IsOk())
			{
				SetColorBox();
			}
		}

		private void AlertColorPanel_Click(object sender, EventArgs e)
		{
			if (AlertColorDialog.ShowDialog().IsOk())
			{
				SetColorBox();
			}
		}

		private void LInputColorPanel_Click(object sender, EventArgs e)
		{
			if (LInputColorDialog.ShowDialog().IsOk())
			{
				SetColorBox();
			}
		}

		private void MovieInputColor_Click(object sender, EventArgs e)
		{
			if (MovieInputColorDialog.ShowDialog().IsOk())
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
