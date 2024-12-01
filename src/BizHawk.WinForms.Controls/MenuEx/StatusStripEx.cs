using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	/// <summary>
	/// This class adds on to the functionality provided in <see cref="StatusStrip"/>.
	/// </summary>
	public class StatusStripEx : StatusStrip
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size Size => base.Size;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new string Text => "";

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (m.Msg == NativeConstants.WM_MOUSEACTIVATE
				&& m.Result == (IntPtr)NativeConstants.MA_ACTIVATEANDEAT)
			{
				m.Result = (IntPtr)NativeConstants.MA_ACTIVATE;
			}
		}
	}
}
