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
