using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	internal class LuaButton : Button
	{
		protected override void OnClick(EventArgs e)
		{
			(Parent as LuaWinform)?.DoLuaEvent(Handle);
			base.OnClick(e);
		}
	}
}
