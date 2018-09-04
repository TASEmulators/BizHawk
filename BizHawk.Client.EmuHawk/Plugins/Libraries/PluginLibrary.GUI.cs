using System;
using System.Drawing;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class GUIPluginLibrary : GUIDrawPluginBase
    {
		public GUIPluginLibrary() : base()
		{ }

        private DisplaySurface _GUISurface = null;

        #region Gui API

        public override void DrawNew(string name, bool? clear = true)
		{
			try
			{
				DrawFinish();
				_GUISurface = GlobalWin.DisplayManager.LockLuaSurface(name, clear ?? true);
                HasGUISurface = (_GUISurface != null);

            }
			catch (InvalidOperationException ex)
			{
                Console.WriteLine(ex.ToString());
			}
		}

		public override void DrawFinish()
		{
			if (_GUISurface != null)
			{
				GlobalWin.DisplayManager.UnlockLuaSurface(_GUISurface);
			}

			_GUISurface = null;
            HasGUISurface = false;

        }
        #endregion

        #region Helpers
        protected override Graphics GetGraphics()
		{
			var g = _GUISurface == null ? Graphics.FromImage(new Bitmap(1,1)) : _GUISurface.GetGraphics();

			// we don't like CoreComm, right? Someone should find a different way to do this then.
			var tx = Emulator.CoreComm.ScreenLogicalOffsetX;
			var ty = Emulator.CoreComm.ScreenLogicalOffsetY;
			if (tx != 0 || ty != 0)
			{
				var transform = g.Transform;
				transform.Translate(-tx, -ty);
				g.Transform = transform;
			}

			return g;
		}
        #endregion

        public override void AddMessage(string message)
		{
			GlobalWin.OSD.AddMessage(message);
		}

        public override void ClearGraphics()
		{
			_GUISurface.Clear();
			DrawFinish();
		}

        public override void ClearText()
		{
			GlobalWin.OSD.ClearGUIText();
		}

        public override void DrawText(int x, int y, string message, Color? forecolor = null, Color? backcolor = null, string fontfamily = null)
        {
            using (var g = GetGraphics())
            {
                try
                {
                    var index = 0;
                    if (string.IsNullOrEmpty(fontfamily))
                    {
                        index = _defaultPixelFont;
                    }
                    else
                    {
                        switch (fontfamily)
                        {
                            case "fceux":
                            case "0":
                                index = 0;
                                break;
                            case "gens":
                            case "1":
                                index = 1;
                                break;
                            default:
                                Console.WriteLine($"Unable to find font family: {fontfamily}");
                                return;
                        }
                    }

                    var f = new StringFormat(StringFormat.GenericTypographic)
                    {
                        FormatFlags = StringFormatFlags.MeasureTrailingSpaces
                    };
                    var font = new Font(GlobalWin.DisplayManager.CustomFonts.Families[index], 8, FontStyle.Regular, GraphicsUnit.Pixel);
                    Size sizeOfText = g.MeasureString(message, font, 0, f).ToSize();
                    var rect = new Rectangle(new Point(x, y), sizeOfText + new Size(1, 0));
                    if (backcolor.HasValue) g.FillRectangle(GetBrush(backcolor.Value), rect);
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                    g.DrawString(message, font, GetBrush(forecolor ?? _defaultForeground), x, y);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        public override void Text(int x, int y, string message, Color? forecolor = null, string anchor = null)
		{
			var a = 0;

			if (!string.IsNullOrEmpty(anchor))
			{
				switch (anchor)
				{
					case "0":
					case "topleft":
						a = 0;
						break;
					case "1":
					case "topright":
						a = 1;
						break;
					case "2":
					case "bottomleft":
						a = 2;
						break;
					case "3":
					case "bottomright":
						a = 3;
						break;
				}
			}
			else
			{
				x -= Emulator.CoreComm.ScreenLogicalOffsetX;
				y -= Emulator.CoreComm.ScreenLogicalOffsetY;
			}

			GlobalWin.OSD.AddGUIText(message, x, y, Color.Black, forecolor ?? Color.White, a);
		}
	}
}