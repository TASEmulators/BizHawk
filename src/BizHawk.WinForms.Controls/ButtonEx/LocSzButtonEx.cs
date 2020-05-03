using System.ComponentModel;

namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.Button"/>
	public class LocSzButtonEx : ButtonExBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;
	}
}
