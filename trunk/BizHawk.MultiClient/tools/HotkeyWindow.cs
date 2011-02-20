using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient.tools
{
    public partial class HotkeyWindow : Form
    {
        public HotkeyWindow()
        {
            InitializeComponent();
            IDW_FRAMEADVANCE.Text = Global.Config.FrameAdvanceBinding;
            IDW_PAUSE.Text = Global.Config.EmulatorPauseBinding;
            IDW_HARDRESET.Text = Global.Config.HardResetBinding;
            IDW_REWIND.Text = Global.Config.RewindBinding;
            IDW_FASTFORWARD.Text = Global.Config.FastForwardBinding;

            IDW_QuickSave.Text = Global.Config.QuickSave;
            IDW_QuickLoad.Text = Global.Config.QuickLoad;
            //Save States
            IDW_SS0.Text = Global.Config.SaveSlot0;
            IDW_SS1.Text = Global.Config.SaveSlot1;
            IDW_SS2.Text = Global.Config.SaveSlot2;
            IDW_SS3.Text = Global.Config.SaveSlot3;
            IDW_SS4.Text = Global.Config.SaveSlot4;
            IDW_SS5.Text = Global.Config.SaveSlot5;
            IDW_SS6.Text = Global.Config.SaveSlot6;
            IDW_SS7.Text = Global.Config.SaveSlot7;
            IDW_SS8.Text = Global.Config.SaveSlot8;
            IDW_SS9.Text = Global.Config.SaveSlot9;
            //Load States
            IDW_LS0.Text = Global.Config.LoadSlot0;
            IDW_LS1.Text = Global.Config.LoadSlot1;
            IDW_LS2.Text = Global.Config.LoadSlot2;
            IDW_LS3.Text = Global.Config.LoadSlot3;
            IDW_LS4.Text = Global.Config.LoadSlot4;
            IDW_LS5.Text = Global.Config.LoadSlot5;
            IDW_LS6.Text = Global.Config.LoadSlot6;
            IDW_LS7.Text = Global.Config.LoadSlot7;
            IDW_LS8.Text = Global.Config.LoadSlot8;
            IDW_LS9.Text = Global.Config.LoadSlot9;
            //Select States
            IDW_ST0.Text = Global.Config.SelectSlot0;
            IDW_ST1.Text = Global.Config.SelectSlot1;
            IDW_ST2.Text = Global.Config.SelectSlot2;
            IDW_ST3.Text = Global.Config.SelectSlot3;
            IDW_ST4.Text = Global.Config.SelectSlot4;
            IDW_ST5.Text = Global.Config.SelectSlot5;
            IDW_ST6.Text = Global.Config.SelectSlot6;
            IDW_ST7.Text = Global.Config.SelectSlot7;
            IDW_ST8.Text = Global.Config.SelectSlot8;
            IDW_ST9.Text = Global.Config.SelectSlot9;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void IDB_SAVE_Click(object sender, EventArgs e)
        {

            Global.Config.FastForwardBinding = IDW_FASTFORWARD.Text;
            Global.Config.FrameAdvanceBinding = IDW_FRAMEADVANCE.Text;
            Global.Config.HardResetBinding = IDW_HARDRESET.Text;
            Global.Config.RewindBinding = IDW_REWIND.Text;
            Global.Config.EmulatorPauseBinding = IDW_PAUSE.Text;

            Global.Config.QuickLoad = IDW_QuickLoad.Text;
            Global.Config.QuickSave = IDW_QuickSave.Text;

            Global.Config.SaveSlot0 = IDW_SS0.Text;
            Global.Config.SaveSlot1 = IDW_SS1.Text;
            Global.Config.SaveSlot2 = IDW_SS2.Text;
            Global.Config.SaveSlot3 = IDW_SS3.Text;
            Global.Config.SaveSlot4 = IDW_SS4.Text;
            Global.Config.SaveSlot5 = IDW_SS5.Text;
            Global.Config.SaveSlot6 = IDW_SS6.Text;
            Global.Config.SaveSlot7 = IDW_SS7.Text;
            Global.Config.SaveSlot8 = IDW_SS8.Text;
            Global.Config.SaveSlot9 = IDW_SS9.Text;

            Global.Config.LoadSlot0 = IDW_LS0.Text;
            Global.Config.LoadSlot1 = IDW_LS1.Text;
            Global.Config.LoadSlot2 = IDW_LS2.Text;
            Global.Config.LoadSlot3 = IDW_LS3.Text;
            Global.Config.LoadSlot4 = IDW_LS4.Text;
            Global.Config.LoadSlot5 = IDW_LS5.Text;
            Global.Config.LoadSlot6 = IDW_LS6.Text;
            Global.Config.LoadSlot7 = IDW_LS7.Text;
            Global.Config.LoadSlot8 = IDW_LS8.Text;
            Global.Config.LoadSlot9 = IDW_LS9.Text;

            Global.Config.SelectSlot0 = IDW_ST0.Text;
            Global.Config.SelectSlot1 = IDW_ST1.Text;
            Global.Config.SelectSlot2 = IDW_ST2.Text;
            Global.Config.SelectSlot3 = IDW_ST3.Text;
            Global.Config.SelectSlot4 = IDW_ST4.Text;
            Global.Config.SelectSlot5 = IDW_ST5.Text;
            Global.Config.SelectSlot6 = IDW_ST6.Text;
            Global.Config.SelectSlot7 = IDW_ST7.Text;
            Global.Config.SelectSlot8 = IDW_ST8.Text;
            Global.Config.SelectSlot9 = IDW_ST9.Text;            
            

            this.Close();
        }
       
    }
}
