using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public class LuaDropDown : ComboBox
	{
		public LuaDropDown(ICollection<string> items)
		{
			Items.AddRange(items.Cast<object>().ToArray());
			SelectedIndex = 0;
			DropDownStyle = ComboBoxStyle.DropDownList;
		}

		public void SetItems(ICollection<string> items)
		{
			Items.Clear();
			Items.AddRange(items.Cast<object>().ToArray());
			SelectedIndex = 0;
		}
	}
}
