using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.IO;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class HexEditor : Form, IToolForm
	{
		//TODO:
		//Increment/Decrement wrapping logic for 4 byte values is messed up
		//2 & 4 byte text area clicking is off

		private int _defaultWidth;
		private int _defaultHeight;
		private readonly List<ToolStripMenuItem> _domainMenuItems = new List<ToolStripMenuItem>();
		private int _rowsVisible;
		private int _numDigits = 4;
		private string _numDigitsStr = "{0:X4}";
		private string _digitFormatString = "{0:X2}";
		private readonly char[] _nibbles = { 'G', 'G', 'G', 'G', 'G', 'G', 'G', 'G' };    //G = off 0-9 & A-F are acceptable values
		private int _addressHighlighted = -1;
		private readonly List<int> _secondaryHighlightedAddresses = new List<int>();
		private int _addressOver = -1;
		private int _maxRow;
		private MemoryDomain _domain = new MemoryDomain("NULL", 1024, MemoryDomain.Endian.Little, addr => 0, (a, v) => { v = 0; });
		private int _row;
		private int _addr;
		private const int FontWidth = 7; //Width of 1 digits
		private string _findStr = String.Empty;
		private bool _loaded;
		private bool _mouseIsDown;
		private byte[] _rom;
		private MemoryDomain _romDomain;

		// Configurations
		private bool _autoLoad;
		private bool _saveWindowPosition;
		private int _wndx = -1;
		private int _wndy = -1;
		private int _width = -1;
		private int _height = -1;
		private bool _bigEndian;
		private int _dataSize;

		private HexFind _hexFind = new HexFind();

		public bool AskSave() { return true; }
		public bool UpdateBefore { get { return false; } }

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
			_autoLoad = Global.Config.AutoLoadHexEditor;
			_saveWindowPosition = Global.Config.SaveWindowPosition;
			_wndx = Global.Config.HexEditorWndx;
			_wndy = Global.Config.HexEditorWndy;
			_width = Global.Config.HexEditorWidth;
			_height = Global.Config.HexEditorHeight;
			_bigEndian = Global.Config.HexEditorBigEndian;
			_dataSize = Global.Config.HexEditorDataSize;
			//Colors
			menuStrip1.BackColor = Global.Config.HexMenubarColor;
			MemoryViewerBox.BackColor = Global.Config.HexBackgrndColor;
			MemoryViewerBox.ForeColor = Global.Config.HexForegrndColor;
			Header.BackColor = Global.Config.HexBackgrndColor;
			Header.ForeColor = Global.Config.HexForegrndColor;

		}

		public void SaveConfigSettings()
		{
			if (_hexFind.IsHandleCreated || !_hexFind.IsDisposed)
			{
				_hexFind.Close();
			}

			Global.Config.AutoLoadHexEditor = _autoLoad;
			Global.Config.HexEditorSaveWindowPosition = _saveWindowPosition;
			if (_saveWindowPosition)
			{
				Global.Config.HexEditorWndx = _loaded ? Location.X : _wndx;
				Global.Config.HexEditorWndy = _loaded ? Location.Y : _wndy;
				Global.Config.HexEditorWidth = _loaded ? Right - Left : _width;
				Global.Config.HexEditorHeight = _loaded ? Bottom - Top : _height;
			}
			Global.Config.HexEditorBigEndian = _bigEndian;
			Global.Config.HexEditorDataSize = _dataSize;
		}

		private void HexEditor_Load(object sender, EventArgs e)
		{
			_defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			_defaultHeight = Size.Height;
			if (_saveWindowPosition)
			{
				if (_wndx >= 0 && _wndy >= 0)
				{
					Location = new Point(_wndx, _wndy);
				}

				if (_width >= 0 && _height >= 0)
				{
					Size = new Size(_width, _height);
				}
			}
			SetMemoryDomainMenu();
			SetDataSize(_dataSize);
			UpdateValues();
			_loaded = true;
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
			var addrStr = new StringBuilder();

			for (var i = 0; i < _rowsVisible; i++)
			{
				_row = i + vScrollBar1.Value;
				_addr = (_row << 4);
				if (_addr >= _domain.Size)
				{
					break;
				}

				if (_numDigits == 4)
				{
					addrStr.Append("    "); //Hack to line things up better between 4 and 6
				}
				else if (_numDigits == 6)
				{
					addrStr.Append("  ");
				}
				addrStr.AppendLine(_addr.ToHexString(_numDigits));
			}

			return addrStr.ToString();
		}

		public string GenerateMemoryViewString()
		{
			var rowStr = new StringBuilder();

			for (var i = 0; i < _rowsVisible; i++)
			{
				_row = i + vScrollBar1.Value;
				_addr = (_row << 4);
				if (_addr >= _domain.Size)
					break;

				for (var j = 0; j < 16; j += _dataSize)
				{
					if (_addr + j + _dataSize <= _domain.Size)
					{
						rowStr.AppendFormat(_digitFormatString, MakeValue(_addr + j));
					}
					else
					{
						for (var t = 0; t < _dataSize; t++)
						{
							rowStr.Append("  ");
						}
						rowStr.Append(' ');
					}
				}
				rowStr.Append("  | ");
				for (var k = 0; k < 16; k++)
				{
					if (_addr + k < _domain.Size)
					{
						rowStr.Append(Remap(_domain.PeekByte(_addr + k)));
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

			switch (_dataSize)
			{
				default:
				case 1:
					return _domain.PeekByte(address);
				case 2:
					if (_bigEndian)
					{
						var value = 0;
						value |= _domain.PeekByte(address) << 8;
						value |= _domain.PeekByte(address + 1);
						return value;
					}
					else
					{
						var value = 0;
						value |= _domain.PeekByte(address);
						value |= _domain.PeekByte(address + 1) << 8;
						return value;
					}
				case 4:
					if (_bigEndian)
					{
						var value = 0;
						value |= _domain.PeekByte(address) << 24;
						value |= _domain.PeekByte(address + 1) << 16;
						value |= _domain.PeekByte(address + 2) << 8;
						value |= _domain.PeekByte(address + 3) << 0;
						return value;
					}
					else
					{
						var value = 0;
						value |= _domain.PeekByte(address) << 0;
						value |= _domain.PeekByte(address + 1) << 8;
						value |= _domain.PeekByte(address + 2) << 16;
						value |= _domain.PeekByte(address + 3) << 24;
						return value;
					}
			}
		}

		private static int? GetDomainInt(string name)
		{
			for (var i = 0; i < Global.Emulator.MemoryDomains.Count; i++)
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
			SetMemoryDomain(GetDomainInt(domain.Name) ?? 0);
			SetHeader();
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed) return;

			var theDomain = _domain.Name.ToLower() == "rom file" ? 999 : GetDomainInt(_domain.Name);

			SetMemoryDomainMenu(); //Calls update routines

			if (theDomain.HasValue)
			{
				SetMemoryDomain(theDomain.Value);
			}

			SetHeader();
			ResetScrollBar();
			SetDataSize(_dataSize);
			UpdateValues();
			AddressLabel.Text = GenerateAddressString();
		}

		private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);
			SetUpScrollBar();
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_autoLoad ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			enToolStripMenuItem.Checked = _bigEndian;
			DataSizeByteMenuItem.Checked = _dataSize == 1;
			DataSizeWordMenuItem.Checked = _dataSize == 2;
			DataSizeDWordMenuItem.Checked = _dataSize == 4;

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


			addToRamWatchToolStripMenuItem1.Enabled =
				freezeAddressToolStripMenuItem.Enabled =
				HighlightedAddress.HasValue;
		}

		public void SetMemoryDomain(MemoryDomain d)
		{
			_domain = d;
			_bigEndian = d.EndianType == MemoryDomain.Endian.Big;
			_maxRow = _domain.Size / 2;
			SetUpScrollBar();
			if (0 >= vScrollBar1.Minimum && 0 <= vScrollBar1.Maximum)
			{
				vScrollBar1.Value = 0;
			}
			Refresh();
		}

		private static bool CurrentRomIsArchive()
		{
			var path = GlobalWin.MainForm.CurrentlyOpenRom;
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

				return file.IsArchive;
			}
		}

		private static byte[] GetRomBytes()
		{
			var path = GlobalWin.MainForm.CurrentlyOpenRom;
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
				_rom = GetRomBytes() ?? new byte[] { 0xFF };

				//<zeromus> THIS IS HORRIBLE.
				_romDomain = new MemoryDomain("ROM File", _rom.Length, MemoryDomain.Endian.Little,
					i => _rom[i],
					(i, value) => _rom[i] = value);

				//<zeromus> THIS IS HORRIBLE.
				_domain = _romDomain;
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
			var memoryDomain = _domain.ToString();
			var systemID = Global.Emulator.SystemId;
			var addresses = _domain.Size / _dataSize;
			var addressesString = "0x" + string.Format("{0:X8}", addresses).TrimStart('0');
			MemoryViewerBox.Text = systemID + " " + memoryDomain + "  -  " + addressesString + " addresses";
		}

		private void SetMemoryDomainMenu()
		{
			memoryDomainsToolStripMenuItem.DropDownItems.Clear();

			for (var i = 0; i < Global.Emulator.MemoryDomains.Count; i++)
			{
				if (Global.Emulator.MemoryDomains[i].Size > 0)
				{
					var str = Global.Emulator.MemoryDomains[i].ToString();
					var item = new ToolStripMenuItem { Text = str };
					{
						var temp = i;
						item.Click += (o, ev) => SetMemoryDomain(temp);
					}
					if (i == 0)
					{
						SetMemoryDomain(i);
					}
					memoryDomainsToolStripMenuItem.DropDownItems.Add(item);
					_domainMenuItems.Add(item);
				}
			}

			//Add ROM File memory domain
			//<zeromus> THIS IS HORRIBLE.
			var rom_item = new ToolStripMenuItem { Text = "ROM File" };
			rom_item.Click += (o, ev) => SetMemoryDomain(999); //999 will denote ROM file
			memoryDomainsToolStripMenuItem.DropDownItems.Add(rom_item);
			_domainMenuItems.Add(rom_item);
		}



		private void goToAddressToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GoToSpecifiedAddress();
		}

		private static int GetNumDigits(Int32 i)
		{
			if (i <= 0x10000) return 4;
			if (i <= 0x1000000) return 6;
			else return 8;
		}

		private Point GetPromptPoint()
		{
			return PointToScreen(
				new Point(MemoryViewerBox.Location.X + 30, MemoryViewerBox.Location.Y + 30)
			);
		}

		public void GoToSpecifiedAddress()
		{
			var inputPrompt = new InputPrompt {Text = "Go to Address", _Location = GetPromptPoint()};
			inputPrompt.SetMessage("Enter a hexadecimal value");
			GlobalWin.Sound.StopSound();
			inputPrompt.ShowDialog();
			GlobalWin.Sound.StartSound();

			if (inputPrompt.UserOK)
			{
				if (InputValidate.IsValidHexNumber(inputPrompt.UserText))
				{
					GoToAddress(int.Parse(inputPrompt.UserText, NumberStyles.HexNumber));
				}
			}
			AddressLabel.Text = GenerateAddressString();
		}

		private void ClearNibbles()
		{
			for (var i = 0; i < 8; i++)
			{
				_nibbles[i] = 'G';
			}
		}

		private void GoToAddress(int address)
		{
			if (address < 0)
			{
				address = 0;
			}

			if (address >= _domain.Size)
			{
				address = _domain.Size - 1;
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
				_secondaryHighlightedAddresses.Clear();
				_secondaryHighlightedAddresses.AddRange(addresses.Where(x => x != addresses[0]).ToList());
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

			if (address >= _domain.Size)
			{
				address = _domain.Size - 1;
			}

			if (!IsVisible(address))
			{
				var value = (address / 16) - _rowsVisible + 1;
				if (value < 0)
				{
					value = 0;
				}
				vScrollBar1.Value = value;
			}
			_addressHighlighted = address;
			_addressOver = address;
			ClearNibbles();
			UpdateFormText();
		}

		private void UpdateFormText()
		{
			if (_addressHighlighted >= 0)
				Text = "Hex Editor - Editing Address 0x" + String.Format(_numDigitsStr, _addressHighlighted);
			else
				Text = "Hex Editor";
		}

		public bool IsVisible(int address)
		{
			var i = address >> 4;
			return i >= vScrollBar1.Value && i < (_rowsVisible + vScrollBar1.Value);
		}

		private void HexEditor_Resize(object sender, EventArgs e)
		{
			SetUpScrollBar();
			UpdateValues();
		}

		private void SetHeader()
		{
			switch (_dataSize)
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
			_numDigits = GetNumDigits(_domain.Size);
			_numDigitsStr = "{0:X" + _numDigits + "}  ";
		}

		public void SetDataSize(int size)
		{
			if (size == 1 || size == 2 || size == 4)
				_dataSize = size;
			_digitFormatString = "{0:X" + (_dataSize * 2) + "} ";
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
			_bigEndian ^= true;
			UpdateValues();
		}

		private Watch MakeWatch(int address)
		{
			switch (_dataSize)
			{
				default:
				case 1:
					return new ByteWatch(_domain, address, Watch.DisplayType.Hex, _bigEndian, String.Empty);
				case 2:
					return new WordWatch(_domain, address, Watch.DisplayType.Hex, _bigEndian, String.Empty);
				case 4:
					return new DWordWatch(_domain, address, Watch.DisplayType.Hex, _bigEndian, String.Empty);
			}
		}

		private void AddToRamWatch()
		{
			if (HighlightedAddress.HasValue || _secondaryHighlightedAddresses.Count > 0)
			{
				GlobalWin.MainForm.LoadRamWatch(true);
			}

			if (HighlightedAddress.HasValue)
			{
				GlobalWin.Tools.RamWatch.AddWatch(MakeWatch(HighlightedAddress.Value));
			}
			
			_secondaryHighlightedAddresses.ForEach(addr => 
				GlobalWin.Tools.RamWatch.AddWatch(MakeWatch(addr))
			);
		}

		private void PokeAddress()
		{
			var addresses = new List<int>();
			if (HighlightedAddress.HasValue)
			{
				addresses.Add(HighlightedAddress.Value);
			}

			if (_secondaryHighlightedAddresses.Count > 0)
			{
				addresses.AddRange(_secondaryHighlightedAddresses);
			}

			if (addresses.Any())
			{
				var poke = new RamPoke
				{
					InitialLocation = GetAddressCoordinates(addresses[0])
				};

				var Watches = addresses.Select(address => 
					Watch.GenerateWatch(
						_domain, address, 
						(Watch.WatchSize)_dataSize,
						Watch.DisplayType.Hex,
						String.Empty,
						_bigEndian
				)).ToList();

				poke.SetWatch(Watches);

				GlobalWin.Sound.StopSound();
				poke.ShowDialog();
				UpdateValues();
				GlobalWin.Sound.StartSound();
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

		public void PokeHighlighted(int value)
		{
			if (_addressHighlighted >= 0)
			{
				switch (_dataSize)
				{
					default:
					case 1:
						_domain.PokeByte(_addressHighlighted, (byte)value);
						break;
					case 2:
						_domain.PokeWord(_addressHighlighted, (ushort)value, _bigEndian);
						break;
					case 4:
						_domain.PokeDWord(_addressHighlighted, (uint)value, _bigEndian);
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
			_saveWindowPosition ^= true;
		}

		private void settingsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = _autoLoad;
			saveWindowsSettingsToolStripMenuItem.Checked = _saveWindowPosition;
		}

		private void freezeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFreeze();
		}

		public int? HighlightedAddress
		{
			get
			{
				if (_addressHighlighted >= 0)
				{
					return _addressHighlighted;
				}
				else
				{
					return null; //Negative = no address highlighted
				}
			}
		}

		private bool IsFrozen(int address)
		{
			return Global.CheatList.IsActive(_domain, address);
		}

		private void ToggleFreeze()
		{
			if (HighlightedAddress.HasValue)
			{
				if (IsFrozen(HighlightedAddress.Value))
				{
					UnFreezeAddress(HighlightedAddress.Value);
				}
				else
				{
					FreezeAddress(HighlightedAddress.Value);
				}
			}

			foreach (var addr in _secondaryHighlightedAddresses)
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

			UpdateRelatedDialogs();
		}

		private void UnFreezeAddress(int address)
		{
			if (address >= 0) //TODO: can't unfreeze address 0??
			{
				Global.CheatList.RemoveRange(
					Global.CheatList.Where(x => x.Contains(address))
				);
			}
			MemoryViewerBox.Refresh();
		}

		private Watch.WatchSize WatchSize
		{
			get
			{
				return (Watch.WatchSize) _dataSize;
			}
		}

		private void UpdateRelatedDialogs()
		{
			GlobalWin.Tools.UpdateValues<RamWatch>();
			GlobalWin.Tools.UpdateValues<RamSearch>();
			GlobalWin.Tools.UpdateValues<Cheats>();
			GlobalWin.MainForm.UpdateCheatStatus();
			UpdateValues();
		}

		private void FreezeAddress(int address) //TODO refactor to int?
		{
			if (address >= 0)
			{
				var watch = Watch.GenerateWatch(
					_domain,
					address,
					WatchSize,
					Watch.DisplayType.Hex,
					String.Empty,
					_bigEndian);

				Global.CheatList.Add(new Cheat(
					watch,
					watch.Value ?? 0));

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
			foreach (var t in _domainMenuItems)
			{
				t.Checked = _domain.Name == t.Text;
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
			//TODO: use StringBuilder!
			var path = GetSaveFileFromUser();
			if (!String.IsNullOrWhiteSpace(path))
			{
				var file = new FileInfo(path);
				using (var sw = new StreamWriter(file.FullName))
				{
					var str = String.Empty;

					for (var i = 0; i < _domain.Size / 16; i++)
					{
						for (var j = 0; j < 16; j++)
						{
							str += String.Format("{0:X2} ", _domain.PeekByte((i * 16) + j));
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

		private void SaveFileBinary(string path)
		{
			var file = new FileInfo(path);
			using (var binWriter = new BinaryWriter(File.Open(file.FullName, FileMode.Create)))
			{
				for (var i = 0; i < _domain.Size; i++)
				{
					binWriter.Write(_domain.PeekByte(i));
				}
			}
		}

		private static string GetSaveFileFromUser()
		{
			var sfd = new SaveFileDialog();

			if (!(Global.Emulator is NullEmulator))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
			}
			else
			{
				sfd.FileName = "MemoryDump";
			}

			sfd.InitialDirectory = PathManager.GetPlatformBase(Global.Emulator.SystemId);

			sfd.Filter = "Text (*.txt)|*.txt|All Files|*.*";
			sfd.RestoreDirectory = true;
			GlobalWin.Sound.StopSound();
			var result = sfd.ShowDialog();
			GlobalWin.Sound.StartSound();

			return result == DialogResult.OK ? sfd.FileName : String.Empty;
		}

		private string GetSaveFileFilter()
		{
			if (_domain.Name == "ROM File")
			{
				var extension = Path.GetExtension(GlobalWin.MainForm.CurrentlyOpenRom);

				return "Binary (*" + extension + ")|*" + extension + "|All Files|*.*";
			}
			else
			{
				return "Binary (*.bin)|*.bin|All Files|*.*";
			}
		}

		private string GetBinarySaveFileFromUser()
		{
			var sfd = new SaveFileDialog();

			if (!(Global.Emulator is NullEmulator))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
			}
			else
			{
				sfd.FileName = "MemoryDump";
			}

			sfd.InitialDirectory = PathManager.GetPlatformBase(Global.Emulator.SystemId);
			sfd.Filter = GetSaveFileFilter();
			sfd.RestoreDirectory = true;
			GlobalWin.Sound.StopSound();
			var result = sfd.ShowDialog();
			GlobalWin.Sound.StartSound();

			return result == DialogResult.OK ? sfd.FileName : String.Empty;
		}

		public void ResetScrollBar()
		{
			vScrollBar1.Value = 0;
			SetUpScrollBar();
			Refresh();
		}

		public void SetUpScrollBar()
		{
			_rowsVisible = ((MemoryViewerBox.Height - (fontHeight * 2) - (fontHeight / 2)) / fontHeight);
			var totalRows = _domain.Size / 16;

			vScrollBar1.Maximum = totalRows - 1;
			vScrollBar1.LargeChange = _rowsVisible;
			vScrollBar1.Visible = totalRows > _rowsVisible;

			AddressLabel.Text = GenerateAddressString();
		}

		private int GetPointedAddress(int x, int y)
		{

			int address;
			//Scroll value determines the first row
			var i = vScrollBar1.Value;
			var rowoffset = y / fontHeight;
			i += rowoffset;
			int colWidth;
			switch (_dataSize)
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
			var column = (x /*- 43*/) / (FontWidth * colWidth);

			var start = GetTextOffset() - 50;
			if (x > start)
			{
				column = (x - start) / (FontWidth / _dataSize);
			}

			if (i >= 0 && i <= _maxRow && column >= 0 && column < (16 / _dataSize))
			{
				address = i * 16 + (column * _dataSize);
			}
			else
			{
				address = -1;
			}
			return address;
		}

		private void HexEditor_ResizeEnd(object sender, EventArgs e)
		{
			SetUpScrollBar();
		}

		private void AddressesLabel_MouseMove(object sender, MouseEventArgs e)
		{
			_addressOver = GetPointedAddress(e.X, e.Y);

			if (_mouseIsDown)
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
			if (_addressOver >= 0)
			{
				_secondaryHighlightedAddresses.Clear();
				if (_addressOver < _addressHighlighted)
				{
					for (var x = _addressOver; x < _addressHighlighted; x++)
					{
						_secondaryHighlightedAddresses.Add(x);
					}
				}
				else if (_addressOver > _addressHighlighted)
				{
					for (var x = _addressHighlighted + _dataSize; x <= _addressOver; x++)
					{
						_secondaryHighlightedAddresses.Add(x);
					}
				}
			}
		}

		private void ClearHighlighted()
		{
			_addressHighlighted = -1;
			UpdateFormText();
			MemoryViewerBox.Refresh();
		}

		private Point GetAddressCoordinates(int address)
		{
			switch (_dataSize)
			{
				default:
				case 1:
					return new Point(((address % 16) * (FontWidth * 3)) + 67, (((address / 16) - vScrollBar1.Value) * fontHeight) + 30);
				case 2:
					return new Point((((address % 16) / _dataSize) * (FontWidth * 5)) + 67, (((address / 16) - vScrollBar1.Value) * fontHeight) + 30);
				case 4:
					return new Point((((address % 16) / _dataSize) * (FontWidth * 9)) + 67, (((address / 16) - vScrollBar1.Value) * fontHeight) + 30);
			}
		}

		private int GetTextOffset()
		{
			int start;
			switch (_dataSize)
			{
				default:
				case 1:
					start = (16 * (FontWidth * 3)) + 67;
					break;
				case 2:
					start = ((16 / _dataSize) * (FontWidth * 5)) + 67;
					break;
				case 4:
					start = ((16 / _dataSize) * (FontWidth * 9)) + 67;
					break;
			}
			start += (FontWidth * 4);
			return start;
		}

		private int GetTextX(int address)
		{
			return GetTextOffset() + ((address % 16) * FontWidth);
		}

		private void MemoryViewerBox_Paint(object sender, PaintEventArgs e)
		{
			var activeCheats = Global.CheatList.Where(x => x.Enabled);
			foreach (var cheat in activeCheats)
			{
				if (IsVisible(cheat.Address ?? 0))
				{
					if (_domain.ToString() == cheat.Domain.Name)
					{
						var rect = new Rectangle(GetAddressCoordinates(cheat.Address ?? 0), new Size(15 * _dataSize, fontHeight));
						e.Graphics.DrawRectangle(new Pen(Brushes.Black), rect);
						e.Graphics.FillRectangle(new SolidBrush(Global.Config.HexFreezeColor), rect);
					}
				}
			}

			if (_addressHighlighted >= 0 && IsVisible(_addressHighlighted))
			{
				var point = GetAddressCoordinates(_addressHighlighted);
				var textX = GetTextX(_addressHighlighted);
				var textpoint = new Point(textX, point.Y);

				var rect = new Rectangle(point, new Size(15 * _dataSize, fontHeight));
				e.Graphics.DrawRectangle(new Pen(Brushes.Black), rect);

				var textrect = new Rectangle(textpoint, new Size((8 * _dataSize), fontHeight));

				if (Global.CheatList.IsActive(_domain, _addressHighlighted))
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
			foreach (var address in _secondaryHighlightedAddresses)
			{
				var point = GetAddressCoordinates(address);
				var textX = GetTextX(address);
				var textpoint = new Point(textX, point.Y);

				var rect = new Rectangle(point, new Size(15 * _dataSize, fontHeight));
				e.Graphics.DrawRectangle(new Pen(Brushes.Black), rect);

				var textrect = new Rectangle(textpoint, new Size(8, fontHeight));

				if (Global.CheatList.IsActive(_domain, address))
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
				e.Graphics.DrawString(MakeNibbles(), new Font("Courier New", 8, FontStyle.Italic), Brushes.Black, new Point(158, 4));
			}
		}

		private bool HasNibbles()
		{
			return _nibbles.Any(x => x != 'G');
		}

		private string MakeNibbles()
		{
			var str = String.Empty;
			for (var x = 0; x < (_dataSize * 2); x++)
			{
				if (_nibbles[x] != 'G')
				{
					str += _nibbles[x];
				}
				else
				{
					break;
				}
			}
			return str;
		}

		private void AddressesLabel_MouseLeave(object sender, EventArgs e)
		{
			_addressOver = -1;
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
					newHighlighted = _addressHighlighted - 16;
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = newHighlighted + 1; i <= _addressHighlighted; i++)
						{
							AddToSecondaryHighlights(i);
						}
						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.Down:
					newHighlighted = _addressHighlighted + 16;
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = newHighlighted - 16; i < newHighlighted; i++)
						{
							AddToSecondaryHighlights(i);
						}
						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.Left:
					newHighlighted = _addressHighlighted - (1 * _dataSize);
					if (e.Modifiers == Keys.Shift)
					{
						AddToSecondaryHighlights(_addressHighlighted);
						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.Right:
					newHighlighted = _addressHighlighted + (1 * _dataSize);
					if (e.Modifiers == Keys.Shift)
					{
						AddToSecondaryHighlights(_addressHighlighted);
						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.PageUp:
					newHighlighted = _addressHighlighted - (_rowsVisible * 16);
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = newHighlighted + 1; i <= _addressHighlighted; i++)
						{
							AddToSecondaryHighlights(i);
						}
						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.PageDown:
					newHighlighted = _addressHighlighted + (_rowsVisible * 16);
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = _addressHighlighted + 1; i < newHighlighted; i++)
						{
							AddToSecondaryHighlights(i);
						}
						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.Tab:
					_secondaryHighlightedAddresses.Clear();
					if (e.Modifiers == Keys.Shift)
						GoToAddress(_addressHighlighted - 8);
					else
						GoToAddress(_addressHighlighted + 8);
					break;
				case Keys.Home:
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = 1; i <= _addressHighlighted; i++)
						{
							AddToSecondaryHighlights(i);
						}
						GoToAddress(0);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(0);
					}
					break;
				case Keys.End:
					newHighlighted = _domain.Size - (_dataSize);
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = _addressHighlighted; i < newHighlighted; i++)
						{
							AddToSecondaryHighlights(i);
						}
						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}
					break;
				case Keys.Add:
					IncrementHighlighted();
					UpdateValues();
					break;
				case Keys.Subtract:
					DecrementHighlighted();
					UpdateValues();
					break;
				case Keys.Space:
					ToggleFreeze();
					break;
				case Keys.Delete:
					if (e.Modifiers == Keys.Shift)
					{
						Global.CheatList.DisableAll();
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
					_secondaryHighlightedAddresses.Clear();
					ClearHighlighted();
					break;
			}
		}

		private void AddToSecondaryHighlights(int address)
		{
			if (address >= 0 && address < _domain.Size)
			{
				_secondaryHighlightedAddresses.Add(address);
			}
		}


		private static bool IsHexKeyCode(char key)
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
		private static char ForceCorrectKeyString(Keys keycode)
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

			switch (_dataSize)
			{
				default:
				case 1:
					if (_nibbles[0] == 'G')
					{
						_nibbles[0] = ForceCorrectKeyString(e.KeyCode);
					}
					else
					{
						var temp = _nibbles[0].ToString() + ForceCorrectKeyString(e.KeyCode);
						var x = byte.Parse(temp, NumberStyles.HexNumber);
						_domain.PokeByte(_addressHighlighted, x);
						ClearNibbles();
						SetHighlighted(_addressHighlighted + 1);
						UpdateValues();
					}
					break;
				case 2:
					if (_nibbles[0] == 'G')
					{
						_nibbles[0] = ForceCorrectKeyString(e.KeyCode);
					}
					else if (_nibbles[1] == 'G')
					{
						_nibbles[1] = ForceCorrectKeyString(e.KeyCode);
					}
					else if (_nibbles[2] == 'G')
					{
						_nibbles[2] = ForceCorrectKeyString(e.KeyCode);
					}
					else if (_nibbles[3] == 'G')
					{
						var temp = _nibbles[0].ToString() + _nibbles[1];
						var x1 = byte.Parse(temp, NumberStyles.HexNumber);

						var temp2 = _nibbles[2].ToString() + ((char)e.KeyCode);
						var x2 = byte.Parse(temp2, NumberStyles.HexNumber);

						PokeWord(_addressHighlighted, x1, x2);
						ClearNibbles();
						SetHighlighted(_addressHighlighted + 2);
						UpdateValues();
					}
					break;
				case 4:
					if (_nibbles[0] == 'G')
					{
						_nibbles[0] = ForceCorrectKeyString(e.KeyCode);
					}
					else if (_nibbles[1] == 'G')
					{
						_nibbles[1] = ForceCorrectKeyString(e.KeyCode);
					}
					else if (_nibbles[2] == 'G')
					{
						_nibbles[2] = ForceCorrectKeyString(e.KeyCode);
					}
					else if (_nibbles[3] == 'G')
					{
						_nibbles[3] = ForceCorrectKeyString(e.KeyCode);
					}
					else if (_nibbles[4] == 'G')
					{
						_nibbles[4] = ForceCorrectKeyString(e.KeyCode);
					}
					else if (_nibbles[5] == 'G')
					{
						_nibbles[5] = ForceCorrectKeyString(e.KeyCode);
					}
					else if (_nibbles[6] == 'G')
					{
						_nibbles[6] = ForceCorrectKeyString(e.KeyCode);
					}
					else if (_nibbles[7] == 'G')
					{
						var temp = _nibbles[0].ToString() + _nibbles[1];
						var x1 = byte.Parse(temp, NumberStyles.HexNumber);

						var temp2 = _nibbles[2].ToString() + _nibbles[3];
						var x2 = byte.Parse(temp2, NumberStyles.HexNumber);

						var temp3 = _nibbles[4].ToString() + _nibbles[5];
						var x3 = byte.Parse(temp3, NumberStyles.HexNumber);

						var temp4 = _nibbles[6].ToString() + ForceCorrectKeyString(e.KeyCode);
						var x4 = byte.Parse(temp4, NumberStyles.HexNumber);

						PokeWord(_addressHighlighted, x1, x2);
						PokeWord(_addressHighlighted + 2, x3, x4);
						ClearNibbles();
						SetHighlighted(_addressHighlighted + 4);
						UpdateValues();
					}
					break;
			}
			MemoryViewerBox.Refresh();
		}

		//TODO: obsolete me
		private void PokeWord(int address, byte _1, byte _2)
		{
			if (_bigEndian)
			{
				_domain.PokeByte(address, _2);
				_domain.PokeByte(address + 1, _1);
			}
			else
			{
				_domain.PokeByte(address, _1);
				_domain.PokeByte(address + 1, _2);
			}
		}

		private void unfreezeAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.CheatList.DisableAll();
		}

		private void unfreezeAllToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			Global.CheatList.DisableAll();
		}

		private void HexEditor_MouseWheel(object sender, MouseEventArgs e)
		{
			var delta = 0;
			if (e.Delta > 0)
			{
				delta = -1;
			}
			else if (e.Delta < 0)
			{
				delta = 1;
			}

			var newValue = vScrollBar1.Value + delta;
			if (newValue < vScrollBar1.Minimum) newValue = vScrollBar1.Minimum;
			if (newValue > vScrollBar1.Maximum - vScrollBar1.LargeChange + 1) newValue = vScrollBar1.Maximum - vScrollBar1.LargeChange + 1;
			if (newValue != vScrollBar1.Value)
			{
				vScrollBar1.Value = newValue;
				MemoryViewerBox.Refresh();
			}
		}

		private void IncrementAddress(int address)
		{
			if (Global.CheatList.IsActive(_domain, address))
			{
				//TODO: Increment should be intelligent since IsActive is.  If this address is part of a multi-byte cheat it should intelligently increment just that byte
				Global.CheatList.FirstOrDefault(x => x.Domain == _domain && x.Address == address).Increment();
			}
			else
			{
				switch (_dataSize)
				{
					default:
					case 1:
						_domain.PokeByte(
							address,
							(byte)(_domain.PeekByte(address) + 1)
						);
						break;
					case 2:
						_domain.PokeWord(
							address,
							(ushort)(_domain.PeekWord(address, _bigEndian) + 1),
							_bigEndian
						);
						break;
					case 4:
						_domain.PokeDWord(
							address,
							_domain.PeekDWord(address, _bigEndian) + 1,
							_bigEndian
						);
						break;
				}
			}
		}

		private void DecrementAddress(int address)
		{
			if (Global.CheatList.IsActive(_domain, address))
			{
				//TODO: Increment should be intelligent since IsActive is.  If this address is part of a multi-byte cheat it should intelligently increment just that byte
				Global.CheatList.FirstOrDefault(x => x.Domain == _domain && x.Address == address).Decrement();
			}
			else
			{
				switch (_dataSize)
				{
					default:
					case 1:
						_domain.PokeByte(
							address,
							(byte)(_domain.PeekByte(address) - 1)
						);
						break;
					case 2:
						_domain.PokeWord(
							address,
							(ushort)(_domain.PeekWord(address, _bigEndian) - 1),
							_bigEndian
						);
						break;
					case 4:
						_domain.PokeDWord(
							address,
							_domain.PeekDWord(address, _bigEndian) - 1,
							_bigEndian
						);
						break;
				}
			}
		}

		private void IncrementHighlighted()
		{
			if (HighlightedAddress.HasValue)
			{
				IncrementAddress(HighlightedAddress.Value);
			}

			_secondaryHighlightedAddresses.ForEach(IncrementAddress);
		}

		private void DecrementHighlighted()
		{
			if (HighlightedAddress.HasValue)
			{
				DecrementAddress(HighlightedAddress.Value);
			}
			_secondaryHighlightedAddresses.ForEach(DecrementAddress);
		}

		private void incrementToolStripMenuItem_Click(object sender, EventArgs e)
		{
			IncrementHighlighted();
		}

		private void decrementToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DecrementHighlighted();
		}

		private void ViewerContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			var iData = Clipboard.GetDataObject();

			CopyContextItem.Visible = 
				FreezeContextItem.Visible =
				AddToRamWatchContextItem.Visible =
				PokeContextItem.Visible = 
				IncrementContextItem.Visible =
				DecrementContextItem.Visible =
				ContextSeparator2.Visible =
				HighlightedAddress.HasValue || _secondaryHighlightedAddresses.Any();

			UnfreezeAllContextItem.Visible = Global.CheatList.ActiveCount > 0;
			PasteContextItem.Visible = (iData != null && iData.GetDataPresent(DataFormats.Text));

			ContextSeparator1.Visible =
				HighlightedAddress.HasValue ||
				_secondaryHighlightedAddresses.Any() ||
				(iData != null && iData.GetDataPresent(DataFormats.Text));

			if (HighlightedAddress.HasValue && IsFrozen(HighlightedAddress.Value))
			{
				FreezeContextItem.Text = "Un&freeze";
				FreezeContextItem.Image = Properties.Resources.Unfreeze;
			}
			else
			{
				FreezeContextItem.Text = "&Freeze";
				FreezeContextItem.Image = Properties.Resources.Freeze;
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
				return String.Format(_digitFormatString, MakeValue(address)).Trim();
			}
			else
			{
				return String.Empty;
			}
		}

		private void OpenFindBox()
		{

			_findStr = GetFindValues();
			if (!_hexFind.IsHandleCreated || _hexFind.IsDisposed)
			{
				_hexFind = new HexFind();
				_hexFind.SetLocation(PointToScreen(AddressesLabel.Location));
				_hexFind.SetInitialValue(_findStr);
				_hexFind.Show();
			}
			else
			{
				_hexFind.SetInitialValue(_findStr);
				_hexFind.Focus();
			}
		}

		private string GetFindValues()
		{
			if (HighlightedAddress.HasValue)
			{
				var values = ValueString(HighlightedAddress.Value);
				return _secondaryHighlightedAddresses.Aggregate(values, (current, x) => current + ValueString(x));
			}
			else
			{
				return "";
			}
		}

		public void FindNext(string value, Boolean wrap)
		{
			var found = -1;

			var search = value.Replace(" ", "").ToUpper();
			if (search.Length == 0)
				return;

			var numByte = search.Length / 2;

			int startByte;
			if (_addressHighlighted == -1)
			{
				startByte = 0;
			}
			else if (_addressHighlighted >= (_domain.Size - 1 - numByte))
			{
				startByte = 0;
			}
			else
			{
				startByte = _addressHighlighted + _dataSize;
			}

			for (var i = startByte; i < (_domain.Size - numByte); i++)
			{
				var ramblock = new StringBuilder();
				for (var j = 0; j < numByte; j++)
				{
					ramblock.Append(String.Format("{0:X2}", (int)_domain.PeekByte(i + j)));
				}
				var block = ramblock.ToString().ToUpper();
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
				_findStr = search;
				MemoryViewerBox.Focus();
			}
			else if (wrap == false)  // Search the opposite direction if not found
			{
				FindPrev(value, true);
			}
		}

		public void FindPrev(string value, Boolean wrap)
		{
			var found = -1;

			var search = value.Replace(" ", "").ToUpper();
			if (!String.IsNullOrEmpty(search))
			{
				return;
			}

			var numByte = search.Length / 2;

			int startByte;
			if (_addressHighlighted == -1)
			{
				startByte = _domain.Size - _dataSize;
			}
			else
			{
				startByte = _addressHighlighted - 1;
			}

			for (var i = startByte; i >= 0; i--)
			{
				var ramblock = new StringBuilder();
				for (var j = 0; j < numByte; j++)
				{
					ramblock.Append(String.Format("{0:X2}", (int)_domain.PeekByte(i + j)));
				}
				var block = ramblock.ToString().ToUpper();
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
				_findStr = search;
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
			_secondaryHighlightedAddresses.Clear();

			var addrLength = _dataSize * 2;
			if (value.Length <= addrLength)
			{
				return;
			}
			var numToHighlight = ((value.Length / addrLength)) - 1;

			for (var i = 0; i < numToHighlight; i++)
			{
				_secondaryHighlightedAddresses.Add(found + 1 + i);
			}

		}

		private void Copy()
		{
			var value = HighlightedAddress.HasValue ? ValueString(HighlightedAddress.Value) : String.Empty;
			value = _secondaryHighlightedAddresses.Aggregate(value, (current, x) => current + ValueString(x));
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
			var iData = Clipboard.GetDataObject();

			if (iData != null && iData.GetDataPresent(DataFormats.Text))
			{
				var clipboardRaw = (String)iData.GetData(DataFormats.Text);
				var hex = InputValidate.DoHexString(clipboardRaw);

				var numBytes = hex.Length / 2;
				for (var i = 0; i < numBytes; i++)
				{
					var value = int.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
					var address = _addressHighlighted + i;
					_domain.PokeByte(address, (byte)value);
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
		}

		private void setColorsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			new HexColorsForm().Show();
		}

		private void setColorsToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			GlobalWin.Sound.StopSound();
			new HexColorsForm().ShowDialog();
			GlobalWin.Sound.StartSound();
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
			FindNext(_findStr, false);
		}

		private void findPrevToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FindPrev(_findStr, false);
		}

		private void editToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			findNextToolStripMenuItem.Enabled = !String.IsNullOrWhiteSpace(_findStr);
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
				var pointed_address = GetPointedAddress(e.X, e.Y);
				if (pointed_address >= 0)
				{
					if ((ModifierKeys & Keys.Control) == Keys.Control)
					{
						if (pointed_address == _addressHighlighted)
						{
							ClearHighlighted();
						}
						else if (_secondaryHighlightedAddresses.Contains(pointed_address))
						{
							_secondaryHighlightedAddresses.Remove(pointed_address);
						}
						else
						{
							_secondaryHighlightedAddresses.Add(pointed_address);
						}
					}
					else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
					{
						DoShiftClick();
					}
					else
					{
						SetHighlighted(pointed_address);
						_secondaryHighlightedAddresses.Clear();
						_findStr = String.Empty;
					}

					MemoryViewerBox.Refresh();
				}

				_mouseIsDown = true;
			}
		}

		private void AddressesLabel_MouseUp(object sender, MouseEventArgs e)
		{
			_mouseIsDown = false;
		}

		private void alwaysOnTopToolStripMenuItem_Click(object sender, EventArgs e)
		{
			alwaysOnTopToolStripMenuItem.Checked = alwaysOnTopToolStripMenuItem.Checked == false;
			TopMost = alwaysOnTopToolStripMenuItem.Checked;
		}

		#region Events

		#region File Menu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if (_domain.Name == "ROM File")
			{
				SaveMenuItem.Visible = !CurrentRomIsArchive();
				SaveAsBinaryMenuItem.Text = "Save as ROM...";
			}
			else
			{
				SaveAsBinaryMenuItem.Text = "Save as binary...";
			}
		}
		
		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (!CurrentRomIsArchive())
			{
				SaveFileBinary(GlobalWin.MainForm.CurrentlyOpenRom);
			}
		}

		private void SaveAsBinaryMenuItem_Click(object sender, EventArgs e)
		{
			SaveAsBinary();
		}

		#endregion
		
		#endregion
	}
} 
