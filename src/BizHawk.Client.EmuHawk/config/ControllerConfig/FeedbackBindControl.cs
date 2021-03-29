#nullable enable

using System.ComponentModel;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public class FeedbackBindControl : UserControl
	{
		private readonly Container _components = new();

		/// <summary>'+'-delimited e.g. <c>"Mono"</c>, <c>"Left"</c>, <c>"Left+Right"</c></summary>
		public string BoundChannels { get; private set; }

		public string BoundGamepadPrefix { get; private set; }

		public float Prescale { get; private set; }

		public readonly string VChannelName;

		public FeedbackBindControl(string vChannel, FeedbackBind existingBind, IHostInputAdapter hostInputAdapter)
		{
			BoundChannels = existingBind.Channel ?? string.Empty;
			BoundGamepadPrefix = existingBind.GamepadPrefix ?? string.Empty;
			Prescale = existingBind.IsZeroed ? 1.0f : existingBind.Prescale;
			VChannelName = vChannel;

			SzTextBoxEx txtBoundPrefix = new() { ReadOnly = true, Size = new(19, 19) };
			ComboBox cbBoundChannel = new() { Enabled = false, Size = new(112, 24) };
			void UpdateDropdownAndLabel(string newPrefix)
			{
				txtBoundPrefix.Text = newPrefix;
				var wasSelected = (string) cbBoundChannel.SelectedItem;
				cbBoundChannel.Enabled = false;
				cbBoundChannel.SelectedIndex = -1;
				cbBoundChannel.Items.Clear();
				if (hostInputAdapter.GetHapticsChannels().TryGetValue(newPrefix, out var channels) && channels.Count != 0)
				{
					var hasLeft = false;
					var hasRight = false;
					foreach (var hostChannel in channels)
					{
						cbBoundChannel.Items.Add(hostChannel);
						if (hostChannel == "Left") hasLeft = true;
						else if (hostChannel == "Right") hasRight = true;
					}
					if (hasLeft && hasRight) cbBoundChannel.Items.Add("Left+Right");
					cbBoundChannel.SelectedItem = wasSelected;
					cbBoundChannel.Enabled = true;
				}
				else if (!string.IsNullOrEmpty(newPrefix))
				{
					cbBoundChannel.Items.Add("(none available)");
					cbBoundChannel.SelectedIndex = 0;
				}
			}
			UpdateDropdownAndLabel(BoundGamepadPrefix);
			cbBoundChannel.SelectedIndexChanged += (changedSender, _)
				=> BoundChannels = (string?) ((ComboBox) changedSender).SelectedItem ?? string.Empty;
			SingleRowFLP flpBindReadout = new() { Controls = { txtBoundPrefix, cbBoundChannel, new LabelEx { Text = vChannel } } };

			Timer timer = new(_components);
			SzButtonEx btnBind = new() { Size = new(75, 23), Text = "Bind!" };
			void UpdateListeningState(bool newState)
			{
				if (newState)
				{
					Input.Instance.StartListeningForAxisEvents();
					timer.Start();
					btnBind.Text = "Cancel!";
				}
				else
				{
					timer.Stop();
					Input.Instance.StopListeningForAxisEvents();
					btnBind.Text = "Bind!";
				}
			}
			var isListening = false;
			timer.Tick += (_, _) =>
			{
				var bindValue = Input.Instance.GetNextAxisEvent();
				if (bindValue == null) return;
				UpdateListeningState(isListening = false);
				UpdateDropdownAndLabel(BoundGamepadPrefix = bindValue.SubstringBefore(' ') + ' ');
			};
			btnBind.Click += (_, _) => UpdateListeningState(isListening = !isListening);
			SzButtonEx btnUnbind = new() { Size = new(75, 23), Text = "Unbind!" };
			btnUnbind.Click += (_, _) => UpdateDropdownAndLabel(BoundGamepadPrefix = string.Empty);
			LocSingleColumnFLP flpButtons = new() { Controls = { btnBind, btnUnbind } };

			LabelEx lblPrescale = new() { Margin = new(0, 0, 0, 24) };
			TransparentTrackBar tbPrescale = new() { Maximum = 20, Size = new(96, 45), TickFrequency = 5 };
			tbPrescale.ValueChanged += (changedSender, _) =>
			{
				Prescale = ((TrackBar) changedSender).Value / 10.0f;
				lblPrescale.Text = $"Pre-scaled by: {Prescale:F1}x";
			};
			tbPrescale.Value = (int) (Prescale * 10.0f);
			LocSzSingleRowFLP flpPrescale = new() { Controls = { lblPrescale, tbPrescale }, Size = new(200, 32) };

			SuspendLayout();
			AutoScaleDimensions = new(6.0f, 13.0f);
			AutoScaleMode = AutoScaleMode.Font;
			Size = new(378, 99);
			Controls.Add(new SingleColumnFLP
			{
				Controls =
				{
					flpBindReadout,
					new SingleRowFLP { Controls = { flpButtons, flpPrescale } }
				}
			});
			ResumeLayout();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) _components.Dispose();
			base.Dispose(disposing);
		}
	}
}
