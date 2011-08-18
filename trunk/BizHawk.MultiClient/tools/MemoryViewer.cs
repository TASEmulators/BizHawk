using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;


namespace BizHawk.MultiClient
{
	public class MemoryViewer : Panel
	{
		//TODO: highlighting and address determining for 2 & 4 byte viewing
		//2 & 4 byte typing in
		//show nibbles on top of highlighted address
		//Multi-highlight
		//different back color for frozen addresses

		public VScrollBar vScrollBar1;
		public Label info;
		MemoryDomain Domain = new MemoryDomain("NULL", 1024, Endian.Little, addr => { return 0; }, (a, v) => { v = 0; });

		Font font = new Font("Courier New", 8);
		public Brush highlightBrush = Brushes.LightBlue;
		int RowsVisible = 0;
		int DataSize = 1;
		public bool BigEndian = false;
		string Header = "";
		char[] nibbles = { 'G', 'G', 'G', 'G' };    //G = off 0-9 & A-F are acceptable values
		int addressHighlighted = -1;
		int addressOver = -1;
		int addrOffset = 0;     //If addresses are > 4 digits, this offset is how much the columns are moved to the right
		int maxRow = 0;

		const int rowX = 12;
		const int rowY = 16;
		const int rowYoffset = 20;

		public bool BlazingFast = false;

