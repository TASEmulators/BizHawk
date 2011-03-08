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
            PaletteView.Refresh();
        }

        private void NESPPU_Load(object sender, EventArgs e)
        {
            LoadConfigSettings();
            Nes = Global.Emulator as NES;
        }
    }
}
