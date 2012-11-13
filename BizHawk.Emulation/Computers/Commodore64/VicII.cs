using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class SpriteRegs
	{
		public int MC; // (internal)
		public int MCBASE; // (internal)
		public bool MD; // (internal)
		public bool MDMA; // (internal)
		public int MPTR; // (internal)
		public Int32 MSR; // (internal)
		public bool MSRA; // (internal)
		public int MSRC; // (internal)
		public int MxC; // sprite color
		public bool MxD; // sprite-data collision
		public bool MxDP; // sprite priority
		public bool MxE; // sprite enabled
		public bool MxM; // sprite-sprite collision
		public bool MxMC; // sprite multicolor
		public int MxX; // sprite X coordinate
		public bool MxXE; // sprite X expansion
		public bool MxXEToggle; // (internal)
		public int MxXLatch; // (internal)
		public int MxY; // sprite Y coordinate
		public bool MxYE; // sprite Y expansion
		public bool MxYEToggle; // (internal)
	}

	public class VicIIRegs
	{
		public bool BMM; // bitmap mode
		public int[] BxC = new int[4]; // background colors
		public int CB; // character bitmap offset
		public bool CSEL; // column select
		public bool DEN; // display enabled
		public int EC; // border color
		public bool ECM; // extra color mode
		public bool ELP; // enable lightpen interrupt
		public bool EMBC; // enable sprite-data interrupt
		public bool EMMC; // enable sprite-sprite interrupt
		public bool ERST; // enable raster line interrupt
		public bool ILP; // light pen interrupt active
		public bool IMBC; // sprite-data interrupt active
		public bool IMMC; // sprite-sprite interrupt active
		public bool IRQ; // interrupt was triggered
		public bool IRST; // raster line interrupt active
		public int LPX; // lightpen X coordinate
		public int LPY; // lightpen Y coordinate
		public bool MCM; // multicolor mode
		public int[] MMx = new int[2]; // sprite extra color
		public int RASTER; // current raster line
		public int RC; // (internal)
		public bool RES; // reset bit (does nothing in this version of the VIC)
		public bool RSEL; // row select
		public int VC; // (internal)
		public int VCBASE; // (internal)
		public int VM; // video memory offset
		public int VMLI; // (internal)
		public int XSCROLL; // X scroll position
		public int YSCROLL; // Y scroll position

		public SpriteRegs[] Sprites = new SpriteRegs[8];

		public VicIIRegs()
		{
			// power on state
			
			this[0x16] = 0xC0;
			this[0x18] = 0x01;
			this[0x19] = 0x71;
			this[0x1A] = 0xF0;

			// init sprites
			for (int i = 0; i < 8; i++)
				Sprites[i] = new SpriteRegs();
		}

		public byte this[int addr]
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
						result = Sprites[addr >> 1].MxX;
						break;
					case 0x01:
					case 0x03:
					case 0x05:
					case 0x07:
					case 0x09:
					case 0x0B:
					case 0x0D:
					case 0x0F:
						result = Sprites[addr >> 1].MxY;
						break;
					case 0x10:
						result = ((Sprites[0].MxX & 0x100) != 0) ? 0x01 : 0x00;
						result |= ((Sprites[1].MxX & 0x100) != 0) ? 0x02 : 0x00;
						result |= ((Sprites[2].MxX & 0x100) != 0) ? 0x04 : 0x00;
						result |= ((Sprites[3].MxX & 0x100) != 0) ? 0x08 : 0x00;
						result |= ((Sprites[4].MxX & 0x100) != 0) ? 0x10 : 0x00;
						result |= ((Sprites[5].MxX & 0x100) != 0) ? 0x20 : 0x00;
						result |= ((Sprites[6].MxX & 0x100) != 0) ? 0x40 : 0x00;
						result |= ((Sprites[7].MxX & 0x100) != 0) ? 0x80 : 0x00;
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
						result = (Sprites[0].MxE ? 0x01 : 0x00);
						result |= (Sprites[1].MxE ? 0x02 : 0x00);
						result |= (Sprites[2].MxE ? 0x04 : 0x00);
						result |= (Sprites[3].MxE ? 0x08 : 0x00);
						result |= (Sprites[4].MxE ? 0x10 : 0x00);
						result |= (Sprites[5].MxE ? 0x20 : 0x00);
						result |= (Sprites[6].MxE ? 0x40 : 0x00);
						result |= (Sprites[7].MxE ? 0x80 : 0x00);
						break;
					case 0x16:
						result &= 0xC0;
						result |= XSCROLL & 0x07;
						result |= (CSEL ? 0x08 : 0x00);
						result |= (MCM ? 0x10 : 0x00);
						result |= (RES ? 0x20 : 0x00);
						break;
					case 0x17:
						result = (Sprites[0].MxYE ? 0x01 : 0x00);
						result |= (Sprites[1].MxYE ? 0x02 : 0x00);
						result |= (Sprites[2].MxYE ? 0x04 : 0x00);
						result |= (Sprites[3].MxYE ? 0x08 : 0x00);
						result |= (Sprites[4].MxYE ? 0x10 : 0x00);
						result |= (Sprites[5].MxYE ? 0x20 : 0x00);
						result |= (Sprites[6].MxYE ? 0x40 : 0x00);
						result |= (Sprites[7].MxYE ? 0x80 : 0x00);						
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
						result = (Sprites[0].MxDP ? 0x01 : 0x00);
						result |= (Sprites[1].MxDP ? 0x02 : 0x00);
						result |= (Sprites[2].MxDP ? 0x04 : 0x00);
						result |= (Sprites[3].MxDP ? 0x08 : 0x00);
						result |= (Sprites[4].MxDP ? 0x10 : 0x00);
						result |= (Sprites[5].MxDP ? 0x20 : 0x00);
						result |= (Sprites[6].MxDP ? 0x40 : 0x00);
						result |= (Sprites[7].MxDP ? 0x80 : 0x00);						
						break;
					case 0x1C:
						result = (Sprites[0].MxMC ? 0x01 : 0x00);
						result |= (Sprites[1].MxMC ? 0x02 : 0x00);
						result |= (Sprites[2].MxMC ? 0x04 : 0x00);
						result |= (Sprites[3].MxMC ? 0x08 : 0x00);
						result |= (Sprites[4].MxMC ? 0x10 : 0x00);
						result |= (Sprites[5].MxMC ? 0x20 : 0x00);
						result |= (Sprites[6].MxMC ? 0x40 : 0x00);
						result |= (Sprites[7].MxMC ? 0x80 : 0x00);						
						break;
					case 0x1D:
						result = (Sprites[0].MxXE ? 0x01 : 0x00);
						result |= (Sprites[1].MxXE ? 0x02 : 0x00);
						result |= (Sprites[2].MxXE ? 0x04 : 0x00);
						result |= (Sprites[3].MxXE ? 0x08 : 0x00);
						result |= (Sprites[4].MxXE ? 0x10 : 0x00);
						result |= (Sprites[5].MxXE ? 0x20 : 0x00);
						result |= (Sprites[6].MxXE ? 0x40 : 0x00);
						result |= (Sprites[7].MxXE ? 0x80 : 0x00);						
						break;
					case 0x1E:
						result = (Sprites[0].MxM ? 0x01 : 0x00);
						result |= (Sprites[1].MxM ? 0x02 : 0x00);
						result |= (Sprites[2].MxM ? 0x04 : 0x00);
						result |= (Sprites[3].MxM ? 0x08 : 0x00);
						result |= (Sprites[4].MxM ? 0x10 : 0x00);
						result |= (Sprites[5].MxM ? 0x20 : 0x00);
						result |= (Sprites[6].MxM ? 0x40 : 0x00);
						result |= (Sprites[7].MxM ? 0x80 : 0x00);						
						break;
					case 0x1F:
						result = (Sprites[0].MxD ? 0x01 : 0x00);
						result |= (Sprites[1].MxD ? 0x02 : 0x00);
						result |= (Sprites[2].MxD ? 0x04 : 0x00);
						result |= (Sprites[3].MxD ? 0x08 : 0x00);
						result |= (Sprites[4].MxD ? 0x10 : 0x00);
						result |= (Sprites[5].MxD ? 0x20 : 0x00);
						result |= (Sprites[6].MxD ? 0x40 : 0x00);
						result |= (Sprites[7].MxD ? 0x80 : 0x00);						
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
						result |= Sprites[addr - 0x27].MxC & 0x0F;
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
						Sprites[index].MxX &= 0x100;
						Sprites[index].MxX |= (val & 0xFF);
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
						Sprites[index].MxY &= 0x100;
						Sprites[index].MxY |= (val & 0xFF);
						break;
					case 0x10:
						Sprites[0].MxX = (Sprites[0].MxX & 0xFF) | ((val & 0x01) << 8);
						Sprites[1].MxX = (Sprites[1].MxX & 0xFF) | ((val & 0x02) << 7);
						Sprites[2].MxX = (Sprites[2].MxX & 0xFF) | ((val & 0x04) << 6);
						Sprites[3].MxX = (Sprites[3].MxX & 0xFF) | ((val & 0x08) << 5);
						Sprites[4].MxX = (Sprites[4].MxX & 0xFF) | ((val & 0x10) << 4);
						Sprites[5].MxX = (Sprites[5].MxX & 0xFF) | ((val & 0x20) << 3);
						Sprites[6].MxX = (Sprites[6].MxX & 0xFF) | ((val & 0x40) << 2);
						Sprites[7].MxX = (Sprites[7].MxX & 0xFF) | ((val & 0x80) << 1);
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
						Sprites[0].MxE = ((val & 0x01) != 0x00);
						Sprites[1].MxE = ((val & 0x02) != 0x00);
						Sprites[2].MxE = ((val & 0x04) != 0x00);
						Sprites[3].MxE = ((val & 0x08) != 0x00);
						Sprites[4].MxE = ((val & 0x10) != 0x00);
						Sprites[5].MxE = ((val & 0x20) != 0x00);
						Sprites[6].MxE = ((val & 0x40) != 0x00);
						Sprites[7].MxE = ((val & 0x80) != 0x00);
						break;
					case 0x16:
						XSCROLL = (val & 0x07);
						CSEL = ((val & 0x08) != 0x00);
						MCM = ((val & 0x10) != 0x00);
						RES = ((val & 0x20) != 0x00);
						break;
					case 0x17:
						Sprites[0].MxYE = ((val & 0x01) != 0x00);
						Sprites[1].MxYE = ((val & 0x02) != 0x00);
						Sprites[2].MxYE = ((val & 0x04) != 0x00);
						Sprites[3].MxYE = ((val & 0x08) != 0x00);
						Sprites[4].MxYE = ((val & 0x10) != 0x00);
						Sprites[5].MxYE = ((val & 0x20) != 0x00);
						Sprites[6].MxYE = ((val & 0x40) != 0x00);
						Sprites[7].MxYE = ((val & 0x80) != 0x00);
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
						Sprites[0].MxDP = ((val & 0x01) != 0x00);
						Sprites[1].MxDP = ((val & 0x02) != 0x00);
						Sprites[2].MxDP = ((val & 0x04) != 0x00);
						Sprites[3].MxDP = ((val & 0x08) != 0x00);
						Sprites[4].MxDP = ((val & 0x10) != 0x00);
						Sprites[5].MxDP = ((val & 0x20) != 0x00);
						Sprites[6].MxDP = ((val & 0x40) != 0x00);
						Sprites[7].MxDP = ((val & 0x80) != 0x00);
						break;
					case 0x1C:
						Sprites[0].MxMC = ((val & 0x01) != 0x00);
						Sprites[1].MxMC = ((val & 0x02) != 0x00);
						Sprites[2].MxMC = ((val & 0x04) != 0x00);
						Sprites[3].MxMC = ((val & 0x08) != 0x00);
						Sprites[4].MxMC = ((val & 0x10) != 0x00);
						Sprites[5].MxMC = ((val & 0x20) != 0x00);
						Sprites[6].MxMC = ((val & 0x40) != 0x00);
						Sprites[7].MxMC = ((val & 0x80) != 0x00);
						break;
					case 0x1D:
						Sprites[0].MxXE = ((val & 0x01) != 0x00);
						Sprites[1].MxXE = ((val & 0x02) != 0x00);
						Sprites[2].MxXE = ((val & 0x04) != 0x00);
						Sprites[3].MxXE = ((val & 0x08) != 0x00);
						Sprites[4].MxXE = ((val & 0x10) != 0x00);
						Sprites[5].MxXE = ((val & 0x20) != 0x00);
						Sprites[6].MxXE = ((val & 0x40) != 0x00);
						Sprites[7].MxXE = ((val & 0x80) != 0x00);
						break;
					case 0x1E:
						Sprites[0].MxM = ((val & 0x01) != 0x00);
						Sprites[1].MxM = ((val & 0x02) != 0x00);
						Sprites[2].MxM = ((val & 0x04) != 0x00);
						Sprites[3].MxM = ((val & 0x08) != 0x00);
						Sprites[4].MxM = ((val & 0x10) != 0x00);
						Sprites[5].MxM = ((val & 0x20) != 0x00);
						Sprites[6].MxM = ((val & 0x40) != 0x00);
						Sprites[7].MxM = ((val & 0x80) != 0x00);
						break;
					case 0x1F:
						Sprites[0].MxD = ((val & 0x01) != 0x00);
						Sprites[1].MxD = ((val & 0x02) != 0x00);
						Sprites[2].MxD = ((val & 0x04) != 0x00);
						Sprites[3].MxD = ((val & 0x08) != 0x00);
						Sprites[4].MxD = ((val & 0x10) != 0x00);
						Sprites[5].MxD = ((val & 0x20) != 0x00);
						Sprites[6].MxD = ((val & 0x40) != 0x00);
						Sprites[7].MxD = ((val & 0x80) != 0x00);
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
						Sprites[addr - 0x27].MxC = val & 0x0F;
						break;
				}
			}
		}
	}

	public partial class VicII : IVideoProvider
	{
		// graphics buffer
		public int[] buffer;
		public int bufferSize;

		// palette
		public int[] palette =
		{
			Colors.ARGB(0x00, 0x00, 0x00),
			Colors.ARGB(0xFF, 0xFF, 0xFF),
			Colors.ARGB(0x68, 0x37, 0x2B),
			Colors.ARGB(0x70, 0xA4, 0xB2),
			Colors.ARGB(0x6F, 0x3D, 0x86),
			Colors.ARGB(0x58, 0x8D, 0x43),
			Colors.ARGB(0x35, 0x28, 0x79),
			Colors.ARGB(0xB8, 0xC7, 0x6F),
			Colors.ARGB(0x6F, 0x4F, 0x25),
			Colors.ARGB(0x43, 0x39, 0x00),
			Colors.ARGB(0x9A, 0x67, 0x59),
			Colors.ARGB(0x44, 0x44, 0x44),
			Colors.ARGB(0x6C, 0x6C, 0x6C),
			Colors.ARGB(0x9A, 0xD2, 0x84),
			Colors.ARGB(0x6C, 0x5E, 0xB5),
			Colors.ARGB(0x95, 0x95, 0x95)
		};

		// raster
		public bool badLine;
		public byte bitmapData;
		public byte bitmapDataMask;
		public int borderBottom;
		public int borderLeft;
		public bool borderOnMain;
		public bool borderOnVertical;
		public int borderRight;
		public int borderTop;
		public int characterColumn;
		public byte characterData;
		public byte characterDataBus;
		public byte[] characterMemory;
		public bool charactersEnabled;
		public byte colorData;
		public byte colorDataBus;
		public byte[] colorMemory;
		public int cycle;
		public int cycleLeft;
		public int cyclesPerFrame;
		public bool dataForeground;
		public bool displayEnabled;
		public bool hBlank;
		public bool idle;
		public int[] pixelBuffer;
		public bool[] pixelBufferForeground;
		public int pixelBufferIndex;
		public int rasterInterruptLine;
		public int rasterLineLeft;
		public int rasterOffset;
		public int rasterOffsetX;
		public int rasterTotalLines;
		public int rasterWidth;
		public int refreshAddress;
		public int renderOffset;
		public int spriteFetchStartCycle;
		public int spriteFetchIndex;
		public bool spriteForeground;
		public SpriteGenerator[] spriteGenerators;
		public int totalCycles;
		public bool vBlank;
		public int visibleBottom;
		public int visibleHeight;
		public int visibleLeft;
		public bool visibleRenderX;
		public bool visibleRenderY;
		public int visibleRight;
		public int visibleTop;
		public int visibleWidth;

		public Memory mem;
		public VicIIRegs regs;
		public ChipSignals signal;

		private Action FetchC;
		private Action FetchG;
		private Func<int> Plotter;
		private Action PerformCycleFunction;
		private SpriteRegs[] sprites;

		public VicII(ChipSignals newSignal, Region newRegion)
		{
			signal = newSignal;

			switch (newRegion)
			{
				case Region.NTSC:
					totalCycles = 65;
					rasterTotalLines = 263;
					rasterLineLeft = 0x19C;
					cycleLeft = 0;
					spriteFetchStartCycle = 59;
					visibleLeft = 0x008;
					visibleRight = 0x168;
					visibleTop = 0x023; //0x041;
					visibleBottom = 0x004; //0x013;
					visibleRenderX = false;
					visibleRenderY = false;
					visibleWidth = 352;
					visibleHeight = 232;
					renderOffset = 0;
					PerformCycleFunction = PerformCycleNTSC;
					break;
				case Region.PAL:
					break;
				default:
					break;
			}
			HardReset();
		}

		// increment raster line
		private void AdvanceRaster()
		{
			regs.RASTER++;

			// if we reach the bottom, reset to the top
			if (regs.RASTER == rasterTotalLines)
			{
				regs.RASTER = 0;
				regs.VCBASE = 0;
				displayEnabled = false;
			}

			// check to see if we are within viewing area Y
			if (regs.RASTER == visibleBottom)
			{
				visibleRenderY = false;
				renderOffset = 0;
			}
			if (regs.RASTER == visibleTop)
				visibleRenderY = true;
		}

		// standard text mode
		public void Fetch000C()
		{
			int cAddress = (regs.VM << 10) | regs.VC;
			characterDataBus = mem.VicRead((ushort)cAddress);
			colorDataBus = mem.colorRam[regs.VC];
		}
		public void Fetch000G()
		{
			int gAddress = (regs.CB << 11) | (characterData << 3) | regs.RC;
			bitmapData = mem.VicRead((ushort)gAddress);
		}

		// standard bitmap mode
		public void Fetch010G()
		{
			int gAddress = ((regs.CB & 0x4) << 11) | (regs.VC << 3) | regs.RC;
			bitmapData = mem.VicRead((ushort)gAddress);
		}

		// extra color text mode
		public void Fetch100G()
		{
			int gAddress = (regs.CB << 11) | ((characterData & 0x3F) << 3) | regs.RC;
			bitmapData = mem.VicRead((ushort)gAddress);
		}

		// invalid bitmap mode 110
		public void Fetch110G()
		{
			int gAddress = ((regs.CB & 0x4) << 11) | ((regs.VC & 0x33F) << 3) | regs.RC;
			bitmapData = mem.VicRead((ushort)gAddress);
		}

		// idle fetch
		public void FetchIdleC()
		{
			characterDataBus = 0;
			colorDataBus = 0;
		}
		public void FetchIdleG()
		{
			if (regs.ECM)
				mem.VicRead(0x39FF);
			else
				mem.VicRead(0x3FFF);
		}

		public void HardReset()
		{
			// initialize raster
			rasterWidth = totalCycles * 8;
			rasterOffsetX = rasterLineLeft;
			borderOnMain = true;
			borderOnVertical = true;

			// initialize buffer
			buffer = new int[visibleWidth * visibleHeight];
			bufferSize = buffer.Length;

			// initialize screen buffer
			characterMemory = new byte[40];
			colorMemory = new byte[40];
			pixelBuffer = new int[12];
			pixelBufferForeground = new bool[12];
			pixelBufferIndex = 0;

			// initialize registers
			spriteFetchIndex = 0;
			idle = true;
			refreshAddress = 0x3FFF;
			regs = new VicIIRegs();
			sprites = regs.Sprites;
			regs.RC = 7;
			signal.VicAEC = true;
			signal.VicIRQ = false;

			// initialize screen
			UpdateBorder();
			UpdatePlotter();

			// some helpful values
			cyclesPerFrame = totalCycles * rasterTotalLines;

			// initialize sprite generators
			spriteGenerators = new SpriteGenerator[8];
			for (int i = 0; i < 8; i++)
				spriteGenerators[i] = new SpriteGenerator(regs, i, rasterWidth, rasterLineLeft);
		}

		public byte Peek(int addr)
		{
			return regs[addr & 0x3F];
		}

		private void PerformBorderCheck()
		{
			if ((regs.RASTER == borderTop) && (regs.DEN))
				borderOnVertical = false;
			if (regs.RASTER == borderBottom)
				borderOnVertical = true;
		}

		public void PerformCycle()
		{
			if (cycle >= totalCycles)
			{
				cycle = 0;
				AdvanceRaster();
			}

			PerformCycleCommon();
			PerformCycleFunction();
			RenderCycle();

			// increment cycle
			cycle++;
			UpdateInterrupts();
			signal.VicIRQ = regs.IRQ;
		}

		// these operations are done every cycle
		private void PerformCycleCommon()
		{
			// display enable check on line 030 (this must be run every cycle)
			if (regs.RASTER == 0x030)
				displayEnabled = (displayEnabled | regs.DEN);

			// badline check
			if (regs.RASTER >= 0x030 && regs.RASTER < 0x0F8)
				badLine = ((regs.YSCROLL == (regs.RASTER & 0x07)) && displayEnabled);
			else
				badLine = false;

			if (badLine)
				idle = false;

			// sprite Y stretch flipflop
			for (int i = 0; i < 8; i++)
				if (!sprites[i].MxYE)
					sprites[i].MxYEToggle = true;
		}

		// operations timed to NTSC
		private void PerformCycleNTSC()
		{
			switch (cycle)
			{
				case 0:
					// rasterline IRQ happens on cycle 1 on rasterline 0
					if (regs.RASTER > 0 && regs.RASTER == rasterInterruptLine)
						regs.IRST = true;
					PerformSpritePointerFetch(3);
					break;
				case 1:
					// rasterline IRQ happens on cycle 1 on rasterline 0
					if (regs.RASTER == 0 && regs.RASTER == rasterInterruptLine)
						regs.IRST = true;
					PerformSpriteDataFetch(3);
					break;
				case 2:
					PerformSpritePointerFetch(4);
					break;
				case 3:
					PerformSpriteDataFetch(4);
					break;
				case 4:
					PerformSpritePointerFetch(5);
					break;
				case 5:
					PerformSpriteDataFetch(5);
					break;
				case 6:
					PerformSpritePointerFetch(6);
					break;
				case 7:
					PerformSpriteDataFetch(6);
					break;
				case 8:
					PerformSpritePointerFetch(7);
					break;
				case 9:
					PerformSpriteDataFetch(7);
					break;
				case 10:
					signal.VicAEC = true;
					PerformDRAMRefresh();
					break;
				case 11:
					PerformDRAMRefresh();
					break;
				case 12:
					PerformDRAMRefresh();
					break;
				case 13:
					PerformVCReset();
					PerformDRAMRefresh();
					break;
				case 14:
					PerformDRAMRefresh();
					break;
				case 15:
					spriteGenerators[0].Render();
					spriteGenerators[1].Render();
					spriteGenerators[2].Render();
					spriteGenerators[3].Render();
					spriteGenerators[4].Render();
					spriteGenerators[5].Render();
					spriteGenerators[6].Render();
					spriteGenerators[7].Render();
					PerformSpriteMCBASEAdvance();
					PerformScreenCAccess();
					break;
				case 16:
					PerformScreenCAccess();
					break;
				case 17:
					PerformScreenCAccess();
					break;
				case 18:
					PerformScreenCAccess();
					break;
				case 19:
					PerformScreenCAccess();
					break;
				case 20:
					PerformScreenCAccess();
					break;
				case 21:
					PerformScreenCAccess();
					break;
				case 22:
					PerformScreenCAccess();
					break;
				case 23:
					PerformScreenCAccess();
					break;
				case 24:
					PerformScreenCAccess();
					break;
				case 25:
					PerformScreenCAccess();
					break;
				case 26:
					PerformScreenCAccess();
					break;
				case 27:
					PerformScreenCAccess();
					break;
				case 28:
					PerformScreenCAccess();
					break;
				case 29:
					PerformScreenCAccess();
					break;
				case 30:
					PerformScreenCAccess();
					break;
				case 31:
					PerformScreenCAccess();
					break;
				case 32:
					PerformScreenCAccess();
					break;
				case 33:
					PerformScreenCAccess();
					break;
				case 34:
					PerformScreenCAccess();
					break;
				case 35:
					PerformScreenCAccess();
					break;
				case 36:
					PerformScreenCAccess();
					break;
				case 37:
					PerformScreenCAccess();
					break;
				case 38:
					PerformScreenCAccess();
					break;
				case 39:
					PerformScreenCAccess();
					break;
				case 40:
					PerformScreenCAccess();
					break;
				case 41:
					PerformScreenCAccess();
					break;
				case 42:
					PerformScreenCAccess();
					break;
				case 43:
					PerformScreenCAccess();
					break;
				case 44:
					PerformScreenCAccess();
					break;
				case 45:
					PerformScreenCAccess();
					break;
				case 46:
					PerformScreenCAccess();
					break;
				case 47:
					PerformScreenCAccess();
					break;
				case 48:
					PerformScreenCAccess();
					break;
				case 49:
					PerformScreenCAccess();
					break;
				case 50:
					PerformScreenCAccess();
					break;
				case 51:
					PerformScreenCAccess();
					break;
				case 52:
					PerformScreenCAccess();
					break;
				case 53:
					PerformScreenCAccess();
					break;
				case 54:
					PerformScreenCAccess();
					PerformSpriteYExpansionFlip();
					PerformSpriteComparison();
					break;
				case 55:
					signal.VicAEC = true;
					PerformSpriteComparison();
					break;
				case 56:
					break;
				case 57:
					PerformSpriteDMAEnable();
					PerformRCReset();
					break;
				case 58:
					break;
				case 59:
					PerformSpritePointerFetch(0);
					break;
				case 60:
					PerformSpriteDataFetch(0);
					break;
				case 61:
					PerformSpritePointerFetch(1);
					break;
				case 62:
					PerformSpriteDataFetch(1);
					break;
				case 63:
					PerformSpritePointerFetch(2);
					PerformBorderCheck();
					break;
				case 64:
					PerformSpriteDataFetch(2);
					break;
			}
		}

		// operations timed to PAL
		private void PerformCyclePAL()
		{
		}

		private void PerformDRAMRefresh()
		{
			// dram refresh
			mem.VicRead((ushort)refreshAddress);
			refreshAddress = (refreshAddress - 1) & 0xFF;
			refreshAddress |= 0x3F00;
		}

		private void PerformRCReset()
		{
			// row counter processing
			if (regs.RC == 7)
			{
				idle = true;
				regs.VCBASE = regs.VC;
			}
			if (!idle)
				regs.RC = (regs.RC + 1) & 0x07;
		}

		private void PerformScreenCAccess()
		{
			// screen memory c-access
			if (badLine)
			{
				FetchC();
				colorMemory[regs.VMLI] = colorDataBus;
				characterMemory[regs.VMLI] = characterDataBus;
			}
			signal.VicAEC = !badLine;
		}

		private void PerformSpriteComparison()
		{
			// sprite comparison
			for (int i = 0; i < 8; i++)
			{
				if (sprites[i].MxE == true && sprites[i].MxY == (regs.RASTER & 0xFF) && sprites[i].MDMA == false)
				{
					sprites[i].MDMA = true;
					sprites[i].MCBASE = 0;
					if (sprites[i].MxYE)
						sprites[i].MxYEToggle = false;
				}
				sprites[i].MxXEToggle = false;
			}
		}

		private void PerformSpriteDataFetch(int spriteIndex)
		{
			// second half of the fetch cycle
			signal.VicAEC = !sprites[spriteIndex].MDMA;
			if (sprites[spriteIndex].MDMA)
			{
				for (int i = 0; i < 2; i++)
				{
					sprites[spriteIndex].MSR <<= 8;
					sprites[spriteIndex].MSR |= mem.VicRead((ushort)((sprites[spriteIndex].MPTR << 6) | (sprites[spriteIndex].MC)));
					sprites[spriteIndex].MC++;
				}
			}
		}

		private void PerformSpriteDMAEnable()
		{
			// sprite MC processing
			for (int i = 0; i < 8; i++)
			{
				sprites[i].MC = sprites[i].MCBASE;
				if (sprites[i].MDMA && sprites[i].MxY == (regs.RASTER & 0xFF))
				{
					sprites[i].MD = true;
					sprites[i].MxXEToggle = false;
				}
			}
		}

		private void PerformSpriteMCBASEAdvance()
		{
			for (int i = 0; i < 8; i++)
			{
				if (sprites[i].MxYEToggle)
				{
					sprites[i].MCBASE += 3;
					if (sprites[i].MxYEToggle && sprites[i].MCBASE == 63)
					{
						sprites[i].MD = false;
						sprites[i].MDMA = false;
					}
				}
			}
		}

		private void PerformSpritePointerFetch(int spriteIndex)
		{
			// first half of the fetch cycle, always fetch pointer
			ushort pointerOffset = (ushort)((regs.VM << 10) | 0x3F8 | spriteIndex);
			sprites[spriteIndex].MPTR = mem.VicRead(pointerOffset);

			// also fetch upper 8 bits if enabled
			signal.VicAEC = !sprites[spriteIndex].MDMA;
			if (sprites[spriteIndex].MDMA)
			{
				sprites[spriteIndex].MSRC = 24;
				sprites[spriteIndex].MSR = mem.VicRead((ushort)((sprites[spriteIndex].MPTR << 6) | (sprites[spriteIndex].MC)));
				sprites[spriteIndex].MC++;
			}
		}

		private void PerformSpriteYExpansionFlip()
		{
			for (int i = 0; i < 8; i++)
				if (sprites[i].MxYE)
					sprites[i].MxYEToggle = !sprites[i].MxYEToggle;
		}

		private void PerformVCReset()
		{
			// VC reset
			regs.VC = regs.VCBASE;
			regs.VMLI = 0;
			characterColumn = 0;
			if (badLine)
			{
				regs.RC = 0;
			}
			bitmapData = 0;
			colorData = 0;
			characterData = 0;
		}

		public void Poke(int addr, byte val)
		{
			regs[addr & 0x3F] = val;
		}

		// standard text mode
		private int Plot000()
		{
			if (characterColumn >= 0)
			{
				byte charData = bitmapData;
				charData <<= characterColumn;
				if ((charData & 0x80) != 0x00)
				{
					dataForeground = true;
					return colorData;
				}
			}
			dataForeground = false;
			return regs.BxC[0];
		}

		// multicolor text mode
		private int Plot001()
		{
			if (characterColumn >= 0)
			{
				if ((colorData & 0x08) != 0x00)
				{
					int offset = characterColumn;
					byte charData = bitmapData;
					offset |= 0x01;
					offset ^= 0x01;
					charData <<= offset;
					charData >>= 6;
					switch (charData)
					{
						case 1:
							dataForeground = false;
							return regs.BxC[1];
						case 2:
							dataForeground = true;
							return regs.BxC[2];
						case 3:
							dataForeground = true;
							return colorData & 0x07;
					}
				}
				else
				{
					return Plot000();
				}
			}
			dataForeground = false;
			return regs.BxC[0];
		}

		// standard bitmap mode
		private int Plot010()
		{
			if (characterColumn >= 0)
			{
				byte charData = bitmapData;
				charData <<= characterColumn;
				if ((charData & 0x80) != 0x00)
				{
					dataForeground = true;
					return characterData >> 4;
				}
			}
			dataForeground = false;
			return characterData & 0xF;
		}

		// multicolor bitmap mode
		private int Plot011()
		{
			if (characterColumn >= 0)
			{
				int offset = characterColumn;
				byte charData = bitmapData;
				offset |= 0x01;
				offset ^= 0x01;
				charData <<= offset;
				charData >>= 6;
				switch (charData)
				{
					case 1:
						dataForeground = false;
						return characterData >> 4;
					case 2:
						dataForeground = true;
						return characterData & 0xF;
					case 3:
						dataForeground = true;
						return colorData & 0xF;
				}
			}
			dataForeground = false;
			return regs.BxC[0];
		}

		// extra color text mode
		private int Plot100()
		{
			if (characterColumn >= 0)
			{
				byte charData = bitmapData;
				charData <<= characterColumn;
				if ((charData & 0x80) != 0x00)
				{
					dataForeground = true;
					return colorData;
				}
				else
				{
					dataForeground = false;
					return regs.BxC[characterData >> 6];
				}
			}
			dataForeground = false;
			return regs.BxC[0];
		}

		// invalid mode (TODO: implement collision)
		// this mode always outputs black
		private int Plot101()
		{
			dataForeground = false;
			return 0;
		}

		// invalid mode (TODO: implement collision)
		// this mode always outputs black
		private int Plot110()
		{
			dataForeground = false;
			return 0;
		}

		// invalid mode (TODO: implement collision)
		// this mode always outputs black
		private int Plot111()
		{
			dataForeground = false;
			return 0;
		}

		public byte Read(ushort addr)
		{
			byte result = 0;
			addr &= 0x3F;

			switch (addr)
			{
				case 0x1E:
					// clear after read
					result = regs[addr];
					regs[addr] = 0x00;
					regs.IMMC = false;
					break;
				case 0x1F:
					// clear after read
					result = regs[addr];
					regs[addr] = 0x00;
					regs.IMBC = false;
					break;
				default:
					result = regs[addr];
					break;
			}

			return result;
		}

		private void RenderCycle()
		{
			int inputPixel;
			int outputPixel;
			int spriteBits;
			int spritePixel;
			int spritePixelOwner;

			for (int i = 0; i < 8; i++)
			{
				spritePixelOwner = -1;
				// draw screen memory if needed
				if (!idle && cycle >= 15 && cycle < 55)
				{
					if (regs.XSCROLL == i)
					{
						characterColumn = 0;
						characterData = characterMemory[regs.VMLI];
						colorData = colorMemory[regs.VMLI];
						FetchG();
						regs.VC++;
						regs.VMLI++;
					}
				}

				// check to see if we are within viewing area X
				if (rasterOffsetX == visibleRight)
					visibleRenderX = false;
				if (rasterOffsetX == visibleLeft)
					visibleRenderX = true;

				// check to see if we are at the border
				if (rasterOffsetX == borderRight)
					borderOnMain = true;
				if (rasterOffsetX == borderLeft)
				{
					if (regs.RASTER == borderBottom)
						borderOnVertical = true;
					if ((regs.RASTER == borderTop) && regs.DEN)
						borderOnVertical = false;
					if (!borderOnVertical)
						borderOnMain = false;
				}

				// render plotter
				if (idle)
					inputPixel = regs.BxC[0];
				else
					inputPixel = Plotter();

				// render sprites
				outputPixel = pixelBuffer[pixelBufferIndex];

				for (int j = 0; j < 8; j++)
				{
					if (spriteGenerators[j].hasData)
					{
						if (spriteGenerators[j].dataBuffer[rasterOffsetX] != 0)
						{

							// process collisions if the border is off
							if (!borderOnVertical)
							{
								if (spritePixelOwner == -1)
								{
									spritePixelOwner = j;
									if (!sprites[j].MxDP || (!pixelBufferForeground[pixelBufferIndex]))
									{
										outputPixel = spriteGenerators[j].colorBuffer[rasterOffsetX];
									}
								}
								else
								{
									// a sprite already occupies this space
									sprites[spritePixelOwner].MxM = true;
									sprites[j].MxM = true;
									regs.IMMC = true;
								}
								if (pixelBufferForeground[pixelBufferIndex])
								{
									sprites[j].MxD = true;
									regs.IMBC = true;
								}
							}
						}
					}
				}

				// send pixel to bitmap
				if (visibleRenderX && visibleRenderY)
				{
					if (borderOnMain || borderOnVertical)
					{
						WritePixel(regs.EC);
					}
					else
					{
						WritePixel(outputPixel);
					}
				}

				// process 12 pixel delay
				pixelBuffer[pixelBufferIndex] = inputPixel;
				pixelBufferForeground[pixelBufferIndex] = dataForeground;
				pixelBufferIndex++;
				if (pixelBufferIndex == 12)
				{
					pixelBufferIndex = 0;
				}

				// advance raster X
				characterColumn++;
				rasterOffset++;
				rasterOffsetX++;
				if (rasterOffsetX == rasterWidth)
				{
					rasterOffsetX -= rasterWidth;
				}
			}
		}

		private void UpdateBorder()
		{
			borderTop = regs.RSEL ? 0x033 : 0x037;
			borderBottom = regs.RSEL ? 0x0FB : 0x0F7;
			borderLeft = regs.CSEL ? 0x018 : 0x01F;
			borderRight = regs.CSEL ? 0x158 : 0x14F;
		}

		private void UpdateInterrupts()
		{
			// check for anything that would've triggered an interrupt and raise the flag if so
			regs.IRQ = ((regs.IRST & regs.ERST) || (regs.IMMC & regs.EMMC) || (regs.IMBC & regs.EMBC) || (regs.ILP & regs.ELP));
		}

		private void UpdatePlotter()
		{
			// determine the plot and fetch mode
			if (!regs.ECM && !regs.BMM && !regs.MCM)
			{
				FetchC = Fetch000C;
				FetchG = Fetch000G;
				Plotter = Plot000;
			}
			else if (!regs.ECM && !regs.BMM && regs.MCM)
			{
				FetchC = Fetch000C;
				FetchG = Fetch000G;
				Plotter = Plot001;
			}
			else if (!regs.ECM && regs.BMM && !regs.MCM)
			{
				FetchC = Fetch000C;
				FetchG = Fetch010G;
				Plotter = Plot010;
			}
			else if (!regs.ECM && regs.BMM && regs.MCM)
			{
				FetchC = Fetch000C;
				FetchG = Fetch010G;
				Plotter = Plot011;
			}
			else if (regs.ECM && !regs.BMM && !regs.MCM)
			{
				FetchC = Fetch000C;
				FetchG = Fetch100G;
				Plotter = Plot100;
			}
			else if (regs.ECM && !regs.BMM && regs.MCM)
			{
				FetchC = Fetch000C;
				FetchG = Fetch100G;
				Plotter = Plot101;
			}
			else if (regs.ECM && regs.BMM && !regs.MCM)
			{
				FetchC = Fetch000C;
				FetchG = Fetch110G;
				Plotter = Plot110;
			}
			else
			{
				FetchC = Fetch000C;
				FetchG = Fetch110G;
				Plotter = Plot111;
			}
		}

		public void Write(ushort addr, byte val)
		{
			addr &= 0x3F;

			switch (addr)
			{
				case 0x11:
					rasterInterruptLine &= 0xFF;
					rasterInterruptLine |= (val & 0x80) << 1;
					// raster upper bit can't be changed, save and restore the value
					val &= 0x7F;
					val |= (byte)(regs[addr] & 0x80);
					regs[addr] = val;
					UpdateBorder();
					UpdatePlotter();
					break;
				case 0x12:
					// raster interrupt lower 8 bits
					rasterInterruptLine &= 0x100;
					rasterInterruptLine |= (val & 0xFF);
					break;
				case 0x16:
					regs[addr] = val;
					UpdateBorder();
					UpdatePlotter();
					break;
				case 0x19:
					// only allow clearing of these flags
					if ((val & 0x01) != 0x00)
						regs.IRST = false;
					if ((val & 0x02) != 0x00)
						regs.IMBC = false;
					if ((val & 0x04) != 0x00)
						regs.IMMC = false;
					if ((val & 0x08) != 0x00)
						regs.ILP = false;
					UpdateInterrupts();
					break;
				case 0x1E:
				case 0x1F:
					// can't write to these regs
					break;
				default:
					regs[addr] = val;
					break;
			}
		}

		private void WritePixel(int value)
		{
			buffer[renderOffset++] = palette[value];
		}
	}
}
