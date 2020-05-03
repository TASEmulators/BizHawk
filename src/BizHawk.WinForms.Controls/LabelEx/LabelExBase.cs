using System.ComponentModel;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	public abstract class LabelExBase : Label
	{
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new int TabIndex => base.TabIndex;
	}
}
