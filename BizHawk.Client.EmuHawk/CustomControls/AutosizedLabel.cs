using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public class AutosizedLabel : Label
	{
		public override bool AutoSize => true;

		public AutosizedLabel() : base() {
			Anchor = AnchorStyles.None;
		}

		public AutosizedLabel(string labelText) : this()
		{
			Text = labelText;
		}
	}
}
