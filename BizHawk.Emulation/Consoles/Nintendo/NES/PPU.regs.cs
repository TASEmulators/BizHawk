//blargg: Reading from $2007 when the VRAM address is $3fxx will fill the internal read buffer with the contents at VRAM address $3fxx, in addition to reading the palette RAM. 

				//static const byte powerUpPalette[] =
				//{
				//    0x3F,0x01,0x00,0x01, 0x00,0x02,0x02,0x0D, 0x08,0x10,0x08,0x24, 0x00,0x00,0x04,0x2C,
				//    0x09,0x01,0x34,0x03, 0x00,0x04,0x00,0x14, 0x08,0x3A,0x00,0x02, 0x00,0x20,0x2C,0x08
				//};

using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using BizHawk.Emulation.CPUs.M6502;


namespace BizHawk.Emulation.Consoles.Nintendo
{
	partial class NES
	{
		partial class PPU
		{
			class Reg_2001
			{
				public Bit color_disable; //Color disable (0: normal color; 1: AND all palette entries with 110000, effectively producing a monochrome display)
				public Bit show_bg_leftmost; //Show leftmost 8 pixels of background
				public Bit show_obj_leftmost; //Show sprites in leftmost 8 pixels
				public Bit show_bg; //Show background
				public Bit show_obj; //Show sprites
				public Bit intense_green; //Intensify greens (and darken other colors)
				public Bit intense_blue; //Intensify blues (and darken other colors)
				public Bit intense_red; //Intensify reds (and darken other colors)

				public bool PPUON { get { return show_bg || show_obj; } }

				public byte Value
				{
					get
					{
						return (byte)(color_disable | (show_bg_leftmost << 1) | (show_obj_leftmost << 2) | (show_bg << 3) | (show_obj << 4) | (intense_green << 5) | (intense_blue << 6) | (intense_red << 7));
					}
					set
					{
						color_disable = (value & 1);
						show_bg_leftmost = (value >> 1) & 1;
						show_obj_leftmost = (value >> 2) & 1;
						show_bg = (value >> 3) & 1;
						show_obj = (value >> 4) & 1;
						intense_green = (value >> 5) & 1;
						intense_blue = (value >> 6) & 1;
						intense_red = (value >> 7) & 1;
					}
				}
			}


			public struct PPUSTATUS
			{
				public int sl;
				public int cycle, end_cycle;
			}

			//uses the internal counters concept at http://nesdev.icequake.net/PPU%20addressing.txt
			//TODO - this should be turned into a state machine
			public class PPUREGS
			{
				PPU ppu;
				public PPUREGS(PPU ppu)
				{
					this.ppu = ppu;
					reset();
				}

				public void SaveStateBinary(BinaryWriter bw)
				{
					bw.Write(fv);
					bw.Write(v);
					bw.Write(h);
					bw.Write(vt);
					bw.Write(ht);
					bw.Write(_fv);
					bw.Write(_v);
					bw.Write(_h);
					bw.Write(_vt);
					bw.Write(_ht);
					bw.Write(fh);
					bw.Write(status.cycle);
					bw.Write(status.end_cycle);
					bw.Write(status.sl);
				}

				public void LoadStateBinary(BinaryReader br)
				{
					fv = br.ReadInt32();
					v = br.ReadInt32();
					h = br.ReadInt32();
					vt = br.ReadInt32();
					ht = br.ReadInt32();
					_fv = br.ReadInt32();
					_v = br.ReadInt32();
					_h = br.ReadInt32();
					_vt = br.ReadInt32();
					_ht = br.ReadInt32();
					fh = br.ReadInt32();
					status.cycle = br.ReadInt32();
					status.end_cycle = br.ReadInt32();
					status.sl = br.ReadInt32();
				}

				//normal clocked regs. as the game can interfere with these at any time, they need to be savestated
				public int fv;//3
				public int v;//1
				public int h;//1
				public int vt;//5
				public int ht;//5

				//temp unlatched regs (need savestating, can be written to at any time)
				public int _fv, _vt, _v, _h, _ht;

				//other regs that need savestating
				public int fh;//3 (horz scroll)

				//other regs that don't need saving
				public int par;//8 (sort of a hack, just stored in here, but not managed by this system)

				//cached state data. these are always reset at the beginning of a frame and don't need saving
				//but just to be safe, we're gonna save it
				public PPUSTATUS status = new PPUSTATUS();

				public void reset()
				{
					fv = v = h = vt = ht = 0;
					fh = par = 0;
					_fv = _v = _h = _vt = _ht = 0;
					status.cycle = 0;
					status.end_cycle = 341;
					status.sl = 241;
				}

