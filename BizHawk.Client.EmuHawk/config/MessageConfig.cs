using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MessageConfig : Form
	{
		private readonly Config _config;

		private MessagePosition _fps;
		private MessagePosition _frameCounter;
		private MessagePosition _lagCounter;
		private MessagePosition _inputDisplay;
		private MessagePosition _reRecordCounter;
		private MessagePosition _multitrackRecorder;
		private MessagePosition _messages;
		private MessagePosition _autohold;
		private MessagePosition _ramWatches;

		private int _messageColor;
		private int _alertColor;
		private int _lastInputColor;
		private int _movieInput;
		
		private int _px;
		private int _py;
		private bool _mousedown;
		private bool _programmaticallyChangingValues;

		public MessageConfig(Config config)
		{
			_config = config;

			_fps = _config.Fps.Clone();
			_frameCounter = _config.FrameCounter.Clone();
			_lagCounter = _config.LagCounter.Clone();
			_inputDisplay = _config.InputDisplay.Clone();
			_reRecordCounter = _config.ReRecordCounter.Clone();
			_multitrackRecorder = _config.MultitrackRecorder.Clone();
			_messages = _config.Messages.Clone();
			_autohold = _config.Autohold.Clone();
			_ramWatches = _config.RamWatches.Clone();

			_messageColor = _config.MessagesColor;
			_alertColor = _config.AlertMessageColor;
			_lastInputColor = _config.LastInputColor;
			_movieInput = _config.MovieInput;

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
			StackMessagesCheckbox.Checked = _config.StackOSDMessages;
		}

		private void SetMaxXy()
		{
			var video = NullVideo.Instance; // Good enough
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

		private void SetFromOption(MessagePosition position)
		{
			_programmaticallyChangingValues = true;
			XNumeric.Value = position.X;
			YNumeric.Value = position.Y;
			_px = position.X;
			_py = position.Y;

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

		private void Ok_Click(object sender, EventArgs e)
		{
			_config.Fps = _fps;
			_config.FrameCounter = _frameCounter;
			_config.LagCounter = _lagCounter;
			_config.InputDisplay = _inputDisplay;
			_config.ReRecordCounter = _reRecordCounter;
			_config.MultitrackRecorder = _multitrackRecorder;
			_config.Messages = _messages;
			_config.Autohold = _autohold;
			_config.RamWatches = _ramWatches;

			_config.MessagesColor = _messageColor;
			_config.AlertMessageColor = _alertColor;
			_config.LastInputColor = _lastInputColor;
			_config.MovieInput = _movieInput;

			_config.StackOSDMessages = StackMessagesCheckbox.Checked;
			DialogResult = DialogResult.OK;
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

			using var p = new Pen(Color.Black);
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
			_px = mx;
			_py = my;

			PositionPanel.Refresh();
			SetPositionLabels();

			_programmaticallyChangingValues = false;
		}

		private void PositionPanel_MouseMove(object sender, MouseEventArgs e)
		{
			if (_mousedown)
			{
				SetNewPosition(e.X, e.Y);
			}
		}

		private void SetOptionPosition(MessagePosition position)
		{
			position.X = _px;
			position.Y = _py;
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

		private string ToCoordinateStr(MessagePosition position)
		{
			return $"{position.X}, {position.Y}";
		}

		private void ResetDefaultsButton_Click(object sender, EventArgs e)
		{
			_fps = _config.Fps = DefaultMessagePositions.Fps.Clone();
			_frameCounter = _config.FrameCounter = DefaultMessagePositions.FrameCounter.Clone();
			_lagCounter = _config.LagCounter = DefaultMessagePositions.LagCounter.Clone();
			_inputDisplay = _config.InputDisplay = DefaultMessagePositions.InputDisplay.Clone();
			_reRecordCounter = _config.ReRecordCounter = DefaultMessagePositions.ReRecordCounter.Clone();
			_multitrackRecorder = _config.MultitrackRecorder = DefaultMessagePositions.MultitrackRecorder.Clone();
			_messages = _config.Messages = DefaultMessagePositions.Messages.Clone();
			_autohold = _config.Autohold = DefaultMessagePositions.Autohold.Clone();
			_ramWatches = _config.RamWatches = DefaultMessagePositions.RamWatches.Clone();

			_messageColor = _config.MessagesColor = DefaultMessagePositions.MessagesColor;
			_alertColor = _config.AlertMessageColor = DefaultMessagePositions.AlertMessageColor;
			_lastInputColor = _config.LastInputColor = DefaultMessagePositions.LastInputColor;
			_movieInput = _config.MovieInput = DefaultMessagePositions.MovieInput;

			MessageColorDialog.Color = Color.FromArgb(_messageColor);
			AlertColorDialog.Color = Color.FromArgb(_alertColor);
			LInputColorDialog.Color = Color.FromArgb(_lastInputColor);
			MovieInputColorDialog.Color = Color.FromArgb(_movieInput);

			SetMaxXy();
			SetColorBox();
			SetPositionInfo();

			StackMessagesCheckbox.Checked = _config.StackOSDMessages = true;
		}

		private void SetAnchorValue(MessagePosition.AnchorType value)
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
				SetAnchorValue(MessagePosition.AnchorType.TopLeft);
			}

			PositionPanel.Refresh();
		}

		private void TR_CheckedChanged(object sender, EventArgs e)
		{
			if (TR.Checked)
			{
				SetAnchorValue(MessagePosition.AnchorType.TopRight);
			}

			PositionPanel.Refresh();
		}

		private void BL_CheckedChanged(object sender, EventArgs e)
		{
			if (BL.Checked)
			{
				SetAnchorValue(MessagePosition.AnchorType.BottomLeft);
			}

			PositionPanel.Refresh();
		}

		private void BR_CheckedChanged(object sender, EventArgs e)
		{
			if (BR.Checked)
			{
				SetAnchorValue(MessagePosition.AnchorType.BottomRight);
			}

			PositionPanel.Refresh();
		}

		private void XNumeric_Changed(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValues)
			{
				_px = (int)XNumeric.Value;
				SetPositionLabels();
				PositionPanel.Refresh();	
			}
		}

		private void YNumeric_Changed(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValues)
			{
				_py = (int)YNumeric.Value;
				SetPositionLabels();
				PositionPanel.Refresh();
			}
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
	}
}
