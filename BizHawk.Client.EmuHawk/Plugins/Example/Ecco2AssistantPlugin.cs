using System;
using System.Collections.Generic;
using System.Drawing;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class Ecco2AssistantPlugin : PluginBase
	{
		public override string Name => "Ecco 2 Assistant";

		public override string Description => "Displays a hud with hitboxes, etc. Assists with maintaining maximum speed.";

		private enum Modes { disabled, Ecco1, Ecco2 }
		private Modes _mode;

		private int _camX = 0;
		private int _camY = 0;
		private int _camXAddr;
		private int _camYAddr;
		private bool _prevOn = false;
		private uint _prevCharge = 0;
		private uint _prevF = 0;

		void DrawEccoOct(int x, int y, int r, Color? color = null, int fillAlpha = 0)
		{
			Point[] octPoints = {
				new Point(x, y - r), 
				new Point((int)(x + Math.Sin(Math.PI / 4) * r), (int)(y - Math.Sin(Math.PI / 4) *r)), 
				new Point(x + r, y), 
				new Point((int)(x + Math.Sin(Math.PI / 4) * r), (int)(y + Math.Sin(Math.PI / 4) *r)), 
				new Point(x, y + r), 
				new Point((int)(x - Math.Sin(Math.PI / 4) * r), (int)(y + Math.Sin(Math.PI / 4) *r)), 
				new Point(x - r, y), 
				new Point((int)(x - Math.Sin(Math.PI / 4) * r), (int)(y - Math.Sin(Math.PI / 4) *r))
			};
			Color fillColor = color.HasValue ? Color.FromArgb(fillAlpha, color.Value) : Color.Empty;
			_api.GUILib.DrawPolygon(octPoints, color, fillColor);
		}
		void DrawBoxMWH(int x, int y, int w, int h, Color? color = null, int fillAlpha = 0)
		{
			Color fillColor = color.HasValue ? Color.FromArgb(fillAlpha, color.Value) : Color.Empty;
			_api.GUILib.DrawRectangle(x - w, y - h, w << 1, h << 1, color, fillColor);
		}
		void Print_Text(string message, int size, int x, int y, Color color)
		{
			_api.GUILib.DrawText(x, y, message, color, null);
		}
		void PutText(string message, int x, int y, int xl, int yl, int xh, int yh, Color bg, Color fg)
		{
			xl = Math.Max(xl, 0);
			yl = Math.Max(xl, 0);
			xh = Math.Min(xh + 639, 639);
			yh = Math.Min(yh + 441, 441);
			xh -= 4 * message.Length;
			x = x - ((5 * (message.Length - 1)) / 2);
			x = Math.Min(Math.Max(x, Math.Max(xl, 1)), Math.Min(xh, 638 - 4 * (int)message.Length));
			y = Math.Min(Math.Max(y - 3, Math.Max(yl, 1)), yh);
			int[] xOffset = { -1, -1, -1, 0, 1, 1, 1, 0 };
			int[] yOffset = { -1, 0, 1, 1, 1, 0, -1, -1 };
			for (int i = 0; i < 8; i++)
				Print_Text(message, message.Length, x + xOffset[i], y + yOffset[i], bg);
			Print_Text(message, message.Length, x, y, fg);
		}


		void EccoDraw3D()
		{
			int ScreenX = (_api.MemLib.ReadS32(0xFFD5E0) >> 0xC);
			int ScreenY = (_api.MemLib.ReadS32(0xFFD5E8) >> 0xC);
			int ScreenZ = (_api.MemLib.ReadS32(0xFFD5E4) >> 0xB);
			uint curObj = _api.MemLib.ReadU24(0xFFD4C1);
			while (curObj != 0)
			{
				int Xpos = (_api.MemLib.ReadS32(curObj + 0x6) >> 0xC);
				int Ypos = (_api.MemLib.ReadS32(curObj + 0xE) >> 0xC);
				int Zpos = (_api.MemLib.ReadS32(curObj + 0xA) >> 0xB);
				int Y = 224 - (Zpos - ScreenZ);
				int X = (Xpos - ScreenX) + 0xA0;
				uint type = _api.MemLib.ReadU32(curObj + 0x5A);
				short height, width;
				int display = 0;
				if ((type == 0xD817E) || (type == 0xD4AB8))
				{
					Y = 113 - (Ypos - ScreenY);
					height = 0x10;
					if (_api.MemLib.ReadU32(0xFFB166) < 0x1800) height = 0x8;
					short radius = 31;
					if (type == 0xD4AB8)
					{
						radius = 7;
						height = 0x20;
					}
					width = radius;
					DrawEccoOct(X, Y, radius, Color.Lime, 0);
					display = 1;
				}
				else
				{
					width = height = 1;
					if (curObj == 0xFFB134) display = 3;
				}
				if ((display & 1) != 0)
				{
					Y = 224 - (Zpos - ScreenZ);
					DrawBoxMWH(X, Y, width, height, Color.Blue, 0);
				}
				if ((display & 2) != 0)
				{
					Y = 113 - (Ypos - ScreenY);
					DrawBoxMWH(X, Y, width, height, Color.Lime, 0);
				}
				curObj = _api.MemLib.ReadU24(curObj+1);
			}
		}
		void EccoDrawBoxes()
		{
		//	CamX-=8;
			int Width2, Height2;
			//Ecco HP and Air
			int i = 0;
			int HP = _api.MemLib.ReadS16(0xFFAA16) << 3;
			int air = _api.MemLib.ReadS16(0xFFAA18);
			Color color;
            int off = 0;
            for (int j = 0; j < air; j++)
			{
				if (j - off == 448)
				{
					i++; off += 448;
				}
				color = Color.FromArgb(j >> 2, j >> 2, j >> 2);
				_api.GUILib.DrawLine(128, j - off, 144, j - off, color);
			}
			for (i = 0; i < 16; i++)
				for (int j = 0; j < Math.Min(HP, 448); j++)
				{
					color = Color.FromArgb(0, 0, (j>>1 & 0xF0));
					_api.GUILib.DrawPixel(144 + i, j, color);
				}

			//Asterite
			uint curObj = _api.MemLib.ReadU24(0xFFCFC9);
			int Xpos, Xpos2, Ypos, Ypos2, Xmid, Ymid, X, X2, Y, Y2;
			Xmid = 0;
			Ymid = 0;
			while (curObj != 0)
			{
				if (_api.MemLib.ReadU32(curObj + 8) != 0)
				{
					Xpos = _api.MemLib.ReadS16(curObj + 0x3C);
					Xpos2= _api.MemLib.ReadS16(curObj + 0x24);
					Ypos = _api.MemLib.ReadS16(curObj + 0x40);
					Ypos2= _api.MemLib.ReadS16(curObj + 0x28);
					Xpos -= _camX; Xpos2 -= _camX;
					Ypos -= _camY; Ypos2 -= _camY;
					Xmid = (Xpos + Xpos2) >> 1;
					Ymid = (Ypos + Ypos2) >> 1;
					if (_api.MemLib.ReadU8(curObj + 0x71) != 0)
					{
						DrawEccoOct(Xpos, Ypos, 40, Color.FromArgb(255, 192, 0), 0x7F);
						DrawEccoOct(Xpos2, Ypos2, 40, Color.FromArgb(255, 192, 0), 0x7F);
					}
				}
				else 
				{
					Xmid = _api.MemLib.ReadS16(curObj + 0x24) - _camX;
					Ymid = _api.MemLib.ReadS16(curObj + 0x28) - _camY;
				}
				Print_Text(curObj.ToString("X8"), 8, Math.Min(Math.Max(0, Xmid-16), 605), Math.Min(Math.Max(0, Ymid-5), 424), Color.Lime);
				Print_Text(curObj.ToString("X8"), 8, Math.Min(Math.Max(2, Xmid-14), 607), Math.Min(Math.Max(2, Ymid-3), 426), Color.Lime);
				Print_Text(curObj.ToString("X8"), 8, Math.Min(Math.Max(0, Xmid-16), 605), Math.Min(Math.Max(2, Ymid-3), 426), Color.Lime);
				Print_Text(curObj.ToString("X8"), 8, Math.Min(Math.Max(2, Xmid-14), 607), Math.Min(Math.Max(0, Ymid-5), 424), Color.Lime);
				Print_Text(curObj.ToString("X8"), 8, Math.Min(Math.Max(1, Xmid-15), 606), Math.Min(Math.Max(1, Ymid-4), 425), Color.Blue);
				curObj = _api.MemLib.ReadU24(curObj+1);
			}
			uint curlev = _api.MemLib.ReadU8(0xFFA7E0);
			if ((_api.MemLib.ReadU8(0xFFA7D0) == 30))
			{
				curObj = _api.MemLib.ReadU24(0xFFD425);
				if ((curObj != 0) && (_api.MemLib.ReadU32(curObj + 8) != 0))
				{
					Xpos = _api.MemLib.ReadS16(curObj + 0x1C) - _camX;
					Ypos = _api.MemLib.ReadS16(curObj + 0x20) - _camY;
					DrawEccoOct(Xpos, Ypos, 20, Color.FromArgb(255, 192, 0));
				}
			}
			//aqua tubes
			curObj = _api.MemLib.ReadU24(0xFFCFC5);
			while (curObj != 0)
			{
				Xpos = _api.MemLib.ReadS16(curObj + 0x2C);
				Xpos2= _api.MemLib.ReadS16(curObj + 0x34);
				Ypos = _api.MemLib.ReadS16(curObj + 0x30);
				Ypos2= _api.MemLib.ReadS16(curObj + 0x38);
				Xpos -= _camX; Xpos2 -= _camX;
				Ypos -= _camY; Ypos2 -= _camY;
				Xmid = (Xpos + Xpos2) >> 1;
				Ymid = (Ypos + Ypos2) >> 1;
		//		displayed = false;
				uint type = _api.MemLib.ReadU8(curObj + 0x7E);
				int yoff = 0;
				switch (type)
				{
		/*			case 0x11:
						Xpos2 = Xmid;
						Xmid = (Xpos + Xpos2) >> 1;
						break;
					case 0x12:
						Xpos = Xmid;
						Xmid = (Xpos + Xpos2) >> 1;
						break;
					case 0x13:
						Ypos = Ymid;
						Ymid = (Ypos + Ypos2) >> 1;
						break;
					case 0x14:
						Ypos2 = Ymid;
						Ymid = (Ypos + Ypos2) >> 1;
						break;*/
					case 0x15:
						for (int TempX = 0; TempX <= Xmid-Xpos; TempX++, yoff++)
							_api.GUILib.DrawPixel(Xpos2 - TempX, Ymid + yoff, Color.FromArgb(127, 0, 255));
						for (int TempX = Math.Min(Math.Max(0, Xmid), 320); TempX <= Math.Min(Math.Max(8, Xpos2), 327); TempX++)
							_api.GUILib.DrawPixel(TempX, Ymid, Color.FromArgb(127, 0, 255));
						for (uint TempX = (uint)Math.Min(Math.Max(0, Ymid), 223); TempX <= Math.Min(Math.Max(0, Ypos2), 223); TempX++)
							_api.GUILib.DrawPixel(Xmid, (int)TempX, Color.FromArgb(127, 0, 255));
						break;
					case 0x18:
					case 0x19:
						for (int TempX = 0; TempX <= Ymid-Ypos; TempX++)
						{
							_api.GUILib.DrawPixel(Xmid + yoff, Ypos2-TempX, Color.FromArgb(127, 0, 255));
							if ((TempX & 1) != 0) yoff++;
						}
						break;
					case 0x1A:
						for (int TempX = 0; TempX <= Xmid-Xpos; TempX++, yoff++)
							_api.GUILib.DrawPixel(Xpos + TempX, Ymid + yoff, Color.FromArgb(127, 0, 255));
						for (int TempX = Math.Min(Math.Max(0, Xpos), 320); TempX <= Math.Min(Math.Max(8, Xmid), 327); TempX++)
							_api.GUILib.DrawPixel(TempX, Ymid, Color.FromArgb(127, 0, 255));
						for (uint TempX = (uint)Math.Min(Math.Max(0, Ymid), 223); TempX <= Math.Min(Math.Max(0, Ypos2), 223); TempX++)
							_api.GUILib.DrawPixel(Xmid, (int)TempX, Color.FromArgb(127, 0, 255));
						break;
					case 0x1D:
						for (int TempX = 0; TempX <= Ymid-Ypos; TempX++)
						{
							_api.GUILib.DrawPixel(Xmid - yoff, Ypos2 - TempX, Color.FromArgb(127, 0, 255));
							if ((TempX & 1) != 0) yoff++;
						}
						break;
					case 0x1F:
						for (int TempX = 0; TempX <= Xmid-Xpos; TempX++, yoff++)
							_api.GUILib.DrawPixel(Xpos + TempX, Ymid - yoff, Color.FromArgb(127, 0, 255));
						for (int TempX = Math.Min(Math.Max(0, Xpos), 320); TempX <= Math.Min(Math.Max(8, Xmid), 327); TempX++)
							_api.GUILib.DrawPixel(TempX, Ymid, Color.FromArgb(127, 0, 255));
						for (uint TempX = (uint)Math.Min(Math.Max(0, Ypos), 223); TempX <= Math.Min(Math.Max(0, Ymid), 223); TempX++)
							_api.GUILib.DrawPixel(Xmid, (int)TempX, Color.FromArgb(127, 0, 255));
						break;
					case 0x20:
					case 0x21:
						for (int TempX = 0; TempX <= Xmid-Xpos; TempX++)
						{
							_api.GUILib.DrawPixel(Xpos + TempX, Ymid - yoff, Color.FromArgb(127, 0, 255));
							if ((TempX & 1) != 0) yoff++;
						}
						break;
					case 0x22:
					case 0x23:
						for (int TempX = 0; TempX <= Ymid-Ypos; TempX++)
						{
							_api.GUILib.DrawPixel(Xmid - yoff, Ypos + TempX, Color.FromArgb(127, 0, 255));
							if ((TempX & 1) != 0) yoff++;
						}
						break;
					case 0x24:
						for (int TempX = 0; TempX <= Xmid-Xpos; TempX++, yoff++)
							_api.GUILib.DrawPixel(Xpos2 - TempX, Ymid - yoff, Color.FromArgb(127, 0, 255));
						break;
					case 0x25:
					case 0x26:
						for (int TempX = 0; TempX <= Xmid-Xpos; TempX++)
						{
							_api.GUILib.DrawPixel(Xpos2 - TempX, Ymid - yoff, Color.FromArgb(127, 0, 255));
							if ((TempX & 1) != 0) yoff++;
						}
						break;
					case 0x27:
					case 0x28:
						for (int TempX = 0; TempX <= Ymid-Ypos; TempX++)
						{
							_api.GUILib.DrawPixel(Xmid + yoff, Ypos + TempX, Color.FromArgb(127, 0, 255));
							if ((TempX & 1) != 0) yoff++;
						}
						break;
					case 0x2B:
                        _api.GUILib.DrawLine(Xpos, Ymid, Xpos2, Ymid, Color.FromArgb(127, 0, 255));
                        for (int TempX = 0; TempX <= Ymid - Ypos; TempX++)
                        {
                            _api.GUILib.DrawPixel(Xpos + yoff, Ymid - TempX, Color.FromArgb(127, 0, 255));
                            _api.GUILib.DrawPixel(Xpos2 - yoff, Ymid - TempX, Color.FromArgb(127, 0, 255));
                            if ((TempX & 1) != 0) yoff++;
                        }
                        yoff = Xmid - (Xpos + yoff);
                        _api.GUILib.DrawLine(Xmid - yoff, Ypos, Xmid + yoff, Ypos, Color.FromArgb(127, 0, 255));
						break;
					default:
						_api.GUILib.DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.FromArgb(255, 127, 0, 255));
						break;
				}
		//		if (!displayed)
				if (type != 0x10)
				{
					uint temp = _api.MemLib.ReadU8(curObj + 0x7E);
					Print_Text(temp.ToString("%02X"), 2, Math.Min(Math.Max(0, Xmid-3), 628), Math.Min(Math.Max(0, Ymid-5), 424), Color.Red);
					Print_Text(temp.ToString("%02X"), 2, Math.Min(Math.Max(2, Xmid-1), 630), Math.Min(Math.Max(2, Ymid-3), 426), Color.Red);
					Print_Text(temp.ToString("%02X"), 2, Math.Min(Math.Max(0, Xmid-3), 628), Math.Min(Math.Max(2, Ymid-3), 426), Color.Red);
					Print_Text(temp.ToString("%02X"), 2, Math.Min(Math.Max(2, Xmid-1), 630), Math.Min(Math.Max(0, Ymid-5), 424), Color.Red);
					Print_Text(temp.ToString("%02X"), 2, Math.Min(Math.Max(1, Xmid-2), 629), Math.Min(Math.Max(1, Ymid-4), 425), Color.Blue);
				}
				curObj = _api.MemLib.ReadU24(curObj+1);
			}
			//walls
			bool displayed;
			curObj = _api.MemLib.ReadU24(0xFFCFC1);
			while (curObj != 0)
			{

				Xpos = _api.MemLib.ReadS16(curObj + 0x2C);
				Xpos2= _api.MemLib.ReadS16(curObj + 0x34);
				Ypos = _api.MemLib.ReadS16(curObj + 0x30);
				Ypos2= _api.MemLib.ReadS16(curObj + 0x38);
				Xpos -= _camX; Xpos2 -= _camX;
				Ypos -= _camY; Ypos2 -= _camY;
				Xmid = (Xpos + Xpos2) >> 1;
				Ymid = (Ypos + Ypos2) >> 1;
				displayed = false;
				uint type = _api.MemLib.ReadU8(curObj + 0x7E);
				int yoff = 0;
				switch (type)
				{
					case 0x11:
						Xpos2 = Xmid;
						Xmid = (Xpos + Xpos2) >> 1;
						break;
					case 0x12:
						Xpos = Xmid;
						Xmid = (Xpos + Xpos2) >> 1;
						break;
					case 0x13:
						Ypos = Ymid;
						Ymid = (Ypos + Ypos2) >> 1;
						break;
					case 0x14:
						Ypos2 = Ymid;
						Ymid = (Ypos + Ypos2) >> 1;
						break;
					case 0x15:
					case 0x16:
					case 0x17:
					case 0x18:
					case 0x19:
						_api.GUILib.DrawLine(Xmid, Ypos2, Xpos2, Ymid, Color.White);
                        _api.GUILib.DrawLine(Xmid, Ypos2,  Xmid, Ymid, Color.FromArgb(127,Color.White));
                        _api.GUILib.DrawLine(Xmid,  Ymid, Xpos2, Ymid, Color.FromArgb(127, Color.White));
                        displayed = true;
						break;
					case 0x1A:
					case 0x1B:
					case 0x1C:
					case 0x1D:
					case 0x1E:
						_api.GUILib.DrawLine(Xpos, Ymid, Xmid, Ypos2, Color.White);
                        _api.GUILib.DrawLine(Xmid, Ymid, Xmid, Ypos2, Color.FromArgb(127, Color.White));
                        _api.GUILib.DrawLine(Xpos, Ymid, Xmid, Ymid, Color.FromArgb(127, Color.White));
                        displayed = true;
						break;
					case 0x1F:
					case 0x20:
					case 0x21:
					case 0x22:
					case 0x23:
						_api.GUILib.DrawLine(Xpos, Ymid, Xmid, Ypos, Color.White);
                        _api.GUILib.DrawLine(Xmid, Ymid, Xmid, Ypos, Color.FromArgb(127, Color.White));
                        _api.GUILib.DrawLine(Xpos, Ymid, Xmid, Ymid, Color.FromArgb(127, Color.White));
                        displayed = true;
						break;
					case 0x24:
					case 0x25:
					case 0x26:
					case 0x27:
					case 0x28:
						_api.GUILib.DrawLine(Xmid, Ypos, Xpos2, Ymid, Color.White);
                        _api.GUILib.DrawLine(Xmid, Ypos,  Xmid, Ymid, Color.FromArgb(127, Color.White));
                        _api.GUILib.DrawLine(Xmid, Ymid, Xpos2, Ymid, Color.FromArgb(127, Color.White));
                        displayed = true;
						break;
					case 0x2B:
                        _api.GUILib.DrawLine(Xpos, Ymid, Xpos2, Ymid, Color.FromArgb(127, Color.White));
						for (int TempX = 0; TempX <= Ymid-Ypos; TempX++)
						{
							_api.GUILib.DrawPixel(Xpos + yoff, Ymid - TempX, Color.White);
							_api.GUILib.DrawPixel(Xpos2 - yoff, Ymid - TempX, Color.White);
							if ((TempX & 1) != 0) yoff++;
						}
						yoff = Xmid - (Xpos + yoff);
                        _api.GUILib.DrawLine(Xmid - yoff, Ypos, Xmid + yoff, Ypos);
						displayed = true;
						break;
					default:
						if (type != 0x10)
						{
							var temp = _api.MemLib.ReadU8(curObj + 0x7E);
							Print_Text(temp.ToString("%02X"), 2, Math.Min(Math.Max(0, Xmid-3), 628), Math.Min(Math.Max(0, Ymid-5), 424), Color.Red);
							Print_Text(temp.ToString("%02X"), 2, Math.Min(Math.Max(2, Xmid-1), 630), Math.Min(Math.Max(2, Ymid-3), 426), Color.Red);
							Print_Text(temp.ToString("%02X"), 2, Math.Min(Math.Max(0, Xmid-3), 628), Math.Min(Math.Max(2, Ymid-3), 426), Color.Red);
							Print_Text(temp.ToString("%02X"), 2, Math.Min(Math.Max(2, Xmid-1), 630), Math.Min(Math.Max(0, Ymid-5), 424), Color.Red);
							Print_Text(temp.ToString("%02X"), 2, Math.Min(Math.Max(1, Xmid-2), 629), Math.Min(Math.Max(1, Ymid-4), 425), Color.Lime);
						}
						break;
				}
				if (!displayed) _api.GUILib.DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.White);
				curObj = _api.MemLib.ReadU24(curObj+1);
			}
			//inanimate objects
			curObj = _api.MemLib.ReadU24(0xFFCFBD);
			while (curObj != 0)
			{
				if (_api.MemLib.ReadU8(curObj + 0x7E) > 0xF);
				{
					Xpos = _api.MemLib.ReadS16(curObj + 0x2C);
					Xpos2= _api.MemLib.ReadS16(curObj + 0x34);
					Ypos = _api.MemLib.ReadS16(curObj + 0x30);
					Ypos2= _api.MemLib.ReadS16(curObj + 0x38);
					Xpos -= _camX; Xpos2 -= _camX;
					Ypos -= _camY; Ypos2 -= _camY;
                    _api.GUILib.DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue);
                }
                Xpos += Xpos2;
				Ypos += Ypos2;
				Xpos >>= 1;
				Ypos >>= 1;
				Print_Text(curObj.ToString("X8"), 8, Math.Min(Math.Max(0, Xpos-16), 605), Math.Min(Math.Max(0, Ypos-5), 424), Color.Lime);
				Print_Text(curObj.ToString("X8"), 8, Math.Min(Math.Max(2, Xpos-14), 607), Math.Min(Math.Max(2, Ypos-3), 426), Color.Lime);
				Print_Text(curObj.ToString("X8"), 8, Math.Min(Math.Max(0, Xpos-16), 605), Math.Min(Math.Max(2, Ypos-3), 426), Color.Lime);
				Print_Text(curObj.ToString("X8"), 8, Math.Min(Math.Max(2, Xpos-14), 607), Math.Min(Math.Max(0, Ypos-5), 424), Color.Lime);
				Print_Text(curObj.ToString("X8"), 8, Math.Min(Math.Max(1, Xpos-15), 606), Math.Min(Math.Max(1, Ypos-4), 425), Color.Blue);
                curObj = _api.MemLib.ReadU24(curObj+1);
			}
			//animate objects
			if (_mode == Modes.Ecco2)
				curObj = _api.MemLib.ReadU24(0xFFCFB9);
			else
				curObj = _api.MemLib.ReadU24(0xFFD829); 
			while (curObj != 0)
			{
				uint type = 0;
				switch (_mode) {
					case Modes.Ecco2:
					{
						uint flags = _api.MemLib.ReadU16(curObj + 0x10);
						//if ((flags & 0x2000) || !(flags & 2));
						type = _api.MemLib.ReadU32(curObj + 0xC);
						if ((type == 0xBA52E) || (type == 0xBA66E))
						{
							uint Adelikat = curObj;
							while (Adelikat != 0)
							{
								Xpos = _api.MemLib.ReadS16(Adelikat + 0x24);
								Ypos = _api.MemLib.ReadS16(Adelikat + 0x28);
								Xpos -= _camX;
								Ypos -= _camY;
								DrawEccoOct(Xpos, Ypos, _api.MemLib.ReadS16(Adelikat + 0x44), Color.Lime);
								Adelikat = _api.MemLib.ReadU32(Adelikat + 4);
							}
							Xpos = _api.MemLib.ReadS16(curObj + 0x24);
							Ypos = _api.MemLib.ReadS16(curObj + 0x28);
							Xpos -= _camX;
							Ypos -= _camY;
						}
						else if (type == 0xE47EE)
						{
							uint Adelikat = curObj;
							while (Adelikat != 0)
							{
								Xpos = _api.MemLib.ReadS16(Adelikat + 0x1C);
								Ypos = _api.MemLib.ReadS16(Adelikat + 0x20);
								Xpos -= _camX;
								Ypos -= _camY;
								DrawEccoOct(Xpos, Ypos, (_api.MemLib.ReadS16(Adelikat + 0x2C) >> 1) + 16, Color.Lime);
								Adelikat = _api.MemLib.ReadU32(Adelikat + 4);
							}
							Xpos = _api.MemLib.ReadS16(curObj + 0x24);
							Ypos = _api.MemLib.ReadS16(curObj + 0x28);
							Xpos -= _camX;
							Ypos -= _camY;
						}
						else if ((type == 0x9F5B0) || (type == 0xA3B18))
						{
							Xpos = _api.MemLib.ReadS16(curObj + 0x24);
							Ypos = _api.MemLib.ReadS16(curObj + 0x28);
							Xpos -= _camX;
							Ypos -= _camY;
							DrawEccoOct(Xpos, Ypos, _api.MemLib.ReadS16(curObj + 0x44), Color.Lime);
						}
						else if (type == 0xDCEE0)
						{
							Xpos = _api.MemLib.ReadS16(curObj + 0x24);
							Ypos = _api.MemLib.ReadS16(curObj + 0x28);
							Xpos -= _camX; Ypos -= _camY;
							DrawEccoOct(Xpos, Ypos, 0x5C, Color.Lime);
						}
						else
						{
							Xpos = _api.MemLib.ReadS16(curObj + 0x2C);
							Xpos2 = _api.MemLib.ReadS16(curObj + 0x34);
							Ypos = _api.MemLib.ReadS16(curObj + 0x30);
							Ypos2 = _api.MemLib.ReadS16(curObj + 0x38);
							Xmid = _api.MemLib.ReadS16(curObj + 0x24);
							Ymid = _api.MemLib.ReadS16(curObj + 0x28);
							Xpos -= _camX; Xpos2 -= _camX;
							Ypos -= _camY; Ypos2 -= _camY;
							Xmid -= _camX; Ymid -= _camY;
							if ((type == 0xA6C4A) || (type == 0xC43D4)) DrawEccoOct(Xmid, Ymid, 70, Color.Lime);
							_api.GUILib.DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Lime);
						}
						break;
					}
					case Modes.Ecco1:
						type = _api.MemLib.ReadU32(curObj + 0x6);
						Xpos = _api.MemLib.ReadS16(curObj + 0x17);
						Xpos2 = _api.MemLib.ReadS16(curObj + 0x1F);
						Ypos = _api.MemLib.ReadS16(curObj + 0x1B);
						Ypos2 = _api.MemLib.ReadS16(curObj + 0x23);
						Xmid = _api.MemLib.ReadS16(curObj + 0x0F);
						Ymid = _api.MemLib.ReadS16(curObj + 0x13);
						Xpos >>= 2;
						Xpos2 >>= 2;
						Ypos >>= 2;
						Ypos2 >>= 2;
						Xmid >>= 2;
						Ymid >>= 2;
						Xpos -= _camX; Xpos2 -= _camX;
						Ypos -= _camY; Ypos2 -= _camY;
						Xmid -= _camX; Ymid -= _camY;
                        _api.GUILib.DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Lime);
                        break;
				}
				Print_Text(type.ToString("X8"), 8, Math.Min(Math.Max(0, Xmid-16), 605), Math.Min(Math.Max(0, Ymid-5), 424), Color.Blue);
				Print_Text(type.ToString("X8"), 8, Math.Min(Math.Max(2, Xmid-14), 607), Math.Min(Math.Max(2, Ymid-3), 426), Color.Blue);
				Print_Text(type.ToString("X8"), 8, Math.Min(Math.Max(0, Xmid-16), 605), Math.Min(Math.Max(2, Ymid-3), 426), Color.Blue);
				Print_Text(type.ToString("X8"), 8, Math.Min(Math.Max(2, Xmid-14), 607), Math.Min(Math.Max(0, Ymid-5), 424), Color.Blue);
				Print_Text(type.ToString("X8"), 8, Math.Min(Math.Max(1, Xmid-15), 606), Math.Min(Math.Max(1, Ymid-4), 425), Color.Red);
                curObj = _api.MemLib.ReadU24(curObj+1);
			}
			//events
			curObj = _api.MemLib.ReadU24(0xFFCFB5);
			while (curObj != 0)
			{
				uint type = _api.MemLib.ReadU32(curObj + 0xC);
				if ((type == 0xA44EE) || (type == 0xD120C))
				{
					Xmid = _api.MemLib.ReadS16(curObj + 0x1C) - _camX;
					Ymid = _api.MemLib.ReadS16(curObj + 0x20) - _camY;
					DrawEccoOct(Xmid, Ymid, 0x20, Color.Cyan);
				}
				else if (type == 0xDEF94)
				{
					Xmid = _api.MemLib.ReadS16(curObj + 0x24) - _camX;
					Ymid = _api.MemLib.ReadS16(curObj + 0x28) - _camY;
					DrawEccoOct(Xmid, Ymid, 0x18, Color.Cyan);
				}
				else 
				{
					Xpos = _api.MemLib.ReadS16(curObj + 0x2C);
					Xpos2= _api.MemLib.ReadS16(curObj + 0x34);
					Ypos = _api.MemLib.ReadS16(curObj + 0x30);
					Ypos2= _api.MemLib.ReadS16(curObj + 0x38);
					Xmid = _api.MemLib.ReadS16(curObj + 0x24);
					Ymid = _api.MemLib.ReadS16(curObj + 0x28);
					Xpos -= _camX; Xpos2 -= _camX;
					Ypos -= _camY; Ypos2 -= _camY;
					Xmid -= _camX; Ymid -= _camY;
                    _api.GUILib.DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Cyan);
                }
                PutText(type.ToString("X8"), Xmid, Ymid, 1, 1, 0, 0, Color.White, Color.Blue);
				PutText(curObj.ToString("X8"), Xmid, Ymid, 1, 9, 0, 0, Color.White, Color.Blue);
                curObj = _api.MemLib.ReadU24(curObj+1);
			}
			//Ecco body
			Xpos = _api.MemLib.ReadS16(0xFFAA22);
			Ypos = _api.MemLib.ReadS16(0xFFAA26);
			Xpos2 = _api.MemLib.ReadS16(0xFFAA2A);
			Ypos2 = _api.MemLib.ReadS16(0xFFAA2E);
			Xmid = _api.MemLib.ReadS16(0xFFAA1A);
			Ymid = _api.MemLib.ReadS16(0xFFAA1E);
			Xpos -= _camX; Xpos2 -= _camX;
			Ypos -= _camY; Ypos2 -= _camY;
			Xmid -= _camX; Ymid -= _camY;
			X = Xpos;
			X2 = Xpos2;
			Y = Ypos;
			Y2 = Ypos2;
			int X3 = (Xmid + (ushort) Xpos) >> 1;
			int X4 = (Xmid + (ushort) Xpos2) >> 1;
			int Y3 = (Ymid + (ushort) Ypos) >> 1;
			int Y4 = (Ymid + (ushort) Ypos2) >> 1;
			DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Magenta);
			DrawBoxMWH(X, Y, 1, 1, Color.Magenta);
			DrawBoxMWH(X2, Y2, 1, 1, Color.Magenta);
			DrawBoxMWH(X3, Y3, 1, 1, Color.Magenta);
			DrawBoxMWH(X4, Y4, 1, 1, Color.Magenta);
			_api.GUILib.DrawPixel(Xmid, Ymid, Color.Blue);
			_api.GUILib.DrawPixel(X, Y, Color.Blue);
			_api.GUILib.DrawPixel(X2, Y2, Color.Blue);
			_api.GUILib.DrawPixel(X3, Y3, Color.Blue);
			_api.GUILib.DrawPixel(X4, Y4, Color.Blue);
			_api.GUILib.DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue);
			//Ecco head
			Xpos = _api.MemLib.ReadS16(0xFFA8F8);
			Xpos2 = _api.MemLib.ReadS16(0xFFA900);
			Ypos = _api.MemLib.ReadS16(0xFFA8FC);
			Ypos2 = _api.MemLib.ReadS16(0xFFA904);
			Xpos -= _camX; Xpos2 -= _camX;
			Ypos -= _camY; Ypos2 -= _camY;
			_api.GUILib.DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.White);
			//Ecco tail
			Xpos = _api.MemLib.ReadS16(0xFFA978);
			Xpos2 = _api.MemLib.ReadS16(0xFFA980);
			Ypos = _api.MemLib.ReadS16(0xFFA97C);
			Ypos2 = _api.MemLib.ReadS16(0xFFA984);
			Xpos -= _camX; Xpos2 -= _camX;
			Ypos -= _camY; Ypos2 -= _camY;
			_api.GUILib.DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.White);
			// sonar
			if (_api.MemLib.ReadU8(0xFFAB77) != 0)
			{
				Xmid = _api.MemLib.ReadS16(0xFFA9EC);
				Width2 = _api.MemLib.ReadS16(0xFFA9FC);
				Xmid -= _camX;
				Ymid = _api.MemLib.ReadS16(0xFFA9F0);
				Ymid -= _camY;
				Height2 = _api.MemLib.ReadS16(0xFFAA00);
				color = ((_api.MemLib.ReadU8(0xFFAA0C) != 0) ? Color.FromArgb(255, 0, 127) : Color.FromArgb(0, 0, 255));
				DrawBoxMWH(Xmid, Ymid, Width2, Height2, color);
				DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue);
				_api.GUILib.DrawPixel(Xmid, Ymid, color);
			}
			//Pulsar
			curObj = _api.MemLib.ReadU24(0xFFCFD1);
			if (curObj != 0)
			{
		//		sbyte Blah = _api.MemLib.ReadU8(CardBoard + 0x15);
				curObj += 0x26;
		//		if (!(Blah & 1))
		//			CardBoard += 0x14;
				for (int l = 0; l < 4; l++)
				{
					if (_api.MemLib.ReadU16(curObj + 0x12) != 0)
					{
						Xmid = _api.MemLib.ReadS16(curObj);
						Ymid = _api.MemLib.ReadS16(curObj + 4);
						Xmid -= _camX; Ymid -= _camY;
						DrawBoxMWH(Xmid, Ymid, 0x30, 0x30, Color.Red);
						DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue);
						_api.GUILib.DrawPixel(Xmid, Ymid, Color.Red);
					}
					curObj += 0x14;
				}
			}
		}

		void EccoAutofire(bool on)
		{
			//Modif N - ECCO HACK - make caps lock (weirdly) autofire player 1's C key
			uint charge;
			uint mode = _api.MemLib.ReadU8(0xFFA555);
			int frameCount = _api.EmuLib.FrameCount();
			int lagCount = _api.EmuLib.LagCount();
			switch (mode)
			{
				case 0x00:
					if (on)
					{
						if (_api.MemLib.ReadU16(0xFFF342) == 0xFFFD)
							_api.JoypadLib.Set("C", false, 1);
						else
							_api.JoypadLib.Set("C", true, 1);
					}
					break;
				case 0xE6:
					if (_api.MemLib.ReadU16(0xFFD5E8) == 0x00000002) {
						Dictionary<string, bool> buttons = new Dictionary<string, bool>();
						buttons["B"] = !(buttons["C"] = false);
						_api.JoypadLib.Set(buttons, 1);
					}
					else
					{
						Dictionary<string, bool> buttons = new Dictionary<string, bool>();
						buttons["B"] = !(buttons["C"] = true);
						_api.JoypadLib.Set(buttons, 1);
					}
					break;
				case 0xF6:
					charge = _api.MemLib.ReadU8(0xFFB19B);
					if (on)
					{
						if ((charge == 1) || (_prevCharge == 1) || !(_prevOn || (_api.MemLib.ReadU8(0xFFB19B) != 0)))
							_api.JoypadLib.Set("B", true, 1);
						else
							_api.JoypadLib.Set("B", false, 1);
						if ((_api.MemLib.ReadU16(0xFFB168) == 0x3800) && ((_api.MemLib.ReadU16(0xFFA7C8) % 2) != 0))
							_api.EmuLib.SetIsLagged(true);
						_api.JoypadLib.Set("C", (_api.MemLib.ReadU16(0xFFA7C8) % 2) != 0, 1);
					}
					_prevCharge = charge;
					break;
				case 0x20:
				case 0x28:
				case 0xAC:
					if (on)
					{
						_api.JoypadLib.Set("C", (_api.MemLib.ReadS8(0xFFAA6E) >= 11), 1);
					}
					break;
				default:
					break;
			}
			_prevOn = on;
		}
		public override void Init(IPluginAPI api) 
		{
			base.Init(api);
			_api.MemLib.SetBigEndian();
			string gameName = _api.GameInfoLib.GetRomName();
			if ((gameName == "ECCO - The Tides of Time (J) [!]") ||
				(gameName == "ECCO - The Tides of Time (U) [!]") ||
				(gameName == "ECCO - The Tides of Time (E) [!]"))
			{
				_mode = Modes.Ecco2;
				_camXAddr = 0xFFAD9C;
				_camYAddr = 0xFFAD9E;
                EmuHawkPluginLibrary.SetGameExtraPadding(160, 112, 160, 112);
            }
			else if ((gameName == "ECCO The Dolphin (J) [!]") ||
					 (gameName == "ECCO The Dolphin (UE) [!]"))

			{
				_mode = Modes.Ecco1;
				_camXAddr = 0xFFB836;
				_camYAddr = 0xFFB834;
				EmuHawkPluginLibrary.SetGameExtraPadding(160,112,160,112);
			}
			else
			{
				_mode = Modes.disabled;
				Running = false;
			}
		}
		public override void PreFrameCallback()
		{
			if (_mode != Modes.disabled)
			{
				EccoAutofire(_api.JoypadLib.Get(1)["C"] != _api.JoypadLib.GetImmediate()["P1 C"]);
			}
		}
		public override void PostFrameCallback()
		{
			uint frame = _api.MemLib.ReadU32(0xFFA524);
			uint mode = _api.MemLib.ReadByte(0xFFA555);
			switch (mode) {
				case 0x20:
				case 0x28:
				case 0xAC:
                    EmuHawkPluginLibrary.SetGameExtraPadding(160, 112, 160, 112);
                    EccoDrawBoxes();
					break;
				case 0xF6:
                    EmuHawkPluginLibrary.SetGameExtraPadding(0, 0, 0, 0);
					EccoDraw3D();
					break;
				default:
                    EmuHawkPluginLibrary.SetGameExtraPadding(0, 0, 0, 0);
                    break;
			}
			_camX = _api.MemLib.ReadS16(_camXAddr)-160;
			_camY = _api.MemLib.ReadS16(_camYAddr)-112;
			if (frame <= _prevF)
				_api.EmuLib.SetIsLagged(true);
			_prevF = frame;
		}
		public override void LoadStateCallback(string name)
		{
			_prevF = _api.MemLib.ReadU32(0xFFA524);
		}
	}
}
