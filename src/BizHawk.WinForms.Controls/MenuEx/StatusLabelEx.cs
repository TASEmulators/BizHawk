using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	public class StatusLabelEx : ToolStripStatusLabel
	{
		private string? _name;

		public StatusLabelEx()
		{
			AccessibleRole = AccessibleRole.StaticText;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size Size => base.Size;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new string Name
		{
			get => _name ?? base.Name;
			set => _name = value;
		}
	}
}
