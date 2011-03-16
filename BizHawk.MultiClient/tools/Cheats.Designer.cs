namespace BizHawk.MultiClient
{
    partial class Cheats
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Cheats));
            this.CheatListView = new System.Windows.Forms.ListView();
            this.CheatName = new System.Windows.Forms.ColumnHeader();
            this.Address = new System.Windows.Forms.ColumnHeader();
            this.Value = new System.Windows.Forms.ColumnHeader();
            this.Compare = new System.Windows.Forms.ColumnHeader();
            this.CheatsMenu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.appendFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cheatsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addCheatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeCheatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.duplicateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.insertSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.moveUpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.moveDownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.newToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.cutToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.copyToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSeparator = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonMoveUp = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonMoveDown = new System.Windows.Forms.ToolStripButton();
            this.MessageLabel = new System.Windows.Forms.Label();
            this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.findAndLoadCheatFileByGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autoTurnOnCheatsOnLoadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CheatsMenu.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // CheatListView
            // 
            this.CheatListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.CheatListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.CheatName,
            this.Address,
            this.Value,
            this.Compare});
            this.CheatListView.GridLines = true;
            this.CheatListView.Location = new System.Drawing.Point(12, 56);
            this.CheatListView.Name = "CheatListView";
            this.CheatListView.Size = new System.Drawing.Size(294, 293);
            this.CheatListView.TabIndex = 0;
            this.CheatListView.UseCompatibleStateImageBehavior = false;
            this.CheatListView.View = System.Windows.Forms.View.Details;
            // 
            // CheatName
            // 
            this.CheatName.Text = "Name";
            this.CheatName.Width = 110;
            // 
            // Address
            // 
            this.Address.Text = "Address";
            // 
            // Value
            // 
            this.Value.Text = "Value";
            // 
            // Compare
            // 
            this.Compare.Text = "Compare";
            // 
            // CheatsMenu
            // 
            this.CheatsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.cheatsToolStripMenuItem,
            this.optionsToolStripMenuItem});
            this.CheatsMenu.Location = new System.Drawing.Point(0, 0);
            this.CheatsMenu.Name = "CheatsMenu";
            this.CheatsMenu.Size = new System.Drawing.Size(509, 24);
            this.CheatsMenu.TabIndex = 1;
            this.CheatsMenu.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.appendFileToolStripMenuItem,
            this.recentToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.newToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.newToolStripMenuItem.Text = "&New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
                        | System.Windows.Forms.Keys.S)));
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.saveAsToolStripMenuItem.Text = "Save &As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // appendFileToolStripMenuItem
            // 
            this.appendFileToolStripMenuItem.Name = "appendFileToolStripMenuItem";
            this.appendFileToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.appendFileToolStripMenuItem.Text = "Append File";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(201, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            // 
            // cheatsToolStripMenuItem
            // 
            this.cheatsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addCheatToolStripMenuItem,
            this.removeCheatToolStripMenuItem,
            this.duplicateToolStripMenuItem,
            this.insertSeparatorToolStripMenuItem,
            this.toolStripSeparator3,
            this.moveUpToolStripMenuItem,
            this.moveDownToolStripMenuItem});
            this.cheatsToolStripMenuItem.Name = "cheatsToolStripMenuItem";
            this.cheatsToolStripMenuItem.Size = new System.Drawing.Size(53, 20);
            this.cheatsToolStripMenuItem.Text = "&Cheats";
            // 
            // addCheatToolStripMenuItem
            // 
            this.addCheatToolStripMenuItem.Name = "addCheatToolStripMenuItem";
            this.addCheatToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.addCheatToolStripMenuItem.Text = "&Add Cheat";
            // 
            // removeCheatToolStripMenuItem
            // 
            this.removeCheatToolStripMenuItem.Name = "removeCheatToolStripMenuItem";
            this.removeCheatToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.removeCheatToolStripMenuItem.Text = "&Remove Cheat";
            // 
            // duplicateToolStripMenuItem
            // 
            this.duplicateToolStripMenuItem.Name = "duplicateToolStripMenuItem";
            this.duplicateToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.duplicateToolStripMenuItem.Text = "&Duplicate";
            // 
            // insertSeparatorToolStripMenuItem
            // 
            this.insertSeparatorToolStripMenuItem.Name = "insertSeparatorToolStripMenuItem";
            this.insertSeparatorToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.insertSeparatorToolStripMenuItem.Text = "Insert Separator";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(162, 6);
            // 
            // moveUpToolStripMenuItem
            // 
            this.moveUpToolStripMenuItem.Name = "moveUpToolStripMenuItem";
            this.moveUpToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.moveUpToolStripMenuItem.Text = "Move &Up";
            this.moveUpToolStripMenuItem.Click += new System.EventHandler(this.moveUpToolStripMenuItem_Click);
            // 
            // moveDownToolStripMenuItem
            // 
            this.moveDownToolStripMenuItem.Name = "moveDownToolStripMenuItem";
            this.moveDownToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.moveDownToolStripMenuItem.Text = "Move &Down";
            this.moveDownToolStripMenuItem.Click += new System.EventHandler(this.moveDownToolStripMenuItem_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripButton,
            this.openToolStripButton,
            this.saveToolStripButton,
            this.toolStripSeparator,
            this.cutToolStripButton,
            this.copyToolStripButton,
            this.toolStripButtonSeparator,
            this.toolStripSeparator2,
            this.toolStripButtonMoveUp,
            this.toolStripButtonMoveDown});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(509, 25);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // newToolStripButton
            // 
            this.newToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.newToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("newToolStripButton.Image")));
            this.newToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.newToolStripButton.Name = "newToolStripButton";
            this.newToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.newToolStripButton.Text = "&New";
            this.newToolStripButton.Click += new System.EventHandler(this.newToolStripButton_Click);
            // 
            // openToolStripButton
            // 
            this.openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.openToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripButton.Image")));
            this.openToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripButton.Name = "openToolStripButton";
            this.openToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.openToolStripButton.Text = "&Open";
            this.openToolStripButton.Click += new System.EventHandler(this.openToolStripButton_Click);
            // 
            // saveToolStripButton
            // 
            this.saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.saveToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripButton.Image")));
            this.saveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveToolStripButton.Name = "saveToolStripButton";
            this.saveToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.saveToolStripButton.Text = "&Save";
            this.saveToolStripButton.Click += new System.EventHandler(this.saveToolStripButton_Click);
            // 
            // toolStripSeparator
            // 
            this.toolStripSeparator.Name = "toolStripSeparator";
            this.toolStripSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // cutToolStripButton
            // 
            this.cutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.cutToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("cutToolStripButton.Image")));
            this.cutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cutToolStripButton.Name = "cutToolStripButton";
            this.cutToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.cutToolStripButton.Text = "&Remove";
            // 
            // copyToolStripButton
            // 
            this.copyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.copyToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("copyToolStripButton.Image")));
            this.copyToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.copyToolStripButton.Name = "copyToolStripButton";
            this.copyToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.copyToolStripButton.Text = "&Duplicate";
            // 
            // toolStripButtonSeparator
            // 
            this.toolStripButtonSeparator.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSeparator.Image = global::BizHawk.MultiClient.Properties.Resources.InserSeparator;
            this.toolStripButtonSeparator.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSeparator.Name = "toolStripButtonSeparator";
            this.toolStripButtonSeparator.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonSeparator.Text = "Insert Separator";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonMoveUp
            // 
            this.toolStripButtonMoveUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonMoveUp.Image = global::BizHawk.MultiClient.Properties.Resources.BuilderDialog_moveup;
            this.toolStripButtonMoveUp.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonMoveUp.Name = "toolStripButtonMoveUp";
            this.toolStripButtonMoveUp.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonMoveUp.Text = "toolStripButton1";
            this.toolStripButtonMoveUp.Click += new System.EventHandler(this.toolStripButtonMoveUp_Click);
            // 
            // toolStripButtonMoveDown
            // 
            this.toolStripButtonMoveDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonMoveDown.Image = global::BizHawk.MultiClient.Properties.Resources.BuilderDialog_movedown;
            this.toolStripButtonMoveDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonMoveDown.Name = "toolStripButtonMoveDown";
            this.toolStripButtonMoveDown.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonMoveDown.Text = "Move Down";
            this.toolStripButtonMoveDown.Click += new System.EventHandler(this.toolStripButtonMoveDown_Click);
            // 
            // MessageLabel
            // 
            this.MessageLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.MessageLabel.AutoSize = true;
            this.MessageLabel.Location = new System.Drawing.Point(13, 356);
            this.MessageLabel.Name = "MessageLabel";
            this.MessageLabel.Size = new System.Drawing.Size(35, 13);
            this.MessageLabel.TabIndex = 3;
            this.MessageLabel.Text = "label1";
            // 
            // recentToolStripMenuItem
            // 
            this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
            this.recentToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.recentToolStripMenuItem.Text = "Recent";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveWindowPositionToolStripMenuItem,
            this.findAndLoadCheatFileByGameToolStripMenuItem,
            this.autoTurnOnCheatsOnLoadToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            // 
            // saveWindowPositionToolStripMenuItem
            // 
            this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
            this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
            // 
            // findAndLoadCheatFileByGameToolStripMenuItem
            // 
            this.findAndLoadCheatFileByGameToolStripMenuItem.Name = "findAndLoadCheatFileByGameToolStripMenuItem";
            this.findAndLoadCheatFileByGameToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.findAndLoadCheatFileByGameToolStripMenuItem.Text = "Find and Load Cheat File by Game";
            // 
            // autoTurnOnCheatsOnLoadToolStripMenuItem
            // 
            this.autoTurnOnCheatsOnLoadToolStripMenuItem.Name = "autoTurnOnCheatsOnLoadToolStripMenuItem";
            this.autoTurnOnCheatsOnLoadToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.autoTurnOnCheatsOnLoadToolStripMenuItem.Text = "Auto Turn on Cheats on Load";
            // 
            // Cheats
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(509, 379);
            this.Controls.Add(this.MessageLabel);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.CheatListView);
            this.Controls.Add(this.CheatsMenu);
            this.MainMenuStrip = this.CheatsMenu;
            this.Name = "Cheats";
            this.Text = "Cheats";
            this.Load += new System.EventHandler(this.Cheats_Load);
            this.CheatsMenu.ResumeLayout(false);
            this.CheatsMenu.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView CheatListView;
        private System.Windows.Forms.MenuStrip CheatsMenu;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem appendFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cheatsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addCheatToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeCheatToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem moveUpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem moveDownToolStripMenuItem;
        private System.Windows.Forms.ColumnHeader CheatName;
        private System.Windows.Forms.ColumnHeader Address;
        private System.Windows.Forms.ColumnHeader Value;
        private System.Windows.Forms.ColumnHeader Compare;
        private System.Windows.Forms.ToolStripMenuItem duplicateToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton newToolStripButton;
        private System.Windows.Forms.ToolStripButton openToolStripButton;
        private System.Windows.Forms.ToolStripButton saveToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
        private System.Windows.Forms.ToolStripButton cutToolStripButton;
        private System.Windows.Forms.ToolStripButton copyToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem insertSeparatorToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButtonMoveUp;
        private System.Windows.Forms.ToolStripButton toolStripButtonSeparator;
        private System.Windows.Forms.ToolStripButton toolStripButtonMoveDown;
        private System.Windows.Forms.Label MessageLabel;
        private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem findAndLoadCheatFileByGameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoTurnOnCheatsOnLoadToolStripMenuItem;
    }
}