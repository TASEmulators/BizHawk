using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Globalization;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public class MemoryViewer : Panel
	{
		//TODO: highlighting and address determining for 2 & 4 byte viewing
		//2 & 4 byte typing in
		//show nibbles on top of highlighted address
		//Multi-highlight
		//different back color for frozen addresses

		public VScrollBar VScrollBar1;
		public Brush HighlightBrush = Brushes.LightBlue;
		public bool BigEndian = false;
		public bool BlazingFast = false;

		private string _info = String.Empty;
		private MemoryDomain _domain = new MemoryDomain("NULL", 1024, MemoryDomain.Endian.Little, addr => 0,
		                                                delegate(int a, byte v) { v = 0; });
		private readonly Font _font = new Font("Courier New", 8);
		private int _rowsVisible;
		private int _dataSize = 1;
		private string _header = String.Empty;
		private int _numDigits = 4;
		private readonly char[] _nibbles = { 'G', 'G', 'G', 'G' };    //G = off 0-9 & A-F are acceptable values
		private int _addressHighlighted = -1;
		private int _addressOver = -1;
		private int _addrOffset;     //If addresses are > 4 digits, this offset is how much the columns are moved to the right
		private int _maxRow;
		private int _row;
		private int _addr;

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
			VScrollBar1 = new VScrollBar();
			Point n = new Point(Size);
			VScrollBar1.Location = new Point(n.X - 16, n.Y - Height + 7);
			VScrollBar1.Height = Height - 8;
			VScrollBar1.Width = 16;
			VScrollBar1.Visible = true;
			VScrollBar1.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom)
			                     | AnchorStyles.Right;
			VScrollBar1.LargeChange = 16;
			VScrollBar1.Name = "vScrollBar1";
			VScrollBar1.TabIndex = 0;
			VScrollBar1.Scroll += vScrollBar1_Scroll;
			Controls.Add(VScrollBar1);
			
			SetHeader();
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Up)
			{
				GoToAddress(_addressHighlighted - 16);
			}

			else if (keyData == Keys.Down)
			{
				GoToAddress(_addressHighlighted + 16);
			}

			else if (keyData == Keys.Left)
			{
				GoToAddress(_addressHighlighted - 1);
			}

			else if (keyData == Keys.Right)
			{
				GoToAddress(_addressHighlighted + 1);
			}

			else if (keyData == Keys.Tab)
			{
				_addressHighlighted += 8;
				Refresh();
			}

			else if (keyData == Keys.PageDown)
			{
				GoToAddress(_addressHighlighted + (_rowsVisible * 16));
			}

			else if (keyData == Keys.PageUp)
			{
				GoToAddress(_addressHighlighted - (_rowsVisible * 16));
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
				_nibbles[x] = 'G';
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
			if (_nibbles[0] == 'G')
			{
				_nibbles[0] = (char)e.KeyCode;
				_info = _nibbles[0].ToString();
			}
			else
			{
				string temp = _nibbles[0].ToString() + ((char)e.KeyCode).ToString();
				int x = int.Parse(temp, NumberStyles.HexNumber);
				_domain.PokeByte(_addressHighlighted, (byte)x);
				ClearNibbles();
				SetHighlighted(_addressHighlighted + 1);
				Refresh();
			}

			base.OnKeyUp(e);
		}

		public void SetHighlighted(int address)
		{
			if (address < 0)
				address = 0;
			if (address >= _domain.Size)
				address = _domain.Size - 1;
			
			if (!IsVisible(address))
			{
				int v = (address / 16) - _rowsVisible + 1;
				if (v < 0)
					v = 0;
				VScrollBar1.Value = v;
			}
			_addressHighlighted = address;
			_addressOver = address;
			_info = String.Format("{0:X4}", _addressOver);
			Refresh();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			unchecked
			{
				_row = 0;
				_addr = 0;

				StringBuilder rowStr = new StringBuilder("");
				_addrOffset = (_numDigits % 4) * 9;

				if (_addressHighlighted >= 0 && IsVisible(_addressHighlighted))
				{
					int left = ((_addressHighlighted % 16) * 20) + 52 + _addrOffset - (_addressHighlighted % 4);
					int top = (((_addressHighlighted / 16) - VScrollBar1.Value) * (_font.Height - 1)) + 36;
					Rectangle rect = new Rectangle(left, top, 16, 14);
					e.Graphics.DrawRectangle(new Pen(HighlightBrush), rect);
					e.Graphics.FillRectangle(HighlightBrush, rect);
				}

				rowStr.Append(_domain.Name + "    " + _info + '\n');
				rowStr.Append(_header + '\n');
				
				for (int i = 0; i < _rowsVisible; i++)
				{
					_row = i + VScrollBar1.Value;
					if (_row * 16 >= _domain.Size)
						break;
					rowStr.AppendFormat("{0:X" + _numDigits + "}  ", _row * 16);
					switch (_dataSize)
					{
						default:
						case 1:
							_addr = (_row * 16);
							for (int j = 0; j < 16; j++)
							{
								if (_addr + j < _domain.Size)
									rowStr.AppendFormat("{0:X2} ", _domain.PeekByte(_addr + j));
							}
							rowStr.Append("  | ");
							for (int k = 0; k < 16; k++)
							{
								rowStr.Append(Remap(_domain.PeekByte(_addr + k)));
							}
							rowStr.AppendLine();
							break;
						case 2:
							_addr = (_row * 16);
							for (int j = 0; j < 16; j += 2)
							{
								if (_addr + j < _domain.Size)
									rowStr.AppendFormat("{0:X4} ", MakeValue(_addr + j, _dataSize, BigEndian));
							}
							rowStr.AppendLine();
							rowStr.Append("  | ");
							for (int k = 0; k < 16; k++)
							{
								rowStr.Append(Remap(_domain.PeekByte(_addr + k)));
							}
							break;
						case 4:
							_addr = (_row * 16);
							for (int j = 0; j < 16; j += 4)
							{
								if (_addr < _domain.Size)
									rowStr.AppendFormat("{0:X8} ", MakeValue(_addr + j, _dataSize, BigEndian));
							}
							rowStr.AppendLine();
							rowStr.Append("  | ");
							for (int k = 0; k < 16; k++)
							{
								rowStr.Append(Remap(_domain.PeekByte(_addr + k)));
							}
							break;

					}
					
				}
				e.Graphics.DrawString(rowStr.ToString(), _font, Brushes.Black, new Point(ROWX, ROWY));
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

		private int MakeValue(int address, int size, bool bigendian)
		{
			int x = 0;
			if (size == 1 || size == 2 || size == 4)
			{
				switch (size)
				{
					case 1:
						x = _domain.PeekByte(address);
						break;
					case 2:
						x = _domain.PeekWord(address, bigendian);
						break;
					case 4:
						x = (int) _domain.PeekDWord(address, bigendian);
						break;
				}
				return x;
			}
			else
			{
				return 0; //fail
			}
		}

		public void ResetScrollBar()
		{
			VScrollBar1.Value = 0;
			SetUpScrollBar();
			Refresh();
		}

		public void SetUpScrollBar()
		{
			_rowsVisible = ((Height - 8) / 13) - 1;
			int totalRows = _domain.Size / 16;
			int MaxRows = (totalRows - _rowsVisible) + 16;

			if (MaxRows > 0)
			{
				VScrollBar1.Visible = true;
				if (VScrollBar1.Value > MaxRows)
				{
					VScrollBar1.Value = MaxRows;
				}
				VScrollBar1.Maximum = MaxRows;
			}
			else
			{
				VScrollBar1.Visible = false;
			}

		}

		public void SetMemoryDomain(MemoryDomain d)
		{
			_domain = d;
			_maxRow = _domain.Size / 2;
			SetUpScrollBar();
			VScrollBar1.Value = 0;
			Refresh();
		}

		public string DomainName()
		{
			return _domain.ToString();
		}

		private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
		{
			SetUpScrollBar();
			Refresh();
		}

		public void SetDataSize(int size)
		{
			if (size == 1 || size == 2 || size == 4)
				_dataSize = size;

			SetHeader();
		}

		private void SetHeader()
		{
			switch (_dataSize)
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
			_numDigits = GetNumDigits(_domain.Size);
		}

		public int GetDataSize()
		{
			return _dataSize;
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
			int i = VScrollBar1.Value;
			i += (y - 36) / (_font.Height - 1);
			int column = (x - (49 + _addrOffset)) / 20;
			
			//TODO: 2 & 4 byte views

			if (i >= 0 && i <= _maxRow && column >= 0 && column < 16)
			{
				_addressOver = i * 16 + column;
				_info = String.Format("{0:X4}", _addressOver);
			}
			else
			{
				_addressOver = -1;
				_info = String.Empty;
			}
		}

		private void MemoryViewer_MouseMove(object sender, MouseEventArgs e)
		{
			SetAddressOver(e.X, e.Y);
		}

		public void HighlightPointed()
		{
			if (_addressOver >= 0)
			{
				_addressHighlighted = _addressOver;
			}
			else
				_addressHighlighted = -1;
			ClearNibbles();
			Focus();
			Refresh();
		}

		private void MemoryViewer_MouseClick(object sender, MouseEventArgs e)
		{
			SetAddressOver(e.X, e.Y);
			if (_addressOver == _addressHighlighted && _addressOver >= 0)
			{
				_addressHighlighted = -1;
				Refresh();
			}
			else
			{
				HighlightPointed();
			}
		}

		public int GetPointedAddress()
		{
			if (_addressOver >= 0)
			{
				return _addressOver;
			}
			else
			{
				return -1;  //Negative = no address pointed
			}
		}

		public int GetHighlightedAddress()
		{
			if (_addressHighlighted >= 0)
			{
				return _addressHighlighted;
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

				if (i >= VScrollBar1.Value && i < (_rowsVisible + VScrollBar1.Value))
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
			if (_addressHighlighted >= 0)
				_domain.PokeByte(_addressHighlighted, (byte)value);
		}

		public int GetSize()
		{
			return _domain.Size;
		}

		public byte GetPointedValue()
		{
			return _domain.PeekByte(_addressOver);
		}

		public MemoryDomain GetDomain()
		{
			return _domain;
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
			GlobalWin.Sound.StopSound();
			i.ShowDialog();
			GlobalWin.Sound.StartSound();

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
