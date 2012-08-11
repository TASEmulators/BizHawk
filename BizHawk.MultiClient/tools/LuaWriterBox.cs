using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;




using System.ComponentModel;
using System.Diagnostics;
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

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			base.OnPaintBackground(pevent);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
		}

		#region win32interop

		protected override void WndProc(ref System.Windows.Forms.Message m)
		{
			base.WndProc(ref m);
			if (m.Msg == 0x000F && !InhibitPaint) //WM_PAINT
			{
				// raise the paint event
				using (Graphics graphic = base.CreateGraphics())
					OnPaint(new PaintEventArgs(graphic,
					 base.ClientRectangle));
			}

		}

		#endregion

		
	}
}
