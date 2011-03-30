using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
    public partial class NESNameTableViewer : Form
    {
        int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;
        NES Nes;

        public NESNameTableViewer()
        {
            InitializeComponent();
            Closing += (o, e) => SaveConfigSettings();
        }

        private void SaveConfigSettings()
        {
            Global.Config.NESNameTableWndx = this.Location.X;
            Global.Config.NESNameTableWndy = this.Location.Y;
        }

        public void UpdateValues()
        {
            int NTAddr;
            int AttributeAddr;
            if (!(Global.Emulator is NES)) return;
            if (!this.IsHandleCreated || this.IsDisposed) return;

            for (int table = 0; table < 4; table++)
            {
                for (int y = 0; y < 30; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        NTAddr = (y * 32) + x;
                        AttributeAddr = 0x3C0 + ((y >> 2) << 3) + (x >> 2);


                        for (int i = 0; i < 8; i++)
                        {
                            for (int j = 0; j < 8; j++)
                            {
                                int cvalue = Nes.LookupColor(Nes.ppu.PALRAM[5]);

                                Color color = Color.FromArgb(cvalue);
                                this.NameTableView.nametables.SetPixel((x * 8) + i, (y * 8) + j, color);
                            }
                        }
                    }
                }
            }
            NameTableView.Refresh();
        }

        public void Restart()
        {
            if (!(Global.Emulator is NES)) this.Close();
            Nes = Global.Emulator as NES;
        }

        private void NESNameTableViewer_Load(object sender, EventArgs e)
        {
            defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = this.Size.Height;

            if (Global.Config.NESNameTableSaveWindowPosition && Global.Config.NESNameTableWndx >= 0 && Global.Config.NESNameTableWndy >= 0)
                this.Location = new Point(Global.Config.NESNameTableWndx, Global.Config.NESNameTableWndy);

            Nes = Global.Emulator as NES;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.AutoLoadNESNameTable ^= true;
        }

        private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.NESNameTableSaveWindowPosition ^= true;
        }

        private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            autoloadToolStripMenuItem.Checked = Global.Config.AutoLoadNESNameTable;
            saveWindowPositionToolStripMenuItem.Checked = Global.Config.NESNameTableSaveWindowPosition;
        }
    }
}
