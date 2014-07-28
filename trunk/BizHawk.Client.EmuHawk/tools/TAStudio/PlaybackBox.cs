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
		private bool _programmaticallyChangingSeekBox = false;

		public TAStudio Tastudio { get; set; }

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

		public PlaybackBox()
		{
			InitializeComponent();
			_programmaticallyChangingSeekBox = true;
			if (Global.Config != null) // A check needed for the designer
			{
				TurboSeekCheckbox.Checked = Global.Config.TurboSeek;
			}
			_programmaticallyChangingSeekBox = false;
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
			if (!_programmaticallyChangingSeekBox)
			{
				Global.Config.TurboSeek ^= true;
			}
		}
	}
}
