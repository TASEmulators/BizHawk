using System.ComponentModel;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public class AutosizedLabel : Label
	{
		
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
