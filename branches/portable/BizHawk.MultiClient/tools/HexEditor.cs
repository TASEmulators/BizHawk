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
		//Increment/Decrement wrapping logic for 4 byte values is messed up
		//2 & 4 byte text area clicking is off

		int defaultWidth;
		int defaultHeight;
		List<ToolStripMenuItem> domainMenuItems = new List<ToolStripMenuItem>();
		int RowsVisible = 0;
		int NumDigits = 4;
		string NumDigitsStr = "{0:X4}";
		string DigitFormatString = "{0:X2}";
		char[] nibbles = { 'G', 'G', 'G', 'G' , 'G', 'G', 'G', 'G'};    //G = off 0-9 & A-F are acceptable values
		int addressHighlighted = -1;
		List<int> SecondaryHighlightedAddresses = new List<int>();
		int addressOver = -1;
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
		string FindStr = "";
		bool loaded = false;
		
		byte[] ROM;
		MemoryDomain ROMDomain;

		// Configurations
		bool AutoLoad;
		bool SaveWindowPosition;
		int Wndx = -1;
		int Wndy = -1;
		int Width_ = -1;
		int Height_ = -1;
		bool BigEndian;
		int DataSize;

		HexFind HexFind1 = new HexFind();

		public HexEditor()
		{
			InitializeComponent();
			AddressesLabel.BackColor = Color.Transparent;
			LoadConfigSettings();
			SetHeader();
			Closing += (o, e) => SaveConfigSettings();
			Header.Font = new Font("Courier New", 8);
			AddressesLabel.Font = new Font("Courier New", 8);
			AddressLabel.Font = new Font("Courier New", 8);
		}

		private void LoadConfigSettings()
		{
			AutoLoad = Global.Config.AutoLoadHexEditor;
			SaveWindowPosition = Global.Config.SaveWindowPosition;
			Wndx = Global.Config.HexEditorWndx;
			Wndy = Global.Config.HexEditorWndy;
			Width_ = Global.Config.HexEditorWidth;
			Height_ = Global.Config.HexEditorHeight;
			BigEndian = Global.Config.HexEditorBigEndian;
			DataSize = Global.Config.HexEditorDataSize;
			//Colors
			menuStrip1.BackColor = Global.Config.HexMenubarColor;
			MemoryViewerBox.BackColor = Global.Config.HexBackgrndColor;
			MemoryViewerBox.ForeColor = Global.Config.HexForegrndColor;
			Header.BackColor = Global.Config.HexBackgrndColor;
			Header.ForeColor = Global.Config.HexForegrndColor;

		}

		public void SaveConfigSettings()
		{
			if (HexFind1.IsHandleCreated || !HexFind1.IsDisposed)
			{
				HexFind1.Close();
			}

			Global.Config.AutoLoadHexEditor = AutoLoad;
			Global.Config.HexEditorSaveWindowPosition = SaveWindowPosition;
			if (SaveWindowPosition)
			{
				Global.Config.HexEditorWndx = loaded ? this.Location.X : Wndx;
				Global.Config.HexEditorWndy = loaded ? this.Location.Y : Wndy;
				Global.Config.HexEditorWidth = loaded ? this.Right - this.Left : Width_;
				Global.Config.HexEditorHeight = loaded ? this.Bottom - this.Top : Height_;
			}
			Global.Config.HexEditorBigEndian = BigEndian;
			Global.Config.HexEditorDataSize = DataSize;
		}

		private void HexEditor_Load(object sender, EventArgs e)
		{
			defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = this.Size.Height;
			if (SaveWindowPosition)
			{
				if (Wndx >= 0 && Wndy >= 0)
					this.Location = new Point(Wndx, Wndy);

				if (Width_ >= 0 && Height_ >= 0)
					this.Size = new System.Drawing.Size(Width_, Height_);
			}
			SetMemoryDomainMenu();
			SetDataSize(DataSize);
			UpdateValues();
			loaded = true;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;

			AddressesLabel.Text = GenerateMemoryViewString();
			AddressLabel.Text = GenerateAddressString();
		}

		public string GenerateAddressString()
		{
			StringBuilder addrStr = new StringBuilder();

			for (int i = 0; i < RowsVisible; i++)
			{
				row = i + vScrollBar1.Value;
				addr = (row << 4);
				if (addr >= Domain.Size)
					break;

				if (NumDigits == 4)
				{
					addrStr.Append("  "); //Hack to line things up better between 4 and 6
				}
				addrStr.Append(String.Format("{0:X" + NumDigits + "}", addr));
				addrStr.Append('\n');
				
			}

			return addrStr.ToString();
		}

		public string GenerateMemoryViewString()
		{
			StringBuilder rowStr = new StringBuilder();

			for (int i = 0; i < RowsVisible; i++)
			{
				row = i + vScrollBar1.Value;
				addr = (row << 4);
				if (addr >= Domain.Size)
					break;

				for (int j = 0; j < 16; j += DataSize)
				{
					if (addr + j < Domain.Size)
					{
						rowStr.AppendFormat(DigitFormatString, MakeValue(addr + j));
					}
				}
				rowStr.Append("  | ");
				for (int k = 0; k < 16; k++)
				{
					if (addr + k < Domain.Size)
					{
						rowStr.Append(Remap(Domain.PeekByte(addr + k)));
					}
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
			
			switch (DataSize)
			{
				default:
				case 1:
					return Domain.PeekByte(addr);
				case 2:
					if (BigEndian)
					{
						int value = 0;
						value |= Domain.PeekByte(addr) << 8;
						value |= Domain.PeekByte(addr + 1);
						return value;
					}
					else
					{
						int value = 0;
						value |= Domain.PeekByte(addr);
						value |= Domain.PeekByte(addr + 1) << 8;
						return value;
					}
				case 4:
					if (BigEndian)
					{
						int value = 0;
						value |= Domain.PeekByte(addr) << 24;
						value |= Domain.PeekByte(addr + 1) << 16;
						value |= Domain.PeekByte(addr + 2) << 8;
						value |= Domain.PeekByte(addr + 3) << 0;
						return value;
					}
					else
					{
						int value = 0;
						value |= Domain.PeekByte(addr) << 0;
						value |= Domain.PeekByte(addr + 1) << 8;
						value |= Domain.PeekByte(addr + 2) << 16;
						value |= Domain.PeekByte(addr + 3) << 24;
						return value;
					}
			}
		}

		private int? GetDomainInt(string name)
		{
			for (int i = 0; i < Global.Emulator.MemoryDomains.Count; i++)
			{
				if (Global.Emulator.MemoryDomains[i].Name == name)
				{
					return i;
				}
			}

			return null;
		}

		public void SetDomain(MemoryDomain domain)
		{
			Domain = domain;
			int? theDomain = GetDomainInt(Domain.Name);
			SetMemoryDomain(theDomain ?? 0);
		}

		public void Restart()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			
			int? theDomain = null;
			if (Domain.Name.ToLower() == "rom file")
			{
				theDomain = 999;
			}
			else
			{
				theDomain = GetDomainInt(Domain.Name);
			}
			
			
			
			SetMemoryDomainMenu(); //Calls update routines

			if (theDomain != null)
			{
				SetMemoryDomain(theDomain ?? 0);
			}
			
			
			ResetScrollBar();


			SetDataSize(DataSize);
			UpdateValues();
			AddressLabel.Text = GenerateAddressString();
		}

		private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
			SetUpScrollBar();
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AutoLoad ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			enToolStripMenuItem.Checked = BigEndian;
			switch (DataSize)
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

			if (IsFrozen(GetHighlightedAddress()))
			{
				freezeAddressToolStripMenuItem.Image = MultiClient.Properties.Resources.Unfreeze;
				freezeAddressToolStripMenuItem.Text = "Un&freeze Address";
			}
			else
			{
				freezeAddressToolStripMenuItem.Image = MultiClient.Properties.Resources.Freeze;
				freezeAddressToolStripMenuItem.Text = "&Freeze Address";
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
			if (d.Endian == Endian.Big)
				BigEndian = true;
			else
				BigEndian = false;
			maxRow = Domain.Size / 2;
			SetUpScrollBar();
			vScrollBar1.Value = 0;
			Refresh();
		}

		private bool CurrentROMIsArchive()
		{
			string path = Global.MainForm.CurrentlyOpenRom;
			if (path == null)
			{
				return false;
			}

			using (var file = new HawkFile())
			{
				file.Open(path);

				if (!file.Exists)
				{
					return false;
				}

				if (file.IsArchive)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		private byte[] GetRomBytes()
		{
			string path = Global.MainForm.CurrentlyOpenRom;
			if (path == null)
			{
				return null;
			}

			using (var file = new HawkFile())
			{
				file.Open(path);

				if (!file.Exists)
				{
					return null;
				}

				if (file.IsArchive)
				{
					var stream = file.GetStream();
					return Util.ReadAllBytes(stream);
				}
				else
				{
					return File.ReadAllBytes(path);
				}
			}
		}

		private void SetMemoryDomain(int pos)
		{
			if (pos == 999)
			{
				ROM = GetRomBytes();
				if (ROM == null)
				{
					ROM = new byte[1] { 0xFF };
				}
				
				ROMDomain = new MemoryDomain("ROM File", ROM.Length, Endian.Little,
					addr => ROM[addr],
					(addr, value) => ROM[addr] = value);
			
				Domain = ROMDomain;
			}
			else if (pos < Global.Emulator.MemoryDomains.Count)  //Sanity check
			{
				SetMemoryDomain(Global.Emulator.MemoryDomains[pos]);
			}
			UpdateGroupBoxTitle();
			ResetScrollBar();
			UpdateValues();
			MemoryViewerBox.Refresh();
		}

		private void UpdateGroupBoxTitle()
		{
			string memoryDomain = Domain.ToString();
			string systemID = Global.Emulator.SystemId;
			int addresses = Domain.Size / DataSize;
			string addressesString = "0x" + string.Format("{0:X8}", addresses).TrimStart('0');
			//if ((addresses & 0x3FF) == 0)
			//  addressesString = (addresses >> 10).ToString() + "K";
			//else addressesString = addresses.ToString();
			MemoryViewerBox.Text = systemID + " " + memoryDomain + "  -  " + addressesString + " addresses";
		}

		private void SetMemoryDomainMenu()
		{
			memoryDomainsToolStripMenuItem.DropDownItems.Clear();
			
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
			
			//Add ROM File memory domain
			var rom_item = new ToolStripMenuItem();
			rom_item.Text = "ROM File";
			rom_item.Click += (o, ev) => SetMemoryDomain(999); //999 will denote ROM file
			memoryDomainsToolStripMenuItem.DropDownItems.Add(rom_item);
			domainMenuItems.Add(rom_item);
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
			AddressLabel.Text = GenerateAddressString();
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
			AddressLabel.Text = GenerateAddressString();
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
			switch (DataSize)
			{
				case 1:
					Header.Text = "       0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F";
					break;
				case 2:
					Header.Text = "       0    2    4    6    8    A    C    E";
					break;
				case 4:
					Header.Text = "       0        4        8        C";
					break;
			}
			NumDigits = GetNumDigits(Domain.Size);
			NumDigitsStr = "{0:X" + NumDigits.ToString() + "}  ";
		}

		public void SetDataSize(int size)
		{
			if (size == 1 || size == 2 || size == 4)
				DataSize = size;
			DigitFormatString = "{0:X" + (DataSize * 2).ToString() + "} ";
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
			BigEndian ^= true;
			UpdateValues();
		}

		private Watch MakeWatch(int address)
		{
			Watch w = new Watch();
			w.Address = address;
			w.BigEndian = BigEndian;
			w.Signed = Watch.DISPTYPE.HEX;
			w.Domain = Domain;
			switch (DataSize)
			{
				default:
				case 1:
					w.Type = Watch.TYPE.BYTE;
					break;
				case 2:
					w.Type = Watch.TYPE.WORD;
					break;
				case 4:
					w.Type = Watch.TYPE.DWORD;
					break;
			}
			return w;
		}

		private void AddToRamWatch()
		{
			//Add to RAM Watch
			int address = GetHighlightedAddress();

			if (address >= 0 || SecondaryHighlightedAddresses.Count > 0)
			{
				Global.MainForm.LoadRamWatch(true);
			}

			if (address >= 0)
			{
				Global.MainForm.RamWatch1.AddWatch(MakeWatch(address));
			}
			foreach (int addr in SecondaryHighlightedAddresses)
			{
				Global.MainForm.RamWatch1.AddWatch(MakeWatch(addr));
			}
		}

		private void MemoryViewer_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			AddToRamWatch();
		}

		private void pokeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private void PokeAddress()
		{
			int p = GetHighlightedAddress();
			if (p >= 0)
			{
				Watch w = new Watch();
				w.Address = p;
				w.Value = MakeValue(p);
				w.BigEndian = BigEndian;
				w.Signed = Watch.DISPTYPE.HEX;
				w.Domain = Domain;

				switch (DataSize)
				{
					default:
					case 1:
						w.Type = Watch.TYPE.BYTE;
						break;
					case 2:
						w.Type = Watch.TYPE.WORD;
						break;
					case 4:
						w.Type = Watch.TYPE.DWORD;
						break;
				}
				
				RamPoke poke = new RamPoke();
				poke.SetWatchObject(w);
				poke.location = GetAddressCoordinates(p);
				Global.Sound.StopSound();
				poke.ShowDialog();
				Global.Sound.StartSound();
				UpdateValues();
			}
		}

		public int GetPointedAddress()
		{
			if (addressOver >= 0)
			{
				return addressOver;
			}
			else
			{
				return -1;  //Negative = no address pointed
			}
		}

		public void PokeHighlighted(int value)
		{
			//TODO: 4 byte
			if (addressHighlighted >= 0)
			{
				switch (DataSize)
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
			SaveWindowPosition ^= true;
		}

		private void settingsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = AutoLoad;
			saveWindowsSettingsToolStripMenuItem.Checked = SaveWindowPosition;
		}

		private void freezeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFreeze();
		}

		public int GetHighlightedAddress()
		{
			if (addressHighlighted >= 0)
				return addressHighlighted;
			else
				return -1; //Negative = no address highlighted
		}

		private bool IsFrozen(int address)
		{
			return Global.CheatList.IsActiveCheat(Domain, address);
		}

		private void ToggleFreeze()
		{
			int address = GetHighlightedAddress();
			if (IsFrozen(address))
			{
				UnFreezeAddress(address);
			}
			else
			{
				FreezeAddress(GetHighlightedAddress());
			}
			foreach (int addr in SecondaryHighlightedAddresses)
			{
				if (IsFrozen(addr))
				{
					UnFreezeAddress(addr);
				}
				else
				{
					FreezeAddress(addr);
				}
			}
		}

		private void UnFreezeAddress(int address)
		{
			if (address >= 0)
			{
				Cheat c = new Cheat();
				c.address = address;
				c.value = Domain.PeekByte(address);
				c.domain = Domain;
				Global.MainForm.Cheats1.RemoveCheat(c);

				switch (DataSize)
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
						Global.MainForm.Cheats1.RemoveCheat(c2);
						break;
					case 4:
						Cheat c42 = new Cheat();
						c42.address = address + 1;
						c42.domain = Domain;
						c42.value = Domain.PeekByte(address + 1);
						c42.Enable();
						Global.MainForm.Cheats1.RemoveCheat(c42);
						Cheat c43 = new Cheat();
						c43.address = address + 2;
						c43.domain = Domain;
						c43.value = Domain.PeekByte(address + 2);
						c43.Enable();
						Global.MainForm.Cheats1.RemoveCheat(c43);
						Cheat c44 = new Cheat();
						c44.address = address + 3;
						c44.domain = Domain;
						c44.value = Domain.PeekByte(address + 3);
						c44.Enable();
						Global.MainForm.Cheats1.RemoveCheat(c44);
						break;
				}
			}
			MemoryViewerBox.Refresh();
		}

		private void FreezeAddress(int address)
		{
			if (address >= 0)
			{
				Cheat c = new Cheat();
				c.address = address;
				c.value = Domain.PeekByte(address);
				c.domain = Domain;
				c.Enable();
				Global.MainForm.Cheats1.AddCheat(c);

				switch (DataSize)
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
			ToggleFreeze();
		}

		private void CheckDomainMenuItems()
		{
			for (int x = 0; x < domainMenuItems.Count; x++)
			{
				if (Domain.Name == domainMenuItems[x].Text)
				{
					domainMenuItems[x].Checked = true;
				}
				else
				{
					domainMenuItems[x].Checked = false;
				}
			}
		}

		private void memoryDomainsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			CheckDomainMenuItems();
		}

		private void dumpToFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveAsText();
		}

		private void SaveAsText()
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

		private void SaveAsBinary()
		{
			SaveFileBinary(GetBinarySaveFileFromUser());
		}

		private void SaveFileBinary(FileInfo file)
		{
			if (file != null)
			{
				using (BinaryWriter binWriter = new BinaryWriter(File.Open(file.FullName, FileMode.Create)))
				{
					byte[] dump = new byte[Domain.Size];

					for (int x = 0; x < Domain.Size; x++)
					{
						binWriter.Write(Domain.PeekByte(x));
					}
				}
			}
		}

		private FileInfo GetSaveFileFromUser()
		{
			var sfd = new SaveFileDialog();

			if (!(Global.Emulator is NullEmulator))
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
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

		private string GetSaveFileFilter()
		{
			if (Domain.Name == "ROM File")
			{
				string extension = Path.GetExtension(Global.MainForm.CurrentlyOpenRom);

				return "Binary (*" + extension + ")|*" +  extension + "|All Files|*.*";
			}
			else
			{
				return "Binary (*.bin)|*.bin|All Files|*.*";
			}
		}

		private FileInfo GetBinarySaveFileFromUser()
		{
			var sfd = new SaveFileDialog();

			if (!(Global.Emulator is NullEmulator))
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
			else
				sfd.FileName = "MemoryDump";


			sfd.InitialDirectory = PathManager.GetPlatformBase(Global.Emulator.SystemId);

			sfd.Filter = GetSaveFileFilter();
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
			
			vScrollBar1.Maximum = totalRows - 1;
			vScrollBar1.LargeChange = RowsVisible;
			vScrollBar1.Visible = totalRows > RowsVisible;

			AddressLabel.Text = GenerateAddressString();
		}

		private int GetPointedAddress(int x, int y)
		{
			
			int address = -1;
			//Scroll value determines the first row
			int row = vScrollBar1.Value;
			int rowoffset = y / fontHeight;
			row += rowoffset;
			int colWidth = 0;
			switch (DataSize)
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
			int column = (x /*- 43*/) / (fontWidth * colWidth);

			int start = GetTextOffset() - 50;
			if (x > start)
			{
				column = (x - start)  / (fontWidth / DataSize);
			}

			if (row >= 0 && row <= maxRow && column >= 0 && column < (16 / DataSize))
			{
				address = row * 16 + (column * DataSize);
				info = String.Format(NumDigitsStr, addressOver);
			}
			else
			{
				address = -1;
				info = "";
			}
			return address;
		}

		private void HexEditor_ResizeEnd(object sender, EventArgs e)
		{
			SetUpScrollBar();
		}

		private void AddressesLabel_MouseMove(object sender, MouseEventArgs e)
		{
			addressOver = GetPointedAddress(e.X, e.Y);
			Pointedx = e.X;
			Pointedy = e.Y;
		}

		private void AddressesLabel_MouseClick(object sender, MouseEventArgs e)
		{
			int addressOver = GetPointedAddress(e.X, e.Y);
			if (addressOver >= 0)
			{
				if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
				{
					if (addressOver == addressHighlighted)
					{
						ClearHighlighted();
					}
					else if (SecondaryHighlightedAddresses.Contains(addressOver))
					{
						SecondaryHighlightedAddresses.Remove(addressOver);
					}
					else
					{
						SecondaryHighlightedAddresses.Add(addressOver);
					}
				}
				else if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
				{
					if (addressOver >= 0)
					{
						SecondaryHighlightedAddresses.Clear();
						if (addressOver < addressHighlighted)
						{
							for (int x = addressOver; x < addressHighlighted; x++)
							{
								SecondaryHighlightedAddresses.Add(x);
							}
						}
						else if (addressOver > addressHighlighted)
						{
							for (int x = addressHighlighted + DataSize; x <= addressOver; x++)
							{
								SecondaryHighlightedAddresses.Add(x);
							}
						}
					}
				}
				else if (addressOver == addressHighlighted)
				{
					ClearHighlighted();
				}
				else
				{
					SetHighlighted(addressOver);
					SecondaryHighlightedAddresses.Clear();
					FindStr = "";
				}

				MemoryViewerBox.Refresh();
			}
		}

		private void ClearHighlighted()
		{
			addressHighlighted = -1;
			UpdateFormText();
			MemoryViewerBox.Refresh();
		}

		private Point GetAddressCoordinates(int address)
		{
			switch (DataSize)
			{
				default:
				case 1:
					return new Point(((address % 16) * (fontWidth * 3)) + 57, (((address / 16) - vScrollBar1.Value) * fontHeight) + 30);
				case 2:
					return new Point((((address % 16) / DataSize) * (fontWidth * 5)) + 57, (((address / 16) - vScrollBar1.Value) * fontHeight) + 30);
				case 4:
					return new Point((((address % 16) / DataSize) * (fontWidth * 9)) + 57, (((address / 16) - vScrollBar1.Value) * fontHeight) + 30);
			}
		}

		private int GetTextOffset()
		{
			int start = 0;
			switch (DataSize)
			{
				default:
				case 1:
					start = (16 * (fontWidth * 3)) + 57;
					break;
				case 2:
					start = ((16 / DataSize) * (fontWidth * 5)) + 57;
					break;
				case 4:
					start = ((16 / DataSize) * (fontWidth * 9)) + 57;
					break;
			}
			start += (fontWidth * 4);
			return start;
		}

		private int GetTextX(int address)
		{
			int start = GetTextOffset();
			return start + ((address % 16) * fontWidth);
		}

		private void MemoryViewerBox_Paint(object sender, PaintEventArgs e)
		{
			
			for (int x = 0; x < Global.CheatList.Count; x++)
			{
				if (IsVisible(Global.CheatList.cheatList[x].address))
				{
					if (Domain.ToString() == Global.CheatList.cheatList[x].domain.ToString())
					{
						Rectangle rect = new Rectangle(GetAddressCoordinates(Global.CheatList.cheatList[x].address), new Size(15 * DataSize, fontHeight));
						e.Graphics.DrawRectangle(new Pen(Brushes.Black), rect);
						e.Graphics.FillRectangle(new SolidBrush(Global.Config.HexFreezeColor), rect);
					}
				}
			}
			if (addressHighlighted >= 0 && IsVisible(addressHighlighted))
			{
				Point point = GetAddressCoordinates(addressHighlighted);
				int textX = GetTextX(addressHighlighted);
				Point textpoint = new Point(textX, point.Y);

				Rectangle rect = new Rectangle(point, new Size(15 * DataSize, fontHeight));
				e.Graphics.DrawRectangle(new Pen(Brushes.Black), rect);

				Rectangle textrect = new Rectangle(textpoint, new Size((8 * DataSize), fontHeight));
				
				if (Global.CheatList.IsActiveCheat(Domain, addressHighlighted))
				{
					e.Graphics.FillRectangle(new SolidBrush(Global.Config.HexHighlightFreezeColor), rect);
					e.Graphics.FillRectangle(new SolidBrush(Global.Config.HexHighlightFreezeColor), textrect);
				}
				else
				{
					e.Graphics.FillRectangle(new SolidBrush(Global.Config.HexHighlightColor), rect);
					e.Graphics.FillRectangle(new SolidBrush(Global.Config.HexHighlightColor), textrect);
				}
			}
			foreach (int address in SecondaryHighlightedAddresses)
			{
				Point point = GetAddressCoordinates(address);
				int textX = GetTextX(address);
				Point textpoint = new Point(textX, point.Y);

				Rectangle rect = new Rectangle(point, new Size(15 * DataSize, fontHeight));
				e.Graphics.DrawRectangle(new Pen(Brushes.Black), rect);

				Rectangle textrect = new Rectangle(textpoint, new Size(8, fontHeight));

				if (Global.CheatList.IsActiveCheat(Domain, address))
				{
					e.Graphics.FillRectangle(new SolidBrush(Global.Config.HexHighlightFreezeColor), rect);
					e.Graphics.FillRectangle(new SolidBrush(Global.Config.HexHighlightFreezeColor), textrect);
				}
				else
				{
					e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0x77FFD4D4)), rect);
					e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0x77FFD4D4)), textrect);
				}
			}
			if (HasNibbles())
			{
				e.Graphics.DrawString(MakeNibbles(), new Font("Courier New", 8, FontStyle.Italic), Brushes.Black, new Point(158,4));
			}
		}

		private bool HasNibbles()
		{
			for (int x = 0; x < (DataSize * 2); x++)
			{
				if (nibbles[x] != 'G')
					return true;
			}
			return false;
		}

		private string MakeNibbles()
		{
			string str = "";
			for (int x = 0; x < (DataSize * 2); x++)
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
			int newHighlighted;
			switch (e.KeyCode)
			{
				case Keys.Up:
					newHighlighted = addressHighlighted - 16;
					if (e.Modifiers == Keys.Shift)
					{
						for (int i = newHighlighted + 1; i <= addressHighlighted; i++)
						{
							AddToSecondaryHighlights(i);
						}
						GoToAddress(newHighlighted);
					}
					else
					{
						SecondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.Down:
					newHighlighted = addressHighlighted + 16;
					if (e.Modifiers == Keys.Shift)
					{
						for (int i = newHighlighted - 16; i < newHighlighted; i++)
						{
							AddToSecondaryHighlights(i);
						}
						GoToAddress(newHighlighted);
					}
					else
					{
						SecondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.Left:
					newHighlighted = addressHighlighted - (1 * DataSize);
					if (e.Modifiers == Keys.Shift)
					{
						AddToSecondaryHighlights(addressHighlighted);
						GoToAddress(newHighlighted);
					}
					else
					{
						SecondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.Right:
					newHighlighted = addressHighlighted + (1 * DataSize);
					if (e.Modifiers == Keys.Shift)
					{
						AddToSecondaryHighlights(addressHighlighted);
						GoToAddress(newHighlighted);
					}
					else
					{
						SecondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.PageUp:
					newHighlighted = addressHighlighted - (RowsVisible * 16);
					if (e.Modifiers == Keys.Shift)
					{
						for (int i = newHighlighted + 1; i <= addressHighlighted; i++)
						{
							AddToSecondaryHighlights(i);
						}
						GoToAddress(newHighlighted);
					}
					else
					{
						SecondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.PageDown:
					newHighlighted = addressHighlighted + (RowsVisible * 16);
					if (e.Modifiers == Keys.Shift)
					{
						for (int i = addressHighlighted + 1; i < newHighlighted; i++)
						{
							AddToSecondaryHighlights(i);
						}
						GoToAddress(newHighlighted);
					}
					else
					{
						SecondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.Tab:
					SecondaryHighlightedAddresses.Clear();
					if (e.Modifiers == Keys.Shift)
						GoToAddress(addressHighlighted - 8);
					else
						GoToAddress(addressHighlighted + 8);
					break;
				case Keys.Home:
					if (e.Modifiers == Keys.Shift)
					{
						for (int i = 1; i <= addressHighlighted; i++)
						{
							AddToSecondaryHighlights(i);
						}
						GoToAddress(0);
					}
					else
					{
						SecondaryHighlightedAddresses.Clear();
						GoToAddress(0);
					}
					break;
				case Keys.End:
					newHighlighted = Domain.Size - (DataSize);
					if (e.Modifiers == Keys.Shift)
					{
						for (int i = addressHighlighted; i < newHighlighted; i++)
						{
							AddToSecondaryHighlights(i);
						}
						GoToAddress(newHighlighted);
					}
					else
					{
						SecondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.Add:
					IncrementAddress();
					UpdateValues();
					break;
				case Keys.Subtract:
					DecrementAddress();
					UpdateValues();
					break;
				case Keys.Space:
					ToggleFreeze();
					break;
				case Keys.Delete:
					if (e.Modifiers == Keys.Shift)
						RemoveAllCheats();
					else
						UnFreezeAddress(GetHighlightedAddress());
					break;
				case Keys.W:
					if (e.Modifiers == Keys.Control)
						AddToRamWatch();
					break;
			}
		}

		private void AddToSecondaryHighlights(int address)
		{
			if (address >= 0 && address < Domain.Size)
			{
				SecondaryHighlightedAddresses.Add(address);
			}
		}

		private void HexEditor_KeyUp(object sender, KeyEventArgs e)
		{
			if (!InputValidate.IsValidHexNumber(((char)e.KeyCode).ToString()))
			{
				if (e.Control && e.KeyCode == Keys.G)
					GoToSpecifiedAddress();
				if (e.Control && e.KeyCode == Keys.P)
					PokeAddress();
				e.Handled = true;
				return;
			}

			if (e.Control || e.Shift || e.Alt) //If user is pressing one of these, don't type into the hex editor
			{
				return;
			}

			switch (DataSize)
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
			if (BigEndian)
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
			int delta = 0;
			if (e.Delta > 0)
				delta = -1;
			else if (e.Delta < 0)
				delta = 1;

			int newValue = vScrollBar1.Value + delta;
			if(newValue < vScrollBar1.Minimum) newValue = vScrollBar1.Minimum;
			if(newValue > vScrollBar1.Maximum - vScrollBar1.LargeChange + 1) newValue = vScrollBar1.Maximum - vScrollBar1.LargeChange + 1;
			if(newValue != vScrollBar1.Value)
			{
				vScrollBar1.Value = newValue;
				MemoryViewerBox.Refresh();
			}
		}

		private void IncrementAddress()
		{
			int address = GetHighlightedAddress();
			byte value;
			if (address >= 0)
			{
				switch (DataSize)
				{
					default:
					case 1: 
						value = Domain.PeekByte(address);
						HexPokeAddress(address, (byte)(value + 1));
						break;
					case 2:
						if (BigEndian)
						{
							value = Domain.PeekByte(address + 1);
							if (value == 0xFF) //Wrapping logic
							{
								HexPokeAddress(address, (byte)(value + 1));
								HexPokeAddress(address + 1, (byte)(value + 1));
							}
							else
							{
								HexPokeAddress(address + 1, (byte)(value + 1));
							}
						}
						else
						{
							value = Domain.PeekByte(address);
							if (value == 0xFF) //Wrapping logic
							{
								HexPokeAddress(address, (byte)(value + 1));
								HexPokeAddress(address + 1, (byte)(value + 1));
							}
							else
							{
								HexPokeAddress(address, (byte)(value + 1));
							}
						}
						break;
					case 4:
						if (BigEndian)
						{
							value = Domain.PeekByte(address + 3);
							if (value == 0xFF) //Wrapping logic
							{
								HexPokeAddress(address + 3, (byte)(value + 1));
								HexPokeAddress(address + 2, (byte)(value + 1));
							}
							else
							{
								HexPokeAddress(address + 2, (byte)(value + 1));
							}
						}
						else
						{
							value = Domain.PeekByte(address);
							HexPokeAddress(address, (byte)(value + 1));
						}
						break;
				}
			}
		}

		private void HexPokeAddress(int address, byte value)
		{
			if (Global.CheatList.IsActiveCheat(Domain, address))
			{
				UnFreezeAddress(address);
				Domain.PokeByte(address, value);
				FreezeAddress(address);
			}
			else
			{
				Domain.PokeByte(address, (byte)value);
			}
		}

		private void DecrementAddress()
		{
			int address = GetHighlightedAddress();
			byte value;
			if (address >= 0)
			{
				switch (DataSize)
				{
					default:
					case 1:
						value = Domain.PeekByte(address);
						HexPokeAddress(address, (byte)(value - 1));
						break;
					case 2:
						if (BigEndian)
						{
							value = Domain.PeekByte(address + 1);
							if (value == 0) //Wrapping logic
							{
								HexPokeAddress(address, (byte)(value - 1));
								HexPokeAddress(address + 1, (byte)(value - 1));
							}
							else
							{
								Domain.PokeByte(address + 1, (byte)(value - 1));
							}
						}
						else
						{
							value = Domain.PeekByte(address);
							if (value == 0) //Wrapping logic
							{
								HexPokeAddress(address, (byte)(value - 1));
								HexPokeAddress(address + 1, (byte)(value - 1));
							}
							else
							{
								HexPokeAddress(address, (byte)(value - 1));
							}
						}
						break;
					case 4:
						if (BigEndian)
						{
							value = Domain.PeekByte(address + 3);
							if (value == 0xFF) //Wrapping logic
							{
								HexPokeAddress(address + 3, (byte)(value - 1));
								HexPokeAddress(address + 2, (byte)(value - 1));
							}
							else
							{
								HexPokeAddress(address + 3, (byte)(value - 1));
							}
						}
						else
						{
							value = Domain.PeekByte(address);
							if (value == 0)
							{
								HexPokeAddress(address, (byte)(value - 1));
								HexPokeAddress(address + 1, (byte)(value - 1));
								int value2 = Domain.PeekByte(address + 1);
								if (value2 == 0xFF)
								{
									Domain.PokeByte(address + 2, (byte)(value - 1));
									int value3 = Domain.PeekByte(address + 1);
									if (value3 == 0xFF)
									{
										HexPokeAddress(address + 3, (byte)(value - 1));
									}
								}
							}
							else
							{
								HexPokeAddress(address, (byte)(value - 1));
							}
						}
						break;
				}
			}
		}

		private void incrementToolStripMenuItem_Click(object sender, EventArgs e)
		{
			IncrementAddress();
		}

		private void decrementToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DecrementAddress();
		}

		private void ViewerContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			if (addressHighlighted > 0 || SecondaryHighlightedAddresses.Count() > 0)
			{
				copyToolStripMenuItem1.Visible = true;
			}
			else
			{
				copyToolStripMenuItem1.Visible = false;
			}

			IDataObject iData = Clipboard.GetDataObject();

			if (iData.GetDataPresent(DataFormats.Text))
			{
				pasteToolStripMenuItem1.Visible = true;
			}
			else
			{
				pasteToolStripMenuItem1.Visible = false;
			}

			if (IsFrozen(GetHighlightedAddress()))
			{
				freezeToolStripMenuItem.Text = "Un&freeze";
				freezeToolStripMenuItem.Image = MultiClient.Properties.Resources.Unfreeze;
			}
			else
			{
				freezeToolStripMenuItem.Text = "&Freeze";
				freezeToolStripMenuItem.Image = MultiClient.Properties.Resources.Freeze;
			}
		}

		private void gotoAddressToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			GoToSpecifiedAddress();
		}

		private string ValueString(int address)
		{
			if (address != -1)
			{
				return String.Format(DigitFormatString, (int)MakeValue(address)).Trim();
			}
			else
			{
				return "";
			}
		}

		private void OpenFindBox()
		{

			FindStr = GetFindValues();
			if (!HexFind1.IsHandleCreated || HexFind1.IsDisposed)
			{
				HexFind1 = new HexFind();
				Point p = PointToScreen(AddressesLabel.Location);
				HexFind1.SetLocation(p);
				HexFind1.SetInitialValue(FindStr);
				HexFind1.Show();
			}
			else
			{
				HexFind1.SetInitialValue(FindStr);
				HexFind1.Focus();
			}
		}

		private string GetFindValues()
		{
			string values = "";
			if (addressHighlighted > 0)
			{
				values += ValueString(GetHighlightedAddress());
				foreach (int x in SecondaryHighlightedAddresses)
				{
					values += ValueString(x);
				}
			}
			return values;
		}

		public void FindNext(string value)
		{
			int found = 0;

			string search = value.Replace(" ", "").ToUpper();
			if (search.Length == 0)
				return;

			int numByte = search.Length / 2;

			int startByte = 0;
			if (addressHighlighted == -1)
			{
				startByte = 0;
			}
			else if (addressHighlighted >= (Domain.Size - 1 - numByte))
			{
				startByte = 0;
			}
			else
			{
				startByte = addressHighlighted + DataSize;
			}

			for (int i = startByte; i < (Domain.Size - numByte); i++)
			{
				StringBuilder ramblock = new StringBuilder();
				for (int j = 0; j < numByte; j++)
				{
					ramblock.Append(String.Format("{0:X2}", (int)Domain.PeekByte(i + j)));
				}
				string block = ramblock.ToString().ToUpper();
				if (search == block)
				{
					found = i;
					break;
				}
			}

			if (found > 0)
			{
				HighlightSecondaries(search, found);
				GoToAddress(found);
				FindStr = search;
				MemoryViewerBox.Focus();
			}
		}

		public void FindPrev(string value)
		{
			int found = 0;

			string search = value.Replace(" ", "").ToUpper();
			if (search.Length == 0)
				return;

			int numByte = search.Length / 2;

			int startByte = 0;
			if (addressHighlighted == -1)
			{
				startByte = Domain.Size - DataSize;
			}
			else
			{
				startByte = addressHighlighted - 1;
			}

			for (int i = startByte; i >= 0; i--)
			{
				StringBuilder ramblock = new StringBuilder();
				for (int j = 0; j < numByte; j++)
				{
					ramblock.Append(String.Format("{0:X2}", (int)Domain.PeekByte(i + j)));
				}
				string block = ramblock.ToString().ToUpper();
				if (search == block)
				{
					found = i;
					break;
				}
			}

			if (found > 0)
			{
				HighlightSecondaries(search, found);
				GoToAddress(found);
				FindStr = search;
				MemoryViewerBox.Focus();
			}
		}

		private void HighlightSecondaries(string value, int found)
		{
			//This function assumes that the primary highlighted value has been set and sets the remaining characters in this string
			SecondaryHighlightedAddresses.Clear();
			
			int addrLength = DataSize * 2;
			if (value.Length <= addrLength)
			{
				return;
			}
			int numToHighlight = ((value.Length / addrLength)) - 1;

			for (int i = 0; i < numToHighlight; i++)
			{
				SecondaryHighlightedAddresses.Add(found + 1 + i);
			}
			
		}

		private void Find()
		{
			InputPrompt prompt = new InputPrompt();
			prompt.SetMessage("Enter a set of hex values to search for");
			prompt.SetCasing(CharacterCasing.Upper);
			prompt.TextInputType = InputPrompt.InputType.HEX;
			if (addressHighlighted > 0)
			{
				string values = ValueString(GetHighlightedAddress());
				foreach (int x in SecondaryHighlightedAddresses)
				{
					values += ValueString(x);
				}
				prompt.SetInitialValue(values);
			}
			prompt.ShowDialog();


			if (prompt.UserOK)
			{
				int found = 0;

				string search = prompt.UserText.Replace(" ", "").ToUpper();
				if (search.Length == 0)
					return;

				int numByte = search.Length / 2;

				int startByte = 0;
				if (addressHighlighted == -1)
				{
					startByte = 0;
				}
				else if (addressHighlighted >= (Domain.Size - 1 - numByte))
				{
					startByte = 0;
				}
				else
				{
					startByte = addressHighlighted + DataSize;
				}

				for (int i = startByte; i < (Domain.Size - numByte); i++)
				{
					StringBuilder ramblock = new StringBuilder();
					for (int j = 0; j < numByte; j++)
					{
						ramblock.Append(String.Format("{0:X2}", (int)Domain.PeekByte(i + j)));
					}
					string block = ramblock.ToString().ToUpper();
					if (search == block)
					{
						found = i;
						break;
					}
				}

				if (found > 0)
				{
					GoToAddress(found);

				}
				else
				{
					MessageBox.Show("Could not find the values: " + search);
				}
			}
		}

		private void Copy()
		{
			string value = ValueString(GetHighlightedAddress());
			foreach (int x in SecondaryHighlightedAddresses)
			{
				value += MakeValue(x);
			}
			if (!String.IsNullOrWhiteSpace(value))
			{
				Clipboard.SetDataObject(value);
			}
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Copy();
		}

		private void Paste()
		{
			IDataObject iData = Clipboard.GetDataObject();

			if (iData.GetDataPresent(DataFormats.Text))
			{
				string clipboardRaw = (String)iData.GetData(DataFormats.Text);
				string hex = InputValidate.DoHexString(clipboardRaw);

				int numBytes = hex.Length / 2;
				for (int i = 0; i < numBytes; i++)
				{
					int value = int.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
					int address = addressHighlighted + i;
					Domain.PokeByte(address, (byte)value);
				}

				UpdateValues();
			}
			else
			{
				//Do nothing
			}
		}

		private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Paste();
		}

		private void findToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			OpenFindBox();
			//Find();
		}

		private void saveAsBinaryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveAsBinary();
		}

		private void setColorsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			HexColors_Form h = new HexColors_Form();
			h.Show();
		}

		private void setColorsToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			HexColors_Form h = new HexColors_Form();
			Global.Sound.StopSound();
			h.ShowDialog();
			Global.Sound.StartSound();
		}

		private void resetToDefaultToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			this.MemoryViewerBox.BackColor = Color.FromName("Control");
			this.MemoryViewerBox.ForeColor = Color.FromName("ControlText");
			this.menuStrip1.BackColor = Color.FromName("Control");
			this.Header.BackColor = Color.FromName("Control");
			this.Header.ForeColor = Color.FromName("ControlText");
			Global.Config.HexMenubarColor = Color.FromName("Control");
			Global.Config.HexForegrndColor = Color.FromName("ControlText");
			Global.Config.HexBackgrndColor = Color.FromName("Control");
			Global.Config.HexFreezeColor = Color.LightBlue;
			Global.Config.HexHighlightColor = Color.Pink;
			Global.Config.HexHighlightFreezeColor = Color.Violet;
		}

		private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			Copy();
		}

		private void pasteToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			Paste();
		}

		private void findNextToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FindNext(FindStr);
		}

		private void findPrevToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FindPrev(FindStr);
		}

		private void editToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (String.IsNullOrWhiteSpace(FindStr))
			{
				findNextToolStripMenuItem.Enabled = false;
				findPrevToolStripMenuItem.Enabled = false;
			}
			else
			{
				findNextToolStripMenuItem.Enabled = true;
				findPrevToolStripMenuItem.Enabled = true;
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (CurrentROMIsArchive())
			{
				return;
			}
			else
			{
				FileInfo file = new FileInfo(Global.MainForm.CurrentlyOpenRom);
				SaveFileBinary(file);
			}
		}

		private void fileToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (Domain.Name == "ROM File")
			{
				if (!CurrentROMIsArchive())
				{
					saveToolStripMenuItem.Visible = true;
				}
				else
				{
					saveToolStripMenuItem.Visible = false;
				}

				saveAsBinaryToolStripMenuItem.Text = "Save as ROM...";
			}
			else
			{
				saveAsBinaryToolStripMenuItem.Text = "Save as binary...";
			}
		}

		private void pokeAddressToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private void pokeAddressToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private void vScrollBar1_ValueChanged(object sender, EventArgs e)
		{
			UpdateValues();
		}


	}
} 
