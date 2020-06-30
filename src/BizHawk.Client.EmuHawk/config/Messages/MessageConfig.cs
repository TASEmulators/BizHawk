using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.Common;

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

		private Dictionary<string, MessagePosition> Positions => new Dictionary<string, MessagePosition>
		{
			["Fps"] = _fps,
			["Frame Counter"] = _frameCounter,
			["Lag Counter"] = _lagCounter,
			["Input Display"] = _inputDisplay,
			["Watches"] = _ramWatches,
			["Messages"] = _messages,
			["Rerecords"] = _reRecordCounter,
			["Autohold"] = _autohold
		};

		private int _messageColor;
		private int _alertColor;
		private int _lastInputColor;
		private int _movieInput;

		private Dictionary<string, int> Colors => new Dictionary<string, int>
		{
			["Main Messages"] = _messageColor,
			["Alert Messages"] = _alertColor,
			["Previous Frame Input"] = _lastInputColor,
			["Movie Input"] = _movieInput
		};

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
			CreateMessageRows();
			CreateColorBoxes();
			StackMessagesCheckbox.Checked = _config.StackOSDMessages;
		}

		private void CreateMessageRows()
		{
			void SetMessagePosition(MessageRow row, MessagePosition position)
			{
				MessageTypeBox.Controls
					.OfType<MessageRow>()
					.ToList()
					.ForEach(m => m.Checked = false);
				row.Checked = true;
				MessageEditor.Bind(position, () => { row.SetText(); });
			}

			MessageTypeBox.Controls.Clear();

			int y = 12;
			foreach (var position in Positions)
			{
				var row = new MessageRow { Location = new Point(10, y) };
				row.Size = new Size(MessageTypeBox.Width - 12, row.Size.Height);
				row.Bind(position.Key, position.Value, (e) => { SetMessagePosition(row, e); });
				if (position.Value == _fps)
				{
					row.Checked = true;
					MessageEditor.Bind(position.Value, () => { row.SetText(); });
				}
				y += row.Size.Height;

				MessageTypeBox.Controls.Add(row);
			}
		}

		private void CreateColorBoxes()
		{
			ColorBox.Controls.Clear();
			int y = 12;
			foreach (var color in Colors)
			{
				var row = new ColorRow {  Location = new Point(10, y) };
				row.Size = new Size(ColorBox.Width - 12, row.Size.Height);
				row.Bind(color.Key, color.Value);

				y += row.Size.Height;
				ColorBox.Controls.Add(row);
			}
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

			CreateMessageRows();
			CreateColorBoxes();
			StackMessagesCheckbox.Checked = _config.StackOSDMessages = true;
		}
	}
}
