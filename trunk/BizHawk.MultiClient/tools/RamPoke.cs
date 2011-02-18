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
    public partial class RamPoke : Form
    {
        //TODO:
        //If signed/unsigned/hex radios selected, auto-change the value box
        //Checked signed/u/h value on RamPoke_Load and set value appopriately
        public Watch watch = new Watch();
        public Point location = new Point();

        public RamPoke()
        {
            InitializeComponent();
        }

        public void SetWatchObject(Watch w)
        {
            watch = w;
        }

        private void RamPoke_Load(object sender, EventArgs e)
        {
            SetTypeRadio(watch.type);
            SetSignedRadio(watch.signed);
            if (watch.bigendian == true)
                BigEndianRadio.Checked = true;
            else
                LittleEndianRadio.Checked = true;
            AddressBox.Text = String.Format("{0:X}", watch.address);

            
            ValueBox.Text = watch.value.ToString();
            
            
            if (location.X > 0 && location.Y > 0)
                this.Location = location;
            
        }

        private void SetTypeRadio(atype a)
        {
            switch (a)
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
        }

        private void SetSignedRadio(asigned a)
        {
            switch (a)
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
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            //Put user settings in the watch file

            if (InputValidate.IsValidHexNumber(AddressBox.Text))    //TODO: also validate it is within a valid memory address range!
                watch.address = int.Parse(AddressBox.Text, NumberStyles.HexNumber);
            else
            {
                MessageBox.Show("Invalid Address, must be a valid hex number", "Invalid Address", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AddressBox.Focus();
                AddressBox.SelectAll();
                return;
            }
            
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

            switch (watch.signed)
            {
                case asigned.UNSIGNED:
                    if (InputValidate.IsValidUnsignedNumber(ValueBox.Text))
                    {
                        watch.value = int.Parse(ValueBox.Text);
                    }
                    else
                    {
                        MessageBox.Show("Invalid Address, must be a valid number number", "Invalid Address", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ValueBox.Focus();
                        ValueBox.SelectAll();
                        return;
                    }
                    break;
                case asigned.SIGNED:
                    if (InputValidate.IsValidSignedNumber(ValueBox.Text))
                    {
                        watch.value = int.Parse(ValueBox.Text);
                    }
                    else
                    {
                        MessageBox.Show("Invalid Address, must be a valid number number", "Invalid Address", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ValueBox.Focus();
                        ValueBox.SelectAll();
                    }
                    break;
                case asigned.HEX:
                    if (InputValidate.IsValidHexNumber(ValueBox.Text))
                    {
                        watch.value = int.Parse(ValueBox.Text);
                    }
                    else
                    {
                        MessageBox.Show("Invalid Address, must be a valid hex number", "Invalid Address", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ValueBox.Focus();
                        ValueBox.SelectAll();
                        return;
                    }
                    break;
                default:
                return;
        }

            //TODO: Try/Catch this? Seems destined for failures

            switch (watch.type)
            {
                case atype.BYTE:
                    Global.Emulator.MainMemory.PokeByte(watch.address, (byte)watch.value);
                    break;
                case atype.WORD:
                    if (watch.bigendian)
                    {
                        Global.Emulator.MainMemory.PokeByte(watch.address, (byte)(watch.value / 256));
                        Global.Emulator.MainMemory.PokeByte(watch.address + 1, (byte)(watch.value % 256));
                    }
                    else
                    {
                        Global.Emulator.MainMemory.PokeByte(watch.address + 1, (byte)(watch.value / 256));
                        Global.Emulator.MainMemory.PokeByte(watch.address, (byte)(watch.value % 256));
                    }
                    break;
                case atype.DWORD:
                    //TODO
                    break;
                default:
                    break;
            }

            
            //TODO: format value based on watch.type
            OutputLabel.Text = watch.value.ToString() + " written to " + String.Format("{0:X}", watch.address);
        }
    }
}