				public void install_latches()
				{
					fv = _fv;
					v = _v;
					h = _h;
					vt = _vt;
					ht = _ht;
				}

				public void install_h_latches()
				{
					ht = _ht;
					h = _h;
				}

				public void clear_latches()
				{
					_fv = _v = _h = _vt = _ht = 0;
					fh = 0;
				}

				public void increment_hsc()
				{
					//The first one, the horizontal scroll counter, consists of 6 bits, and is
					//made up by daisy-chaining the HT counter to the H counter. The HT counter is
					//then clocked every 8 pixel dot clocks (or every 8/3 CPU clock cycles).
					ht++;
					h += (ht >> 5);
					ht &= 31;
					h &= 1;
				}

				public void increment_vs()
				{
					fv++;
					vt += (fv >> 3);
					vt &= 31; //fixed tecmo super bowl
					v += (vt == 30) ? 1 : 0;
					fv &= 7;
					if (vt == 30) vt = 0;
					v &= 1;
				}

				public int get_ntread()
				{
					return 0x2000 | (v << 0xB) | (h << 0xA) | (vt << 5) | ht;
				}

				public int get_2007access()
				{
					return ((fv & 3) << 0xC) | (v << 0xB) | (h << 0xA) | (vt << 5) | ht;
				}

				//The PPU has an internal 4-position, 2-bit shifter, which it uses for
				//obtaining the 2-bit palette select data during an attribute table byte
				//fetch. To represent how this data is shifted in the diagram, letters a..c
				//are used in the diagram to represent the right-shift position amount to
				//apply to the data read from the attribute data (a is always 0). This is why
				//you only see bits 0 and 1 used off the read attribute data in the diagram.
				public int get_atread()
				{
					return 0x2000 | (v << 0xB) | (h << 0xA) | 0x3C0 | ((vt & 0x1C) << 1) | ((ht & 0x1C) >> 2);
				}

				//address line 3 relates to the pattern table fetch occuring (the PPU always makes them in pairs).
				public int get_ptread()
				{
					int s = ppu.reg_2000.bg_pattern_hi;
					return (s << 0xC) | (par << 0x4) | fv;
				}

				public void increment2007(bool by32)
				{

					//If the VRAM address increment bit (2000.2) is clear (inc. amt. = 1), all the
					//scroll counters are daisy-chained (in the order of HT, VT, H, V, FV) so that
					//the carry out of each counter controls the next counter's clock rate. The
					//result is that all 5 counters function as a single 15-bit one. Any access to
					//2007 clocks the HT counter here.
					//
					//If the VRAM address increment bit is set (inc. amt. = 32), the only
					//difference is that the HT counter is no longer being clocked, and the VT
					//counter is now being clocked by access to 2007.
					if (by32)
					{
						vt++;
					}
					else
					{
						ht++;
						vt += (ht >> 5) & 1;
					}
					h += (vt >> 5);
					v += (h >> 1);
					fv += (v >> 1);
					ht &= 31;
					vt &= 31;
					h &= 1;
					v &= 1;
					fv &= 7;
				}
			};

			class Reg_2000
			{
				PPU ppu;
				public Reg_2000(PPU ppu)
				{
					this.ppu = ppu;
				}
				//these bits go straight into PPUR
				//(00 = $2000; 01 = $2400; 02 = $2800; 03 = $2c00)

				public Bit vram_incr32; //(0: increment by 1, going across; 1: increment by 32, going down)
				public Bit obj_pattern_hi; //Sprite pattern table address for 8x8 sprites (0: $0000; 1: $1000)
				public Bit bg_pattern_hi; //Background pattern table address (0: $0000; 1: $1000)
				public Bit obj_size_16; //Sprite size (0: 8x8 sprites; 1: 8x16 sprites)
				public Bit ppu_layer; //PPU layer select (should always be 0 in the NES; some Nintendo arcade boards presumably had two PPUs)
				public Bit vblank_nmi_gen; //Vertical blank NMI generation (0: off; 1: on)


				public byte Value
				{
					get
					{
						return (byte)(ppu.ppur._h | (ppu.ppur._v << 1) | (vram_incr32 << 2) | (obj_pattern_hi << 3) | (bg_pattern_hi << 4) | (obj_size_16 << 5) | (ppu_layer << 6) | (vblank_nmi_gen << 7));
					}
					set
					{
						ppu.ppur._h = value & 1;
						ppu.ppur._v = (value >> 1) & 1;
						vram_incr32 = (value >> 2) & 1;
						obj_pattern_hi = (value >> 3) & 1;
						bg_pattern_hi = (value >> 4) & 1;
						obj_size_16 = (value >> 5) & 1;
						ppu_layer = (value >> 6) & 1;
						vblank_nmi_gen = (value >> 7) & 1;
					}
				}
			}


