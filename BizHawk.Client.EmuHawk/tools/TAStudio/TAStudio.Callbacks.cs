using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		// Everything here is currently for Lua
		public Func<int, string, Color?> QueryItemBgColorCallback { get; set; }
		public Func<int, string, string> QueryItemTextCallback { get; set; }
		public Func<int, string, Bitmap> QueryItemIconCallback { get; set; }

		public Action<int> GreenzoneInvalidatedCallback { get; set; }

		private Color? GetColorOverride(int index, InputRoll.RollColumn column)
		{
			if (QueryItemBgColorCallback != null)
			{
				return QueryItemBgColorCallback(index, column.Name);
			}

			return null;
		}

		private string GetTextOverride(int index, InputRoll.RollColumn column)
		{
			if (QueryItemTextCallback != null)
			{
				return QueryItemTextCallback(index, column.Name);
			}

			return null;
		}

		private Bitmap GetIconOverride(int index, InputRoll.RollColumn column)
		{
			if (QueryItemIconCallback != null)
			{
				return QueryItemIconCallback(index, column.Name);
			}

			return null;
		}

		private void GreenzoneInvalidated(int index)
		{
			if (GreenzoneInvalidatedCallback != null)
			{
				GreenzoneInvalidatedCallback(index);
			}
		}
	}
}
