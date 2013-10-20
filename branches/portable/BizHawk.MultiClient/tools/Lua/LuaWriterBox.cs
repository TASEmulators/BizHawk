using System.Windows.Forms;
using System.Drawing;

namespace BizHawk.MultiClient
{
	class LuaWriterBox : RichTextBox
	{
		public bool InhibitPaint = false;

		public LuaWriterBox()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
		}

		#region win32interop

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (m.Msg == 0x000F && !InhibitPaint) //WM_PAINT
			{
				// raise the paint event
				using (Graphics graphic = CreateGraphics())
					OnPaint(new PaintEventArgs(graphic,
					 ClientRectangle));
			}

		}

		#endregion

		
	}
}