			Bit Reg2002_objoverflow;  //Sprite overflow. The PPU can handle only eight sprites on one scanline and sets this bit if it starts drawing sprites.
			Bit Reg2002_objhit; //Sprite 0 overlap.  Set when a nonzero pixel of sprite 0 is drawn overlapping a nonzero background pixel.  Used for raster timing.
			Bit Reg2002_vblank_active;  //Vertical blank start (0: has not started; 1: has started)
			byte PPUGenLatch;
			public PPUREGS ppur;
			Reg_2000 reg_2000;
			Reg_2001 reg_2001;
			byte reg_2003;
			byte[] OAM;
			public byte[] PALRAM;
			bool vtoggle;
			byte VRAMBuffer;
			void regs_reset()
			{
				//TODO - would like to reconstitute the entire PPU instead of all this..
				reg_2000 = new Reg_2000(this);
				reg_2001 = new Reg_2001();
				ppur = new PPUREGS(this);
				Reg2002_objoverflow = false;
				Reg2002_objhit = false;
				Reg2002_vblank_active = false;
				PPUGenLatch = 0;
				reg_2003 = 0;
				OAM = new byte[0x100];
				PALRAM = new byte[0x20];
				vtoggle = false;
				VRAMBuffer = 0;
			}
			//---------------------

			//PPU CONTROL (write)
			void write_2000(byte value)
			{
				if (!reg_2000.vblank_nmi_gen & ((value & 0x80) != 0) && (Reg2002_vblank_active))
				{
					//if we just unleashed the vblank interrupt then activate it now
					//FCEUX would use a "trigger NMI2" here in order to result in some delay effect
					TriggerNMI();
				}
				reg_2000.Value = value;
			}
			byte read_2000() { return PPUGenLatch; }

			//PPU MASK (write)
			void write_2001(byte value)
			{
				//printf("%04x:$%02x, %d\n",A,V,scanline);
				reg_2001.Value = value;
			}
			byte read_2001() { return PPUGenLatch; }

			//PPU STATUS (read)
			void write_2002(byte value) { }
			byte read_2002()
			{
				//once we thought we clear latches here, but that caused midframe glitches.
				//i think we should only reset the state machine for 2005/2006
				//ppur.clear_latches();

				vtoggle = false;
				int ret = (Reg2002_vblank_active << 7) | (Reg2002_objhit << 6) | (Reg2002_objoverflow << 5) | (PPUGenLatch & 0x1F);

				Reg2002_vblank_active = 0;

				return (byte)ret;
			}
			void clear_2002()
			{
				Reg2002_vblank_active = Reg2002_objhit = Reg2002_objoverflow = 0;
			}

			//OAM ADDRESS (write)
			void write_2003(byte value)
			{
				//just record the oam buffer write target
				reg_2003 = value;
			}
			byte read_2003() { return PPUGenLatch; }

			//OAM DATA (write)
			void write_2004(byte value)
			{
				if ((reg_2003 & 3) == 2) value &= 0xE3; //some of the OAM bits are unwired so we mask them out here
				//otherwise we just write this value and move on to the next oam byte
				OAM[reg_2003] = value;
				reg_2003++;
			}
			byte read_2004() { return 0xFF; /* TODO !!!!!! THIS IS UGLY. WE SHOULD PASTE IT IN OR REWRITE IT BUT WE NEED TO ASK QEED FOR TEST CASES*/ }

			//SCROLL (write)
			void write_2005(byte value)
			{
				if (!vtoggle)
				{
					ppur._ht= value >> 3;
					ppur.fh = value & 7;
				}
				else
				{
					ppur._vt = value >> 3;
					ppur._fv = value & 7;
				}
				vtoggle ^= true;
			}
			byte read_2005() { return PPUGenLatch; }

			//VRAM address register (write)
			void write_2006(byte value)
			{
				if (!vtoggle)
				{
					ppur._vt &= 0x07;
					ppur._vt |= (value & 0x3) << 3;
					ppur._h = (value >> 2) & 1;
					ppur._v = (value >> 3) & 1;
					ppur._fv = (value >> 4) & 3;
				}
				else
				{
					ppur._vt &= 0x18;
					ppur._vt |= (value >> 5);
					ppur._ht = value & 31;
					ppur.install_latches();
				}
				vtoggle ^= true;
			}
			byte read_2006() { return PPUGenLatch; }

