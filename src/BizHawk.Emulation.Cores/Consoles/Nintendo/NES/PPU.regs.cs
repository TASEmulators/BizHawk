//blargg: Reading from $2007 when the VRAM address is $3fxx will fill the internal read buffer with the contents at VRAM address $3fxx, in addition to reading the palette RAM. 

//static const byte powerUpPalette[] =
//{
//    0x3F,0x01,0x00,0x01, 0x00,0x02,0x02,0x0D, 0x08,0x10,0x08,0x24, 0x00,0x00,0x04,0x2C,
//    0x09,0x01,0x34,0x03, 0x00,0x04,0x00,0x14, 0x08,0x3A,0x00,0x02, 0x00,0x20,0x2C,0x08
//};

using System.Runtime.CompilerServices;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class PPU
	{
		public sealed class Reg_2001
		{
			public Bit color_disable; //Color disable (0: normal color; 1: AND all palette entries with 110000, effectively producing a monochrome display)
			public Bit show_bg_leftmost; //Show leftmost 8 pixels of background
			public Bit show_obj_leftmost; //Show sprites in leftmost 8 pixels
			public Bit show_bg; //Show background
			public Bit show_obj; //Show sprites
			public Bit intense_green; //Intensify greens (and darken other colors)
			public Bit intense_blue; //Intensify blues (and darken other colors)
			public Bit intense_red; //Intensify reds (and darken other colors)

			public int intensity_lsl_6; //an optimization..

			public byte Value
			{
				get => (byte)(color_disable | (show_bg_leftmost << 1) | (show_obj_leftmost << 2) | (show_bg << 3) | (show_obj << 4) | (intense_green << 5) | (intense_blue << 6) | (intense_red << 7));
				set
				{
					color_disable = (value & 1);
					show_bg_leftmost = (value >> 1) & 1;
					show_obj_leftmost = (value >> 2) & 1;
					show_bg = (value >> 3) & 1;
					show_obj = (value >> 4) & 1;
					intense_blue = (value >> 6) & 1;
					intense_red = (value >> 7) & 1;
					intense_green = (value >> 5) & 1;
					intensity_lsl_6 =  ((value >> 5) & 7)<<6;
				}
			}
		}

		public bool PPUON => show_bg_new || show_obj_new;


		// this byte is used to simulate open bus reads and writes
		// it should be modified by every read and write to a ppu register
		public byte ppu_open_bus=0;
		public long double_2007_read; // emulates a hardware bug of back to back 2007 reads
		public int[] ppu_open_bus_decay_timer = new int[8];
		public byte[] glitchy_reads_2003 = new byte[8];

		public struct PPUSTATUS
		{
			public int sl;
			public bool rendering => sl >= 0 && sl < 241;
			public int cycle;
		}

		//uses the internal counters concept at http://nesdev.icequake.net/PPU%20addressing.txt
		//TODO - this should be turned into a state machine
		public sealed class PPUREGS
		{
			public PPUREGS()
			{
				reset();
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync(nameof(fv), ref fv);
				ser.Sync(nameof(v), ref v);
				ser.Sync(nameof(h), ref h);
				ser.Sync(nameof(vt), ref vt);
				ser.Sync(nameof(ht), ref ht);
				ser.Sync(nameof(_fv), ref _fv);
				ser.Sync(nameof(_v), ref _v);
				ser.Sync(nameof(_h), ref _h);
				ser.Sync(nameof(_vt), ref _vt);
				ser.Sync(nameof(_ht), ref _ht);
				ser.Sync(nameof(fh), ref fh);
				ser.Sync($"{nameof(status)}.{nameof(status.cycle)}", ref status.cycle);
				ser.Sync($"{nameof(status)}.{nameof(status.sl)}", ref status.sl);
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

			//cached state data. these are always reset at the beginning of a frame and don't need saving
			//but just to be safe, we're gonna save it
			public PPUSTATUS status = new PPUSTATUS();

			//public int ComputeIndex()
			//{
			//    return fv | (v << 3) | (h << 4) | (vt << 5) | (ht << 10) | (fh << 15);
			//}
			//public void DecodeIndex(int index)
			//{
			//    fv = index & 7;
			//    v = (index >> 3) & 1;
			//    h = (index >> 4) & 1;
			//    vt = (index >> 5) & 0x1F;
			//    ht = (index >> 10) & 0x1F;
			//    fh = (index >> 15) & 7;
			//}

			//const int tbl_size = 1 << 18;
			//int[] tbl_increment_hsc = new int[tbl_size];
			//int[] tbl_increment_vs = new int[tbl_size];
			//public void BuildTables()
			//{
			//    for (int i = 0; i < tbl_size; i++)
			//    {
			//        DecodeIndex(i);
			//        increment_hsc();
			//        tbl_increment_hsc[i] = ComputeIndex();
			//        DecodeIndex(i);
			//        increment_vs();
			//        tbl_increment_vs[i] = ComputeIndex();
			//    }
			//}

			public void reset()
			{
				fv = v = h = vt = ht = 0;
				fh = 0;
				_fv = _v = _h = _vt = _ht = 0;
				status.cycle = 0;
				status.sl = 0;
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
				int fv_overflow = (fv >> 3);
				vt += fv_overflow;
				vt &= 31; //fixed tecmo super bowl
				if (vt == 30 && fv_overflow==1) //caution here (only do it at the exact instant of overflow) fixes p'radikus conflict
				{
					v++;
					vt = 0;
				}
				fv &= 7;
				v &= 1;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int get_ntread()
			{
				return 0x2000 | (v << 0xB) | (h << 0xA) | (vt << 5) | ht;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int get_atread()
			{
				return 0x2000 | (v << 0xB) | (h << 0xA) | 0x3C0 | ((vt & 0x1C) << 1) | ((ht & 0x1C) >> 2);
			}

			public void increment2007(bool rendering, bool by32)
			{
				if (rendering)
				{
					//don't do this:
					//if (by32) increment_vs();
					//else increment_hsc();
					//do this instead:
					increment_vs(); //yes, even if we're moving by 32
					return;
				}

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
		}

		public sealed class Reg_2000
		{
			private readonly PPUREGS _regs;
			public Reg_2000(PPUREGS regs)
			{
				_regs = regs;
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
				get => (byte)(_regs._h | (_regs._v << 1) | (vram_incr32 << 2) | (obj_pattern_hi << 3) | (bg_pattern_hi << 4) | (obj_size_16 << 5) | (ppu_layer << 6) | (vblank_nmi_gen << 7));
				set
				{
					_regs._h = value & 1;
					_regs._v = (value >> 1) & 1;
					vram_incr32 = (value >> 2) & 1;
					obj_pattern_hi = (value >> 3) & 1;
					bg_pattern_hi = (value >> 4) & 1;
					obj_size_16 = (value >> 5) & 1;
					ppu_layer = (value >> 6) & 1;
					vblank_nmi_gen = (value >> 7) & 1;
				}
			}
		}


		private Bit Reg2002_objoverflow;  //Sprite overflow. The PPU can handle only eight sprites on one scanline and sets this bit if it starts drawing sprites.
		private Bit Reg2002_objhit; //Sprite 0 overlap.  Set when a nonzero pixel of sprite 0 is drawn overlapping a nonzero background pixel.  Used for raster timing.
		public Bit Reg2002_vblank_active;  //Vertical blank start (0: has not started; 1: has started)
		public bool Reg2002_vblank_active_pending; //set if Reg2002_vblank_active is pending
		private bool Reg2002_vblank_clear_pending; //ppu's clear of vblank flag is pending
		public PPUREGS ppur;
		public Reg_2000 reg_2000;
		public Reg_2001 reg_2001;
		public byte reg_2003;
		public byte reg_2006_2;

		private void regs_reset()
		{
			//TODO - would like to reconstitute the entire PPU instead of all this..
			ppur = new PPUREGS();
			reg_2000 = new Reg_2000(ppur);
			reg_2001 = new Reg_2001();
			Reg2002_objoverflow = false;
			Reg2002_objhit = false;
			Reg2002_vblank_active = false;
			PPUGenLatch = 0;
			reg_2003 = 0;
			vtoggle = false;
			VRAMBuffer = 0;
		}

		//PPU CONTROL (write)
		private void write_2000(byte value)
		{
			if (!reg_2000.vblank_nmi_gen & ((value & 0x80) != 0) && (Reg2002_vblank_active) && !Reg2002_vblank_clear_pending)
			{
				//if we just unleashed the vblank interrupt then activate it now
				//if (ppudead != 1)
					NMI_PendingInstructions = 2;
			}
			//if (ppudead != 1)
				reg_2000.Value = value;
		}

		private byte read_2000() { return ppu_open_bus; }
		private byte peek_2000() { return ppu_open_bus; }

		//PPU MASK (write)
		private void write_2001(byte value)
		{
			//printf("%04x:$%02x, %d\n",A,V,scanline);
			reg_2001.Value = value;
			install_2001 = 2;
		}

		private byte read_2001() {return ppu_open_bus; }
		private byte peek_2001() {return ppu_open_bus; }

		//PPU STATUS (read)
		private void write_2002(byte value) { }

		private byte read_2002()
		{
			byte ret = peek_2002();
			/*
			if (nes.do_the_reread_2002 > 0)
			{
				if (Reg2002_vblank_active || Reg2002_vblank_active_pending)
					Console.WriteLine("reread 2002");
			}
			*/
			
			// reading from $2002 resets the destination for $2005 and $2006 writes
			vtoggle = false;
			Reg2002_vblank_active = 0;
			Reg2002_vblank_active_pending = false;

			if (nes.do_the_reread_2002 > 0)
			{
				ret = peek_2002();
				// could be another reread, but no other side effects, so don't bother
			}

			// update the open bus here
			ppu_open_bus = ret;
			PpuOpenBusDecay(DecayType.High);
			return ret;
		}

		private byte peek_2002()
		{
			//I'm not happy with this, but apparently this is how mighty bobm jack VS works.
			//quite strange that is makes the sprite hit flag go high like this
			if (nes._isVS2c05==2)
			{
				if (nes.Frame<3)
				{

					return (byte)((Reg2002_vblank_active << 7) | (Reg2002_objhit << 6) | (1 << 5) | (0x1D));
				}
				else
				{
					return (byte)((Reg2002_vblank_active << 7) | (Reg2002_objhit << 6) | (Reg2002_objoverflow << 5) | (0x1D));
				}
				
			}
			if (nes._isVS2c05 == 3)
			{
				return (byte)((Reg2002_vblank_active << 7) | (Reg2002_objhit << 6) | (Reg2002_objoverflow << 5) | (0x1C));
			}
			if (nes._isVS2c05 == 4)
			{
				return (byte)((Reg2002_vblank_active << 7) | (Reg2002_objhit << 6) | (Reg2002_objoverflow << 5) | (0x1B));
			}

			return (byte)((Reg2002_vblank_active << 7) | (Reg2002_objhit << 6) | (Reg2002_objoverflow << 5) | (ppu_open_bus & 0x1F));
		}

		//OAM ADDRESS (write)
		private void write_2003(int addr, byte value)
		{
			if (region == Region.NTSC)
			{
				// in NTSC this does several glitchy things to corrupt OAM
				// commented out for now until better understood
				byte temp = (byte)(reg_2003 & 0xF8);
				byte temp_2 = (byte)(addr >> 16 & 0xF8);
				/*
				for (int i=0;i<8;i++)
				{
					glitchy_reads_2003[i] = OAM[temp + i];
					//OAM[temp_2 + i] = glitchy_reads_2003[i];
				}
				*/
				reg_2003 = value;
			}
			else
			{
				// in PAL, just record the oam buffer write target
				reg_2003 = value;
			}
		}

		private byte read_2003() { return ppu_open_bus; }
		private byte peek_2003() { return ppu_open_bus; }

		//OAM DATA (write)
		private void write_2004(byte value)
		{
			if ((reg_2003 & 3) == 2)
			{
				//some of the OAM bits are unwired so we mask them out here
				//otherwise we just write this value and move on to the next oam byte
				value &= 0xE3; 
			}						
			if (ppur.status.rendering)
			{
				// don't write to OAM if the screen is on and we are in the active display area
				// this impacts sprite evaluation
				if (show_bg_new || show_obj_new)
				{
					// glitchy increment of OAM index
					oam_index += 4;
				}
				else
				{
					OAM[reg_2003] = value;
					reg_2003++;
				}				
			}
			else
			{
				OAM[reg_2003] = value;
				reg_2003++;
			}	
		}

		private byte read_2004()
		{
			byte ret;
			// Console.WriteLine("read 2004");
			// behaviour depends on whether things are being rendered or not
			if (PPUON)
			{
				if (ppur.status.sl < 241)
				{
					if (ppur.status.cycle <= 64)
					{
						ret = 0xFF; // during this time all reads return FF
					}
					else if (ppur.status.cycle <= 256)
					{
						ret = read_value;
					}
					else if (ppur.status.cycle <= 320)
					{
						ret = read_value;
					}
					else
					{
						ret = soam[0];
					}
				}
				else
				{
					ret = OAM[reg_2003];
				}
			}
			else
			{
				ret = OAM[reg_2003];
			}

			ppu_open_bus = ret;
			PpuOpenBusDecay(DecayType.All);
			return ret;
		}

		private byte peek_2004() { return OAM[reg_2003]; }

		//SCROLL (write)
		private void write_2005(byte value)
		{
			if (!vtoggle)
			{
				ppur._ht = value >> 3;
				ppur.fh = value & 7;
				//nes.LogLine("scroll wrote ht = {0} and fh = {1}", ppur._ht, ppur.fh);
			}
			else
			{
				ppur._vt = value >> 3;
				ppur._fv = value & 7;
				//nes.LogLine("scroll wrote vt = {0} and fv = {1}", ppur._vt, ppur._fv);
			}
			vtoggle ^= true;
		}

		private byte read_2005() { return ppu_open_bus; }
		private byte peek_2005() { return ppu_open_bus; }

		//VRAM address register (write)
		private void write_2006(byte value)
		{

			if (!vtoggle)
			{
				ppur._vt &= 0x07;
				ppur._vt |= (value & 0x3) << 3;
				ppur._h = (value >> 2) & 1;
				ppur._v = (value >> 3) & 1;
				ppur._fv = (value >> 4) & 3;
				//nes.LogLine("addr wrote fv = {0}", ppur._fv);
				reg_2006_2 = value;
			}
			else
			{
				ppur._vt &= 0x18;
				ppur._vt |= (value >> 5);
				ppur._ht = value & 31;

				// testing indicates that this operation is delayed by 3 pixels
				//ppur.install_latches();				
				install_2006 = 3;
			}

			vtoggle ^= true;
		}

		private byte read_2006() { return ppu_open_bus; }
		private byte peek_2006() { return ppu_open_bus; }

		//VRAM data register (r/w)
		private void write_2007(byte value)
		{
			//does this take 4x longer? nestopia indicates so perhaps...

			int addr = ppur.get_2007access();
			if (ppuphase == PPU_PHASE_BG)
			{
				if (show_bg_new)
				{
					addr = ppur.get_ntread();
				}
			}

			if ((addr & 0x3F00) == 0x3F00)
			{
				//handle palette. this is being done nestopia style, because i found some documentation for it (appendix 1)
				addr &= 0x1F;
				byte color = (byte)(value & 0x3F); //are these bits really unwired? can they be read back somehow?

				//this little hack will help you debug things while the screen is black
				//color = (byte)(addr & 0x3F);

				PALRAM[addr] = color;
				if ((addr & 3) == 0)
				{
					PALRAM[addr ^ 0x10] = color;
				}
			}
			else
			{
				addr &= 0x3FFF;

				ppubus_write(addr, value);

			}

			ppur.increment2007(ppur.status.rendering && PPUON, reg_2000.vram_incr32 != 0);

			//see comments in $2006
			if (ppur.status.sl >= 241 || !PPUON)
				nes.Board.AddressPpu(ppur.get_2007access()); 
		}

		private byte read_2007()
		{
			int addr = ppur.get_2007access() & 0x3FFF;
			int bus_case = 0;	
			//ordinarily we return the buffered values
			byte ret = VRAMBuffer;

			//in any case, we read from the ppu bus
			VRAMBuffer = ppubus_read(addr, false, false);

			//but reads from the palette are implemented in the PPU and return immediately
			if ((addr & 0x3F00) == 0x3F00)
			{
				//TODO apply greyscale shit?
				ret = (byte)(PALRAM[addr & 0x1F] + ((byte)(ppu_open_bus & 0xC0)));
				bus_case = 1;
			}

			ppur.increment2007(ppur.status.rendering && PPUON, reg_2000.vram_incr32 != 0);

			//see comments in $2006
			if (ppur.status.sl >= 241 || !PPUON)
				nes.Board.AddressPpu(ppur.get_2007access());

			// update open bus here
			ppu_open_bus = ret;
			if (bus_case==0)
			{
				PpuOpenBusDecay(DecayType.All);
			}
			else
			{
				PpuOpenBusDecay(DecayType.Low);
			}

			return ret;
		}

		private byte peek_2007()
		{
			int addr = ppur.get_2007access() & 0x3FFF;

			//ordinarily we return the buffered values
			byte ret = VRAMBuffer;

			//in any case, we read from the ppu bus
			// can't do this in peek; updates the value that will be used later
			// VRAMBuffer = ppubus_peek(addr);

			//but reads from the palette are implemented in the PPU and return immediately
			if ((addr & 0x3F00) == 0x3F00)
			{
				//TODO apply greyscale shit?
				ret = PALRAM[addr & 0x1F];
			}

			return ret;
		}
		
		public byte ReadReg(int addr)
		{
			byte ret_spec;
			switch (addr)
			{
				case 0:
					{
						if (nes._isVS2c05>0)
							return read_2001();
						else
							return read_2000();
					}
				case 1:
					{
						if (nes._isVS2c05>0)
							return read_2000();
						else
							return read_2001();
					}
				case 2: return read_2002();
				case 3: return read_2003();
				case 4: return read_2004();
				case 5: return read_2005();
				case 6: return read_2006();
				case 7:
					{
						if (nes.cpu.TotalExecutedCycles == double_2007_read)
						{
							return ppu_open_bus;							
						} 
						else
						{
							ret_spec = read_2007();
							double_2007_read = nes.cpu.TotalExecutedCycles + 1;
						}
						
						if (nes.do_the_reread_2007 > 0)
						{
							ret_spec = read_2007();
							ret_spec = read_2007();
							// always 2?
						}
						return ret_spec;
					}
				default: throw new InvalidOperationException();
			}
		}

		public byte PeekReg(int addr)
		{
			switch (addr)
			{
				case 0: return peek_2000(); case 1: return peek_2001(); case 2: return peek_2002(); case 3: return peek_2003();
				case 4: return peek_2004(); case 5: return peek_2005(); case 6: return peek_2006(); case 7: return peek_2007();
				default: throw new InvalidOperationException();
			}
		}

		public void WriteReg(int addr, byte value)
		{
			PPUGenLatch = value;
			ppu_open_bus = value;

			switch (addr & 0x07)
			{
				case 0:
					if (nes._isVS2c05>0)
						write_2001(value);
					else
						write_2000(value);
					break;
				case 1:
					if (nes._isVS2c05>0)
						write_2000(value);
					else
						write_2001(value);
					break;
				case 2: write_2002(value); break;
				case 3: write_2003(addr, value); break;
				case 4: write_2004(value); break;
				case 5: write_2005(value); break;
				case 6: write_2006(value); break;
				case 7: write_2007(value); break;
				default: throw new InvalidOperationException();
			}
		}
		
		private enum DecayType
		{
			None = 0, // if there is no action, decrement the timer
			All = 1, // reset the timer for all bits (reg 2004 / 2007 (non-palette)
			High = 2, // reset the timer for high 3 bits (reg 2002)
			Low = 3 // reset the timer for all low 6 bits (reg 2007 (palette))

			// other values of action are reserved for possibly needed expansions, but this passes
			// ppu_open_bus for now.
		}

		private void PpuOpenBusDecay(DecayType action)
		{
			switch (action)
			{
				case DecayType.None:
					for (int i = 0; i < 8; i++)
					{
						if (ppu_open_bus_decay_timer[i] == 0)
						{
							ppu_open_bus = (byte)(ppu_open_bus & (0xff - (1 << i)));
							ppu_open_bus_decay_timer[i] = 1786840; // about 1 second worth of cycles
						}
						else
						{
							ppu_open_bus_decay_timer[i]--;
						}
					}
					break;
				case DecayType.All:
					for (int i = 0; i < 8; i++)
					{
						ppu_open_bus_decay_timer[i] = 1786840;
					}
					break;
				case DecayType.High:
					ppu_open_bus_decay_timer[7] = 1786840;
					ppu_open_bus_decay_timer[6] = 1786840;
					ppu_open_bus_decay_timer[5] = 1786840;
					break;
				case DecayType.Low:
					for (int i = 0; i < 6; i++)
					{
						ppu_open_bus_decay_timer[i] = 1786840;
					}
					break;
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


//Address    Size    Description
//$0000      $1000   Pattern Table 0
//$1000      $1000   Pattern Table 1
//$2000      $3C0    Name Table 0
//$23C0      $40     Attribute Table 0
//$2400      $3C0    Name Table 1
//$27C0      $40     Attribute Table 1
//$2800      $3C0    Name Table 2
//$2BC0      $40     Attribute Table 2
//$2C00      $3C0    Name Table 3
//$2FC0      $40     Attribute Table 3
//$3000      $F00    Mirror of 2000h-2EFFh
//$3F00      $10     BG Palette
//$3F10      $10     Sprite Palette
//$3F20      $E0     Mirror of 3F00h-3F1Fh


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
