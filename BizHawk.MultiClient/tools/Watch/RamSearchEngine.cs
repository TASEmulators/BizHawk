using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.MultiClient
{
	class RamSearchEngine
	{
		private List<MiniWatch> _watchList = new List<MiniWatch>();
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
					for (int i = 0; i < _settings.Domain.Size; i++)
					{
						_watchList.Add(new MiniByteWatch(_settings.Domain, i));
					}
					break;
				case Watch.WatchSize.Word:
					for (int i = 0; i < _settings.Domain.Size; i += (_settings.CheckMisAligned ? 1 : 2))
					{
						_watchList.Add(new MiniWordWatch(_settings.Domain, i, _settings.BigEndian));
					}
					break;
				case Watch.WatchSize.DWord:
					for (int i = 0; i < _settings.Domain.Size; i += (_settings.CheckMisAligned ? 1 : 4))
					{
						_watchList.Add(new MiniDWordWatch(_settings.Domain, i, _settings.BigEndian));
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
				//TODO: must set prev value, change count, and display type!
				return Watch.GenerateWatch(
					_settings.Domain,
					_watchList[index].Address,
					_settings.Size,
					_settings.Type,
					_settings.BigEndian,
					_watchList[index].Value,
					_watchList[index].Value /*TODO*/);
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
			//TODO
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
		private interface MiniWatch
		{
			int Address { get; }
			int Value { get; }
		}

		private class MiniByteWatch : MiniWatch
		{
			public int Address { get; private set; }
			private byte val;

			public MiniByteWatch(MemoryDomain domain, int addr)
			{
				Address = addr;
				val = domain.PeekByte(addr);
			}

			public int Value
			{
				get { return val; }
			}
		}

		private class MiniWordWatch : MiniWatch
		{
			public int Address { get; private set; }
			private ushort val;

			public MiniWordWatch(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;
				if (bigEndian)
				{
					val = (ushort)((domain.PeekByte(addr) << 8) | (domain.PeekByte(addr + 1)));
				}
				else
				{
					val = (ushort)((domain.PeekByte(addr)) | (domain.PeekByte(addr + 1) << 8));
				}
			}

			public int Value
			{
				get { return val; }
			}

		}

		public class MiniDWordWatch : MiniWatch
		{
			public int Address { get; private set; }
			private uint val;

			public MiniDWordWatch(MemoryDomain domain, int addr, bool bigEndian)
			{
				Address = addr;

				if (bigEndian)
				{
					val = (uint)((domain.PeekByte(addr) << 24)
						| (domain.PeekByte(addr + 1) << 16)
						| (domain.PeekByte(addr + 2) << 8)
						| (domain.PeekByte(addr + 3) << 0));
				}
				else
				{
					val = (uint)((domain.PeekByte(addr) << 0)
						| (domain.PeekByte(addr + 1) << 8)
						| (domain.PeekByte(addr + 2) << 16)
						| (domain.PeekByte(addr + 3) << 24));
				}
			}

			public int Value
			{
				get { return (int) val; }
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
		}
		#endregion
	}
}
