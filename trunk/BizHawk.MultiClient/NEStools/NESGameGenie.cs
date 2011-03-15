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
    public partial class NESGameGenie : Form
    {
        int address = -1;
        int value = -1;
        int compare = -1;
        Dictionary<char, int> GameGenieTable = new Dictionary<char, int>();

        public NESGameGenie()
        {
            InitializeComponent();
        }

        private void NESGameGenie_Load(object sender, EventArgs e)
        {
            GameGenieTable.Add('A', 0);     //0000
            GameGenieTable.Add('P', 1);     //0001
            GameGenieTable.Add('Z', 2);     //0010
            GameGenieTable.Add('L', 3);     //0011
            GameGenieTable.Add('G', 4);     //0100
            GameGenieTable.Add('I', 5);     //0101
            GameGenieTable.Add('T', 6);     //0110
            GameGenieTable.Add('Y', 7);     //0111
            GameGenieTable.Add('E', 8);     //1000
            GameGenieTable.Add('O', 9);     //1001
            GameGenieTable.Add('X', 10);    //1010
            GameGenieTable.Add('U', 11);    //1011
            GameGenieTable.Add('K', 12);    //1100
            GameGenieTable.Add('S', 13);    //1101
            GameGenieTable.Add('V', 14);    //1110
            GameGenieTable.Add('N', 15);    //1111
        }

        private void GameGenieCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Make uppercase
            if (e.KeyChar > 97 && e.KeyChar < 123)
                e.KeyChar -= (char)32;

            if (!(GameGenieTable.ContainsKey(e.KeyChar))) 
                e.Handled = true;
        }

        private int GetBit(int value, int bit)
        {
            return (value >> bit) & 1;
        }

        private void DecodeGameGenieCode(string code)
        {
            //char 3 bit 3 denotes the code length.
            if (code.Length == 6)
            {
                //Char # |   1   |   2   |   3   |   4   |   5   |   6   |
                //Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
                //maps to|1|6|7|8|H|2|3|4|-|I|J|K|L|A|B|C|D|M|N|O|5|E|F|G|
                value = 0;
                address = 0x8000;
                int x;

                GameGenieTable.TryGetValue(code[0], out x);
                value |= (x & 0x07);
                value |= (x & 0x08) << 4;

                GameGenieTable.TryGetValue(code[1], out x);
                value |= (x & 0x07) << 4;
                address |= (x & 0x08) << 4; 
                
                GameGenieTable.TryGetValue(code[2], out x);
                address |= (x & 0x07) << 4;

                GameGenieTable.TryGetValue(code[3], out x);
                address |= (x & 0x07) << 12;
                address |= (x & 0x08);

                GameGenieTable.TryGetValue(code[4], out x);
                address |= (x & 0x07);
                address |= (x & 0x08) << 8;
                
                GameGenieTable.TryGetValue(code[5], out x);
                address |= (x & 0x07) << 8;
                value |= (x & 0x08);

                SetProperties();

            }
            else if (code.Length == 8)
            {
                //Char # |   1   |   2   |   3   |   4   |   5   |   6   |   7   |   8   |
                //Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
                //maps to|1|6|7|8|H|2|3|4|-|I|J|K|L|A|B|C|D|M|N|O|%|E|F|G|!|^|&|*|5|@|#|$|
                value = 0;
                address = 0x8000;
                compare = 0;
                int x;

                GameGenieTable.TryGetValue(code[0], out x);
                value |= (x & 0x07);
                value |= (x & 0x08) << 4;

                GameGenieTable.TryGetValue(code[1], out x);
                value |= (x & 0x07) << 4;
                address |= (x & 0x08) << 4;

                GameGenieTable.TryGetValue(code[2], out x);
                address |= (x & 0x07) << 4;

                GameGenieTable.TryGetValue(code[3], out x);
                address |= (x & 0x07) << 12;
                address |= (x & 0x08);

                GameGenieTable.TryGetValue(code[4], out x);
                address |= (x & 0x07);
                address |= (x & 0x08) << 8;

                GameGenieTable.TryGetValue(code[5], out x);
                address |= (x & 0x07) << 8;
                compare |= (x & 0x08);

                GameGenieTable.TryGetValue(code[6], out x);
                compare |= (x & 0x07);
                compare |= (x & 0x08) << 4;

                GameGenieTable.TryGetValue(code[7], out x);
                compare |= (x & 0x07) << 4;
                value |= (x & 0x08);
                SetProperties();
            }
        }

        private void SetProperties()
        {
            if (address >= 0)
                AddressBox.Text = String.Format("{0:X4}", address);
            else
                AddressBox.Text = "";

            if (compare >= 0)
                CompareBox.Text = String.Format("{0:X2}", compare);
            else
                CompareBox.Text = "";

            if (value >= 0)
                ValueBox.Text = String.Format("{0:X2}", value);

        }

        private void ClearProperties()
        {
            address = -1;
            value = -1;
            compare = -1;
            AddressBox.Text = "";
            CompareBox.Text = "";
            ValueBox.Text = "";
        }

        private void GameGenieCode_TextChanged(object sender, EventArgs e)
        {
            if (GameGenieCode.Text.Length == 6 || GameGenieCode.Text.Length == 8)
                DecodeGameGenieCode(GameGenieCode.Text);
            else
                ClearProperties();
        }

        private void Keypad_Click(object sender, EventArgs e)
        {
            if (GameGenieCode.Text.Length < 8)
            {
                if (sender == A) GameGenieCode.Text += "A";
                if (sender == P) GameGenieCode.Text += "P";
                if (sender == Z) GameGenieCode.Text += "Z";
                if (sender == L) GameGenieCode.Text += "L";
                if (sender == G) GameGenieCode.Text += "G";
                if (sender == I) GameGenieCode.Text += "I";
                if (sender == T) GameGenieCode.Text += "T";
                if (sender == Y) GameGenieCode.Text += "Y";
                if (sender == E) GameGenieCode.Text += "E";
                if (sender == O) GameGenieCode.Text += "O";
                if (sender == X) GameGenieCode.Text += "X";
                if (sender == U) GameGenieCode.Text += "U";
                if (sender == K) GameGenieCode.Text += "K";
                if (sender == S) GameGenieCode.Text += "S";
                if (sender == V) GameGenieCode.Text += "V";
                if (sender == N) GameGenieCode.Text += "N";
            }
        }

        private void AddressBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            //TODO: IsValidHex
            //if not ignore input
        }

        private void CompareBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            //TODO: IsValidHex
        }

        private void ValueBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            //TODO: IsValidHex
        }

        private void AddressBox_TextChanged(object sender, EventArgs e)
        {
            /*
            int a = int.Parse(AddressBox.Text); //TODO: try/catch just in case?
            if (a >= 0x8000)
            {
                if (ValueBox.Text.Length > 0 && CompareBox.Text.Length > 0)
                {
                    address = a;
                    EncodeGameGenie(); //TODO: check to make sure value & compare are set
                }
            }
            */ //TODO: decoder will change the text and trigger this event, find a way around it
        }

        private void CompareBox_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void ValueBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void EncodeGameGenie()
        {

        }
    }
}
