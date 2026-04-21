using System.ComponentModel;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PlaybackBox : UserControl
	{
		private bool _loading = true;

		public TAStudio Tastudio { get; set; }

		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool TurboSeek
		{
			get => Tastudio.Config.TurboSeek;
			set => TurboSeekCheckbox.Checked = value;
		}

		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool AutoRestore
		{
			get => Tastudio.Settings.AutoRestoreLastPosition;
			set => AutoRestoreCheckbox.Checked = value;
		}

		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool FollowCursor
		{
			get => Tastudio.Settings.FollowCursor;
			set => FollowCursorCheckbox.Checked = value;
		}

		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool RecordingMode
		{
			get => Tastudio.CurrentTasMovie.IsRecording();
			set
			{
				RecordingModeCheckbox.Checked = value;
				Tastudio.MovieSession.ReadOnly = !value;
				if (RecordingModeCheckbox.Checked)
				{
					Tastudio.CurrentTasMovie.SwitchToRecord();
				}
				else
				{
					Tastudio.CurrentTasMovie.SwitchToPlay();
				}

				Tastudio.MainForm.SetMainformMovieInfo();
			}
		}

		public PlaybackBox()
		{
			InitializeComponent();
		}

		public void UpdateHotkeyTooltips(Config config)
		{
			string GetBindingText(string hotkey, string hardcodedBinding = null)
			{
				string raw = config.HotkeyBindings[hotkey];
				if (raw.Length == 0)
				{
					return $"Hotkey: {hardcodedBinding ?? "unbound"}";
				}
				else if (hardcodedBinding != null)
				{
					return $"Hotkey: {hardcodedBinding} or {raw.Replace(",", " or ")}";
				}
				return $"Hotkey: {raw.Replace(",", " or ")}";
			}

			toolTip1.SetToolTip(NextMarkerButton, GetBindingText("Seek To Next Marker")
				+ "\nseek to next marker");
			toolTip1.SetToolTip(PreviousMarkerButton, GetBindingText("Seek To Prev Marker")
				+ "\nseek to previous marker");
			toolTip1.SetToolTip(FrameAdvanceButton, GetBindingText("Frame Advance", "Right Mouse Button + Wheel Down")
				+ "\nframe advance");
			toolTip1.SetToolTip(RewindButton, GetBindingText("Rewind", "Right Mouse Button + Wheel Up")
				+ "\ngo back 1 step (size of step is configurable in settings)"
				+ "\nWheel is always 1 frame per tick.");
			toolTip1.SetToolTip(PauseButton, GetBindingText("Pause", "Middle Mouse Button")
				+ "\ntoggle pause");

			toolTip1.SetToolTip(FollowCursorCheckbox, GetBindingText("Toggle Follow Cursor")
				+ "\nWhen enabled, the current emulator frame will be"
				+ "\nmade visible every time the current frame changes.");
			toolTip1.SetToolTip(AutoRestoreCheckbox, GetBindingText("Toggle Auto-Restore")
				+ "\nWhen enabled and you edit a frame before the current emulator frame,"
				+ "\nTAStudio will automatically seek back to said emulator frame.");
			toolTip1.SetToolTip(TurboSeekCheckbox, GetBindingText("Toggle Turbo Seek")
				+ "\nWhen enabled, TAStudio will run the emulator as fast as possible during seeks.");
			toolTip1.SetToolTip(RecordingModeCheckbox, GetBindingText("Toggle read-only")
				+ "\nWhen enabled, you can record inputs while unpaused or when frame advancing.");
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (DesignMode)
			{
				return;
			}

			TurboSeekCheckbox.Checked = Tastudio.Config?.TurboSeek ?? false;
			AutoRestoreCheckbox.Checked = Tastudio.Settings.AutoRestoreLastPosition;
			FollowCursorCheckbox.Checked = Tastudio.Settings.FollowCursor;
			RecordingModeCheckbox.Checked = RecordingMode;

			_loading = false;
		}

		private void PreviousMarkerButton_Click(object sender, EventArgs e)
		{
			Tastudio.GoToPreviousMarker();
		}

		private void PauseButton_Click(object sender, EventArgs e)
		{
			Tastudio.TogglePause();
		}

		private void NextMarkerButton_Click(object sender, EventArgs e)
		{
			Tastudio.GoToNextMarker();
		}

		private void TurboSeekCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_loading) Tastudio.Config.TurboSeek = !Tastudio.Config.TurboSeek;
		}

		private void AutoRestoreCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_loading) Tastudio.Settings.AutoRestoreLastPosition = !Tastudio.Settings.AutoRestoreLastPosition;
		}

		private void FollowCursorCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Tastudio.Settings.FollowCursor = !Tastudio.Settings.FollowCursor;
				if (Tastudio.Settings.FollowCursor)
				{
					Tastudio.SetVisibleFrame();
					Tastudio.RefreshDialog();
				}
			}
		}

		private void RecordingModeCheckbox_MouseClick(object sender, MouseEventArgs e)
		{
			RecordingMode = !RecordingMode;
			Tastudio.WasRecording = RecordingMode; // hard reset at manual click and hotkey
		}

		private void RewindButton_MouseDown(object sender, MouseEventArgs e)
		{
			Tastudio.MainForm.PressRewind = true;
		}

		private void RewindButton_MouseUp(object sender, MouseEventArgs e)
		{
			Tastudio.MainForm.PressRewind = false;
		}

		private void RewindButton_MouseLeave(object sender, EventArgs e)
		{
			Tastudio.MainForm.PressRewind = false;
		}

		private void FrameAdvanceButton_MouseDown(object sender, MouseEventArgs e)
		{
			Tastudio.MainForm.HoldFrameAdvance = true;
		}

		private void FrameAdvanceButton_MouseLeave(object sender, EventArgs e)
		{
			Tastudio.MainForm.HoldFrameAdvance = false;
		}

		private void FrameAdvanceButton_MouseUp(object sender, MouseEventArgs e)
		{
			Tastudio.MainForm.HoldFrameAdvance = false;
		}
	}
}
