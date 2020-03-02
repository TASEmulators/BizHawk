using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.GroupBox"/>
	public class SzGroupBoxEx : GroupBox
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Point Location => base.Location;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new int TabIndex => base.TabIndex;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool TabStop => base.TabStop;
	}
}
