using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicII
	{
		private void BASprite(uint index0)
		{
		}

		private void BASprite(uint index0, uint index1)
		{
		}

		private void BASprite(uint index0, uint index1, uint index2)
		{
		}

		private void CheckBadline()
		{
			if ((rasterLine & 0x07) == ySmooth)
			{
				badline = true;
				idle = false;
			}
			else
			{
				badline = false;
			}
		}

		private void CheckBorderBottom()
		{
			if (rasterLine == borderBottom)
				borderOnVerticalEnable = true;
		}

		private void CheckBorderLeft0()
		{
		}

		private void CheckBorderLeft1()
		{
		}

		private void CheckBorderRight0()
		{
		}

		private void CheckBorderRight1()
		{
		}

		private void CheckBorderTop()
		{
			if (rasterLine == borderTop)
			{
				borderOnVertical = false;
				borderOnVerticalEnable = false;
			}
		}

		private void CheckSpriteCrunch()
		{
		}

		private void CheckSpriteDisplay()
		{
			for (int i = 0; i < 8; i++)
			{
				Sprite spr = sprites[i];
				spr.mc = spr.mcBase;

				if (spr.dma)
				{
					if ((spr.enable) && (spr.y == (rasterLine & 0xFF)))
						spr.display = true;
				}
				else
					spr.display = false;
			}
		}

		private void CheckSpriteDma()
		{
			for (int i = 0; i < 8; i++)
			{
				Sprite spr = sprites[i];
				if (spr.enable && (spr.y == (rasterLine & 0xFF)) && !spr.dma)
				{
					spr.dma = true;
					spr.mcBase = 0;
					spr.exp = true;
				}
			}
		}

		private void CheckSpriteExpansion()
		{
			for (int i = 0; i < 8; i++)
			{
				Sprite spr = sprites[i];
				if (spr.dma && spr.exp)
				{
					spr.exp = !spr.exp;
				}
			}
		}

		private ushort FetchAddrG()
		{
			ushort addr;

			if (bitmapMode)
				addr = (ushort)((vc << 3) | rc | ((bitmapRam & 0x8) << 10));
			else
				addr = (ushort)((videoBuffer[vmli] << 3) | rc | ((bitmapRam & 0xE) << 10));

			if (extraColorMode)
				addr &= (ushort)0x39FF;

			return addr;
		}

		private uint Fetch0(ushort addr)
		{
			if (cycleFetchG)
			{
				if (!idle)
					return FetchG();
				else
					return FetchGIdle();
			}
			else if (cycleFetchP)
			{
				return FetchP();
			}
			else if (cycleFetchS)
			{
				return FetchS();
			}
			else if (cycleFetchR)
			{
				return FetchR();
			}
			return FetchIdle();
		}

		private uint Fetch1(ushort addr)
		{
			phaseRead1 = mem.VicRead(addr);
			return phaseRead1;
		}

		private ushort FetchAddrV(uint offset)
		{
			return (ushort)((videoRam << 10) | offset);
		}

		private ushort FetchC()
		{
			return 0;
		}

		private ushort FetchG()
		{
			return 0;
		}

		private ushort FetchGIdle()
		{
			return 0;
		}

		private ushort FetchIdle()
		{
			return 0;
		}

		private ushort FetchP()
		{
			return 0;
		}

		private ushort FetchR()
		{
			return 0;
		}

		private ushort FetchS()
		{
			return 0;
		}

		private void Idle()
		{
		}

		private void LineStart()
		{
			if ((rasterLine == 0x30) && (!badlineEnabled) && (displayEnable))
				badlineEnabled = true;

			if (rasterLine == 0xF8)
				badlineEnabled = false;

			badline = false;
		}

		private void NextRasterLine()
		{
		}

		private void Phase0()
		{
		}

		private void Phase1()
		{
		}

		private void Refresh()
		{
		}

		private void SpritePointer(int index)
		{
		}

		private void SpriteDma0(uint index)
		{
		}

		private void SpriteDma1(int index)
		{
		}

		private void SpriteDma2(int index)
		{
		}

		private void UpdateBorder()
		{
			borderBottom = rowSelect ? (uint)0xFA : (uint)0xF6;
			borderLeft = columnSelect ? (uint)0x018 : (uint)0x01F;
			borderRight = columnSelect ? (uint)0x157 : (uint)0x14E;
			borderTop = rowSelect ? (uint)0x33 : (uint)0x37;
		}

		private void UpdateInterrupts()
		{
			irq =
				(irqDataCollision & enableIrqDataCollision) |
				(irqLightPen & enableIrqLightPen) |
				(irqRaster & enableIrqRaster) |
				(irqSpriteCollision & enableIrqSpriteCollision);
		}

		private void UpdateMCBASE()
		{
			for (int i = 0; i < 8; i++)
			{
				Sprite spr = sprites[i];
				if (spr.exp)
				{
					spr.mcBase = spr.mc;
					if (spr.mcBase == 63)
					{
						spr.dma = false;
					}
				}
			}
		}

		private void UpdateRC()
		{
			if (rc == 7)
			{
				idle = true;
				vcbase = vc;
			}
			if (!idle || badline)
			{
				idle = false;
				rc = (rc + 1) & 0x7;
			}
		}

		private void UpdateVC()
		{
			vc = vcbase;
			vmli = 0;
			if (badline)
				rc = 0;
		}

		private void Vis(uint index)
		{
		}
	}
}
