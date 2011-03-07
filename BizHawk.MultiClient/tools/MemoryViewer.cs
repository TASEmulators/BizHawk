using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    public class MemoryViewer : GroupBox
    {
        //TODO: 4 byte

        public VScrollBar vScrollBar1;
        MemoryDomain Domain = new MemoryDomain("NULL", 1, Endian.Little, addr => 0, (a, v) => { });
        Font font = new Font("Courier New", 10);
        Brush regBrush = Brushes.Black;
        int RowsVisible = 0;
        int DataSize = 1;
        public bool BigEndian = false;
        string Header = "";

        public MemoryViewer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.MemoryViewer_Paint);
            this.vScrollBar1 = new VScrollBar();
            
            Point n = new Point(this.Size);
            this.vScrollBar1.Location = new System.Drawing.Point(n.X-18, n.Y-this.Height+7);
            this.vScrollBar1.Height = this.Height-8;
            this.vScrollBar1.Width = 16;
            this.vScrollBar1.Visible = true;
            this.vScrollBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                       | System.Windows.Forms.AnchorStyles.Right)));
            this.vScrollBar1.LargeChange = 16;
            this.vScrollBar1.Name = "vScrollBar1";
            this.vScrollBar1.TabIndex = 0;
            this.vScrollBar1.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScrollBar1_Scroll);

            this.Controls.Add(this.vScrollBar1);
        }

        //protected unsafe override void OnPaint(PaintEventArgs e)
        private void Display(Graphics g)
        {
            unchecked
            {
                int row = 0;
                int rowX = 8;
                int rowY = 16;
                int rowYoffset = 20;
                string rowStr = "";
                int addr = 0;
                int aOffset = (GetNumDigits(Domain.Size) % 4) * 9 ;
                g.DrawLine(new Pen(regBrush), this.Left + 38 + aOffset, this.Top, this.Left + 38 + aOffset, this.Bottom - 40);
                g.DrawLine(new Pen(regBrush), this.Left, 34, this.Right - 16, 34);

                for (int i = 0; i < RowsVisible; i++)
                {
                    row = i + vScrollBar1.Value;
                    rowStr = String.Format("{0:X" + GetNumDigits(Domain.Size) + "}", row * 16) + "  "; //TODO: fix offsets on vertical line & digits if > 4
                    switch (DataSize)
                    {
                        default:
                        case 1:
                            Header = "       0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F";
                            for (int j = 0; j < 16; j++)
                            {
                                addr = (row * 16) + j;
                                if (addr < Domain.Size)
                                    rowStr += String.Format("{0:X2}", Domain.PeekByte(addr)) + " ";
                            }
                            break;
                        case 2:
                            Header = "         0    2    4    6    8    A    C    E";
                            for (int j = 0; j < 16; j+=2)
                            {
                                addr = (row * 16) + j;
                                if (addr < Domain.Size)
                                    rowStr += String.Format("{0:X4}", MakeValue(addr, DataSize, BigEndian)) + " ";
                            }
                            break;
                        case 4:
                            break;

                    }

                    g.DrawString(Header, font, regBrush, new Point(rowX + aOffset, rowY));
                    if (row * 16 < Domain.Size)
                        g.DrawString(rowStr, font, regBrush, new Point(rowX, (rowY * (i + 1)) + rowYoffset));
                }
            }
        }

        private int MakeValue(int addr, int size, bool Bigendian)
        {
            int x = 0;
            if (size == 1 || size == 2 || size == 4)
            {
                switch (size)
                {
                    case 1:
                        x = Domain.PeekByte(addr);
                        break;
                    case 2:
                        if (Bigendian)
                        {
                            x = Domain.PeekByte(addr) + (Domain.PeekByte(addr + 1) * 255);
                        }
                        else
                        {
                            x = (Domain.PeekByte(addr) * 255) + Domain.PeekByte(addr + 1);
                        }
                        break;
                    case 3:
                        if (Bigendian)
                        {
                            //TODO
                        }
                        else
                        {
                        }
                        break;
                }
                return x;
            }
            else
                return 0; //fail
        }

        public void ResetScrollBar()
        {
            vScrollBar1.Value = 0;
            SetUpScrollBar();
            Refresh();
        }

        public void SetUpScrollBar()
        {
            RowsVisible = this.Height / 16;
            int totalRows = Domain.Size / 16;
            int MaxRows = (totalRows - RowsVisible) + 17;

            if (MaxRows > 0)
            {
                vScrollBar1.Visible = true;
                if (vScrollBar1.Value > MaxRows)
                    vScrollBar1.Value = MaxRows;
                vScrollBar1.Maximum = MaxRows;
            }
            else
                vScrollBar1.Visible = false;

        }

        public void SetMemoryDomain(MemoryDomain d)
        {
            Domain = d;
            SetUpScrollBar();
            vScrollBar1.Value = 0;
            Refresh();
        }

        public string GetMemoryDomainStr()
        {
            return Domain.ToString();
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            this.SetUpScrollBar();
            this.Refresh();
        }

        private void MemoryViewer_Paint(object sender, PaintEventArgs e)
        {
            Display(e.Graphics);
        }

        public void SetDataSize(int size)
        {
            if (size == 1 || size == 2 || size == 4)
                DataSize = size;
        }

        public int GetDataSize()
        {
            return DataSize;
        }

        private int GetNumDigits(Int32 i)
        {
            if (i <= 0x10000) return 4;
            if (i <= 0x1000000) return 6;
            else return 8;
        }
    }
}
