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
        public MessageConfig()
        {
            InitializeComponent();
        }

        private void MessageConfig_Load(object sender, EventArgs e)
        {
            MessageColorDialog.Color = Color.FromArgb(Global.Config.MessagesColor);
            SetColorBox();
            SetPositionInfo();
            SetMaxXY();
        }

        private void SetMaxXY()
        {
            //set by platform
        }

        private void SetColorBox()
        {
            MessageColorBox.BackColor = MessageColorDialog.Color;
        }

        private void SetPositionInfo()
        {
            if (FPSRadio.Checked)
            {
            }
            else if (FrameCounterRadio.Checked)
            {
            }
            else if (LagCounterRadio.Checked)
            {
            }
            else if (InputDisplayRadio.Checked)
            {
            }
            else if (MessagesRadio.Checked)
            {
            }
        }

        private void OK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageColorDialog.ShowDialog();
        }
    }
}
