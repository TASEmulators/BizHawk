using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.IO;

namespace BizHawk.MultiClient
{
	public partial class HexEditor : Form
	{
		//TODO:
		//Find text box - autohighlights matches, and shows total matches
		//Users can customize background, & text colors
		//Tool strip
		int defaultWidth;
		int defaultHeight;
		List<ToolStripMenuItem> domainMenuItems = new List<ToolStripMenuItem>();
		int RowsVisible = 0;
		string Header = "";
		int NumDigits = 4;
		string NumDigitsStr = "{0:X4}  ";
		string DigitFormatString = "{0:X2} ";
		char[] nibbles = { 'G', 'G', 'G', 'G' , 'G', 'G', 'G', 'G'};    //G = off 0-9 & A-F are acceptable values
		int addressHighlighted = -1;
		int addressOver = -1;
		int addrOffset = 0;     //If addresses are > 4 digits, this offset is how much the columns are moved to the right
		int maxRow = 0;
		MemoryDomain Domain = new MemoryDomain("NULL", 1024, Endian.Little, addr => { return 0; }, (a, v) => { v = 0; });
		string info = "";
		int row = 0;
		int addr = 0;
		private int Pointedx = 0;
		private int Pointedy = 0;
		const int rowX = 1;
		const int rowY = 4;
		const int rowYoffset = 20;
		const int fontHeight = 14;
		const int fontWidth = 7; //Width of 1 digits

		public HexEditor()
		{
			InitializeComponent();
			AddressesLabel.BackColor = Color.Transparent;
			SetHeader();
			Closing += (o, e) => SaveConfigSettings();
			AddressesLabel.Font = new Font("Courier New", 8); ;
		}

		public void SaveConfigSettings()
		{
			if (Global.Config.HexEditorSaveWindowPosition)
			{
				Global.Config.HexEditorWndx = this.Location.X;
				Global.Config.HexEditorWndy = this.Location.Y;
				Global.Config.HexEditorWidth = this.Right - this.Left;
				Global.Config.HexEditorHeight = this.Bottom - this.Top;
			}
		}

		private void HexEditor_Load(object sender, EventArgs e)
		{
			defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = this.Size.Height;
			if (Global.Config.HexEditorSaveWindowPosition)
			{
				if (Global.Config.HexEditorSaveWindowPosition && Global.Config.HexEditorWndx >= 0 && Global.Config.HexEditorWndy >= 0)
					this.Location = new Point(Global.Config.HexEditorWndx, Global.Config.HexEditorWndy);

				if (Global.Config.HexEditorWidth >= 0 && Global.Config.HexEditorHeight >= 0)
				{
					this.Size = new System.Drawing.Size(Global.Config.HexEditorWidth, Global.Config.HexEditorHeight);
				}
			}
			SetMemoryDomainMenu();
			SetDataSize(Global.Config.HexEditorDataSize);
			UpdateValues();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;

			AddressesLabel.Text = GenerateMemoryViewString();
		}

		public string GenerateMemoryViewString()
		{
			StringBuilder rowStr = new StringBuilder("");
			addrOffset = (NumDigits % 4) * 9;

			rowStr.Append(Header + '\n');

			for (int i = 0; i < RowsVisible; i++)
			{
				row = i + vScrollBar1.Value;
				addr = (row * 16);
				if (addr >= Domain.Size)
					break;
				rowStr.AppendFormat(NumDigitsStr, addr);
					
				for (int j = 0; j < 16; j += Global.Config.HexEditorDataSize)
				{
					if (addr + j < Domain.Size)
						rowStr.AppendFormat(DigitFormatString, MakeValue(addr + j));
				}
				rowStr.Append("  | ");
				for (int k = 0; k < 16; k++)
				{
					rowStr.Append(Remap(Domain.PeekByte(addr + k)));
				}
				rowStr.AppendLine();

			}
			return rowStr.ToString();
		}

		static char Remap(byte val)
		{
			if (val < ' ') return '.';
			else if (val >= 0x80) return '.';
			else return (char)val;
		}

		private int MakeValue(int addr)
		{
			
			switch (Global.Config.HexEditorDataSize)
			{
				default:
				case 1:
					return Domain.PeekByte(addr);
				case 2:
					if (Global.Config.HexEditorBigEndian)
						return MakeWordBig(addr);
					else
						return MakeWordLittle(addr);
				case 4:
					if (Global.Config.HexEditorBigEndian)
						return (MakeWordBig(addr) * 65536) + MakeWordBig(addr + 2);
					else
						return (MakeWordLittle(addr) * 65536) + MakeWordLittle(addr);
			}
		}

