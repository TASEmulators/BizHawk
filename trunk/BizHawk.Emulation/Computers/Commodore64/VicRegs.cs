using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicII
	{
		private class Sprite
		{
			public uint color;
			public bool dataCollision;
			public bool display;
			public bool dma;
			public bool enable;
			public bool exp;
			public uint mc;
			public uint mcBase;
			public bool mcChange;
			public bool multicolor;
			public bool priority;
			public uint ptr;
			public uint ptrinc;
			public bool spriteCollision;
			public uint x;
			public uint xShift;
			public uint xShiftSum;
			public bool xExpand;
			public uint y;
			public bool yExpand;

			public void SyncState(Serializer ser)
			{
				ser.Sync("color", ref color);
				ser.Sync("dataCollision", ref dataCollision);
				ser.Sync("display", ref display);
				ser.Sync("dma", ref dma);
				ser.Sync("enable", ref enable);
				ser.Sync("exp", ref exp);
				ser.Sync("mc", ref mc);
				ser.Sync("mcBase", ref mcBase);
				ser.Sync("mcChange", ref mcChange);
				ser.Sync("multicolor", ref multicolor);
				ser.Sync("priority", ref priority);
				ser.Sync("ptr", ref ptr);
				ser.Sync("ptrinc", ref ptrinc);
				ser.Sync("spriteCollision", ref spriteCollision);
				ser.Sync("x", ref x);
				ser.Sync("xShift", ref xShift);
				ser.Sync("xShiftSum", ref xShiftSum);
				ser.Sync("xExpand", ref xExpand);
				ser.Sync("y", ref y);
				ser.Sync("yExpand", ref yExpand);
			}
		}

		private uint backgroundColor0;
		private uint backgroundColor1;
		private uint backgroundColor2;
		private uint backgroundColor3;
		private bool badline;
		private bool badlineEnabled;
		private bool bitmapMode;
		private uint bitmapRam;
		private uint borderBottom;
		private uint borderColor;
		private uint borderLeft;
		private bool borderOnMain;
		private bool borderOnVertical;
		private bool borderOnVerticalEnable;
		private uint borderRight;
		private uint borderTop;
		private bool columnSelect;
		private bool cycleFetchG;
		private bool cycleFetchP;
		private bool cycleFetchR;
		private bool cycleFetchS;
		private bool displayEnable;
		private bool enableIrqDataCollision;
		private bool enableIrqLightPen;
		private bool enableIrqRaster;
		private bool enableIrqSpriteCollision;
		private bool extraColorMode;
		private bool idle;
		private bool irq;
		private bool irqDataCollision;
		private bool irqLightPen;
		private bool irqRaster;
		private bool irqSpriteCollision;
		private uint lightPenX;
		private uint lightPenY;
		private bool multiColorMode;
		private uint phaseRead0;
		private uint phaseRead1;
		private uint rasterCycle;
		private uint rasterIrqLine;
		private uint rasterLine;
		private uint rc;
		private ushort refreshAddr;
		private bool reset;
		private bool rowSelect;
		private uint spriteIndex;
		private uint spriteMultiColor0;
		private uint spriteMultiColor1;
		private Sprite[] sprites;
		private uint vc;
		private uint vcbase;
		private uint[] videoBuffer;
		private uint videoRam;
		private uint vmli;
		private uint xScroll;
		private uint yScroll;
		private uint ySmooth;

		public void SyncState(Serializer ser)
		{
			ser.Sync("backgroundColor0", ref backgroundColor0);
			ser.Sync("backgroundColor1", ref backgroundColor1);
			ser.Sync("backgroundColor2", ref backgroundColor2);
			ser.Sync("backgroundColor3", ref backgroundColor3);
			ser.Sync("badline", ref badline);
			ser.Sync("badlineEnabled", ref badlineEnabled);
			ser.Sync("bitmapMode", ref bitmapMode);
			ser.Sync("bitmapRam", ref bitmapRam);
			ser.Sync("borderBottom", ref borderBottom);
			ser.Sync("borderColor", ref borderColor);
			ser.Sync("borderLeft", ref borderLeft);
			ser.Sync("borderOnMain", ref borderOnMain);
			ser.Sync("borderOnVertical", ref borderOnVertical);
			ser.Sync("borderOnVerticalEnable", ref borderOnVerticalEnable);
			ser.Sync("borderRight", ref borderRight);
			ser.Sync("borderTop", ref borderTop);
			ser.Sync("columnSelect", ref columnSelect);
			ser.Sync("displayEnable", ref displayEnable);
			ser.Sync("enableIrqDataCollision", ref enableIrqDataCollision);
			ser.Sync("enableIrqLightPen", ref enableIrqLightPen);
			ser.Sync("enableIrqRaster", ref enableIrqRaster);
			ser.Sync("enableIrqSpriteCollision", ref enableIrqSpriteCollision);
			ser.Sync("extraColorMode", ref extraColorMode);
			ser.Sync("idle", ref idle);
			ser.Sync("irq", ref irq);
			ser.Sync("irqDataCollision", ref irqDataCollision);
			ser.Sync("irqLightPen", ref irqLightPen);
			ser.Sync("irqRaster", ref irqRaster);
			ser.Sync("irqSpriteCollision", ref irqSpriteCollision);
			ser.Sync("lightPenX", ref lightPenX);
			ser.Sync("lightPenY", ref lightPenY);
			ser.Sync("multiColorMode", ref multiColorMode);
			ser.Sync("phaseRead0", ref phaseRead0);
			ser.Sync("phaseRead1", ref phaseRead1);
			ser.Sync("rasterCycle", ref rasterCycle);
			ser.Sync("rasterIrqLine", ref rasterIrqLine);
			ser.Sync("rasterLine", ref rasterLine);
			ser.Sync("rc", ref rc);
			ser.Sync("refreshAddr", ref refreshAddr);
			ser.Sync("reset", ref reset);
			ser.Sync("rowSelect", ref rowSelect);
			ser.Sync("spriteIndex", ref spriteIndex);
			ser.Sync("spriteMultiColor0", ref spriteMultiColor0);
			ser.Sync("spriteMultiColor1", ref spriteMultiColor1);
			ser.Sync("vc", ref vc);
			ser.Sync("vcbase", ref vcbase);
			ser.Sync("videoBuffer", ref videoBuffer, false);
			ser.Sync("videoRam", ref videoRam);
			ser.Sync("vmli", ref vmli);
			ser.Sync("xScroll", ref xScroll);
			ser.Sync("yScroll", ref yScroll);
			ser.Sync("ySmooth", ref ySmooth);

			for (int i = 0; i < 8; i++)
			{
				ser.BeginSection("sprite" + i.ToString());
				sprites[i].SyncState(ser);
				ser.EndSection();
			}
		}

		private byte this[uint addr]
		{
			get
			{
				byte result;

				switch (addr & 0x3F)
				{
					case 0x00: result = Reg00; break;
					case 0x01: result = Reg01; break;
					case 0x02: result = Reg02; break;
					case 0x03: result = Reg03; break;
					case 0x04: result = Reg04; break;
					case 0x05: result = Reg05; break;
					case 0x06: result = Reg06; break;
					case 0x07: result = Reg07; break;
					case 0x08: result = Reg08; break;
					case 0x09: result = Reg09; break;
					case 0x0A: result = Reg0A; break;
					case 0x0B: result = Reg0B; break;
					case 0x0C: result = Reg0C; break;
					case 0x0D: result = Reg0D; break;
					case 0x0E: result = Reg0E; break;
					case 0x0F: result = Reg0F; break;
					case 0x10: result = Reg10; break;
					case 0x11: result = Reg11; break;
					case 0x12: result = Reg12; break;
					case 0x13: result = Reg13; break;
					case 0x14: result = Reg14; break;
					case 0x15: result = Reg15; break;
					case 0x16: result = Reg16; break;
					case 0x17: result = Reg17; break;
					case 0x18: result = Reg18; break;
					case 0x19: result = Reg19; break;
					case 0x1A: result = Reg1A; break;
					case 0x1B: result = Reg1B; break;
					case 0x1C: result = Reg1C; break;
					case 0x1D: result = Reg1D; break;
					case 0x1E: result = Reg1E; break;
					case 0x1F: result = Reg1F; break;
					case 0x20: result = Reg20; break;
					case 0x21: result = Reg21; break;
					case 0x22: result = Reg22; break;
					case 0x23: result = Reg23; break;
					case 0x24: result = Reg24; break;
					case 0x25: result = Reg25; break;
					case 0x26: result = Reg26; break;
					case 0x27: result = Reg27; break;
					case 0x28: result = Reg28; break;
					case 0x29: result = Reg29; break;
					case 0x2A: result = Reg2A; break;
					case 0x2B: result = Reg2B; break;
					case 0x2C: result = Reg2C; break;
					case 0x2D: result = Reg2D; break;
					case 0x2E: result = Reg2E; break;
					default: result = 0xFF; break;
				}
				return result;
			}
			set
			{
				switch (addr & 0x3F)
				{
					case 0x00: Reg00 = value; break;
					case 0x01: Reg01 = value; break;
					case 0x02: Reg02 = value; break;
					case 0x03: Reg03 = value; break;
					case 0x04: Reg04 = value; break;
					case 0x05: Reg05 = value; break;
					case 0x06: Reg06 = value; break;
					case 0x07: Reg07 = value; break;
					case 0x08: Reg08 = value; break;
					case 0x09: Reg09 = value; break;
					case 0x0A: Reg0A = value; break;
					case 0x0B: Reg0B = value; break;
					case 0x0C: Reg0C = value; break;
					case 0x0D: Reg0D = value; break;
					case 0x0E: Reg0E = value; break;
					case 0x0F: Reg0F = value; break;
					case 0x10: Reg10 = value; break;
					case 0x11: Reg11 = value; break;
					case 0x12: Reg12 = value; break;
					case 0x15: Reg15 = value; break;
					case 0x16: Reg16 = value; break;
					case 0x17: Reg17 = value; break;
					case 0x18: Reg18 = value; break;
					case 0x19: Reg19 = value; break;
					case 0x1A: Reg1A = value; break;
					case 0x1B: Reg1B = value; break;
					case 0x1C: Reg1C = value; break;
					case 0x1D: Reg1D = value; break;
					case 0x1E: Reg1E = value; break;
					case 0x1F: Reg1F = value; break;
					case 0x20: Reg20 = value; break;
					case 0x21: Reg21 = value; break;
					case 0x22: Reg22 = value; break;
					case 0x23: Reg23 = value; break;
					case 0x24: Reg24 = value; break;
					case 0x25: Reg25 = value; break;
					case 0x26: Reg26 = value; break;
					case 0x27: Reg27 = value; break;
					case 0x28: Reg28 = value; break;
					case 0x29: Reg29 = value; break;
					case 0x2A: Reg2A = value; break;
					case 0x2B: Reg2B = value; break;
					case 0x2C: Reg2C = value; break;
					case 0x2D: Reg2D = value; break;
					case 0x2E: Reg2E = value; break;
					default: break;
				}
			}
		}

		private byte Reg00 { 
			get { return (byte)(sprites[0].x & 0xFF); } 
			set { sprites[0].x &= 0x100; sprites[0].x |= value; } 
		}
		private byte Reg01 { 
			get { return (byte)(sprites[0].y); } 
			set { sprites[0].y = value; } 
		}
		private byte Reg02 { 
			get { return (byte)(sprites[1].x & 0xFF); } 
			set { sprites[1].x &= 0x100; sprites[1].x |= value; } 
		}
		private byte Reg03 { 
			get { return (byte)(sprites[1].y); } 
			set { sprites[1].y = value; } 
		}
		private byte Reg04 {
			get { return (byte)(sprites[2].x & 0xFF); } 
			set { sprites[2].x &= 0x100; sprites[2].x |= value; }
		}
		private byte Reg05 { 
			get { return (byte)(sprites[2].y); } 
			set { sprites[2].y = value; }
		}
		private byte Reg06 { 
			get { return (byte)(sprites[3].x & 0xFF); } 
			set { sprites[3].x &= 0x100; sprites[3].x |= value; } 
		}
		private byte Reg07 { 
			get { return (byte)(sprites[3].y); }
			set { sprites[3].y = value; } 
		}
		private byte Reg08 { 
			get { return (byte)(sprites[4].x & 0xFF); }
			set { sprites[4].x &= 0x100; sprites[4].x |= value; } 
		}
		private byte Reg09 { 
			get { return (byte)(sprites[4].y); } 
			set { sprites[4].y = value; } 
		}
		private byte Reg0A { 
			get { return (byte)(sprites[5].x & 0xFF); } 
			set { sprites[5].x &= 0x100; sprites[5].x |= value; }
		}
		private byte Reg0B { 
			get { return (byte)(sprites[5].y); } 
			set { sprites[5].y = value; } 
		}
		private byte Reg0C { 
			get { return (byte)(sprites[6].x & 0xFF); } 
			set { sprites[6].x &= 0x100; sprites[6].x |= value; } 
		}
		private byte Reg0D { 
			get { return (byte)(sprites[6].y); } 
			set { sprites[6].y = value; } 
		}
		private byte Reg0E { 
			get { return (byte)(sprites[7].x & 0xFF); }
			set { sprites[7].x &= 0x100; sprites[7].x |= value; }
		}
		private byte Reg0F { 
			get { return (byte)(sprites[7].y); } 
			set { sprites[7].y = value; } 
		}
		private byte Reg10 {
			get
			{
				return (byte)(
					((sprites[0].x & 0x100) >> 8) |
					((sprites[1].x & 0x100) >> 7) |
					((sprites[2].x & 0x100) >> 6) |
					((sprites[3].x & 0x100) >> 5) |
					((sprites[4].x & 0x100) >> 4) |
					((sprites[5].x & 0x100) >> 3) |
					((sprites[6].x & 0x100) >> 2) |
					((sprites[7].x & 0x100) >> 1));
			}
			set
			{
				uint val = value;
				sprites[0].x = (sprites[0].x & 0xFF) | ((val & 0x01) << 8);
				sprites[1].x = (sprites[1].x & 0xFF) | ((val & 0x02) << 7);
				sprites[2].x = (sprites[2].x & 0xFF) | ((val & 0x04) << 6);
				sprites[3].x = (sprites[3].x & 0xFF) | ((val & 0x08) << 5);
				sprites[4].x = (sprites[4].x & 0xFF) | ((val & 0x10) << 4);
				sprites[5].x = (sprites[5].x & 0xFF) | ((val & 0x20) << 3);
				sprites[6].x = (sprites[6].x & 0xFF) | ((val & 0x40) << 2);
				sprites[7].x = (sprites[7].x & 0xFF) | ((val & 0x80) << 1);
			}
		}
		private byte Reg11 {
			get
			{
				return (byte)(
					(yScroll & 0x07) |
					(columnSelect ? (uint)0x08 : (uint)0x00) |
					(displayEnable ? (uint)0x10 : (uint)0x00) |
					(bitmapMode ? (uint)0x20 : (uint)0x00) |
					(extraColorMode ? (uint)0x40 : (uint)0x00) |
					((rasterLine & 0x100) >> 1));
			}
			set
			{
				yScroll = (uint)value & 0x07;
				columnSelect = ((value & 0x08) != 0);
				displayEnable = ((value & 0x10) != 0);
				bitmapMode = ((value & 0x20) != 0);
				extraColorMode = ((value & 0x40) != 0);
				rasterIrqLine &= 0x100;
				rasterIrqLine |= (uint)(value & 0x80) << 1;
			}
		}
		private byte Reg12 {
			get { return (byte)(rasterLine & 0xFF); }
			set { rasterIrqLine &= 0x100; rasterIrqLine |= value; }
		}
		private byte Reg13 {
			get { return (byte)((lightPenX >> 1) & 0xFF); }
		}
		private byte Reg14 {
			get { return (byte)(lightPenY & 0xFF); }
		}
		private byte Reg15 {
			get
			{
				return (byte)(
					(sprites[0].enable ? 0x01 : 0x00) |
					(sprites[1].enable ? 0x02 : 0x00) |
					(sprites[2].enable ? 0x04 : 0x00) |
					(sprites[3].enable ? 0x08 : 0x00) |
					(sprites[4].enable ? 0x10 : 0x00) |
					(sprites[5].enable ? 0x20 : 0x00) |
					(sprites[6].enable ? 0x40 : 0x00) |
					(sprites[7].enable ? 0x80 : 0x00));
			}
			set 
			{
				sprites[0].enable = ((value & 0x01) != 0);
				sprites[1].enable = ((value & 0x02) != 0);
				sprites[2].enable = ((value & 0x04) != 0);
				sprites[3].enable = ((value & 0x08) != 0);
				sprites[4].enable = ((value & 0x10) != 0);
				sprites[5].enable = ((value & 0x20) != 0);
				sprites[6].enable = ((value & 0x40) != 0);
				sprites[7].enable = ((value & 0x80) != 0);
			}
		}
		private byte Reg16 {
			get
			{
				return (byte)(
					(xScroll & 0x07) |
					(columnSelect ? (uint)0x08 : (uint)0x00) |
					(multiColorMode ? (uint)0x10 : (uint)0x00) |
					(reset ? (uint)0x20 : (uint)0x00) |
					(0xC0));
			}
			set
			{
				xScroll = (uint)value & 0x07;
				columnSelect = ((value & 0x08) != 0);
				multiColorMode = ((value & 0x10) != 0);
				reset = ((value & 0x20) != 0);
			}
		}
		private byte Reg17 {
			get
			{
				return (byte)(
					(sprites[0].yExpand ? 0x01 : 0x00) |
					(sprites[1].yExpand ? 0x02 : 0x00) |
					(sprites[2].yExpand ? 0x04 : 0x00) |
					(sprites[3].yExpand ? 0x08 : 0x00) |
					(sprites[4].yExpand ? 0x10 : 0x00) |
					(sprites[5].yExpand ? 0x20 : 0x00) |
					(sprites[6].yExpand ? 0x40 : 0x00) |
					(sprites[7].yExpand ? 0x80 : 0x00));
			}
			set
			{
				sprites[0].yExpand = ((value & 0x01) != 0);
				sprites[1].yExpand = ((value & 0x02) != 0);
				sprites[2].yExpand = ((value & 0x04) != 0);
				sprites[3].yExpand = ((value & 0x08) != 0);
				sprites[4].yExpand = ((value & 0x10) != 0);
				sprites[5].yExpand = ((value & 0x20) != 0);
				sprites[6].yExpand = ((value & 0x40) != 0);
				sprites[7].yExpand = ((value & 0x80) != 0);
			}
		}
		private byte Reg18 {
			get { return (byte)((videoRam << 4) | (bitmapRam << 1)); }
			set { videoRam = ((uint)(value & 0xF0)) >> 4; bitmapRam = ((uint)(value & 0x0E)) >> 1; }
		}
		private byte Reg19 {
			get
			{
				return (byte)(
					(irqRaster ? (uint)0x01 : (uint)0x00) |
					(irqDataCollision ? (uint)0x02 : (uint)0x00) |
					(irqSpriteCollision ? (uint)0x04 : (uint)0x00) |
					(irqLightPen ? 0x08 : (uint)0x00) |
					(irq ? (uint)0x80 : (uint)0x00) |
					(0x70));
			}
			set
			{
				irqRaster = ((value & 0x01) != 0);
				irqDataCollision = ((value & 0x02) != 0);
				irqSpriteCollision = ((value & 0x04) != 0);
				irqLightPen = ((value & 0x08) != 0);
			}
		}
		private byte Reg1A {
			get
			{
				return (byte)(
					(enableIrqRaster ? (uint)0x01 : (uint)0x00) |
					(enableIrqDataCollision ? (uint)0x02 : (uint)0x00) |
					(enableIrqSpriteCollision ? (uint)0x04 : (uint)0x00) |
					(enableIrqLightPen ? 0x08 : (uint)0x00) |
					(0xF0));
			}
			set
			{
				enableIrqRaster = ((value & 0x01) != 0);
				enableIrqDataCollision = ((value & 0x02) != 0);
				enableIrqSpriteCollision = ((value & 0x04) != 0);
				enableIrqLightPen = ((value & 0x08) != 0);
			}
		}
		private byte Reg1B {
			get
			{
				return (byte)(
					(sprites[0].priority ? 0x01 : 0x00) |
					(sprites[1].priority ? 0x02 : 0x00) |
					(sprites[2].priority ? 0x04 : 0x00) |
					(sprites[3].priority ? 0x08 : 0x00) |
					(sprites[4].priority ? 0x10 : 0x00) |
					(sprites[5].priority ? 0x20 : 0x00) |
					(sprites[6].priority ? 0x40 : 0x00) |
					(sprites[7].priority ? 0x80 : 0x00));
			}
			set
			{
				sprites[0].priority = ((value & 0x01) != 0);
				sprites[1].priority = ((value & 0x02) != 0);
				sprites[2].priority = ((value & 0x04) != 0);
				sprites[3].priority = ((value & 0x08) != 0);
				sprites[4].priority = ((value & 0x10) != 0);
				sprites[5].priority = ((value & 0x20) != 0);
				sprites[6].priority = ((value & 0x40) != 0);
				sprites[7].priority = ((value & 0x80) != 0);
			}
		}
		private byte Reg1C {
			get
			{
				return (byte)(
					(sprites[0].multicolor ? 0x01 : 0x00) |
					(sprites[1].multicolor ? 0x02 : 0x00) |
					(sprites[2].multicolor ? 0x04 : 0x00) |
					(sprites[3].multicolor ? 0x08 : 0x00) |
					(sprites[4].multicolor ? 0x10 : 0x00) |
					(sprites[5].multicolor ? 0x20 : 0x00) |
					(sprites[6].multicolor ? 0x40 : 0x00) |
					(sprites[7].multicolor ? 0x80 : 0x00));
			}
			set
			{
				sprites[0].multicolor = ((value & 0x01) != 0);
				sprites[1].multicolor = ((value & 0x02) != 0);
				sprites[2].multicolor = ((value & 0x04) != 0);
				sprites[3].multicolor = ((value & 0x08) != 0);
				sprites[4].multicolor = ((value & 0x10) != 0);
				sprites[5].multicolor = ((value & 0x20) != 0);
				sprites[6].multicolor = ((value & 0x40) != 0);
				sprites[7].multicolor = ((value & 0x80) != 0);
			}
		}
		private byte Reg1D {
			get
			{
				return (byte)(
					(sprites[0].xExpand ? 0x01 : 0x00) |
					(sprites[1].xExpand ? 0x02 : 0x00) |
					(sprites[2].xExpand ? 0x04 : 0x00) |
					(sprites[3].xExpand ? 0x08 : 0x00) |
					(sprites[4].xExpand ? 0x10 : 0x00) |
					(sprites[5].xExpand ? 0x20 : 0x00) |
					(sprites[6].xExpand ? 0x40 : 0x00) |
					(sprites[7].xExpand ? 0x80 : 0x00));
			}
			set
			{
				sprites[0].xExpand = ((value & 0x01) != 0);
				sprites[1].xExpand = ((value & 0x02) != 0);
				sprites[2].xExpand = ((value & 0x04) != 0);
				sprites[3].xExpand = ((value & 0x08) != 0);
				sprites[4].xExpand = ((value & 0x10) != 0);
				sprites[5].xExpand = ((value & 0x20) != 0);
				sprites[6].xExpand = ((value & 0x40) != 0);
				sprites[7].xExpand = ((value & 0x80) != 0);
			}
		}
		private byte Reg1E {
			get
			{
				return (byte)(
					(sprites[0].spriteCollision ? 0x01 : 0x00) |
					(sprites[1].spriteCollision ? 0x02 : 0x00) |
					(sprites[2].spriteCollision ? 0x04 : 0x00) |
					(sprites[3].spriteCollision ? 0x08 : 0x00) |
					(sprites[4].spriteCollision ? 0x10 : 0x00) |
					(sprites[5].spriteCollision ? 0x20 : 0x00) |
					(sprites[6].spriteCollision ? 0x40 : 0x00) |
					(sprites[7].spriteCollision ? 0x80 : 0x00));
			}
			set
			{
				sprites[0].spriteCollision = ((value & 0x01) != 0);
				sprites[1].spriteCollision = ((value & 0x02) != 0);
				sprites[2].spriteCollision = ((value & 0x04) != 0);
				sprites[3].spriteCollision = ((value & 0x08) != 0);
				sprites[4].spriteCollision = ((value & 0x10) != 0);
				sprites[5].spriteCollision = ((value & 0x20) != 0);
				sprites[6].spriteCollision = ((value & 0x40) != 0);
				sprites[7].spriteCollision = ((value & 0x80) != 0);
			}
		}
		private byte Reg1F {
			get
			{
				return (byte)(
					(sprites[0].dataCollision ? 0x01 : 0x00) |
					(sprites[1].dataCollision ? 0x02 : 0x00) |
					(sprites[2].dataCollision ? 0x04 : 0x00) |
					(sprites[3].dataCollision ? 0x08 : 0x00) |
					(sprites[4].dataCollision ? 0x10 : 0x00) |
					(sprites[5].dataCollision ? 0x20 : 0x00) |
					(sprites[6].dataCollision ? 0x40 : 0x00) |
					(sprites[7].dataCollision ? 0x80 : 0x00));
			}
			set
			{
				sprites[0].dataCollision = ((value & 0x01) != 0);
				sprites[1].dataCollision = ((value & 0x02) != 0);
				sprites[2].dataCollision = ((value & 0x04) != 0);
				sprites[3].dataCollision = ((value & 0x08) != 0);
				sprites[4].dataCollision = ((value & 0x10) != 0);
				sprites[5].dataCollision = ((value & 0x20) != 0);
				sprites[6].dataCollision = ((value & 0x40) != 0);
				sprites[7].dataCollision = ((value & 0x80) != 0);
			}
		}
		private byte Reg20 {
			get { return (byte)borderColor; }
			set { borderColor = value; }
		}
		private byte Reg21 {
			get { return (byte)backgroundColor0; }
			set { backgroundColor0 = value; }
		}
		private byte Reg22 {
			get { return (byte)backgroundColor1; }
			set { backgroundColor1 = value; }
		}
		private byte Reg23 {
			get { return (byte)backgroundColor2; }
			set { backgroundColor2 = value; }
		}
		private byte Reg24 {
			get { return (byte)backgroundColor3; }
			set { backgroundColor3 = value; }
		}
		private byte Reg25 {
			get { return (byte)spriteMultiColor0; }
			set { spriteMultiColor0 = value; }
		}
		private byte Reg26 {
			get { return (byte)spriteMultiColor1; }
			set { spriteMultiColor1 = value; }
		}
		private byte Reg27 {
			get { return (byte)sprites[0].color; }
			set { sprites[0].color = value; }
		}
		private byte Reg28 {
			get { return (byte)sprites[1].color; }
			set { sprites[1].color = value; }
		}
		private byte Reg29 {
			get { return (byte)sprites[2].color; }
			set { sprites[2].color = value; }
		}
		private byte Reg2A {
			get { return (byte)sprites[3].color; }
			set { sprites[3].color = value; }
		}
		private byte Reg2B {
			get { return (byte)sprites[4].color; }
			set { sprites[4].color = value; }
		}
		private byte Reg2C {
			get { return (byte)sprites[5].color; }
			set { sprites[5].color = value; }
		}
		private byte Reg2D {
			get { return (byte)sprites[6].color; }
			set { sprites[6].color = value; }
		}
		private byte Reg2E {
			get { return (byte)sprites[7].color; }
			set { sprites[7].color = value; }
		}

	}

}
