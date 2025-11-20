using System.Collections.Generic;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public class LuaDropDown : ComboBox
	{
		public LuaDropDown(ICollection<string> items)
		{
			this.ReplaceItems(items: items);
			if (items.Count > 0) SelectedIndex = 0;
			DropDownStyle = ComboBoxStyle.DropDownList;
		}

		public void SetItems(ICollection<string> items)
		{
			this.ReplaceItems(items: items);
			if (items.Count > 0) SelectedIndex = 0;
		}
	}
}
