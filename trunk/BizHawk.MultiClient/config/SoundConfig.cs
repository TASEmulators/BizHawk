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
    public partial class SoundConfig : Form
    {
        public SoundConfig()
        {
            InitializeComponent();
        }

        private void SoundConfig_Load(object sender, EventArgs e)
        {
            SoundOnCheckBox.Checked = Global.Config.SoundEnabled;
            MuteFrameAdvance.Checked = Global.Config.MuteFrameAdvance;
        }

        private void OK_Click(object sender, EventArgs e)
        {
            Global.Config.SoundEnabled = SoundOnCheckBox.Checked;
            Global.Config.MuteFrameAdvance = MuteFrameAdvance.Checked;
			Global.Sound.StartSound();
            this.Close();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
