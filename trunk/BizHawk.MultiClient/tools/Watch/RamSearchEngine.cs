using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.MultiClient
{
	class RamSearchEngine
	{
		private List<IMiniWatch> _watchList = new List<IMiniWatch>();
		private Settings _settings;

		#region Constructors

		public RamSearchEngine(Settings settings)
		{
			_settings = settings;
		}

		#endregion

		#region Initialize, Manipulate

		public void Start()
		{
			_watchList.Clear();
			switch (_settings.Size)
			{
				default:
				case Watch.WatchSize.Byte:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						for (int i = 0; i < _settings.Domain.Size; i++)
						{
							_watchList.Add(new MiniByteWatchDetailed(_settings.Domain, i));
						}
					}
					else
					{
						for (int i = 0; i < _settings.Domain.Size; i++)
						{
							_watchList.Add(new MiniByteWatch(_settings.Domain, i));
						}
					}
					break;
				case Watch.WatchSize.Word:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						for (int i = 0; i < _settings.Domain.Size; i += (_settings.CheckMisAligned ? 1 : 2))
						{
							_watchList.Add(new MiniWordWatchDetailed(_settings.Domain, i, _settings.BigEndian));
						}
					}
					else
					{
						for (int i = 0; i < _settings.Domain.Size; i += (_settings.CheckMisAligned ? 1 : 2))
						{
							_watchList.Add(new MiniWordWatch(_settings.Domain, i, _settings.BigEndian));
						}
					}
					break;
				case Watch.WatchSize.DWord:
					if (_settings.Mode == Settings.SearchMode.Detailed)
					{
						for (int i = 0; i < _settings.Domain.Size; i += (_settings.CheckMisAligned ? 1 : 4))
						{
							_watchList.Add(new MiniDWordWatchDetailed(_settings.Domain, i, _settings.BigEndian));
						}
					}
					else
					{
						for (int i = 0; i < _settings.Domain.Size; i += (_settings.CheckMisAligned ? 1 : 4))
						{
							_watchList.Add(new MiniDWordWatch(_settings.Domain, i, _settings.BigEndian));
						}
					}
					break;
			}
		}

		/// <summary>
		/// Exposes the current watch state based on index
		/// </summary>
		public Watch this[int index]
		{
			get
			{
				if (_settings.Mode == Settings.SearchMode.Detailed)
				{
					return Watch.GenerateWatch(
						_settings.Domain,
						_watchList[index].Address,
						_settings.Size,
						_settings.Type,
						_settings.BigEndian,
						_watchList[index].Previous,
						(_watchList[index] as IMiniWatchDetails).ChangeCount
					);
				}
				else
				{
					return Watch.GenerateWatch(
						_settings.Domain,
						_watchList[index].Address,
						_settings.Size,
						_settings.Type,
						_settings.BigEndian,
						_watchList[index].Previous,
						0
					);
				}
			}
		}

		public int Count
		{
			get
			{
				return _watchList.Count;
			}
		}

		public string DomainName
		{
			get { return _settings.Domain.Name; }
		}

		public void Update()
		{
			if (_settings.Mode == Settings.SearchMode.Detailed)
			{
				foreach (IWatchDetails watch in _watchList)
				{
					watch.Update();
				}
			}
			else
			{
				/*TODO*/
			}
		}

		public void SetType(Watch.DisplayType type)
		{
			if (Watch.AvailableTypes(_settings.Size).Contains(type))
			{
				_settings.Type = type;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public void SetEndian(bool bigendian)
		{
			_settings.BigEndian = bigendian;
		}

		public void SetPreviousType(Watch.PreviousType type)
		{
			_settings.PreviousType = type;
		}

		#endregion

		#region Comparisons

		public void LessThan()
		{
		}

		public void LessThanOrEqual()
		{
		}

		public void GreaterThan()
		{
		}

		public void GreaterThanOrEqual()
		{
		}

		public void Equal()
		{
		}

		public void NotEqual()
		{
		}

		#endregion

		#region Private parts
		#endregion

		#region Classes

		private interface IMiniWatch
		{
			int Address { get; }
			int Previous { get; }
		}

		private interface IMiniWatchDetails
		{
			int ChangeCount { get; }
			void Update();
		}

		private class MiniByteWatch : IMiniWatch
		{
			public int Address { get; private set; }
			private byte _previous;

			public MiniByteWatch(MemoryDomain domain, int addr)
			{
				Address = addr;
				_previous = domain.PeekByte(addr);
			}

			public int Previous
			{
				get { return _previous; }
			}
		}

		private class MiniWordWatch : IMiniWatch
		{
			public int Address { get; private set; }
			private ushort _previous;

			public MiniWordWatch(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;
				if (bigEndian)
				{
					_previous = (ushort)((domain.PeekByte(addr) << 8) | (domain.PeekByte(addr + 1)));
				}
				else
				{
					_previous = (ushort)((domain.PeekByte(addr)) | (domain.PeekByte(addr + 1) << 8));
				}
			}

			public int Previous
			{
				get { return _previous; }
			}

		}

		public class MiniDWordWatch : IMiniWatch
		{
			public int Address { get; private set; }
			private uint _previous;

			public MiniDWordWatch(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;

				if (bigEndian)
				{
					_previous = (uint)((domain.PeekByte(addr) << 24)
						| (domain.PeekByte(addr + 1) << 16)
						| (domain.PeekByte(addr + 2) << 8)
						| (domain.PeekByte(addr + 3) << 0));
				}
				else
				{
					_previous = (uint)((domain.PeekByte(addr) << 0)
						| (domain.PeekByte(addr + 1) << 8)
						| (domain.PeekByte(addr + 2) << 16)
						| (domain.PeekByte(addr + 3) << 24));
				}
			}

			public int Previous
			{
				get { return (int) _previous; }
			}
		}

		private class MiniByteWatchDetailed : IMiniWatch, IMiniWatchDetails
		{
			public int Address { get; private set; }
			private byte _previous;
			int _changecount = 0;

			public MiniByteWatchDetailed(MemoryDomain domain, int addr)
			{
				Address = addr;
				_previous = domain.PeekByte(addr);
			}

			public int Previous
			{
				get { return _previous; }
			}

			public int ChangeCount
			{
				get { return _changecount; }
			}

			public void Update()
			{
				/*TODO*/
			}
		}

		private class MiniWordWatchDetailed : IMiniWatch, IMiniWatchDetails
		{
			public int Address { get; private set; }
			private ushort _previous;
			int _changecount = 0;

			public MiniWordWatchDetailed(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;
				if (bigEndian)
				{
					_previous = (ushort)((domain.PeekByte(addr) << 8) | (domain.PeekByte(addr + 1)));
				}
				else
				{
					_previous = (ushort)((domain.PeekByte(addr)) | (domain.PeekByte(addr + 1) << 8));
				}
			}

			public int Previous
			{
				get { return _previous; }
			}

			public int ChangeCount
			{
				get { return _changecount; }
			}

			public void Update()
			{
				/*TODO*/
			}
		}

		public class MiniDWordWatchDetailed : IMiniWatch, IMiniWatchDetails
		{
			public int Address { get; private set; }
			private uint _previous;
			int _changecount = 0;

			public MiniDWordWatchDetailed(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;

				if (bigEndian)
				{
					_previous = (uint)((domain.PeekByte(addr) << 24)
						| (domain.PeekByte(addr + 1) << 16)
						| (domain.PeekByte(addr + 2) << 8)
						| (domain.PeekByte(addr + 3) << 0));
				}
				else
				{
					_previous = (uint)((domain.PeekByte(addr) << 0)
						| (domain.PeekByte(addr + 1) << 8)
						| (domain.PeekByte(addr + 2) << 16)
						| (domain.PeekByte(addr + 3) << 24));
				}
			}

			public int Previous
			{
				get { return (int)_previous; }
			}

			public int ChangeCount
			{
				get { return _changecount; }
			}

			public void Update()
			{
				/*TODO*/
			}
		}

		public class Settings
		{
			/*Require restart*/
			public enum SearchMode { Fast, Detailed }
			
			public SearchMode Mode = SearchMode.Detailed;
			public MemoryDomain Domain = Global.Emulator.MainMemory;
			public Watch.WatchSize Size = Watch.WatchSize.Byte;
			public bool CheckMisAligned = false;

			/*Can be changed mid-search*/
			public Watch.DisplayType Type = Watch.DisplayType.Unsigned;
			public bool BigEndian = false;
			public Watch.PreviousType PreviousType = Watch.PreviousType.LastSearch;
		}

		#endregion
	}
}
