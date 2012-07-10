using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient.tools
{
	class LuaButton : Button
	{
		private void DoLuaClick(object sender, EventArgs e)
		{
			LuaWinform parent = this.Parent as LuaWinform;
			parent.DoLuaEvent(this.Handle);
		}

		protected override void OnClick(EventArgs e)
		{
			DoLuaClick(this, e);
			base.OnClick(e);
		}
	}
}
