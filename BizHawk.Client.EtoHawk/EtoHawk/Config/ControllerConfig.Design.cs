using System;
using System.Collections.Generic;
using System.Text;
using Eto;
using Eto.Forms;
using Eto.Drawing;

namespace EtoHawk.Config
{
    public partial class ControllerConfig
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
            if (_timer != null)
            {
                _timer.Interrupt(); //Shouldn't happen, we kill it when it loses focus.
                if (!_timer.Join(50))
                {
                    _timer.Abort(); //Should have ended right away and didn't, so now it must die.
                }
                _timer = null;
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
            this.components = new System.ComponentModel.Container();
            this.tabControl1 = new TabControl();
            this.NormalControlsTab = new TabPage();
            this.AutofireControlsTab = new TabPage();
            this.AnalogControlsTab = new TabPage();
            this.checkBoxAutoTab = new CheckBox();
            this.checkBoxUDLR = new CheckBox();
            this.buttonOK = new Button();
            this.buttonCancel = new Button();
            this.pictureBox1 = new ImageView();
            this.contextMenuStrip1 = new ContextMenu();
            this.testToolStripMenuItem = new ButtonMenuItem();
            this.loadDefaultsToolStripMenuItem = new ButtonMenuItem();
            this.clearToolStripMenuItem = new ButtonMenuItem();
            this.label3 = new Label();
            this.label2 = new Label();
            this.label38 = new Label();
            this.btnMisc = new Button();
            this.tabControl1.SuspendLayout();
            //this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Pages.Add(this.NormalControlsTab);
            this.tabControl1.Pages.Add(this.AutofireControlsTab);
            this.tabControl1.Pages.Add(this.AnalogControlsTab);
            //this.tabControl1.Dock = DockStyle.Fill;
            //this.tabControl1.Location = new System.Drawing.Point(3, 3);
            //this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            //this.tabControl1.Size = new System.Drawing.Size(562, 521);
            //this.tabControl1.TabIndex = 1;
            // 
            // NormalControlsTab
            // 
            //this.NormalControlsTab.Location = new System.Drawing.Point(4, 22);
            //this.NormalControlsTab.Name = "NormalControlsTab";
            this.NormalControlsTab.Padding = new Padding(3);
            //this.NormalControlsTab.Size = new System.Drawing.Size(554, 495);
            //this.NormalControlsTab.TabIndex = 0;
            this.NormalControlsTab.Text = "Normal Controls";
            //this.NormalControlsTab.UseVisualStyleBackColor = true;
            // 
            // AutofireControlsTab
            // 
            //this.AutofireControlsTab.Location = new System.Drawing.Point(4, 22);
            //this.AutofireControlsTab.Name = "AutofireControlsTab";
            this.AutofireControlsTab.Padding = new Padding(3);
            //this.AutofireControlsTab.Size = new System.Drawing.Size(554, 478);
            //this.AutofireControlsTab.TabIndex = 1;
            this.AutofireControlsTab.Text = "Autofire Controls";
            //this.AutofireControlsTab.UseVisualStyleBackColor = true;
            // 
            // AnalogControlsTab
            // 
            //this.AnalogControlsTab.Location = new System.Drawing.Point(4, 22);
            //this.AnalogControlsTab.Name = "AnalogControlsTab";
            //this.AnalogControlsTab.Size = new System.Drawing.Size(554, 478);
            //this.AnalogControlsTab.TabIndex = 2;
            this.AnalogControlsTab.Text = "Analog Controls";
            //this.AnalogControlsTab.UseVisualStyleBackColor = true;
            // 
            // checkBoxAutoTab
            // 
            //this.checkBoxAutoTab.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left)));
            //this.checkBoxAutoTab.AutoSize = true;
            //this.checkBoxAutoTab.Location = new System.Drawing.Point(394, 548);
            //this.checkBoxAutoTab.Name = "checkBoxAutoTab";
            this.checkBoxAutoTab.Size = new Size(70, 17);
            //this.checkBoxAutoTab.TabIndex = 3;
            this.checkBoxAutoTab.Text = "Auto Tab";
            //this.checkBoxAutoTab.UseVisualStyleBackColor = true;
            this.checkBoxAutoTab.CheckedChanged += this.CheckBoxAutoTab_CheckedChanged;
            // 
            // checkBoxUDLR
            // 
            //this.checkBoxUDLR.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left)));
            //this.checkBoxUDLR.AutoSize = true;
            //this.checkBoxUDLR.Location = new Point(470, 548);
            //this.checkBoxUDLR.Name = "checkBoxUDLR";
            this.checkBoxUDLR.Size = new Size(101, 17);
            //this.checkBoxUDLR.TabIndex = 4;
            this.checkBoxUDLR.Text = "Allow U+D/L+R";
            //this.checkBoxUDLR.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            //this.buttonOK.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            //this.buttonOK.Location = new Point(764, 542);
            //this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new Size(75, 23);
            //this.buttonOK.TabIndex = 5;
            this.buttonOK.Text = "&Save";
            //this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += this.ButtonOk_Click;
            // 
            // buttonCancel
            // 
            //this.buttonCancel.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            //this.buttonCancel.DialogResult = DialogResult.Cancel;
            //this.buttonCancel.Location = new Point(845, 542);
            //this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new Size(75, 23);
            //this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "&Cancel";
            //this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += this.ButtonCancel_Click;
            // 
            // pictureBox1
            // 
            //this.pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            //this.pictureBox1.Dock = DockStyle.Fill;
            //this.pictureBox1.Location = new Point(571, 23);
            //this.pictureBox1.Margin = new Padding(3, 23, 3, 3);
            //this.pictureBox1.Name = "pictureBox1";
            //this.pictureBox1.TabIndex = 2;
            //this.pictureBox1.TabStop = false;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new MenuItem[] {
            this.testToolStripMenuItem,
            this.loadDefaultsToolStripMenuItem,
            this.clearToolStripMenuItem});
            //this.contextMenuStrip1.Name = "contextMenuStrip1";
            // 
            // testToolStripMenuItem
            // 
            //this.testToolStripMenuItem.Name = "testToolStripMenuItem";
            //this.testToolStripMenuItem.Size = new Size(146, 22);
            this.testToolStripMenuItem.Text = "Save Defaults";
            this.testToolStripMenuItem.Click += this.ButtonSaveDefaults_Click;
            // 
            // loadDefaultsToolStripMenuItem
            // 
            //this.loadDefaultsToolStripMenuItem.Name = "loadDefaultsToolStripMenuItem";
            //this.loadDefaultsToolStripMenuItem.Size = new Size(146, 22);
            this.loadDefaultsToolStripMenuItem.Text = "Load Defaults";
            this.loadDefaultsToolStripMenuItem.Click += this.ButtonLoadDefaults_Click;
            // 
            // clearToolStripMenuItem
            // 
            //this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            //this.clearToolStripMenuItem.Size = new Size(146, 22);
            this.clearToolStripMenuItem.Text = "Clear";
            this.clearToolStripMenuItem.Click += this.ClearBtn_Click;
            // 
            // label3
            // 
            //this.label3.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left)));
            //this.label3.AutoSize = true;
            //this.label3.Location = new Point(11, 550);
            //this.label3.Name = "label3";
            //this.label3.Size = new Size(30, 13);
            //this.label3.TabIndex = 112;
            this.label3.Text = "Tips:";
            // 
            // label2
            // 
            //this.label2.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left)));
            //this.label2.AutoSize = true;
            //this.label2.Location = new Point(206, 550);
            //this.label2.Name = "label2";
            //this.label2.Size = new Size(168, 13);
            //this.label2.TabIndex = 111;
            this.label2.Text = "* Disable Auto Tab to multiply bind";
            // 
            // label38
            // 
            //this.label38.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left)));
            //this.label38.AutoSize = true;
            //this.label38.Location = new Point(47, 550);
            //this.label38.Name = "label38";
            //this.label38.Size = new Size(153, 13);
            //this.label38.TabIndex = 110;
            this.label38.Text = "* Escape clears a key mapping";
            // 
            // btnMisc
            // 
            //this.btnMisc.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            //this.btnMisc.Location = new Point(683, 542);
            //this.btnMisc.Menu = this.contextMenuStrip1;
            //this.btnMisc.Name = "btnMisc";
            //this.btnMisc.Size = new Size(75, 23);
            //this.btnMisc.TabIndex = 11;
            this.btnMisc.Text = "Misc...";
            //this.btnMisc.UseVisualStyleBackColor = true;
            // 
            // ControllerConfig
            // 
            //this.AcceptButton = this.buttonOK;
            //this.AutoScaleDimensions = new SizeF(6F, 13F);
            //this.AutoScaleMode = AutoScaleMode.Font;
            //this.CancelButton = this.buttonCancel;
            this.ClientSize = new Size(920, 560);
            /*this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label38);
            this.Controls.Add(this.btnMisc);
            this.Controls.Add(this.checkBoxUDLR);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.checkBoxAutoTab);*/
            //this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            //this.Name = "ControllerConfig";
            //this.StartPosition = FormStartPosition.CenterParent;
            this.Title = "Controller Config";
            this.Load += this.NewControllerConfig_Load;
            //this.tabControl1.ResumeLayout(false);
            //this.tableLayoutPanel1.ResumeLayout(false);
            //((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            //this.contextMenuStrip1.ResumeLayout();
            this.ResumeLayout();
            //this.PerformLayout();

            dlTipsChecks = new DynamicLayout();
            dlTipsChecks.BeginHorizontal();
            dlTipsChecks.Add(label3);
            dlTipsChecks.Add(label38);
            dlTipsChecks.Add(label2);
            dlTipsChecks.Add(checkBoxAutoTab);
            dlTipsChecks.Add(checkBoxUDLR);
            dlTipsChecks.EndHorizontal();

            DynamicLayout dlgButtons = new DynamicLayout ();
            dlgButtons.BeginHorizontal ();
            dlgButtons.Add(btnMisc);
            dlgButtons.Add(buttonOK);
            dlgButtons.Add(buttonCancel);
            dlgButtons.EndHorizontal ();

            dlMainLayout = new DynamicLayout();
            dlMainLayout.BeginHorizontal(true);
            dlMainLayout.Add(tabControl1,true);
            dlMainLayout.Add(pictureBox1);
            dlMainLayout.EndHorizontal();
            dlMainLayout.BeginHorizontal();
            dlMainLayout.Add(dlTipsChecks,true);
            dlMainLayout.Add (dlgButtons);
            dlMainLayout.EndHorizontal();
            Content = dlMainLayout;
        }

        #endregion

        private TabControl tabControl1;
        private TabPage NormalControlsTab;
        private TabPage AutofireControlsTab;
        private CheckBox checkBoxAutoTab;
        private CheckBox checkBoxUDLR;
        private Button buttonOK;
        private Button buttonCancel;
        private ImageView pictureBox1;
        private TabPage AnalogControlsTab;
        private ContextMenu contextMenuStrip1;
        private Button btnMisc;
        private ButtonMenuItem testToolStripMenuItem;
        private ButtonMenuItem loadDefaultsToolStripMenuItem;
        private ButtonMenuItem clearToolStripMenuItem;
        private Label label3;
        private Label label2;
        private Label label38;
        private DynamicLayout dlMainLayout;
        private DynamicLayout dlTipsChecks;
    }
}
