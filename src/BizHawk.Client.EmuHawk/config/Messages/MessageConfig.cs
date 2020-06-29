using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
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
		private MessagePosition _messages;
		private MessagePosition _autohold;
		private MessagePosition _ramWatches;

		private int _messageColor;
		private int _alertColor;
		private int _lastInputColor;
		private int _movieInput;
		
		private int _px;
		private int _py;

		public MessageConfig(Config config)
		{
			_config = config;

			_fps = _config.Fps.Clone();
			_frameCounter = _config.FrameCounter.Clone();
			_lagCounter = _config.LagCounter.Clone();
			_inputDisplay = _config.InputDisplay.Clone();
			_reRecordCounter = _config.ReRecordCounter.Clone();
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
			MessageEditor.SetMaxXy(video.BufferWidth - 12, video.BufferHeight - 12);
			MessageEditor.Size = new Size(video.BufferWidth + 2, video.BufferHeight + 2);
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

		private void SetFromOption(MessagePosition position, Label label)
		{
			MessageEditor.Bind(position, () => { label.Text = ToCoordinateStr(position); });
		}

		private void SetPositionInfo()
		{
			if (FPSRadio.Checked)
			{
				SetFromOption(_fps, FpsPosLabel);
			}
			else if (FrameCounterRadio.Checked)
			{
				SetFromOption(_frameCounter, FCLabel);
			}
			else if (LagCounterRadio.Checked)
			{
				SetFromOption(_lagCounter, LagLabel);
			}
			else if (InputDisplayRadio.Checked)
			{
				SetFromOption(_inputDisplay, InpLabel);
			}
			else if (WatchesRadio.Checked)
			{
				SetFromOption(_ramWatches, WatchesLabel);
			}
			else if (MessagesRadio.Checked)
			{
				SetFromOption(_messages, WatchesLabel);
			}
			else if (RerecordsRadio.Checked)
			{
				SetFromOption(_reRecordCounter, RerecLabel);
			}
			else if (AutoholdRadio.Checked)
			{
				SetFromOption(_autohold, AutoholdLabel);
			}

			// TODO: refresh
			SetPositionLabels();
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			_config.Fps = _fps;
			_config.FrameCounter = _frameCounter;
			_config.LagCounter = _lagCounter;
			_config.InputDisplay = _inputDisplay;
			_config.ReRecordCounter = _reRecordCounter;
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
