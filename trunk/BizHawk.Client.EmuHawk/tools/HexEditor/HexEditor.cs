using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Client.EmuHawk.ToolExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class HexEditor : Form, IToolForm
	{
		private bool fontSizeSet = false;
		private int fontWidth;
		private int fontHeight;

		private readonly List<ToolStripMenuItem> _domainMenuItems = new List<ToolStripMenuItem>();
		private readonly char[] _nibbles = { 'G', 'G', 'G', 'G', 'G', 'G', 'G', 'G' };    // G = off 0-9 & A-F are acceptable values
		private readonly List<int> _secondaryHighlightedAddresses = new List<int>();

		private readonly Dictionary<int, char> _textTable = new Dictionary<int, char>();

		private int _defaultWidth;
		private int _defaultHeight;
		private int _rowsVisible;
		private int _numDigits = 4;
		private string _numDigitsStr = "{0:X4}";
		private string _digitFormatString = "{0:X2}";
		private int _addressHighlighted = -1;
		private int _addressOver = -1;

		private int _maxRow;

		private long _domainSize;
		private MemoryDomain _domain = new MemoryDomain(
			"NULL", 1024, MemoryDomain.Endian.Little, addr => 0, delegate(int a, byte v) { v = 0; });

		private int _row;
		private int _addr;
		private string _findStr = string.Empty;
		private bool _mouseIsDown;
		private byte[] _rom;
		private MemoryDomain _romDomain;
		private HexFind _hexFind = new HexFind();

		// Configurations
		private bool _bigEndian;
		private int _dataSize;

		private readonly MemoryDomainList MemoryDomains;

		public HexEditor()
		{
			var font = new Font("Courier New", 8);
			fontWidth = (int)font.Size;
			fontHeight = font.Height + 1;

			MemoryDomains = ((IMemoryDomains)Global.Emulator).MemoryDomains; // The cast is intentional, we want a specific cast error, not an eventual null reference error
			InitializeComponent();
			AddressesLabel.BackColor = Color.Transparent;
			LoadConfigSettings();
			SetHeader();
			Closing += (o, e) => SaveConfigSettings();

			Header.Font = font;
			AddressesLabel.Font = font;
			AddressLabel.Font = font;

			TopMost = Global.Config.HexEditorSettings.TopMost;
		}

		private int? HighlightedAddress
		{
			get
			{
				if (_addressHighlighted >= 0)
				{
					return _addressHighlighted;
				}
				
				return null; // Negative = no address highlighted
			}
		}

		private Watch.WatchSize WatchSize
		{
			get
			{
				return (Watch.WatchSize)_dataSize;
			}
		}

		#region API

		public bool UpdateBefore
		{
			get { return false; }
		}

		public bool AskSaveChanges()
		{
			return true;
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			AddressesLabel.Text = GenerateMemoryViewString();
			AddressLabel.Text = GenerateAddressString();
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		public void Restart()
		{
			if (!Global.Emulator.HasMemoryDomains())
			{
				Close();
			}

			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			var theDomain = _domain.Name.ToLower() == "file on disk" ? 999 : GetDomainInt(_domain.Name);

			SetMemoryDomainMenu(); // Calls update routines, TODO: refactor, that is confusing!

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

		public void SetToAddresses(IEnumerable<int> addresses, MemoryDomain domain, Watch.WatchSize size)
		{
			_dataSize = (int)size;
			SetDataSize(_dataSize);
			var addrList = addresses.ToList();
			if (addrList.Any())
			{
				SetDomain(domain);
				SetHighlighted(addrList[0]);
				_secondaryHighlightedAddresses.Clear();
				_secondaryHighlightedAddresses.AddRange(addrList.Where(addr => addr != addrList[0]).ToList());
				ClearNibbles();
				UpdateValues();
				MemoryViewerBox.Refresh();
				AddressLabel.Text = GenerateAddressString();
			}
		}

		public byte[] ConvertTextToBytes(string str)
		{
			if (_textTable.Any())
			{
				var byteArr = new List<byte>();
				foreach (var chr in str)
				{
					byteArr.Add((byte)_textTable.FirstOrDefault(kvp => kvp.Value == chr).Key);
				}

				return byteArr.ToArray();
			}

			return str.Select(Convert.ToByte).ToArray();
		}

		public void FindNext(string value, bool wrap)
		{
			var found = -1;

			var search = value.Replace(" ", string.Empty).ToUpper();
			if (string.IsNullOrEmpty(search))
			{
				return;
			}

			var numByte = search.Length / 2;

			int startByte;
			if (_addressHighlighted == -1)
			{
				startByte = 0;
			}
			else if (_addressHighlighted >= (_domainSize - 1 - numByte))
			{
				startByte = 0;
			}
			else
			{
				startByte = _addressHighlighted + _dataSize;
			}

			for (var i = startByte; i < (_domainSize - numByte); i++)
			{
				var ramblock = new StringBuilder();
				for (var j = 0; j < numByte; j++)
				{
					ramblock.Append(string.Format("{0:X2}", (int)_domain.PeekByte(i + j)));
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
			}
			else if (wrap == false)  
			{
				FindPrev(value, true); // Search the opposite direction if not found
			}

			_hexFind.Close();
		}

		public void FindPrev(string value, bool wrap)
		{
			var found = -1;

			var search = value.Replace(" ", string.Empty).ToUpper();
			if (string.IsNullOrEmpty(search))
			{
				return;
			}

			var numByte = search.Length / 2;

			int startByte;
			if (_addressHighlighted == -1)
			{
				startByte = (int)(_domainSize - _dataSize);
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
					ramblock.Append(string.Format("{0:X2}", (int)_domain.PeekByte(i + j)));
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
			}
			else if (wrap == false) 
			{
				FindPrev(value, true); // Search the opposite direction if not found
			}

			_hexFind.Close();
		}

		#endregion

		private char Remap(byte val)
		{
			if (_textTable.Any())
			{
				if (_textTable.ContainsKey(val))
				{
					return _textTable[val];
				}

				return '?';
			}
			else
			{
				if (val < ' ')
				{
					return '.';
				}

				if (val >= 0x80)
				{
					return '.';
				}

				return (char)val;
			}
		}

		private int? GetDomainInt(string name)
		{
			for (var i = 0; i < MemoryDomains.Count; i++)
			{
				if (MemoryDomains[i].Name == name)
				{
					return i;
				}
			}

			return null;
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
				return new byte[] { 0xFF };
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
					return stream.ReadAllBytes();
				}
				
				return File.ReadAllBytes(path);
			}
		}

		private static int GetNumDigits(long i)
		{
			if (i <= 0x10000)
			{
				return 4;
			}

			return i <= 0x1000000 ? 6 : 8;
		}

		private static char ForceCorrectKeyString(char keycode)
		{
			return (char)keycode;
		}

		private void RefreshFloatingWindowControl()
		{
			Owner = Global.Config.RamSearchSettings.FloatingWindow ? null : GlobalWin.MainForm;
		}

		private static string GetSaveFileFromUser()
		{
			var sfd = new SaveFileDialog
			{
				Filter = "Text (*.txt)|*.txt|All Files|*.*",
				RestoreDirectory = true
			};

			if (Global.Emulator is NullEmulator)
			{
				sfd.FileName = "MemoryDump";
				sfd.InitialDirectory = PathManager.GetBasePathAbsolute();
			}
			else
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = Path.GetDirectoryName(PathManager.MakeAbsolutePath(Global.Config.RecentRoms.MostRecent, null));
			}

			var result = sfd.ShowHawkDialog();

			return result == DialogResult.OK ? sfd.FileName : string.Empty;
		}

		private static bool IsHexKeyCode(char key)
		{
			if (key >= '0' && key <= '9') // 0-9
			{
				return true;
			}
			
			if (key >= 'a' && key <= 'f') // A-F
			{
				return true;
			}
			
			if (key >= 'A' && key <= 'F') // A-F
			{
				return true;
			}
			
			return false;
		}

		private void HexEditor_Load(object sender, EventArgs e)
		{
			_defaultWidth = Size.Width;     // Save these first so that the user can restore to its original size
			_defaultHeight = Size.Height;

			if (Global.Config.HexEditorSettings.UseWindowPosition)
			{
				Location = Global.Config.HexEditorSettings.WindowPosition;
			}
			
			if (Global.Config.HexEditorSettings.UseWindowSize)
			{
				Size = Global.Config.HexEditorSettings.WindowSize;
			}

			SetMemoryDomainMenu();
			SetDataSize(_dataSize);

			if (Global.Config.RecentTables.AutoLoad)
			{
				LoadFileFromRecent(Global.Config.RecentTables[0]);
			}

			UpdateValues();
		}

		private void LoadConfigSettings()
		{
			_bigEndian = Global.Config.HexEditorBigEndian;
			_dataSize = Global.Config.HexEditorDataSize;

			HexMenuStrip.BackColor = Global.Config.HexMenubarColor;
			MemoryViewerBox.BackColor = Global.Config.HexBackgrndColor;
			MemoryViewerBox.ForeColor = Global.Config.HexForegrndColor;
			Header.BackColor = Global.Config.HexBackgrndColor;
			Header.ForeColor = Global.Config.HexForegrndColor;
		}

		private void SaveConfigSettings()
		{
			if (_hexFind.IsHandleCreated || !_hexFind.IsDisposed)
			{
				_hexFind.Close();
			}

			if (Global.Config.SaveWindowPosition)
			{
				Global.Config.HexEditorSettings.Wndx = Location.X;
				Global.Config.HexEditorSettings.Wndy = Location.Y;
				Global.Config.HexEditorSettings.Width = Right - Left;
				Global.Config.HexEditorSettings.Height = Bottom - Top;
			}

			Global.Config.HexEditorBigEndian = _bigEndian;
			Global.Config.HexEditorDataSize = _dataSize;
		}

		private string GenerateAddressString()
		{
			var addrStr = new StringBuilder();

			for (var i = 0; i < _rowsVisible; i++)
			{
				_row = i + HexScrollBar.Value;
				_addr = _row << 4;
				if (_addr >= _domainSize)
				{
					break;
				}

				if (_numDigits == 4)
				{
					addrStr.Append("    "); // Hack to line things up better between 4 and 6
				}
				else if (_numDigits == 6)
				{
					addrStr.Append("  ");
				}

				addrStr.AppendLine(_addr.ToHexString(_numDigits));
			}

			return addrStr.ToString();
		}

		private string GenerateMemoryViewString()
		{
			var rowStr = new StringBuilder();

			for (var i = 0; i < _rowsVisible; i++)
			{
				_row = i + HexScrollBar.Value;
				_addr = _row << 4;
				if (_addr >= _domainSize)
				{
					break;
				}

				for (var j = 0; j < 16; j += _dataSize)
				{
					if (_addr + j + _dataSize <= _domainSize)
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
					if (_addr + k < _domainSize)
					{
						rowStr.Append(Remap(MakeByte(_addr + k)));
					}
				}

				rowStr.AppendLine();
			}

			return rowStr.ToString();
		}

		private byte MakeByte(int address)
		{
			return Global.CheatList.IsActive(_domain, address)
				? Global.CheatList.GetByteValue(_domain, address).Value
				: _domain.PeekByte(address); 
		}

		private int MakeValue(int address)
		{
			if (Global.CheatList.IsActive(_domain, address))
			{
				return Global.CheatList.GetCheatValue(_domain, address, (Watch.WatchSize)_dataSize ).Value;
			}

			switch (_dataSize)
			{
				default:
				case 1:
					return _domain.PeekByte(address);
				case 2:
					return _domain.PeekWord(address, _bigEndian);
				case 4:
					return (int)_domain.PeekDWord(address, _bigEndian);
			}
		}

		private void SetMemoryDomain(MemoryDomain d)
		{
			_domain = d;

			//store domain size separately as a long, to apply 0-hack
			_domainSize = _domain.Size;
			if (_domainSize == 0)
				_domainSize = 0x100000000;

			_bigEndian = d.EndianType == MemoryDomain.Endian.Big;
			_maxRow = (int)(_domainSize / 2);
			SetUpScrollBar();
			if (0 >= HexScrollBar.Minimum && 0 <= HexScrollBar.Maximum)
			{
				HexScrollBar.Value = 0;
			}

			Refresh();
		}

		private void SetDomain(MemoryDomain domain)
		{
			SetMemoryDomain(GetDomainInt(domain.Name) ?? 0);
			SetHeader();
		}

		// TODO: this should be removable or at least refactorable
		private void SetMemoryDomain(int pos)
		{
			// <zeromus> THIS IS HORRIBLE.
			if (pos == 999) 
			{
				// <zeromus> THIS IS HORRIBLE.
				_rom = GetRomBytes();

				// <zeromus> THIS IS HORRIBLE.
				_romDomain = new MemoryDomain(
					"File on Disk", _rom.Length, MemoryDomain.Endian.Little, i => _rom[i], (i, value) => _rom[i] = value);

				// <zeromus> THIS IS HORRIBLE.
				SetMemoryDomain(_romDomain);
			}
			else if (pos < MemoryDomains.Count)
			{
				SetMemoryDomain(MemoryDomains[pos]);
			}

			SetHeader();
			UpdateGroupBoxTitle();
			ResetScrollBar();
			UpdateValues();
			MemoryViewerBox.Refresh();
		}

		private void UpdateGroupBoxTitle()
		{
			var addressesString = "0x" + string.Format("{0:X8}", _domainSize / _dataSize).TrimStart('0');
			MemoryViewerBox.Text = Global.Emulator.SystemId + " " + _domain + "  -  " + addressesString + " addresses";
		}

		private void SetMemoryDomainMenu()
		{
			MemoryDomainsMenuItem.DropDownItems.Clear();

			for (var i = 0; i < MemoryDomains.Count; i++)
			{
				//zero 11-sep-2014 - historically, memorydomains of size zero were ignored.
				//now, they'll be confused with a 32bit memorydomain. hope that's not a problem.
				var str = MemoryDomains[i].ToString();
				var item = new ToolStripMenuItem { Text = str };
				{
					var temp = i;
					item.Click += (o, ev) => SetMemoryDomain(temp);
				}

				if (i == 0)
				{
					SetMemoryDomain(i);
				}

				MemoryDomainsMenuItem.DropDownItems.Add(item);
				_domainMenuItems.Add(item);
			}

			// Add File on Disk memory domain
			// <zeromus> THIS IS HORRIBLE.
			var rom_item = new ToolStripMenuItem { Text = "File on Disk" };
			rom_item.Click += (o, ev) => SetMemoryDomain(999); // 999 will denote File on Disk
			MemoryDomainsMenuItem.DropDownItems.Add(rom_item);
			_domainMenuItems.Add(rom_item);
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

			if (address >= _domainSize)
			{
				address = (int)(_domainSize - 1);
			}

			SetHighlighted(address);
			ClearNibbles();
			UpdateValues();
			MemoryViewerBox.Refresh();
			AddressLabel.Text = GenerateAddressString();
		}

		private void SetHighlighted(int address)
		{
			if (address < 0)
			{
				address = 0;
			}

			if (address >= _domainSize)
			{
				address = (int)(_domainSize - 1);
			}

			if (!IsVisible(address))
			{
				var value = (address / 16) - _rowsVisible + 1;
				if (value < 0)
				{
					value = 0;
				}

				HexScrollBar.Value = value;
			}

			_addressHighlighted = address;
			_addressOver = address;
			ClearNibbles();
			UpdateFormText();
		}

		private void UpdateFormText()
		{
			if (_addressHighlighted >= 0)
			{
				Text = "Hex Editor - Editing Address 0x" + string.Format(_numDigitsStr, _addressHighlighted);
			}
			else
			{
				Text = "Hex Editor";
			}
		}

		private bool IsVisible(int address)
		{
			var i = address >> 4;
			return i >= HexScrollBar.Value && i < (_rowsVisible + HexScrollBar.Value);
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

			_numDigits = GetNumDigits(_domainSize);
			_numDigitsStr = "{0:X" + _numDigits + "}  ";
		}

		private void SetDataSize(int size)
		{
			if (size == 1 || size == 2 || size == 4)
			{
				_dataSize = size;
				_digitFormatString = "{0:X" + (_dataSize * 2) + "} ";
				SetHeader();
				UpdateGroupBoxTitle();
				UpdateValues();
			}
		}

		private Watch MakeWatch(int address)
		{
			switch (_dataSize)
			{
				default:
				case 1:
					return new ByteWatch(_domain, address, Watch.DisplayType.Hex, _bigEndian, string.Empty);
				case 2:
					return new WordWatch(_domain, address, Watch.DisplayType.Hex, _bigEndian, string.Empty);
				case 4:
					return new DWordWatch(_domain, address, Watch.DisplayType.Hex, _bigEndian, string.Empty);
			}
		}

		private bool IsFrozen(int address)
		{
			return Global.CheatList.IsActive(_domain, address);
		}

		private void UnFreezeAddress(int address)
		{
			if (address >= 0) 
			{
				// TODO: can't unfreeze address 0??
				Global.CheatList.RemoveRange(
					Global.CheatList.Where(x => x.Contains(address)).ToList());
			}

			MemoryViewerBox.Refresh();
		}

		// TODO refactor to int?
		private void FreezeAddress(int address)
		{
			if (address >= 0)
			{
				var watch = Watch.GenerateWatch(
					_domain,
					address,
					WatchSize,
					Watch.DisplayType.Hex,
					string.Empty,
					_bigEndian);

				Global.CheatList.Add(new Cheat(
					watch,
					watch.Value ?? 0));
			}
		}

		private void FreezeSecondaries()
		{
			var cheats = new List<Cheat>();
			foreach (var address in _secondaryHighlightedAddresses)
			{
				var watch = Watch.GenerateWatch(
					_domain,
					address,
					WatchSize,
					Watch.DisplayType.Hex,
					string.Empty,
					_bigEndian);

				cheats.Add(new Cheat(
					watch,
					watch.Value ?? 0));
			}

			Global.CheatList.AddRange(cheats);
		}

		private void UnfreezeSecondaries()
		{
			Global.CheatList.RemoveRange(
				Global.CheatList.Where(
					cheat => !cheat.IsSeparator && cheat.Domain == _domain && _secondaryHighlightedAddresses.Contains(cheat.Address.Value)));
		}

		private void SaveFileBinary(string path)
		{
			var file = new FileInfo(path);
			using (var binWriter = new BinaryWriter(File.Open(file.FullName, FileMode.Create)))
			{
				for (var i = 0; i < _domainSize; i++)
				{
					binWriter.Write(_domain.PeekByte(i));
				}
			}
		}

		private string GetSaveFileFilter()
		{
			if (_domain.Name == "File on Disk")
			{
				var extension = Path.GetExtension(GlobalWin.MainForm.CurrentlyOpenRom);

				return "Binary (*" + extension + ")|*" + extension + "|All Files|*.*";
			}
			
			return "Binary (*.bin)|*.bin|All Files|*.*";
		}

		private string GetBinarySaveFileFromUser()
		{
			var sfd = new SaveFileDialog
			{
				Filter = GetSaveFileFilter(),
				RestoreDirectory = true
			};

			if (Global.Emulator is NullEmulator)
			{
				sfd.FileName = "MemoryDump";
				sfd.InitialDirectory = PathManager.GetBasePathAbsolute();
			}
			else
			{
				if (_domain.Name == "File on Disk")
				{
					sfd.FileName = Path.GetFileName(Global.Config.RecentRoms.MostRecent);
				}
				else
				{
					sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				}

				sfd.InitialDirectory = Path.GetDirectoryName(PathManager.MakeAbsolutePath(Global.Config.RecentRoms.MostRecent, null));
			}

			var result = sfd.ShowHawkDialog();

			return result == DialogResult.OK ? sfd.FileName : string.Empty;
		}

		private void ResetScrollBar()
		{
			HexScrollBar.Value = 0;
			SetUpScrollBar();
			Refresh();
		}

		private void SetUpScrollBar()
		{
			_rowsVisible = (MemoryViewerBox.Height - (fontHeight * 2) - (fontHeight / 2)) / fontHeight;
			var totalRows = (int)((_domainSize + 15) / 16);

			if (totalRows < _rowsVisible)
			{
				_rowsVisible = totalRows;
			}

			HexScrollBar.Maximum = totalRows - 1;
			HexScrollBar.LargeChange = _rowsVisible;
			HexScrollBar.Visible = totalRows > _rowsVisible;

			AddressLabel.Text = GenerateAddressString();
		}

		private int GetPointedAddress(int x, int y)
		{
			int address;

			// Scroll value determines the first row
			var i = HexScrollBar.Value;
			var rowoffset = y / fontHeight;
			i += rowoffset;
			int colWidth = _dataSize * 2 + 1;

			var column = x / (fontWidth * colWidth);

			var start = GetTextOffset() - 50;
			if (x > start)
			{
				column = (x - start) / (fontWidth / _dataSize);
			}

			if (i >= 0 && i <= _maxRow && column >= 0 && column < (16 / _dataSize))
			{
				address = (i * 16) + (column * _dataSize);
			}
			else
			{
				address = -1;
			}

			return address;
		}

		private void DoShiftClick()
		{
			if (_addressOver >= 0 && _addressOver < _domainSize)
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
			var extra = (address % _dataSize) * fontWidth * 2;
			var xOffset = AddressesLabel.Location.X + fontWidth / 2;
			var yOffset = AddressesLabel.Location.Y;

			return new Point(
				(((address % 16) / _dataSize) * (fontWidth * (_dataSize * 2 + 1))) + xOffset + extra,
				(((address / 16) - HexScrollBar.Value) * fontHeight) + yOffset
				);
		}

		// TODO: rename this, but it is a hack work around for highlighting misaligned addresses that result from highlighting on in a smaller data size and switching size
		private bool NeedsExtra(int val)
		{
			return val % _dataSize > 0;
		}

		private int GetTextOffset()
		{
			int start = (16 / _dataSize) * (fontWidth * (_dataSize * 2 + 1));
			start += AddressesLabel.Location.X + fontWidth / 2;
			start += fontWidth * 4;
			return start;
		}

		private int GetTextX(int address)
		{
			return GetTextOffset() + ((address % 16) * fontWidth);
		}

		private bool HasNibbles()
		{
			return _nibbles.Any(x => x != 'G');
		}

		private string MakeNibbles()
		{
			var str = string.Empty;
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

		private void AddToSecondaryHighlights(int address)
		{
			if (address >= 0 && address < _domainSize)
			{
				_secondaryHighlightedAddresses.Add(address);
			}
		}

		// TODO: obsolete me
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

		private void IncrementAddress(int address)
		{
			if (Global.CheatList.IsActive(_domain, address))
			{
				// TODO: Increment should be intelligent since IsActive is.  If this address is part of a multi-byte cheat it should intelligently increment just that byte
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
							(byte)(_domain.PeekByte(address) + 1));
						break;
					case 2:
						_domain.PokeWord(
							address,
							(ushort)(_domain.PeekWord(address, _bigEndian) + 1),
							_bigEndian);
						break;
					case 4:
						_domain.PokeDWord(
							address,
							_domain.PeekDWord(address, _bigEndian) + 1,
							_bigEndian);
						break;
				}
			}
		}

		private void DecrementAddress(int address)
		{
			if (Global.CheatList.IsActive(_domain, address))
			{
				// TODO: Increment should be intelligent since IsActive is.  If this address is part of a multi-byte cheat it should intelligently increment just that byte
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
							(byte)(_domain.PeekByte(address) - 1));
						break;
					case 2:
						_domain.PokeWord(
							address,
							(ushort)(_domain.PeekWord(address, _bigEndian) - 1),
							_bigEndian);
						break;
					case 4:
						_domain.PokeDWord(
							address,
							_domain.PeekDWord(address, _bigEndian) - 1,
							_bigEndian);
						break;
				}
			}
		}

		private string ValueString(int address)
		{
			if (address != -1)
			{
				return string.Format(_digitFormatString, MakeValue(address)).Trim();
			}
			
			return string.Empty;
		}

		private string GetFindValues()
		{
			if (HighlightedAddress.HasValue)
			{
				var values = ValueString(HighlightedAddress.Value);
				return _secondaryHighlightedAddresses.Aggregate(values, (current, x) => current + ValueString(x));
			}
			
			return string.Empty;
		}

		private void HighlightSecondaries(string value, int found)
		{
			// This function assumes that the primary highlighted value has been set and sets the remaining characters in this string
			_secondaryHighlightedAddresses.Clear();

			var addrLength = _dataSize * 2;
			if (value.Length <= addrLength)
			{
				return;
			}

			var numToHighlight = (value.Length / addrLength) - 1;

			for (var i = 0; i < numToHighlight; i++)
			{
				_secondaryHighlightedAddresses.Add(found + 1 + i);
			}
		}

		private bool LoadTable(string path)
		{
			var file = new FileInfo(path);
			if (!file.Exists)
			{
				return false;
			}

			using (var sr = file.OpenText())
			{
				string line;

				while ((line = sr.ReadLine()) != null)
				{
					var parts = line.Split('=');
					_textTable.Add(
						int.Parse(parts[0],
						NumberStyles.HexNumber), parts[1].First());
				}
			}

			return true;
		}

		#region Events

		#region File Menu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if (_domain.Name == "File on Disk")
			{
				SaveMenuItem.Visible = !CurrentRomIsArchive();
				SaveAsBinaryMenuItem.Text = "Save as ROM...";
			}
			else
			{
				SaveAsBinaryMenuItem.Text = "Save as binary...";
			}

			CloseTableFileMenuItem.Enabled = _textTable.Any();
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
			var path = GetBinarySaveFileFromUser();
			if (!string.IsNullOrEmpty(path))
			{
				SaveFileBinary(path);
			}
		}

		private void SaveAsTextMenuItem_Click(object sender, EventArgs e)
		{
			var path = GetSaveFileFromUser();
			if (!string.IsNullOrWhiteSpace(path))
			{
				var file = new FileInfo(path);
				using (var sw = new StreamWriter(file.FullName))
				{
					var sb = new StringBuilder();

					for (var i = 0; i < _domainSize / 16; i++)
					{
						for (var j = 0; j < 16; j++)
						{
							sb.Append(string.Format("{0:X2} ", _domain.PeekByte((i * 16) + j)));
						}

						sb.AppendLine();
					}

					sw.WriteLine(sb);
				}
			}
		}

		private void LoadTableFileMenuItem_Click(object sender, EventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				FileName = Path.GetFileNameWithoutExtension(Global.Config.RecentRoms.MostRecent) + ".tbl",
				InitialDirectory = Path.GetDirectoryName(PathManager.MakeAbsolutePath(Global.Config.RecentRoms.MostRecent, null)),
				Filter = "Text Table files (*.tbl)|*.tbl|All Files|*.*",
				RestoreDirectory = false
			};

			GlobalWin.Sound.StopSound();
			var result = ofd.ShowDialog();
			GlobalWin.Sound.StartSound();

			if (result == DialogResult.OK)
			{
				LoadTable(ofd.FileName);
				Global.Config.RecentTables.Add(ofd.FileName);
				UpdateValues();
			}
		}

		private void CloseTableFileMenuItem_Click(object sender, EventArgs e)
		{
			_textTable.Clear();
		}

		public void LoadFileFromRecent(string path)
		{

			var result = LoadTable(path);
			if (!result)
			{
				Global.Config.RecentTables.HandleLoadError(path);
			}
			else
			{
				Global.Config.RecentTables.Add(path);
				UpdateValues();
			}
		}

		private void RecentTablesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentTablesSubMenu.DropDownItems.Clear();
			RecentTablesSubMenu.DropDownItems.AddRange(
				Global.Config.RecentTables.RecentMenu(LoadFileFromRecent, true));
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Edit

		private void EditMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			FindNextMenuItem.Enabled = !string.IsNullOrWhiteSpace(_findStr);
		}

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			var value = HighlightedAddress.HasValue ? ValueString(HighlightedAddress.Value) : string.Empty;
			value = _secondaryHighlightedAddresses.Aggregate(value, (current, x) => current + ValueString(x));
			if (!string.IsNullOrWhiteSpace(value))
			{
				Clipboard.SetDataObject(value);
			}
		}

		private void PasteMenuItem_Click(object sender, EventArgs e)
		{
			var data = Clipboard.GetDataObject();

			if (data != null && data.GetDataPresent(DataFormats.Text))
			{
				var clipboardRaw = (string)data.GetData(DataFormats.Text);
				var hex = clipboardRaw.OnlyHex();

				var numBytes = hex.Length / 2;
				for (var i = 0; i < numBytes; i++)
				{
					var value = int.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
					var address = _addressHighlighted + i;
					_domain.PokeByte(address, (byte)value);
				}

				UpdateValues();
			}
		}

		private void FindMenuItem_Click(object sender, EventArgs e)
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

		private void FindNextMenuItem_Click(object sender, EventArgs e)
		{
			FindNext(_findStr, false);
		}

		private void FindPrevMenuItem_Click(object sender, EventArgs e)
		{
			FindPrev(_findStr, false);
		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			BigEndianMenuItem.Checked = _bigEndian;
			DataSizeByteMenuItem.Checked = _dataSize == 1;
			DataSizeWordMenuItem.Checked = _dataSize == 2;
			DataSizeDWordMenuItem.Checked = _dataSize == 4;

			if (HighlightedAddress.HasValue && IsFrozen(HighlightedAddress.Value))
			{
				FreezeAddressMenuItem.Image = Properties.Resources.Unfreeze;
				FreezeAddressMenuItem.Text = "Un&freeze Address";
			}
			else
			{
				FreezeAddressMenuItem.Image = Properties.Resources.Freeze;
				FreezeAddressMenuItem.Text = "&Freeze Address";
			}

			AddToRamWatchMenuItem.Enabled =
				FreezeAddressMenuItem.Enabled =
				HighlightedAddress.HasValue;
		}

		private void MemoryDomainsMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			foreach (var menuItem in _domainMenuItems)
			{
				menuItem.Checked = _domain.Name == menuItem.Text;
			}
		}

		private void DataSizeByteMenuItem_Click(object sender, EventArgs e)
		{
			SetDataSize(1);
		}

		private void DataSizeWordMenuItem_Click(object sender, EventArgs e)
		{
			SetDataSize(2);
		}

		private void DataSizeDWordMenuItem_Click(object sender, EventArgs e)
		{
			SetDataSize(4);
		}

		private void BigEndianMenuItem_Click(object sender, EventArgs e)
		{
			_bigEndian ^= true;
			UpdateValues();
		}

		private void GoToAddressMenuItem_Click(object sender, EventArgs e)
		{
			var inputPrompt = new InputPrompt
			{
				Text = "Go to Address",
				StartLocation = this.ChildPointToScreen(MemoryViewerBox),
				Message = "Enter a hexadecimal value"
			};

			var result = inputPrompt.ShowHawkDialog();

			if (result == DialogResult.OK && inputPrompt.PromptText.IsHex())
			{
				GoToAddress(int.Parse(inputPrompt.PromptText, NumberStyles.HexNumber));
			}

			AddressLabel.Text = GenerateAddressString();
		}

		private void AddToRamWatchMenuItem_Click(object sender, EventArgs e)
		{
			if (HighlightedAddress.HasValue || _secondaryHighlightedAddresses.Any())
			{
				GlobalWin.Tools.LoadRamWatch(true);
			}

			if (HighlightedAddress.HasValue)
			{
				GlobalWin.Tools.RamWatch.AddWatch(MakeWatch(HighlightedAddress.Value));
			}

			_secondaryHighlightedAddresses.ForEach(addr =>
				GlobalWin.Tools.RamWatch.AddWatch(MakeWatch(addr)));
		}

		private void FreezeAddressMenuItem_Click(object sender, EventArgs e)
		{
			if (HighlightedAddress.HasValue)
			{
				if (IsFrozen(HighlightedAddress.Value))
				{
					UnFreezeAddress(HighlightedAddress.Value);
					UnfreezeSecondaries();
				}
				else
				{
					FreezeAddress(HighlightedAddress.Value);
					FreezeSecondaries();
				}
			}

			ToolHelpers.UpdateCheatRelatedTools(null, null);
			MemoryViewerBox.Refresh();
		}

		private void UnfreezeAllMenuItem_Click(object sender, EventArgs e)
		{
			Global.CheatList.RemoveAll();
		}

		private void PokeAddressMenuItem_Click(object sender, EventArgs e)
		{
			var addresses = new List<int>();
			if (HighlightedAddress.HasValue)
			{
				addresses.Add(HighlightedAddress.Value);
			}

			if (_secondaryHighlightedAddresses.Any())
			{
				addresses.AddRange(_secondaryHighlightedAddresses);
			}

			if (addresses.Any())
			{
				var poke = new RamPoke
				{
					InitialLocation = GetAddressCoordinates(addresses[0])
				};

				var watches = addresses.Select(
					address => Watch.GenerateWatch(
						_domain,
						address,
						(Watch.WatchSize)_dataSize,
						Watch.DisplayType.Hex,
						string.Empty,
						_bigEndian));

				poke.SetWatch(watches);
				poke.ShowHawkDialog();
				UpdateValues();
			}
		}

		#endregion

		#region Settings Menu

		private void SettingsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadMenuItem.Checked = Global.Config.AutoLoadHexEditor;
			SaveWindowsPositionMenuItem.Checked = Global.Config.SaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Global.Config.HexEditorSettings.TopMost;
			FloatingWindowMenuItem.Checked = Global.Config.HexEditorSettings.FloatingWindow;
		}

		private void SetColorsMenuItem_Click(object sender, EventArgs e)
		{
			new HexColorsForm().ShowHawkDialog();
		}

		private void ResetColorsToDefaultMenuItem_Click(object sender, EventArgs e)
		{
			MemoryViewerBox.BackColor = Color.FromName("Control");
			MemoryViewerBox.ForeColor = Color.FromName("ControlText");
			this.HexMenuStrip.BackColor = Color.FromName("Control");
			Header.BackColor = Color.FromName("Control");
			Header.ForeColor = Color.FromName("ControlText");
			Global.Config.HexMenubarColor = Color.FromName("Control");
			Global.Config.HexForegrndColor = Color.FromName("ControlText");
			Global.Config.HexBackgrndColor = Color.FromName("Control");
			Global.Config.HexFreezeColor = Color.LightBlue;
			Global.Config.HexHighlightColor = Color.Pink;
			Global.Config.HexHighlightFreezeColor = Color.Violet;
		}

		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadHexEditor ^= true;
		}

		private void SaveWindowsPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.HexEditorSettings.TopMost ^= true;
			TopMost = Global.Config.HexEditorSettings.TopMost;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.HexEditorSettings.FloatingWindow ^= true;
			RefreshFloatingWindowControl();
		}

		private void RestoreDefaultSettingsMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);
			SetUpScrollBar();

			Global.Config.HexEditorSettings.TopMost = false;
			Global.Config.HexEditorSettings.SaveWindowPosition = true;
			Global.Config.HexEditorSettings.FloatingWindow = false;
			Global.Config.AutoLoadHexEditor = false;

			RefreshFloatingWindowControl();
		}

		#endregion

		#region Context Menu and Dialog Events

		private void HexEditor_Resize(object sender, EventArgs e)
		{
			SetUpScrollBar();
			UpdateValues();
		}

		private void HexEditor_ResizeEnd(object sender, EventArgs e)
		{
			SetUpScrollBar();
		}

		private void HexEditor_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.G)
			{
				GoToAddressMenuItem_Click(sender, e);
				return;
			}

			if (e.Control && e.KeyCode == Keys.P)
			{
				PokeAddressMenuItem_Click(sender, e);
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
					{
						GoToAddress(_addressHighlighted - 8);
					}
					else
					{
						GoToAddress(_addressHighlighted + 8);
					}

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
					newHighlighted = (int)(_domainSize - _dataSize);
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
					IncrementContextItem_Click(sender, e);
					break;
				case Keys.Subtract:
					DecrementContextItem_Click(sender, e);
					break;
				case Keys.Space:
					FreezeAddressMenuItem_Click(sender, e);
					break;
				case Keys.Delete:
					if (e.Modifiers == Keys.Shift)
					{
						Global.CheatList.RemoveAll();
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
						AddToRamWatchMenuItem_Click(sender, e);
					}

					break;
				case Keys.Escape:
					_secondaryHighlightedAddresses.Clear();
					ClearHighlighted();
					break;
			}
		}

		private void HexEditor_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!IsHexKeyCode(e.KeyChar))
			{
				e.Handled = true;
				return;
			}

			if ((ModifierKeys & (Keys.Control | Keys.Shift | Keys.Alt)) != 0)
			{
				return;
			}

			switch (_dataSize)
			{
				default:
				case 1:
					if (_nibbles[0] == 'G')
					{
						_nibbles[0] = ForceCorrectKeyString(e.KeyChar);
					}
					else
					{
						var temp = _nibbles[0].ToString() + ForceCorrectKeyString(e.KeyChar);
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
						_nibbles[0] = ForceCorrectKeyString(e.KeyChar);
					}
					else if (_nibbles[1] == 'G')
					{
						_nibbles[1] = ForceCorrectKeyString(e.KeyChar);
					}
					else if (_nibbles[2] == 'G')
					{
						_nibbles[2] = ForceCorrectKeyString(e.KeyChar);
					}
					else if (_nibbles[3] == 'G')
					{
						var temp = _nibbles[0].ToString() + _nibbles[1];
						var x1 = byte.Parse(temp, NumberStyles.HexNumber);

						var temp2 = _nibbles[2].ToString() + ((char)e.KeyChar);
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
						_nibbles[0] = ForceCorrectKeyString(e.KeyChar);
					}
					else if (_nibbles[1] == 'G')
					{
						_nibbles[1] = ForceCorrectKeyString(e.KeyChar);
					}
					else if (_nibbles[2] == 'G')
					{
						_nibbles[2] = ForceCorrectKeyString(e.KeyChar);
					}
					else if (_nibbles[3] == 'G')
					{
						_nibbles[3] = ForceCorrectKeyString(e.KeyChar);
					}
					else if (_nibbles[4] == 'G')
					{
						_nibbles[4] = ForceCorrectKeyString(e.KeyChar);
					}
					else if (_nibbles[5] == 'G')
					{
						_nibbles[5] = ForceCorrectKeyString(e.KeyChar);
					}
					else if (_nibbles[6] == 'G')
					{
						_nibbles[6] = ForceCorrectKeyString(e.KeyChar);
					}
					else if (_nibbles[7] == 'G')
					{
						var temp = _nibbles[0].ToString() + _nibbles[1];
						var x1 = byte.Parse(temp, NumberStyles.HexNumber);

						var temp2 = _nibbles[2].ToString() + _nibbles[3];
						var x2 = byte.Parse(temp2, NumberStyles.HexNumber);

						var temp3 = _nibbles[4].ToString() + _nibbles[5];
						var x3 = byte.Parse(temp3, NumberStyles.HexNumber);

						var temp4 = _nibbles[6].ToString() + ForceCorrectKeyString(e.KeyChar);
						var x4 = byte.Parse(temp4, NumberStyles.HexNumber);

						PokeWord(_addressHighlighted, x1, x2);
						PokeWord(_addressHighlighted + 2, x3, x4);
						ClearNibbles();
						SetHighlighted(_addressHighlighted + 4);
						UpdateValues();
					}

					break;
			}

			UpdateValues();
		}

		private void ViewerContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			var data = Clipboard.GetDataObject();

			CopyContextItem.Visible =
				FreezeContextItem.Visible =
				AddToRamWatchContextItem.Visible =
				PokeContextItem.Visible =
				IncrementContextItem.Visible =
				DecrementContextItem.Visible =
				ContextSeparator2.Visible =
				HighlightedAddress.HasValue || _secondaryHighlightedAddresses.Any();

			UnfreezeAllContextItem.Visible = Global.CheatList.ActiveCount > 0;
			PasteContextItem.Visible = data != null && data.GetDataPresent(DataFormats.Text);

			ContextSeparator1.Visible =
				HighlightedAddress.HasValue ||
				_secondaryHighlightedAddresses.Any() ||
				(data != null && data.GetDataPresent(DataFormats.Text));

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

		private void IncrementContextItem_Click(object sender, EventArgs e)
		{
			if (HighlightedAddress.HasValue)
			{
				IncrementAddress(HighlightedAddress.Value);
			}

			_secondaryHighlightedAddresses.ForEach(IncrementAddress);

			UpdateValues();
		}

		private void DecrementContextItem_Click(object sender, EventArgs e)
		{
			if (HighlightedAddress.HasValue)
			{
				DecrementAddress(HighlightedAddress.Value);
			}

			_secondaryHighlightedAddresses.ForEach(DecrementAddress);

			UpdateValues();
		}

		#endregion

		#region MemoryViewer Events

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

			var newValue = HexScrollBar.Value + delta;
			if (newValue < HexScrollBar.Minimum)
			{
				newValue = HexScrollBar.Minimum;
			}

			if (newValue > HexScrollBar.Maximum - HexScrollBar.LargeChange + 1)
			{
				newValue = HexScrollBar.Maximum - HexScrollBar.LargeChange + 1;
			}
			
			if (newValue != HexScrollBar.Value)
			{
				HexScrollBar.Value = newValue;
				MemoryViewerBox.Refresh();
			}
		}

		private void MemoryViewerBox_Paint(object sender, PaintEventArgs e)
		{
			// Update font size
			if (!fontSizeSet)
			{
				fontSizeSet = true;
				var fontSize = e.Graphics.MeasureString("x", AddressesLabel.Font);
				fontWidth = (int)Math.Round(fontSize.Width / 1.5);
				fontHeight = (int)Math.Round(fontSize.Height);
			}

			var activeCheats = Global.CheatList.Where(x => x.Enabled);
			foreach (var cheat in activeCheats)
			{
				if (IsVisible(cheat.Address ?? 0))
				{
					if (_domain.ToString() == cheat.Domain.Name)
					{
						var gaps = (int)cheat.Size - (int)_dataSize;

						if (cheat.Size == Watch.WatchSize.DWord && _dataSize == 2)
						{
							gaps -= 1;
						}

						if (gaps < 0) { gaps = 0; }
						
						var width = (fontWidth * 2 * (int)cheat.Size) + (gaps * fontWidth);

						var rect = new Rectangle(GetAddressCoordinates(cheat.Address ?? 0), new Size(width, fontHeight));
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

				var rect = new Rectangle(point, new Size(fontWidth * 2 * _dataSize + (NeedsExtra(_addressHighlighted) ? fontWidth : 0), fontHeight));
				e.Graphics.DrawRectangle(new Pen(Brushes.Black), rect);

				var textrect = new Rectangle(textpoint, new Size(fontWidth * _dataSize, fontHeight));

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

				var rect = new Rectangle(point, new Size(fontWidth * 2 * _dataSize, fontHeight));
				e.Graphics.DrawRectangle(new Pen(Brushes.Black), rect);

				var textrect = new Rectangle(textpoint, new Size(fontWidth, fontHeight));

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

		private void AddressesLabel_MouseUp(object sender, MouseEventArgs e)
		{
			_mouseIsDown = false;
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

		private void AddressesLabel_MouseLeave(object sender, EventArgs e)
		{
			_addressOver = -1;
			MemoryViewerBox.Refresh();
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
						_findStr = string.Empty;
					}

					MemoryViewerBox.Refresh();
				}

				_mouseIsDown = true;
			}
		}

		bool _programmaticallyChangingValue = false;
		private void HexScrollBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValue)
			{
				if (HexScrollBar.Value < 0)
				{
					_programmaticallyChangingValue = true;
					HexScrollBar.Value = 0;
					_programmaticallyChangingValue = false;
				}

				UpdateValues();
			}
		}

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl();
			base.OnShown(e);
		}

		#endregion

		#endregion
	}
} 
