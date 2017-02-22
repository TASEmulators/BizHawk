using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class ScreenshotPopup
	{
		public TasBranch Branch { get; set; }
		public Font Font;
		public FontStyle FontStyle;
		public int Width;
		public int Height;
		public int FontSize;
		public int DrawingHeight;
		public int Padding;
		public string Text;

		public ScreenshotPopup()
		{
			Width = 0;
			Height = 0;
			FontSize = 10;
			FontStyle = FontStyle.Regular;
			Font = new Font(FontFamily.GenericMonospace, FontSize, FontStyle);
			DrawingHeight = 0;
			Padding = 0;
		}

		public void UpdateValues(TasBranch branch, int width, int height, int padding)
		{
			Branch = branch;
			Width = width;
			Padding = padding;
			DrawingHeight = height;
			Text = Branch.UserText;

			// Set the screenshot to "1x" resolution of the core
			// cores like n64 and psx are going to still have sizes too big for the control, so cap them
			if (Width > 320)
			{
				double ratio = 320.0 / (double)Width;
				Width = 320;
				DrawingHeight = (int)((double)(DrawingHeight) * ratio);
			}

			if (Padding > 0)
				Padding += 2;
			Height = DrawingHeight + Padding;
		}

		public void Popup(object sender, PopupEventArgs e)
		{
			e.ToolTipSize = new Size(Width, Height);
		}

		public void Draw(object sender, DrawToolTipEventArgs e)
		{
			Branch.OSDFrameBuffer.DiscardAlpha();
			var bitmap = Branch.OSDFrameBuffer.ToSysdrawingBitmap();

			e.DrawBackground();
			e.DrawBorder();
			e.Graphics.DrawImage(bitmap, e.Bounds.Left, e.Bounds.Top, Width, DrawingHeight);

			if (Padding > 0)
				e.Graphics.DrawString(Text, Font, Brushes.Black, 
					new Rectangle(3, DrawingHeight, Width - 3, Height));
		}
	}
}
