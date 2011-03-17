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
            this.CheatListView = new BizHawk.VirtualListView();
            this.CheatName = new System.Windows.Forms.ColumnHeader();
            this.Address = new System.Windows.Forms.ColumnHeader();
            this.Value = new System.Windows.Forms.ColumnHeader();
            this.On = new System.Windows.Forms.ColumnHeader();
            this.CheatsMenu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.appendFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autoLoadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.findAndLoadCheatFileByGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autoTurnOnCheatsOnLoadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.AddCheatGroup = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.AddCheatButton = new System.Windows.Forms.Button();
            this.ValueBox = new System.Windows.Forms.TextBox();
            this.AddressBox = new System.Windows.Forms.TextBox();
            this.NameBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.NumCheatsLabel = new System.Windows.Forms.Label();
            this.CheatsMenu.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.AddCheatGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // CheatListView
            // 
            this.CheatListView.AllowColumnReorder = true;
            this.CheatListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.CheatListView.AutoArrange = false;
            this.CheatListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.CheatName,
            this.Address,
            this.Value,
            this.On});
            this.CheatListView.FullRowSelect = true;
            this.CheatListView.GridLines = true;
            this.CheatListView.ItemCount = 0;
            this.CheatListView.LabelEdit = true;
            this.CheatListView.Location = new System.Drawing.Point(12, 72);
            this.CheatListView.Name = "CheatListView";
            this.CheatListView.selectedItem = -1;
            this.CheatListView.Size = new System.Drawing.Size(294, 277);
            this.CheatListView.TabIndex = 0;
            this.CheatListView.UseCompatibleStateImageBehavior = false;
            this.CheatListView.View = System.Windows.Forms.View.Details;
            this.CheatListView.DoubleClick += new System.EventHandler(this.CheatListView_DoubleClick);
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
            // On
            // 
            this.On.Text = "On";
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
            // recentToolStripMenuItem
            // 
            this.recentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem,
            this.toolStripSeparator4,
            this.clearToolStripMenuItem,
            this.autoLoadToolStripMenuItem});
            this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
            this.recentToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.recentToolStripMenuItem.Text = "Recent";
            this.recentToolStripMenuItem.DropDownOpened += new System.EventHandler(this.recentToolStripMenuItem_DropDownOpened);
            // 
            // noneToolStripMenuItem
            // 
            this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
            this.noneToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.noneToolStripMenuItem.Text = "None";
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(129, 6);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.clearToolStripMenuItem.Text = "Clear";
            // 
            // autoLoadToolStripMenuItem
            // 
            this.autoLoadToolStripMenuItem.Name = "autoLoadToolStripMenuItem";
            this.autoLoadToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.autoLoadToolStripMenuItem.Text = "Auto-load";
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
            this.addCheatToolStripMenuItem.Click += new System.EventHandler(this.addCheatToolStripMenuItem_Click);
            // 
            // removeCheatToolStripMenuItem
            // 
            this.removeCheatToolStripMenuItem.Name = "removeCheatToolStripMenuItem";
            this.removeCheatToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.removeCheatToolStripMenuItem.Text = "&Remove Cheat";
            this.removeCheatToolStripMenuItem.Click += new System.EventHandler(this.removeCheatToolStripMenuItem_Click);
            // 
            // duplicateToolStripMenuItem
            // 
            this.duplicateToolStripMenuItem.Name = "duplicateToolStripMenuItem";
            this.duplicateToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.duplicateToolStripMenuItem.Text = "&Duplicate";
            this.duplicateToolStripMenuItem.Click += new System.EventHandler(this.duplicateToolStripMenuItem_Click);
            // 
            // insertSeparatorToolStripMenuItem
            // 
            this.insertSeparatorToolStripMenuItem.Name = "insertSeparatorToolStripMenuItem";
            this.insertSeparatorToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.insertSeparatorToolStripMenuItem.Text = "Insert Separator";
            this.insertSeparatorToolStripMenuItem.Click += new System.EventHandler(this.insertSeparatorToolStripMenuItem_Click);
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
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveWindowPositionToolStripMenuItem,
            this.findAndLoadCheatFileByGameToolStripMenuItem,
            this.autoTurnOnCheatsOnLoadToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            this.optionsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.optionsToolStripMenuItem_DropDownOpened);
            // 
            // saveWindowPositionToolStripMenuItem
            // 
            this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
            this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
            this.saveWindowPositionToolStripMenuItem.Click += new System.EventHandler(this.saveWindowPositionToolStripMenuItem_Click);
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
            this.cutToolStripButton.Image = global::BizHawk.MultiClient.Properties.Resources.BuilderDialog_delete;
            this.cutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cutToolStripButton.Name = "cutToolStripButton";
            this.cutToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.cutToolStripButton.Text = "&Remove";
            this.cutToolStripButton.Click += new System.EventHandler(this.cutToolStripButton_Click);
            // 
            // copyToolStripButton
            // 
            this.copyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.copyToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("copyToolStripButton.Image")));
            this.copyToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.copyToolStripButton.Name = "copyToolStripButton";
            this.copyToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.copyToolStripButton.Text = "&Duplicate";
            this.copyToolStripButton.Click += new System.EventHandler(this.copyToolStripButton_Click);
            // 
            // toolStripButtonSeparator
            // 
            this.toolStripButtonSeparator.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSeparator.Image = global::BizHawk.MultiClient.Properties.Resources.InserSeparator;
            this.toolStripButtonSeparator.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSeparator.Name = "toolStripButtonSeparator";
            this.toolStripButtonSeparator.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonSeparator.Text = "Insert Separator";
            this.toolStripButtonSeparator.Click += new System.EventHandler(this.toolStripButtonSeparator_Click);
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
            this.MessageLabel.Size = new System.Drawing.Size(0, 13);
            this.MessageLabel.TabIndex = 3;
            // 
            // AddCheatGroup
            // 
            this.AddCheatGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddCheatGroup.Controls.Add(this.label5);
            this.AddCheatGroup.Controls.Add(this.label4);
            this.AddCheatGroup.Controls.Add(this.AddCheatButton);
            this.AddCheatGroup.Controls.Add(this.ValueBox);
            this.AddCheatGroup.Controls.Add(this.AddressBox);
            this.AddCheatGroup.Controls.Add(this.NameBox);
            this.AddCheatGroup.Controls.Add(this.label3);
            this.AddCheatGroup.Controls.Add(this.label2);
            this.AddCheatGroup.Controls.Add(this.label1);
            this.AddCheatGroup.Location = new System.Drawing.Point(327, 72);
            this.AddCheatGroup.Name = "AddCheatGroup";
            this.AddCheatGroup.Size = new System.Drawing.Size(170, 150);
            this.AddCheatGroup.TabIndex = 4;
            this.AddCheatGroup.TabStop = false;
            this.AddCheatGroup.Text = "Add Cheat";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(80, 85);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(18, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "0x";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(80, 56);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(18, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "0x";
            // 
            // AddCheatButton
            // 
            this.AddCheatButton.Location = new System.Drawing.Point(99, 115);
            this.AddCheatButton.Name = "AddCheatButton";
            this.AddCheatButton.Size = new System.Drawing.Size(65, 23);
            this.AddCheatButton.TabIndex = 6;
            this.AddCheatButton.Text = "&Add";
            this.AddCheatButton.UseVisualStyleBackColor = true;
            this.AddCheatButton.Click += new System.EventHandler(this.AddCheatButton_Click);
            // 
            // ValueBox
            // 
            this.ValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ValueBox.Location = new System.Drawing.Point(99, 79);
            this.ValueBox.MaxLength = 2;
            this.ValueBox.Name = "ValueBox";
            this.ValueBox.Size = new System.Drawing.Size(65, 20);
            this.ValueBox.TabIndex = 5;
            // 
            // AddressBox
            // 
            this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.AddressBox.Location = new System.Drawing.Point(99, 51);
            this.AddressBox.MaxLength = 8;
            this.AddressBox.Name = "AddressBox";
            this.AddressBox.Size = new System.Drawing.Size(65, 20);
            this.AddressBox.TabIndex = 4;
            // 
            // NameBox
            // 
            this.NameBox.Location = new System.Drawing.Point(64, 25);
            this.NameBox.Name = "NameBox";
            this.NameBox.Size = new System.Drawing.Size(100, 20);
            this.NameBox.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 82);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(34, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Value";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Address";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name";
            // 
            // NumCheatsLabel
            // 
            this.NumCheatsLabel.AutoSize = true;
            this.NumCheatsLabel.Location = new System.Drawing.Point(9, 52);
            this.NumCheatsLabel.Name = "NumCheatsLabel";
            this.NumCheatsLabel.Size = new System.Drawing.Size(49, 13);
            this.NumCheatsLabel.TabIndex = 5;
            this.NumCheatsLabel.Text = "0 Cheats";
            // 
            // Cheats
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(509, 379);
            this.Controls.Add(this.NumCheatsLabel);
            this.Controls.Add(this.AddCheatGroup);
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
            this.AddCheatGroup.ResumeLayout(false);
            this.AddCheatGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private VirtualListView CheatListView;
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
        private System.Windows.Forms.ColumnHeader On;
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
        private System.Windows.Forms.GroupBox AddCheatGroup;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox AddressBox;
        private System.Windows.Forms.TextBox NameBox;
        private System.Windows.Forms.TextBox ValueBox;
        private System.Windows.Forms.Button AddCheatButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label NumCheatsLabel;
        private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoLoadToolStripMenuItem;
    }
}