using System.Collections.Generic;
using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		public delegate Color? QueryColor(int index, string column);
		public delegate string/*?*/ QueryText(int index, string column);
		public delegate Bitmap/*?*/ QueryIcon(int index, string column);

		// Everything here is currently for Lua
		private List<QueryColor> _queryColorCallbacks = new();
		public void AddQueryBgColorCallback(QueryColor query) => _queryColorCallbacks.Add(query);
		public void RemoveQueryBgColorCallback(QueryColor query) => _queryColorCallbacks.Remove(query);
		private Color? QueryItemBgColorCallback(int index, string column)
		{
			foreach (QueryColor q in _queryColorCallbacks)
			{
				Color? ret = q(index, column);
				if (ret != null) return ret;
			}
			return null;
		}

		private List<QueryText> _queryTextCallbacks = new();
		public void AddQueryItemTextCallback(QueryText query) => _queryTextCallbacks.Add(query);
		public void RemoveQueryItemTextCallback(QueryText query) => _queryTextCallbacks.Remove(query);
		private string/*?*/ QueryItemTextCallback(int index, string column)
		{
			foreach (QueryText q in _queryTextCallbacks)
			{
				string/*?*/ ret = q(index, column);
				if (ret != null) return ret;
			}
			return null;
		}

		private List<QueryIcon> _queryIconCallbacks = new();
		public void AddQueryItemIconCallback(QueryIcon query) => _queryIconCallbacks.Add(query);
		public void RemoveQueryItemIconCallback(QueryIcon query) => _queryIconCallbacks.Remove(query);
		private Bitmap/*?*/ QueryItemIconCallback(int index, string column)
		{
			foreach (QueryIcon q in _queryIconCallbacks)
			{
				Bitmap/*?*/ ret = q(index, column);
				if (ret != null) return ret;
			}
			return null;
		}

		public Action<int> GreenzoneInvalidatedCallback { get; set; }
		public Action<int> BranchLoadedCallback { get; set; }
		public Action<int> BranchSavedCallback { get; set; }
		public Action<int> BranchRemovedCallback { get; set; }
	}
}
