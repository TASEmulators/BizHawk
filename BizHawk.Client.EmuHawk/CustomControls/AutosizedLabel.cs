using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public sealed class AutosizedLabel : Label
	{
		public AutosizedLabel(string labelText)
		{
			AutoSize = true;
			Text = labelText;
		}
	}
}
