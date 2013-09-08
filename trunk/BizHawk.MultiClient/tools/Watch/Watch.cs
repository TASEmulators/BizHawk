using System;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public abstract class Watch
	{
		public enum WatchSize { Byte = 1, Word = 2, DWord = 4, Separator = 0 };
		public enum DisplayType { Separator, Signed, Unsigned, Hex, Binary, FixedPoint_12_4, FixedPoint_20_12, Float };
		public enum PreviousType { OriginalValue = 0, LastSearch = 1, LastFrame = 2, LastChange = 3 };

		public static string DisplayTypeToString(DisplayType type)
		{
			switch (type)
			{
				default:
					return type.ToString();
				case DisplayType.FixedPoint_12_4:
					return "Fixed Point 12.4";
				case DisplayType.FixedPoint_20_12:
					return "Fixed Point 20.12";
			}
		}

		public static DisplayType StringToDisplayType(string name)
		{
			switch(name)
			{
				default:
					return (DisplayType) Enum.Parse(typeof(DisplayType), name);
				case "Fixed Point 12.4":
					return DisplayType.FixedPoint_12_4;
				case "Fixed Point 20.12":
					return DisplayType.FixedPoint_20_12;
			}
		}

		protected int _address;
		protected MemoryDomain _domain;
		protected DisplayType _type;
		protected bool _bigEndian;

		public abstract int? Value { get; }
		public abstract string ValueString { get; }
		public abstract WatchSize Size { get; }

		public virtual DisplayType Type { get { return _type; } set {  _type = value; } }
		public virtual bool BigEndian { get { return _bigEndian; } set { _bigEndian = value; } }

		public MemoryDomain Domain
		{
			get { return _domain; }
		}

		public virtual int? Address
		{
			get { return _address; }
		}

		public virtual string AddressString
		{
			get { return _address.ToString(AddressFormatStr); }
		}

		public virtual bool IsSeparator
		{
			get { return false; }
		}

		public char SizeAsChar
		{
			get
			{
				switch (Size)
				{
					default:
					case WatchSize.Separator:
						return 'S';
					case WatchSize.Byte:
						return 'b';
					case WatchSize.Word:
						return 'w';
					case WatchSize.DWord:
						return 'd';
				}
			}
		}

		public static WatchSize SizeFromChar(char c)
		{
			switch (c)
			{
				default:
				case 'S':
					return WatchSize.Separator;
				case 'b':
					return WatchSize.Byte;
				case 'w':
					return WatchSize.Word;
				case 'd':
					return WatchSize.DWord;
			}
		}

		public char TypeAsChar
		{
			get
			{
				switch (Type)
				{
					default:
					case DisplayType.Separator:
						return '_';
					case DisplayType.Unsigned:
						return 's';
					case DisplayType.Signed:
						return 'u';
					case DisplayType.Hex:
						return 'h';
					case DisplayType.Binary:
						return 'b';
					case DisplayType.FixedPoint_12_4:
						return '1';
					case DisplayType.FixedPoint_20_12:
						return '2';
					case DisplayType.Float:
						return 'f';
				}
			}
		}

		public static DisplayType DisplayTypeFromChar(char c)
		{
			switch (c)
			{
				default:
				case '_':
					return DisplayType.Separator;
				case 'u':
					return DisplayType.Unsigned;
				case 's':
					return DisplayType.Signed;
				case 'h':
					return DisplayType.Hex;
				case 'b':
					return DisplayType.Binary;
				case '1':
					return DisplayType.FixedPoint_12_4;
				case '2':
					return DisplayType.FixedPoint_20_12;
				case 'f':
					return DisplayType.Float;
			}
		}

		public string AddressFormatStr
		{
			get
			{
				if (_domain != null)
				{
					return "X" + IntHelpers.GetNumDigits(_domain.Size - 1).ToString();
				}
				else
				{
					return "";
				}
			}
		}

		protected byte GetByte()
		{
			return _domain.PeekByte(_address);
		}

		protected ushort GetWord()
		{
			if (_bigEndian)
			{
				return (ushort)((_domain.PeekByte(_address) << 8) | (_domain.PeekByte(_address + 1)));
			}
			else
			{
				return (ushort)((_domain.PeekByte(_address)) | (_domain.PeekByte(_address + 1) << 8));
			}
		}

		protected uint GetDWord()
		{
			if (_bigEndian)
			{
				return (uint)((_domain.PeekByte(_address) << 24)
					| (_domain.PeekByte(_address + 1) << 16)
					| (_domain.PeekByte(_address + 2) << 8)
					| (_domain.PeekByte(_address + 3) << 0));
			}
			else
			{
				return (uint)((_domain.PeekByte(_address) << 0)
					| (_domain.PeekByte(_address + 1) << 8)
					| (_domain.PeekByte(_address + 2) << 16)
					| (_domain.PeekByte(_address + 3) << 24));
			}
		}

		public static Watch GenerateWatch(MemoryDomain domain, int address, WatchSize size, bool details)
		{
			switch (size)
			{
				default:
				case WatchSize.Separator:
					return new SeparatorWatch();
				case WatchSize.Byte:
					if (details)
					{
						return new DetailedByteWatch(domain, address);
					}
					else
					{
						return new ByteWatch(domain, address);
					}
				case WatchSize.Word:
					if (details)
					{
						return new DetailedWordWatch(domain, address);
					}
					else
					{
						return new WordWatch(domain, address);
					}
				case WatchSize.DWord:
					if (details)
					{
						return new DetailedDWordWatch(domain, address);
					}
					else
					{
						return new DWordWatch(domain, address);
					}
			}
		}
	}

	public interface IWatchDetails
	{
		int ChangeCount { get; }
		void ClearChangeCount();

		int? Previous { get; }
		string PreviousStr { get; }
		void ResetPrevious();
		string Diff { get; }
		string Notes { get; set; }

		void Update();
	}

	public class SeparatorWatch : Watch
	{
		public SeparatorWatch() { }

		public static SeparatorWatch Instance
		{
			get
			{
				return new SeparatorWatch();
			}
		}

		public override int? Address
		{
			get { return null; }
		}

		public override int? Value
		{
			get { return null; }
		}

		public override string AddressString
		{
			get { return ""; }
		}

		public override string ValueString
		{
			get { return ""; }
		}

		public override string ToString()
		{
			return "----";
		}

		public override bool IsSeparator
		{
			get { return true; }
		}

		public override WatchSize Size
		{
			get { return WatchSize.Separator; }
		}

		public static List<DisplayType> ValidTypes
		{
			get { return new List<DisplayType>() { DisplayType.Separator }; }
		}

		public override DisplayType Type
		{
			get { return DisplayType.Separator; }
		}
	}

	public class ByteWatch : Watch
	{
		public ByteWatch(MemoryDomain domain, int address)
		{
			_address = address;
			_domain = domain;
		}

		public override int? Address
		{
			get { return _address; }
		}

		public override int? Value
		{
			get { return GetByte(); }
		}

		public override string ValueString
		{
			get { return FormatValue(GetByte()); }
		}

		public override string ToString()
		{
			return AddressString + ": " + ValueString;
		}

		public override bool IsSeparator
		{
			get { return false; }
		}

		public override WatchSize Size
		{
			get { return WatchSize.Byte; }
		}

		public static List<DisplayType> ValidTypes
		{
			get
			{
				return new List<DisplayType>()
				{
					DisplayType.Unsigned, DisplayType.Signed, DisplayType.Hex, DisplayType.Binary
				};
			}
		}

		protected string FormatValue(byte val)
		{
			switch (Type)
			{
				default:
				case DisplayType.Unsigned:
					return ((byte)val).ToString();
				case DisplayType.Signed:
					return ((sbyte)val).ToString();
				case DisplayType.Hex:
					return String.Format("{0:X2}", val);
				case DisplayType.Binary:
					return Convert.ToString(val, 2).PadLeft(8, '0').Insert(4, " ");
			}
		}
	}

	public class DetailedByteWatch : ByteWatch, IWatchDetails
	{
		private byte _value;
		private byte _previous;

		public DetailedByteWatch(MemoryDomain domain, int address)
			: base(domain, address)
		{
			Notes = String.Empty;
			_previous = _value = GetByte();
		}

		public override string ToString()
		{
			return Notes + ": " + ValueString;
		}

		public int ChangeCount { get; private set; }
		public void ClearChangeCount() { ChangeCount = 0; }

		public int? Previous { get { return _previous; } }
		public string PreviousStr { get { return FormatValue(_previous); } }
		public void ResetPrevious() { _previous = _value; }

		public string Diff
		{
			get { return FormatValue((byte)(_previous - _value)); }
		}

		public string Notes { get; set; }

		public void Update()
		{
			switch (Global.Config.RamWatchDefinePrevious)
			{
				case PreviousType.LastSearch: //TODO
				case PreviousType.OriginalValue:
					/*Do Nothing*/
					break;
				case PreviousType.LastChange:
					var temp = _value;
					_value = GetByte();
					if (_value != temp)
					{
						_previous = _value;
						ChangeCount++;
					}
					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = GetByte();
					if (_value != Previous)
					{
						ChangeCount++;
					}
					break;
			}
		}
	}

	public class WordWatch : Watch
	{
		public WordWatch(MemoryDomain domain, int address)
		{
			_domain = domain;
			_address = address;
		}

		public override int? Value
		{
			get { return GetWord(); }
		}

		public override WatchSize Size
		{
			get { return WatchSize.Word; }
		}

		public static List<DisplayType> ValidTypes
		{
			get
			{
				return new List<DisplayType>()
				{
					DisplayType.Unsigned, DisplayType.Signed, DisplayType.Hex, DisplayType.FixedPoint_12_4, DisplayType.Binary
				};
			}
		}

		public override string ValueString
		{
			get { return FormatValue(GetWord()); }
		}

		public override string ToString()
		{
			return AddressString + ": " + ValueString;
		}

		protected string FormatValue(ushort val)
		{
			switch (Type)
			{
				default:
				case DisplayType.Unsigned:
					return val.ToString();
				case DisplayType.Signed:
					return ((short)val).ToString();
				case DisplayType.Hex:
					return String.Format("{0:X4}", val);
				case DisplayType.FixedPoint_12_4:
					return String.Format("{0:F4}", ((double)val / 16.0));
				case DisplayType.Binary:
					return Convert.ToString(val, 2).PadLeft(16, '0').Insert(8, " ").Insert(4, " ").Insert(14, " ");
			}
		}
	}

	public class DetailedWordWatch : WordWatch, IWatchDetails
	{
		private ushort _value;
		private ushort _previous;

		public DetailedWordWatch(MemoryDomain domain, int address)
			: base(domain, address)
		{
			Notes = String.Empty;
			_previous = _value = GetWord();
		}

		public override string ToString()
		{
			return Notes + ": " + ValueString;
		}

		public int ChangeCount { get; private set; }
		public void ClearChangeCount() { ChangeCount = 0; }

		public int? Previous { get { return _previous; } }
		public string PreviousStr { get { return FormatValue(_previous); } }
		public void ResetPrevious() { _previous = _value; }

		public string Diff
		{
			get { return FormatValue((ushort)(_previous - _value)); }
		}

		public string Notes { get; set; }

		public void Update()
		{
			switch (Global.Config.RamWatchDefinePrevious)
			{
				case PreviousType.LastSearch: //TODO
				case PreviousType.OriginalValue:
					/*Do Nothing*/
					break;
				case PreviousType.LastChange:
					var temp = _value;
					_value = GetWord();

					if (_value != temp)
					{
						_previous = temp;
						ChangeCount++;
					}
					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = GetWord();
					if (_value != Previous)
					{
						ChangeCount++;
					}
					break;
			}
		}
	}

	public class DWordWatch : Watch
	{
		public DWordWatch(MemoryDomain domain, int address)
		{
			_domain = domain;
			_address = address;
		}

		public override int? Value
		{
			get { return (int)GetDWord(); }
		}

		public override WatchSize Size
		{
			get { return WatchSize.DWord; }
		}

		public static List<DisplayType> ValidTypes
		{
			get
			{
				return new List<DisplayType>()
				{
					DisplayType.Unsigned, DisplayType.Signed, DisplayType.Hex, DisplayType.FixedPoint_20_12, DisplayType.Float
				};
			}
		}

		public override string ValueString
		{
			get { return FormatValue(GetDWord()); }
		}

		public override string ToString()
		{
			return AddressString + ": " + ValueString;
		}

		protected string FormatValue(uint val)
		{
			switch (Type)
			{
				default:
				case DisplayType.Unsigned:
					return val.ToString();
				case DisplayType.Signed:
					return ((int)val).ToString();
				case DisplayType.Hex:
					return String.Format("{0:X8}", val);
				case DisplayType.FixedPoint_20_12:
					return String.Format("{0:F5}", ((double)val / 4096.0));
				case DisplayType.Float:
					byte[] bytes = BitConverter.GetBytes(val);
					float _float = System.BitConverter.ToSingle(bytes, 0);
					return String.Format("{0:F6}", _float);
			}
		}
	}

	public class DetailedDWordWatch : DWordWatch, IWatchDetails
	{
		private uint _value;
		private uint _previous;

		public DetailedDWordWatch(MemoryDomain domain, int address)
			: base(domain, address)
		{
			Notes = String.Empty;
			_previous = _value = GetDWord();
		}

		public override string ToString()
		{
			return Notes + ": " + ValueString;
		}
		public int ChangeCount { get; private set; }
		public void ClearChangeCount() { ChangeCount = 0; }

		public int? Previous { get { return (int)_previous; } }
		public string PreviousStr { get { return FormatValue(_previous); } }
		public void ResetPrevious() { _previous = _value; }

		public string Diff
		{
			get { return FormatValue((uint)(_previous - _value)); }
		}

		public string Notes { get; set; }

		public void Update()
		{
			switch (Global.Config.RamWatchDefinePrevious)
			{
				case PreviousType.LastSearch: //TODO
				case PreviousType.OriginalValue:
					/*Do Nothing*/
					break;
				case PreviousType.LastChange:
					var temp = _value;
					_value = GetDWord();
					if (_value != temp)
					{
						_previous = _value;
						ChangeCount++;
					}
					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = GetDWord();
					if (_value != Previous)
					{
						ChangeCount++;
					}
					break;
			}
		}
	}

	public class WatchList : IEnumerable
	{
		private string _currentFilename = "";

		public enum WatchPrevDef { LastSearch, Original, LastFrame, LastChange };

		private List<Watch> _watchList = new List<Watch>();
		private MemoryDomain _domain = null;

		public WatchList() { }

		public IEnumerator<Watch> GetEnumerator()
		{
			return _watchList.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int Count
		{
			get { return _watchList.Count; }
		}

		public Watch this[int index]
		{
			get
			{
				return _watchList[index];
			}
			set
			{
				_watchList[index] = value;
			}
		}

		public int WatchCount
		{
			get
			{
				return _watchList.Count(w => !w.IsSeparator);
			}
		}

		public int ItemCount
		{
			get
			{
				return _watchList.Count;
			}
		}

		public string AddressFormatStr
		{
			get
			{
				if (_domain != null)
				{
					return "{0:X" + IntHelpers.GetNumDigits(_domain.Size - 1).ToString() + "}";
				}
				else
				{
					return "";
				}
			}
		}

		public void Clear()
		{
			_watchList.Clear();
			Changes = false;
			_currentFilename = "";
		}

		public MemoryDomain Domain { get { return _domain; } set { _domain = value; } }

		public void UpdateValues()
		{
			var detailedWatches = _watchList.OfType<IWatchDetails>().ToList();
			foreach (var watch in detailedWatches)
			{
				watch.Update();
			}
		}

		public void Add(Watch watch)
		{
			_watchList.Add(watch);
			Changes = true;
		}

		public void AddRange(IList<Watch> watches)
		{
			_watchList.AddRange(watches);
			Changes = true;
		}

		public void Remove(Watch watch)
		{
			_watchList.Remove(watch);
			Changes = true;
		}

		public void Insert(int index, Watch watch)
		{
			_watchList.Insert(index, watch);
		}

		public void ClearChangeCounts()
		{
			var detailedWatches = _watchList.OfType<IWatchDetails>().ToList();
			foreach (var watch in detailedWatches)
			{
				watch.ClearChangeCount();
			}
		}

		#region File handling logic - probably needs to be its own class

		public string CurrentFileName { get { return _currentFilename; } set { _currentFilename = value; } }
		public bool Changes { get; set; }

		public bool Save()
		{
			bool result = false;
			if (!String.IsNullOrWhiteSpace(CurrentFileName))
			{
				result = SaveFile();
			}
			else
			{
				result = SaveAs();
			}

			if (result)
			{
				Changes = false;
			}

			return result;
		}

		public bool Load(string path, bool details, bool append)
		{
			bool result = LoadFile(path, details, append);

			if (result)
			{
				if (append)
				{
					Changes = true;
				}
				else
				{
					CurrentFileName = path;
					Changes = false;
				}
			}

			return result;
		}

		public void Reload()
		{
			if (!String.IsNullOrWhiteSpace(CurrentFileName))
			{
				LoadFile(CurrentFileName, true, false);
				Changes = false;
			}
		}

		private bool SaveFile()
		{
			if (String.IsNullOrWhiteSpace(CurrentFileName))
			{
				return false;
			}

			using (StreamWriter sw = new StreamWriter(CurrentFileName))
			{
				StringBuilder sb = new StringBuilder();
				sb
					.Append("Domain ").AppendLine(_domain.Name)
					.Append("SystemID ").AppendLine(Global.Emulator.SystemId);

				foreach (Watch w in _watchList)
				{
					sb
						.Append(String.Format(AddressFormatStr, w.Address)).Append('\t')
						.Append(w.SizeAsChar).Append('\t')
						.Append(w.TypeAsChar).Append('\t')
						.Append(w.BigEndian ? '1' : '0').Append('\t')
						.Append(w.Domain.Name).Append('\t')
						.Append(w is IWatchDetails ? (w as IWatchDetails).Notes : String.Empty)
						.AppendLine();
				}

				sw.WriteLine(sb.ToString());
			}

			return true;
		}

		public bool SaveAs()
		{
			var file = WatchCommon.GetSaveFileFromUser(CurrentFileName);
			if (file != null)
			{
				CurrentFileName = file.FullName;
				return SaveFile();
			}
			else
			{
				return false;
			}
		}

		private bool LoadFile(string path, bool details, bool append)
		{
			string domain = "";
			var file = new FileInfo(path);
			if (file.Exists == false) return false;
			bool isBizHawkWatch = true; //Hack to support .wch files from other emulators
			bool isOldBizHawkWatch = false;
			using (StreamReader sr = file.OpenText())
			{
				string line;

				if (append == false)
				{
					Clear();
				}

				while ((line = sr.ReadLine()) != null)
				{
					//.wch files from other emulators start with a number representing the number of watch, that line can be discarded here
					//Any properly formatted line couldn't possibly be this short anyway, this also takes care of any garbage lines that might be in a file
					if (line.Length < 5)
					{
						isBizHawkWatch = false;
						continue;
					}

					if (line.Length >= 6 && line.Substring(0, 6) == "Domain")
					{
						domain = line.Substring(7, line.Length - 7);
						isBizHawkWatch = true;
					}

					if (line.Length >= 8 && line.Substring(0, 8) == "SystemID")
					{
						continue;
					}

					int numColumns = StringHelpers.HowMany(line, '\t');
					int startIndex;
					if (numColumns == 5)
					{
						//If 5, then this is a post 1.0.5 .wch file
						if (isBizHawkWatch)
						{
							//Do nothing here
						}
						else
						{
							startIndex = line.IndexOf('\t') + 1;
							line = line.Substring(startIndex, line.Length - startIndex);   //5 digit value representing the watch position number
						}
					}
					else if (numColumns == 4)
					{
						isOldBizHawkWatch = true;
					}
					else //4 is 1.0.5 and earlier
					{
						continue;   //If not 4, something is wrong with this line, ignore it
					}



					//Temporary, rename if kept
					int addr = 0;
					Watch.WatchSize size = Watch.WatchSize.Separator;
					Watch.DisplayType type = Watch.DisplayType.Unsigned;
					bool bigEndian = false;
					MemoryDomain memDomain = Global.Emulator.MainMemory;
					string notes;

					string temp = line.Substring(0, line.IndexOf('\t'));
					try
					{
						addr = Int32.Parse(temp, NumberStyles.HexNumber);
					}
					catch
					{
						continue;
					}

					startIndex = line.IndexOf('\t') + 1;
					line = line.Substring(startIndex, line.Length - startIndex);   //Type
					size = Watch.SizeFromChar(line[0]);


					startIndex = line.IndexOf('\t') + 1;
					line = line.Substring(startIndex, line.Length - startIndex);   //Signed
					type = Watch.DisplayTypeFromChar(line[0]);

					startIndex = line.IndexOf('\t') + 1;
					line = line.Substring(startIndex, line.Length - startIndex);   //Endian
					try
					{
						startIndex = Int16.Parse(line[0].ToString());
					}
					catch
					{
						continue;
					}
					if (startIndex == 0)
					{
						bigEndian = false;
					}
					else
					{
						bigEndian = true;
					}

					if (isBizHawkWatch && !isOldBizHawkWatch)
					{
						startIndex = line.IndexOf('\t') + 1;
						line = line.Substring(startIndex, line.Length - startIndex);   //Domain
						temp = line.Substring(0, line.IndexOf('\t'));
						memDomain = Global.Emulator.MemoryDomains[GetDomainPos(temp)];
					}

					startIndex = line.IndexOf('\t') + 1;
					notes = line.Substring(startIndex, line.Length - startIndex);   //User notes

					Watch w = Watch.GenerateWatch(memDomain, addr, size, details);
					w.BigEndian = bigEndian;
					w.Type = type;
					if (w is IWatchDetails)
					{
						(w as IWatchDetails).Notes = notes;
					}

					_watchList.Add(w);
					_domain = Global.Emulator.MemoryDomains[GetDomainPos(domain)];
				}
			}

			return true;
		}

		private static int GetDomainPos(string name)
		{
			//Attempts to find the memory domain by name, if it fails, it defaults to index 0
			for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
			{
				if (Global.Emulator.MemoryDomains[x].Name == name)
					return x;
			}
			return 0;
		}

		public static FileInfo GetFileFromUser(string currentFile)
		{
			var ofd = new OpenFileDialog();
			if (currentFile.Length > 0)
				ofd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.WatchPath, null);
			ofd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
			ofd.RestoreDirectory = true;

			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(ofd.FileName);
			return file;
		}

		public static FileInfo GetSaveFileFromUser(string currentFile)
		{
			var sfd = new SaveFileDialog();
			if (currentFile.Length > 0)
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(currentFile);
				sfd.InitialDirectory = Path.GetDirectoryName(currentFile);
			}
			else if (!(Global.Emulator is NullEmulator))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.WatchPath, null);
			}
			else
			{
				sfd.FileName = "NULL";
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.WatchPath, null);
			}
			sfd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
			sfd.RestoreDirectory = true;
			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(sfd.FileName);
			return file;
		}

		#endregion
	}
}
