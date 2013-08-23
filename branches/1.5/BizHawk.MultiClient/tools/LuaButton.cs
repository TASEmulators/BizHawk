using System;
using System.Windows.Forms;

namespace BizHawk.MultiClient.tools
{
	class LuaButton : Button
	{
		private void DoLuaClick(object sender, EventArgs e)
		{
			LuaWinform parent = Parent as LuaWinform;
			if (parent != null) parent.DoLuaEvent(Handle);
		}

		protected override void OnClick(EventArgs e)
		{
			DoLuaClick(this, e);
			base.OnClick(e);
		}
	}
}
