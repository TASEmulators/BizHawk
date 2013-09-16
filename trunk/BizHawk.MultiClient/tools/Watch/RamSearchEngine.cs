using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.MultiClient
{
	class RamSearchEngine
	{
		private WatchList _watchList;
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
			_watchList = new WatchList(_settings.Domain);

			//TODO: other byte sizes, mis-aligned
			for (int i = 0; i < _settings.Domain.Size; i++)
			{
				_watchList.Add(Watch.GenerateWatch(_settings.Domain, i, _settings.Size, _settings.Mode == Settings.SearchMode.Detailed));
			}
		}

		/// <summary>
		/// Exposes the current watch state based on index
		/// </summary>
		public Watch this[int index]
		{
			get
			{
				return _watchList[index];
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
