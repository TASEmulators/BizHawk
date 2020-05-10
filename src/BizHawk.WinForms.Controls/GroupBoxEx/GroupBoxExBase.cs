using System.ComponentModel;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	public abstract class GroupBoxExBase : GroupBox
	{
		public readonly RadioButtonGroupTracker Tracker = new RadioButtonGroupTracker();

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new int TabIndex => base.TabIndex;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool TabStop => base.TabStop;
	}
}
