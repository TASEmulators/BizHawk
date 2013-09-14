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

		#region Constructors
		
		public RamSearchEngine(MemoryDomain domain, Mode mode = Mode.Detailed)
		{
			_domain = domain;
			_mode = mode;
		}

		#endregion

		#region Public

		/// <summary>
		/// Exposes the current watch state based on index
		/// </summary>
		public Watch this[int index]
		{
			get
			{
				return SeparatorWatch.Instance; //TODO
			}
		}

		public int Count
		{
			get
			{
				return 0; //TODO
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

		#endregion

		#region Private parts
		#endregion
	}
}
