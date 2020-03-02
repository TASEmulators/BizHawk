using System.ComponentModel;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	public abstract class RadioButtonExBase : RadioButton
	{
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new int TabIndex => base.TabIndex;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool TabStop => base.TabStop;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool UseVisualStyleBackColor => base.UseVisualStyleBackColor;
	}
}
