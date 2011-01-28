namespace BizHawk.MultiClient
{
    partial class RamSearch
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RamSearch));
            this.SearchtoolStrip1 = new System.Windows.Forms.ToolStrip();
            this.newToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.cutToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.WatchtoolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.PoketoolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.TotalSearchLabel = new System.Windows.Forms.Label();
            this.listView1 = new System.Windows.Forms.ListView();
            this.Address = new System.Windows.Forms.ColumnHeader();
            this.Value = new System.Windows.Forms.ColumnHeader();
            this.Previous = new System.Windows.Forms.ColumnHeader();
            this.Changes = new System.Windows.Forms.ColumnHeader();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hackyAutoLoadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.UndotoolStripButton = new System.Windows.Forms.ToolStripButton();
            this.NewSearchtoolStripButton = new System.Windows.Forms.ToolStripButton();
            this.ClearChangeCountstoolStripButton = new System.Windows.Forms.ToolStripButton();
            this.SearchtoolStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // SearchtoolStrip1
            // 
            this.SearchtoolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.SearchtoolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripButton,
            this.openToolStripButton,
            this.saveToolStripButton,
            this.toolStripSeparator,
            this.cutToolStripButton,
            this.WatchtoolStripButton1,
            this.PoketoolStripButton1,
            this.toolStripSeparator1});
            this.SearchtoolStrip1.Location = new System.Drawing.Point(3, 0);
            this.SearchtoolStrip1.Name = "SearchtoolStrip1";
            this.SearchtoolStrip1.Size = new System.Drawing.Size(160, 25);
            this.SearchtoolStrip1.TabIndex = 0;
            this.SearchtoolStrip1.Text = "Search";
            // 
            // newToolStripButton
            // 
            this.newToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.newToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("newToolStripButton.Image")));
            this.newToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.newToolStripButton.Name = "newToolStripButton";
            this.newToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.newToolStripButton.Text = "&New";
            this.newToolStripButton.ToolTipText = "New Search List";
            // 
            // openToolStripButton
            // 
            this.openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.openToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripButton.Image")));
            this.openToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripButton.Name = "openToolStripButton";
            this.openToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.openToolStripButton.Text = "&Open";
            this.openToolStripButton.ToolTipText = "Open Search List";
            // 
            // saveToolStripButton
            // 
            this.saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.saveToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripButton.Image")));
            this.saveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveToolStripButton.Name = "saveToolStripButton";
            this.saveToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.saveToolStripButton.Text = "&Save";
            this.saveToolStripButton.ToolTipText = "Save Watch List";
            // 
            // toolStripSeparator
            // 
            this.toolStripSeparator.Name = "toolStripSeparator";
            this.toolStripSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // cutToolStripButton
            // 
            this.cutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.cutToolStripButton.Image = global::BizHawk.MultiClient.Properties.Resources.BuilderDialog_delete;
            this.cutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cutToolStripButton.Name = "cutToolStripButton";
            this.cutToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.cutToolStripButton.Text = "C&ut";
            this.cutToolStripButton.ToolTipText = "Eliminate Selected Items";
            // 
            // WatchtoolStripButton1
            // 
            this.WatchtoolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.WatchtoolStripButton1.Image = global::BizHawk.MultiClient.Properties.Resources.FindHS;
            this.WatchtoolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.WatchtoolStripButton1.Name = "WatchtoolStripButton1";
            this.WatchtoolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.WatchtoolStripButton1.Text = "toolStripButton1";
            // 
            // PoketoolStripButton1
            // 
            this.PoketoolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.PoketoolStripButton1.Image = global::BizHawk.MultiClient.Properties.Resources.poke;
            this.PoketoolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.PoketoolStripButton1.Name = "PoketoolStripButton1";
            this.PoketoolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.PoketoolStripButton1.Text = "Poke";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // TotalSearchLabel
            // 
            this.TotalSearchLabel.AutoSize = true;
            this.TotalSearchLabel.Location = new System.Drawing.Point(13, 33);
            this.TotalSearchLabel.Name = "TotalSearchLabel";
            this.TotalSearchLabel.Size = new System.Drawing.Size(64, 13);
            this.TotalSearchLabel.TabIndex = 2;
            this.TotalSearchLabel.Text = "0 addresses";
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Address,
            this.Value,
            this.Previous,
            this.Changes});
            this.listView1.GridLines = true;
            this.listView1.Location = new System.Drawing.Point(16, 58);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(221, 391);
            this.listView1.TabIndex = 3;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // Address
            // 
            this.Address.Text = "Address";
            this.Address.Width = 66;
            // 
            // Value
            // 
            this.Value.Text = "Value";
            this.Value.Width = 48;
            // 
            // Previous
            // 
            this.Previous.Text = "Prev";
            this.Previous.Width = 48;
            // 
            // Changes
            // 
            this.Changes.Text = "Changes";
            this.Changes.Width = 55;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(449, 24);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.hackyAutoLoadToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // hackyAutoLoadToolStripMenuItem
            // 
            this.hackyAutoLoadToolStripMenuItem.Name = "hackyAutoLoadToolStripMenuItem";
            this.hackyAutoLoadToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.hackyAutoLoadToolStripMenuItem.Text = "HackyAutoLoad";
            this.hackyAutoLoadToolStripMenuItem.Click += new System.EventHandler(this.hackyAutoLoadToolStripMenuItem_Click);
            // 
            // toolStripContainer1
            // 
            this.toolStripContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(206, 125);
            this.toolStripContainer1.Location = new System.Drawing.Point(243, 58);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(206, 175);
            this.toolStripContainer1.TabIndex = 5;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.SearchtoolStrip1);
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewSearchtoolStripButton,
            this.toolStripButton1,
            this.ClearChangeCountstoolStripButton,
            this.UndotoolStripButton});
            this.toolStrip1.Location = new System.Drawing.Point(3, 25);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(102, 25);
            this.toolStrip1.TabIndex = 1;
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton1.Text = "toolStripButton1";
            // 
            // UndotoolStripButton
            // 
            this.UndotoolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.UndotoolStripButton.Image = global::BizHawk.MultiClient.Properties.Resources.undo;
            this.UndotoolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.UndotoolStripButton.Name = "UndotoolStripButton";
            this.UndotoolStripButton.Size = new System.Drawing.Size(23, 22);
            this.UndotoolStripButton.Text = "Undo Search";
            // 
            // NewSearchtoolStripButton
            // 
            this.NewSearchtoolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.NewSearchtoolStripButton.Image = global::BizHawk.MultiClient.Properties.Resources.restart;
            this.NewSearchtoolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.NewSearchtoolStripButton.Name = "NewSearchtoolStripButton";
            this.NewSearchtoolStripButton.Size = new System.Drawing.Size(23, 22);
            this.NewSearchtoolStripButton.Text = "New Search";
            // 
            // ClearChangeCountstoolStripButton
            // 
            this.ClearChangeCountstoolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ClearChangeCountstoolStripButton.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ClearChangeCountstoolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("ClearChangeCountstoolStripButton.Image")));
            this.ClearChangeCountstoolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ClearChangeCountstoolStripButton.Name = "ClearChangeCountstoolStripButton";
            this.ClearChangeCountstoolStripButton.Size = new System.Drawing.Size(23, 22);
            this.ClearChangeCountstoolStripButton.Text = "C";
            this.ClearChangeCountstoolStripButton.ToolTipText = "Clear Change Counts";
            // 
            // RamSearch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(449, 461);
            this.Controls.Add(this.toolStripContainer1);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.TotalSearchLabel);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "RamSearch";
            this.Text = "Ram Search";
            this.Load += new System.EventHandler(this.RamSearch_Load);
            this.SearchtoolStrip1.ResumeLayout(false);
            this.SearchtoolStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip SearchtoolStrip1;
        private System.Windows.Forms.ToolStripButton newToolStripButton;
        private System.Windows.Forms.ToolStripButton openToolStripButton;
        private System.Windows.Forms.ToolStripButton saveToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
        private System.Windows.Forms.ToolStripButton cutToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton WatchtoolStripButton1;
        private System.Windows.Forms.ToolStripButton PoketoolStripButton1;
        private System.Windows.Forms.Label TotalSearchLabel;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader Address;
        private System.Windows.Forms.ColumnHeader Value;
        private System.Windows.Forms.ColumnHeader Previous;
        private System.Windows.Forms.ColumnHeader Changes;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hackyAutoLoadToolStripMenuItem;
        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton NewSearchtoolStripButton;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripButton ClearChangeCountstoolStripButton;
        private System.Windows.Forms.ToolStripButton UndotoolStripButton;
    }
}