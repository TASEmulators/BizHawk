using System;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public class LuaCheckbox : CheckBox
	{
		private void DoLuaClick(object sender, EventArgs e)
		{
			var parent = Parent as LuaWinform;
			if (parent != null)
			{
				parent.DoLuaEvent(Handle);
			}
		}

		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);
			DoLuaClick(this, e);
		}
	}
}
