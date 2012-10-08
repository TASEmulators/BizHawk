namespace BizHawk.MultiClient.config.ControllerConfig
{
    partial class UserControlGamePad
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
            this.flowLayoutPanelLabels = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanelCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();
            // 
            // flowLayoutPanelLabels
            // 
            this.flowLayoutPanelLabels.Dock = System.Windows.Forms.DockStyle.Left;
            this.flowLayoutPanelLabels.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelLabels.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelLabels.Name = "flowLayoutPanelLabels";
            this.flowLayoutPanelLabels.Size = new System.Drawing.Size(155, 400);
            this.flowLayoutPanelLabels.TabIndex = 0;
            // 
            // flowLayoutPanelCommands
            // 
            this.flowLayoutPanelCommands.Dock = System.Windows.Forms.DockStyle.Right;
            this.flowLayoutPanelCommands.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelCommands.Location = new System.Drawing.Point(161, 0);
            this.flowLayoutPanelCommands.Name = "flowLayoutPanelCommands";
            this.flowLayoutPanelCommands.Size = new System.Drawing.Size(154, 400);
            this.flowLayoutPanelCommands.TabIndex = 1;
            // 
            // UserControlGamePad
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.flowLayoutPanelCommands);
            this.Controls.Add(this.flowLayoutPanelLabels);
            this.Name = "UserControlGamePad";
            this.Size = new System.Drawing.Size(315, 400);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelLabels;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelCommands;


    }
}
