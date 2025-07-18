using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.WinForms.Controls
{
	public class ToolStripMenuItemEx : ToolStripMenuItem
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size Size => base.Size;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new string Name
			=> Util.GetRandomUUIDStr();

		public void SetStyle(FontStyle style) => Font = new Font(Font.FontFamily, Font.Size, style);
	}

	public class ToolStripSeparatorEx : ToolStripSeparator
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size Size => base.Size;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new string Name
			=> Util.GetRandomUUIDStr();
	}
}