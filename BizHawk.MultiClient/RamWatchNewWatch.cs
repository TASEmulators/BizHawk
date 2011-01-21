using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace BizHawk.MultiClient
{
    public partial class RamWatchNewWatch : Form
    {
        public Watch watch = new Watch();
        public bool userSelected = false;

        public RamWatchNewWatch()
        {
            InitializeComponent();
        }

        public void SetToEditWatch(Watch w)
        {
            //Sets this dialog to Edit Watch and receives default values
            this.Text = "Edit Watch";
            
            AddressBox.Text = string.Format("{0:X4}", w.address);
            NotesBox.Text = w.notes;

            switch (w.type)
            {
                case atype.BYTE:
                    Byte1Radio.Checked = true;
                    break;
                case atype.WORD:
                    Byte2Radio.Checked = true;
                    break;
                case atype.DWORD:
                    Byte4Radio.Checked = true;
                    break;
                default:
                    break;
            }

            switch (w.signed)
            {
                case asigned.SIGNED:
                    SignedRadio.Checked = true;
                    break;
                case asigned.UNSIGNED:
                    UnsignedRadio.Checked = true;
                    break;
                case asigned.HEX:
                    HexRadio.Checked = true;
                    break;
                default:
                    break;
            }

            if (w.bigendian == true)
                BigEndianRadio.Checked = true;
            else
                LittleEndianRadio.Checked = true;
        }

        private void RamWatchNewWatch_Load(object sender, EventArgs e)
        {

        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            userSelected = false;
            this.Close();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            //Put user settings in the watch file
            userSelected = true;

            watch.address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);

            if (SignedRadio.Checked)
                watch.signed = asigned.SIGNED;
            else if (UnsignedRadio.Checked)
                watch.signed = asigned.UNSIGNED;
            else if (HexRadio.Checked)
                watch.signed = asigned.HEX;

            if (Byte1Radio.Checked)
                watch.type = atype.BYTE;
            else if (Byte2Radio.Checked)
                watch.type = atype.WORD;
            else if (Byte4Radio.Checked)
                watch.type = atype.DWORD;

            if (BigEndianRadio.Checked)
                watch.bigendian = true;
            else if (LittleEndianRadio.Checked)
                watch.bigendian = false;

            watch.notes = NotesBox.Text;

            this.Close();
        }
    }
}
