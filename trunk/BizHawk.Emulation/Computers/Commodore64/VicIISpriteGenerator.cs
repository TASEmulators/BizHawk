using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicII : IVideoProvider
	{
		public class SpriteGenerator
		{
			private int color;
			private int data;
			private bool enabled = false;
			private bool mc;
			private int mmx0;
			private int mmx1;
			private int msr;
			private int msrc;
			private int mxc;
			private int rasterLeft;
			private int rasterX;
			private int rasterWidth;
			private VicIIRegs regs;
			private SpriteRegs sprite;
			private int x;
			private bool xe;
			private bool xeToggle;

			public int[] colorBuffer;
			public int[] dataBuffer;
			public bool hasData;
			public int spriteNumber;

			public SpriteGenerator(VicIIRegs newRegs, int newNumber, int newRasterWidth, int newRasterLeft)
			{
				spriteNumber = newNumber;
				regs = newRegs;
				sprite = regs.Sprites[spriteNumber];

				rasterWidth = newRasterWidth;
				rasterLeft = newRasterLeft;
				colorBuffer = new int[rasterWidth];
				dataBuffer = new int[rasterWidth];
				rasterX = rasterLeft;
			}

			// render a scanline of a sprite
			public void Render()
			{
				hasData = false;
				enabled = false;
				mc = sprite.MxMC;
				mmx0 = regs.MMx[0];
				mmx1 = regs.MMx[1];
				msr = sprite.MSR;
				msrc = 24;
				mxc = sprite.MxC;
				x = sprite.MxX;
				xe = sprite.MxXE;

				xeToggle = !xe;
				for (int i = 0; i < rasterWidth; i++)
				{
					if (rasterX == x)
					{
						enabled = sprite.MD;
					}

					if (enabled)
					{
						if (mc)
						{
							data = ((msr >> 22) & 0x3);
							if ((rasterX & 0x1) != (x & 0x1))
							{
								if (!xe || xeToggle)
								{
									msr <<= 2;
									msrc--;
								}
								xeToggle = !xeToggle;
							}
						}
						else
						{
							data = ((msr >> 22) & 0x2);
							if (!xe || xeToggle)
							{
								msr <<= 1;
								msrc--;
							}
							xeToggle = !xeToggle;
						}
						switch (data)
						{
							case 0:
								color = 0;
								break;
							case 1:
								color = mmx0;
								break;
							case 2:
								color = mxc;
								break;
							case 3:
								color = mmx1;
								break;
						}

						dataBuffer[rasterX] = data;
						colorBuffer[rasterX] = color;
						hasData |= (data != 0);

						if (msrc == 0)
						{
							enabled = false;
						}
					}
					else
					{
						dataBuffer[rasterX] = 0;
					}

					rasterX++;
					if (rasterX >= rasterWidth)
						rasterX = 0;
				}
				sprite.MSR = msr;
			}
		}
	}
}
