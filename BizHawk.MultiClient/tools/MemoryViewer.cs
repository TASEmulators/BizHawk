using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    public class MemoryViewer : Panel
    {
        public VScrollBar vScrollBar1;
        public Label info;

        MemoryDomain Domain = new MemoryDomain("NULL", 1, Endian.Little, addr => 0, (a, v) => { });
        Font font = new Font("Courier New", 10);
        public Brush regBrush = Brushes.Black;
        public Brush highlightBrush = Brushes.LightBlue;
        int RowsVisible = 0;
        int DataSize = 1;
        public bool BigEndian = false;
        string Header = "";

        int addressHighlighted = -1;
        int addressOver = -1;
        int addrOffset = 0;     //If addresses are > 4 digits, this offset is how much the columns are moved to the right
        int maxRow = 0;

        public MemoryViewer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.MemoryViewer_Paint);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MemoryViewer_MouseMove);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MemoryViewer_MouseClick);

            this.vScrollBar1 = new VScrollBar();
            Point n = new Point(this.Size);
            this.vScrollBar1.Location = new System.Drawing.Point(n.X-16, n.Y-this.Height+7);
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

            //Debugging control
            this.info = new Label();
            this.info.Text = "";
            this.info.Font = new Font("Courier New", 8);
            this.info.Location = new System.Drawing.Point(n.X / 2, 1);
            this.info.Height = 11;
            this.Controls.Add(this.info);
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
                addrOffset = (GetNumDigits(Domain.Size) % 4) * 9 ;
                g.DrawLine(new Pen(regBrush), this.Left + 38 + addrOffset, this.Top, this.Left + 38 + addrOffset, this.Bottom - 40);
                g.DrawLine(new Pen(regBrush), this.Left, 34, this.Right - 16, 34);

                if (addressHighlighted >= 0) //&& visible (determine this)
                {
                    
                    int left = ((addressHighlighted % 16) * 24) + 60 + addrOffset;
                    int top = ((addressHighlighted / 16) * 16) + 36;
                    Rectangle rect = new Rectangle(left, top, 24, 16);
                    g.DrawRectangle(new Pen(highlightBrush), rect);
                    g.FillRectangle(highlightBrush, rect);
                }

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
                            Header = "             0        4        8        C";
                            for (int j = 0; j < 16; j += 4)
                            {
                                addr = (row * 16) + j;
                                if (addr < Domain.Size)
                                    rowStr += String.Format("{0:X8}", MakeValue(addr, DataSize, BigEndian)) + " ";
                            }
                            break;

                    }
                    g.DrawString(Domain.Name, font, regBrush, new Point(1, 1));
                    g.DrawString(Header, font, regBrush, new Point(rowX + addrOffset, rowY));
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
                        x = MakeWord(addr, Bigendian);
                        break;
                    case 4:
                        x = (MakeWord(addr, Bigendian) * 65536) +
                            MakeWord(addr + 2, Bigendian);
                        break;
                }
                return x;
            }
            else
                return 0; //fail
        }

        private int MakeWord(int addr, bool endian)
        {
            if (endian)
                return Domain.PeekByte(addr) + (Domain.PeekByte(addr + 1) * 255);
            else
                return (Domain.PeekByte(addr) * 255) + Domain.PeekByte(addr + 1);
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
            maxRow = Domain.Size / 2;
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

        private void SetAddressOver(int x, int y)
        {
            //info.Text = e.X.ToString() + "," + e.Y.ToString(); //Debug

            //Determine row - 32 pix header, 16 pix width
            //Scroll value determines the first row
            int row = vScrollBar1.Value;
            row += (y - 36) / 16;
            //info.Text += " " + row.ToString(); //Debug

            //Determine colums - 60 + addrOffset left padding
            //24 pixel wide addresses (when 1 byte)
            int column = (x - (60 + addrOffset)) / 24;
            //info.Text += " " + column.ToString(); //Debug
            //TODO: 2 & 4 byte views


            if (row >= 0 && row <= maxRow && column >= 0 && column < 16)
            {
                addressOver = row * 16 + column;
                info.Text = String.Format("{0:X4}", addressOver);
            }
            else
            {
                addressOver = -1;
                info.Text = "";
            }
        }
        
        private void MemoryViewer_MouseMove(object sender, MouseEventArgs e)
        {
            SetAddressOver(e.X, e.Y);
        }

        private void MemoryViewer_MouseClick(object sender, MouseEventArgs e)
        {
            SetAddressOver(e.X, e.Y);
            if (addressOver >= 0)
                addressHighlighted = addressOver;
            else
                addressHighlighted = -1;
        }

        public int GetPointedAddress()
        {
            if (addressOver >= 0)
                return addressOver;
            else
                return -1;  //Negative = no address pointed
        }

        public int GetHighlightedAddress()
        {
            if (addressHighlighted >= 0)
                return addressHighlighted;
            else
                return -1; //Negative = no address highlighted
        }
    }
}
