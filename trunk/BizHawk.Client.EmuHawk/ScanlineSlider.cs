using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
    public partial class ScanlineSlider : Form
    {
        public ScanlineSlider()
        {
            InitializeComponent();
        }

        private void scanlinetrackbar_Scroll(object sender, EventArgs e)
        {
            label1.Text = scanlinetrackbar.Value.ToString() + "%";
            Global.Config.TargetScanlineFilterIntensity = 256 - (256 * scanlinetrackbar.Value / 100);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ScanlineSlider_Load(object sender, EventArgs e)
        {
            float intensity = (256 - (float)Global.Config.TargetScanlineFilterIntensity) / 256 * 100;
            scanlinetrackbar.Value = (int)intensity;
            label1.Text = scanlinetrackbar.Value.ToString() + "%";
        }

    }
}
