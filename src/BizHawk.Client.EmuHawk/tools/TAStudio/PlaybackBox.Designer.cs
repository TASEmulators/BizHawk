namespace BizHawk.Client.EmuHawk
{
	partial class PlaybackBox
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlaybackBox));
            this.PlaybackGroupBox = new System.Windows.Forms.GroupBox();
            this.RecordingModeCheckbox = new System.Windows.Forms.CheckBox();
            this.AutoRestoreCheckbox = new System.Windows.Forms.CheckBox();
            this.TurboSeekCheckbox = new System.Windows.Forms.CheckBox();
            this.FollowCursorCheckbox = new System.Windows.Forms.CheckBox();
            this.NextMarkerButton = new System.Windows.Forms.Button();
            this.FrameAdvanceButton = new BizHawk.Client.EmuHawk.RepeatButton();
            this.PauseButton = new System.Windows.Forms.Button();
            this.RewindButton = new BizHawk.Client.EmuHawk.RepeatButton();
            this.PreviousMarkerButton = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.PlaybackGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // PlaybackGroupBox
            // 
            resources.ApplyResources(this.PlaybackGroupBox, "PlaybackGroupBox");
            this.PlaybackGroupBox.Controls.Add(this.RecordingModeCheckbox);
            this.PlaybackGroupBox.Controls.Add(this.AutoRestoreCheckbox);
            this.PlaybackGroupBox.Controls.Add(this.TurboSeekCheckbox);
            this.PlaybackGroupBox.Controls.Add(this.FollowCursorCheckbox);
            this.PlaybackGroupBox.Controls.Add(this.NextMarkerButton);
            this.PlaybackGroupBox.Controls.Add(this.FrameAdvanceButton);
            this.PlaybackGroupBox.Controls.Add(this.PauseButton);
            this.PlaybackGroupBox.Controls.Add(this.RewindButton);
            this.PlaybackGroupBox.Controls.Add(this.PreviousMarkerButton);
            this.PlaybackGroupBox.Name = "PlaybackGroupBox";
            this.PlaybackGroupBox.TabStop = false;
            // 
            // RecordingModeCheckbox
            // 
            resources.ApplyResources(this.RecordingModeCheckbox, "RecordingModeCheckbox");
            this.RecordingModeCheckbox.Name = "RecordingModeCheckbox";
            this.RecordingModeCheckbox.UseVisualStyleBackColor = true;
            this.RecordingModeCheckbox.MouseClick += new System.Windows.Forms.MouseEventHandler(this.RecordingModeCheckbox_MouseClick);
            // 
            // AutoRestoreCheckbox
            // 
            resources.ApplyResources(this.AutoRestoreCheckbox, "AutoRestoreCheckbox");
            this.AutoRestoreCheckbox.Name = "AutoRestoreCheckbox";
            this.AutoRestoreCheckbox.UseVisualStyleBackColor = true;
            this.AutoRestoreCheckbox.CheckedChanged += new System.EventHandler(this.AutoRestoreCheckbox_CheckedChanged);
            // 
            // TurboSeekCheckbox
            // 
            resources.ApplyResources(this.TurboSeekCheckbox, "TurboSeekCheckbox");
            this.TurboSeekCheckbox.Name = "TurboSeekCheckbox";
            this.TurboSeekCheckbox.UseVisualStyleBackColor = true;
            this.TurboSeekCheckbox.CheckedChanged += new System.EventHandler(this.TurboSeekCheckbox_CheckedChanged);
            // 
            // FollowCursorCheckbox
            // 
            resources.ApplyResources(this.FollowCursorCheckbox, "FollowCursorCheckbox");
            this.FollowCursorCheckbox.Name = "FollowCursorCheckbox";
            this.FollowCursorCheckbox.UseVisualStyleBackColor = true;
            this.FollowCursorCheckbox.CheckedChanged += new System.EventHandler(this.FollowCursorCheckbox_CheckedChanged);
            // 
            // NextMarkerButton
            // 
            resources.ApplyResources(this.NextMarkerButton, "NextMarkerButton");
            this.NextMarkerButton.Name = "NextMarkerButton";
            this.NextMarkerButton.UseVisualStyleBackColor = true;
            this.NextMarkerButton.Click += new System.EventHandler(this.NextMarkerButton_Click);
            // 
            // FrameAdvanceButton
            // 
            resources.ApplyResources(this.FrameAdvanceButton, "FrameAdvanceButton");
            this.FrameAdvanceButton.InitialDelay = 500;
            this.FrameAdvanceButton.Name = "FrameAdvanceButton";
            this.FrameAdvanceButton.RepeatDelay = 50;
            this.FrameAdvanceButton.UseVisualStyleBackColor = true;
            this.FrameAdvanceButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FrameAdvanceButton_MouseDown);
            this.FrameAdvanceButton.MouseLeave += new System.EventHandler(this.FrameAdvanceButton_MouseLeave);
            this.FrameAdvanceButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.FrameAdvanceButton_MouseUp);
            // 
            // PauseButton
            // 
            resources.ApplyResources(this.PauseButton, "PauseButton");
            this.PauseButton.Name = "PauseButton";
            this.PauseButton.UseVisualStyleBackColor = true;
            this.PauseButton.Click += new System.EventHandler(this.PauseButton_Click);
            // 
            // RewindButton
            // 
            resources.ApplyResources(this.RewindButton, "RewindButton");
            this.RewindButton.InitialDelay = 1000;
            this.RewindButton.Name = "RewindButton";
            this.RewindButton.RepeatDelay = 100;
            this.RewindButton.UseVisualStyleBackColor = true;
            this.RewindButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.RewindButton_MouseDown);
            this.RewindButton.MouseLeave += new System.EventHandler(this.RewindButton_MouseLeave);
            this.RewindButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.RewindButton_MouseUp);
            // 
            // PreviousMarkerButton
            // 
            resources.ApplyResources(this.PreviousMarkerButton, "PreviousMarkerButton");
            this.PreviousMarkerButton.Name = "PreviousMarkerButton";
            this.PreviousMarkerButton.UseVisualStyleBackColor = true;
            this.PreviousMarkerButton.Click += new System.EventHandler(this.PreviousMarkerButton_Click);
            // 
            // PlaybackBox
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.PlaybackGroupBox);
            this.Name = "PlaybackBox";
            this.PlaybackGroupBox.ResumeLayout(false);
            this.PlaybackGroupBox.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox PlaybackGroupBox;
		private System.Windows.Forms.Button NextMarkerButton;
		private RepeatButton FrameAdvanceButton;
		private System.Windows.Forms.Button PauseButton;
		private RepeatButton RewindButton;
		private System.Windows.Forms.Button PreviousMarkerButton;
		private System.Windows.Forms.CheckBox AutoRestoreCheckbox;
		private System.Windows.Forms.CheckBox TurboSeekCheckbox;
		private System.Windows.Forms.CheckBox FollowCursorCheckbox;
		private System.Windows.Forms.CheckBox RecordingModeCheckbox;
		private System.Windows.Forms.ToolTip toolTip1;
	}
}