			//VRAM data register (r/w)
			void write_2007(byte value)
			{
				//does this take 4x longer? nestopia indicates so perhaps...

				int addr = ppur.get_2007access() & 0x3FFF;
				if ((addr & 0x3F00) == 0x3F00)
				{
					//handle palette. this is being done nestopia style, because i found some documentation for it (appendix 1)
					addr &= 0x1F;
					byte color = (byte)(value & 0x3F); //are these bits really unwired? can they be read back somehow?

					PALRAM[addr] = color;
					if ((addr & 3) == 0)
					{
						PALRAM[addr ^ 0x10] = color;
					}
				}
				else
				{
					ppubus_write(addr, value);
				}

				ppur.increment2007(reg_2000.vram_incr32 != 0);
			}
			byte read_2007()
			{
				int addr = ppur.get_2007access() & 0x3FFF;
				
				//ordinarily we return the buffered values
				byte ret = VRAMBuffer;

				//in any case, we read from the ppu bus
				VRAMBuffer = ppubus_read(addr);

				//but reads from the palette are implemented in the PPU and return immediately
				if ((addr & 0x3F00) == 0x3F00)
				{
					//TODO apply greyscale shit?
					ret = PALRAM[addr & 0x1F];
				}

				ppur.increment2007(reg_2000.vram_incr32 != 0);

				return ret;
			}
			//--------
		
			public byte ReadReg(int addr)
			{
				switch (addr)
				{
					case 0: return read_2000(); case 1: return read_2001(); case 2: return read_2002(); case 3: return read_2003();
					case 4: return read_2004(); case 5: return read_2005(); case 6: return read_2006(); case 7: return read_2007();
					default: throw new InvalidOperationException();
				}
			}
			public void WriteReg(int addr, byte value)
			{
				PPUGenLatch = value;
				switch (addr)
				{
					case 0: write_2000(value); break; case 1: write_2001(value); break; case 2: write_2002(value); break; case 3: write_2003(value); break;
					case 4: write_2004(value); break; case 5: write_2005(value); break; case 6: write_2006(value); break; case 7: write_2007(value); break;
					default: throw new InvalidOperationException();
				}
			}		
		}
	}
}


		//ARead[x]=A200x;
		//BWrite[x]=B2000;
		//ARead[x+1]=A200x;
		//BWrite[x+1]=B2001;
		//ARead[x+2]=A2002;
		//BWrite[x+2]=B2002;
		//ARead[x+3]=A200x;
		//BWrite[x+3]=B2003;
		//ARead[x+4]=A2004; //A2004;
		//BWrite[x+4]=B2004;
		//ARead[x+5]=A200x;
		//BWrite[x+5]=B2005;
		//ARead[x+6]=A200x;
		//BWrite[x+6]=B2006;
		//ARead[x+7]=A2007;
		//BWrite[x+7]=B2007;


//Address 	Size 	Description
//$0000 	$1000 	Pattern Table 0
//$1000 	$1000 	Pattern Table 1
//$2000 	$3C0 	Name Table 0
//$23C0 	$40 	Attribute Table 0
//$2400 	$3C0 	Name Table 1
//$27C0 	$40 	Attribute Table 1
//$2800 	$3C0 	Name Table 2
//$2BC0 	$40 	Attribute Table 2
//$2C00 	$3C0 	Name Table 3
//$2FC0 	$40 	Attribute Table 3
//$3000 	$F00 	Mirror of 2000h-2EFFh
//$3F00 	$10 	BG Palette
//$3F10 	$10 	Sprite Palette
//$3F20 	$E0 	Mirror of 3F00h-3F1Fh


//appendix 1
//http://nocash.emubase.de/everynes.htm#ppupalettes
//Palette Memory (25 entries used)
//  3F00h        Background Color (Color 0)
//  3F01h-3F03h  Background Palette 0 (Color 1-3)
//  3F05h-3F07h  Background Palette 1 (Color 1-3)
//  3F09h-3F0Bh  Background Palette 2 (Color 1-3)
//  3F0Dh-3F0Fh  Background Palette 3 (Color 1-3)
//  3F11h-3F13h  Sprite Palette 0 (Color 1-3)
//  3F15h-3F17h  Sprite Palette 1 (Color 1-3)
//  3F19h-3F1Bh  Sprite Palette 2 (Color 1-3)
//  3F1Dh-3F1Fh  Sprite Palette 3 (Color 1-3)
//Palette Gaps and Mirrors
//  3F04h,3F08h,3F0Ch - Three general purpose 6bit data registers.
//  3F10h,3F14h,3F18h,3F1Ch - Mirrors of 3F00h,3F04h,3F08h,3F0Ch.
//  3F20h-3FFFh - Mirrors of 3F00h-3F1Fh.