using System.ComponentModel;

namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.LabelOrLinkLabel"/>
	public class LocSzLabelEx : LabelExBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;
	}
}
