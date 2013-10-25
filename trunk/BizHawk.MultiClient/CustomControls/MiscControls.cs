using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Core
{
	public class HorizontalLine : Control
	{
		protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
		{
			base.SetBoundsCore(x, y, width, 2, specified);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			ControlPaint.DrawBorder3D(e.Graphics, 0, 0, Width, 2, Border3DStyle.Etched);
		}
	}

	public class CustomCheckBox : CheckBox
	{
		Color _CheckBackColor = SystemColors.Control;
		public Color CheckBackColor
		{
			get { return _CheckBackColor; }
			set { _CheckBackColor = value; Refresh(); }
		}

		bool? _ForceChecked;
		public bool? ForceChecked
		{
			get { return _ForceChecked; }
			set { _ForceChecked = value; Refresh(); }
		}

		protected override void OnPaint(PaintEventArgs pevent)
		{
			//draw text-label part of the control with something so that it isn't hallofmirrorsy
			using(var brush = new SolidBrush(Parent.BackColor))
				pevent.Graphics.FillRectangle(brush, ClientRectangle);
			
			var r = new Rectangle(ClientRectangle.Location, SystemInformation.MenuCheckSize);
			var glyphLoc = ClientRectangle;
			glyphLoc.Size = SystemInformation.MenuCheckSize;

			//draw the selectedbackdrop color roughly where the glyph belongs
			using (var brush = new SolidBrush(_CheckBackColor))
				pevent.Graphics.FillRectangle(brush, glyphLoc);

			//draw a checkbox menu glyph (we could do this more elegantly with DrawFrameControl) 
			bool c = CheckState == CheckState.Checked;
			if (ForceChecked.HasValue)
			{
				c = ForceChecked.Value;
			}
			if (c)
			{
				glyphLoc.Y--;
				glyphLoc.X++;
				ControlPaint.DrawMenuGlyph(pevent.Graphics, glyphLoc, MenuGlyph.Checkmark, Color.Black, Color.Transparent);
			}

			//draw a border on top of it all
			ControlPaint.DrawBorder3D(pevent.Graphics, r, Border3DStyle.Sunken);

			//stuff that didnt work
			//CheckBoxRenderer.DrawParentBackground(pevent.Graphics, ClientRectangle, this);
			//CheckBoxRenderer.DrawCheckBox(pevent.Graphics, ClientRectangle.Location, System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal);
			//glyphLoc.Size = new System.Drawing.Size(SystemInformation.MenuCheckSize.Width-1,SystemInformation.MenuCheckSize.Height-1);
		}
	}

}