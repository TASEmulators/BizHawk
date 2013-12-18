namespace BizHawk.Client.EmuHawk.tools.TAStudio
{
	partial class MarkerControlsBox
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
			this.SelectionPreviousMarkerButton = new System.Windows.Forms.Button();
			this.SelectionSimilarMarkerButton = new System.Windows.Forms.Button();
			this.SelectionMoreButton = new System.Windows.Forms.Button();
			this.SelectionNextMarker = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// SelectionPreviousMarkerButton
			// 
			this.SelectionPreviousMarkerButton.Location = new System.Drawing.Point(13, 3);
			this.SelectionPreviousMarkerButton.Name = "SelectionPreviousMarkerButton";
			this.SelectionPreviousMarkerButton.Size = new System.Drawing.Size(38, 23);
			this.SelectionPreviousMarkerButton.TabIndex = 0;
			this.SelectionPreviousMarkerButton.Text = "<<";
			this.SelectionPreviousMarkerButton.UseVisualStyleBackColor = true;
			// 
			// SelectionSimilarMarkerButton
			// 
			this.SelectionSimilarMarkerButton.Location = new System.Drawing.Point(51, 3);
			this.SelectionSimilarMarkerButton.Name = "SelectionSimilarMarkerButton";
			this.SelectionSimilarMarkerButton.Size = new System.Drawing.Size(50, 23);
			this.SelectionSimilarMarkerButton.TabIndex = 1;
			this.SelectionSimilarMarkerButton.Text = "Similar";
			this.SelectionSimilarMarkerButton.UseVisualStyleBackColor = true;
			// 
			// SelectionMoreButton
			// 
			this.SelectionMoreButton.Location = new System.Drawing.Point(101, 3);
			this.SelectionMoreButton.Name = "SelectionMoreButton";
			this.SelectionMoreButton.Size = new System.Drawing.Size(50, 23);
			this.SelectionMoreButton.TabIndex = 2;
			this.SelectionMoreButton.Text = "More";
			this.SelectionMoreButton.UseVisualStyleBackColor = true;
			// 
			// SelectionNextMarker
			// 
			this.SelectionNextMarker.Location = new System.Drawing.Point(151, 3);
			this.SelectionNextMarker.Name = "SelectionNextMarker";
			this.SelectionNextMarker.Size = new System.Drawing.Size(38, 23);
			this.SelectionNextMarker.TabIndex = 3;
			this.SelectionNextMarker.Text = ">>";
			this.SelectionNextMarker.UseVisualStyleBackColor = true;
			// 
			// MarkerControlsBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.SelectionNextMarker);
			this.Controls.Add(this.SelectionMoreButton);
			this.Controls.Add(this.SelectionSimilarMarkerButton);
			this.Controls.Add(this.SelectionPreviousMarkerButton);
			this.Name = "MarkerControlsBox";
			this.Size = new System.Drawing.Size(204, 30);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button SelectionPreviousMarkerButton;
		private System.Windows.Forms.Button SelectionSimilarMarkerButton;
		private System.Windows.Forms.Button SelectionMoreButton;
		private System.Windows.Forms.Button SelectionNextMarker;
	}
}
