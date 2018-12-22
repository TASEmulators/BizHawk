using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;

using System.Linq;
using BizHawk.Client.ApiHawk;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;

namespace BizHawk.Client.EmuHawk
{
	public sealed class Ecco2AssistantPlugin : PluginBase
	{
		[RequiredApi]
		private IMem Mem {get; set;}

		[RequiredApi]
		private IGui Gui { get; set; }

		[RequiredApi]
		private IJoypad Joy { get; set; }

		[RequiredApi]
		private IEmu Emu { get; set; }

		[RequiredApi]
		private IGameInfo GI { get; set; }

		[RequiredApi]
		private IMemorySaveState MemSS { get; set; }

		public override string Name => "Ecco 2 Assistant";

		public override string Description => "Displays a hud with hitboxes, etc. Assists with maintaining maximum speed.";

		private enum Modes { disabled, Ecco1, Ecco2 }
		private Modes _mode;

		private int _camX = 0;
		private int _camY = 0;
		private int _camXAddr;
		private int _camYAddr;
		private uint _prevF = 0;
		private int _top = 0;
		private int _bottom = 0;
		private int _left = 0;
		private int _right = 0;
		private const int _signalAlpha = 255;
		private int _tickerY = 81;
		private int _dumpMap = 0;
		private int _prevX = 0;
		private int _prevY = 0;
		private int _destX = 0;
		private int _destY = 0;
		private int _snapPast = 0;
		private string _rowStateGuid = string.Empty;
		private Color[] _turnSignalColors =
		{
			Color.FromArgb(_signalAlpha, 127, 127,   0),
			Color.FromArgb(_signalAlpha, 255,   0,   0),
			Color.FromArgb(_signalAlpha, 192,   0,  63),
			Color.FromArgb(_signalAlpha,  63,   0, 192),
			Color.FromArgb(_signalAlpha,   0,   0, 255),
			Color.FromArgb(_signalAlpha,   0,  63, 192),
			Color.FromArgb(_signalAlpha,   0, 192,  63),
			Color.FromArgb(_signalAlpha,   0, 255,   0)
		};
		private int _rseed = 1;
		private int EccoRand(bool refresh = false)
		{
			if (refresh)
			{
				_rseed = (int)(Mem.ReadU16(0xFFE2F8));
			}
			bool odd = (_rseed & 1) != 0;
			_rseed >>= 1;
			if (odd)
			{
				_rseed ^= 0xB400;
			}
			return _rseed;
		}
		private void DrawEccoRhomb(int x, int y, int radius, Color color, int fillAlpha = 63)
		{
			Point[] rhombus = {
				new Point(x, y - radius),
				new Point(x + radius, y),
				new Point(x, y + radius),
				new Point(x - radius, y)
			};
			Color? fillColor = null;
			if (fillAlpha > 0) fillColor = Color.FromArgb(fillAlpha, color);
			Gui.DrawPolygon(rhombus, color, fillColor);
		}
		private void DrawEccoRhomb_scaled(int x, int y, int width, int height, int rscale, int bscale, int lscale, int tscale, Color color, int fillAlpha = 63)
		{
			Point[] rhombus = {
				new Point(x + (width << rscale), y),
				new Point(x, y + (height << bscale)),
				new Point(x - (width << lscale), y),
				new Point(x, y - (height << tscale))
			};
			Color? fillColor = null;
			if (fillAlpha > 0) fillColor = Color.FromArgb(fillAlpha, color);
			Gui.DrawPolygon(rhombus, color, fillColor);
		}
		private void DrawEccoOct(int x, int y, int r, Color color, int fillAlpha = 63)
		{
			var octOff = (int)(Math.Sqrt(2) * r) >> 1;
			Point[] octagon = {
				new Point(x, y - r), 
				new Point(x + octOff, y - octOff), 
				new Point(x + r, y), 
				new Point(x + octOff, y + octOff), 
				new Point(x, y + r), 
				new Point(x - octOff, y + octOff), 
				new Point(x - r, y), 
				new Point(x - octOff, y - octOff)
			};
			Color? fillColor = null;
			if (fillAlpha > 0) fillColor = Color.FromArgb(fillAlpha, color);
			Gui.DrawPolygon(octagon, color, fillColor);
		}
		private void DrawEccoOct_scaled(int x, int y, int xscale, int yscale, int r, Color color, int fillAlpha = 63)
		{
			var octOff = (int)(Math.Sin(Math.PI / 4) * r);
			var xoctOff = octOff << xscale;
			var yoctOff = octOff << yscale;
			var xr = r << xscale;
			var yr = r << yscale;
			Point[] octagon = {
				new Point(x, y - yr),
				new Point(x + xoctOff, y - yoctOff),
				new Point(x + xr, y),
				new Point(x + xoctOff, y + yoctOff),
				new Point(x, y + yr),
				new Point(x - xoctOff, y + yoctOff),
				new Point(x - xr, y),
				new Point(x - xoctOff, y - yoctOff)
			};
			Color? fillColor = null;
			if (fillAlpha > 0) fillColor = Color.FromArgb(fillAlpha, color);
			Gui.DrawPolygon(octagon, color, fillColor);
		}
		private Point? Intersection(Point start1, Point end1, Point start2, Point end2)
		{
			if ((Math.Max(start1.X, end1.X) < Math.Min(start2.X, end2.X))
			 || (Math.Min(start1.X, end1.X) > Math.Max(start2.X, end2.X))
			 || (Math.Max(start1.Y, end1.Y) < Math.Min(start2.Y, end2.Y))
			 || (Math.Min(start1.Y, end1.Y) > Math.Max(start2.Y, end2.Y)))
				return null;


			double ay_cy, ax_cx, px, py;
			double dx_cx = end2.X - start2.X,
				dy_cy = end2.Y - start2.Y,
				bx_ax = end1.X - start1.X,
				by_ay = end1.Y - start1.Y;

			double de = (bx_ax) * (dy_cy) - (by_ay) * (dx_cx);

			if (Math.Abs(de) < 0.01)
				return null;

			ax_cx = start1.X - start2.X;
			ay_cy = start1.Y - start2.Y;
			double r = ((ay_cy) * (dx_cx) - (ax_cx) * (dy_cy)) / de;
			double s = ((ay_cy) * (bx_ax) - (ax_cx) * (by_ay)) / de;
			px = start1.X + r * (bx_ax);
			py = start1.Y + r * (by_ay);
			if ((px < Math.Min(start1.X, end1.X)) || (px > Math.Max(start1.X, end1.X))
			 || (px < Math.Min(start2.X, end2.X)) || (px > Math.Max(start2.X, end2.X))
			 || (py < Math.Min(start1.Y, end1.Y)) || (py > Math.Max(start1.Y, end1.Y))
			 || (py < Math.Min(start2.Y, end2.Y)) || (py > Math.Max(start2.Y, end2.Y)))
				return null;
			return new Point((int)px, (int)py);
		}
		private void DrawRectRhombusIntersection(Point rectMid, Point rhombMid, int rw, int rh, int r, Color color, int fillAlpha = 63) // Octagon provided by the intersection of a rectangle and a rhombus
		{
			Point[] rect =
			{
				new Point(rectMid.X - rw, rectMid.Y + rh),
				new Point(rectMid.X - rw, rectMid.Y - rh),
				new Point(rectMid.X + rw, rectMid.Y - rh),
				new Point(rectMid.X + rw, rectMid.Y + rh)
			};
			Point[] rhombus =
			{
				new Point(rhombMid.X - r, rhombMid.Y),
				new Point(rhombMid.X, rhombMid.Y - r),
				new Point(rhombMid.X + r, rhombMid.Y),
				new Point(rhombMid.X, rhombMid.Y + r)
			};
			List<Point> finalShape = new List<Point>();
			foreach (Point p in rect)
			{
				if (Math.Abs(p.X - rhombMid.X) + Math.Abs(p.Y - rhombMid.Y) <= r)
					finalShape.Add(p);
			}
			foreach (Point p in rhombus)
			{
				if ((Math.Abs(p.X - rectMid.X) <= rw) && (Math.Abs(p.Y - rectMid.Y) <= rh))
					finalShape.Add(p);
			}
			for (int i = 0; i < 5; i++)
			{
				Point? p = Intersection(rhombus[i & 3], rhombus[(i + 1) & 3], rect[i & 3], rect[(i + 1) & 3]);
				if (p.HasValue) finalShape.Add(p.Value);
				p = Intersection(rhombus[i & 3], rhombus[(i + 1) & 3], rect[(i + 1) & 3], rect[(i + 2) & 3]);
				if (p.HasValue) finalShape.Add(p.Value);
			}
			double mX = 0;
			double my = 0;
			foreach (Point p in finalShape)
			{
				mX += p.X;
				my += p.Y;
			}
			mX /= finalShape.ToArray().Length;
			my /= finalShape.ToArray().Length;
			Color? fillColor = null;
			if (fillAlpha > 0) fillColor = Color.FromArgb(fillAlpha, color);
			Gui.DrawPolygon(finalShape.OrderBy(p => Math.Atan2(p.Y - my, p.X - mX)).ToArray(), color, fillColor);
		}
		private void DrawEccoTriangle(int x1, int y1, int x2, int y2, int x3, int y3, Color color, int fillAlpha = 63)
		{
			Color? fillColor = null;
			Point[] triPoints =
			{
				new Point(x1, y1),
				new Point(x2, y2),
				new Point(x3, y3)
			};
			if (fillAlpha > 0) fillColor = Color.FromArgb(fillAlpha, color);
			Gui.DrawPolygon(triPoints, color, fillColor);
		}
		private void DrawBoxMWH(int x, int y, int w, int h, Color color, int fillAlpha = 63)
		{
			Color? fillColor = null;
			if (fillAlpha > 0) fillColor = Color.FromArgb(fillAlpha, color);
			Gui.DrawRectangle(x - w, y - h, w << 1, h << 1, color, fillColor);
		}
		private void DrawBox(int x, int y, int x2, int y2, Color color, int fillAlpha = 63)
		{
			Color? fillColor = null;
			if (fillAlpha > 0) fillColor = Color.FromArgb(fillAlpha, color);
			Gui.DrawBox(x, y, x2, y2, color, fillColor);
		}
		private void Print_Text(string message, int x, int y, Color color)
		{
			Gui.DrawText(x, y, message, color, null);
		}
		private void PutText(string message, int x, int y, int xl, int yl, int xh, int yh, Color bg, Color fg)
		{
			xl = Math.Max(xl, 0);
			yl = Math.Max(yl, 0);
			xh = Math.Min(xh + 639, 639);
			yh = Math.Min(yh + 441, 441);
			xh -= 4 * message.Length;
			x = x - ((5 * (message.Length - 1)) / 2);
			y -= 3;
//			x = Math.Min(Math.Max(x, Math.Max(xl, 1)), Math.Min(xh, 638 - 4 * (int)message.Length));
//			y = Math.Min(Math.Max(y - 3, Math.Max(yl, 1)), yh);
			int[] xOffset = { -1, -1, -1, 0, 1, 1, 1, 0 };
			int[] yOffset = { -1, 0, 1, 1, 1, 0, -1, -1 };
			for (int i = 0; i < 8; i++)
				Print_Text(message, x + xOffset[i], y + yOffset[i], bg);
			Print_Text(message, x, y, fg);
		}
		private void TickerText(string message, Color? fg = null)
		{
			if (_dumpMap == 0)
				Gui.Text(1, _tickerY, message, fg);
			_tickerY += 16;
		}
		private void EccoDraw3D()
		{
			int CamX = (Mem.ReadS32(0xFFD5E0) >> 0xC) - _left;
			int CamY = (Mem.ReadS32(0xFFD5E8) >> 0xC) + _top;
			int CamZ = (Mem.ReadS32(0xFFD5E4) >> 0xC) + _top;
			uint curObj = Mem.ReadU24(0xFFD4C1);
			while (curObj != 0)
			{
				int Xpos = (Mem.ReadS32(curObj + 0x6) >> 0xC);
				int Ypos = (Mem.ReadS32(curObj + 0xE) >> 0xC);
				int Zpos = (Mem.ReadS32(curObj + 0xA) >> 0xC);
				int Xmid =		  160 + (Xpos - CamX);
				int Ymid =		  112 - (Ypos - CamY);
				int Zmid = _top + 112 - (Zpos - CamZ);
				uint type = Mem.ReadU32(curObj + 0x5A);
				int width, height, depth = height = width = 0;
				if (type == 0xD4AB8) // 3D poison Bubble
				{
					depth = 0x10;
					int radius = 8;
					width = radius;
					DrawEccoOct(Xmid, Ymid, radius, Color.Lime);
					DrawBoxMWH(Xmid, Zmid, width, depth, Color.Blue);
					DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Lime, 0);
					DrawBoxMWH(Xmid, Zmid, 1, 1, Color.Blue, 0);
				}
				else if (type == 0xD817E)// 3D Ring
				{
					depth = 8;
					if (Mem.ReadU32(0xFFB166) < 0x1800) depth = 4;
					int radius = 32;
					width = radius;
					DrawEccoOct(Xmid, Ymid, radius, (Mem.ReadS16(curObj + 0x62) == 0) ? Color.Orange : Color.Gray);
					DrawBoxMWH(Xmid, Zmid, width, depth, Color.Red);
					DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Orange, 0);
					DrawBoxMWH(Xmid, Zmid, 1, 1, Color.Red, 0);
					TickerText($"{Mem.ReadS32(curObj + 0x6) / 4096.0:0.######}:{Mem.ReadS32(curObj + 0xE) / 4096.0:0.######}:{Mem.ReadS32(curObj + 0xA) / 2048.0:0.######}:{Mem.ReadByte(curObj + 0x72)}",Color.Lime);
				}
				else if (type == 0xD49CC) // Vines collisions are based on draw position, which is a fucking pain in the ass to calculate
				{
					int Xvel = (Mem.ReadS32(curObj + 0x3A) - Mem.ReadS32(curObj + 0x6));
					int Zvel = (Mem.ReadS32(curObj + 0x3E) - Mem.ReadS32(curObj + 0xA));
					int dx = Mem.ReadS32(0xFFD5E0) - Mem.ReadS32(0xFFD5C8) >> 3;
					int dy = Mem.ReadS32(0xFFD5E8) - Mem.ReadS32(0xFFD600) >> 3;
					int dz = Mem.ReadS32(0xFFD5E4) - Mem.ReadS32(0xFFD5CC);
					var chargeCount = Mem.ReadByte(0xFFB19B);
					if (chargeCount == 0)
					{
						dz >>= 2;
					}
					else if ((chargeCount > 0x20) || (chargeCount <= 0x10))
					{
						dz >>= 3;
					}
					else if (chargeCount > 0x10)
					{
						dz >>= 4;
					}
					if (Mem.ReadByte(curObj + 0x64) == 0)
					{
						Xvel >>= 0xA;
						Zvel >>= 9;
					}
					else
					{
						Xvel >>= 9;
						Zvel >>= 0xA;
					}
					Xvel += Mem.ReadS32(curObj + 0x2E);
					Zvel += Mem.ReadS32(curObj + 0x32);
					Zpos = (Mem.ReadS32(curObj + 0x26) + dz - Mem.ReadS32(0xFFD5E4)) >> 0xB;
					if ((Zpos < 0x600) && (Zpos > 0))
					{
						Zpos += 0x20;
						int Xcur, Xmax, Ycur, Ymax;
						int Zpos2 = (Mem.ReadS32(curObj + 0xA) + Zvel  + dz - Mem.ReadS32(0xFFD5E4)) >> 0xB;
						Zpos2 = Math.Max(Zpos2 + 0x20, 1);
						if (Mem.ReadS16(curObj + 0x62) != 0)
						{
							Xmid = Mem.ReadS32(curObj + 0x6) + dx + Xvel - Mem.ReadS32(0xFFD5E0);
							if (Math.Abs(Xmid) > 0x400000)
								continue;
							Xpos = Mem.ReadS32(curObj + 0x22) + dx - Mem.ReadS32(0xFFD5E0);
							if (Math.Abs(Xpos) > 0x400000)
								continue;
							Xcur = (Xmid << 2) / Zpos2 + (Xmid >> 5) + 0xA000 + (Xmid >> 5);
							Xmax = (Xpos << 2) / Zpos + (Xpos >> 5) + 0xA000 + (Xpos >> 5);
						}
						else
						{
							Xcur = 0;
							Xmax = 256;
						}
						Ymid = Mem.ReadS32(0xFFD5E8) + dy - Mem.ReadS32(curObj + 0xE);
						Ycur = ((Ymid << 3) / Zpos2) + 0x6000;
						Ypos = Mem.ReadS32(0xFFD5E8) + dy - Mem.ReadS32(curObj + 0x2A);
						Ymax = ((Ypos << 3) / Zpos) + 0x6000;
						dx = Xmax - Xcur;
						dy = Ymax - Ycur;
						int asindx = Math.Abs(dx >> 6) & 0xFFFF;
						int asindy = Math.Abs(dy >> 6) & 0xFFFF;
						int ang;
						if (asindx == asindy)
						{
							if (dx > 0)
							{
								if (dy > 0)
								{
									ang = 0x20;
								}
								else
								{
									ang = 0xE0;
								}
							}
							else
							{
								if (dy > 0)
								{
									ang = 0x60;
								}
								else
								{
									ang = 0xA0;
								}
							}
						}
						else
						{
							if (asindx > asindy)
							{
								asindy <<= 5;
								asindy += asindx - 1;
								asindy &= 0xFFFF;
								asindy /= asindx;
							}
							else
							{
								asindx <<= 5;
								asindx += asindy - 1;
								asindx &= 0xFFFF;
								asindx /= asindy;
								asindy = 0x40 - asindx;
							}
							if (dx > 0)
							{
								if (dy > 0)
								{
									ang = asindy;
								}
								else
								{
									ang = 0xff - asindy;
								}
							}
							else
							{
								if (dy > 0)
								{
									ang = 0x7f - asindy;
								}
								else
								{
									ang = 0x81 + asindy;
								}
							}
						}
						Xcur += Mem.ReadS8(0x2CC8 + ang) << 6;
						Ycur += Mem.ReadS8(0x2BC8 + ang) << 6;
						var dSml = Math.Abs(dx);
						var dBig = Math.Abs(dy);
						if (dBig < dSml)
						{
							dSml ^= dBig;
							dBig ^= dSml;
							dSml ^= dBig;
						}
						int OctRad = (dBig + (dSml >> 1) - (dSml >> 3));
						int i = Math.Max(((OctRad >> 8) + 0x1F) >> 5, 1);
						dx /= i;
						dy /= i;

						Zmid = (Mem.ReadS32(curObj + 0xA) + Mem.ReadS32(curObj + 0x26)) >> 1;
						Zmid >>= 0xC;
						Zmid = 112 + _top - (Zmid - CamZ);
						do
						{
							i--;
							DrawEccoRhomb((Xcur >> 8) + _left, (Ycur >> 8) + _top, 8, Color.Lime);
							DrawBoxMWH((Xcur >> 8) + _left, Zmid, 8, 0x10, Color.Blue);
							Xcur += dx;
							Ycur += dy;
						} while (i >= 0);
						DrawBoxMWH((Mem.ReadS32(0xFFB1AA) >> 8) + _left, (Mem.ReadS32(0xFFB1AE) >> 8) + _top, 1, 1, Color.Lime, 0);
					}
				}
				else if ((type == 0xD3B40) || (type == 0xD3DB2)) // 3D Shark and Jellyfish
				{
					width = (Mem.ReadS32(curObj + 0x12) >> 0xC);
					height = (Mem.ReadS32(curObj + 0x1A) >> 0xC);
					depth = (Mem.ReadS32(curObj + 0x16) >> 0xC);
					DrawBoxMWH(Xmid, Ymid, width, height, Color.Lime);
					DrawBoxMWH(Xmid, Zmid, width, depth, Color.Blue);
					DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Lime, 0);
					DrawBoxMWH(Xmid, Zmid, 1, 1, Color.Blue, 0);
				}
				else if ((type == 0xD4028) || (type == 0xD4DBA)) // 3D Eagle and 3D Shell
				{
					DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Lime, 0);
					DrawBoxMWH(Xmid, Zmid, 1, 1, Color.Blue, 0);
				}
				else if (type == 0xD4432) // 3D Sonar Blast
				{
					DrawEccoOct(Xmid, Ymid, 48, Color.Orange);
					DrawEccoOct(Xmid, Ymid, 32, Color.Lime);
					DrawBoxMWH(Xmid, Zmid, 32, 32, Color.Blue);
					DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Lime, 0);
					DrawBoxMWH(Xmid, Zmid, 1, 1, Color.Blue, 0);
				}
				else if (type == 0xD463A) // 3D Homing Bubble
				{
					DrawEccoOct(Xmid, Ymid, 48, Color.Orange);
					DrawEccoOct(Xmid, Ymid, 32, Color.Lime);
					DrawBoxMWH(Xmid, Zmid, 32, 32, Color.Blue);
					DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Lime, 0);
					DrawBoxMWH(Xmid, Zmid, 1, 1, Color.Blue, 0);
				}
				else if ((type == 0xD37CE) || (type == 0xD4214) || (type == 0xD3808)) // bubbles, splashes, gfx sprites
				{
					width = height = depth = 0;
				}
				else
				{
					if (curObj != 0xFFB134)
					{
						DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Lime, 0);
						DrawBoxMWH(Xmid, Zmid, 1, 1, Color.Blue, 0);
						PutText(type.ToString("X8"), Xmid, Ymid - 4, 1, 1, -1, -9, Color.White, Color.Blue);
						PutText(curObj.ToString("X8"), Xmid, Ymid + 4, 1, 9, -1, -1, Color.White, Color.Blue);
					}
					else
					{
						DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Orange);
						DrawBoxMWH(Xmid, Zmid, 1, 1, Color.Red);
					}
				}
				curObj = Mem.ReadU24(curObj+1);
			}
		}
		private void EccoDrawBoxes()
		{
		//	CamX-=8;
			int Width2, Height2;
			//Ecco HP and Air
			int i = 0;
			int HP = Mem.ReadS16(0xFFAA16) << 3;
			int air = Mem.ReadS16(0xFFAA18);
			Color color;
            int off = 0;
            for (int j = 0; j < air; j++)
			{
				if (j - off == 448)
				{
					i++; off += 448;
				}
				color = Color.FromArgb(j >> 2, j >> 2, j >> 2);
				Gui.DrawLine(_left - 32, j - off, _left - 17, j - off, color);
			}
			for (int j = 0; j < HP; j += 8)
			{
				color = Color.FromArgb(Math.Max(0x38 - (j >> 3),0), 0, Math.Min(j >> 1,255));
				Gui.DrawRectangle(_left - 16, j, 15, 7, color, color);
			}

			//Asterite
			uint type = Mem.ReadU32(0xFFD440);
			uint curObj = 0;
			int Xpos, Xpos2, Ypos, Ypos2, Xmid, Ymid, X, X2, Y, Y2;
			Xpos = Ypos = Xpos2 = Ypos2 = Xmid = Ymid = X = X2 = Y = Y2 = 0;
			if (type == 0xB119A)
			{
				curObj = Mem.ReadU24(Mem.ReadU24(0xFFD429)+5);
				while (curObj != 0)
				{
					Xpos = Mem.ReadS16(curObj + 0x3C);
					Xpos2 = Mem.ReadS16(curObj + 0x24);
					Ypos = Mem.ReadS16(curObj + 0x40);
					Ypos2 = Mem.ReadS16(curObj + 0x28);
					Xpos -= _camX; Xpos2 -= _camX;
					Ypos -= _camY; Ypos2 -= _camY;
					Xmid = (Xpos + Xpos2) >> 1;
					Ymid = (Ypos + Ypos2) >> 1;
					if (Mem.ReadU8(curObj + 0x71) != 0)
					{
						DrawEccoOct(Xpos, Ypos, 48, Color.Blue, 16);
						DrawEccoOct(Xpos2, Ypos2, 48, Color.Blue, 16);
					}
					curObj = Mem.ReadU24(curObj + 5);
				}
				if ((Mem.ReadU8(0xFFA7D0) == 30))
				{
					curObj = Mem.ReadU24(0xFFD425);
					if ((curObj != 0) && (Mem.ReadU32(curObj + 8) != 0))
					{
						Xpos = Mem.ReadS16(curObj + 0x1C) - _camX;
						Ypos = Mem.ReadS16(curObj + 0x20) - _camY;
						DrawEccoOct(Xpos, Ypos, 20, Color.Orange);
					}
				}
			}
			else if (type == 0xB2CB8)
			{
				curObj = Mem.ReadU24(Mem.ReadU24(0xFFD429) + 5);
				while (curObj != 0)
				{
					Xpos = Mem.ReadS16(curObj + 0x3C);
					Xpos2 = Mem.ReadS16(curObj + 0x24);
					Ypos = Mem.ReadS16(curObj + 0x40);
					Ypos2 = Mem.ReadS16(curObj + 0x28);
					Xpos -= _camX; Xpos2 -= _camX;
					Ypos -= _camY; Ypos2 -= _camY;
					Xmid = (Xpos + Xpos2) >> 1;
					Ymid = (Ypos + Ypos2) >> 1;
					if (Mem.ReadU8(curObj + 0x71) != 0)
					{
						if (Mem.ReadByte(0xFFA7D0) != 0x1F)
						{
							DrawEccoOct(Xpos, Ypos, 40, Color.Lime);
							DrawEccoOct(Xpos2, Ypos2, 40, Color.Lime);
						}
						DrawEccoOct(Xpos, Ypos, 48, Color.Blue, 16);
						DrawEccoOct(Xpos2, Ypos2, 48, Color.Blue, 16);
					}
					curObj = Mem.ReadU24(curObj + 5);
				}
			}
			//aqua tubes
			curObj = Mem.ReadU24(0xFFCFC5);
			while (curObj != 0)
			{
				Xpos = Mem.ReadS16(curObj + 0x2C);
				Xpos2= Mem.ReadS16(curObj + 0x34);
				Ypos = Mem.ReadS16(curObj + 0x30);
				Ypos2= Mem.ReadS16(curObj + 0x38);
				Xpos -= _camX; Xpos2 -= _camX;
				Ypos -= _camY; Ypos2 -= _camY;
				Xmid = (Xpos + Xpos2) >> 1;
				Ymid = (Ypos + Ypos2) >> 1;
		//		displayed = false;
				type = Mem.ReadU8(curObj + 0x7E);
				switch (type)
				{
					case 0x15:
					case 0x18:
					case 0x19:
						DrawEccoTriangle(Xmid, Ymid, Xmid, Ypos2, Xpos2, Ymid, Color.Purple);
						break;
					case 0x1A:
					case 0x1D:
					case 0x20:
					case 0x21:
						DrawEccoTriangle(Xmid, Ymid, Xmid, Ypos2, Xpos, Ymid, Color.Purple);
						break;
					case 0x1F:
					case 0x22:
					case 0x23:
						DrawEccoTriangle(Xmid, Ymid, Xmid, Ypos, Xpos, Ymid, Color.Purple);
						break;
					case 0x24:
					case 0x25:
					case 0x26:
					case 0x27:
					case 0x28:
						DrawEccoTriangle(Xmid, Ymid, Xmid, Ypos, Xpos2, Ymid, Color.Purple);
						break;
					case 0x2B:
						Point[] trapPoints =
						{
							new Point(Xpos, Ymid),
							new Point(Xpos + (Ymid - Ypos >> 1), Ypos),
							new Point(Xpos2 - (Ymid - Ypos >> 1), Ypos),
							new Point(Xpos2, Ymid)
						};
						Gui.DrawPolygon(trapPoints, Color.Purple, Color.FromArgb(63, Color.Purple));
						break;
					default:
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Purple);
						if (type != 0x10)
							PutText(type.ToString("X2"), Xmid, Ymid, 1, 1, -1, -1, Color.Red, Color.Blue);
						break;
				}
				curObj = Mem.ReadU24(curObj+1);
			}
			//walls
			curObj = Mem.ReadU24(0xFFCFC1);
			while (curObj != 0)
			{
				Xmid = Mem.ReadS16(curObj + 0x24);
				Xmid = Mem.ReadS16(curObj + 0x28);
				Xpos = Mem.ReadS16(curObj + 0x2C);
				Xpos2= Mem.ReadS16(curObj + 0x34);
				Ypos = Mem.ReadS16(curObj + 0x30);
				Ypos2= Mem.ReadS16(curObj + 0x38);
				Xpos -= _camX; Xpos2 -= _camX;
				Ypos -= _camY; Ypos2 -= _camY;
				Xmid -= _camX; Ymid -= _camY;
				int colltype = Mem.ReadS8(curObj + 0x7E);
				switch (colltype)
				{
					case 0x10:
					case 0x2D:
					case 0x2E:
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue);
						break;
					case 0x11:
						Xmid = (Xpos + Xpos2) >> 1;
						Xpos2 = Xmid;
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.FromArgb(63, Color.Yellow));
						Gui.DrawLine(Xpos2, Ypos, Xpos2, Ypos2, Color.PowderBlue);
						break;
					case 0x12:
						Xmid = (Xpos + Xpos2) >> 1;
						Xpos = Xmid;
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.FromArgb(63, Color.Yellow));
						Gui.DrawLine(Xpos, Ypos, Xpos, Ypos2, Color.PowderBlue);
						break;
					case 0x13:
						Ymid = (Ypos + Ypos2) >> 1;
						Ypos = Ymid;
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.FromArgb(63, Color.Yellow));
						Gui.DrawLine(Xpos, Ypos, Xpos2, Ypos, Color.PowderBlue);
						break;
					case 0x14:
						Ymid = (Ypos + Ypos2) >> 1;
						Ypos2 = Ymid;
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.FromArgb(63, Color.Yellow));
						Gui.DrawLine(Xpos, Ypos2, Xpos2, Ypos2, Color.PowderBlue);
						break;
					case 0x15:
					case 0x16:
					case 0x17:
					case 0x18:
					case 0x19:
						Xmid = (Xpos + Xpos2) >> 1;
						Ymid = (Ypos + Ypos2) >> 1;
						DrawEccoTriangle(Xmid, Ymid, Xmid, Ypos2, Xpos2, Ymid, Color.FromArgb(63, Color.Yellow));
						Gui.DrawLine(Xmid, Ypos2, Xpos2, Ymid, Color.PowderBlue);
						break;
					case 0x1A:
					case 0x1B:
					case 0x1C:
					case 0x1D:
					case 0x1E:
						Xmid = (Xpos + Xpos2) >> 1;
						Ymid = (Ypos + Ypos2) >> 1;
						DrawEccoTriangle(Xmid, Ymid, Xmid, Ypos2, Xpos, Ymid, Color.FromArgb(63, Color.Yellow));
                        Gui.DrawLine(Xpos, Ymid, Xmid, Ypos2, Color.PowderBlue);
						break;
					case 0x1F:
					case 0x20:
					case 0x21:
					case 0x22:
					case 0x23:
						Xmid = (Xpos + Xpos2) >> 1;
						Ymid = (Ypos + Ypos2) >> 1;
						DrawEccoTriangle(Xmid, Ymid, Xmid, Ypos, Xpos, Ymid, Color.FromArgb(63,Color.Yellow));
                        Gui.DrawLine(Xpos, Ymid, Xmid, Ypos, Color.PowderBlue);
						break;
					case 0x24:
					case 0x25:
					case 0x26:
					case 0x27:
					case 0x28:
						Xmid = (Xpos + Xpos2) >> 1;
						Ymid = (Ypos + Ypos2) >> 1;
						DrawEccoTriangle(Xmid, Ymid, Xmid, Ypos, Xpos2, Ymid, Color.FromArgb(63,Color.Yellow));
                        Gui.DrawLine(Xmid, Ypos, Xpos2, Ymid, Color.PowderBlue);
						break;
					case 0x29:
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.FromArgb(63, Color.Black));
						Gui.DrawLine(Xpos , Ypos, Xpos , Ypos2, Color.Black);
						Gui.DrawLine(Xpos2, Ypos, Xpos2, Ypos2, Color.Black);
						break;
					case 0x2A:
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.FromArgb(63, Color.Black));
						Gui.DrawLine(Xpos,  Ypos, Xpos2,  Ypos, Color.Black);
						Gui.DrawLine(Xpos, Ypos2, Xpos2, Ypos2, Color.Black);
						break;
					case 0x2B:
						Xmid = (Xpos + Xpos2) >> 1;
						Ymid = (Ypos + Ypos2) >> 1;
						Point[] trapPoints =
						{
							new Point(Xpos, Ymid),
							new Point(Xpos + (Ymid - Ypos >> 1), Ypos),
							new Point(Xpos2 - (Ymid - Ypos >> 1), Ypos),
							new Point(Xpos2, Ymid)
						};
						Gui.DrawPolygon(trapPoints, Color.PowderBlue, Color.FromArgb(63, Color.PowderBlue));
                        //Gui.DrawLine(Xpos, Ymid, Xpos2, Ymid, Color.Yellow);
						break;
					default:
						DrawEccoRhomb_scaled(Xmid, Ymid, Mem.ReadS16(curObj + 0x44), Mem.ReadS16(curObj + 0x44), (colltype & 1), (colltype & 2) >> 1, (colltype & 4) >> 2, (colltype & 8) >> 3, Color.PowderBlue);
						break;
				}
				curObj = Mem.ReadU24(curObj+1);
			}
			//inanimate objects
			curObj = Mem.ReadU24(0xFFCFBD);
			while (curObj != 0)
			{
				type = Mem.ReadU32(curObj + 0xC);
				int colltype = Mem.ReadS8(curObj + 0x7E);
				Xmid = Mem.ReadS32(curObj + 0x24);
				Ymid = Mem.ReadS32(curObj + 0x28);
				int Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54) + Mem.ReadS32(curObj + 0x5C)) >> 16) - _camX;
				int Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58) + Mem.ReadS32(curObj + 0x60)) >> 16) - _camY;
				Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
				Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
				Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
				Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
				Xmid >>= 16; Ymid >>= 16;
				Xmid -= _camX; Ymid -= _camY;
				if (type == 0x9CE3A) //Remnant Stars
				{
					uint subObj = Mem.ReadU24(curObj + 0x5);
					uint anim = Mem.ReadU16(curObj + 0x6C);
					if ((anim <= 7) && (subObj == 0xFFA9D4))
					{
						DrawEccoRhomb(Xmid, Ymid, 96, Color.Red);
						PutText($"{((7 - anim) * 4) - ((Mem.ReadByte(0xFFA7C9) & 3) - 4)}", Xmid, Ymid + 4, 1, 1, -1, -1, Color.Lime, Color.Blue);
					}
				}
				else if ((type == 0x9CC06) || (type == 0x9CA10))
				{
					Xvec = ((Mem.ReadS32(curObj + 0x24) + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
					Yvec = ((Mem.ReadS32(curObj + 0x28) + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
				}
				else if (type == 0x9B5D8)
				{
					Xvec = Xmid;
					Yvec = Ymid;
				}
				else if (type == 0xC0152) // Vortex Future Vertical Gate
				{
					Xvec = Mem.ReadS16(curObj + 0x1C) - _camX;
					Yvec = (Mem.ReadS32(curObj + 0x20) + Mem.ReadS32(curObj + 0x60) >> 16) - _camY;
					Gui.DrawLine(Xmid, 0, Xmid, 448, Color.PowderBlue);
					DrawBoxMWH(Xvec, Yvec, 1, 1, Color.Blue, 0);
					DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
				}
				else if (type == 0xC3330) // City Of Forever Horizontal Gate Slave
				{
					Xvec = (Mem.ReadS32(curObj + 0x1C) + Mem.ReadS32(curObj + 0x5C) >> 16) - _camX;
					Yvec = Mem.ReadS16(curObj + 0x20) - _camY;
					DrawBoxMWH(Xvec, Yvec, 1, 1, Color.Blue, 0);
					DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
				}
				else if (type == 0xC35B0) // City Of Forever Horizontal Gate Master
				{
					var mode = Mem.ReadByte(curObj + 0x15);
					var tmpx = Xpos;
					Xpos = Mem.ReadS32(curObj + 0x1C);
					Xvec = (Xpos + Mem.ReadS32(curObj + 0x5C) >> 16) - _camX;
					Xpos >>= 16; Xpos -= _camX;
					Yvec = Mem.ReadS16(curObj + 0x20) - _camY;
					if ((mode == 1) || (mode == 3))
					{
						DrawEccoOct(Xpos, Yvec, 128, Color.Orange);
					}
					Xpos = tmpx;
					DrawBoxMWH(Xvec, Yvec, 1, 1, Color.Blue, 0);
					DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
				}
				else if (type == 0xC343A) // City Of Forever Vertical Gate
				{
					var mode = Mem.ReadByte(curObj + 0x15);
					if ((mode == 1) || (mode == 3))
					{
						DrawEccoOct(Xmid, Ymid, 128, Color.Orange);
					}
					Xvec = Mem.ReadS16(curObj + 0x1C) - _camX;
					Yvec = (Mem.ReadS32(curObj + 0x20) + Mem.ReadS32(curObj + 0x60) >> 16) - _camY;
					DrawBoxMWH(Xvec, Yvec, 1, 1, Color.Blue, 0);
					DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
				}
				else if (type == 0xA579A) // Antigrav Ball
				{
					DrawEccoOct(Xmid, Ymid, Mem.ReadS16(curObj + 0x4C), (Mem.ReadU16(0xFFA7C8) & 7) == 7 ? Color.Blue : Color.Gray);
					DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue);
					DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue, 0);
					Xpos = Ypos = Xpos2 = Ypos2 = _camX - 128;
				}
				else if (type == 0xDF4E2) // Moray Abyss Conch Shell
				{
					Xpos = Mem.ReadS16(curObj + 0x1C) - _camX;
					Ypos = Mem.ReadS16(curObj + 0x20) - _camY;
					DrawBox(Xpos - 96, 0 - _camY, Xpos + 96, Mem.ReadS16(0xFFA7AC) - _camY - 64, Color.Orange, 0);
					var mode = Mem.ReadByte(curObj + 0x15);
					var modeTimer = Mem.ReadS16(curObj + 0x6E);
					var Xvel1 = Mem.ReadS32(curObj + 0x54) / 65536.0;
					var Yvel1 = Mem.ReadS32(curObj + 0x58) / 65536.0;
					var Xvel2 = Mem.ReadS32(curObj + 0x5C) / 65536.0;
					var Yvel2 = Mem.ReadS32(curObj + 0x60) / 65536.0;
					TickerText($"{mode}:{modeTimer}:{Mem.ReadS16(0xFFA7AC) - 64 - Ymid - _camY}", Color.Red);
					TickerText($"{Xvel1:0.######}:{Yvel1:0.######}", Color.Red);
					TickerText($"{Xvel2:0.######}:{Yvel2:0.######}", Color.Red);
					TickerText($"{Xvel1 + Xvel2:0.######}:{Yvel1 + Yvel2:0.######}", Color.Red);
					switch (mode)
					{
						case 0:
							Xpos2 = Math.Abs(Xmid - Xpos);
							if (Xpos2 > 0x48)
							{
								Xpos2 = (0x60 - Xpos2) << 1;
								Ypos2 = Ymid + Xpos2;
							}
							else
							{
								Ypos2 = Ymid - (Xpos2 >> 1) + 0x60;
							}
							DrawBoxMWH(Xpos, Ypos2, 1, 1, Color.Gray, 0);
							DrawBoxMWH(Xpos, 112 + _top, 72, 224, (modeTimer <= 1) ? Color.Orange : Color.Gray, 0);
							break;
						case 1:
							DrawBoxMWH(Xpos, Ypos, 1, 1, Color.Orange, 0);
							Xpos2 = Mem.ReadS32(0xFFAA1A) - Mem.ReadS32(curObj + 0x24);
							Ypos2 = Mem.ReadS32(0xFFAA1E) - Mem.ReadS32(curObj + 0x28);
							var dSml = Math.Abs(Xpos2);
							var dBig = Math.Abs(Ypos2);
							if (dBig < dSml)
							{
								dSml ^= dBig;
								dBig ^= dSml;
								dSml ^= dBig;
							}
							var rad = (dBig + (dSml >> 1) - (dSml >> 3)) / 65536.0;
							Xpos2 = (int)(Xpos2 * (256.0 / (rad+1))) >> 20;
							Ypos2 = (int)(Ypos2 * (256.0 / (rad+1))) >> 20;
							Gui.DrawLine(Xmid, Ymid, Xmid + Xpos2, Ymid + Ypos2, Color.Gray);
							TickerText($"{Xpos2 / 512.0:0.######}:{Ypos2 / 512.0:0.######}", Color.Red);
							break;
						case 2:
							TickerText($"{Mem.ReadS32(curObj + 0x4C) / 65536.0:0.######}:{Mem.ReadS32(curObj + 0x50) / 65536.0:0.######}", Color.Red);
							break;
					}
				}
				else if ((type == 0xC57A6) || (type == 0xDEE3C) || (type == 0xDF8A0) || (type == 0xDFA98) 
					  || (type == 0xA0BE4) || (type == 0x9FEB2) || (type == 0xA5670) || (type == 0xAEC1A) 
					  || (type == 0xA6C4A) || (type == 0xAB65A) || (type == 0x9F2EC)) { }
				else
				{
					PutText($"{type:X5}:{Mem.ReadByte(curObj + 0x13)}", Xmid, Ymid - 4, 1, 1, -1, -9, Color.Lime, Color.Blue);
					PutText(curObj.ToString("X6"), Xmid, Ymid + 4, 1, 9, -1, -1, Color.Lime, Color.Blue);
				}
				colltype = Mem.ReadS8(curObj + 0x7E);
				switch (colltype)
				{
					case 0x10:
					case 0x2D:
					case 0x2E:
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue);
						break;
					case 0x11:
						Xmid = (Xpos + Xpos2) >> 1;
						Xpos2 = Xmid;
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue);
						break;
					case 0x12:
						Xmid = (Xpos + Xpos2) >> 1;
						Xpos = Xmid;
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue);
						break;
					case 0x13:
						Ymid = (Ypos + Ypos2) >> 1;
						Ypos = Ymid;
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue);
						break;
					case 0x14:
						Ymid = (Ypos + Ypos2) >> 1;
						Ypos2 = Ymid;
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue);
						break;
					case 0x15:
					case 0x16:
					case 0x17:
					case 0x18:
					case 0x19:
						Xmid = (Xpos + Xpos2) >> 1;
						Ymid = (Ypos + Ypos2) >> 1;
						DrawEccoTriangle(Xmid, Ymid, Xmid, Ypos2, Xpos2, Ymid, Color.FromArgb(63, Color.Yellow));
						Gui.DrawLine(Xmid, Ypos2, Xpos2, Ymid, Color.PowderBlue);
						break;
					case 0x1A:
					case 0x1B:
					case 0x1C:
					case 0x1D:
					case 0x1E:
						Xmid = (Xpos + Xpos2) >> 1;
						Ymid = (Ypos + Ypos2) >> 1;
						DrawEccoTriangle(Xmid, Ymid, Xmid, Ypos2, Xpos, Ymid, Color.FromArgb(63, Color.Yellow));
						Gui.DrawLine(Xpos, Ymid, Xmid, Ypos2, Color.PowderBlue);
						break;
					case 0x1F:
					case 0x20:
					case 0x21:
					case 0x22:
					case 0x23:
						Xmid = (Xpos + Xpos2) >> 1;
						Ymid = (Ypos + Ypos2) >> 1;
						DrawEccoTriangle(Xmid, Ymid, Xmid, Ypos, Xpos, Ymid, Color.FromArgb(63, Color.Yellow));
						Gui.DrawLine(Xpos, Ymid, Xmid, Ypos, Color.PowderBlue);
						break;
					case 0x24:
					case 0x25:
					case 0x26:
					case 0x27:
					case 0x28:
						Xmid = (Xpos + Xpos2) >> 1;
						Ymid = (Ypos + Ypos2) >> 1;
						DrawEccoTriangle(Xmid, Ymid, Xmid, Ypos, Xpos2, Ymid, Color.FromArgb(63, Color.Yellow));
						Gui.DrawLine(Xmid, Ypos, Xpos2, Ymid, Color.PowderBlue);
						break;
					case 0x2A:
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.FromArgb(63, Color.Black));
						Gui.DrawLine(Xpos, Ypos, Xpos2, Ypos, Color.Black);
						Gui.DrawLine(Xpos, Ypos2, Xpos2, Ypos2, Color.Black);
						break;
					case 0x2B:
						Xmid = (Xpos + Xpos2) >> 1;
						Ymid = (Ypos + Ypos2) >> 1;
						Point[] trapPoints =
						{
							new Point(Xpos, Ymid),
							new Point(Xpos + (Ymid - Ypos >> 1), Ypos),
							new Point(Xpos2 - (Ymid - Ypos >> 1), Ypos),
							new Point(Xpos2, Ymid)
						};
						Gui.DrawPolygon(trapPoints, Color.PowderBlue, Color.FromArgb(63, Color.PowderBlue));
						break;
					case 0x2C:
						break;
					default:
						DrawEccoRhomb_scaled(Xmid, Ymid, Mem.ReadS16(curObj + 0x44), Mem.ReadS16(curObj + 0x44), (colltype & 1), (colltype & 2) >> 1, (colltype & 4) >> 2, (colltype & 8) >> 3, Color.PowderBlue);
						break;
				}
				Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
				Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
				DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
				Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
				curObj = Mem.ReadU24(curObj+1);
			}
			//animate objects
			if (_mode == Modes.Ecco2)
				curObj = Mem.ReadU24(0xFFCFB9);
			else
				curObj = Mem.ReadU24(0xFFD829); 
			while (curObj != 0)
			{
				type = 0;
				switch (_mode) {
					case Modes.Ecco2:
						{
							uint flags = Mem.ReadU16(curObj + 0x10);
							int Xvec = 0;
							int Yvec = 0;
							//if ((flags & 0x2000) || !(flags & 2));
							HP = Mem.ReadS8(curObj + 0x7B);
							type = Mem.ReadU32(curObj + 0xC);
							if ((type == 0xA1FE6) || (type == 0xA208E) // Chain link creatures such as vortex worm, magic arm, etc
							 || (type == 0xA2288) || (type == 0xA27A4) || (type == 0xA2BB0) || (type == 0xA2C50))
							{
								uint subObj = curObj;
								while (subObj != 0)
								{
									Xpos = Mem.ReadS32(subObj + 0x24);
									Ypos = Mem.ReadS32(subObj + 0x28);
									Xvec = ((Xpos + Mem.ReadS32(subObj + 0x54)) >> 16) - _camX;
									Yvec = ((Ypos + Mem.ReadS32(subObj + 0x58)) >> 16) - _camY;
									Xpos >>= 16; Ypos >>= 16;
									Xpos -= _camX;
									Ypos -= _camY;
									if (Mem.ReadS16(subObj + 0x44) == Mem.ReadS16(subObj + 0x48))
									{
										DrawEccoOct(Xpos, Ypos, Mem.ReadS16(subObj + 0x44), Color.FromArgb(255, 0, 127));
										DrawEccoOct(Xpos, Ypos, Mem.ReadS16(subObj + 0x44), Color.Cyan, 0);
									}
									else
									{
										DrawEccoOct(Xpos, Ypos, Mem.ReadS16(subObj + 0x44), Color.FromArgb(255, 0, 127));
										DrawEccoOct(Xpos, Ypos, Mem.ReadS16(subObj + 0x44), Color.Lime, 0);
										DrawEccoOct(Xpos, Ypos, Mem.ReadS16(subObj + 0x44), Color.FromArgb(255, 0, 127));
										DrawEccoOct(Xpos, Ypos, Mem.ReadS16(subObj + 0x48), Color.Blue, 0);
									}
									DrawBoxMWH(Xpos, Ypos, 1, 1, Color.Blue, 0);
									Gui.DrawLine(Xpos, Ypos, Xvec, Yvec, Color.Orange);
									subObj = Mem.ReadU24(subObj + 5);
								}
								if (HP > 2)
								{
									Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
									Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
									PutText($"{HP - 1}", Xmid, Ymid, 1, 1, -1, -9, Color.Blue, Color.Red);
								}
							}
							else if ((type == 0xB7486) || (type == 0xB864E) //Chain link creatures such as eels and worms
								  || (type == 0xB8A64) || (type == 0xB8C1A) 
								  || (type == 0xB904A) || (type == 0xB9728) || (type == 0xB9B6A) || (type == 0xBA52E) 
								  || (type == 0xBA66E) || (type == 0xE0988) || (type == 0xA18E2) || (type == 0xE069A))
							{
								uint subObj = curObj;
								while (subObj != 0)
								{
									Xpos = Mem.ReadS32(subObj + 0x24);
									Ypos = Mem.ReadS32(subObj + 0x28);
									Xvec = ((Xpos + Mem.ReadS32(subObj + 0x54)) >> 16) - _camX;
									Yvec = ((Ypos + Mem.ReadS32(subObj + 0x58)) >> 16) - _camY;
									Xpos >>= 16; Ypos >>= 16;
									Xpos -= _camX;
									Ypos -= _camY;
									if (Mem.ReadS16(subObj + 0x44) == Mem.ReadS16(subObj + 0x48))
									{
										DrawEccoOct(Xpos, Ypos, Mem.ReadS16(subObj + 0x44), Color.FromArgb(255, 0, 127));
										DrawEccoOct(Xpos, Ypos, Mem.ReadS16(subObj + 0x44), Color.White, 0);
									}
									else
									{
										DrawEccoOct(Xpos, Ypos, Mem.ReadS16(subObj + 0x44), Color.FromArgb(255, 0, 127));
										DrawEccoOct(Xpos, Ypos, Mem.ReadS16(subObj + 0x44), Color.Lime, 0);
										DrawEccoOct(Xpos, Ypos, Mem.ReadS16(subObj + 0x48), Color.FromArgb(255, 0, 127));
										DrawEccoOct(Xpos, Ypos, Mem.ReadS16(subObj + 0x48), Color.Magenta, 0);
									}
									DrawBoxMWH(Xpos, Ypos, 1, 1, Color.Blue, 0);
									Gui.DrawLine(Xpos, Ypos, Xvec, Yvec, Color.Orange);
									if (type == 0xBA66E)
									{
										DrawEccoOct(Xpos, Ypos, 32, Color.Blue, 16);
										if (subObj == curObj)
										{
											var mode = Mem.ReadByte(subObj + 0x15);
											TickerText($"{Mem.ReadByte(subObj + 0x14)}:{mode}:{Mem.ReadByte(subObj + 0x70)}", Color.Red);
											TickerText($"{Mem.ReadS32(subObj + 0x54) / 65536.0:0.######}:{Mem.ReadS32(subObj + 0x58) / 65536.0:0.######}", Color.Red);
											TickerText($"{Mem.ReadS32(subObj + 0x4C) / 65536.0:0.######}:{Mem.ReadS32(subObj + 0x50) / 65536.0:0.######}", Color.Red);
											switch (mode)
											{
												case 0:
												case 2:
												case 4:
													Xpos2 = Mem.ReadS32(0xFFAA22) - Mem.ReadS32(subObj + 0x24);
													Ypos2 = Mem.ReadS32(0xFFAA26) - Mem.ReadS32(subObj + 0x28);
													var dSml = Math.Abs(Xpos2);
													var dBig = Math.Abs(Ypos2);
													if (dBig < dSml)
													{
														dSml ^= dBig;
														dBig ^= dSml;
														dSml ^= dBig;
													}
													var rad = (dBig + (dSml >> 1) - (dSml >> 3)) / 65536.0;
													Xpos2 = (int)(Xpos2 * (256.0 / (rad + 1))) >> 20;
													Ypos2 = (int)(Ypos2 * (256.0 / (rad + 1))) >> 20;
													Gui.DrawLine(Xpos, Ypos, Xpos + Xpos2, Ypos + Ypos2, Color.Red);
													break;
												default:
													break;

											}
										}
									}
									else if ((type == 0xBA52E) && (subObj == Mem.ReadU24(curObj + 0x1D)))
									{
										DrawEccoOct(Xpos, Ypos, 32, (Mem.ReadByte(subObj + 0x70) == 0) ? Color.Blue : Color.Gray, 16);
										var mode = Mem.ReadByte(curObj + 0x15);
										TickerText($"{Mem.ReadByte(curObj + 0x14)}:{mode}:{Mem.ReadS16(curObj + 0x6E)}:{Mem.ReadByte(subObj + 0x70)}", Color.Red);
										TickerText($"{Mem.ReadS32(subObj + 0x54) / 65536.0:0.######}:{Mem.ReadS32(subObj + 0x58) / 65536.0:0.######}", Color.Red);
										TickerText($"{Mem.ReadS32(curObj + 0x4C) / 65536.0:0.######}:{Mem.ReadS32(curObj + 0x50) / 65536.0:0.######}", Color.Red);
									}
									else if (type == 0xE0988)
									{
										DrawEccoOct(Xpos, Ypos, 48, Color.FromArgb(64, Color.Blue), 16);
									}
									if (HP > 2)
									{
										Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
										Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
										PutText($"{HP - 1}", Xmid, Ymid, 1, 1, -1, -9, Color.Blue, Color.Red);
									}
									subObj = Mem.ReadU24(subObj + 5);
								}
							}
							else if (type == 0xB7DF4)
							{
								Xpos = Mem.ReadS32(curObj + 0x24);
								Ypos = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xpos + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ypos + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xpos >>= 16; Ypos >>= 16;
								Xpos -= _camX;
								Ypos -= _camY;
								DrawEccoOct(Xpos, Ypos, 26, Color.PowderBlue);
								DrawEccoOct(Xpos, Ypos, 26, Color.White);
								DrawBoxMWH(Xpos, Ypos, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xpos, Ypos, Xvec, Yvec);
							}
							else if (type == 0xE47EE)
							{
								uint subObj = Mem.ReadU24(curObj + 5);
								while (subObj != 0)
								{
									Xpos = Mem.ReadS32(subObj + 0x1C);
									Ypos = Mem.ReadS32(subObj + 0x20);
									Xvec = ((Xpos + Mem.ReadS32(subObj + 0x54)) >> 16) - _camX;
									Yvec = ((Ypos + Mem.ReadS32(subObj + 0x58)) >> 16) - _camY;
									Xpos >>= 16; Ypos >>= 16;
									Xpos -= _camX;
									Ypos -= _camY;
									DrawEccoOct(Xpos, Ypos, ((Mem.ReadS16(subObj + 0x2C) & 0xFFFF) >> 1) + 16, Color.White);
									DrawEccoOct(Xpos, Ypos, ((Mem.ReadS16(subObj + 0x2C) & 0xFFFF) >> 1) + 16, Color.Yellow, 0);
									DrawBoxMWH(Xpos, Ypos, 1, 1, Color.Blue, 0);
									Gui.DrawLine(Xpos, Ypos, Xvec, Yvec, Color.Orange);
									subObj = Mem.ReadU24(subObj + 5);
								}
							}
							else if (type == 0xDBE64) // Medusa Boss
							{
								uint subObj = curObj;
								uint next;
								do
								{
									next = Mem.ReadU24(subObj + 5);
									if (next != 0) subObj = next;
								} while (next != 0);
								Xpos = Mem.ReadS16(subObj + 0x2C);
								Xpos2 = Mem.ReadS16(subObj + 0x34);
								Ypos = Mem.ReadS16(subObj + 0x30);
								Ypos2 = Mem.ReadS16(subObj + 0x38);
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								DrawEccoOct(Xpos, Ypos, 32, Color.Red);
								DrawEccoOct(Xpos2, Ypos2, 32, Color.Red);
								Xpos = Mem.ReadS32(curObj + 0x24);
								Ypos = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xpos + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ypos + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xpos >>= 16; Ypos >>= 16;
								Xpos -= _camX;
								Ypos -= _camY;
								var octOff = (int)(Math.Sqrt(2) * 60) >> 1;
								Point[] hemiOctPoints =
								{
									new Point(Xpos - 60, Ypos),
									new Point(Xpos - octOff, Ypos - octOff),
									new Point(Xpos, Ypos - 60),
									new Point(Xpos + octOff, Ypos - octOff),
									new Point(Xpos + 60, Ypos)
								};
								Gui.DrawPolygon(hemiOctPoints, Color.Cyan, Color.FromArgb(0x3F, Color.Cyan));
								for (int l = 0; l < 4; l++)
								{
									Gui.DrawLine(hemiOctPoints[l].X, hemiOctPoints[l].Y, hemiOctPoints[l + 1].X, hemiOctPoints[l + 1].Y, Color.Cyan);
								}
								DrawBoxMWH(Xpos, Ypos + 12, 52, 12, Color.Cyan);
								DrawBoxMWH(Xpos, Ypos, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xpos, Ypos, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xDCEE0) // Globe Holder boss
							{
								uint subObj;
								var mode = Mem.ReadByte(curObj + 0x15);
								if (mode < 4)
								{
									subObj = Mem.ReadU24(curObj + 9);
									while (subObj != 0)
									{
										Xmid = Mem.ReadS32(subObj + 0x24);
										Ymid = Mem.ReadS32(subObj + 0x28);
										Xvec = ((Xmid + Mem.ReadS32(subObj + 0x54) + Mem.ReadS32(subObj + 0x5C)) >> 16) - _camX;
										Yvec = ((Ymid + Mem.ReadS32(subObj + 0x58) + Mem.ReadS32(subObj + 0x60)) >> 16) - _camY;
										Xmid >>= 16; Ymid >>= 16;
										Xmid -= _camX; Ymid -= _camY;
										DrawEccoOct(Xmid, Ymid, 12, Color.Orange);
										DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
										Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
										var next = Mem.ReadU24(subObj + 9);
										if ((next == 0) && ((mode & 1) != 0))
										{
											DrawEccoOct(Xmid, Ymid, Mem.ReadS16(subObj + 0x3C), Color.Orange);
										}
										subObj = Mem.ReadU24(subObj + 9);
									}
									subObj = Mem.ReadU24(curObj + 5);
									while (subObj != 0)
									{
										Xmid = Mem.ReadS32(subObj + 0x24);
										Ymid = Mem.ReadS32(subObj + 0x28);
										Xvec = ((Xmid + Mem.ReadS32(subObj + 0x54) + Mem.ReadS32(subObj + 0x5C)) >> 16) - _camX;
										Yvec = ((Ymid + Mem.ReadS32(subObj + 0x58) + Mem.ReadS32(subObj + 0x60)) >> 16) - _camY;
										Xmid >>= 16; Ymid >>= 16;
										Xmid -= _camX; Ymid -= _camY;
										DrawEccoOct(Xmid, Ymid, 12, Color.Orange);
										DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
										Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
										var next = Mem.ReadU24(subObj + 5);
										if ((next == 0) && ((mode & 2) != 0))
										{
											DrawEccoOct(Xmid, Ymid, Mem.ReadS16(subObj + 0x3C), Color.Orange);
										}
										subObj = Mem.ReadU24(subObj + 5);
									}
								}
								Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
								Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
								int Xtmp = Mem.ReadS32(curObj + 0x2C);
								int Ytmp = Mem.ReadS32(curObj + 0x30);
								int Xtmp2 = Mem.ReadS32(curObj + 0x34);
								int Ytmp2 = Mem.ReadS32(curObj + 0x38);
								Xpos = (Xtmp >> 16) - _camX; Xpos2 = (Xtmp2 >> 16) - _camX;
								Ypos = (Ytmp >> 16) - _camY; Ypos2 = (Ytmp2 >> 16) - _camY;
								Xvec = ((Mem.ReadS32(curObj + 0x24) + Mem.ReadS32(curObj + 0x54) + Mem.ReadS32(curObj + 0x5C)) >> 16) - _camX;
								Yvec = ((Mem.ReadS32(curObj + 0x28) + +Mem.ReadS32(curObj + 0x58) + Mem.ReadS32(curObj + 0x60)) >> 16) - _camY;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								if (mode < 7)
								{
									double overlap = 0;
									DrawEccoOct(Xmid, Ymid, 0x5C, Mem.ReadByte(curObj + 0x7C) == 0 ? Color.Blue : Color.Gray);
									DrawEccoOct(Xmid, Ymid, 0x5C, Color.Cyan, 0);
									Xvec = (Mem.ReadS32(curObj + 0x54) + Mem.ReadS32(curObj + 0x5C));
									Yvec = (Mem.ReadS32(curObj + 0x58) + Mem.ReadS32(curObj + 0x60));
									subObj = Mem.ReadU24(curObj + 0x69);
									if (subObj != 0)
									{
										Xpos = Mem.ReadS32(subObj + 0x2C);
										Ypos = Mem.ReadS32(subObj + 0x30);
										Xpos2 = Mem.ReadS32(subObj + 0x34);
										Ypos2 = Mem.ReadS32(subObj + 0x38);
										while ((Xtmp > Xpos) && (Xtmp2 < Xpos2) && (Ytmp > Ypos) && (Ytmp2 < Ypos2) && ((Xvec != 0) || (Yvec != 0)))
										{
											Xtmp += Xvec; Xtmp2 += Xvec;
											Ytmp += Yvec; Ytmp2 += Yvec;
										}
										overlap = Math.Max(Math.Max(Xpos - Xtmp, Xtmp2 - Xpos2), Math.Max(Ypos - Ytmp, Ytmp2 - Ypos2)) / 65536.0;
										Xpos >>= 16; Xpos2 >>= 16;
										Ypos >>= 16; Ypos2 >>= 16;
										Xpos -= _camX; Xpos2 -= _camX;
										Ypos -= _camY; Ypos2 -= _camY;
										DrawBox(Xpos, Ypos, Xpos2, Ypos2, (overlap >= 6) ? Color.Orange : Color.White, 0);
									}
									Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
									Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
									Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
									Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
									DrawBox(Xpos, Ypos, Xpos2, Ypos2, (overlap >= 6) ? Color.Orange : Color.White, (overlap >= 6) ? 63 : 0);
									if (mode < 4)
									{
										Xmid = Mem.ReadS16(curObj + 0x4C) - _camX;
										Ymid = Mem.ReadS16(curObj + 0x50) - _camY;
										if ((mode & 1) == 0) DrawEccoOct(Xmid, Ymid - 0xAE, 32, Color.Orange);
										if ((mode & 2) == 0) DrawEccoOct(Xmid, Ymid + 0xAE, 32, Color.Orange);
									}
									TickerText($"{mode}:{Mem.ReadByte(curObj + 0x7F)}:{Mem.ReadByte(curObj + 0x6D)}:{Mem.ReadByte(curObj + 0x7C)}", Color.Red);
								}
								else if (mode == 8)
								{
									DrawEccoOct(Xmid - 16, Ymid - 16, 12, Color.Red);
								}
							}
							else if (type == 0xE1BA2) // Vortex Queen Boss
							{
								var vulnCount = Mem.ReadByte(curObj + 0x7F);
								var state = Mem.ReadByte(curObj + 0x7C);
								var stateCounter = Mem.ReadU16(curObj + 0x6E);
								var mode = Mem.ReadU16(curObj + 0x64);
								var modeCounter = Mem.ReadU16(curObj + 0x66);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x40);
								Xvec = Xmid + Mem.ReadS32(curObj + 0x54);
								Yvec = Ymid + Mem.ReadS32(curObj + 0x58);
								Xvec >>= 16; Yvec >>= 16;
								Xmid >>= 16; Ymid >>= 16;
								Xvec -= _camX; Yvec -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								if (mode < 5)
								{
									var octOff = (int)(80 * Math.Sqrt(2)) >> 1;
									Point hexOff = Intersection(new Point(-80, 0), new Point(-octOff, -octOff), new Point(-80, -32), new Point(-octOff, -32)).Value;
									Point[] roundedRect = {
										new Point(Xmid -       80, Ymid),
										new Point(Xmid + hexOff.X, Ymid - 32),
										new Point(Xmid - hexOff.X, Ymid - 32),
										new Point(Xmid +       80, Ymid),
										new Point(Xmid - hexOff.X, Ymid + 32),
										new Point(Xmid + hexOff.X, Ymid + 32)
									};
									Gui.DrawPolygon(roundedRect, Color.Orange, Color.FromArgb(63, Color.Orange));
								}
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								TickerText($"{state:X2}:{stateCounter}:{mode}:{modeCounter}:{Mem.ReadByte(curObj + 0x70) & 0xF}", Color.Red);
								var subObj = Mem.ReadU24(curObj + 0x5);
								var tongueMode = mode;
								mode = Mem.ReadByte(subObj + 0x15);
								modeCounter = Mem.ReadU16(subObj + 0x6E);
								Xmid = Mem.ReadS32(subObj + 0x24);
								Ymid = Mem.ReadS32(subObj + 0x40);
								Xvec = (Xmid + Mem.ReadS32(subObj + 0x5C) >> 16) - _camX;
								Yvec = (Ymid + Mem.ReadS32(subObj + 0x60) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								Ymid -= 32; Yvec -= 32;
								var levelHeight = Mem.ReadS16(0xFFA7AC) - _camY;
								switch (mode)
								{
									case 0:
										DrawBox(Xmid - 32, Ymid - ((state == 5) ? 0x60 : 0x70), Xmid + 32, Ymid - 16, Color.Red);
										break;
									case 2:
										Ypos = Mem.ReadS16(subObj + 0x50) - _camY;
										Gui.DrawLine(Xmid - 48, Ypos, Xmid + 48, Ypos, Color.Orange);
										DrawBoxMWH(Xmid, Ymid + 32, 1, 1, Color.Orange, 0);
										break;
									case 3:
										modeCounter = Mem.ReadByte(subObj + 0x7C);
										break;
									case 5:
										Point[] throatShape =
										{
											new Point(Xmid - 48, levelHeight),
											new Point(Xmid - 48, Ymid + 60),
											new Point(Xmid - 16, Ymid + 20),
											new Point(Xmid + 16, Ymid + 20),
											new Point(Xmid + 48, Ymid + 60),
											new Point(Xmid + 48, levelHeight)
										};
										Gui.DrawPolygon(throatShape, Color.Red, Color.FromArgb(63, Color.Red));
										DrawEccoOct(Xmid, Ymid, 24, Color.Red);
										DrawEccoOct(Xmid, Ymid, 24, Color.White, 0);
										break;
									case 6:
										if ((state != 7) && (vulnCount == 0) && (tongueMode != 7))
										{
											DrawEccoOct(Xmid, Ymid + 16, 64, Color.Blue);
										}
										if (tongueMode == 7)
										{
											uint subObj2 = Mem.ReadU24(0xFFCFCD);
											while (subObj2 != 0)
											{
												if (Mem.ReadU16(subObj2 + 0x10) == 0xFF)
												{
													Xpos = Mem.ReadS16(subObj2 + 0x24) - _camX;
													Ypos = Mem.ReadS16(subObj2 + 0x28) - _camY;
													Xpos2 = ((Mem.ReadS32(subObj2 + 0x24) + Mem.ReadS32(subObj2 + 0x54)) >> 16) - _camX;
													Ypos2 = ((Mem.ReadS32(subObj2 + 0x28) + Mem.ReadS32(subObj2 + 0x58)) >> 16) - _camY;
													DrawBoxMWH(Xpos, Ypos, 1, 1, Color.Blue, 0);
													Gui.DrawLine(Xpos, Ypos, Xpos2, Ypos2, Color.Orange);
												}
												subObj2 = Mem.ReadU24(subObj2 + 1);
											}
										}
										Ypos = Mem.ReadS16(subObj + 0x50) - _camY;
										Gui.DrawLine(Xmid - 48, Ypos - 94, Xmid + 48, Ypos - 94, Color.Orange);
										Gui.DrawLine(Xmid - 48, Ypos, Xmid + 48, Ypos, Color.Orange);
										DrawBoxMWH(Xmid, Ymid + 32, 1, 1, Color.Orange, 0);
										break;
									default:
										break;
								}
								if ((mode < 7) || ((mode == 7) && (Mem.ReadU24(0xFFCFC9) != 0)))
								{
									if (Mem.ReadByte(subObj + 0x70) == 0)
									{
										DrawEccoOct(Xmid, Ymid, 32, Color.Red);
										DrawBox(Xmid - 48, Ymid + 32, Xmid + 48, levelHeight, Color.Red);
									}
									Ypos = Mem.ReadS16(subObj + 0x50) - _camY - 94;
									Gui.DrawLine(Xmid - 48, Ypos, Xmid + 48, Ypos, Color.Orange);
									DrawBoxMWH(Xmid, Ymid + 32, 1, 1, Color.Orange, 0);
								}
								if (Mem.ReadS32(subObj + 0xC) == 0xE17B4)
								{
									Point[] shapePoints =
									{
										new Point(Xmid - 48, levelHeight),
										new Point(Xmid - 48, Ymid + 60),
										new Point(Xmid - 16, Ymid + 20),
										new Point(Xmid + 16, Ymid + 20),
										new Point(Xmid + 48, Ymid + 60),
										new Point(Xmid + 48, levelHeight)
									};
									Gui.DrawPolygon(shapePoints, Color.Red, Color.FromArgb(63, Color.Red));
									DrawEccoOct(Xmid, Ymid, 24, Color.Red);
									DrawEccoOct(Xmid, Ymid, 24, Color.White, 0);
								}
								Ypos = (Mem.ReadS16(subObj + 0x50) - _camY) - 264;
								DrawBoxMWH(160 + _left, Ypos, 320, 12, (32 < stateCounter) && (stateCounter < 160) ? Color.Brown : Color.Gray);
								if ((32 < stateCounter) && (stateCounter < 160))
								{
									DrawBoxMWH(_left + 160, Ypos, 320, 12, Color.White, 0);
								}
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								TickerText($"{mode:X2}:{modeCounter}:{HP}:{vulnCount}", Color.Red);
								HP = 0;
							}
							else if (type == 0xA5BD2) // Telekinetic future dolphins
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue);
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Red, 0);
								DrawEccoOct(Xmid, Ymid, 4, Color.Red);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if ((type == 0x9F5B0) || (type == 0x9F4DC) || (type == 0x9F6A0)) // Falling rock, breaks barriers
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Lime);
								Gui.DrawLine(Xpos, Ypos2, Xpos2, Ypos, Color.Lime);
								Gui.DrawLine(Xpos, Ypos, Xpos2, Ypos2, Color.Lime);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								if (type == 0x9F6A0)
								{
									int width = Mem.ReadS16(curObj + 0x44) << 1;
									DrawBox(Xpos - width, Ypos - (width << 2), Xpos2 + width, Ypos2, Color.Lime);
								}
								TickerText($"{Mem.ReadS32(curObj + 0x54) / 65536.0:0.######}:{Mem.ReadS32(curObj + 0x58) / 65536.0:0.######}", Color.Lime);
							}
							else if (type == 0xA3B18)
							{
								Xpos = Mem.ReadS32(curObj + 0x24);
								Ypos = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xpos + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ypos + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xpos >>= 16; Ypos >>= 16;
								Xpos -= _camX;
								Ypos -= _camY;
								DrawEccoOct(Xpos, Ypos, Mem.ReadS16(curObj + 0x44), Color.Yellow);
								DrawBoxMWH(Xpos, Ypos, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xpos, Ypos, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xA4018)
							{
								Xpos = Mem.ReadS32(curObj + 0x24);
								Ypos = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xpos + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ypos + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xpos >>= 16; Ypos >>= 16;
								Xpos -= _camX;
								Ypos -= _camY;
								DrawEccoOct(Xpos, Ypos, Mem.ReadS16(curObj + 0x44), Color.Gray);
								DrawBoxMWH(Xpos, Ypos, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xpos, Ypos, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xA091E) // Blue Whale
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								Xpos = Xmid; Ypos = Ymid;
								Ymid -= 64; Yvec -= 64;
								DrawEccoOct_scaled(Xmid, Ymid, 2, 0, 0x50, Color.Red, 31);
								DrawEccoOct_scaled(Xmid, Ymid, 2, 0, 0x40, Color.Red, 31);
								DrawEccoOct_scaled(Xmid, Ymid, 2, 0, 0x30, Color.Red, 31);
								DrawEccoOct_scaled(Xmid, Ymid, 2, 0, 0x20, Color.Red, 31);
								DrawEccoOct_scaled(Xmid, Ymid, 2, 0, 0x10, Color.Red, 31);
								if (Mem.ReadByte(curObj + 0x7F) == 0)
								{
									Xpos += (Mem.ReadS16(curObj + 0x6E) == 0) ? -278 : 162;
									Ypos += 44 - Mem.ReadS16(curObj + 0x48);
									DrawEccoOct(Xpos, Ypos, 32, Color.Blue);
								}
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);

							}
							else if (type == 0xE66D8) //Vortex Larva
							{
								uint subObj = Mem.ReadU24(curObj + 5);
								while (subObj != 0)
								{
									Xpos = Mem.ReadS16(subObj + 0x1C);
									Ypos = Mem.ReadS16(subObj + 0x20);
									Xpos2 = Mem.ReadS16(subObj + 0x24);
									Ypos2 = Mem.ReadS16(subObj + 0x28);
									Xpos -= _camX; Ypos -= _camY;
									Xpos2 -= _camX; Ypos2 -= _camY;
									DrawEccoOct(Xpos, Ypos, 30, Color.White, 32);
									DrawEccoOct(Xpos, Ypos, 30, Color.Yellow, 0);
									DrawEccoOct(Xpos2, Ypos2, 30, Color.White, 32);
									DrawEccoOct(Xpos2, Ypos2, 30, Color.Yellow, 0);
									subObj = Mem.ReadU24(subObj + 5);
								}
								Xpos = Mem.ReadS32(curObj + 0x24);
								Ypos = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xpos + Mem.ReadS32(curObj + 0x54) + Mem.ReadS32(curObj + 0x5C)) >> 16) - _camX;
								Yvec = ((Ypos + Mem.ReadS32(curObj + 0x58) + Mem.ReadS32(curObj + 0x60)) >> 16) - _camY;
								Xpos2 = Mem.ReadS16(curObj + 0x72) - _camX;
								Ypos2 = Mem.ReadS16(curObj + 0x76) - _camY;
								Xpos >>= 16; Ypos >>= 16;
								Xpos -= _camX; Ypos -= _camY;
								DrawEccoOct(Xpos, Ypos, 0xB0, Color.Yellow);
								DrawEccoOct(Xpos, Ypos, 0xB0, Color.Red, 0);
								DrawEccoOct(Xpos, Ypos, 0x70, Color.Red);
								DrawEccoOct(Xpos, Ypos, 0x38, Color.White);
								DrawEccoOct(Xpos, Ypos, 0x38, Color.Red, 0);
								DrawEccoOct(Xpos, Ypos, 48, Color.Blue, ((Mem.ReadByte(curObj + 0x7B) > 2) && (Mem.ReadByte(curObj + 0x14) != 0)) ? 63 : 0);
								DrawEccoOct(Xpos2, Ypos2, 32, Color.Orange);
								DrawBoxMWH(Xpos, Ypos, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xpos, Ypos, Xvec, Yvec, Color.Orange);
								Gui.DrawLine(Xpos, Ypos, Xpos2, Ypos2, Color.Orange);
								TickerText($"{Mem.ReadByte(curObj + 0x14):X2}:{Mem.ReadByte(curObj + 0x7B):X2}:{Mem.ReadS16(curObj + 0x6E):D2}", Color.Red);
								TickerText($"{ Mem.ReadByte(curObj + 0x7C):X2}:{Mem.ReadS16(curObj + 0x18):D3}", Color.Red);
								TickerText($"{(Mem.ReadS32(curObj + 0x54) + Mem.ReadS32(curObj + 0x5C))/65536.0:0.######}:{(Mem.ReadS32(curObj + 0x58) + Mem.ReadS32(curObj + 0x60)) / 65536.0:0.######}", Color.Red);

							}
							else if (type == 0x9CE3A) //Remnant Stars
							{
								flags = Mem.ReadU16(curObj + 0x10);
								uint subObj = Mem.ReadU24(curObj + 0x5);
								uint anim = Mem.ReadU16(curObj + 0x6C);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								if ((anim <= 7) && (subObj == 0xFFA9D4))
								{
									DrawEccoRhomb(Xmid, Ymid, 96, Color.Red);
									PutText($"{((7 - anim) * 4) - ((Mem.ReadByte(0xFFA7C9) & 3) - 4)}", Xmid, Ymid + 4, 1, 1, -1, -1, Color.Blue, Color.Red);

								}
							}
							else if (type == 0xA997C) // Vortex Soldier
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = (Xmid + Mem.ReadS32(curObj + 0x54) >> 16) - _camX;
								Yvec = (Ymid + Mem.ReadS32(curObj + 0x58) >> 16) - _camY;
								Xvec += Mem.ReadS16(curObj + 0x64);
								Yvec += Mem.ReadS16(curObj + 0x66);
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								if (Mem.ReadByte(curObj + 0x7A) == 0)
								{
									DrawRectRhombusIntersection(new Point(Xmid, Ymid + 6), new Point(Xmid, Ymid), 50, 44, 64, Color.Red);
								}
								DrawRectRhombusIntersection(new Point(Xmid, Ymid - 25), new Point(Xmid, Ymid), 38, 47, 64, Color.Red);
								DrawBoxMWH(Xmid, Ymid, Mem.ReadS16(curObj + 0x44), Mem.ReadS16(curObj + 0x48), Color.Blue, 16);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if ((type == 0xA6C4A) || (type == 0xC43D4)) // Barrier Glyphs
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								var subType = Mem.ReadByte(curObj + 0x13);
								if ((Mem.ReadU8(curObj + 0x7A) == 0) && (Mem.ReadU8(0xFFA7B5) != 0)
								 && ((type != 0xA6C4A) || (subType == 0x14) || (subType == 0x97)))
								{
									DrawEccoOct(Xmid, Ymid, 70, Color.Red);
								}
								DrawBoxMWH(Xmid, Ymid, Mem.ReadS16(curObj + 0x44), Mem.ReadS16(curObj + 0x48), Color.Blue);
								DrawBoxMWH(Xmid, Ymid, Mem.ReadS16(curObj + 0x44), Mem.ReadS16(curObj + 0x48), Color.PowderBlue, 0);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if ((type == 0xB4F46) || (type == 0xB4E1C) || (type == 0xB4C18) || (type == 0xB4ACC) || (type == 0xB4B72)) // Guiding Orca/Dolphin
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								int Xdst = Mem.ReadS16(curObj + 0x64);
								Xdst <<= 7;
								Xdst = Xdst + 0x40 - _camX;
								int Ydst = Mem.ReadS16(curObj + 0x66);
								Ydst <<= 7;
								Ydst = Ydst + 0x40 - _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								Gui.DrawLine(Xmid, Ymid, Xdst, Ydst, Color.Orange);
								DrawBoxMWH(Xdst, Ydst, 64, 64, Color.Orange);
								TickerText($"{Mem.ReadS16(curObj + 0x24)}:{Mem.ReadS16(curObj + 0x28)}:{Mem.ReadByte(curObj + 0x7D)}:{Mem.ReadByte(curObj + 0x7A)}", Color.Lime);
								TickerText($"{Mem.ReadS32(curObj + 0x54) / 65536.0:0.######}:{Mem.ReadS32(curObj + 0x58) / 65536.0:0.######}", Color.Lime);
								TickerText($"{Mem.ReadS32(curObj + 0x72) / 65536.0:0.######}:{Mem.ReadS32(curObj + 0x76) / 65536.0:0.######}", Color.Lime);
							}
							else if (type == 0xB5938) // Lost Orca
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xpos = Mem.ReadS16(curObj + 0x1C) - _camX;
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								//DrawBoxMWH(Xmid, Ymid, 64, 32, Color.Lime);
								if (Mem.ReadU16(curObj + 0x6E) == 0)
								{
									if (Mem.ReadByte(curObj + 0x7D) == 0)
									{
										DrawBox(Xmid + 8, Ymid - 32, Xmid + 64, Ymid + 32, Color.Red);
									}
									else
									{
										DrawBox(Xmid - 64, Ymid - 32, Xmid - 8, Ymid + 32, Color.Red);
									}
								}
								Gui.DrawLine(Xpos - 80, Ymid, Xpos + 80, Ymid, Color.Green);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if ((type == 0xB552A) || (type == 0xB5C42) || (type == 0xB5AFE)) // Following Orca, Returning Orca, & Idling Orca
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xB624A) // Orca Mother
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								int height = Mem.ReadS16(curObj + 0x48) + 32;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								DrawBoxMWH(Xmid, Ymid, 80, 32, Color.Red, 31);
								DrawBoxMWH(Xmid, Ymid, Mem.ReadS16(curObj + 0x44), Mem.ReadS16(curObj + 0x48), Color.Blue);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								if (Mem.ReadS32(0xFFAB7E) != 0)
								{
									DrawEccoOct(Xmid, Ymid, 0x50, Color.Red, 31);
								}
							}
							else if (type == 0xC047E)
							{
								Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
								Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
								var width = 2;
								var height = 2;
								if (Mem.ReadByte(curObj + 0x15) == 0)
								{
									width = Mem.ReadS16(curObj + 0x44);
									height = Mem.ReadS16(curObj + 0x48);
								}
								DrawBoxMWH(Xmid, Ymid, width, height, Color.Lime);
							}
							else if (type == 0xC056E)
							{
								Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
								Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
								var width = Mem.ReadS16(curObj + 0x44);
								var height = Mem.ReadS16(curObj + 0x48);
								DrawBoxMWH(Xmid, Ymid, width, height, Color.Lime);
							}
							else if (type == 0xC4208) // Broken Glyph Base
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								var width = Mem.ReadS16(curObj + 0x44);
								var height = Mem.ReadS16(curObj + 0x48);
								DrawBoxMWH(Xmid, Ymid, width, height, Color.PowderBlue);
								if (Mem.ReadByte(curObj + 0x15) == 0)
								{
									DrawRectRhombusIntersection(new Point(Xmid, Ymid), new Point(Xmid, Ymid), 80, 80, 120, Color.Orange);
								}
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xAC242) // Broken Glyph Top repairing
							{
								uint subObj = Mem.ReadU24(curObj + 0x5);
								Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
								Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
								Xpos = Mem.ReadS16(subObj + 0x24) - _camX;
								Ypos = Mem.ReadS16(subObj + 0x28) - _camY;
								var width = Mem.ReadS16(curObj + 0x44);
								var height = Mem.ReadS16(curObj + 0x48);
								DrawBoxMWH(Xmid, Ymid, width, height, Color.Gray);
								Point[] rhombPoints =
								{
									new Point(Xpos - 3, Ypos),
									new Point(Xpos, Ypos - 3),
									new Point(Xpos + 3, Ypos),
									new Point(Xpos, Ypos + 3)
								};
								Gui.DrawPolygon(rhombPoints, Color.Orange, Color.FromArgb(63, Color.Orange));
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xpos, Ypos, Color.Orange);
							}
							else if (type == 0xBE9C8) // Broken Glyph Top free
							{
								Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
								Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
								Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
								Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue);
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Lime, 0);
								Gui.DrawLine(Xpos, Ypos2, Xpos2, Ypos, Color.Lime);
								Gui.DrawLine(Xpos, Ypos, Xpos2, Ypos2, Color.Lime);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								TickerText($"{Mem.ReadS32(curObj + 0x54) / 65536.0:0.######}:{Mem.ReadS32(curObj + 0x58) / 65536.0:0.######}", Color.Lime);								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if ((type == 0xD9C0E) || (type == 0xDA9EA))
							{
								Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
								Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
								DrawBoxMWH(Xmid, Ymid, 0xA0, 0x70, Color.Red);
							}
							else if ((type == 0xBF204) || (type == 0xDA2C0))
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54) + Mem.ReadS32(curObj + 0x5C)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58) + Mem.ReadS32(curObj + 0x60)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xAF9CC) // Mirror Dolphin
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xpos = Mem.ReadS16(curObj + 0x1C) - _camX + (Mem.ReadByte(curObj + 0x15) == 0 ? 27 : -27);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54) + Mem.ReadS32(curObj + 0x5C)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58) + Mem.ReadS32(curObj + 0x60)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								var width = Mem.ReadS16(curObj + 0x44);
								var height = Mem.ReadS16(curObj + 0x48);
								DrawBoxMWH(Xmid, Ymid, width, height, Color.Blue, 31);
								if (Mem.ReadByte(curObj + 0x13) != 0xAC)
								{
									DrawBoxMWH(Xmid, Ymid, 96, 96, Color.Orange);
								}
								Gui.DrawLine(Xpos, 0, Xpos, 448, Color.Red);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xAF43E) // Vortex Lightning Trap
							{
								Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
								Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
								if (Mem.ReadByte(curObj + 0x15) != 0)
								{
									if (Mem.ReadS16(0xFFAA12) != 0)
									{
										Ymid -= 8;
									}
									DrawBoxMWH(Xmid, Ymid, 92, 16, Color.Red);
									PutText(Mem.ReadByte(curObj + 0x15).ToString(), Xmid, Ymid, 1, 1, -1, -1, Color.Blue, Color.Red);
								}
								else
								{
									DrawBoxMWH(Xmid, Ymid, 92, 16, Color.Gray);
									PutText(Mem.ReadByte(curObj + 0x7F).ToString(), Xmid, Ymid, 1, 1, -1, -1, Color.Blue, Color.Red);
								}
							}
							else if (type == 0xA6E24) // Barrier Glyph Forcefield 
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = Mem.ReadS32(0xFFAA1A) - Xmid;
								Yvec = Mem.ReadS32(0xFFAA1E) - Ymid;
								var div = Math.Abs(Xvec) + Math.Abs(Yvec);
								Xvec /= div; Yvec /= div;
								Xvec += Xmid; Yvec += Ymid;
								Xmid >>= 16; Ymid >>= 16;
								Xvec >>= 16; Yvec >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								Xvec -= _camX; Yvec -= _camY;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if ((type == 0xC4A44) || (type == 0xAA32C)) // Pulsar power-up and Vortex bullet-spawner
							{
								Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
								Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
								Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
								Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue, 16);
							}
							else if (type == 0xC2F00) // Sky bubbles
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								var mode = Mem.ReadByte(curObj + 0x15);
								switch (mode)
								{
									case 0:
										DrawRectRhombusIntersection(new Point(Xmid, Ymid), new Point(Xmid, Ymid), 70, 70, 105, Color.Gray);
										DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue);
										break;
									case 1:
										DrawRectRhombusIntersection(new Point(Xmid, Ymid), new Point(Xmid, Ymid), 70, 70, 105, Color.Red);
										break;
									case 2:
										DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Red);
										break;
									default:
										DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Gray);
										break;
								}
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0x9FA7E)  //Air refiller/drainer
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.FromArgb(63, Color.Lime));
							}
							else if (type == 0xBFC14) //Pushable fish
							{
								Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
								Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
								Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
								Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue);
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Lime, 0);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								TickerText($"{Mem.ReadS32(curObj + 0x54) / 65536.0:0.######}:{Mem.ReadS32(curObj + 0x58) / 65536.0:0.######}", Color.Orange);

							}
							else if ((type == 0xBE97C)  //Slowing Kelp	//Default Bounds nose-responsive
								  || (type == 0xACDAE))  //Metasphere
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Lime);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xACB42) // Turtle
							{
								Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
								Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
								Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
								Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
								var mode = Mem.ReadByte(curObj + 0x15);
								switch (mode)
								{
									case 0:
									case 1:
									case 2:
									case 3:
										Xvec = ((Xmid + Mem.ReadS32(curObj + 0x4C)) >> 16) - _camX;
										break;
									case 4:
									case 5:
									case 6:
									case 7:
										Xvec = ((Xmid - Mem.ReadS32(curObj + 0x4C)) >> 16) - _camX;
										break;
									default:
										Xvec = (Xmid >> 16) - _camX;
										break;
								}
								Yvec = Ymid;
								Xmid >>= 16; Xmid -= _camX;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Lime);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xpos, Ypos, Xpos2, Ypos2, Color.Lime);
								Gui.DrawLine(Xpos, Ypos2, Xpos2, Ypos, Color.Lime);
								int width = Mem.ReadS16(curObj + 0x44) << 1;
								DrawBox(Xpos - width, Ypos - (width << 2), Xpos2 + width, Ypos2, Color.Lime);
								TickerText($"{Mem.ReadS32(curObj + 0x4C) / 65536.0:0.######}:{Mem.ReadS32(curObj + 0x50) / 65536.0:0.######}", Color.Lime);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xACA7E) // Retracting Turtle
							{
								Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
								Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
								Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
								Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + (Mem.ReadS32(curObj + 0x4C) >> 1)) >> 16) - _camX;
								Yvec = ((Ymid + (Mem.ReadS32(curObj + 0x50) >> 1)) >> 16) - _camY;
								Xmid >>= 16; Xmid -= _camX;
								Ymid >>= 16; Ymid -= _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Lime);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xpos, Ypos, Xpos2, Ypos2, Color.Lime);
								Gui.DrawLine(Xpos, Ypos2, Xpos2, Ypos, Color.Lime);
								int width = Mem.ReadS16(curObj + 0x44) << 1;
								DrawBox(Xpos - width, Ypos - (width << 2), Xpos2 + width, Ypos2, Color.Lime);
								TickerText($"{(Mem.ReadS32(curObj + 0x4C) >> 1) / 65536.0:0.######}:{(Mem.ReadS32(curObj + 0x50) >> 1) / 65536.0:0.######}", Color.Lime);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if ((type == 0xB5134) || (type == 0xA7E0C) //Default Bounds sonar-responsive
								  || (type == 0xAF868) || (type == 0xAF960) || (type == 0xD8E5C) || (type == 0xAA5C6))
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if ((type == 0xACCB4) || (type == 0xACD7E) //Default Baunds non-responsive
								  || (type == 0xD8D96) || (type == 0xA955E) || (type == 0xA92E4) || (type == 0xC05DC) 
								  || (type == 0xC2684))
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Gray);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0x9DE86) // Star Wreath
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								Xpos = Mem.ReadS32(curObj + 0x1C);
								Ypos = Mem.ReadS32(curObj + 0x20);
								Xpos2 = ((Xpos + Mem.ReadS32(curObj + 0x5C)) >> 16) - _camX;
								Ypos2 = ((Ypos + Mem.ReadS32(curObj + 0x60)) >> 16) - _camY;
								Xpos >>= 16; Ypos >>= 16;
								Xpos -= _camX; Ypos -= _camY;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Orange, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								DrawBoxMWH(Xpos, Ypos, 1, 1, (Mem.ReadByte(curObj + 0x7F) == 0) ? Color.Blue : Color.Black, 0);
								Gui.DrawLine(Xpos, Ypos, Xpos2, Ypos2, Color.Orange);
								if (Mem.ReadByte(curObj + 0x12) % 7 == 0)
								{
									TickerText($"{Mem.ReadS32(curObj + 0x5C) / 65536.0:0.######}:{Mem.ReadS32(curObj + 0x60) / 65536.0:0.######}:{Mem.ReadByte(curObj + 0x7F)}", Color.Lime);
								}
							}
							else if ((type == 0x9D774) || (type == 0x9DA26)) // Fish
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								DrawBoxMWH(Xmid, Ymid, 0x14, 0x14, Color.Lime);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xAD87C) // Enemy dolphins in metamorph levels
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								DrawBoxMWH(Mem.ReadS16(curObj + 0x1C) - _camX, Mem.ReadS16(curObj + 0x20) - _camY, 1024, 1024, Color.Orange, 0);
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Red);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xC6128) // Drone attacking dolphin 
							{
								Xmid = Mem.ReadS16(curObj + 0x4C) - _camX;
								Ymid = Mem.ReadS16(curObj + 0x50) - _camY;
								DrawEccoOct(Xmid, Ymid, 360, Color.Red, 0);
							}
							else if (type == 0xC605A) // Drone attacking dolphin sonar
							{
								Xmid = Mem.ReadS16(curObj + 0x24) - _camY;
								Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
								DrawBox(Xmid, Ymid - 1, Xmid + 32, Ymid + 1, Color.Orange);
							}
							else if (type == 0xB1BE0) // Globe
							{
								int mode = Mem.ReadS8(curObj + 0x15);
								if (mode == 1)
								{
									Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
									Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
									Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
									Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
									DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue);
									DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Lime, 0);
									Gui.DrawLine(Xpos, Ypos2, Xpos2, Ypos, Color.Lime);
									Gui.DrawLine(Xpos, Ypos, Xpos2, Ypos2, Color.Lime);
									Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
									Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
									Xmid >>= 16; Ymid >>= 16;
									DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
									Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								}
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xB1A10) // Approaching globes
							{
								Xmid = Mem.ReadS16(0xFFAA1A) - _camX;
								Ymid = Mem.ReadS16(0xFFAA1E) - _camY;
								DrawEccoOct(Xmid - 56, Ymid, 8, Color.Orange);
								DrawEccoOct(Xmid + 56, Ymid, 8, Color.Orange);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xpos = Mem.ReadS32(curObj + 0x4C);
								Ypos = Mem.ReadS32(curObj + 0x50);
								Xvec = ((Xmid - Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid - Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xpos2 = ((Xpos + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Ypos2 = ((Ypos + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								Xpos >>= 16; Ypos >>= 16;
								Xpos -= _camX; Ypos -= _camY;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								DrawBoxMWH(Xpos, Ypos, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xpos, Ypos, Xpos2, Ypos2, Color.Orange);
							}
							else if (type == 0xB1920) // Orbiting globes
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xpos = Mem.ReadS16(curObj + 0x4C) - _camX;
								Ypos = Mem.ReadS16(curObj + 0x50) - _camY;
								Xvec = ((Xmid - Mem.ReadS32(curObj + 0x5C)) >> 16) - _camX;
								Yvec = ((Ymid - Mem.ReadS32(curObj + 0x60)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								DrawBoxMWH(Xpos, Ypos, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xpos, Ypos, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xC28A0) // Control point in Four Islands/Dolphin that gives Rock-breaking song
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Mem.ReadByte(curObj + 0x15) == 0 ? Color.Orange : Color.Blue);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							// Crystal Springs merging glyphs
							else if (type == 0xC651E) // Bound glyph
							{
								Xpos = Mem.ReadS16(curObj + 0x1C) - _camX;
								Ypos = Mem.ReadS16(curObj + 0x20) - _camY;
								DrawEccoRhomb(Xpos, Ypos, 4 << 4, Color.Orange);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
								Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
								Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
								Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xC67E4) // Freed glyph
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
								Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
								Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
								Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Blue);
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue, 0);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xC6970) // Pulled glyph
							{
								uint subObj = Mem.ReadU24(curObj + 5);
								Xvec = Mem.ReadS32(subObj + 0x54);
								Yvec = Mem.ReadS32(subObj + 0x58);
								for (i = 1; i < Mem.ReadByte(curObj + 0x7F); i++)
								{
									Xvec ^= Yvec;
									Yvec ^= Xvec;
									Xvec ^= Yvec;
									Xvec = 0 - Xvec;
								}
								Xpos = (Xvec + Mem.ReadS32(subObj + 0x1C) >> 16) - _camX;
								Ypos = (Yvec + Mem.ReadS32(subObj + 0x20) >> 16) - _camY;
								DrawEccoRhomb(Xpos, Ypos, 3, Color.Orange);
								Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
								Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
								Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
								Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue, 0);
								Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
								Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Orange, 0);
							}
							else if (type == 0xC6BA8) // Delivery Point
							{
								Xvec = Mem.ReadS32(curObj + 0x54);
								Yvec = Mem.ReadS32(curObj + 0x58);
								Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
								Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								for (i = 0; i < 4; i++)
								{
									Xpos = (Xvec + Mem.ReadS32(curObj + 0x1C) >> 16) - _camX;
									Ypos = (Yvec + Mem.ReadS32(curObj + 0x20) >> 16) - _camY;
									Gui.DrawLine(Xmid, Ymid, Xpos, Ypos, Color.Orange);
									Xvec ^= Yvec;
									Yvec ^= Xvec;
									Xvec ^= Yvec;
									Xvec = 0 - Xvec;
								}
							}
							else if (type == 0xC6A6C) // Delivered glyph
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid - Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid - Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xC6F82) // Full delivery point
							{
								Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
								Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
								DrawEccoOct(Xmid, Ymid, 3, Color.Orange);
							}
							else if (type == 0xC6A9E) // Merging glyph
							{
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid - Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid - Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xmid -= _camX; Ymid -= _camY;
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Orange, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
							}
							else if (type == 0xC7052) {
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS16(curObj + 0x24);
								Ymid = Mem.ReadS16(curObj + 0x28);
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								uint dropSpeed = Mem.ReadU8(curObj + 0x16);
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Red);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xmid, Ymid + (int)(dropSpeed), Color.Orange);
							}
							else if ((type == 0xD8B7C) || (type == 0xD89EA) || (type == 0x9E3AA) || (type == 0x9E5A8) // GFX particles don't need displayed.
								  || (type == 0x9B5D8) || (type == 0x9E2A6) || (type == 0xACD1E) || (type == 0xD9678)
								  || (type == 0xD9A3C) || (type == 0xD9240) || (type == 0x9E1DE) || (type == 0xDF86A)
								  || (type == 0xB159A) || (type == 0xDA898) || (type == 0xDA720) || (type == 0xD9FDC)
								  || (type == 0xC0D4E) || (type == 0xC0D38) || (type == 0xDCDAC) || (type == 0xC0B42)
								  || (type == 0xE3CD2) || (type == 0xE385E) || (type == 0xC20E8) || (type == 0xC22A6)
								  || (type == 0xC31B4) || (type == 0xA9EF0) || (type == 0xA9D90) || (type == 0xC6304)
								  || (type == 0xC26E4) || (type == 0xAEE68) || (type == 0xD9B2A) || (type == 0xD95AE)
								  || (type == 0))
							{
								// This space intentionally left blank
							}
							else if ((type == 0xC152C) || (type == 0xA75E8) //Objects with default bounds confirmed
								  || (type == 0x9D076) || (type == 0xA7092) || (type == 0xC02EA) || (type == 0xA5378)
								  || (type == 0xACA7E) || (type == 0x9D28C) || (type == 0xA2D42) || (type == 0xA975E)
								  || (type == 0xBE9C8) || (type == 0xBFDA4) || (type == 0xAC736) || (type == 0xB716E)
								  || (type == 0xB1BE0) || (type == 0xB1A10) || (type == 0x9E546) || (type == 0xC2CB8)
								  || (type == 0xA0F04) || (type == 0xA6ACA) || (type == 0xA35A6) || (type == 0xAA12E)
								  || (type == 0xC651E) || (type == 0x9CC06) || (type == 0xA9202) || (type == 0xA6FDE)
								  || (type == 0xA6F62) || (type == 0xA745C) || (type == 0xC3EF0) || (type == 0xC3F90)
								  || (type == 0xC3FFC) || (type == 0xC3DB8) || (type == 0xAC766) || (type == 0xC5F66)
								  || (type == 0xA306E) || (type == 0xB0C7E) || (type == 0xB17F2) || (type == 0xB0CDC) 
								  || (type == 0xC2106) || (type == 0xC208C) || (type == 0xC1EBA) || (type == 0xC251C) 
								  || (type == 0xC32C8) || (type == 0xAB5E6) || (type == 0xAC796) || (type == 0xAC9F2) 
								  || (type == 0xA538A))
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.FromArgb(255, 0, 127));
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue, 0);
								if (type != 0xA975E)
								{
									DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
									Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								}
								if (HP > 2)
								{
									PutText($"{HP - 1}", Xmid, Ymid, 1, 1, -1, -9, Color.Blue, Color.Red);
								}
							}
							else // Default bounds
							{
								Xpos = Mem.ReadS16(curObj + 0x2C);
								Xpos2 = Mem.ReadS16(curObj + 0x34);
								Ypos = Mem.ReadS16(curObj + 0x30);
								Ypos2 = Mem.ReadS16(curObj + 0x38);
								Xmid = Mem.ReadS32(curObj + 0x24);
								Ymid = Mem.ReadS32(curObj + 0x28);
								Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
								Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
								Xmid >>= 16; Ymid >>= 16;
								Xpos -= _camX; Xpos2 -= _camX;
								Ypos -= _camY; Ypos2 -= _camY;
								Xmid -= _camX; Ymid -= _camY;
								DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue, 0);
								DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
								Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
								PutText(type.ToString("X5"), Xmid, Ymid + 8, 1, 9, -1, -1, Color.Blue, Color.Red);
							}
							break;
						}
					case Modes.Ecco1:
						type = Mem.ReadU32(curObj + 0x6);
						Xpos = Mem.ReadS16(curObj + 0x17);
						Xpos2 = Mem.ReadS16(curObj + 0x1F);
						Ypos = Mem.ReadS16(curObj + 0x1B);
						Ypos2 = Mem.ReadS16(curObj + 0x23);
						Xmid = Mem.ReadS16(curObj + 0x0F);
						Ymid = Mem.ReadS16(curObj + 0x13);
						Xpos >>= 2;
						Xpos2 >>= 2;
						Ypos >>= 2;
						Ypos2 >>= 2;
						Xmid >>= 2;
						Ymid >>= 2;
						Xpos -= _camX; Xpos2 -= _camX;
						Ypos -= _camY; Ypos2 -= _camY;
						Xmid -= _camX; Ymid -= _camY;
                        DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Lime);
						PutText(type.ToString("X8"), Xmid, Ymid, 1, 1, -1, -1, Color.Blue, Color.Red);
						break;
				}
                curObj = Mem.ReadU24(curObj+1);
			}
			//events
			curObj = Mem.ReadU24(0xFFCFB5);
			while (curObj != 0)
			{
				type = Mem.ReadU32(curObj + 0xC);
				if ((type == 0)							   // Null object
				 || (type == 0x9C1FA) || (type == 0x9C6D0) // Skytubes BG Image manager
				 || (type == 0x9ED72) || (type == 0xA57F2) // And Miscellaneous GFX particles
				 || (type == 0xC3A42) || (type == 0xB33D8) // And Cutscene controller
				 || (type == 0xB308A) || (type == 0xA1676) || (type == 0xB6822) || (type == 0xD12E2)) { } 
				else if ((type == 0xA44EE) || (type == 0xD120C))
				{
					Xmid = Mem.ReadS16(curObj + 0x1C) - _camX;
					Ymid = Mem.ReadS16(curObj + 0x20) - _camY;
					DrawEccoOct(Xmid, Ymid, 0x20, Color.Red);
				}
				else if (type == 0x9F0D0) // Water Current
				{
					int Xvec = Mem.ReadS32(curObj + 0x54);
					int Yvec = Mem.ReadS32(curObj + 0x58);
					if ((Xvec != 0) || (Yvec != 0))
					{ 
						Xpos = Mem.ReadS16(curObj + 0x2C);
						Xpos2 = Mem.ReadS16(curObj + 0x34);
						Ypos = Mem.ReadS16(curObj + 0x30);
						Ypos2 = Mem.ReadS16(curObj + 0x38);
						Xmid = Mem.ReadS32(curObj + 0x24);
						Ymid = Mem.ReadS32(curObj + 0x28);
						Xvec += Xmid; Yvec += Ymid;
						Xmid >>= 16; Ymid >>= 16;
						Xvec >>= 16; Yvec >>= 16;
						Xpos -= _camX; Xpos2 -= _camX;
						Ypos -= _camY; Ypos2 -= _camY;
						Xmid -= _camX; Ymid -= _camY;
						Xvec -= _camX; Yvec -= _camY;
						DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.FromArgb(63, Color.Red));
						DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
						Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
					}
				}
				else if (type == 0xDEF94)
				{
					Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
					Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
					DrawEccoOct(Xmid, Ymid, 0x18, Color.Cyan);
				}
				else if (type == 0xA6584) // Eagle
				{
					Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
					Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
					DrawEccoOct(Xmid, Ymid, 0x10, Color.Red);
				}
				else if ((type == 0x9BA8A) // Autoscroller controller
					  || (type == 0xE27D4) || (type == 0xE270E) || (type == 0xE26C2))
				{
					Xmid = Mem.ReadS32(curObj + 0x24);
					Ymid = Mem.ReadS32(curObj + 0x28);
					var Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
					var Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
					Xmid >>= 16; Ymid >>= 16;
					Xmid -= _camX; Ymid -= _camY;
					DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Orange, 0);
					Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
				}
				else if (type == 0x9B948) // Autoscroller waypoint
				{
					Xmid = Mem.ReadS32(curObj + 0x24);
					Ymid = Mem.ReadS32(curObj + 0x28);
					var Xvec = ((Xmid + Mem.ReadS32(curObj + 0x54)) >> 16) - _camX;
					var Yvec = ((Ymid + Mem.ReadS32(curObj + 0x58)) >> 16) - _camY;
					Xmid >>= 16; Ymid >>= 16;
					Xmid -= _camX; Ymid -= _camY;
					DrawBoxMWH(Xmid, Ymid, 8, 8, Color.Orange);
					DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
					Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
				}
				else if (type == 0xA5448) // Bomb spawner
				{
					Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
					Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
					PutText($"{Mem.ReadU16(curObj + 0x6E)}", Xmid, Ymid, 1, 1, -1, -1, Color.White, Color.Blue);
				}
				else if ((type == 0xA529C) || (type == 0xA5236) || (type == 0xA51E6)) // Explosion
				{
					uint subObj = Mem.ReadU24(curObj + 5);
					if (subObj != 0)
					{
						Xpos = Mem.ReadS16(subObj + 0x1C) - _camX;
						Ypos = Mem.ReadS16(subObj + 0x28) - _camY;
						int Width = Mem.ReadS16(subObj + 0x24) - Xpos - _camX;
						DrawBoxMWH(Xpos, Ypos, Width, 16, Color.Red);
					}
					subObj = Mem.ReadU24(curObj + 9);
					if (subObj != 0)
					{
						Xpos = Mem.ReadS16(subObj + 0x1C) - _camX;
						Ypos = Mem.ReadS16(subObj + 0x28) - _camY;
						int Width = Mem.ReadS16(subObj + 0x24) - Xpos - _camX;
						DrawBoxMWH(Xpos, Ypos, Width, 16, Color.Red);
					}
				}
				else if (type == 0x9B5D8)
				{
					var subtype = Mem.ReadByte(curObj + 0x13);
					int width = 0;
					int height = 0;
					switch (subtype)
					{
						case 48:
						case 49:
						case 126:
						case 145:
						case 146:
						case 213:
							PutText($"{type:X5}:{subtype}", Xmid, Ymid - 4, 1, 1, -1, -9, Color.White, Color.Blue);
							PutText(curObj.ToString("X6"), Xmid, Ymid + 4, 1, 9, -1, -1, Color.White, Color.Blue);
							break;
						case 59:
						case 87:
						case 181:
							Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
							Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
							width = Mem.ReadS16(curObj + 0x44);
							height = Mem.ReadS16(curObj + 0x48);
							DrawEccoOct(Xmid, Ymid, (width + height) >> 1, Color.Lime);
							break;
						case 71:
						case 72:
						case 158:
						case 159:
						case 165:
							Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
							Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
							Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
							Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
							DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Red);
							break;
						case 82:
						case 83:
						case 84:
						case 85:
						case 86:
							Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
							Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
							width = Mem.ReadS16(curObj + 0x44);
							height = Mem.ReadS16(curObj + 0x48);
							DrawBoxMWH(Xmid, Ymid, width, height, Color.Red);
							break;
						case 210:
							Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
							Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
							width = Mem.ReadS16(curObj + 0x44);
							height = Mem.ReadS16(curObj + 0x48);
							DrawBoxMWH(Xmid, Ymid, width, height, Color.Blue);
							break;
						case 107:
							Xmid = (Mem.ReadS16(curObj + 0x18) << 7) - _camX + 0x40;
							Ymid = (Mem.ReadS16(curObj + 0x1A) << 7) - _camY + 0x40;
							Xpos = (Mem.ReadS16(curObj + 0x64) << 7) - _camX + 0x40;
							Ypos = (Mem.ReadS16(curObj + 0x66) << 7) - _camY + 0x40;
							DrawBoxMWH(Xmid, Ymid, 64, 64, Color.Orange, 0);
							DrawBoxMWH(Xpos, Ypos, 64, 64, Color.Orange, 0);
							DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
							Gui.DrawLine(Xmid, Ymid, Xpos, Ypos, Color.Orange);
							break;
						case 110: //Gravity conrol points
						case 179:
							Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
							Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
							Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
							Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
							DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.FromArgb(63,Mem.ReadByte(curObj + 0x15) == 0 ? Color.Gray : Color.Red));
							DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Red, 0);
							int dir = Mem.ReadS8(curObj + 0x71) & 7;
							int[] xtable = {  7, 4, -3, -10, -14, -11,  -3,   4};
							int[] ytable = { 11, 4,  7,   4,  -3, -11, -14, -11};
							Xmid = Mem.ReadS16(curObj + 0x24) - _camX;
							Ymid = Mem.ReadS16(curObj + 0x28) - _camY;
							DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
							Gui.DrawImage(".\\dll\\gravitometer_bg.png", Xmid - 15, Ymid - 15);
							Gui.DrawImage(".\\dll\\gravitometer_fg.png", Xmid + xtable[dir], Ymid + ytable[dir]);
							break;
						case 176:
							Xmid = (Mem.ReadS16(curObj + 0x18) << 7) - _camX + 0x40;
							Ymid = (Mem.ReadS16(curObj + 0x1A) << 7) - _camY + 0x40;
							Xpos = (Mem.ReadS16(curObj + 0x64) << 7) - _camX + 0x40;
							Ypos = (Mem.ReadS16(curObj + 0x66) << 7) - _camY + 0x40;
							DrawEccoOct(Xmid, Ymid, 32, Color.Orange, 0);
							DrawEccoOct(Xpos, Ypos, 32, Color.Orange, 0);
							DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue, 0);
							Gui.DrawLine(Xmid, Ymid, Xpos, Ypos, Color.Orange);
							break;
						case 194: // Kill plane
							Xpos = Mem.ReadS16(curObj + 0x2C) - _camX;
							Xpos2 = Mem.ReadS16(curObj + 0x34) - _camX;
							Ypos = Mem.ReadS16(curObj + 0x30) - _camY;
							Ypos2 = Mem.ReadS16(curObj + 0x38) - _camY;
							DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Black, 127);
							DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Red, 0);
							break;
						default:
							Xpos = Mem.ReadS16(curObj + 0x2C);
							Xpos2 = Mem.ReadS16(curObj + 0x34);
							Ypos = Mem.ReadS16(curObj + 0x30);
							Ypos2 = Mem.ReadS16(curObj + 0x38);
							Xmid = Mem.ReadS16(curObj + 0x24);
							Ymid = Mem.ReadS16(curObj + 0x28);
							Xpos -= _camX; Xpos2 -= _camX;
							Ypos -= _camY; Ypos2 -= _camY;
							Xmid -= _camX; Ymid -= _camY;
							DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Cyan);
							PutText($"{type:X5}:{subtype}", Xmid, Ymid - 4, 1, 1, -1, -9, Color.White, Color.Blue);
							PutText(curObj.ToString("X6"), Xmid, Ymid + 4, 1, 9, -1, -1, Color.White, Color.Blue);
							break;
					}
				}
				else
				{
					Xpos =  Mem.ReadS16(curObj + 0x2C);
					Xpos2= Mem.ReadS16(curObj + 0x34);
					Ypos = Mem.ReadS16(curObj + 0x30);
					Ypos2= Mem.ReadS16(curObj + 0x38);
					Xmid = Mem.ReadS16(curObj + 0x24);
					Ymid = Mem.ReadS16(curObj + 0x28);
					Xpos -= _camX; Xpos2 -= _camX;
					Ypos -= _camY; Ypos2 -= _camY;
					Xmid -= _camX; Ymid -= _camY;
					DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.Cyan);
					PutText($"{type:X5}:{Mem.ReadByte(curObj + 0x13)}", Xmid, Ymid - 4, 1, 1, -1, -9, Color.White, Color.Blue);
					PutText(curObj.ToString("X6"), Xmid, Ymid + 4, 1, 9, -1, -1, Color.White, Color.Blue);
				}
				curObj = Mem.ReadU24(curObj+1);
			}
			//Ecco head
			Xpos = Mem.ReadS16(0xFFA8F8);
			Xpos2 = Mem.ReadS16(0xFFA900);
			Ypos = Mem.ReadS16(0xFFA8FC);
			Ypos2 = Mem.ReadS16(0xFFA904);
			Xmid = Mem.ReadS16(0xFFA8F0);
			Ymid = Mem.ReadS16(0xFFA8F4);
			Xpos -= _camX; Xpos2 -= _camX;
			Ypos -= _camY; Ypos2 -= _camY;
			Xmid -= _camX; Ymid -= _camY;
			DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue);
			DrawBoxMWH(Xmid, Ymid, 1, 1, Color.PowderBlue, 0);
			//Ecco tail
			Xpos = Mem.ReadS16(0xFFA978);
			Xpos2 = Mem.ReadS16(0xFFA980);
			Ypos = Mem.ReadS16(0xFFA97C);
			Ypos2 = Mem.ReadS16(0xFFA984);
			Xmid = Mem.ReadS16(0xFFA970);
			Ymid = Mem.ReadS16(0xFFA974);
			Xpos -= _camX; Xpos2 -= _camX;
			Ypos -= _camY; Ypos2 -= _camY;
			Xmid -= _camX; Ymid -= _camY;
			DrawBox(Xpos, Ypos, Xpos2, Ypos2, Color.PowderBlue);
			DrawBoxMWH(Xmid, Ymid, 1, 1, Color.PowderBlue, 0);
			//Ecco body
			Xpos = Mem.ReadS32(0xFFAA22);
			Ypos = Mem.ReadS32(0xFFAA26);
			Xpos2 = Mem.ReadS32(0xFFAA2A);
			Ypos2 = Mem.ReadS32(0xFFAA2E);
			Xmid = Mem.ReadS16(0xFFAA1A);
			Ymid = Mem.ReadS16(0xFFAA1E);
			int Xvel = Mem.ReadS32(0xFFAA36);
			if (Mem.ReadU32(0xFFA9D6) > 7) Xvel += Mem.ReadS32(0xFFA9D6);
			if (Mem.ReadU32(0xFFAA3E) > 7) Xvel += Mem.ReadS32(0xFFAA3E);
			int Yvel = Mem.ReadS32(0xFFAA3A);
			if (Mem.ReadU32(0xFFA9DA) > 7) Yvel += Mem.ReadS32(0xFFA9DA);
			if (Mem.ReadU32(0xFFAA42) > 7) Yvel += Mem.ReadS32(0xFFAA42);
			int XV = ((Xpos + Xvel) >> 16) - _camX;
			int YV = ((Ypos + Yvel) >> 16) - _camY;
			int XV2 = ((Xpos2 + Xvel) >> 16) - _camX;
			int YV2 = ((Ypos2 + Yvel) >> 16) - _camY;
			X = Xpos >> 16;
			X2 = Xpos2 >> 16;
			Y = Ypos >> 16;
			Y2 = Ypos2 >> 16;
			X -= _camX; X2 -= _camX;
			Y -= _camY; Y2 -= _camY;
			Xmid -= _camX; Ymid -= _camY;
			int X3 = (Xmid + X) >> 1;
			int X4 = (Xmid + X2) >> 1;
			int Y3 = (Ymid + Y) >> 1;
			int Y4 = (Ymid + Y2) >> 1;
			Gui.DrawLine(X, Y, Xmid, Ymid, Color.Green);
			Gui.DrawLine(Xmid, Ymid, X2, Y2, Color.Green);
			DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Red);
			DrawBoxMWH(X, Y, 1, 1, Color.Lime);
			DrawBoxMWH(X2, Y2, 1, 1, Color.Blue);
			DrawBoxMWH(X3, Y3, 1, 1, Color.Yellow);
			DrawBoxMWH(X4, Y4, 1, 1, Color.Yellow);
			Gui.DrawLine(X, Y, XV, YV, Color.Orange);
			Gui.DrawLine(X2, Y2, XV2, YV2, Color.Orange);
			// sonar
			if (Mem.ReadU8(0xFFAB77) != 0)
			{
				Xmid = Mem.ReadS32(0xFFA9EC);
				Ymid = Mem.ReadS32(0xFFA9F0);
				int Xvec = ((Mem.ReadS32(0xFFAA04) + Xmid) >> 16) - _camX;
				int Yvec = ((Mem.ReadS32(0xFFAA08) + Ymid) >> 16) - _camY;
				Xmid >>= 16;
				Ymid >>= 16;
				Xmid -= _camX; Ymid -= _camY;
				Width2 = Mem.ReadS16(0xFFA9FC);
				Height2 = Mem.ReadS16(0xFFAA00);
				color = ((Mem.ReadU8(0xFFAA0C) != 0) ? Color.FromArgb(255, 0, 127) : Color.FromArgb(0, 0, 255));
				DrawBoxMWH(Xmid, Ymid, Width2, Height2, color);
				DrawBoxMWH(Xmid, Ymid, 1, 1, color, 0);
				Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
			}
			//Pulsar
			curObj = Mem.ReadU24(0xFFCFD1);
			if ((curObj != 0) && ((Mem.ReadU32(curObj + 0xC) == 0xC9222) || (Mem.ReadU32(curObj + 0xC) == 0xC9456)))
			{
				curObj += 0x26;
				for (int l = 0; l < 4; l++)
				{
					if (Mem.ReadU16(curObj + 0x12) != 0)
					{
						Xmid = Mem.ReadS32(curObj);
						Ymid = Mem.ReadS32(curObj + 4);
						int Xvec = (Xmid + Mem.ReadS32(curObj + 8) >> 16) - _camX;
						int Yvec = (Ymid + Mem.ReadS32(curObj + 0xC) >> 16) - _camY;
						Xmid >>= 16; Ymid >>= 16;
						Xmid -= _camX; Ymid -= _camY;
						DrawBoxMWH(Xmid, Ymid, 0x30, 0x30, Color.Red);
						DrawBoxMWH(Xmid, Ymid, 1, 1, Color.Blue);
						Gui.DrawLine(Xmid, Ymid, Xvec, Yvec, Color.Orange);
					}
					curObj += 0x14;
				}
			}

			//Water Level
			int waterLevel = Mem.ReadS16(0xFFA7B2);
			Gui.DrawLine(0, waterLevel - _camY, _left + 320 + _right, waterLevel - _camY, Color.Aqua);

		}

		void EccoAutofire(bool on)
		{
			//Modif N - ECCO HACK - make caps lock (weirdly) autofire player 1's C key
			uint charge;
			uint mode = Mem.ReadU8(0xFFA555);
			int frameCount = Emu.FrameCount();
			int lagCount = Emu.LagCount();
			Joy.Set("Start", on, 1);
			switch (mode)
			{
				case 0x00:
					if (on)
					{
						if (Mem.ReadU16(0xFFF342) == 0xFFFD)
							Joy.Set("C", true, 1);
						else
							Joy.Set("C", false, 1);
					}
					break;
				case 0xE6:
					if (Mem.ReadU16(0xFFD5E8) == 0x00000002) {
						Dictionary<string, bool> buttons = new Dictionary<string, bool>();
						buttons["B"] = buttons["C"] = true;
						Joy.Set(buttons, 1);
					}
					else
					{
						Dictionary<string, bool> buttons = new Dictionary<string, bool>();
						buttons["B"] = buttons["C"] = false;
						Joy.Set(buttons, 1);
					}
					break;
				case 0xF6:
					charge = Mem.ReadU8(0xFFB19B);
					if (on)
					{
						if ((charge <= 1) && ((Mem.ReadU8(0xFFB1A6) == 0) || (Mem.ReadU8(0xFFB1A9) != 0)))
							Joy.Set("B", true, 1);
						else if (charge > 1)
							Joy.Set("B", false, 1);
						Joy.Set("C", (Mem.ReadU16(0xFFA7C8) % 2) == 0, 1);
					}
					break;
				case 0x20:
				case 0x28:
				case 0xAC:
					if (on)
					{
						if ((Mem.ReadU8(0xFFAB72) & 3) == 0)
							Joy.Set("C", (Mem.ReadS8(0xFFAA6E) < 11), 1);
					}
					break;
				default:
					break;
			}
		}
		public override void Init(IApiContainer api) 
		{
			base.Init(api);
			Mem.SetBigEndian();
			string gameName = GI.GetRomName();
			if ((gameName == "ECCO - The Tides of Time (J) [!]") ||
				(gameName == "ECCO - The Tides of Time (U) [!]") ||
				(gameName == "ECCO - The Tides of Time (E) [!]"))
			{
				_mode = Modes.Ecco2;
				_camXAddr = 0xFFAD9C;
				_camYAddr = 0xFFAD9E;
				_top = _bottom = 112;
				_left = _right = 160;
				ClientApi.SetGameExtraPadding(_left, _top, _right, _bottom);
            }
			else if ((gameName == "ECCO The Dolphin (J) [!]") ||
					 (gameName == "ECCO The Dolphin (UE) [!]"))

			{
				_mode = Modes.Ecco1;
				_camXAddr = 0xFFB836;
				_camYAddr = 0xFFB834;
				_top = _bottom = 112;
				_left = _right = 160;
				ClientApi.SetGameExtraPadding(_left, _top, _right, _bottom);
			}
			else
			{
				_mode = Modes.disabled;
				Running = false;
			}
		}
		private Color BackdropColor()
		{
			uint color = Mem.ReadU16(0, "CRAM");
			int r = (int)(( color       & 0x7) * 0x22);
			int g = (int)(((color >> 3) & 0x7) * 0x22);
			int b = (int)(((color >> 6) & 0x7) * 0x22);
			return Color.FromArgb(r, g, b);
		}
		public override void PreFrameCallback()
		{
			Gui.ClearText();
			if (_mode != Modes.disabled)
			{
				_camX = Mem.ReadS16(_camXAddr) - _left;
				_camY = Mem.ReadS16(_camYAddr) - _top;
				EccoAutofire(Joy.Get(1)["Start"]);
				if (_dumpMap == 0)
				{
					Color bg = BackdropColor();
					Gui.DrawRectangle(0, 0, _left + 320 + _right, _top, bg, bg);
					Gui.DrawRectangle(0, 0, _left, _top + 224 + _bottom, bg, bg);
					Gui.DrawRectangle(_left + 320, 0, _left + 320 + _right, _top + 224 + _bottom, bg, bg);
					Gui.DrawRectangle(0, _top + 224, _left + 320 + _right, _top + 224 + _bottom, bg, bg);
				}
				uint mode = Mem.ReadByte(0xFFA555);
				switch (mode)
				{
					case 0x20:
					case 0x28:
					case 0xAC:
						//ClientApi.SetGameExtraPadding(160, 112, 160, 112);
						if (_dumpMap <= 1) EccoDrawBoxes();
						// Uncomment the following block to enable mapdumping
						if ((Mem.ReadU16(0xFFA7C8) > 1) && (Mem.ReadU16(0xFFA7C8) < 4))
						{
							_dumpMap = 1;
							_rowStateGuid = string.Empty;
							_top = _bottom = _left = _right = 0;
							ClientApi.SetGameExtraPadding(0, 0, 0, 0);
						}
						if (_dumpMap == 3)
						{
							var levelID = Mem.ReadS8(0xFFA7D0);
							int[] nameGroupLengths =
							{
								7,1,11,6,
								4,3,3,3,
								7,1,2,1,
								0,0,0,0
							};
							int[] nameStringPtrOffsets =
							{
								0xECBD0, 0x106BC0, 0x10AF8C, 0x135A48,
								0x1558E8, 0x15F700, 0x16537C, 0x180B00,
								0x193920, 0x1B3ECC, 0x1D7A44, 0x1DBF70,
								0x2DF2, 0x2DF6, 0x2DFA, 0x2DFE
							};
							int nameGroup = 0;
							var i = levelID;
							while ((i >= 0) && (nameGroup < nameGroupLengths.Length))
							{
								i -= nameGroupLengths[nameGroup];
								if (i >= 0) nameGroup++;
							}
							string name = "map";
							if (i < 0)
							{
								i += nameGroupLengths[nameGroup];
								uint strOffset = Mem.ReadU32(nameStringPtrOffsets[nameGroup] + 0x2E);
								Console.WriteLine($"{i}");
								strOffset = Mem.ReadU32(strOffset + ((i << 3) + (i << 5)) + 0x22);
								strOffset += 0x20;
								List<byte> strTmp = new List<byte>();
								byte c;
								do
								{
									c = (byte)Mem.ReadByte(strOffset++);
									if (c != 0)
										strTmp.Add(c);
								} while (c != 0);
								name = System.Text.Encoding.ASCII.GetString(strTmp.ToArray());
								TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
								name = textInfo.ToTitleCase(name).Replace(" ", string.Empty);
							}
							ClientApi.Screenshot($"c:\\Ecco2Maps\\{levelID}_{name}_top.png");
							_destX = _destY = 0;
							ClientApi.SetGameExtraPadding(0, 0, 0, 0);
							_dumpMap++;
						}
						if (_dumpMap == 6)
						{
							var levelID = Mem.ReadS8(0xFFA7D0);
							int[] nameGroupLengths =
							{
								7,1,11,6,
								4,3,3,3,
								7,1,2,1,
								0,0,0,0
							};
							int[] nameStringPtrOffsets =
							{
								0xECBD0, 0x106BC0, 0x10AF8C, 0x135A48,
								0x1558E8, 0x15F700, 0x16537C, 0x180B00,
								0x193920, 0x1B3ECC, 0x1D7A44, 0x1DBF70,
								0x2DF2, 0x2DF6, 0x2DFA, 0x2DFE
							};
							int nameGroup = 0;
							var i = levelID;
							while ((i >= 0) && (nameGroup < nameGroupLengths.Length))
							{
								i -= nameGroupLengths[nameGroup];
								if (i >= 0) nameGroup++;
							}
							string name = "map";
							if (i < 0)
							{
								i += nameGroupLengths[nameGroup];
								uint strOffset = Mem.ReadU32(nameStringPtrOffsets[nameGroup] + 0x2E);
								Console.WriteLine($"{i}");
								strOffset = Mem.ReadU32(strOffset + ((i << 3) + (i << 5)) + 0x22);
								strOffset += 0x20;
								List<byte> strTmp = new List<byte>();
								byte c;
								do
								{
									c = (byte)Mem.ReadByte(strOffset++);
									if (c != 0)
										strTmp.Add(c);
								} while (c != 0);
								name = System.Text.Encoding.ASCII.GetString(strTmp.ToArray());
								TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
								name = textInfo.ToTitleCase(name).Replace(" ", string.Empty);
							}
							ClientApi.Screenshot($"c:\\Ecco2Maps\\{levelID}_{name}_bottom.png");
							_destX = _destY = 0;
							_left = _right = 160;
							_top = _bottom = 112;
							ClientApi.SetGameExtraPadding(_left, _top, _right, _bottom);
							_dumpMap = 0;
						}
						break;
					case 0xF6:
						EccoDraw3D();
						break;
					default:
						break;
				}
			_prevF = Mem.ReadU32(0xFFA524);
			}
		}
		public override void PostFrameCallback()
		{
			uint frame = Mem.ReadU32(0xFFA524);
			if ((frame <= _prevF) && !Emu.IsLagged())
			{
				Emu.SetIsLagged(true);
				Emu.SetLagCount(Emu.LagCount() + 1);
			}
			uint mode = Mem.ReadByte(0xFFA555);
			_tickerY = 81;
			string valueTicker = $"{Mem.ReadU32(0xFFA520)}:{Mem.ReadU32(0xFFA524)}:{Mem.ReadU16(0xFFA7C8)}:{mode:X2}";
			TickerText(valueTicker);
			switch (mode)
			{
				case 0x20:
				case 0x28:
				case 0xAC:
					valueTicker = $"{Mem.ReadS16(0xFFAD9C)}:{Mem.ReadS16(0xFFAD9E)}";
					TickerText(valueTicker);
					valueTicker = $"{Mem.ReadS32(0xFFAA1A) / 65536.0:0.######}:{Mem.ReadS32(0xFFAA1E) / 65536.0:0.######}";
					TickerText(valueTicker);
					valueTicker = $"{Mem.ReadS32(0xFFAA32) / 65536.0:0.######}:{Mem.ReadU8(0xFFAA6D)}:{Mem.ReadU8(0xFFAA6E)}";
					TickerText(valueTicker);
					valueTicker = $"{Mem.ReadS32(0xFFAA36) / 65536.0:0.######}:{Mem.ReadS32(0xFFAA3A) / 65536.0:0.######}";
					TickerText(valueTicker);
					valueTicker = $"{Mem.ReadS32(0xFFA9D6) / 65536.0:0.######}:{Mem.ReadS32(0xFFA9DA) / 65536.0:0.######}";
					TickerText(valueTicker);
					valueTicker = $"{Mem.ReadS32(0xFFAA3E) / 65536.0:0.######}:{Mem.ReadS32(0xFFAA42) / 65536.0:0.######}";
					TickerText(valueTicker);
					valueTicker = $"{(Mem.ReadS32(0xFFAA36) + Mem.ReadS32(0xFFA9D6) + Mem.ReadS32(0xFFAA3E)) / 65536.0:0.######}:" +
								  $"{(Mem.ReadS32(0xFFAA3A) + Mem.ReadS32(0xFFA9DA) + Mem.ReadS32(0xFFAA42)) / 65536.0:0.######}";
					TickerText(valueTicker);
					valueTicker = $"{Mem.ReadU8(0xFFAB72)}:{Mem.ReadU8(0xFFAB70)}:{(short)Mem.ReadS16(0xFFAA52):X4}:{(short)Mem.ReadS16(0xFFAA5A):X4}";
					TickerText(valueTicker);
					switch (Mem.ReadU8(0xFFA7D0))
					{
						case 1:
						case 2:
						case 3:
						case 30:
						case 46:
							var globeFlags = Mem.ReadU32(0xFFD434) >> 1;
							var globeFlags2 = Mem.ReadU32(0xFFD438) >> 1;
							int i, j = i = 0;
							while (globeFlags > 0)
							{
								globeFlags >>= 1;
								i++;
							}
							while (globeFlags2 > 0)
							{
								globeFlags2 >>= 1;
								j++;
							}
							TickerText($"{i}:{j}", Color.Blue);
							break;
						default:
							break;
					}
					if (_dumpMap != 0)
					{
						Mem.WriteS16(0xFFAA16, 7);
						Mem.WriteS16(0xFFAA18, 56);
						int PlayerX = Mem.ReadS16(0xFFAA1A) - _camX;
						int PlayerY = Mem.ReadS16(0xFFAA1E) - _camY;
						if ((PlayerX < -64) || (PlayerX > 384) || (PlayerY < -64) || (PlayerY > 288))
						{
							Mem.WriteByte(0xFFAA70, 0xC);
							Mem.WriteS16(0xFFA7CA, 1);
						}
						else
						{
							Mem.WriteByte(0xFFAA70, 0x0);
							Mem.WriteS16(0xFFA7CA, 0);
						}
					}
					if (_dumpMap == 1)
					{
						int levelWidth = Mem.ReadS16(0xFFA7A8);
						int levelHeight = Mem.ReadS16(0xFFA7AC);
						var levelID = Mem.ReadByte(0xFFA7D0);
						var s = Emu.GetSettings() as GPGX.GPGXSettings;
						s.DrawBGA = false;
						s.DrawBGB = false;
						s.DrawBGW = false;
						s.DrawObj = false;
						s.Backdrop = true;
						Emu.PutSettings(s);
						if ((_camX == _destX) && (_camY == _destY))
						{
							if ((_prevX != _camX) || (_prevY != _camY))
							{
								if (_destX == 0)
								{
									if (_rowStateGuid != string.Empty)
									{
										MemSS.DeleteState(_rowStateGuid);
									}
									_rowStateGuid = MemSS.SaveCoreStateToMemory();
								}
								_snapPast = 1;
							}
							else
							{
								_snapPast--;
							}
							if (_snapPast == 0)
							{
								ClientApi.Screenshot($"c:\\Ecco2Maps\\{levelID}\\{_destY}_{_destX}_top.png");
								if (_destX >= levelWidth - 320)
								{
									if (_destY < levelHeight - 224)
									{
										if (_rowStateGuid != string.Empty)
										{
											MemSS.LoadCoreStateFromMemory(_rowStateGuid);
										}
										_destX = 0;
										_destY = Math.Min(_destY + 111, levelHeight - 224);
									}
								}
								else
									_destX = Math.Min(_destX + 159, levelWidth - 320); 
								if ((_prevX == _destX) && (_prevY == _destY))
								{
									ClientApi.SetGameExtraPadding(levelWidth - 320, levelHeight - 224, 0, 0);
									_dumpMap++;
								}
							}
						}
						Mem.WriteS16(0xFFAD8C, _destX);
						Mem.WriteS16(0xFFAD90, _destY);
					}
					else if (_dumpMap == 2)
					{
						if (_rowStateGuid != String.Empty)
							MemSS.DeleteState(_rowStateGuid);
						_rowStateGuid = String.Empty;
						int levelWidth = Mem.ReadS16(0xFFA7A8);
						int levelHeight = Mem.ReadS16(0xFFA7AC);
						ClientApi.SetGameExtraPadding(levelWidth - 320, levelHeight - 224, 0, 0);
						var levelID = Mem.ReadS8(0xFFA7D0);
						var s = Emu.GetSettings() as GPGX.GPGXSettings;
						s.DrawBGA = false;
						s.DrawBGB = false;
						s.DrawBGW = false;
						s.DrawObj = false;
						s.Backdrop = true;
						Emu.PutSettings(s);

						var a = Gui.GetAttributes();
						a.SetColorKey(Color.FromArgb(0, 0x11, 0x22, 0x33), Color.FromArgb(0, 0x11, 0x22, 0x33));
						Gui.SetAttributes(a);
						Gui.ToggleCompositingMode();
						
						Gui.DrawImageRegion($"c:\\Ecco2Maps\\{levelID}\\{levelHeight - 224}_{levelWidth - 320}_top.png", 2, 2, 318, 222, (levelWidth - 318), (levelHeight - 222), 318, 222);
						for (int x = ((levelWidth - 320) / 159) * 159; x >= 0; x -= 159)
						{
							var dx = (x == 0) ? 0 : 2;
							Gui.DrawImageRegion($"c:\\Ecco2Maps\\{levelID}\\{levelHeight - 224}_{x}_top.png", dx, 2, 320 - dx, 222, x + dx, (levelHeight - 222), 320 - dx, 222);
						}
						for (int y = ((levelHeight - 224) / 111) * 111; y >= 0; y -= 111)
						{
							var dy = (y == 0) ? 0 : 2;
							Gui.DrawImageRegion($"c:\\Ecco2Maps\\{levelID}\\{y}_{levelWidth - 320}_top.png", 2, dy, 318, 224 - 2, levelWidth - 318, y + dy, 318, 224 - dy);
							for (int x = ((levelWidth - 320) / 159) * 159; x >= 0; x -= 159)
							{
								var dx = (x == 0) ? 0 : 2;
								Gui.DrawImageRegion($"c:\\Ecco2Maps\\{levelID}\\{y}_{x}_top.png", dx, dy, 320 - dx, 224 - dy, x + dx, y + dy, 320 - dx, 224 - dy);
							}
						}

						Gui.ToggleCompositingMode();
						Gui.SetAttributes(new System.Drawing.Imaging.ImageAttributes());
						Gui.DrawFinish();
						_dumpMap++;
					}
					else if (_dumpMap == 4)
					{
						int levelWidth = Mem.ReadS16(0xFFA7A8);
						int levelHeight = Mem.ReadS16(0xFFA7AC);
						var levelID = Mem.ReadByte(0xFFA7D0);
						var s = Emu.GetSettings() as GPGX.GPGXSettings;
						s.DrawBGA = (levelID != 29);
						s.DrawBGB = (levelID == 7);
						s.DrawBGW = true;
						s.DrawObj = true;
						s.Backdrop = false;
						Emu.PutSettings(s);
						if ((_camX == _destX) && (_camY == _destY))
						{
							if ((_prevX != _camX) || (_prevY != _camY))
							{
								if (_destX == 0)
								{
									if (_rowStateGuid != string.Empty)
									{
										MemSS.DeleteState(_rowStateGuid);
									}
									_rowStateGuid = MemSS.SaveCoreStateToMemory();
								}
								_snapPast = 1;
							}
							else
							{
								_snapPast--;
							}
							if (_snapPast == 0)
							{
								ClientApi.Screenshot($"c:\\Ecco2Maps\\{levelID}\\{_destY}_{_destX}_bottom.png");
								if (_destX >= levelWidth - 320)
								{
									if (_destY < levelHeight - 224)
									{
										if (_rowStateGuid != string.Empty)
										{
											MemSS.LoadCoreStateFromMemory(_rowStateGuid);
										}
										_destX = 0;
										_destY = Math.Min(_destY + 111, levelHeight - 224);
									}
								}
								else
									_destX = Math.Min(_destX + 159, levelWidth - 320);
								if ((_prevX == _destX) && (_prevY == _destY))
								{
									ClientApi.SetGameExtraPadding(levelWidth - 320, levelHeight - 224, 0, 0);
									_dumpMap++;
								}
							}
						}
						Mem.WriteS16(0xFFAD8C, _destX);
						Mem.WriteS16(0xFFAD90, _destY);
					}
					else if (_dumpMap == 5)
					{
						if (_rowStateGuid != String.Empty)
							MemSS.DeleteState(_rowStateGuid);
						_rowStateGuid = String.Empty;
						int levelWidth = Mem.ReadS16(0xFFA7A8);
						int levelHeight = Mem.ReadS16(0xFFA7AC);
						var levelID = Mem.ReadS8(0xFFA7D0);
						var s = Emu.GetSettings() as GPGX.GPGXSettings;
						s.DrawBGA = (levelID != 29);
						s.DrawBGB = (levelID == 7);
						s.DrawBGW = true;
						s.DrawObj = true;
						s.Backdrop = false;
						Emu.PutSettings(s);
						Gui.DrawImageRegion($"c:\\Ecco2Maps\\{levelID}\\{levelHeight - 224}_{levelWidth - 320}_bottom.png", 2, 2, 318, 222, (levelWidth - 318), (levelHeight - 222), 318, 222);
						for (int x = ((levelWidth - 320) / 159) * 159; x >= 0; x -= 159)
						{
							var dx = (x == 0) ? 0 : 2;
							Gui.DrawImageRegion($"c:\\Ecco2Maps\\{levelID}\\{levelHeight - 224}_{x}_bottom.png", dx, 2, 320 - dx, 222, x + dx, (levelHeight - 222), 320 - dx, 222);
						}
						for (int y = ((levelHeight - 224) / 111) * 111; y >= 0; y -= 111)
						{
							var dy = (y == 0) ? 0 : 2;
							Gui.DrawImageRegion($"c:\\Ecco2Maps\\{levelID}\\{y}_{levelWidth - 320}_bottom.png", 2, dy, 318, 224 - 2, levelWidth - 318, y + dy, 318, 224 - dy);
							for (int x = ((levelWidth - 320) / 159) * 159; x >= 0; x -= 159)
							{
								var dx = (x == 0) ? 0 : 2;
								Gui.DrawImageRegion($"c:\\Ecco2Maps\\{levelID}\\{y}_{x}_bottom.png", dx, dy, 320 - dx, 224 - dy, x + dx, y + dy, 320 - dx, 224 - dy);
							}
						}
						Gui.DrawFinish();
						_dumpMap++;
					}
					_prevX = _camX;
					_prevY = _camY;
					break;
				case 0xF6:
					valueTicker = $"{Mem.ReadS32(0xFFD5E0) / 4096.0:0.######}:{Mem.ReadS32(0xFFD5E8) / 4096.0:0.######}:{Mem.ReadS32(0xFFD5E4) / 2048.0:0.######}";
					TickerText(valueTicker);
					valueTicker = $"{Mem.ReadS32(0xFFB13A) / 4096.0:0.######}:{Mem.ReadS32(0xFFB142) / 4096.0:0.######}:{Mem.ReadS32(0xFFB13E) / 2048.0:0.######}";
					TickerText(valueTicker);
					valueTicker = $"{Mem.ReadS32(0xFFB162) / 4096.0:0.######}:{Mem.ReadS32(0xFFB16A) / 4096.0:0.######}:{Mem.ReadS32(0xFFB166) / 2048.0:0.######}";
					TickerText(valueTicker);
					valueTicker = $"{Mem.ReadU8(0xFFB19B)}:{Mem.ReadU8(0xFFB1A6)}:{Mem.ReadU8(0xFFB1A9)}";
					TickerText(valueTicker);
					int SpawnZ = Mem.ReadS32(0xFFD5F0) + 0x180000;
					int nextRingZ = SpawnZ;
					while (((nextRingZ >> 17) & 0xF) != 0)
					{
						nextRingZ += 0x20000;
					}
					valueTicker = $"{Mem.ReadS32(0xFFD856) / 4096.0:0.######}:{Mem.ReadS32(0xFFD85A) / 4096.0:0.######}:{(nextRingZ - 0x160000) / 2048.0:0.######}:{nextRingZ / 2048.0:0.######}";
					TickerText(valueTicker);
					var levelId = -1 - Mem.ReadS16(0xFFA79E);
					bool spawn = false;
					bool firstRand = true;
					int SpawnX, SpawnY, z;
					int CamX = (Mem.ReadS32(0xFFD5E0) >> 0xC) - _left;
					int CamY = (Mem.ReadS32(0xFFD5E8) >> 0xC) + _top;
					int CamZ = (Mem.ReadS32(0xFFD5E4) >> 0xC) + _top;
					while (!spawn)
					{
						var temp = (SpawnZ >> 17) & 0xFF;
						var controlList = Mem.ReadS32(0x7B54 + (levelId << 2));
						temp = Mem.ReadS16(controlList + (temp << 1));
						var v = temp & 0xFF;
						var num = (temp >> 8) + v;
						temp = v;
						spawn = (num > 2);
						if (spawn) for (; temp < num; temp++)
						{
							switch (temp)
							{
								case 0:
								case 1:
								case 13:
									// Nothing important spawns
									break;
								case 2:
									// Jellyfish
									SpawnX = Mem.ReadS32(0xFFB13A) + 0x40000 - (EccoRand(firstRand) << 3);
									firstRand = false;
									SpawnY = -0xC0000 + (EccoRand() << 3);
									z = SpawnZ + 0x20000;// ? 
									valueTicker = $"{SpawnX / 4096.0:0.######}:{SpawnY / 4096.0:0.######}:{(z - 0x180000) / 2048.0:0.######}:{z / 2048.0:0.######}";
									TickerText(valueTicker);
									SpawnX =		   160 + ((SpawnX >> 0xC) - CamX);
									SpawnY =		   112 - ((SpawnY >> 0xC) - CamY);
									z = _top + 112 - ((z >> 0xC) - CamZ);
									DrawBoxMWH(SpawnX, SpawnY, 1, 1, Color.Gray);
									DrawBoxMWH(SpawnX, z, 1, 1, Color.Gray);
									break;
								case 3:
									// Eagle
									SpawnX = Mem.ReadS32(0xFFB13A) + 0x40000 - (EccoRand(firstRand) << 3);
									firstRand = false;
									SpawnY = 0x50000;
									z = SpawnZ - 0x40000 + 0x20000;// ? 
									valueTicker = $"{SpawnX / 4096.0:0.######}:{SpawnY / 4096.0:0.######}:{(z - 0x180000) / 2048.0:0.######}:{z / 2048.0:0.######}";
									TickerText(valueTicker);
									SpawnX =		   160 + ((SpawnX >> 0xC) - CamX);
									SpawnY =		   112 - ((SpawnY >> 0xC) - CamY);
									z = _top + 112 - ((z >> 0xC) - CamZ);
									DrawBoxMWH(SpawnX, SpawnY, 1, 1, Color.Gray);
									DrawBoxMWH(SpawnX, z, 1, 1, Color.Gray);
									break;
								case 4:
									// Shark
									bool left = (EccoRand(firstRand) > 0x8000);
									firstRand = false;
									var xdiff = 0xC0000 + (EccoRand() << 3);
									SpawnX = Mem.ReadS32(0xFFB13A) + (left ? -xdiff : xdiff);
									SpawnY = Math.Min(Mem.ReadS32(0xFFB142), -0x10000) - (EccoRand() + 0x10000);
									z = SpawnZ + 0x20000;
									valueTicker = $"{SpawnX / 4096.0:0.######}:{SpawnY / 4096.0:0.######}:{(z - 0x180000) / 2048.0:0.######}:{z / 2048.0:0.######}";
									TickerText(valueTicker);
									SpawnX = 160 + ((SpawnX >> 0xC) - CamX);
									SpawnY = 112 - ((SpawnY >> 0xC) - CamY);
									z = _top + 112 - ((z >> 0xC) - CamZ);
									DrawBoxMWH(SpawnX, SpawnY, 1, 1, Color.Gray);
									DrawBoxMWH(SpawnX, z, 1, 1, Color.Gray);
									break;
								case 5:
								case 6:
								case 7:
								case 8:
									// Vine
									EccoRand(firstRand);
									firstRand = false;
									if ((temp & 1) == 1) EccoRand();
									EccoRand();
									break;
								case 9:
								case 10:
								case 11:
								case 12:
									// Unknown, possibly just rand incrementation?
									EccoRand(firstRand);
									firstRand = false;
									if ((temp & 1) == 1) EccoRand();
									break;
								case 14:
									// Shell
									SpawnX = Mem.ReadS32(0xFFB13A) - 0x20000 + (EccoRand(firstRand) << 2);
									firstRand = false;
									SpawnY = -0x80000;
									z = SpawnZ + 0x20000;
									EccoRand();
									valueTicker = $"{SpawnX / 4096.0:0.######}:{SpawnY / 4096.0:0.######}:{(z - 0x180000) / 2048.0:0.######}:{(z - 0x80000) / 2048.0:0.######}";
									TickerText(valueTicker);
									SpawnX = 160 + ((SpawnX >> 0xC) - CamX);
									SpawnY = 112 - ((SpawnY >> 0xC) - CamY);
									z = _top + 112 - ((z >> 0xC) - CamZ);
									DrawBoxMWH(SpawnX, SpawnY, 1, 1, Color.Gray);
									DrawBoxMWH(SpawnX, z, 1, 1, Color.Gray);
									break;
							}
						}
						SpawnZ += 0x20000;
					}
					break;
			}
			Joy.Set("C", null, 1);
			Joy.Set("Start", null, 1);
			var color = _turnSignalColors[Mem.ReadS8(0xFFA7C9) & 7];
			Gui.DrawRectangle(_left - 48, _top - 112, 15, 15, color, color);
		}
		public override void LoadStateCallback(string name)
		{
			Gui.DrawNew("emu");
			PreFrameCallback();
			Gui.DrawFinish();
		}
	}
}
