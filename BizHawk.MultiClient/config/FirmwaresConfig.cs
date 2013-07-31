using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
    public partial class FirmwaresConfig : Form
    {
        //TODO: commodore 64, intellivision



        public FirmwaresConfig()
        {
            InitializeComponent();
        }

        private void CloseBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FirmwaresConfig_Load(object sender, EventArgs e)
        {
            DoScan();
        }

        private void ScanBtn_Click(object sender, EventArgs e)
        {
            DoScan();
        }

        private void DoScan()
        {
            string p = Global.Config.FirmwaresPath;
            FileInfo file;

            file = new FileInfo(Path.Combine(p, Global.Config.FilenameFDSBios));
            if (file.Exists) Disksys_ROM_PicBox.Image = MultiClient.Properties.Resources.GreenCheck; else Disksys_ROM_PicBox.Image = MultiClient.Properties.Resources.ExclamationRed;
        }
    }
}