		public MemoryViewer()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			this.BorderStyle = BorderStyle.Fixed3D;
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MemoryViewer_MouseMove);
			this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MemoryViewer_MouseClick);
			this.vScrollBar1 = new VScrollBar();
			Point n = new Point(this.Size);
			this.vScrollBar1.Location = new System.Drawing.Point(n.X - 16, n.Y - this.Height + 7);
			this.vScrollBar1.Height = this.Height - 8;
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

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Up)
			{
				GoToAddress(addressHighlighted - 16);
			}

			else if (keyData == Keys.Down)
			{
				GoToAddress(addressHighlighted + 16);
			}

			else if (keyData == Keys.Left)
			{
				GoToAddress(addressHighlighted - 1);
			}

			else if (keyData == Keys.Right)
			{
				GoToAddress(addressHighlighted + 1);
			}

			else if (keyData == Keys.Tab)
			{
				addressHighlighted += 8;
				this.Refresh();
			}

			else if (keyData == Keys.PageDown)
			{
				GoToAddress(addressHighlighted + (RowsVisible * 16));
			}

			else if (keyData == Keys.PageUp)
			{
				GoToAddress(addressHighlighted - (RowsVisible * 16));
			}

			else if (keyData == Keys.Home)
			{
				GoToAddress(0);
			}

			else if (keyData == Keys.End)
			{
				GoToAddress(GetSize() - 1);
			}

			return true;
		}

		private void ClearNibbles()
		{
			for (int x = 0; x < 4; x++)
				nibbles[x] = 'G';
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (!InputValidate.IsValidHexNumber(((char)e.KeyCode).ToString()))
			{
				if (e.Control && e.KeyCode == Keys.G)
					GoToSpecifiedAddress();
				e.Handled = true;
				return;
			}

			//TODO: 2 byte & 4 byte
			if (nibbles[0] == 'G')
			{
				nibbles[0] = (char)e.KeyCode;
				info.Text = nibbles[0].ToString();
			}
			else
			{
				string temp = nibbles[0].ToString() + ((char)e.KeyCode).ToString();
				int x = int.Parse(temp, NumberStyles.HexNumber);
				Domain.PokeByte(addressHighlighted, (byte)x);
				ClearNibbles();
				SetHighlighted(addressHighlighted + 1);
				this.Refresh();
			}

			base.OnKeyUp(e);
		}

		public void SetHighlighted(int addr)
		{
			if (addr < 0)
				addr = 0;
			if (addr >= Domain.Size)
				addr = Domain.Size - 1;
			
			if (!IsVisible(addr))
			{
				int v = (addr / 16) - RowsVisible + 1;
				if (v < 0)
					v = 0;
				vScrollBar1.Value = v;
			}
			addressHighlighted = addr;
			addressOver = addr;
			info.Text = String.Format("{0:X4}", addressOver);
			Refresh();
		}

		int row = 0;
		int addr = 0;

		protected override void OnPaint(PaintEventArgs e)
		{
			unchecked
			{
				Pen p = new Pen(Brushes.Black);
				row = 0;
				addr = 0;

				StringBuilder rowStr = new StringBuilder("");
				addrOffset = (GetNumDigits(Domain.Size) % 4) * 9;
				e.Graphics.DrawLine(p, this.Left + 38 + addrOffset, this.Top, this.Left + 38 + addrOffset, this.Bottom - 40);
				e.Graphics.DrawLine(p, this.Left, 34, this.Right - 16, 34);

				if (addressHighlighted >= 0 && IsVisible(addressHighlighted))
				{
					int left = ((addressHighlighted % 16) * 20) + 52 + addrOffset - (addressHighlighted % 4);
					int top = (((addressHighlighted / 16) - vScrollBar1.Value) * (font.Height - 1)) + 36;
					Rectangle rect = new Rectangle(left, top, 16, 14);
					e.Graphics.DrawRectangle(new Pen(highlightBrush), rect);
					e.Graphics.FillRectangle(highlightBrush, rect);
				}

				switch (DataSize)
				{
					case 1:
						Header = "       0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F";
						break;
					case 2:
						Header = "         0    2    4    6    8    A    C    E";
						break;
					case 4:
						Header = "             0        4        8        C";
						break;
				}
				e.Graphics.DrawString(Domain.Name, font, Brushes.Black, new Point(1, 1));
				e.Graphics.DrawString(Header, font, Brushes.Black, new Point(rowX + addrOffset, rowY));

				for (int i = 0; i < RowsVisible; i++)
				{
					row = i + vScrollBar1.Value;
					rowStr.AppendFormat("{0:X" + GetNumDigits(Domain.Size) + "}  ", row * 16);
					switch (DataSize)
					{
						default:
						case 1:
							addr = (row * 16);
							for (int j = 0; j < 16; j++)
							{
								if (addr + j < Domain.Size)
									rowStr.AppendFormat("{0:X2} ", Domain.PeekByte(addr + j));
							}
							rowStr.Append("  | ");
							for (int k = 0; k < 16; k++)
							{
								rowStr.Append(Remap(Domain.PeekByte(addr + k)));
							}
							rowStr.AppendLine();
							break;
						case 2:
							addr = (row * 16);
							for (int j = 0; j < 16; j += 2)
							{
								if (addr + j < Domain.Size)
									rowStr.AppendFormat("{0:X4} ", MakeValue(addr + j, DataSize, BigEndian));
							}
							rowStr.AppendLine();
							rowStr.Append("  | ");
							for (int k = 0; k < 16; k++)
							{
								rowStr.Append(Remap(Domain.PeekByte(addr + k)));
							}
							break;
						case 4:
							addr = (row * 16);
							for (int j = 0; j < 16; j += 4)
							{
								if (addr < Domain.Size)
									rowStr.AppendFormat("{0:X8} ", MakeValue(addr + j, DataSize, BigEndian));
							}
							rowStr.AppendLine();
							rowStr.Append("  | ");
							for (int k = 0; k < 16; k++)
							{
								rowStr.Append(Remap(Domain.PeekByte(addr + k)));
							}
							break;

					}
					if (row * 16 >= Domain.Size)
						break;
				}
				e.Graphics.DrawString(rowStr.ToString(), font, Brushes.Black, new Point(rowX, rowY + rowYoffset));
			}
		}

		static char Remap(byte val)
		{
			unchecked
			{
				if (val < ' ') return '.';
				else if (val >= 0x80) return '.';
				else return (char)val;
			}
		}

		private int MakeValue(int addr, int size, bool Bigendian)
		{
			unchecked
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
		}

		private int MakeWord(int addr, bool endian)
		{
			unchecked
			{
				if (endian)
					return Domain.PeekByte(addr) + (Domain.PeekByte(addr + 1) * 255);
				else
					return (Domain.PeekByte(addr) * 255) + Domain.PeekByte(addr + 1);
			}
		}

		public void ResetScrollBar()
		{
			vScrollBar1.Value = 0;
			SetUpScrollBar();
			Refresh();
		}

		public void SetUpScrollBar()
		{
			RowsVisible = ((this.Height - 8) / 13) - 2;
			int totalRows = Domain.Size / 16;
			int MaxRows = (totalRows - RowsVisible) + 16;

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
			unchecked
			{
				if (i <= 0x10000) return 4;
				if (i <= 0x1000000) return 6;
				else return 8;
			}
		}

		private void SetAddressOver(int x, int y)
		{
			//Scroll value determines the first row
			int row = vScrollBar1.Value;
			row += (y - 36) / (font.Height - 1);
			int column = (x - (49 + addrOffset)) / 20;
			
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

		public void HighlightPointed()
		{
			if (addressOver >= 0)
			{
				addressHighlighted = addressOver;
			}
			else
				addressHighlighted = -1;
			ClearNibbles();
			this.Focus();
			this.Refresh();
		}

		private void MemoryViewer_MouseClick(object sender, MouseEventArgs e)
		{
			SetAddressOver(e.X, e.Y);
			if (addressOver == addressHighlighted && addressOver >= 0)
			{
				addressHighlighted = -1;
				this.Refresh();
			}
			else
				HighlightPointed();
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

		public bool IsVisible(int addr)
		{
			unchecked
			{
				int row = addr >> 4;

				if (row >= vScrollBar1.Value && row < (RowsVisible + vScrollBar1.Value))
					return true;
				else
					return false;
			}
		}

		public void PokeHighlighted(int value)
		{
			//TODO: 2 byte & 4 byte
			if (addressHighlighted >= 0)
				Domain.PokeByte(addressHighlighted, (byte)value);
		}

		public int GetSize()
		{
			return Domain.Size;
		}

		public byte GetPointedValue()
		{
			return Domain.PeekByte(addressOver);
		}

		public MemoryDomain GetDomain()
		{
			return Domain;
		}

		public void GoToAddress(int address)
		{
			if (address < 0)
				address = 0;

			if (address >= GetSize())
				address = GetSize() - 1;
			
			SetHighlighted(address);
			ClearNibbles();
			Refresh();
		}

		public void GoToSpecifiedAddress()
		{
			InputPrompt i = new InputPrompt();
			i.Text = "Go to Address";
			i.SetMessage("Enter a hexadecimal value");
			Global.Sound.StopSound();
			i.ShowDialog();
			Global.Sound.StartSound();

			if (i.UserOK)
			{
				if (InputValidate.IsValidHexNumber(i.UserText))
				{
					GoToAddress(int.Parse(i.UserText, NumberStyles.HexNumber));
				}
			}
		}

		protected override void WndProc(ref System.Windows.Forms.Message m)
		{
			switch (m.Msg)
			{
				case 0x0201: //WM_LBUTTONDOWN
					{
						this.Focus();
						return;
					}
				//case 0x0202://WM_LBUTTONUP
				//{
				//	return;
				//}
				case 0x0203://WM_LBUTTONDBLCLK
					{
						return;
					}
				case 0x0204://WM_RBUTTONDOWN
					{
						return;
					}
				case 0x0205://WM_RBUTTONUP
					{
						return;
					}
				case 0x0206://WM_RBUTTONDBLCLK
					{
						return;
					}
				case 0x0014: //WM_ERASEBKGND
					if (BlazingFast)
					{
						m.Result = new IntPtr(1);
					}
					break;
			}

			base.WndProc(ref m);
		}

		
	}
}
