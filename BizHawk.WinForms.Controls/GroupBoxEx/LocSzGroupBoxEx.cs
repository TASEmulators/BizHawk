using System.ComponentModel;

namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.GroupBox"/>
	public class LocSzGroupBoxEx : GroupBoxExBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;

		public LocSzGroupBoxEx() : base(out _) {}

		public LocSzGroupBoxEx(out IRadioButtonReadOnlyTracker tracker) : base(out tracker) {}
	}
}