		private int MakeWordBig(int addr)
		{
			return (Domain.PeekByte(addr) * 256) + Domain.PeekByte(addr + 1);
		}

		private int MakeWordLittle(int addr) 
		{ 
			return Domain.PeekByte(addr) + (Domain.PeekByte(addr + 1) * 256);
		}

		public void Restart()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			SetMemoryDomainMenu(); //Calls update routines
			ResetScrollBar();
		}

		private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
			SetUpScrollBar();
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadHexEditor ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			enToolStripMenuItem.Checked = Global.Config.HexEditorBigEndian;
			switch (Global.Config.HexEditorDataSize)
			{
				default:
				case 1:
					byteToolStripMenuItem.Checked = true;
					byteToolStripMenuItem1.Checked = false;
					byteToolStripMenuItem2.Checked = false;
					break;
				case 2:
					byteToolStripMenuItem.Checked = false;
					byteToolStripMenuItem1.Checked = true;
					byteToolStripMenuItem2.Checked = false;
					break;
				case 4:
					byteToolStripMenuItem.Checked = false;
					byteToolStripMenuItem1.Checked = false;
					byteToolStripMenuItem2.Checked = true;
					break;
			}

			if (GetHighlightedAddress() >= 0)
			{
				addToRamWatchToolStripMenuItem1.Enabled = true;
				freezeAddressToolStripMenuItem.Enabled = true;
			}
			else
			{
				addToRamWatchToolStripMenuItem1.Enabled = false;
				freezeAddressToolStripMenuItem.Enabled = false;
			}
		}

		public void SetMemoryDomain(MemoryDomain d)
		{
			Domain = d;
			maxRow = Domain.Size / 2;
			SetUpScrollBar();
			vScrollBar1.Value = 0;
			Refresh();
		}

		private void SetMemoryDomain(int pos)
		{
			if (pos < Global.Emulator.MemoryDomains.Count)  //Sanity check
			{
				SetMemoryDomain(Global.Emulator.MemoryDomains[pos]);
			}
			UpdateGroupBoxTitle();
			ResetScrollBar();
		}

		private void UpdateGroupBoxTitle()
		{
			string memoryDomain = Domain.ToString();
			string systemID = Global.Emulator.SystemId;
			MemoryViewerBox.Text = systemID + " " + memoryDomain + "  -  " + (Domain.Size / Global.Config.HexEditorDataSize).ToString() + " addresses";
		}

		private void SetMemoryDomainMenu()
		{
			memoryDomainsToolStripMenuItem.DropDownItems.Clear();
			if (Global.Emulator.MemoryDomains.Count > 0)
			{
				for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
				{
					string str = Global.Emulator.MemoryDomains[x].ToString();
					var item = new ToolStripMenuItem();
					item.Text = str;
					{
						int z = x;
						item.Click += (o, ev) => SetMemoryDomain(z);
					}
					if (x == 0)
					{
						SetMemoryDomain(x);
					}
					memoryDomainsToolStripMenuItem.DropDownItems.Add(item);
					domainMenuItems.Add(item);
				}
			}
			else
				memoryDomainsToolStripMenuItem.Enabled = false;
		}



		private void goToAddressToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GoToSpecifiedAddress();
		}

		private int GetNumDigits(Int32 i)
		{
			if (i <= 0x10000) return 4;
			if (i <= 0x1000000) return 6;
			else return 8;
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

		private void ClearNibbles()
		{
			for (int x = 0; x < 8; x++)
				nibbles[x] = 'G';
		}

		public void GoToAddress(int address)
		{
			if (address < 0)
				address = 0;

			if (address >= Domain.Size)
				address = Domain.Size - 1;

			SetHighlighted(address);
			ClearNibbles();
			UpdateValues();
			MemoryViewerBox.Refresh();
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
			ClearNibbles();
			info = String.Format(NumDigitsStr, addressOver);
			UpdateFormText();
			Refresh();
		}

		private void UpdateFormText()
		{
			if (addressHighlighted >= 0)
				Text = "Hex Editor - Editing Address 0x" + String.Format(NumDigitsStr, addressHighlighted);
			else
				Text = "Hex Editor";
		}

		public bool IsVisible(int addr)
		{
			int row = addr >> 4;

			if (row >= vScrollBar1.Value && row < (RowsVisible + vScrollBar1.Value))
				return true;
			else
				return false;
		}

		private void HexEditor_Resize(object sender, EventArgs e)
		{
			SetUpScrollBar();
			UpdateValues();
		}

		private void SetHeader()
		{
			switch (Global.Config.HexEditorDataSize)
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
			NumDigits = GetNumDigits(Domain.Size);
            NumDigitsStr = "{0:X" + NumDigits.ToString() + "}  ";
		}

		public void SetDataSize(int size)
		{
			if (size == 1 || size == 2 || size == 4)
				Global.Config.HexEditorDataSize = size;
			DigitFormatString = "{0:X" + (Global.Config.HexEditorDataSize * 2).ToString() + "} ";
			SetHeader();
			UpdateGroupBoxTitle();
			UpdateValues();
		}

		private void byteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetDataSize(1);
		}

		private void byteToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			SetDataSize(2);
		}

		private void byteToolStripMenuItem2_Click(object sender, EventArgs e)
		{
			SetDataSize(4);
		}

		private void enToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.HexEditorBigEndian ^= true;
			UpdateValues();
		}

		private void AddToRamWatch()
		{
			//Add to RAM Watch
			int address = GetHighlightedAddress();
			if (address >= 0)
			{
				Watch w = new Watch();
				w.address = address;
				w.bigendian = Global.Config.HexEditorBigEndian;
				w.signed = asigned.HEX;

				switch (Global.Config.HexEditorDataSize)
				{
					default:
					case 1:
						w.type = atype.BYTE;
						break;
					case 2:
						w.type = atype.WORD;
						break;
					case 4:
						w.type = atype.DWORD;
						break;
				}

				Global.MainForm.LoadRamWatch();
				Global.MainForm.RamWatch1.AddWatch(w);
			}
		}

		private void MemoryViewer_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			AddToRamWatch();
		}

		private void pokeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			int p = GetHighlightedAddress();
			if (p >= 0)
			{
				InputPrompt i = new InputPrompt();
				i.Text = "Poke " + String.Format("{0:X}", p);
				i.SetMessage("Enter a hexadecimal value");
				i.ShowDialog();

				if (i.UserOK)
				{
					if (InputValidate.IsValidHexNumber(i.UserText))
					{
						int value = int.Parse(i.UserText, NumberStyles.HexNumber);
						PokeHighlighted(value);
						UpdateValues();
					}
				}
			}
		}

		public int GetPointedAddress()
		{
			if (addressOver >= 0)
				return addressOver;
			else
				return -1;  //Negative = no address pointed
		}

		public void PokeHighlighted(int value)
		{
			//TODO: 4 byte
			if (addressHighlighted >= 0)
			{
				switch (Global.Config.HexEditorDataSize)
				{
					default:
					case 1:
						Domain.PokeByte(addressHighlighted, (byte)value);
						break;
					case 2:
						PokeWord(addressHighlighted, (byte)(value % 256), (byte)value);
						break;
					case 4:
						break;
				}
			}
		}

		private void addToRamWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AddToRamWatch();
		}

		private void addToRamWatchToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			AddToRamWatch();
		}

		private void saveWindowsSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.HexEditorSaveWindowPosition ^= true;
		}

		private void settingsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = Global.Config.AutoLoadHexEditor;
			saveWindowsSettingsToolStripMenuItem.Checked = Global.Config.HexEditorSaveWindowPosition;
		}

		private void freezeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FreezeAddress();
		}

		public int GetHighlightedAddress()
		{
			if (addressHighlighted >= 0)
				return addressHighlighted;
			else
				return -1; //Negative = no address highlighted
		}

		private void FreezeAddress()
		{
			int address = GetHighlightedAddress();
			if (address >= 0)
			{
				Cheat c = new Cheat();
				c.address = address;
				c.value = Domain.PeekByte(address);
				c.domain = Domain;
				c.Enable();
				Global.MainForm.Cheats1.AddCheat(c);

				switch (Global.Config.HexEditorDataSize)
				{
					default:
					case 1:
						break;
					case 2:
						Cheat c2 = new Cheat();
						c2.address = address + 1;
						c2.domain = Domain;
						c2.value = Domain.PeekByte(address + 1);
						c2.Enable();
						Global.MainForm.Cheats1.AddCheat(c2);
						break;
					case 4:
						Cheat c42 = new Cheat();
						c42.address = address + 1;
						c42.domain = Domain;
						c42.value = Domain.PeekByte(address + 1);
						c42.Enable();
						Global.MainForm.Cheats1.AddCheat(c42);
						Cheat c43 = new Cheat();
						c43.address = address + 2;
						c43.domain = Domain;
						c43.value = Domain.PeekByte(address + 2);
						c43.Enable();
						Global.MainForm.Cheats1.AddCheat(c43);
						Cheat c44 = new Cheat();
						c44.address = address + 3;
						c44.domain = Domain;
						c44.value = Domain.PeekByte(address + 3);
						c44.Enable();
						Global.MainForm.Cheats1.AddCheat(c44);
						break;
				}
			}
			MemoryViewerBox.Refresh();
		}

		private void freezeAddressToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FreezeAddress();
		}

		private void CheckDomainMenuItems()
		{
			for (int x = 0; x < domainMenuItems.Count; x++)
			{
				if (Domain.Name == domainMenuItems[x].Text)
					domainMenuItems[x].Checked = true;
				else
					domainMenuItems[x].Checked = false;
			}
		}

		private void memoryDomainsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			CheckDomainMenuItems();
		}

		private void dumpToFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveAs();
		}

		private void SaveAs()
		{
			var file = GetSaveFileFromUser();
			if (file != null)
			{
				using (StreamWriter sw = new StreamWriter(file.FullName))
				{
					string str = "";

					for (int x = 0; x < Domain.Size / 16; x++)
					{
						for (int y = 0; y < 16; y++)
						{
							str += String.Format("{0:X2} ", Domain.PeekByte((x * 16) + y));
						}
						str += "\r\n";
					}

					sw.WriteLine(str);
				}
			}
		}

		private FileInfo GetSaveFileFromUser()
		{
			var sfd = new SaveFileDialog();

			if (!(Global.Emulator is NullEmulator))
				sfd.FileName = Global.Game.Name;
			else
				sfd.FileName = "MemoryDump";


			sfd.InitialDirectory = PathManager.GetPlatformBase(Global.Emulator.SystemId);

			sfd.Filter = "Text (*.txt)|*.txt|All Files|*.*";
			sfd.RestoreDirectory = true;
			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(sfd.FileName);
			return file;
		}

		public void ResetScrollBar()
		{
			vScrollBar1.Value = 0;
			SetUpScrollBar();
			Refresh();
		}

		public void SetUpScrollBar()
		{
			RowsVisible = ((MemoryViewerBox.Height - (fontHeight * 2) - (fontHeight / 2)) / fontHeight);
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

		private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
		{
			this.SetUpScrollBar();
			UpdateValues();
		}

		private void SetAddressOver(int x, int y)
		{
			//Scroll value determines the first row
			int row = vScrollBar1.Value;
			int rowoffset = ((y - 16) / fontHeight);
			row += rowoffset;
			int colWidth = 0;
			switch (Global.Config.HexEditorDataSize)
			{
				default:
				case 1:
					colWidth = 3;
					break;
				case 2:
					colWidth = 5;
					break;
				case 4:
					colWidth = 9;
					break;
			}
			int column = (x - 43) / (fontWidth * colWidth);

			if (row >= 0 && row <= maxRow && column >= 0 && column < (16 / Global.Config.HexEditorDataSize))
			{
				addressOver = row * 16 + (column * Global.Config.HexEditorDataSize);
				info = String.Format(NumDigitsStr, addressOver);
			}
			else
			{
				addressOver = -1;
				info = "";
			}
		}

		private void HexEditor_ResizeEnd(object sender, EventArgs e)
		{
			SetUpScrollBar();
		}

		private void AddressesLabel_MouseMove(object sender, MouseEventArgs e)
		{
			SetAddressOver(e.X, e.Y);
			Pointedx = e.X;
			Pointedy = e.Y;
		}

		private void AddressesLabel_MouseClick(object sender, MouseEventArgs e)
		{
			SetAddressOver(e.X, e.Y);
			if (addressOver == addressHighlighted && addressOver >= 0)
			{
				ClearHighlighted();
			}
			else if (addressOver >= 0)
				SetHighlighted(addressOver);
		}

		private void ClearHighlighted()
		{
			addressHighlighted = -1;
			UpdateFormText();
			MemoryViewerBox.Refresh();
		}

		private Point GetAddressCoordinates(int address)
		{
			switch (Global.Config.HexEditorDataSize)
			{
				default:
				case 1:
					return new Point(((address % 16) * (fontWidth * 3)) + 50 + addrOffset, (((address / 16) - vScrollBar1.Value) * fontHeight) + 30);
				case 2:
					return new Point((((address % 16) / Global.Config.HexEditorDataSize) * (fontWidth * 5)) + 50 + addrOffset, (((address / 16) - vScrollBar1.Value) * fontHeight) + 30);
				case 4:
					return new Point((((address % 16) / Global.Config.HexEditorDataSize) * (fontWidth * 9)) + 50 + addrOffset, (((address / 16) - vScrollBar1.Value) * fontHeight) + 30);
			}
		}

		private void MemoryViewerBox_Paint(object sender, PaintEventArgs e)
		{
			for (int x = 0; x < Global.CheatList.Count; x++)
			{
				if (IsVisible(Global.CheatList.cheatList[x].address))
				{
					Rectangle rect = new Rectangle(GetAddressCoordinates(Global.CheatList.cheatList[x].address), new Size(15 * Global.Config.HexEditorDataSize, fontHeight));
					e.Graphics.DrawRectangle(new Pen(Brushes.Black), rect);
					e.Graphics.FillRectangle(Brushes.LightBlue, rect);
				}
			}
			if (addressHighlighted >= 0 && IsVisible(addressHighlighted))
			{
				Rectangle rect = new Rectangle(GetAddressCoordinates(addressHighlighted), new Size(15 * Global.Config.HexEditorDataSize, fontHeight));
				e.Graphics.DrawRectangle(new Pen(Brushes.Black), rect);
				if (Global.CheatList.IsActiveCheat(Domain, addressHighlighted))
					e.Graphics.FillRectangle(Brushes.Violet, rect);
				else
					e.Graphics.FillRectangle(Brushes.Pink, rect);
			}
			if (HasNibbles())
			{
				e.Graphics.DrawString(MakeNibbles(), new Font("Courier New", 8, FontStyle.Italic), Brushes.Black, new Point(8,8));
			}
		}

		private bool HasNibbles()
		{
			for (int x = 0; x < (Global.Config.HexEditorDataSize * 2); x++)
			{
				if (nibbles[x] != 'G')
					return true;
			}
			return false;
		}

		private string MakeNibbles()
		{
			string str = "";
			for (int x = 0; x < (Global.Config.HexEditorDataSize * 2); x++)
			{
				if (nibbles[x] != 'G')
					str += nibbles[x];
				else
					break;
			}
			return str;
		}

		private void AddressesLabel_MouseLeave(object sender, EventArgs e)
		{
			Pointedx = 0;
			Pointedy = 0;
			addressOver = -1;
			MemoryViewerBox.Refresh();
		}

		private void HexEditor_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Up:
					GoToAddress(addressHighlighted - 16);
					break;
				case Keys.Down:
					GoToAddress(addressHighlighted + 16);
					break;
				case Keys.Left:
					GoToAddress(addressHighlighted - (1 * Global.Config.HexEditorDataSize));
					break;
				case Keys.Right:
					GoToAddress(addressHighlighted + (1 * Global.Config.HexEditorDataSize));
					break;
				case Keys.PageUp:
					GoToAddress(addressHighlighted - (RowsVisible * 16));
					break;
				case Keys.PageDown:
					GoToAddress(addressHighlighted + (RowsVisible * 16));
					break;
				case Keys.Tab:
					if (e.Modifiers == Keys.Shift)
						GoToAddress(addressHighlighted - 8);
					else
						GoToAddress(addressHighlighted + 8);
					break;
				case Keys.Home:
					GoToAddress(0);
					break;
				case Keys.End:
					GoToAddress(Domain.Size - (Global.Config.HexEditorDataSize));
					break;
			}
		}

		private void HexEditor_KeyUp(object sender, KeyEventArgs e)
		{
			if (!InputValidate.IsValidHexNumber(((char)e.KeyCode).ToString()))
			{
				if (e.Control && e.KeyCode == Keys.G)
					GoToSpecifiedAddress();
				e.Handled = true;
				return;
			}

			switch (Global.Config.HexEditorDataSize)
			{
				default:
				case 1:
					if (nibbles[0] == 'G')
					{
						nibbles[0] = (char)e.KeyCode;
						info = nibbles[0].ToString();
					}
					else
					{
						string temp = nibbles[0].ToString() + ((char)e.KeyCode).ToString();
						byte x = byte.Parse(temp, NumberStyles.HexNumber);
						Domain.PokeByte(addressHighlighted, x);
						ClearNibbles();
						SetHighlighted(addressHighlighted + 1);
						UpdateValues();
					}
					break;
				case 2:
					if (nibbles[0] == 'G')
					{
						nibbles[0] = (char)e.KeyCode;
						info = nibbles[0].ToString();
					}
					else if (nibbles[1] == 'G')
					{
						nibbles[1] = (char)e.KeyCode;
						info = nibbles[1].ToString();
					}
					else if (nibbles[2] == 'G')
					{
						nibbles[2] = (char)e.KeyCode;
						info = nibbles[2].ToString();
					}
					else if (nibbles[3] == 'G')
					{
						string temp = nibbles[0].ToString() + nibbles[1].ToString();
						byte x1 = byte.Parse(temp, NumberStyles.HexNumber);
						
						string temp2 = nibbles[2].ToString() + ((char)e.KeyCode).ToString();
						byte x2 = byte.Parse(temp2, NumberStyles.HexNumber);
						
						PokeWord(addressHighlighted, x1, x2);
						ClearNibbles();
						SetHighlighted(addressHighlighted + 2);
						UpdateValues();
					}
					break;
				case 4:
					if (nibbles[0] == 'G')
					{
						nibbles[0] = (char)e.KeyCode;
						info = nibbles[0].ToString();
					}
					else if (nibbles[1] == 'G')
					{
						nibbles[1] = (char)e.KeyCode;
						info = nibbles[1].ToString();
					}
					else if (nibbles[2] == 'G')
					{
						nibbles[2] = (char)e.KeyCode;
						info = nibbles[2].ToString();
					}
					else if (nibbles[3] == 'G')
					{
						nibbles[3] = (char)e.KeyCode;
						info = nibbles[3].ToString();
					}
					else if (nibbles[4] == 'G')
					{
						nibbles[4] = (char)e.KeyCode;
						info = nibbles[4].ToString();
					}
					else if (nibbles[5] == 'G')
					{
						nibbles[5] = (char)e.KeyCode;
						info = nibbles[5].ToString();
					}
					else if (nibbles[6] == 'G')
					{
						nibbles[6] = (char)e.KeyCode;
						info = nibbles[6].ToString();
					}
					else if (nibbles[7] == 'G')
					{
						string temp = nibbles[0].ToString() + nibbles[1].ToString();
						byte x1 = byte.Parse(temp, NumberStyles.HexNumber);

						string temp2 = nibbles[2].ToString() + nibbles[3].ToString();
						byte x2 = byte.Parse(temp2, NumberStyles.HexNumber);

						string temp3 = nibbles[4].ToString() + nibbles[5].ToString();
						byte x3 = byte.Parse(temp3, NumberStyles.HexNumber);

						string temp4 = nibbles[6].ToString() + ((char)e.KeyCode).ToString();
						byte x4 = byte.Parse(temp4, NumberStyles.HexNumber);

						PokeWord(addressHighlighted, x1, x2);
						PokeWord(addressHighlighted + 2, x3, x4);
						ClearNibbles();
						SetHighlighted(addressHighlighted + 4);
						UpdateValues();
					}
					break;
			}
			MemoryViewerBox.Refresh();
		}

		private void PokeWord(int addr, byte _1, byte _2)
		{
			if (Global.Config.HexEditorBigEndian)
			{
				Domain.PokeByte(addr, _2);
				Domain.PokeByte(addr + 1, _1);
			}
			else
			{
				Domain.PokeByte(addr, _1);
				Domain.PokeByte(addr + 1, _2);
			}
		}

		private void RemoveAllCheats()
		{
			Global.MainForm.Cheats1.RemoveAllCheats();
			MemoryViewerBox.Refresh();
		}

		private void unfreezeAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RemoveAllCheats();
		}

		private void unfreezeAllToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			RemoveAllCheats();
		}

		private void HexEditor_MouseWheel(object sender, MouseEventArgs e)
		{
			if (e.Delta > 0)
			{
				if (vScrollBar1.Value > vScrollBar1.Minimum)
				{
					vScrollBar1.Value--;
					MemoryViewerBox.Refresh();
					UpdateValues();
				}
			}
			else if (e.Delta < 0)
			{
				if (vScrollBar1.Value < vScrollBar1.Maximum)
				{
					vScrollBar1.Value++;
					MemoryViewerBox.Refresh();
					UpdateValues();
				}
			}
			
		}
	}
}
