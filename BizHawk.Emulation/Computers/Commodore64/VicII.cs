using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public enum VicIIMode
	{
		NTSC,
		PAL
	}

	public class VicIIRegs
	{
		public bool BMM;
		public int[] BxC = new int[4];
		public int CB;
		public bool CSEL;
		public bool DEN;
		public int EC;
		public bool ECM;
		public bool ELP;
		public bool EMBC;
		public bool EMMC;
		public bool ERST;
		public bool ILP;
		public bool IMBC;
		public bool IMMC;
		public bool IRQ;
		public bool IRST;
		public int LPX;
		public int LPY;
		public bool MCM;
		public int[] MMx = new int[2];
		public int[] MxC = new int[8];
		public bool[] MxD = new bool[8];
		public bool[] MxDP = new bool[8];
		public bool[] MxE = new bool[8];
		public bool[] MxM = new bool[8];
		public bool[] MxMC = new bool[8];
		public int[] MxX = new int[8];
		public bool[] MxXE = new bool[8];
		public int[] MxY = new int[8];
		public bool[] MxYE = new bool[8];
		public int RASTER;
		public int RC;
		public bool RES;
		public bool RSEL;
		public int VC;
		public int VCBASE;
		public int VM;
		public int VMLI;
		public int XSCROLL;
		public int YSCROLL;

		public VicIIRegs()
		{
			// power on state
			
			this[0x16] = 0xC0;
			this[0x18] = 0x01;
			this[0x19] = 0x71;
			this[0x1A] = 0xF0;
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
						result = MxX[addr >> 1];
						break;
					case 0x01:
					case 0x03:
					case 0x05:
					case 0x07:
					case 0x09:
					case 0x0B:
					case 0x0D:
					case 0x0F:
						result = MxY[addr >> 1];
						break;
					case 0x10:
						result = ((MxX[0] & 0x100) != 0) ? 0x01 : 0x00;
						result |= ((MxX[1] & 0x100) != 0) ? 0x02 : 0x00;
						result |= ((MxX[2] & 0x100) != 0) ? 0x04 : 0x00;
						result |= ((MxX[3] & 0x100) != 0) ? 0x08 : 0x00;
						result |= ((MxX[4] & 0x100) != 0) ? 0x10 : 0x00;
						result |= ((MxX[5] & 0x100) != 0) ? 0x20 : 0x00;
						result |= ((MxX[6] & 0x100) != 0) ? 0x40 : 0x00;
						result |= ((MxX[7] & 0x100) != 0) ? 0x80 : 0x00;
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
						result = (MxE[0] ? 0x01 : 0x00);
						result |= (MxE[1] ? 0x02 : 0x00);
						result |= (MxE[2] ? 0x04 : 0x00);
						result |= (MxE[3] ? 0x08 : 0x00);
						result |= (MxE[4] ? 0x10 : 0x00);
						result |= (MxE[5] ? 0x20 : 0x00);
						result |= (MxE[6] ? 0x40 : 0x00);
						result |= (MxE[7] ? 0x80 : 0x00);
						break;
					case 0x16:
						result &= 0xC0;
						result |= XSCROLL & 0x07;
						result |= (CSEL ? 0x08 : 0x00);
						result |= (MCM ? 0x10 : 0x00);
						result |= (RES ? 0x20 : 0x00);
						break;
					case 0x17:
						result = (MxYE[0] ? 0x01 : 0x00);
						result |= (MxYE[1] ? 0x02 : 0x00);
						result |= (MxYE[2] ? 0x04 : 0x00);
						result |= (MxYE[3] ? 0x08 : 0x00);
						result |= (MxYE[4] ? 0x10 : 0x00);
						result |= (MxYE[5] ? 0x20 : 0x00);
						result |= (MxYE[6] ? 0x40 : 0x00);
						result |= (MxYE[7] ? 0x80 : 0x00);						
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
						result = (MxDP[0] ? 0x01 : 0x00);
						result |= (MxDP[1] ? 0x02 : 0x00);
						result |= (MxDP[2] ? 0x04 : 0x00);
						result |= (MxDP[3] ? 0x08 : 0x00);
						result |= (MxDP[4] ? 0x10 : 0x00);
						result |= (MxDP[5] ? 0x20 : 0x00);
						result |= (MxDP[6] ? 0x40 : 0x00);
						result |= (MxDP[7] ? 0x80 : 0x00);						
						break;
					case 0x1C:
						result = (MxMC[0] ? 0x01 : 0x00);
						result |= (MxMC[1] ? 0x02 : 0x00);
						result |= (MxMC[2] ? 0x04 : 0x00);
						result |= (MxMC[3] ? 0x08 : 0x00);
						result |= (MxMC[4] ? 0x10 : 0x00);
						result |= (MxMC[5] ? 0x20 : 0x00);
						result |= (MxMC[6] ? 0x40 : 0x00);
						result |= (MxMC[7] ? 0x80 : 0x00);						
						break;
					case 0x1D:
						result = (MxXE[0] ? 0x01 : 0x00);
						result |= (MxXE[1] ? 0x02 : 0x00);
						result |= (MxXE[2] ? 0x04 : 0x00);
						result |= (MxXE[3] ? 0x08 : 0x00);
						result |= (MxXE[4] ? 0x10 : 0x00);
						result |= (MxXE[5] ? 0x20 : 0x00);
						result |= (MxXE[6] ? 0x40 : 0x00);
						result |= (MxXE[7] ? 0x80 : 0x00);						
						break;
					case 0x1E:
						result = (MxM[0] ? 0x01 : 0x00);
						result |= (MxM[1] ? 0x02 : 0x00);
						result |= (MxM[2] ? 0x04 : 0x00);
						result |= (MxM[3] ? 0x08 : 0x00);
						result |= (MxM[4] ? 0x10 : 0x00);
						result |= (MxM[5] ? 0x20 : 0x00);
						result |= (MxM[6] ? 0x40 : 0x00);
						result |= (MxM[7] ? 0x80 : 0x00);						
						break;
					case 0x1F:
						result = (MxD[0] ? 0x01 : 0x00);
						result |= (MxD[1] ? 0x02 : 0x00);
						result |= (MxD[2] ? 0x04 : 0x00);
						result |= (MxD[3] ? 0x08 : 0x00);
						result |= (MxD[4] ? 0x10 : 0x00);
						result |= (MxD[5] ? 0x20 : 0x00);
						result |= (MxD[6] ? 0x40 : 0x00);
						result |= (MxD[7] ? 0x80 : 0x00);						
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
						result |= MxC[addr - 0x27] & 0x0F;
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
						MxX[index] &= 0x100;
						MxX[index] |= (val & 0xFF);
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
						MxY[index] &= 0x100;
						MxY[index] |= (val & 0xFF);
						break;
					case 0x10:
						MxX[0] = (MxX[0] & 0xFF) | ((val & 0x01) << 8);
						MxX[1] = (MxX[1] & 0xFF) | ((val & 0x02) << 7);
						MxX[2] = (MxX[2] & 0xFF) | ((val & 0x04) << 6);
						MxX[3] = (MxX[3] & 0xFF) | ((val & 0x08) << 5);
						MxX[4] = (MxX[4] & 0xFF) | ((val & 0x10) << 4);
						MxX[5] = (MxX[5] & 0xFF) | ((val & 0x20) << 3);
						MxX[6] = (MxX[6] & 0xFF) | ((val & 0x40) << 2);
						MxX[7] = (MxX[7] & 0xFF) | ((val & 0x80) << 1);
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
						MxE[0] = ((val & 0x01) != 0x00);
						MxE[1] = ((val & 0x02) != 0x00);
						MxE[2] = ((val & 0x04) != 0x00);
						MxE[3] = ((val & 0x08) != 0x00);
						MxE[4] = ((val & 0x10) != 0x00);
						MxE[5] = ((val & 0x20) != 0x00);
						MxE[6] = ((val & 0x40) != 0x00);
						MxE[7] = ((val & 0x80) != 0x00);
						break;
					case 0x16:
						XSCROLL = (val & 0x07);
						CSEL = ((val & 0x08) != 0x00);
						MCM = ((val & 0x10) != 0x00);
						RES = ((val & 0x20) != 0x00);
						break;
					case 0x17:
						MxYE[0] = ((val & 0x01) != 0x00);
						MxYE[1] = ((val & 0x02) != 0x00);
						MxYE[2] = ((val & 0x04) != 0x00);
						MxYE[3] = ((val & 0x08) != 0x00);
						MxYE[4] = ((val & 0x10) != 0x00);
						MxYE[5] = ((val & 0x20) != 0x00);
						MxYE[6] = ((val & 0x40) != 0x00);
						MxYE[7] = ((val & 0x80) != 0x00);
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
						MxDP[0] = ((val & 0x01) != 0x00);
						MxDP[1] = ((val & 0x02) != 0x00);
						MxDP[2] = ((val & 0x04) != 0x00);
						MxDP[3] = ((val & 0x08) != 0x00);
						MxDP[4] = ((val & 0x10) != 0x00);
						MxDP[5] = ((val & 0x20) != 0x00);
						MxDP[6] = ((val & 0x40) != 0x00);
						MxDP[7] = ((val & 0x80) != 0x00);
						break;
					case 0x1C:
						MxMC[0] = ((val & 0x01) != 0x00);
						MxMC[1] = ((val & 0x02) != 0x00);
						MxMC[2] = ((val & 0x04) != 0x00);
						MxMC[3] = ((val & 0x08) != 0x00);
						MxMC[4] = ((val & 0x10) != 0x00);
						MxMC[5] = ((val & 0x20) != 0x00);
						MxMC[6] = ((val & 0x40) != 0x00);
						MxMC[7] = ((val & 0x80) != 0x00);
						break;
					case 0x1D:
						MxXE[0] = ((val & 0x01) != 0x00);
						MxXE[1] = ((val & 0x02) != 0x00);
						MxXE[2] = ((val & 0x04) != 0x00);
						MxXE[3] = ((val & 0x08) != 0x00);
						MxXE[4] = ((val & 0x10) != 0x00);
						MxXE[5] = ((val & 0x20) != 0x00);
						MxXE[6] = ((val & 0x40) != 0x00);
						MxXE[7] = ((val & 0x80) != 0x00);
						break;
					case 0x1E: 
						MxM[0] = ((val & 0x01) != 0x00);
						MxM[1] = ((val & 0x02) != 0x00);
						MxM[2] = ((val & 0x04) != 0x00);
						MxM[3] = ((val & 0x08) != 0x00);
						MxM[4] = ((val & 0x10) != 0x00);
						MxM[5] = ((val & 0x20) != 0x00);
						MxM[6] = ((val & 0x40) != 0x00);
						MxM[7] = ((val & 0x80) != 0x00);
						break;
					case 0x1F:
						MxD[0] = ((val & 0x01) != 0x00);
						MxD[1] = ((val & 0x02) != 0x00);
						MxD[2] = ((val & 0x04) != 0x00);
						MxD[3] = ((val & 0x08) != 0x00);
						MxD[4] = ((val & 0x10) != 0x00);
						MxD[5] = ((val & 0x20) != 0x00);
						MxD[6] = ((val & 0x40) != 0x00);
						MxD[7] = ((val & 0x80) != 0x00);
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
						MxC[addr - 0x27] = val & 0x0F;
						break;
				}
			}
		}
	}

	public enum VicIITask
	{
		Idle,
		VideoMatrix,
		CharGen,
		SpritePointer,
		SpriteData,
		DramRefresh
	}

	public class VicII
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
		public byte[] characterMemory;
		public bool charactersEnabled;
		public byte colorData;
		public byte[] colorMemory;
		public int cycle;
		public int cycleLeft;
		public bool displayEnabled;
		public bool hBlank;
		public bool idle;
		public int rasterInterruptLine;
		public int rasterLineLeft;
		public int rasterOffset;
		public int rasterOffsetX;
		public int rasterTotalLines;
		public int rasterWidth;
		public int refreshAddress;
		public int renderOffset;
		public byte[,] spriteData;
		public int spriteFetchCycle;
		public int spriteFetchIndex;
		public ushort[] spritePointers;
		public int spriteFetchStartCycle;
		public VicIITask task;
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

		public VicII(ChipSignals newSignal, VicIIMode videoMode)
		{
			signal = newSignal;

			switch (videoMode)
			{
				case VicIIMode.NTSC:
					totalCycles = 65;
					rasterTotalLines = 263;
					rasterLineLeft = 0x19C;
					cycleLeft = 0;
					spriteFetchStartCycle = 55;
					visibleLeft = 0x008;
					visibleRight = 0x168;
					visibleTop = 0x023; //0x041;
					visibleBottom = 0x004; //0x013;
					visibleRenderX = false;
					visibleRenderY = false;
					visibleWidth = 352;
					visibleHeight = 232;
					renderOffset = 0;
					break;
				case VicIIMode.PAL:
					break;
				default:
					break;
			}

			// initialize raster
			rasterWidth = totalCycles * 8;
			rasterOffsetX = rasterLineLeft;
			borderOnMain = true;
			borderOnVertical = true;

			// initialize buffer
			buffer = new int[visibleWidth * visibleHeight];
			bufferSize = buffer.Length;

			// initialize sprites
			spritePointers = new ushort[8];
			spriteData = new byte[8, 64];

			// initialize screen buffer
			characterMemory = new byte[40];
			colorMemory = new byte[40];

			// initialize registers
			HardReset();
		}

		// standard text mode
		public void Fetch000C()
		{
			int cAddress = (regs.VM << 10) | regs.VC;
			characterData = mem.VicRead((ushort)cAddress);
			colorData = mem.colorRam[regs.VC];
		}
		public void Fetch000G()
		{
			int gAddress = (regs.CB << 11) | (characterData << 3) | regs.RC;
			bitmapData = mem.VicRead((ushort)gAddress);
		}

		// multicolor text mode (same as standard text mode)
		public void Fetch001C()
		{
			Fetch000C();
		}
		public void Fetch001G()
		{
			Fetch000G();
		}

		// standard bitmap mode
		public void Fetch010C()
		{
			Fetch000C();
		}
		public void Fetch010G()
		{
			int gAddress = ((regs.CB & 0x4) << 11) | (regs.VC << 3) | regs.RC;
			bitmapData = mem.VicRead((ushort)gAddress);
		}

		// multicolor bitmap mode (same as standard bitmap mode)
		public void Fetch011C()
		{
			Fetch000C();
		}
		public void Fetch011G()
		{
			Fetch010G();
		}

		// extra color text mode
		public void Fetch100C()
		{
			Fetch000C();
		}
		public void Fetch100G()
		{
			int gAddress = (regs.CB << 11) | ((characterData & 0x3F) << 3) | regs.RC;
			bitmapData = mem.VicRead((ushort)gAddress);
		}

		// invalid text mode 101
		public void Fetch101C()
		{
			Fetch000C();
		}
		public void Fetch101G()
		{
			Fetch100G();
		}

		// invalid bitmap mode 110
		public void Fetch110C()
		{
			Fetch000C();
		}
		public void Fetch110G()
		{
			int gAddress = ((regs.CB & 0x4) << 11) | ((regs.VC & 0x33F) << 3) | regs.RC;
			bitmapData = mem.VicRead((ushort)gAddress);
		}

		// invalid bitmap mode 111
		public void Fetch111C()
		{
			Fetch000C();
		}
		public void Fetch111G()
		{
			Fetch110G();
		}

		// idle fetch
		public void FetchIdleC()
		{
			characterData = 0;
			colorData = 0;
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
			idle = true;
			refreshAddress = 0x3FFF;
			regs = new VicIIRegs();
			regs.RC = 7;
			signal.VicAEC = true;
			signal.VicIRQ = false;
			UpdateBorder();
			UpdatePlotter();
		}

		public void PerformCycle()
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

			// these actions are exclusive
			if ((regs.RASTER == 0x000 && cycle == 1) || (regs.RASTER > 0x000 && cycle == 0))
			{
				// IRQ is processed on cycle 1 of line 0 and cycle 0 on all other lines
				if (regs.RASTER == rasterInterruptLine)
					regs.IRST = true;
			}
			else if (cycle >= 10 && cycle < 15)
			{
				// dram refresh
				mem.VicRead((ushort)refreshAddress);
				refreshAddress = (refreshAddress - 1) & 0xFF;
				refreshAddress |= 0x3F00;
			}
			else if (cycle >= 16 && cycle < 56)
			{
				// screen memory c-access
				if (badLine)
				{
					FetchC();
					characterMemory[regs.VMLI] = characterData;
					colorMemory[regs.VMLI] = colorData;
				}
			}
			else if (cycle == 55)
			{
				//signal.VicAEC = true;
			}
			else if (cycle == 57)
			{
				if (regs.RC == 7)
				{
					idle = true;
					regs.VCBASE = regs.VC;
				}
				if (!idle)
					regs.RC = (regs.RC + 1) & 0x07;
			}

			// these actions can fall within the other ranges
			if (cycle == 13)
			{
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
			else if (cycle == spriteFetchStartCycle)
			{
				spriteFetchIndex = 0;
			}

			// sprite fetch
			if (spriteFetchIndex < 8)
			{
				
				PerformCycleSpriteFetch(spriteFetchIndex, spriteFetchCycle);
				spriteFetchCycle++;
				if (spriteFetchCycle >= 2)
				{
					spriteFetchCycle = 0;
					spriteFetchIndex++;
				}
			}

			// border check
			if (cycle == 63)
			{
				if ((regs.RASTER == borderTop) && (regs.DEN))
					borderOnVertical = false;
				if (regs.RASTER == borderBottom)
					borderOnVertical = true;
			}

			PerformCycleRender();

			// increment cycle
			cycle++;
			if (cycle == totalCycles)
			{
				cycle = 0;
				PerformCycleAdvanceRaster();
			}

			UpdateInterrupts();
			signal.VicIRQ = regs.IRQ;
		}

		private void PerformCycleAdvanceRaster()
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

		private void PerformCycleRender()
		{
			for (int i = 0; i < 8; i++)
			{
				// draw screen memory if needed
				if (!idle && cycle >= 16 && cycle < 56)
				{
					// todo: implement XSCROLL properly
					if (regs.XSCROLL == (rasterOffsetX & 0x7))
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

				// render pixel
				if (visibleRenderX && visibleRenderY)
				{
					if (borderOnMain || borderOnVertical)
						WritePixel(regs.EC);
					else
						WritePixel(Plotter());
				}
				characterColumn++;
				rasterOffset++;
				rasterOffsetX++;
				if (rasterOffsetX == rasterWidth)
				{
					rasterOffsetX -= rasterWidth;
				}
			}

		}

		private void PerformCycleSpriteFetch(int index, int fetchCycle)
		{
			ushort offset = (ushort)((regs.VM << 10) + 0x3F8 + index);
			spritePointers[index] = (ushort)((regs.VM << 10) + (mem.VicRead(offset) * 64));
			for (ushort i = 0; i < 64; i++)
				spriteData[index, i] = mem.VicRead((ushort)(spritePointers[index] + i));
		}

		// standard text mode
		private int Plot000()
		{
			if (characterColumn >= 0)
			{
				byte charData = bitmapData;
				charData <<= characterColumn;
				if ((charData & 0x80) != 0x00)
					return colorData;
			}
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
							return regs.BxC[1];
						case 2:
							return regs.BxC[2];
						case 3:
							return colorData & 0x07;
					}
				}
				else
				{
					return Plot000();
				}
			}
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
					return characterData >> 4;
			}
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
						return characterData & 0xF;
					case 2:
						return characterData >> 4;
					case 3:
						return colorData & 0xF;
				}
			}
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
					return colorData;
				else
					return regs.BxC[characterData >> 6];
			}
			return regs.BxC[0];
		}

		// invalid mode
		private int Plot101()
		{
			return regs.BxC[0];
		}

		// invalid mode
		private int Plot110()
		{
			return regs.BxC[0];
		}

		// invalid mode
		private int Plot111()
		{
			return regs.BxC[0];
		}

		public byte Read(ushort addr)
		{
			byte result = 0;
			addr &= 0x3F;

			switch (addr)
			{
				case 0x1E:
				case 0x1F:
					// collision regs clear after read
					result = regs[addr];
					regs[addr] = 0x00;
					break;
				default:
					result = regs[addr];
					break;
			}

			return result;
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
				FetchC = Fetch001C;
				FetchG = Fetch001G;
				Plotter = Plot001;
			}
			else if (!regs.ECM && regs.BMM && !regs.MCM)
			{
				FetchC = Fetch010C;
				FetchG = Fetch010G;
				Plotter = Plot010;
			}
			else if (!regs.ECM && regs.BMM && regs.MCM)
			{
				FetchC = Fetch011C;
				FetchG = Fetch011G;
				Plotter = Plot011;
			}
			else if (regs.ECM && !regs.BMM && !regs.MCM)
			{
				FetchC = Fetch100C;
				FetchG = Fetch100G;
				Plotter = Plot100;
			}
			else if (regs.ECM && !regs.BMM && regs.MCM)
			{
				FetchC = Fetch101C;
				FetchG = Fetch101G;
				Plotter = Plot101;
			}
			else if (regs.ECM && regs.BMM && !regs.MCM)
			{
				FetchC = Fetch110C;
				FetchG = Fetch110G;
				Plotter = Plot110;
			}
			else
			{
				FetchC = Fetch111C;
				FetchG = Fetch111G;
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
