using System.ComponentModel;
using System.Drawing;

namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.GroupBox"/>
	public class SzGroupBoxEx : GroupBoxExBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Point Location => base.Location;
	}
}
