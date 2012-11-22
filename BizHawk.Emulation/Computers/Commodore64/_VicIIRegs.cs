using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicIINew : IVideoProvider
	{
		private class VicIINewSprite
		{
			// internal regs
			public int MC;
			public int MCBASE;
			public bool MD;
			public bool MDMA;
			public int MPTR;
			public int MSR;
			public bool MxXEToggle;
			public bool MxYEToggle;
			
			// external regs
			public int MxC; // sprite color
			public bool MxD; // sprite-data collision
			public bool MxDP; // sprite priority
			public bool MxE; // sprite enabled
			public bool MxM; // sprite-sprite collision
			public bool MxMC; // sprite multicolor
			public int MxX; // sprite X coordinate
			public bool MxXE; // sprite X expansion
			public int MxY; // sprite Y coordinate
			public bool MxYE; // sprite Y expansion
		}
		
		// internal regs
		private int RC;
		private int VC;
		private int VCBASE;
		private int VMLI;

		// external regs
		private bool BMM; // bitmap mode
		private int[] BxC = new int[4]; // background colors
		private int CB; // character bitmap offset
		private bool CSEL; // column select
		private bool DEN; // display enabled
		private int EC; // border color
		private bool ECM; // extra color mode
		private bool ELP; // enable lightpen interrupt
		private bool EMBC; // enable sprite-data interrupt
		private bool EMMC; // enable sprite-sprite interrupt
		private bool ERST; // enable raster line interrupt
		private bool ILP; // light pen interrupt active
		private bool IMBC; // sprite-data interrupt active
		private bool IMMC; // sprite-sprite interrupt active
		private bool IRQ; // interrupt was triggered
		private bool IRST; // raster line interrupt active
		private int LPX; // lightpen X coordinate
		private int LPY; // lightpen Y coordinate
		private bool MCM; // multicolor mode
		private int[] MMx = new int[2]; // sprite extra color
		private int RASTER; // current raster line
		private bool RES; // reset bit (does nothing in this version of the VIC)
		private bool RSEL; // row select
		private int VM; // video memory offset
		private int XSCROLL; // X scroll position
		private int YSCROLL; // Y scroll position

		private int spriteData;
		private int spritePixel;
		private bool spritePriority;
		private VicIINewSprite[] sprites;

		private bool advanceX;
		private bool badline;
		private int bitmapColumn;
		private byte bitmapData;
		private int borderBottom;
		private int borderLeft;
		private bool borderOnMain;
		private bool borderOnVertical;
		private int borderRight;
		private int borderTop;
		private bool centerEnabled;
		private byte characterData;
		private byte characterDataBus;
		private byte[] characterMemory;
		private byte colorData;
		private byte colorDataBus;
		private byte[] colorMemory;
		private bool displayEnabled;
		private int fetchCounter;
		private int graphicsMode;
		private bool idle;
		private int plotterBufferIndex;
		private int plotterData;
		private int[] plotterDataBuffer;
		private int plotterDelay;
		private int plotterPixel;
		private int[] plotterPixelBuffer;
		private int rasterInterruptLine;
		private bool rasterInterruptTriggered;
		private int rasterLeft;
		private int rasterLines;
		private int rasterWidth;
		private int rasterX;
		private int refreshAddress;

		private void InitRegs()
		{
			// init sprites
			sprites = new VicIINewSprite[8];
			for (int i = 0; i < 8; i++)
				sprites[i] = new VicIINewSprite();

			// init buffers
			plotterDataBuffer = new int[plotterDelay];
			plotterPixelBuffer = new int[plotterDelay];
			characterMemory = new byte[40];
			colorMemory = new byte[40];

			// init raster data
			rasterX = rasterLeft;
			rasterWidth = pipeline.Length * 8;

			// reset regs
			for (int i = 0; i < 0x40; i++)
				this[i] = 0x00;

			// power on state
			this[0x16] = 0xC0;
			this[0x18] = 0x01;
			this[0x19] = 0x71;
			this[0x1A] = 0xF0;
			RC = 7;
			refreshAddress = 0x3FFF;
			idle = true;
		}

		private byte this[int addr]
		{
			get
			{
				int result = 0xFF; // value for any open bits
				addr &= 0x3F;

				switch (addr)
				{
					case 0x00:
					case 0x02:
					case 0x04:
					case 0x06:
					case 0x08:
					case 0x0A:
					case 0x0C:
					case 0x0E:
						result = sprites[addr >> 1].MxX;
						break;
					case 0x01:
					case 0x03:
					case 0x05:
					case 0x07:
					case 0x09:
					case 0x0B:
					case 0x0D:
					case 0x0F:
						result = sprites[addr >> 1].MxY;
						break;
					case 0x10:
						result = ((sprites[0].MxX & 0x100) != 0) ? 0x01 : 0x00;
						result |= ((sprites[1].MxX & 0x100) != 0) ? 0x02 : 0x00;
						result |= ((sprites[2].MxX & 0x100) != 0) ? 0x04 : 0x00;
						result |= ((sprites[3].MxX & 0x100) != 0) ? 0x08 : 0x00;
						result |= ((sprites[4].MxX & 0x100) != 0) ? 0x10 : 0x00;
						result |= ((sprites[5].MxX & 0x100) != 0) ? 0x20 : 0x00;
						result |= ((sprites[6].MxX & 0x100) != 0) ? 0x40 : 0x00;
						result |= ((sprites[7].MxX & 0x100) != 0) ? 0x80 : 0x00;
						break;
					case 0x11:
						result = YSCROLL & 0x07;
						result |= (RSEL ? 0x08 : 0x00);
						result |= (DEN ? 0x10 : 0x00);
						result |= (BMM ? 0x20 : 0x00);
						result |= (ECM ? 0x40 : 0x00);
						result |= ((RASTER & 0x100) >> 1);
						break;
					case 0x12:
						result = RASTER & 0xFF;
						break;
					case 0x13:
						result = LPX;
						break;
					case 0x14:
						result = LPY;
						break;
					case 0x15:
						result = (sprites[0].MxE ? 0x01 : 0x00);
						result |= (sprites[1].MxE ? 0x02 : 0x00);
						result |= (sprites[2].MxE ? 0x04 : 0x00);
						result |= (sprites[3].MxE ? 0x08 : 0x00);
						result |= (sprites[4].MxE ? 0x10 : 0x00);
						result |= (sprites[5].MxE ? 0x20 : 0x00);
						result |= (sprites[6].MxE ? 0x40 : 0x00);
						result |= (sprites[7].MxE ? 0x80 : 0x00);
						break;
					case 0x16:
						result &= 0xC0;
						result |= XSCROLL & 0x07;
						result |= (CSEL ? 0x08 : 0x00);
						result |= (MCM ? 0x10 : 0x00);
						result |= (RES ? 0x20 : 0x00);
						break;
					case 0x17:
						result = (sprites[0].MxYE ? 0x01 : 0x00);
						result |= (sprites[1].MxYE ? 0x02 : 0x00);
						result |= (sprites[2].MxYE ? 0x04 : 0x00);
						result |= (sprites[3].MxYE ? 0x08 : 0x00);
						result |= (sprites[4].MxYE ? 0x10 : 0x00);
						result |= (sprites[5].MxYE ? 0x20 : 0x00);
						result |= (sprites[6].MxYE ? 0x40 : 0x00);
						result |= (sprites[7].MxYE ? 0x80 : 0x00);
						break;
					case 0x18:
						result &= 0x01;
						result |= (CB & 0x07) << 1;
						result |= (VM & 0x0F) << 4;
						break;
					case 0x19:
						result &= 0x70;
						result |= (IRST ? 0x01 : 0x00);
						result |= (IMBC ? 0x02 : 0x00);
						result |= (IMMC ? 0x04 : 0x00);
						result |= (ILP ? 0x08 : 0x00);
						result |= (IRQ ? 0x80 : 0x00);
						break;
					case 0x1A:
						result &= 0xF0;
						result |= (ERST ? 0x01 : 0x00);
						result |= (EMBC ? 0x02 : 0x00);
						result |= (EMMC ? 0x04 : 0x00);
						result |= (ELP ? 0x08 : 0x00);
						break;
					case 0x1B:
						result = (sprites[0].MxDP ? 0x01 : 0x00);
						result |= (sprites[1].MxDP ? 0x02 : 0x00);
						result |= (sprites[2].MxDP ? 0x04 : 0x00);
						result |= (sprites[3].MxDP ? 0x08 : 0x00);
						result |= (sprites[4].MxDP ? 0x10 : 0x00);
						result |= (sprites[5].MxDP ? 0x20 : 0x00);
						result |= (sprites[6].MxDP ? 0x40 : 0x00);
						result |= (sprites[7].MxDP ? 0x80 : 0x00);
						break;
					case 0x1C:
						result = (sprites[0].MxMC ? 0x01 : 0x00);
						result |= (sprites[1].MxMC ? 0x02 : 0x00);
						result |= (sprites[2].MxMC ? 0x04 : 0x00);
						result |= (sprites[3].MxMC ? 0x08 : 0x00);
						result |= (sprites[4].MxMC ? 0x10 : 0x00);
						result |= (sprites[5].MxMC ? 0x20 : 0x00);
						result |= (sprites[6].MxMC ? 0x40 : 0x00);
						result |= (sprites[7].MxMC ? 0x80 : 0x00);
						break;
					case 0x1D:
						result = (sprites[0].MxXE ? 0x01 : 0x00);
						result |= (sprites[1].MxXE ? 0x02 : 0x00);
						result |= (sprites[2].MxXE ? 0x04 : 0x00);
						result |= (sprites[3].MxXE ? 0x08 : 0x00);
						result |= (sprites[4].MxXE ? 0x10 : 0x00);
						result |= (sprites[5].MxXE ? 0x20 : 0x00);
						result |= (sprites[6].MxXE ? 0x40 : 0x00);
						result |= (sprites[7].MxXE ? 0x80 : 0x00);
						break;
					case 0x1E:
						result = (sprites[0].MxM ? 0x01 : 0x00);
						result |= (sprites[1].MxM ? 0x02 : 0x00);
						result |= (sprites[2].MxM ? 0x04 : 0x00);
						result |= (sprites[3].MxM ? 0x08 : 0x00);
						result |= (sprites[4].MxM ? 0x10 : 0x00);
						result |= (sprites[5].MxM ? 0x20 : 0x00);
						result |= (sprites[6].MxM ? 0x40 : 0x00);
						result |= (sprites[7].MxM ? 0x80 : 0x00);
						break;
					case 0x1F:
						result = (sprites[0].MxD ? 0x01 : 0x00);
						result |= (sprites[1].MxD ? 0x02 : 0x00);
						result |= (sprites[2].MxD ? 0x04 : 0x00);
						result |= (sprites[3].MxD ? 0x08 : 0x00);
						result |= (sprites[4].MxD ? 0x10 : 0x00);
						result |= (sprites[5].MxD ? 0x20 : 0x00);
						result |= (sprites[6].MxD ? 0x40 : 0x00);
						result |= (sprites[7].MxD ? 0x80 : 0x00);
						break;
					case 0x20:
						result &= 0xF0;
						result |= EC & 0x0F;
						break;
					case 0x21:
					case 0x22:
					case 0x23:
					case 0x24:
						result &= 0xF0;
						result |= BxC[addr - 0x21] & 0x0F;
						break;
					case 0x25:
					case 0x26:
						result &= 0xF0;
						result |= MMx[addr - 0x25] & 0x0F;
						break;
					case 0x27:
					case 0x28:
					case 0x29:
					case 0x2A:
					case 0x2B:
					case 0x2C:
					case 0x2D:
					case 0x2E:
						result &= 0xF0;
						result |= sprites[addr - 0x27].MxC & 0x0F;
						break;
					default:
						result = 0xFF;
						break;
				}

				return (byte)(result);
			}
			set
			{
				int index;
				int val = value;
				addr &= 0x3F;

				switch (addr)
				{
					case 0x00:
					case 0x02:
					case 0x04:
					case 0x06:
					case 0x08:
					case 0x0A:
					case 0x0C:
					case 0x0E:
						index = addr >> 1;
						sprites[index].MxX &= 0x100;
						sprites[index].MxX |= (val & 0xFF);
						break;
					case 0x01:
					case 0x03:
					case 0x05:
					case 0x07:
					case 0x09:
					case 0x0B:
					case 0x0D:
					case 0x0F:
						index = addr >> 1;
						sprites[index].MxY &= 0x100;
						sprites[index].MxY |= (val & 0xFF);
						break;
					case 0x10:
						sprites[0].MxX = (sprites[0].MxX & 0xFF) | ((val & 0x01) << 8);
						sprites[1].MxX = (sprites[1].MxX & 0xFF) | ((val & 0x02) << 7);
						sprites[2].MxX = (sprites[2].MxX & 0xFF) | ((val & 0x04) << 6);
						sprites[3].MxX = (sprites[3].MxX & 0xFF) | ((val & 0x08) << 5);
						sprites[4].MxX = (sprites[4].MxX & 0xFF) | ((val & 0x10) << 4);
						sprites[5].MxX = (sprites[5].MxX & 0xFF) | ((val & 0x20) << 3);
						sprites[6].MxX = (sprites[6].MxX & 0xFF) | ((val & 0x40) << 2);
						sprites[7].MxX = (sprites[7].MxX & 0xFF) | ((val & 0x80) << 1);
						break;
					case 0x11:
						YSCROLL = (val & 0x07);
						RSEL = ((val & 0x08) != 0x00);
						DEN = ((val & 0x10) != 0x00);
						BMM = ((val & 0x20) != 0x00);
						ECM = ((val & 0x40) != 0x00);
						RASTER &= 0xFF;
						RASTER |= ((val & 0x80) << 1);
						break;
					case 0x12:
						RASTER &= 0x100;
						RASTER |= (val & 0xFF);
						break;
					case 0x13:
						LPX = (val & 0xFF);
						break;
					case 0x14:
						LPY = (val & 0xFF);
						break;
					case 0x15:
						sprites[0].MxE = ((val & 0x01) != 0x00);
						sprites[1].MxE = ((val & 0x02) != 0x00);
						sprites[2].MxE = ((val & 0x04) != 0x00);
						sprites[3].MxE = ((val & 0x08) != 0x00);
						sprites[4].MxE = ((val & 0x10) != 0x00);
						sprites[5].MxE = ((val & 0x20) != 0x00);
						sprites[6].MxE = ((val & 0x40) != 0x00);
						sprites[7].MxE = ((val & 0x80) != 0x00);
						break;
					case 0x16:
						XSCROLL = (val & 0x07);
						CSEL = ((val & 0x08) != 0x00);
						MCM = ((val & 0x10) != 0x00);
						RES = ((val & 0x20) != 0x00);
						break;
					case 0x17:
						sprites[0].MxYE = ((val & 0x01) != 0x00);
						sprites[1].MxYE = ((val & 0x02) != 0x00);
						sprites[2].MxYE = ((val & 0x04) != 0x00);
						sprites[3].MxYE = ((val & 0x08) != 0x00);
						sprites[4].MxYE = ((val & 0x10) != 0x00);
						sprites[5].MxYE = ((val & 0x20) != 0x00);
						sprites[6].MxYE = ((val & 0x40) != 0x00);
						sprites[7].MxYE = ((val & 0x80) != 0x00);
						break;
					case 0x18:
						CB = (val & 0x0E) >> 1;
						VM = (val & 0xF0) >> 4;
						break;
					case 0x19:
						IRST = ((val & 0x01) != 0x00);
						IMBC = ((val & 0x02) != 0x00);
						IMMC = ((val & 0x04) != 0x00);
						ILP = ((val & 0x08) != 0x00);
						break;
					case 0x1A:
						ERST = ((val & 0x01) != 0x00);
						EMBC = ((val & 0x02) != 0x00);
						EMMC = ((val & 0x04) != 0x00);
						ELP = ((val & 0x08) != 0x00);
						break;
					case 0x1B:
						sprites[0].MxDP = ((val & 0x01) != 0x00);
						sprites[1].MxDP = ((val & 0x02) != 0x00);
						sprites[2].MxDP = ((val & 0x04) != 0x00);
						sprites[3].MxDP = ((val & 0x08) != 0x00);
						sprites[4].MxDP = ((val & 0x10) != 0x00);
						sprites[5].MxDP = ((val & 0x20) != 0x00);
						sprites[6].MxDP = ((val & 0x40) != 0x00);
						sprites[7].MxDP = ((val & 0x80) != 0x00);
						break;
					case 0x1C:
						sprites[0].MxMC = ((val & 0x01) != 0x00);
						sprites[1].MxMC = ((val & 0x02) != 0x00);
						sprites[2].MxMC = ((val & 0x04) != 0x00);
						sprites[3].MxMC = ((val & 0x08) != 0x00);
						sprites[4].MxMC = ((val & 0x10) != 0x00);
						sprites[5].MxMC = ((val & 0x20) != 0x00);
						sprites[6].MxMC = ((val & 0x40) != 0x00);
						sprites[7].MxMC = ((val & 0x80) != 0x00);
						break;
					case 0x1D:
						sprites[0].MxXE = ((val & 0x01) != 0x00);
						sprites[1].MxXE = ((val & 0x02) != 0x00);
						sprites[2].MxXE = ((val & 0x04) != 0x00);
						sprites[3].MxXE = ((val & 0x08) != 0x00);
						sprites[4].MxXE = ((val & 0x10) != 0x00);
						sprites[5].MxXE = ((val & 0x20) != 0x00);
						sprites[6].MxXE = ((val & 0x40) != 0x00);
						sprites[7].MxXE = ((val & 0x80) != 0x00);
						break;
					case 0x1E:
						sprites[0].MxM = ((val & 0x01) != 0x00);
						sprites[1].MxM = ((val & 0x02) != 0x00);
						sprites[2].MxM = ((val & 0x04) != 0x00);
						sprites[3].MxM = ((val & 0x08) != 0x00);
						sprites[4].MxM = ((val & 0x10) != 0x00);
						sprites[5].MxM = ((val & 0x20) != 0x00);
						sprites[6].MxM = ((val & 0x40) != 0x00);
						sprites[7].MxM = ((val & 0x80) != 0x00);
						break;
					case 0x1F:
						sprites[0].MxD = ((val & 0x01) != 0x00);
						sprites[1].MxD = ((val & 0x02) != 0x00);
						sprites[2].MxD = ((val & 0x04) != 0x00);
						sprites[3].MxD = ((val & 0x08) != 0x00);
						sprites[4].MxD = ((val & 0x10) != 0x00);
						sprites[5].MxD = ((val & 0x20) != 0x00);
						sprites[6].MxD = ((val & 0x40) != 0x00);
						sprites[7].MxD = ((val & 0x80) != 0x00);
						break;
					case 0x20:
						EC = (val & 0x0F);
						break;
					case 0x21:
					case 0x22:
					case 0x23:
					case 0x24:
						BxC[addr - 0x21] = val & 0x0F;
						break;
					case 0x25:
					case 0x26:
						MMx[addr - 0x25] = val & 0x0F;
						break;
					case 0x27:
					case 0x28:
					case 0x29:
					case 0x2A:
					case 0x2B:
					case 0x2C:
					case 0x2D:
					case 0x2E:
						sprites[addr - 0x27].MxC = val & 0x0F;
						break;
				}
			}
		}

		public byte Peek(int addr)
		{
			return this[addr & 0x3F];
		}

		public void Poke(int addr, byte val)
		{
			this[addr & 0x3F] = val;
		}

		public byte Read(ushort addr)
		{
			byte result = 0;
			addr &= 0x3F;

			switch (addr)
			{
				case 0x1E:
					// clear after read
					result = this[addr];
					this[addr] = 0x00;
					IMMC = false;
					UpdateInterrupts();
					break;
				case 0x1F:
					// clear after read
					result = this[addr];
					this[addr] = 0x00;
					IMBC = false;
					UpdateInterrupts();
					break;
				default:
					result = this[addr];
					break;
			}

			return result;
		}

		private void UpdateBorder()
		{
			borderTop = RSEL ? 0x033 : 0x037;
			borderBottom = RSEL ? 0x0FB : 0x0F7;
			borderLeft = CSEL ? 0x018 : 0x01F;
			borderRight = CSEL ? 0x158 : 0x14F;
		}

		private void UpdateInterrupts()
		{
			IRQ = ((IRST & ERST) || (IMMC & EMMC) || (IMBC & EMBC) || (ILP & ELP));
		}

		private void UpdatePlotter()
		{
			graphicsMode = (ECM ? 0x04 : 0x00) | (BMM ? 0x02 : 0x00) | (MCM ? 0x01 : 0x00);
		}

		public void Write(ushort addr, byte val)
		{
			addr &= 0x3F;

			switch (addr)
			{
				case 0x11:
					rasterInterruptTriggered = false;
					rasterInterruptLine &= 0xFF;
					rasterInterruptLine |= (val & 0x80) << 1;
					// raster upper bit can't be changed, save and restore the value
					val &= 0x7F;
					val |= (byte)(this[addr] & 0x80);
					this[addr] = val;
					UpdateBorder();
					UpdatePlotter();
					break;
				case 0x12:
					// raster interrupt lower 8 bits
					rasterInterruptTriggered = false;
					rasterInterruptLine &= 0x100;
					rasterInterruptLine |= (val & 0xFF);
					break;
				case 0x16:
					this[addr] = val;
					UpdateBorder();
					UpdatePlotter();
					break;
				case 0x19:
					// only allow clearing of these flags
					if ((val & 0x01) != 0x00)
						IRST = false;
					if ((val & 0x02) != 0x00)
						IMBC = false;
					if ((val & 0x04) != 0x00)
						IMMC = false;
					if ((val & 0x08) != 0x00)
						ILP = false;
					UpdateInterrupts();
					break;
				case 0x1E:
				case 0x1F:
					// can't write to these regs
					break;
				default:
					this[addr] = val;
					break;
			}
		}
	}
}