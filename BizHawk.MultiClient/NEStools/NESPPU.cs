using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
    public partial class NESPPU : Form
    {
        int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;
        NES Nes;

        public NESPPU()
        {
            InitializeComponent();
            Closing += (o, e) => SaveConfigSettings(); 
        }

        private void SaveConfigSettings()
        {
            Global.Config.NESPPUWndx = this.Location.X;
            Global.Config.NESPPUWndy = this.Location.Y;
        }

        public void Restart()
        {
            if (!(Global.Emulator is NES)) this.Close();
            Nes = Global.Emulator as NES;
        }

        private void LoadConfigSettings()
        {
            defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = Size.Height;

            if (Global.Config.NESPPUWndx >= 0 && Global.Config.NESPPUWndy >= 0)
                Location = new Point(Global.Config.NESPPUWndx, Global.Config.NESPPUWndy);
        }

        public void UpdateValues()
        {
            if (!(Global.Emulator is NES)) return;
            if (!this.IsHandleCreated || this.IsDisposed) return;
            
            //Pattern Viewer
            for (int x = 0; x < 16; x++)
            {
				PaletteView.bgPalettes[x].SetValue(Nes.ConvertColor(Nes.ppu.PALRAM[PaletteView.bgPalettes[x].address]));
				PaletteView.spritePalettes[x].SetValue(Nes.ppu.PALRAM[PaletteView.spritePalettes[x].address]);
            }
            PaletteView.Refresh();

            //Pattern Viewer
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        for (int y = 0; y < 8; y++)
                        {
                            int address = (i * 256) + (j * 16) + (x / 4) + (y / 4);
                            byte value;

                            //Incorrectly read the color value for now
                            value = Nes.ppu.ppubus_read(address);
                            value /= 4;
                            /////////////////////////////////////////
                            int cvalue = Nes.ConvertColor(value);
                            unchecked
                            {
                                cvalue = cvalue | (int)0xFF000000;
                            }
                            Color color = Color.FromArgb(cvalue);
                            PatternView.pattern.SetPixel(x + (i*8), y + (j*8), color);
                        }
                    }
                }
            }
            PatternView.Refresh();
        }

        private void NESPPU_Load(object sender, EventArgs e)
        {
            LoadConfigSettings();
            Nes = Global.Emulator as NES;
            ClearDetails();
        }

        private void ClearDetails()
        {
            SectionLabel.Text = "";
            AddressLabel.Text = "";
            ValueLabel.Text = "";
            //TODO: more info labels
        }

        private void PaletteView_MouseLeave(object sender, EventArgs e)
        {
            ClearDetails();
        }

        private void PaletteView_MouseEnter(object sender, EventArgs e)
        {
            SectionLabel.Text = "Section: Palette";
        }

        private void PaletteView_MouseMove(object sender, MouseEventArgs e)
        {
            int baseAddr = 0x3F00;
            if (e.Y > 16)
                baseAddr += 16;
            int column = (e.X - PaletteView.Location.X) / 16;
            int addr = column + baseAddr;
            AddressLabel.Text = "Address: 0x" + String.Format("{0:X4}", addr, NumberStyles.HexNumber);
            int val;
            if (baseAddr == 0x3F00)
                val = PaletteView.bgPalettes[column].GetValue();
            else
                val = PaletteView.spritePalettes[column].GetValue();
            ValueLabel.Text = "Color: 0x" + String.Format("{0:X2}", val, NumberStyles.HexNumber);
        }

        private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.AutoLoadNESPPU ^= true;
        }

        private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.NESPPUSaveWindowPosition ^= true;
        }

        private void toolStripDropDownButton1_DropDownOpened(object sender, EventArgs e)
        {
            autoloadToolStripMenuItem.Checked = Global.Config.AutoLoadNESPPU;
            saveWindowPositionToolStripMenuItem.Checked = Global.Config.NESPPUSaveWindowPosition;
        }
    }
}
