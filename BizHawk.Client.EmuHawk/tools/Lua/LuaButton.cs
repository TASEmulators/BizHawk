using System;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	internal class LuaButton : Button
	{
		private void DoLuaClick(object sender, EventArgs e)
		{
			LuaWinform parent = Parent as LuaWinform;
			parent?.DoLuaEvent(Handle);
		}

		protected override void OnClick(EventArgs e)
		{
			DoLuaClick(this, e);
			base.OnClick(e);
		}
	}
}
