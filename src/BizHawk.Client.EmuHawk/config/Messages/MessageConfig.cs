using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.WinForms.Controls;

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

		private readonly SzNUDEx _nudDuration;

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

		private Dictionary<string, int> Colors => new Dictionary<string, int>
		{
			["Main Messages"] = _config.MessagesColor,
			["Alert Messages"] = _config.AlertMessageColor,
			["Previous Frame Input"] = _config.LastInputColor,
			["Movie Input"] = _config.MovieInputColor
		};

		private IEnumerable<ColorRow> ColorRows => ColorBox.Controls.OfType<ColorRow>();

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

			InitializeComponent();

			// I'm done wasting my time w/ the Designer --yoshi
			SuspendLayout();
			_nudDuration = new() { Maximum = 10.0M, Minimum = 1.0M, Size = new(48, 20) };
			Controls.Add(new LocSzSingleRowFLP
			{
				Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
				Controls =
				{
					new LabelEx { Text = "Messages fade after" },
					_nudDuration,
					new LabelEx { Text = "seconds" },
				},
				Location = OSTailoredCode.IsUnixHost ? new(224, 380) : new(192, 360), // ¯\_(ツ)_/¯
				Size = new(300, 24),
			});
			ResumeLayout();
		}

		private void MessageConfig_Load(object sender, EventArgs e)
		{
			CreateMessageRows();
			CreateColorBoxes();
			StackMessagesCheckbox.Checked = _config.StackOSDMessages;
			_nudDuration.Value = _config.OSDMessageDuration;
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
			foreach (var (name, pos) in Positions)
			{
				var row = new MessageRow
				{
					Name = name,
					Location = new Point(10, y)
				};
				row.Size = new Size(MessageTypeBox.Width - 12, row.Size.Height);
				row.Bind(name, pos, e => SetMessagePosition(row, e));
				if (pos == _fps)
				{
					row.Checked = true;
					MessageEditor.Bind(pos, row.SetText);
				}
				y += row.Size.Height;

				MessageTypeBox.Controls.Add(row);
			}
		}

		private void CreateColorBoxes()
		{
			ColorBox.Controls.Clear();
			int y = 20;
			foreach (var (name, argb) in Colors)
			{
				var row = new ColorRow
				{
					Name = name,
					Location = new Point(10, y),
					DisplayName = name,
					SelectedColor = argb
				};
				row.Size = new Size(ColorBox.Width - 12, row.Size.Height);
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

			_config.MessagesColor = ColorRows.Single(r => r.Name == "Main Messages").SelectedColor;
			_config.AlertMessageColor = ColorRows.Single(r => r.Name == "Alert Messages").SelectedColor;
			_config.LastInputColor = ColorRows.Single(r => r.Name == "Previous Frame Input").SelectedColor;
			_config.MovieInputColor = ColorRows.Single(r => r.Name == "Movie Input").SelectedColor;

			_config.OSDMessageDuration = (int) _nudDuration.Value;
			_config.StackOSDMessages = StackMessagesCheckbox.Checked;
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void ResetDefaultsButton_Click(object sender, EventArgs e)
		{
			_fps = DefaultMessagePositions.Fps.Clone();
			_frameCounter = DefaultMessagePositions.FrameCounter.Clone();
			_lagCounter = DefaultMessagePositions.LagCounter.Clone();
			_inputDisplay = DefaultMessagePositions.InputDisplay.Clone();
			_reRecordCounter = DefaultMessagePositions.ReRecordCounter.Clone();
			_messages = DefaultMessagePositions.Messages.Clone();
			_autohold = DefaultMessagePositions.Autohold.Clone();
			_ramWatches = DefaultMessagePositions.RamWatches.Clone();

			ColorRows.Single(r => r.Name == "Main Messages").SelectedColor = DefaultMessagePositions.MessagesColor;
			ColorRows.Single(r => r.Name == "Alert Messages").SelectedColor = DefaultMessagePositions.AlertMessageColor;
			ColorRows.Single(r => r.Name == "Previous Frame Input").SelectedColor = DefaultMessagePositions.LastInputColor;
			ColorRows.Single(r => r.Name == "Movie Input").SelectedColor = DefaultMessagePositions.MovieInputColor;

			CreateMessageRows();
			StackMessagesCheckbox.Checked = true;
		}
	}
}
