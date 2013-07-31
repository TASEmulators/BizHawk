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

            //FDS
            CheckFile(Global.Config.FilenameFDSBios, Disksys_ROM_PicBox);

            //SNES
            CheckFile("cx4.rom", CX4_PicBox);
            CheckFile("dsp1b.rom", DSP1B_ROM_PicBox);
            CheckFile("dsp2.rom", DSP2_ROM_PicBox);
            CheckFile("dsp3.rom", DSP3_ROM_PicBox);
            CheckFile("dsp4.rom", DSP4_ROM_PicBox);
            CheckFile("st010.rom", ST010_ROM_PicBox);
            CheckFile("st011.rom", ST011_ROM_PicBox);
            CheckFile("st018.rom", ST018_ROM_PicBox);

            //SGB
            CheckFile("sgb.sfc", SGB_SFC_PicBox);

            //PCE
            //CheckFile(Global.Config.Path
        }

        private void CheckFile(string filename, PictureBox pic)
        {
            FileInfo file = new FileInfo(Path.Combine(Global.Config.FirmwaresPath, filename));
            if (file.Exists) pic.Image = MultiClient.Properties.Resources.GreenCheck; else pic.Image = MultiClient.Properties.Resources.ExclamationRed;
        }
    }
}
