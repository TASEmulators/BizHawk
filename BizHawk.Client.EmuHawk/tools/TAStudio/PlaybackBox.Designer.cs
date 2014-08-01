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
			this.PlaybackGroupBox = new System.Windows.Forms.GroupBox();
			this.AutoRestoreCheckbox = new System.Windows.Forms.CheckBox();
			this.TurboSeekCheckbox = new System.Windows.Forms.CheckBox();
			this.FollowCursorCheckbox = new System.Windows.Forms.CheckBox();
			this.NextMarkerButton = new System.Windows.Forms.Button();
			this.FrameAdvanceButton = new System.Windows.Forms.Button();
			this.PauseButton = new System.Windows.Forms.Button();
			this.RewindButton = new System.Windows.Forms.Button();
			this.PreviousMarkerButton = new System.Windows.Forms.Button();
			this.PlaybackGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// PlaybackGroupBox
			// 
			this.PlaybackGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.PlaybackGroupBox.Controls.Add(this.AutoRestoreCheckbox);
			this.PlaybackGroupBox.Controls.Add(this.TurboSeekCheckbox);
			this.PlaybackGroupBox.Controls.Add(this.FollowCursorCheckbox);
			this.PlaybackGroupBox.Controls.Add(this.NextMarkerButton);
			this.PlaybackGroupBox.Controls.Add(this.FrameAdvanceButton);
			this.PlaybackGroupBox.Controls.Add(this.PauseButton);
			this.PlaybackGroupBox.Controls.Add(this.RewindButton);
			this.PlaybackGroupBox.Controls.Add(this.PreviousMarkerButton);
			this.PlaybackGroupBox.Location = new System.Drawing.Point(3, 3);
			this.PlaybackGroupBox.Name = "PlaybackGroupBox";
			this.PlaybackGroupBox.Size = new System.Drawing.Size(198, 90);
			this.PlaybackGroupBox.TabIndex = 0;
			this.PlaybackGroupBox.TabStop = false;
			this.PlaybackGroupBox.Text = "Playback";
			// 
			// AutoRestoreCheckbox
			// 
			this.AutoRestoreCheckbox.AutoSize = true;
			this.AutoRestoreCheckbox.Location = new System.Drawing.Point(10, 69);
			this.AutoRestoreCheckbox.Name = "AutoRestoreCheckbox";
			this.AutoRestoreCheckbox.Size = new System.Drawing.Size(141, 17);
			this.AutoRestoreCheckbox.TabIndex = 8;
			this.AutoRestoreCheckbox.Text = "Auto-restore last position";
			this.AutoRestoreCheckbox.UseVisualStyleBackColor = true;
			this.AutoRestoreCheckbox.CheckedChanged += new System.EventHandler(this.AutoRestoreCheckbox_CheckedChanged);
			// 
			// TurboSeekCheckbox
			// 
			this.TurboSeekCheckbox.AutoSize = true;
			this.TurboSeekCheckbox.Location = new System.Drawing.Point(103, 48);
			this.TurboSeekCheckbox.Name = "TurboSeekCheckbox";
			this.TurboSeekCheckbox.Size = new System.Drawing.Size(80, 17);
			this.TurboSeekCheckbox.TabIndex = 6;
			this.TurboSeekCheckbox.Text = "Turbo seek";
			this.TurboSeekCheckbox.UseVisualStyleBackColor = true;
			this.TurboSeekCheckbox.CheckedChanged += new System.EventHandler(this.TurboSeekCheckbox_CheckedChanged);
			// 
			// FollowCursorCheckbox
			// 
			this.FollowCursorCheckbox.AutoSize = true;
			this.FollowCursorCheckbox.Enabled = false;
			this.FollowCursorCheckbox.Location = new System.Drawing.Point(10, 48);
			this.FollowCursorCheckbox.Name = "FollowCursorCheckbox";
			this.FollowCursorCheckbox.Size = new System.Drawing.Size(89, 17);
			this.FollowCursorCheckbox.TabIndex = 5;
			this.FollowCursorCheckbox.Text = "Follow Cursor";
			this.FollowCursorCheckbox.UseVisualStyleBackColor = true;
			// 
			// NextMarkerButton
			// 
			this.NextMarkerButton.Location = new System.Drawing.Point(154, 19);
			this.NextMarkerButton.Name = "NextMarkerButton";
			this.NextMarkerButton.Size = new System.Drawing.Size(38, 23);
			this.NextMarkerButton.TabIndex = 4;
			this.NextMarkerButton.Text = ">>";
			this.NextMarkerButton.UseVisualStyleBackColor = true;
			this.NextMarkerButton.Click += new System.EventHandler(this.NextMarkerButton_Click);
			// 
			// FrameAdvanceButton
			// 
			this.FrameAdvanceButton.Location = new System.Drawing.Point(117, 19);
			this.FrameAdvanceButton.Name = "FrameAdvanceButton";
			this.FrameAdvanceButton.Size = new System.Drawing.Size(38, 23);
			this.FrameAdvanceButton.TabIndex = 3;
			this.FrameAdvanceButton.Text = ">";
			this.FrameAdvanceButton.UseVisualStyleBackColor = true;
			this.FrameAdvanceButton.Click += new System.EventHandler(this.FrameAdvanceButton_Click);
			// 
			// PauseButton
			// 
			this.PauseButton.Location = new System.Drawing.Point(80, 19);
			this.PauseButton.Name = "PauseButton";
			this.PauseButton.Size = new System.Drawing.Size(38, 23);
			this.PauseButton.TabIndex = 2;
			this.PauseButton.Text = "| |";
			this.PauseButton.UseVisualStyleBackColor = true;
			this.PauseButton.Click += new System.EventHandler(this.PauseButton_Click);
			// 
			// RewindButton
			// 
			this.RewindButton.Location = new System.Drawing.Point(43, 19);
			this.RewindButton.Name = "RewindButton";
			this.RewindButton.Size = new System.Drawing.Size(38, 23);
			this.RewindButton.TabIndex = 1;
			this.RewindButton.Text = "<";
			this.RewindButton.UseVisualStyleBackColor = true;
			this.RewindButton.Click += new System.EventHandler(this.RewindButton_Click);
			// 
			// PreviousMarkerButton
			// 
			this.PreviousMarkerButton.Location = new System.Drawing.Point(6, 19);
			this.PreviousMarkerButton.Name = "PreviousMarkerButton";
			this.PreviousMarkerButton.Size = new System.Drawing.Size(38, 23);
			this.PreviousMarkerButton.TabIndex = 0;
			this.PreviousMarkerButton.Text = "<<";
			this.PreviousMarkerButton.UseVisualStyleBackColor = true;
			this.PreviousMarkerButton.Click += new System.EventHandler(this.PreviousMarkerButton_Click);
			// 
			// PlaybackBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.PlaybackGroupBox);
			this.Name = "PlaybackBox";
			this.Size = new System.Drawing.Size(204, 96);
			this.PlaybackGroupBox.ResumeLayout(false);
			this.PlaybackGroupBox.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox PlaybackGroupBox;
		private System.Windows.Forms.Button NextMarkerButton;
		private System.Windows.Forms.Button FrameAdvanceButton;
		private System.Windows.Forms.Button PauseButton;
		private System.Windows.Forms.Button RewindButton;
		private System.Windows.Forms.Button PreviousMarkerButton;
		private System.Windows.Forms.CheckBox AutoRestoreCheckbox;
		private System.Windows.Forms.CheckBox TurboSeekCheckbox;
		private System.Windows.Forms.CheckBox FollowCursorCheckbox;
	}
}
