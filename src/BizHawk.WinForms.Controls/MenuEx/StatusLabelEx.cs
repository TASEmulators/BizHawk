using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.WinForms.Controls
{
	public class StatusLabelEx : ToolStripStatusLabel
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size Size => base.Size;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new string Name
			=> Util.GetRandomUUIDStr();
	}
}
