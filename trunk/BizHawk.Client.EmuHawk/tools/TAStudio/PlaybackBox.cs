using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PlaybackBox : UserControl
	{
		private bool _programmaticallyChangingValue = false;

		public TAStudio Tastudio { get; set; }

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public bool TurboSeek
		{
			get
			{
				return Global.Config.TurboSeek;
			}

			set
			{
				TurboSeekCheckbox.Checked = Global.Config.TurboSeek = value;
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public bool AutoRestore
		{
			get
			{
				return Tastudio.Settings.AutoRestoreLastPosition;
			}

			set
			{
				AutoRestoreCheckbox.Checked = Tastudio.Settings.AutoRestoreLastPosition = value;
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public bool FollowCursor
		{
			get
			{
				return Tastudio.Settings.FollowCursor;
			}

			set
			{
				FollowCursorCheckbox.Checked = value;
			}
		}

		public PlaybackBox()
		{
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			_programmaticallyChangingValue = true;

			if (Global.Config != null) // For the designer
			{
				TurboSeekCheckbox.Checked = Global.Config.TurboSeek;
			}

			if (Tastudio != null) // For the designer
			{
				AutoRestoreCheckbox.Checked = Tastudio.Settings.AutoRestoreLastPosition;
				FollowCursorCheckbox.Checked = Tastudio.Settings.FollowCursor;
			}

			_programmaticallyChangingValue = false;
		}

		private void PreviousMarkerButton_Click(object sender, EventArgs e)
		{
			Tastudio.GoToPreviousMarker();
		}

		private void RewindButton_Click(object sender, EventArgs e)
		{
			Tastudio.GoToPreviousFrame();
		}

		private void PauseButton_Click(object sender, EventArgs e)
		{
			Tastudio.TogglePause();
		}

		private void FrameAdvanceButton_Click(object sender, EventArgs e)
		{
			Tastudio.GoToNextFrame();
		}

		private void NextMarkerButton_Click(object sender, EventArgs e)
		{
			Tastudio.GoToNextMarker();
		}

		private void TurboSeekCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValue)
			{
				Global.Config.TurboSeek ^= true;
			}
		}

		private void AutoRestoreCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValue)
			{
				Tastudio.Settings.AutoRestoreLastPosition ^= true;
			}
		}

		private void FollowCursorCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			Tastudio.Settings.FollowCursor ^= true;

			if (!_programmaticallyChangingValue)
			{
				if (Tastudio.Settings.FollowCursor)
				{
					Tastudio.SetVisibleIndex();
					Tastudio.RefreshDialog();
				}
			}
		}
	}
}
