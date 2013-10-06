using System;
using System.Collections.Generic;
using System.ComponentModel;
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

		private int defaultWidth;
		private int defaultHeight;
		private readonly List<ToolStripMenuItem> domainMenuItems = new List<ToolStripMenuItem>();
		private int RowsVisible = 0;
		private int NumDigits = 4;
		private string NumDigitsStr = "{0:X4}";
		private string DigitFormatString = "{0:X2}";
		private readonly char[] nibbles = { 'G', 'G', 'G', 'G', 'G', 'G', 'G', 'G' };    //G = off 0-9 & A-F are acceptable values
		private int addressHighlighted = -1;
		private readonly List<int> SecondaryHighlightedAddresses = new List<int>();
		private int addressOver = -1;
		private int maxRow = 0;
		private MemoryDomain Domain = new MemoryDomain("NULL", 1024, Endian.Little, addr => 0, (a, v) => { v = 0; });
		private string info = "";
		private int row;
		private int addr;
		private const int fontHeight = 14;
		private const int fontWidth = 7; //Width of 1 digits
		private string FindStr = "";
		private bool loaded;
		private bool MouseIsDown;
		private byte[] ROM;
		private MemoryDomain ROMDomain;

		// Configurations
		private bool AutoLoad;
		private bool SaveWindowPosition;
		private int Wndx = -1;
		private int Wndy = -1;
		private int Width_ = -1;
		private int Height_ = -1;
		private bool BigEndian;
		private int DataSize;

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
				Global.Config.HexEditorWndx = loaded ? Location.X : Wndx;
				Global.Config.HexEditorWndy = loaded ? Location.Y : Wndy;
				Global.Config.HexEditorWidth = loaded ? Right - Left : Width_;
				Global.Config.HexEditorHeight = loaded ? Bottom - Top : Height_;
			}
			Global.Config.HexEditorBigEndian = BigEndian;
			Global.Config.HexEditorDataSize = DataSize;
		}

		private void HexEditor_Load(object sender, EventArgs e)
		{
			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;
			if (SaveWindowPosition)
			{
				if (Wndx >= 0 && Wndy >= 0)
				{
					Location = new Point(Wndx, Wndy);
				}

				if (Width_ >= 0 && Height_ >= 0)
				{
					Size = new Size(Width_, Height_);
				}
			}
			SetMemoryDomainMenu();
			SetDataSize(DataSize);
			UpdateValues();
			loaded = true;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed) return;

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
					addrStr.Append("    "); //Hack to line things up better between 4 and 6
				}
				else if (NumDigits == 6)
				{
					addrStr.Append("  ");
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
					if (addr + j + DataSize <= Domain.Size)
					{
						rowStr.AppendFormat(DigitFormatString, MakeValue(addr + j));
					}
					else
					{
						for (int t = 0; t < DataSize; t++)
							rowStr.Append("  ");
						rowStr.Append(' ');
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

		private int MakeValue(int address)
		{
			
			switch (DataSize)
			{
				default:
				case 1:
					return Domain.PeekByte(address);
				case 2:
					if (BigEndian)
					{
						int value = 0;
						value |= Domain.PeekByte(address) << 8;
						value |= Domain.PeekByte(address + 1);
						return value;
					}
					else
					{
						int value = 0;
						value |= Domain.PeekByte(address);
						value |= Domain.PeekByte(address + 1) << 8;
						return value;
					}
				case 4:
					if (BigEndian)
					{
						int value = 0;
						value |= Domain.PeekByte(address) << 24;
						value |= Domain.PeekByte(address + 1) << 16;
						value |= Domain.PeekByte(address + 2) << 8;
						value |= Domain.PeekByte(address + 3) << 0;
						return value;
					}
					else
					{
						int value = 0;
						value |= Domain.PeekByte(address) << 0;
						value |= Domain.PeekByte(address + 1) << 8;
						value |= Domain.PeekByte(address + 2) << 16;
						value |= Domain.PeekByte(address + 3) << 24;
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
			SetHeader();
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed) return;
			
			int? theDomain;
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
				SetMemoryDomain((int) theDomain);
			}
			SetHeader();
			
			ResetScrollBar();


			SetDataSize(DataSize);
			UpdateValues();
			AddressLabel.Text = GenerateAddressString();
		}

		private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(defaultWidth, defaultHeight);
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

			if (HighlightedAddress.HasValue && IsFrozen(HighlightedAddress.Value))
			{
				freezeAddressToolStripMenuItem.Image = Properties.Resources.Unfreeze;
				freezeAddressToolStripMenuItem.Text = "Un&freeze Address";
			}
			else
			{
				freezeAddressToolStripMenuItem.Image = Properties.Resources.Freeze;
				freezeAddressToolStripMenuItem.Text = "&Freeze Address";
			}
			

			if (HighlightedAddress.HasValue)
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
			if (0 >= vScrollBar1.Minimum &&  0 <= vScrollBar1.Maximum)
			{
				vScrollBar1.Value = 0;
			}
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
			//<zeromus> THIS IS HORRIBLE.
			if (pos == 999)
			{
				//<zeromus> THIS IS HORRIBLE.
				ROM = GetRomBytes() ?? new byte[] { 0xFF };

				//<zeromus> THIS IS HORRIBLE.
				ROMDomain = new MemoryDomain("ROM File", ROM.Length, Endian.Little,
					i => ROM[i],
					(i, value) => ROM[i] = value);

				//<zeromus> THIS IS HORRIBLE.
				Domain = ROMDomain;
			}
			else if (pos < Global.Emulator.MemoryDomains.Count)  //Sanity check
			{
				SetMemoryDomain(Global.Emulator.MemoryDomains[pos]);
			}
			SetHeader();
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
			
			for (int i = 0; i < Global.Emulator.MemoryDomains.Count; i++)
			{
				if (Global.Emulator.MemoryDomains[i].Size > 0)
				{
					string str = Global.Emulator.MemoryDomains[i].ToString();
					var item = new ToolStripMenuItem {Text = str};
					{
						int z = i;
						item.Click += (o, ev) => SetMemoryDomain(z);
					}
					if (i == 0)
					{
						SetMemoryDomain(i);
					}
					memoryDomainsToolStripMenuItem.DropDownItems.Add(item);
					domainMenuItems.Add(item);
				}
			}
			
			//Add ROM File memory domain
			//<zeromus> THIS IS HORRIBLE.
			var rom_item = new ToolStripMenuItem {Text = "ROM File"};
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
			AddressLabel.Text = GenerateAddressString();
		}

		private void ClearNibbles()
		{
			for (int x = 0; x < 8; x++)
			{
				nibbles[x] = 'G';
			}
		}

		private void GoToAddress(int address)
		{
			if (address < 0)
			{
				address = 0;
			}

			if (address >= Domain.Size)
			{
				address = Domain.Size - 1;
			}

			SetHighlighted(address);
			ClearNibbles();
			UpdateValues();
			MemoryViewerBox.Refresh();
			AddressLabel.Text = GenerateAddressString();
		}

		public void SetToAddresses(List<int> addresses)
		{
			if (addresses.Any())
			{
				SetHighlighted(addresses[0]);
				SecondaryHighlightedAddresses.Clear();
				SecondaryHighlightedAddresses.AddRange(addresses.Where(x => x != addresses[0]).ToList());
				ClearNibbles();
				UpdateValues();
				MemoryViewerBox.Refresh();
				AddressLabel.Text = GenerateAddressString();
			}
		}

		public void SetHighlighted(int address)
		{
			if (address < 0)
			{
				address = 0;
			}

			if (address >= Domain.Size)
			{
				address = Domain.Size - 1;
			}

			if (!IsVisible(address))
			{
				int v = (address / 16) - RowsVisible + 1;
				if (v < 0)
				{
					v = 0;
				}
				vScrollBar1.Value = v;
			}
			addressHighlighted = address;
			addressOver = address;
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

		public bool IsVisible(int address)
		{
			int i = address >> 4;
			return i >= vScrollBar1.Value && i < (RowsVisible + vScrollBar1.Value);
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
			switch (DataSize)
			{
				default:
				case 1:
					return new ByteWatch(Domain, address, Watch.DisplayType.Hex, BigEndian, String.Empty);
				case 2:
					return new WordWatch(Domain, address, Watch.DisplayType.Hex, BigEndian, String.Empty);
				case 4:
					return new DWordWatch(Domain, address, Watch.DisplayType.Hex, BigEndian, String.Empty);
			}
		}

		private void AddToRamWatch()
		{
			if (HighlightedAddress.HasValue || SecondaryHighlightedAddresses.Count > 0)
			{
				Global.MainForm.LoadRamWatch(true);
			}

			if (HighlightedAddress.HasValue)
			{
				Global.MainForm.RamWatch1.AddWatch(MakeWatch(HighlightedAddress.Value));
			}
			foreach (int i in SecondaryHighlightedAddresses)
			{
				Global.MainForm.RamWatch1.AddWatch(MakeWatch(i));
			}
		}

		private void PokeAddress()
		{
			var addresses = new List<int>();
			if (HighlightedAddress.HasValue)
			{
				addresses.Add(HighlightedAddress.Value);
			}

			if (SecondaryHighlightedAddresses.Count > 0)
			{
				addresses.AddRange(SecondaryHighlightedAddresses);
			}

			if (addresses.Any())
			{
				var poke = new RamPoke
				{
					InitialLocation = GetAddressCoordinates(addresses[0])
				};

				var Watches = new List<Watch>();
				foreach (var address in addresses)
				{
					Watches.Add(Watch.GenerateWatch(
							Domain, 
							address, 
							(Watch.WatchSize)DataSize, 
							Watch.DisplayType.Hex,
							String.Empty,
							BigEndian));
				}

				poke.SetWatch(Watches);

				Global.Sound.StopSound();
				var result = poke.ShowDialog();
				UpdateValues();
				Global.Sound.StartSound();
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

		public int? HighlightedAddress
		{
			get
			{
				if (addressHighlighted >= 0)
				{
					return addressHighlighted;
				}
				else
				{
					return null; //Negative = no address highlighted
				}
			}
		}

		private bool IsFrozen(int address)
		{
			return Global.CheatList.IsActive(Domain, address);
		}

		private void ToggleFreeze()
		{
			if (HighlightedAddress.HasValue && IsFrozen(HighlightedAddress.Value))
			{
				UnFreezeAddress(HighlightedAddress.Value);
			}
			else
			{
				FreezeAddress(HighlightedAddress.Value);
			}

			foreach (int i in SecondaryHighlightedAddresses)
			{
				if (IsFrozen(i))
				{
					UnFreezeAddress(i);
				}
				else
				{
					FreezeAddress(i);
				}
			}

			UpdateRelatedDialogs();
		}

		private void UnFreezeAddress(int address) //TODO: refactor to int?
		{
			if (address >= 0)
			{
				var cheats = Global.CheatList.Where(x => x.Contains(address)).ToList();
				Global.CheatList.RemoveRange(cheats);
			}
			MemoryViewerBox.Refresh();
		}

		private Watch.WatchSize WatchSize
		{
			get
			{
				switch (DataSize)
				{
					default:
					case 1:
						return Watch.WatchSize.Byte;
					case 2:
						return Watch.WatchSize.Word;
					case 4:
						return Watch.WatchSize.DWord;
				}
			}
		}

		private void UpdateRelatedDialogs()
		{
			Global.MainForm.UpdateCheatStatus();
			Global.MainForm.RamSearch1.UpdateValues();
			Global.MainForm.RamWatch1.UpdateValues();
			Global.MainForm.Cheats_UpdateValues();
			UpdateValues();
		}

		private void FreezeAddress(int address) //TODO refactor to int?
		{
			if (address >= 0)
			{
				Watch watch = Watch.GenerateWatch(
					Domain,
					address,
					WatchSize,
					Watch.DisplayType.Hex,
					String.Empty,
					BigEndian);

				Global.CheatList.Add(new Cheat(watch, compare: null, enabled: true));

				MemoryViewerBox.Refresh();
				UpdateRelatedDialogs();
			}
		}

		private void freezeAddressToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFreeze();
		}

		private void CheckDomainMenuItems()
		{
			foreach (ToolStripMenuItem t in domainMenuItems)
			{
				if (Domain.Name == t.Text)
				{
					t.Checked = true;
				}
				else
				{
					t.Checked = false;
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
			
			int address;
			//Scroll value determines the first row
			int i = vScrollBar1.Value;
			int rowoffset = y / fontHeight;
			i += rowoffset;
			int colWidth;
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

			if (i >= 0 && i <= maxRow && column >= 0 && column < (16 / DataSize))
			{
				address = i * 16 + (column * DataSize);
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

			if (MouseIsDown)
			{
				DoShiftClick();
				MemoryViewerBox.Refresh();
			}
		}

		private void AddressesLabel_MouseClick(object sender, MouseEventArgs e)
		{
			
		}

		private void DoShiftClick()
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
					return new Point(((address % 16) * (fontWidth * 3)) + 67, (((address / 16) - vScrollBar1.Value) * fontHeight) + 30);
				case 2:
					return new Point((((address % 16) / DataSize) * (fontWidth * 5)) + 67, (((address / 16) - vScrollBar1.Value) * fontHeight) + 30);
				case 4:
					return new Point((((address % 16) / DataSize) * (fontWidth * 9)) + 67, (((address / 16) - vScrollBar1.Value) * fontHeight) + 30);
			}
		}

		private int GetTextOffset()
		{
			int start;
			switch (DataSize)
			{
				default:
				case 1:
					start = (16 * (fontWidth * 3)) + 67;
					break;
				case 2:
					start = ((16 / DataSize) * (fontWidth * 5)) + 67;
					break;
				case 4:
					start = ((16 / DataSize) * (fontWidth * 9)) + 67;
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
			var activeCheats = Global.CheatList.Where(x => x.Enabled);
			foreach(var cheat in activeCheats)
			{
				if (IsVisible(cheat.Address.Value))
				{
					if (Domain.ToString() == cheat.Domain.Name)
					{
						Rectangle rect = new Rectangle(GetAddressCoordinates(cheat.Address.Value), new Size(15 * DataSize, fontHeight));
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
				
				if (Global.CheatList.IsActive(Domain, addressHighlighted))
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

				if (Global.CheatList.IsActive(Domain, address))
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
			addressOver = -1;
			MemoryViewerBox.Refresh();
		}

		private void HexEditor_KeyDown(object sender, KeyEventArgs e)
		{
            if (e.Control && e.KeyCode == Keys.G)
            {
                GoToSpecifiedAddress();
                return;
            }
            if (e.Control && e.KeyCode == Keys.P)
            {
                PokeAddress();
                return;
            }
            
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
					{
						ToolHelpers.UnfreezeAll();
					}
					else
					{
						if (HighlightedAddress.HasValue)
						{
							UnFreezeAddress(HighlightedAddress.Value);
						}
					}
					break;
				case Keys.W:
					if (e.Modifiers == Keys.Control)
					{
						AddToRamWatch();
					}
					break;
				case Keys.Escape:
					SecondaryHighlightedAddresses.Clear();
					ClearHighlighted();
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


		private bool IsHexKeyCode(char key)
		{
			if (key >= 48 && key <= 57) //0-9
			{
				return true;
			}
			else if (key >= 65 && key <= 70) //A-F
			{
				return true;
			}
			else if (key >= 96 && key <= 106) //0-9 Numpad
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		//Winform key events suck at the numberpad, so this is necessary
		private char ForceCorrectKeyString(Keys keycode)
		{
			if ((int)keycode >= 96 && (int)keycode <= 106)
			{
				return (char)((int)keycode - 48);
			}
			else
			{
				return (char)keycode;
			}
		}

		private void HexEditor_KeyUp(object sender, KeyEventArgs e)
		{
			if (!IsHexKeyCode((char)e.KeyCode))
			{
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
						nibbles[0] = ForceCorrectKeyString(e.KeyCode);
						info = nibbles[0].ToString();
					}
					else
					{
						string temp = nibbles[0].ToString() + ForceCorrectKeyString(e.KeyCode).ToString();
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
						nibbles[0] = ForceCorrectKeyString(e.KeyCode);
						info = nibbles[0].ToString();
					}
					else if (nibbles[1] == 'G')
					{
						nibbles[1] = ForceCorrectKeyString(e.KeyCode);
						info = nibbles[1].ToString();
					}
					else if (nibbles[2] == 'G')
					{
						nibbles[2] = ForceCorrectKeyString(e.KeyCode);
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
						nibbles[0] = ForceCorrectKeyString(e.KeyCode);
						info = nibbles[0].ToString();
					}
					else if (nibbles[1] == 'G')
					{
						nibbles[1] = ForceCorrectKeyString(e.KeyCode);
						info = nibbles[1].ToString();
					}
					else if (nibbles[2] == 'G')
					{
						nibbles[2] = ForceCorrectKeyString(e.KeyCode);
						info = nibbles[2].ToString();
					}
					else if (nibbles[3] == 'G')
					{
						nibbles[3] = ForceCorrectKeyString(e.KeyCode);
						info = nibbles[3].ToString();
					}
					else if (nibbles[4] == 'G')
					{
						nibbles[4] = ForceCorrectKeyString(e.KeyCode);
						info = nibbles[4].ToString();
					}
					else if (nibbles[5] == 'G')
					{
						nibbles[5] = ForceCorrectKeyString(e.KeyCode);
						info = nibbles[5].ToString();
					}
					else if (nibbles[6] == 'G')
					{
						nibbles[6] = ForceCorrectKeyString(e.KeyCode);
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

						string temp4 = nibbles[6].ToString() + ForceCorrectKeyString(e.KeyCode).ToString();
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

		private void PokeWord(int address, byte _1, byte _2)
		{
			if (BigEndian)
			{
				Domain.PokeByte(address, _2);
				Domain.PokeByte(address + 1, _1);
			}
			else
			{
				Domain.PokeByte(address, _1);
				Domain.PokeByte(address + 1, _2);
			}
		}

		private void unfreezeAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToolHelpers.UnfreezeAll();
		}

		private void unfreezeAllToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			ToolHelpers.UnfreezeAll();
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
			if (HighlightedAddress.HasValue)
			{
				int address = HighlightedAddress.Value;
				byte value;
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
			if (Global.CheatList.IsActive(Domain, address))
			{
				UnFreezeAddress(address);
				Domain.PokeByte(address, value);
				FreezeAddress(address);
			}
			else
			{
				Domain.PokeByte(address, value);
			}
		}

		private void DecrementAddress()
		{
			
			if (HighlightedAddress.HasValue)
			{
				int address = HighlightedAddress.Value;
				byte value;
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
			copyToolStripMenuItem1.Visible = HighlightedAddress.HasValue || SecondaryHighlightedAddresses.Any();

			IDataObject iData = Clipboard.GetDataObject();

			pasteToolStripMenuItem1.Visible = iData != null && iData.GetDataPresent(DataFormats.Text);

			if (HighlightedAddress.HasValue && IsFrozen(HighlightedAddress.Value))
			{
				freezeToolStripMenuItem.Text = "Un&freeze";
				freezeToolStripMenuItem.Image = Properties.Resources.Unfreeze;
			}
			else
			{
				freezeToolStripMenuItem.Text = "&Freeze";
				freezeToolStripMenuItem.Image = Properties.Resources.Freeze;
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
				return String.Format(DigitFormatString, MakeValue(address)).Trim();
			}
			else
			{
				return String.Empty;
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
			if (HighlightedAddress.HasValue)
			{
				string values = ValueString(HighlightedAddress.Value);
				return SecondaryHighlightedAddresses.Aggregate(values, (current, x) => current + ValueString(x));
			}
			else
			{
				return "";
			}
		}

		public void FindNext(string value, Boolean wrap)
		{
			int found = -1;

			string search = value.Replace(" ", "").ToUpper();
			if (search.Length == 0)
				return;

			int numByte = search.Length / 2;

			int startByte;
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

			if (found > -1)
			{
				HighlightSecondaries(search, found);
				GoToAddress(found);
				FindStr = search;
				MemoryViewerBox.Focus();
			}
			else if (wrap == false)  // Search the opposite direction if not found
			{
				FindPrev(value, true);
			}
		}

		public void FindPrev(string value, Boolean wrap)
		{
			int found = -1;

			string search = value.Replace(" ", "").ToUpper();
			if (search.Length == 0)
				return;

			int numByte = search.Length / 2;

			int startByte;
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

			if (found > -1)
			{
				HighlightSecondaries(search, found);
				GoToAddress(found);
				FindStr = search;
				MemoryViewerBox.Focus();
			}
			else if (wrap == false) // Search the opposite direction if not found
			{
				FindPrev(value, true);
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

		private void Copy()
		{
			string value = HighlightedAddress.HasValue ? ValueString(HighlightedAddress.Value) : String.Empty;
			value = SecondaryHighlightedAddresses.Aggregate(value, (current, x) => current + ValueString(x));
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

			if (iData != null && iData.GetDataPresent(DataFormats.Text))
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
			MemoryViewerBox.BackColor = Color.FromName("Control");
			MemoryViewerBox.ForeColor = Color.FromName("ControlText");
			menuStrip1.BackColor = Color.FromName("Control");
			Header.BackColor = Color.FromName("Control");
			Header.ForeColor = Color.FromName("ControlText");
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
			FindNext(FindStr, false);
		}

		private void findPrevToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FindPrev(FindStr, false);
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

		private void AddressesLabel_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				int pointed_address = GetPointedAddress(e.X, e.Y);
				if (pointed_address >= 0)
				{
					if ((ModifierKeys & Keys.Control) == Keys.Control)
					{
						if (pointed_address == addressHighlighted)
						{
							ClearHighlighted();
						}
						else if (SecondaryHighlightedAddresses.Contains(pointed_address))
						{
							SecondaryHighlightedAddresses.Remove(pointed_address);
						}
						else
						{
							SecondaryHighlightedAddresses.Add(pointed_address);
						}
					}
					else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
					{
						DoShiftClick();
					}
					//else if (addressOver == addressHighlighted)
					//{
					//    ClearHighlighted();
					//}
					else
					{
						SetHighlighted(pointed_address);
						SecondaryHighlightedAddresses.Clear();
						FindStr = "";
					}

					MemoryViewerBox.Refresh();
				}

				MouseIsDown = true;
			}
		}

		private void AddressesLabel_MouseUp(object sender, MouseEventArgs e)
		{
			MouseIsDown = false;
		}

		private void AddressesLabel_Click(object sender, EventArgs e)
		{

		}

		private void MemoryViewerBox_Enter(object sender, EventArgs e)
		{

		}

        private void alwaysOnTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            alwaysOnTopToolStripMenuItem.Checked = alwaysOnTopToolStripMenuItem.Checked == false;
            this.TopMost = alwaysOnTopToolStripMenuItem.Checked;
        }
	}
} 
