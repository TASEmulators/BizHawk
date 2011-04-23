using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
    public partial class MessageConfig : Form
    {
        //TODO: 
        //Implement message position as a variable
        //Make a checkbox to enable/disable the stacking effect of message label
        //Deal with typing into Numerics properly

        int DispFPSx = Global.Config.DispFPSx;
        int DispFPSy = Global.Config.DispFPSy;
        int DispFrameCx = Global.Config.DispFrameCx;
        int DispFrameCy = Global.Config.DispFrameCy;
        int DispLagx = Global.Config.DispLagx;
        int DispLagy = Global.Config.DispLagy;
        int DispInpx = Global.Config.DispInpx;
        int DispInpy = Global.Config.DispInpy;
        int MessageColor = Global.Config.MessagesColor;

        public Brush brush = Brushes.Black;
        int px = 0;
        int py = 0;
        bool mousedown = false;

        public MessageConfig()
        {
            InitializeComponent();
        }

        private void MessageConfig_Load(object sender, EventArgs e)
        {
            SetMaxXY();
            MessageColorDialog.Color = Color.FromArgb(MessageColor);
            SetColorBox();
            SetPositionInfo();
        }

        private void SetMaxXY()
        {
            XNumeric.Maximum = 500; //TODO: set by platform
            YNumeric.Maximum = 500; //TODO: set by platform
            //Set PositionPanel size, and group box that contains it, and dialog size if necessary
        }

        private void SetColorBox()
        {
            MessageColor = MessageColorDialog.Color.ToArgb();
            ColorPanel.BackColor = MessageColorDialog.Color;
            ColorText.Text =  String.Format("{0:X8}", MessageColor);
        }

        private void SetPositionInfo()
        {
            if (FPSRadio.Checked)
            {
                XNumeric.Value = DispFPSx;
                YNumeric.Value = DispFPSy;
                px = DispFPSx;
                py = DispFPSy;
            }
            else if (FrameCounterRadio.Checked)
            {
                XNumeric.Value = DispFrameCx;
                YNumeric.Value = DispFrameCy;
                px = DispFrameCx;
                py = DispFrameCy;
            }
            else if (LagCounterRadio.Checked)
            {
                XNumeric.Value = DispLagx;
                YNumeric.Value = DispLagy;
                px = DispLagx;
                py = DispLagy;
            }
            else if (InputDisplayRadio.Checked)
            {
                XNumeric.Value = DispInpx;
                XNumeric.Value = DispInpy;
                px = DispInpx;
                py = DispInpy;
            }
            else if (MessagesRadio.Checked)
            {
                XNumeric.Value = 0;
                YNumeric.Value = 0;
                px = 0;
                py = 0;
            }

            PositionPanel.Refresh();
            SetPositionLabels();
        }

        private void SaveSettings()
        {
            Global.Config.DispFPSx = DispFPSx;
            Global.Config.DispFPSy = DispFPSy;
            Global.Config.DispFrameCx = DispFrameCx;
            Global.Config.DispFrameCy = DispFrameCy;
            Global.Config.DispLagx = DispLagx;
            Global.Config.DispLagy = DispLagy;
            Global.Config.DispInpx = DispInpx;
            Global.Config.DispInpy = DispInpy;
            Global.Config.MessagesColor = MessageColor;
        }

        private void OK_Click(object sender, EventArgs e)
        {
            SaveSettings();
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageColorDialog.ShowDialog() == DialogResult.OK)
                SetColorBox();
        }

        private void FPSRadio_CheckedChanged(object sender, EventArgs e)
        {
            SetPositionInfo();
        }

        private void FrameCounterRadio_CheckedChanged(object sender, EventArgs e)
        {
            SetPositionInfo();
        }

        private void LagCounterRadio_CheckedChanged(object sender, EventArgs e)
        {
            SetPositionInfo();
        }

        private void InputDisplayRadio_CheckedChanged(object sender, EventArgs e)
        {
            SetPositionInfo();
        }

        private void MessagesRadio_CheckedChanged(object sender, EventArgs e)
        {
            SetPositionInfo();
        }

        private void XNumericChange()
        {
            px = (int)XNumeric.Value;
            SetPositionLabels();
            PositionPanel.Refresh();
        }

        private void YNumericChange()
        {
            py = (int)YNumeric.Value;
            SetPositionLabels();
            PositionPanel.Refresh();
        }

        private void XNumeric_ValueChanged(object sender, EventArgs e)
        {
            XNumericChange();
        }

        private void YNumeric_ValueChanged(object sender, EventArgs e)
        {
            YNumericChange();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void PositionPanel_MouseEnter(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void PositionPanel_MouseLeave(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void PositionPanel_Paint(object sender, PaintEventArgs e)
        {
            Pen p = new Pen(brush);
            e.Graphics.DrawLine(p, new Point(px - 2, py - 2), new Point(px + 2, py + 2));
            e.Graphics.DrawLine(p, new Point(px + 2, py - 2), new Point(px - 2, py + 2));
        }

        private void PositionPanel_MouseDown(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
            mousedown = true;
            SetNewPosition(e.X, e.Y);
        }

        private void PositionPanel_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
            mousedown = false;
        }

        private void SetNewPosition(int mx, int my)
        {
            if (mx < 0) mx = 0;
            if (my < 0) my = 0;
            if (mx > XNumeric.Maximum) mx = (int)XNumeric.Maximum;
            if (my > YNumeric.Maximum) my = (int)YNumeric.Maximum;
            XNumeric.Value = mx;
            YNumeric.Value = my;
            px = mx;
            py = my;
            PositionPanel.Refresh();
            SetPositionLabels();
        }

        private void PositionPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (mousedown)
            {
                SetNewPosition(e.X, e.Y);
            }
        }

        private void SetPositionLabels()
        {
            if (FPSRadio.Checked)
            {
                DispFPSx = px;
                DispFPSy = py;
            }
            else if (FrameCounterRadio.Checked)
            {
                DispFrameCx = px;
                DispFrameCy = py;
            }
            else if (LagCounterRadio.Checked)
            {
                DispLagx = px;
                DispLagy = py;
            }
            else if (InputDisplayRadio.Checked)
            {
                DispInpx = px;
                DispInpy = py;
            }
            else if (MessagesRadio.Checked)
            {
                //TODO
            }
            FpsPosLabel.Text = DispFPSx.ToString() + ", " + DispFPSy.ToString();
            FCLabel.Text = DispFrameCx.ToString() + ", " + DispFrameCy.ToString();
            LagLabel.Text = DispLagx.ToString() + ", " + DispLagy.ToString();
            InpLabel.Text = DispInpx.ToString() + ", " + DispInpy.ToString();
            MessLabel.Text = "0, 0";
        }

        private void ResetDefaultsButton_Click(object sender, EventArgs e)
        {
            Global.Config.DispFPSx = 0;
            Global.Config.DispFPSy = 0;
            Global.Config.DispFrameCx = 0;
            Global.Config.DispFrameCy = 12;
            Global.Config.DispLagx = 0;
            Global.Config.DispLagy = 36;
            Global.Config.DispInpx = 0;
            Global.Config.DispInpy = 24;
            Global.Config.MessagesColor = -1;

            DispFPSx = Global.Config.DispFPSx;
            DispFPSy = Global.Config.DispFPSy;
            DispFrameCx = Global.Config.DispFrameCx;
            DispFrameCy = Global.Config.DispFrameCy;
            DispLagx = Global.Config.DispLagx;
            DispLagy = Global.Config.DispLagy;
            DispInpx = Global.Config.DispInpx;
            DispInpy = Global.Config.DispInpy;
            MessageColor = Global.Config.MessagesColor;


            SetMaxXY();
            MessageColorDialog.Color = Color.FromArgb(MessageColor);
            SetColorBox();
            SetPositionInfo();
        }

        private void ColorPanel_DoubleClick(object sender, EventArgs e)
        {
            if (MessageColorDialog.ShowDialog() == DialogResult.OK)
                SetColorBox();
        }
    }
}
