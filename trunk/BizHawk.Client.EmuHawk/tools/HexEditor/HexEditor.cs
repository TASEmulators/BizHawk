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
	// int to long TODO: 32 bit domains have more digits than the hex editor can account for and the address covers up the 0 column
	public partial class HexEditor : Form, IToolFormAutoConfig
	{
		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		[RequiredService]
		private IEmulator Emulator { get; set; }

		private bool fontSizeSet = false;
		private int fontWidth;
		private int fontHeight;

		private readonly List<ToolStripMenuItem> _domainMenuItems = new List<ToolStripMenuItem>();
		private readonly char[] _nibbles = { 'G', 'G', 'G', 'G', 'G', 'G', 'G', 'G' };    // G = off 0-9 & A-F are acceptable values
		private readonly List<long> _secondaryHighlightedAddresses = new List<long>();

		private readonly Dictionary<int, char> _textTable = new Dictionary<int, char>();

		private int _rowsVisible;
		private int _numDigits = 4;
		private string _numDigitsStr = "{0:X4}";
		private string _digitFormatString = "{0:X2}";
		private long _addressHighlighted = -1;
		private long _addressOver = -1;

		private long _maxRow;

		private MemoryDomain _domain = new MemoryDomain(
			"NULL", 1024, MemoryDomain.Endian.Little, addr => 0, delegate(long a, byte v) { v = 0; });

		private long _row;
		private long _addr;
		private string _findStr = string.Empty;
		private bool _mouseIsDown;
		private byte[] _rom;
		private MemoryDomain _romDomain;
		private HexFind _hexFind = new HexFind();

		[ConfigPersist]
		private bool BigEndian { get; set; }

		[ConfigPersist]
		private int DataSize { get; set; }

		[ConfigPersist]
		private RecentFiles RecentTables { get; set; }

		public HexEditor()
		{
			RecentTables = new RecentFiles(8);
			DataSize = 1;

			var font = new Font("Courier New", 8);
			fontWidth = (int)font.Size;
			fontHeight = font.Height + 1;

			InitializeComponent();
			AddressesLabel.BackColor = Color.Transparent;
			LoadConfigSettings();
			SetHeader();
			Closing += (o, e) => SaveConfigSettings();

			Header.Font = font;
			AddressesLabel.Font = font;
			AddressLabel.Font = font;
		}

		private long? HighlightedAddress
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
				return (Watch.WatchSize)DataSize;
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
			AddressesLabel.Text = GenerateMemoryViewString();
			AddressLabel.Text = GenerateAddressString();
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		public void Restart()
		{
			_rom = GetRomBytes();
			_romDomain = MemoryDomain.FromByteArray("File on Disk", MemoryDomain.Endian.Little, _rom);

			if (_domain.Name == _romDomain.Name)
			{
				_domain = _romDomain;
			}
			else if (MemoryDomains.Any(x => x.Name == _domain.Name))
			{
				_domain = MemoryDomains[_domain.Name];
			}
			else
			{
				_domain = MemoryDomains.MainMemory;
			}

			BigEndian = _domain.EndianType == MemoryDomain.Endian.Big;
			_maxRow = _domain.Size / 2;

			ResetScrollBar();
			SetDataSize(DataSize);
			UpdateValues();
			AddressLabel.Text = GenerateAddressString();
		}

		public void SetToAddresses(IEnumerable<long> addresses, MemoryDomain domain, Watch.WatchSize size)
		{
			DataSize = (int)size;
			SetDataSize(DataSize);
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
			long found = -1;

			var search = value.Replace(" ", string.Empty).ToUpper();
			if (string.IsNullOrEmpty(search))
			{
				return;
			}

			var numByte = search.Length / 2;

			long startByte;
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
				startByte = _addressHighlighted + DataSize;
			}

			for (var i = startByte; i < (_domain.Size - numByte); i++)
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
			long found = -1;

			var search = value.Replace(" ", string.Empty).ToUpper();
			if (string.IsNullOrEmpty(search))
			{
				return;
			}

			var numByte = search.Length / 2;

			long startByte;
			if (_addressHighlighted == -1)
			{
				startByte = _domain.Size - DataSize;
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

		//private int? GetDomainInt(string name)
		//{
		//	for (var i = 0; i < MemoryDomains.Count; i++)
		//	{
		//		if (MemoryDomains[i].Name == name)
		//		{
		//			return i;
		//		}
		//	}

		//	return null;
		//}

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

		private static string GetSaveFileFromUser()
		{
			var sfd = new SaveFileDialog
			{
				Filter = "Text (*.txt)|*.txt|All Files|*.*",
				RestoreDirectory = true
			};

			sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
			sfd.InitialDirectory = Path.GetDirectoryName(PathManager.MakeAbsolutePath(Global.Config.RecentRoms.MostRecent, null));

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
			SetDataSize(DataSize);

			if (RecentTables.AutoLoad)
			{
				LoadFileFromRecent(RecentTables[0]);
			}

			UpdateValues();
		}

		private void LoadConfigSettings()
		{
			HexMenuStrip.BackColor = Global.Config.HexMenubarColor;
			MemoryViewerBox.BackColor = Global.Config.HexBackgrndColor;
			MemoryViewerBox.ForeColor = Global.Config.HexForegrndColor;
			Header.BackColor = Global.Config.HexBackgrndColor;
			Header.ForeColor = Global.Config.HexForegrndColor;
		}

		// TODO: rename me
		private void SaveConfigSettings()
		{
			if (_hexFind.IsHandleCreated || !_hexFind.IsDisposed)
			{
				_hexFind.Close();
			}
		}

		private string GenerateAddressString()
		{
			var addrStr = new StringBuilder();

			for (var i = 0; i < _rowsVisible; i++)
			{
				_row = i + HexScrollBar.Value;
				_addr = _row << 4;
				if (_addr >= _domain.Size)
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
				if (_addr >= _domain.Size)
				{
					break;
				}

				for (var j = 0; j < 16; j += DataSize)
				{
					if (_addr + j + DataSize <= _domain.Size)
					{
						rowStr.AppendFormat(_digitFormatString, MakeValue(_addr + j));
					}
					else
					{
						for (var t = 0; t < DataSize; t++)
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
						rowStr.Append(Remap(MakeByte(_addr + k)));
					}
				}

				rowStr.AppendLine();
			}

			return rowStr.ToString();
		}

		private byte MakeByte(long address)
		{
			return Global.CheatList.IsActive(_domain, address)
				? Global.CheatList.GetByteValue(_domain, address).Value
				: _domain.PeekByte(address); 
		}

		private int MakeValue(long address)
		{
			if (Global.CheatList.IsActive(_domain, address))
			{
				return Global.CheatList.GetCheatValue(_domain, address, (Watch.WatchSize)DataSize ).Value;
			}

			switch (DataSize)
			{
				default:
				case 1:
					return _domain.PeekByte(address);
				case 2:
					return _domain.PeekWord(address, BigEndian);
				case 4:
					return (int)_domain.PeekDWord(address, BigEndian);
			}
		}

		private void SetMemoryDomain(string name)
		{
			if (name == _romDomain.Name)
			{
				_domain = _romDomain;
			}
			else
			{
				_domain = MemoryDomains[name];
			}

			BigEndian = _domain.EndianType == MemoryDomain.Endian.Big;
			_maxRow = _domain.Size / 2;
			SetUpScrollBar();
			if (0 >= HexScrollBar.Minimum && 0 <= HexScrollBar.Maximum)
			{
				HexScrollBar.Value = 0;
			}

			UpdateGroupBoxTitle();
			UpdateValues();
		}

		private void SetDomain(MemoryDomain domain)
		{
			SetHeader();
			UpdateGroupBoxTitle();
			ResetScrollBar();
			UpdateValues();
			MemoryViewerBox.Refresh();
		}

		private void UpdateGroupBoxTitle()
		{
			var addressesString = "0x" + string.Format("{0:X8}", _domain.Size / DataSize).TrimStart('0');
			MemoryViewerBox.Text = Emulator.SystemId + " " + _domain + "  -  " + addressesString + " addresses";
		}

		private void ClearNibbles()
		{
			for (var i = 0; i < 8; i++)
			{
				_nibbles[i] = 'G';
			}
		}

		private void GoToAddress(long address)
		{
			if (address < 0)
			{
				address = 0;
			}

			if (address >= _domain.Size)
			{
				address = _domain.Size - DataSize;
			}

			SetHighlighted(address);
			ClearNibbles();
			UpdateValues();
			MemoryViewerBox.Refresh();
			AddressLabel.Text = GenerateAddressString();
		}

		private void SetHighlighted(long address)
		{
			if (address < 0)
			{
				address = 0;
			}

			if (address >= _domain.Size)
			{
				address = _domain.Size - DataSize;
			}

			if (!IsVisible(address))
			{
				var value = (address / 16) - _rowsVisible + 1;
				if (value < 0)
				{
					value = 0;
				}

				HexScrollBar.Value = (int)value; // This will fail on a sufficiently large domain
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

		private bool IsVisible(long address)
		{
			var i = address >> 4;
			return i >= HexScrollBar.Value && i < (_rowsVisible + HexScrollBar.Value);
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

			_numDigits = GetNumDigits(_domain.Size);
			_numDigitsStr = "{0:X" + _numDigits + "}  ";
		}

		private void SetDataSize(int size)
		{
			if (size == 1 || size == 2 || size == 4)
			{
				DataSize = size;
				_digitFormatString = "{0:X" + (DataSize * 2) + "} ";
				SetHeader();
				UpdateGroupBoxTitle();
				UpdateValues();
			}
		}

		private Watch MakeWatch(long address)
		{
			switch (DataSize)
			{
				default:
				case 1:
					return new ByteWatch(_domain, address, Watch.DisplayType.Hex, BigEndian, string.Empty);
				case 2:
					return new WordWatch(_domain, address, Watch.DisplayType.Hex, BigEndian, string.Empty);
				case 4:
					return new DWordWatch(_domain, address, Watch.DisplayType.Hex, BigEndian, string.Empty);
			}
		}

		private bool IsFrozen(long address)
		{
			return Global.CheatList.IsActive(_domain, address);
		}

		private void UnFreezeAddress(long address)
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
		private void FreezeAddress(long address)
		{
			if (address >= 0)
			{
				var watch = Watch.GenerateWatch(
					_domain,
					address,
					WatchSize,
					Watch.DisplayType.Hex,
					string.Empty,
					BigEndian);

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
					BigEndian);

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
				for (var i = 0; i < _domain.Size; i++)
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
				RestoreDirectory = true,
				InitialDirectory = Path.GetDirectoryName(PathManager.MakeAbsolutePath(Global.Config.RecentRoms.MostRecent, null))
			};

			if (_domain.Name == "File on Disk")
			{
				sfd.FileName = Path.GetFileName(Global.Config.RecentRoms.MostRecent);
			}
			else
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
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
			var totalRows = (int)((_domain.Size + 15) / 16);

			if (totalRows < _rowsVisible)
			{
				_rowsVisible = totalRows;
			}

			HexScrollBar.Maximum = totalRows - 1;
			HexScrollBar.LargeChange = _rowsVisible;
			HexScrollBar.Visible = totalRows > _rowsVisible;

			AddressLabel.Text = GenerateAddressString();
		}

		private long GetPointedAddress(int x, int y)
		{
			long address;

			// Scroll value determines the first row
			long i = HexScrollBar.Value;
			var rowoffset = y / fontHeight;
			i += rowoffset;
			int colWidth = DataSize * 2 + 1;

			var column = x / (fontWidth * colWidth);

			var start = GetTextOffset() - 50;
			if (x > start)
			{
				column = (x - start) / (fontWidth / DataSize);
			}

			if (i >= 0 && i <= _maxRow && column >= 0 && column < (16 / DataSize))
			{
				address = (i * 16) + (column * DataSize);
			}
			else
			{
				address = -1;
			}

			return address;
		}

		private void DoShiftClick()
		{
			if (_addressOver >= 0 && _addressOver < _domain.Size)
			{
				_secondaryHighlightedAddresses.Clear();
				if (_addressOver < _addressHighlighted)
				{
					for (var x = _addressOver; x < _addressHighlighted; x += DataSize)
					{
						_secondaryHighlightedAddresses.Add(x);
					}
				}
				else if (_addressOver > _addressHighlighted)
				{
					for (var x = _addressHighlighted + DataSize; x <= _addressOver; x += DataSize)
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

		private Point GetAddressCoordinates(long address)
		{
			var extra = (address % DataSize) * fontWidth * 2;
			var xOffset = AddressesLabel.Location.X + fontWidth / 2 - 2;
			var yOffset = AddressesLabel.Location.Y;

			return new Point(
				(int)((((address % 16) / DataSize) * (fontWidth * (DataSize * 2 + 1))) + xOffset + extra),
				(int)((((address / 16) - HexScrollBar.Value) * fontHeight) + yOffset)
				);
		}

		// TODO: rename this, but it is a hack work around for highlighting misaligned addresses that result from highlighting on in a smaller data size and switching size
		private bool NeedsExtra(long val)
		{
			return val % DataSize > 0;
		}

		private int GetTextOffset()
		{
			int start = (16 / DataSize) * fontWidth * (DataSize * 2 + 1);
			start += AddressesLabel.Location.X + fontWidth / 2;
			start += fontWidth * 4;
			return start;
		}

		private long GetTextX(long address)
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
			for (var x = 0; x < (DataSize * 2); x++)
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

		private void AddToSecondaryHighlights(long address)
		{
			if (address >= 0 && address < _domain.Size)
			{
				_secondaryHighlightedAddresses.Add(address);
			}
		}

		// TODO: obsolete me
		private void PokeWord(long address, byte _1, byte _2)
		{
			if (BigEndian)
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

		private void IncrementAddress(long address)
		{
			if (Global.CheatList.IsActive(_domain, address))
			{
				// TODO: Increment should be intelligent since IsActive is.  If this address is part of a multi-byte cheat it should intelligently increment just that byte
				Global.CheatList.FirstOrDefault(x => x.Domain == _domain && x.Address == address).Increment();
			}
			else
			{
				switch (DataSize)
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
							(ushort)(_domain.PeekWord(address, BigEndian) + 1),
							BigEndian);
						break;
					case 4:
						_domain.PokeDWord(
							address,
							_domain.PeekDWord(address, BigEndian) + 1,
							BigEndian);
						break;
				}
			}
		}

		private void DecrementAddress(long address)
		{
			if (Global.CheatList.IsActive(_domain, address))
			{
				// TODO: Increment should be intelligent since IsActive is.  If this address is part of a multi-byte cheat it should intelligently increment just that byte
				Global.CheatList.FirstOrDefault(x => x.Domain == _domain && x.Address == address).Decrement();
			}
			else
			{
				switch (DataSize)
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
							(ushort)(_domain.PeekWord(address, BigEndian) - 1),
							BigEndian);
						break;
					case 4:
						_domain.PokeDWord(
							address,
							_domain.PeekDWord(address, BigEndian) - 1,
							BigEndian);
						break;
				}
			}
		}

		private string ValueString(long address)
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

		private void HighlightSecondaries(string value, long found)
		{
			// This function assumes that the primary highlighted value has been set and sets the remaining characters in this string
			_secondaryHighlightedAddresses.Clear();

			var addrLength = DataSize * 2;
			if (value.Length <= addrLength)
			{
				return;
			}

			var numToHighlight = (value.Length / addrLength) - 1;

			for (var i = 0; i < numToHighlight; i += DataSize)
			{
				_secondaryHighlightedAddresses.Add(found + DataSize + i);
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

					for (var i = 0; i < _domain.Size / 16; i++)
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
				RecentTables.Add(ofd.FileName);
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
				RecentTables.HandleLoadError(path);
			}
			else
			{
				RecentTables.Add(path);
				UpdateValues();
			}
		}

		private void RecentTablesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentTablesSubMenu.DropDownItems.Clear();
			RecentTablesSubMenu.DropDownItems.AddRange(
				RecentTables.RecentMenu(LoadFileFromRecent, true));
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
			BigEndianMenuItem.Checked = BigEndian;
			DataSizeByteMenuItem.Checked = DataSize == 1;
			DataSizeWordMenuItem.Checked = DataSize == 2;
			DataSizeDWordMenuItem.Checked = DataSize == 4;

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
			MemoryDomainsMenuItem.DropDownItems.Clear();
			MemoryDomainsMenuItem.DropDownItems.AddRange(
				MemoryDomains.MenuItems(SetMemoryDomain, _domain.Name)
				.ToArray());

			
			var romMenuItem = new ToolStripMenuItem
			{
				Text = _romDomain.Name,
				Checked = _domain.Name == _romDomain.Name
			};

			MemoryDomainsMenuItem.DropDownItems.Add(new ToolStripSeparator());
			MemoryDomainsMenuItem.DropDownItems.Add(romMenuItem);

			romMenuItem.Click += (o, ev) => SetMemoryDomain(_romDomain.Name);

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
			BigEndian ^= true;
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
				GoToAddress(long.Parse(inputPrompt.PromptText, NumberStyles.HexNumber));
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
			var addresses = new List<long>();
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
					InitialLocation = this.ChildPointToScreen(AddressLabel),
					ParentTool = this
				};

				var watches = addresses.Select(
					address => Watch.GenerateWatch(
						_domain,
						address,
						(Watch.WatchSize)DataSize,
						Watch.DisplayType.Hex,
						string.Empty,
						BigEndian));

				poke.SetWatch(watches);
				poke.ShowHawkDialog();
				UpdateValues();
			}
		}

		#endregion

		#region Settings Menu

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

			long newHighlighted;
			switch (e.KeyCode)
			{
				case Keys.Up:
					newHighlighted = _addressHighlighted - 16;
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = newHighlighted + DataSize; i <= _addressHighlighted; i += DataSize)
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
						for (var i = _addressHighlighted; i < newHighlighted; i += DataSize)
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
					newHighlighted = _addressHighlighted - (1 * DataSize);
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
					newHighlighted = _addressHighlighted + (1 * DataSize);
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
						for (var i = newHighlighted + 1; i <= _addressHighlighted; i += DataSize)
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
						for (var i = _addressHighlighted + 1; i < newHighlighted; i += DataSize)
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
						for (var i = 1; i <= _addressHighlighted; i += DataSize)
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
					newHighlighted = _domain.Size - DataSize;
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = _addressHighlighted; i < newHighlighted; i += DataSize)
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

			switch (DataSize)
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

			viewN64MatrixToolStripMenuItem.Visible = DataSize == 4;
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
			// Update font size, as drawing width seems system-dependent.
			if (!fontSizeSet)
			{
				fontSizeSet = true;
				var fontSize = e.Graphics.MeasureString("x", AddressesLabel.Font);
				fontWidth = (int)Math.Round(fontSize.Width / 1.5);
			}

			var activeCheats = Global.CheatList.Where(x => x.Enabled);
			foreach (var cheat in activeCheats)
			{
				if (IsVisible(cheat.Address ?? 0))
				{
					if (_domain.ToString() == cheat.Domain.Name)
					{
						var gaps = (int)cheat.Size - (int)DataSize;

						if (cheat.Size == Watch.WatchSize.DWord && DataSize == 2)
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
				// Create a slight offset to increase rectangle sizes
				var point = GetAddressCoordinates(_addressHighlighted);
				var textX = (int)GetTextX(_addressHighlighted);
				var textpoint = new Point(textX, point.Y);

				var rect = new Rectangle(point, new Size(fontWidth * 2 * DataSize + (NeedsExtra(_addressHighlighted) ? fontWidth : 0) + 2, fontHeight));
				e.Graphics.DrawRectangle(new Pen(Brushes.Black), rect);

				var textrect = new Rectangle(textpoint, new Size(fontWidth * DataSize, fontHeight));

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
				if (IsVisible(address))
				{
					var point = GetAddressCoordinates(address);
					var textX = (int)GetTextX(address);
					var textpoint = new Point(textX, point.Y);

					var rect = new Rectangle(point, new Size(fontWidth * 2 * DataSize + 2, fontHeight));
					e.Graphics.DrawRectangle(new Pen(Brushes.Black), rect);

					var textrect = new Rectangle(textpoint, new Size(fontWidth * DataSize, fontHeight));

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
				var pointedAddress = GetPointedAddress(e.X, e.Y);
				if (pointedAddress >= 0)
				{
					if ((ModifierKeys & Keys.Control) == Keys.Control)
					{
						if (pointedAddress == _addressHighlighted)
						{
							ClearHighlighted();
						}
						else if (_secondaryHighlightedAddresses.Contains(pointedAddress))
						{
							_secondaryHighlightedAddresses.Remove(pointedAddress);
						}
						else
						{
							_secondaryHighlightedAddresses.Add(pointedAddress);
						}
					}
					else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
					{
						DoShiftClick();
					}
					else
					{
						SetHighlighted(pointedAddress);
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

		#endregion

		private void HexMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{

		}

		#endregion

		private void viewN64MatrixToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!HighlightedAddress.HasValue)
				return;

			bool bigend = true;
			long addr = HighlightedAddress.Value;
			//ushort  = _domain.PeekWord(addr,bigend);

			float[,] matVals = new float[4,4];

			for (int i = 0; i < 4; i++)
			{
					for (int j = 0; j < 4; j++) 
					{
						ushort hi = _domain.PeekWord(((addr+(i<<3)+(j<<1)     )^0x0),bigend);
						ushort lo = _domain.PeekWord(((addr+(i<<3)+(j<<1) + 32)^0x0),bigend);
						matVals[i,j] = (int)(((hi << 16) | lo)) / 65536.0f;
					}
			}

			//if needed
			//var mat = new SlimDX.Matrix();
			//mat.M11 = matVals[0, 0]; mat.M12 = matVals[0, 1]; mat.M13 = matVals[0, 2]; mat.M14 = matVals[0, 3];
			//mat.M21 = matVals[1, 0]; mat.M22 = matVals[1, 1]; mat.M23 = matVals[1, 2]; mat.M24 = matVals[1, 3];
			//mat.M31 = matVals[2, 0]; mat.M32 = matVals[2, 1]; mat.M33 = matVals[2, 2]; mat.M34 = matVals[2, 3];
			//mat.M41 = matVals[3, 0]; mat.M42 = matVals[3, 1]; mat.M43 = matVals[3, 2]; mat.M44 = matVals[3, 3];
			//MessageBox.Show(mat.ToString());

			StringWriter sw = new StringWriter();
				for(int i=0;i<4;i++)
			sw.WriteLine("{0,18:0.00000} {1,18:0.00000} {2,18:0.00000} {3,18:0.00000}", matVals[i, 0], matVals[i, 1], matVals[i, 2], matVals[i, 3]);
			var str = sw.ToString();
			MessageBox.Show(str);

		}
	}
} 
