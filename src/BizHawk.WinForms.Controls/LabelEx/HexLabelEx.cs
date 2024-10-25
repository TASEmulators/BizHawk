using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using BizHawk.Common;


namespace BizHawk.WinForms.Controls
{
	/// <inheritdoc cref="Docs.LabelOrLinkLabel"/>
	public class HexLabelEx : LabelExBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool AutoSize => base.AutoSize;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Size Size => base.Size;

		public Color ZeroColor { get; set; } = Color.SlateGray;

		private float spacingHexModifier;
		private float spacingPreDivider;
		private float spacingPostDividerModifier;
		private float spacingAscii;
		private float spacingLineModifier;

		public HexLabelEx()
		{
			base.AutoSize = true;
			this.BackColor = Color.Transparent;

			spacingHexModifier = 0.7F;
			spacingPreDivider = 3.0F;
			spacingPostDividerModifier = 3.5F;
			spacingAscii = 7.0F;
			spacingLineModifier = 0.56F;

			if (OSTailoredCode.IsUnixHost)
			{
				// TODO: spacing values will probably be different on linux
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			string text = this.Text;
			Font font = this.Font;
			PointF point = new PointF(0, 0);
			char gap = ' ';

			string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

			Color color00 = this.ForeColor;
			Color color = this.ForeColor;

			foreach (string line in lines)
			{
				// split left and right panes
				string[] panes = line.Split('|');

				if (panes.Length < 2)
				{
					// skip - last line appears to be empty
					continue;
				}

				// hex pane
				string[] words = panes[0].Split(gap);
				foreach (var word in words)
				{
					SizeF size = e.Graphics.MeasureString(word, font);

					switch (word)
					{
						case "00":
							color = ZeroColor;
							break;

						default:
							color = this.ForeColor;
							break;
					}
					using (Brush brush = new SolidBrush(color))
					{
						e.Graphics.DrawString(word, font, brush, point);
					}

					point.X += size.Width + e.Graphics.MeasureString(gap.ToString(), font).Width + spacingHexModifier;
				}

				// divider
				string div = "|";
				point.X -= spacingPreDivider;
				SizeF sizeDiv = e.Graphics.MeasureString(div, font);
				using (Brush brush = new SolidBrush(this.ForeColor))
				{
					e.Graphics.DrawString(div, font, brush, point);					
				}

				point.X += e.Graphics.MeasureString(gap.ToString(), font).Width + spacingPostDividerModifier;

				// ascii pane
				char[] chars = panes[1].ToCharArray();
				foreach (var c in chars)
				{
					string str = c.ToString();

					using (Brush brush = new SolidBrush(this.ForeColor))
					{
						e.Graphics.DrawString(str, font, brush, point);
					}

					// fixed size
					point.X += spacingAscii;
				}

				point.X = 0;
				point.Y += e.Graphics.MeasureString(line, font).Height + spacingLineModifier;
			}
		}
	}
}
