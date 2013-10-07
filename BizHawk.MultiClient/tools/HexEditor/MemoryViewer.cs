using System;
using System.Drawing;
using System.Windows.Forms;
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
		public Brush highlightBrush = Brushes.LightBlue;
		public bool BigEndian = false;
		public bool BlazingFast = false;

		private string info = "";
		private MemoryDomain Domain = new MemoryDomain("NULL", 1024, Endian.Little, addr => 0, (a, v) => { v = 0; });
		private readonly Font font = new Font("Courier New", 8);
		private int _rows_visible;
		private int _data_size = 1;
		private string _header = "";
		private int _num_digits = 4;
		private readonly char[] nibbles = { 'G', 'G', 'G', 'G' };    //G = off 0-9 & A-F are acceptable values
		private int addressHighlighted = -1;
		private int addressOver = -1;
		private int addrOffset;     //If addresses are > 4 digits, this offset is how much the columns are moved to the right
		private int maxRow;

		private const int ROWX = 1;
		private const int ROWY = 4;

		public MemoryViewer()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			BorderStyle = BorderStyle.Fixed3D;
			MouseMove += MemoryViewer_MouseMove;
			MouseClick += MemoryViewer_MouseClick;
			vScrollBar1 = new VScrollBar();
			Point n = new Point(Size);
			vScrollBar1.Location = new Point(n.X - 16, n.Y - Height + 7);
			vScrollBar1.Height = Height - 8;
			vScrollBar1.Width = 16;
			vScrollBar1.Visible = true;
			vScrollBar1.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom)
			                     | AnchorStyles.Right;
			vScrollBar1.LargeChange = 16;
			vScrollBar1.Name = "vScrollBar1";
			vScrollBar1.TabIndex = 0;
			vScrollBar1.Scroll += vScrollBar1_Scroll;
			Controls.Add(vScrollBar1);
			
			SetHeader();
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
				Refresh();
			}

			else if (keyData == Keys.PageDown)
			{
				GoToAddress(addressHighlighted + (_rows_visible * 16));
			}

			else if (keyData == Keys.PageUp)
			{
				GoToAddress(addressHighlighted - (_rows_visible * 16));
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
				info = nibbles[0].ToString();
			}
			else
			{
				string temp = nibbles[0].ToString() + ((char)e.KeyCode).ToString();
				int x = int.Parse(temp, NumberStyles.HexNumber);
				Domain.PokeByte(addressHighlighted, (byte)x);
				ClearNibbles();
				SetHighlighted(addressHighlighted + 1);
				Refresh();
			}

			base.OnKeyUp(e);
		}

		public void SetHighlighted(int address)
		{
			if (address < 0)
				address = 0;
			if (address >= Domain.Size)
				address = Domain.Size - 1;
			
			if (!IsVisible(address))
			{
				int v = (address / 16) - _rows_visible + 1;
				if (v < 0)
					v = 0;
				vScrollBar1.Value = v;
			}
			addressHighlighted = address;
			addressOver = address;
			info = String.Format("{0:X4}", addressOver);
			Refresh();
		}

		int row;
		int addr;

		protected override void OnPaint(PaintEventArgs e)
		{
			unchecked
			{
				row = 0;
				addr = 0;

				StringBuilder rowStr = new StringBuilder("");
				addrOffset = (_num_digits % 4) * 9;

				if (addressHighlighted >= 0 && IsVisible(addressHighlighted))
				{
					int left = ((addressHighlighted % 16) * 20) + 52 + addrOffset - (addressHighlighted % 4);
					int top = (((addressHighlighted / 16) - vScrollBar1.Value) * (font.Height - 1)) + 36;
					Rectangle rect = new Rectangle(left, top, 16, 14);
					e.Graphics.DrawRectangle(new Pen(highlightBrush), rect);
					e.Graphics.FillRectangle(highlightBrush, rect);
				}

				rowStr.Append(Domain.Name + "    " + info + '\n');
				rowStr.Append(_header + '\n');
				
				for (int i = 0; i < _rows_visible; i++)
				{
					row = i + vScrollBar1.Value;
					if (row * 16 >= Domain.Size)
						break;
					rowStr.AppendFormat("{0:X" + _num_digits + "}  ", row * 16);
					switch (_data_size)
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
									rowStr.AppendFormat("{0:X4} ", MakeValue(addr + j, _data_size, BigEndian));
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
									rowStr.AppendFormat("{0:X8} ", MakeValue(addr + j, _data_size, BigEndian));
							}
							rowStr.AppendLine();
							rowStr.Append("  | ");
							for (int k = 0; k < 16; k++)
							{
								rowStr.Append(Remap(Domain.PeekByte(addr + k)));
							}
							break;

					}
					
				}
				e.Graphics.DrawString(rowStr.ToString(), font, Brushes.Black, new Point(ROWX, ROWY));
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

		private int MakeValue(int address, int size, bool Bigendian)
		{
			unchecked
			{
				int x = 0;
				if (size == 1 || size == 2 || size == 4)
				{
					switch (size)
					{
						case 1:
							x = Domain.PeekByte(address);
							break;
						case 2:
							x = MakeWord(address, Bigendian);
							break;
						case 4:
							x = (MakeWord(address, Bigendian) * 65536) +
								MakeWord(address + 2, Bigendian);
							break;
					}
					return x;
				}
				else
					return 0; //fail
			}
		}

		private int MakeWord(int address, bool endian)
		{
			unchecked
			{
				if (endian)
				{
					return Domain.PeekByte(address) + (Domain.PeekByte(address + 1)*255);
				}
				else
				{
					return (Domain.PeekByte(address)*255) + Domain.PeekByte(address + 1);
				}
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
			_rows_visible = ((Height - 8) / 13) - 1;
			int totalRows = Domain.Size / 16;
			int MaxRows = (totalRows - _rows_visible) + 16;

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
			SetUpScrollBar();
			Refresh();
		}

		public void SetDataSize(int size)
		{
			if (size == 1 || size == 2 || size == 4)
				_data_size = size;

			SetHeader();
		}

		private void SetHeader()
		{
			switch (_data_size)
			{
				case 1:
					_header = "       0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F";
					break;
				case 2:
					_header = "         0    2    4    6    8    A    C    E";
					break;
				case 4:
					_header = "             0        4        8        C";
					break;
			}
			_num_digits = GetNumDigits(Domain.Size);
		}

		public int GetDataSize()
		{
			return _data_size;
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
			int i = vScrollBar1.Value;
			i += (y - 36) / (font.Height - 1);
			int column = (x - (49 + addrOffset)) / 20;
			
			//TODO: 2 & 4 byte views

			if (i >= 0 && i <= maxRow && column >= 0 && column < 16)
			{
				addressOver = i * 16 + column;
				info = String.Format("{0:X4}", addressOver);
			}
			else
			{
				addressOver = -1;
				info = "";
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
			Focus();
			Refresh();
		}

		private void MemoryViewer_MouseClick(object sender, MouseEventArgs e)
		{
			SetAddressOver(e.X, e.Y);
			if (addressOver == addressHighlighted && addressOver >= 0)
			{
				addressHighlighted = -1;
				Refresh();
			}
			else
			{
				HighlightPointed();
			}
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
			{
				return addressHighlighted;
			}
			else
			{
				return -1; //Negative = no address highlighted
			}
		}

		public bool IsVisible(int address)
		{
			unchecked
			{
				int i = address >> 4;

				if (i >= vScrollBar1.Value && i < (_rows_visible + vScrollBar1.Value))
				{
					return true;
				}
				else
				{
					return false;
				}
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
			InputPrompt i = new InputPrompt {Text = "Go to Address"};
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

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case 0x0201: //WM_LBUTTONDOWN
					{
						Focus();
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
