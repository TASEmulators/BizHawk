using System;
using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		// Everything here is currently for Lua
		public Func<int, string, Color?> QueryItemBgColorCallback { get; set; }
		public Func<int, string, string> QueryItemTextCallback { get; set; }
		public Func<int, string, Bitmap> QueryItemIconCallback { get; set; }

		public Action<int> GreenzoneInvalidatedCallback { get; set; }
		public Action<int> BranchLoadCallback { get; set; }

		private Color? GetColorOverride(int index, InputRoll.RollColumn column)
		{
			return QueryItemBgColorCallback?.Invoke(index, column.Name);
		}

		private string GetTextOverride(int index, InputRoll.RollColumn column)
		{
			return QueryItemTextCallback?.Invoke(index, column.Name);
		}

		private Bitmap GetIconOverride(int index, InputRoll.RollColumn column)
		{
			return QueryItemIconCallback?.Invoke(index, column.Name);
		}

		private void GreenzoneInvalidated(int index)
		{
			GreenzoneInvalidatedCallback?.Invoke(index);
		}

		private void BranchLoad(int index)
		{
			BranchLoadCallback?.Invoke(index);
		}
	}
}
