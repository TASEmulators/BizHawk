using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.MultiClient
{
	class RamSearchEngine
	{
		public enum Mode { Fast, Detailed }

		private MemoryDomain _domain;
		private Mode _mode;
		private WatchList _watchList;

		#region Constructors
		
		public RamSearchEngine(MemoryDomain domain, Mode mode = Mode.Detailed)
		{
			_domain = domain;
			_mode = mode;
		}

		#endregion

		#region Initialize, Manipulate

		public void Start()
		{
			_watchList = new WatchList(_domain);

			//TODO: other byte sizes, mis-aligned
			for(int i = 0; i < _domain.Size; i++)
			{
				_watchList.Add(Watch.GenerateWatch(_domain, i, Watch.WatchSize.Byte, true));
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

		public Mode SearchMode
		{
			get { return _mode; }
		}

		public string DomainName
		{
			get { return _domain.Name; }
		}

		public void Update()
		{
			if (_mode == Mode.Detailed)
			{
			}
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
	}
}
