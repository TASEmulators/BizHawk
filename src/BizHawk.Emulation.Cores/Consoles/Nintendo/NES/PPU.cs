using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class PPU
	{
		public int cpu_step, cpu_stepcounter;

		public bool HasClockPPU = false;

		// this only handles region differences within the PPU
		private int preNMIlines;
		private int postNMIlines;
		private bool chopdot;
		public enum Region { NTSC, PAL, Dendy, RGB }

		private Region _region;
		public Region region
		{
			get => _region;
			set { _region = value; SyncRegion(); }
		}

		private void SyncRegion()
		{
			switch (region)
			{
				case Region.NTSC:
					preNMIlines = 1; postNMIlines = 20; chopdot = true; break;
				case Region.PAL:
					preNMIlines = 1; postNMIlines = 70; chopdot = false; break;
				case Region.Dendy:
					preNMIlines = 51; postNMIlines = 20; chopdot = false; break;
				case Region.RGB:
					preNMIlines = 1; postNMIlines = 20; chopdot = false; break;
			}
		}

		public class DebugCallback
		{
			public int Scanline;
			//public int Dot; //not supported
			public Action Callback;
		}

		public DebugCallback NTViewCallback;
		public DebugCallback PPUViewCallback;

		// luminance of each palette value for lightgun calculations
		// this is all 101% guesses, and most certainly various levels of wrong
		public static readonly int[] PaletteLumaNES =
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 0, 0,
			32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 0, 0,
			48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 0, 0
		};

		public static readonly int[] PaletteLuma2C03 =
		{
			27, 9, 3, 22, 9, 11, 16, 20, 18, 14, 19, 25, 14, 0, 0, 0, 
			45, 23, 17, 12, 14, 16, 13, 30, 27, 27, 25, 34, 28, 0, 0, 0, 
			63, 42, 37, 35, 18, 37, 39, 45, 50, 44, 45, 52, 49, 0, 0, 0, 
			63, 52, 48, 50, 43, 48, 54, 59, 60, 55, 54, 52, 50, 0, 0, 0, 
		};

		public static readonly int[] PaletteLuma2C04_1 =
		{
			48, 35, 13, 37, 28, 14, 18, 16, 63, 27, 45, 11, 9, 50, 18, 63, 
			42, 45, 12, 44, 50, 48, 54, 17, 52, 52, 0, 3, 54, 36, 18, 9, 
			1, 52, 50, 45, 49, 14, 34, 14, 0, 20, 43, 16, 12, 3, 39, 0, 
			0, 27, 45, 19, 55, 22, 58, 30, 12, 23, 25, 9, 60, 37, 27, 54, 
		};

		public static readonly int[] PaletteLuma2C04_2 =
		{
			0, 45, 27, 55, 54, 37, 28, 52, 13, 12, 60, 43, 63, 35, 50, 25, 
			12, 42, 16, 54, 34, 44, 3, 37, 18, 18, 1, 52, 48, 18, 0, 22, 
			9, 54, 39, 50, 23, 12, 45, 3, 14, 52, 27, 14, 17, 0, 50, 63, 
			45, 9, 45, 30, 14, 9, 16, 27, 0, 49, 20, 58, 48, 11, 19, 36, 
		};

		public static readonly int[] PaletteLuma2C04_3 =
		{
			14, 37, 54, 45, 25, 63, 52, 14, 9, 0, 54, 18, 16, 54, 45, 50, 
			37, 28, 11, 17, 27, 27, 30, 34, 27, 22, 0, 3, 13, 16, 43, 48, 
			35, 12, 1, 58, 9, 45, 39, 63, 44, 9, 42, 18, 23, 36, 0, 12, 
			49, 3, 55, 50, 20, 45, 50, 18, 19, 0, 48, 60, 12, 52, 52, 14, 
		};

		public static readonly int[] PaletteLuma2C04_4 =
		{
			27, 22, 28, 50, 0, 48, 9, 30, 45, 12, 45, 1, 54, 58, 25, 55, 
			37, 3, 17, 43, 0, 18, 16, 39, 45, 34, 37, 27, 9, 0, 54, 42, 
			11, 19, 20, 3, 12, 14, 27, 16, 14, 54, 23, 12, 9, 60, 36, 18, 
			50, 63, 18, 13, 52, 52, 63, 50, 0, 45, 35, 52, 44, 48, 49, 14, 
		};

		private int[] _currentLuma = PaletteLumaNES;

		public int[] CurrentLuma
		{
			get => _currentLuma;
			set => _currentLuma = value;
		}

		// true = light sensed
		public bool LightGunCallback(int x, int y)
		{
			// the actual light gun circuit is very complex
			// and this doesn't do it justice at all, as expected

			const int radius = 10; // look at pixel values up to this far away, roughly

			int sum = 0;
			int ymin = Math.Max(Math.Max(y - radius, ppur.status.sl - 20), 0);
			if (ymin > 239) { ymin = 239; }
			int ymax = Math.Min(y + radius, 239);
			int xmin = Math.Max(x - radius, 0);
			int xmax = Math.Min(x + radius, 255);

			int ystop = ppur.status.sl - 2;
			int xstop = ppur.status.cycle - 20;

			bool all_stop = false;

			int j = ymin;
			int i = xmin;
			short s = 0;
			short palcolor = 0;
			short intensity = 0;

			if (j >= ystop && i >= xstop || j > ystop) { all_stop = true; }

			while (!all_stop)
			{
				s = xbuf[j * 256 + i];
				palcolor = (short)(s & 0x3F);
				intensity = (short)((s >> 6) & 0x7);

				sum += _currentLuma[palcolor];

				i++;
				if (i > xmax)
				{
					i = xmin;
					j++;

					if (j > ymax)
					{
						all_stop = true;
					}
				}

				if (j >= ystop && i >= xstop || j > ystop) { all_stop = true; }
			}

			return sum >= 2000;
		}

		//when the ppu issues a write it goes through here and into the game board
		public void ppubus_write(int addr, byte value)
		{
			if (ppur.status.sl >= 241 || !PPUON)
				nes.Board.AddressPpu(addr);

			nes.Board.WritePpu(addr, value);
		}

		//when the ppu issues a read it goes through here and into the game board
		public byte ppubus_read(int addr, bool ppu, bool addr_ppu)
		{
			//hardware doesnt touch the bus when the PPU is disabled
			if (!PPUON && ppu)
				return 0xFF;

			if (addr_ppu)
				nes.Board.AddressPpu(addr);

			return nes.Board.ReadPpu(addr);
		}

		//debug tools peek into the ppu through this
		public byte ppubus_peek(int addr)
		{
			return nes.Board.PeekPPU(addr);
		}

		public const int PPU_PHASE_VBL = 0;
		public const int PPU_PHASE_BG = 1;
		public const int PPU_PHASE_OBJ = 2;

		public int ppuphase;

		private readonly NES nes;
		public PPU(NES nes)
		{
			this.nes = nes;

			OAM = new byte[0x100];

			//power-up palette verified by blargg's power_up_palette test.
			//he speculates that these may differ depending on the system tested..
			//and I don't see why the ppu would waste any effort setting these.. 
			//but for the sake of uniformity, we'll do it.
			PALRAM = new byte[] {
				0x09,0x01,0x00,0x01,0x00,0x02,0x02,0x0D,0x08,0x10,0x08,0x24,0x00,0x00,0x04,0x2C,
				0x09,0x01,0x34,0x03,0x00,0x04,0x00,0x14,0x08,0x3A,0x00,0x02,0x00,0x20,0x2C,0x08
			};

			Reset();
		}

		public void NESSoftReset()
		{
			//this hasn't been brought up to date since NEShawk was first made.
			//in particular http://wiki.nesdev.com/w/index.php/PPU_power_up_state should be studied, but theres no use til theres test cases
			Reset();
		}

		//state
		public int ppudead; //measured in frames
		private bool idleSynch;
		private int NMI_PendingInstructions;
		private byte PPUGenLatch;
		private bool vtoggle;
		private byte VRAMBuffer;
		public byte[] OAM;
		public byte[] PALRAM;

		private long _totalCycles;
		public long TotalCycles => _totalCycles;

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(cpu_step), ref cpu_step);
			ser.Sync(nameof(cpu_stepcounter), ref cpu_stepcounter);
			ser.Sync(nameof(ppudead), ref ppudead);
			ser.Sync(nameof(idleSynch), ref idleSynch);
			ser.Sync(nameof(NMI_PendingInstructions), ref NMI_PendingInstructions);
			ser.Sync(nameof(PPUGenLatch), ref PPUGenLatch);
			ser.Sync(nameof(vtoggle), ref vtoggle);
			ser.Sync(nameof(VRAMBuffer), ref VRAMBuffer);
			ser.Sync(nameof(ppu_addr_temp), ref ppu_addr_temp);

			ser.Sync(nameof(spr_true_count), ref spr_true_count);
			ser.Sync(nameof(sprite_eval_write), ref sprite_eval_write);
			ser.Sync(nameof(read_value), ref read_value);
			ser.Sync("Prev_soam_index", ref soam_index_prev);
			ser.Sync("Spr_Zero_Go", ref sprite_zero_go);
			ser.Sync("Spr_zero_in_Range", ref sprite_zero_in_range);
			ser.Sync(nameof(is_even_cycle), ref is_even_cycle);
			ser.Sync(nameof(soam_index), ref soam_index);
			ser.Sync(nameof(soam_m_index), ref soam_m_index);
			ser.Sync(nameof(oam_index), ref oam_index);
			ser.Sync(nameof(oam_index_aux), ref oam_index_aux);
			ser.Sync(nameof(soam_index_aux), ref soam_index_aux);
			ser.Sync(nameof(yp), ref yp);
			ser.Sync(nameof(target), ref target);
			ser.Sync(nameof(ppu_was_on), ref ppu_was_on);
			ser.Sync(nameof(ppu_was_on_spr), ref ppu_was_on_spr);
			ser.Sync(nameof(spriteHeight), ref spriteHeight);
			ser.Sync(nameof(install_2006), ref install_2006);
			ser.Sync(nameof(race_2006), ref race_2006);
			ser.Sync(nameof(race_2006_2), ref race_2006_2);
			ser.Sync(nameof(install_2001), ref install_2001);
			ser.Sync(nameof(show_bg_new), ref show_bg_new);
			ser.Sync(nameof(show_obj_new), ref show_obj_new);

			ser.Sync(nameof(ppu_open_bus), ref ppu_open_bus);
			ser.Sync(nameof(double_2007_read), ref double_2007_read);
			ser.Sync(nameof(ppu_open_bus_decay_timer), ref ppu_open_bus_decay_timer, false);
			ser.Sync(nameof(glitchy_reads_2003), ref glitchy_reads_2003, false);

			ser.Sync(nameof(OAM), ref OAM, false);
			ser.Sync(nameof(soam), ref soam, false);
			ser.Sync(nameof(PALRAM), ref PALRAM, false);
			ser.Sync(nameof(ppuphase), ref ppuphase);

			ser.Sync(nameof(Reg2002_objoverflow), ref Reg2002_objoverflow);
			ser.Sync(nameof(Reg2002_objhit), ref Reg2002_objhit);
			ser.Sync(nameof(Reg2002_vblank_active), ref Reg2002_vblank_active);
			ser.Sync(nameof(Reg2002_vblank_active_pending), ref Reg2002_vblank_active_pending);
			ser.Sync(nameof(Reg2002_vblank_clear_pending), ref Reg2002_vblank_clear_pending);
			ppur.SyncState(ser);
			byte temp8 = reg_2000.Value; ser.Sync($"{nameof(reg_2000)}.{nameof(reg_2000.Value)}", ref temp8); reg_2000.Value = temp8;
			temp8 = reg_2001.Value; ser.Sync($"{nameof(reg_2001)}.{nameof(reg_2001.Value)}", ref temp8); reg_2001.Value = temp8;
			ser.Sync(nameof(reg_2003), ref reg_2003);

			//don't sync framebuffer into binary (rewind) states
			if(ser.IsText)
				ser.Sync(nameof(xbuf), ref xbuf, false);

			ser.Sync(nameof(_totalCycles), ref _totalCycles);

			ser.Sync(nameof(do_vbl), ref do_vbl);
			ser.Sync(nameof(do_active_sl), ref do_active_sl);
			ser.Sync(nameof(do_pre_vbl), ref do_pre_vbl);

			ser.Sync(nameof(nmi_destiny), ref nmi_destiny);
			ser.Sync(nameof(evenOddDestiny), ref evenOddDestiny);
			ser.Sync(nameof(start_up_offset), ref start_up_offset);
			ser.Sync(nameof(NMI_offset), ref NMI_offset);
			ser.Sync(nameof(yp_shift), ref yp_shift);
			ser.Sync(nameof(sprite_eval_cycle), ref sprite_eval_cycle);
			ser.Sync(nameof(xt), ref xt);
			ser.Sync(nameof(xp), ref xp);
			ser.Sync(nameof(xstart), ref xstart);
			ser.Sync(nameof(rasterpos), ref rasterpos);
			ser.Sync(nameof(renderspritenow), ref renderspritenow);
			ser.Sync(nameof(s), ref s);
			ser.Sync(nameof(ppu_aux_index), ref ppu_aux_index);
			ser.Sync(nameof(junksprite), ref junksprite);
			ser.Sync(nameof(line), ref line);
			ser.Sync(nameof(patternNumber), ref patternNumber);
			ser.Sync(nameof(patternAddress), ref patternAddress);
			ser.Sync(nameof(temp_addr), ref temp_addr);
			ser.Sync(nameof(sl_sprites), ref sl_sprites, false);

			byte bg_byte;
			for (int i = 0; i < 34; i++)
			{
				string str = "bgdata" + i + "at";
				bg_byte = bgdata[i].at; ser.Sync(str, ref bg_byte); bgdata[i].at = bg_byte;
				str = "bgdata" + i + "nt";
				bg_byte = bgdata[i].nt; ser.Sync(str, ref bg_byte); bgdata[i].nt = bg_byte;
				str = "bgdata" + i + "pt0";
				bg_byte = bgdata[i].pt_0; ser.Sync(str, ref bg_byte); bgdata[i].pt_0 = bg_byte;
				str = "bgdata" + i + "pt1";
				bg_byte = bgdata[i].pt_1; ser.Sync(str, ref bg_byte); bgdata[i].pt_1 = bg_byte;
			}

			byte oam_byte;
			for (int i = 0; i < 64; i++)
			{
				string str = "oamdata" + i + "y";
				oam_byte = t_oam[i].oam_y; ser.Sync(str, ref oam_byte); t_oam[i].oam_y = oam_byte;
				str = "oamdata" + i + "ind";
				oam_byte = t_oam[i].oam_ind; ser.Sync(str, ref oam_byte); t_oam[i].oam_ind = oam_byte;
				str = "oamdata" + i + "attr";
				oam_byte = t_oam[i].oam_attr; ser.Sync(str, ref oam_byte); t_oam[i].oam_attr = oam_byte;
				str = "oamdata" + i + "x";
				oam_byte = t_oam[i].oam_x; ser.Sync(str, ref oam_byte); t_oam[i].oam_x = oam_byte;
				str = "oamdata" + i + "p0";
				oam_byte = t_oam[i].patterns_0; ser.Sync(str, ref oam_byte); t_oam[i].patterns_0 = oam_byte;
				str = "oamdata" + i + "p1";
				oam_byte = t_oam[i].patterns_1; ser.Sync(str, ref oam_byte); t_oam[i].patterns_1 = oam_byte;
			}
		}

		public void Reset()
		{
			regs_reset();
			ppudead = 1;
			idleSynch = true;
			ppu_open_bus = 0;
			ppu_open_bus_decay_timer = new int[8];
			double_2007_read = 0;
			start_up_offset = 4;
		}

		private void runppu()
		{
			//run one ppu cycle at a time so we can interact with the ppu and clockPPU at high granularity			
			if (install_2006 > 0)
			{
				install_2006--;
				if (install_2006==0)
				{
					if (!race_2006) { ppur.install_latches(); }
					else { race_2006_2 = true; }

					//nes.LogLine("addr wrote vt = {0}, ht = {1}", ppur._vt, ppur._ht);
					//normally the address isnt observed by the board till it gets clocked by a read or write.
					//but maybe that's just because a ppu read/write shoves it on the address bus
					//apparently this shoves it on the address bus, too, or else blargg's mmc3 tests don't pass
					//ONLY if the ppu is not rendering
					if (ppur.status.sl >= 241 || !PPUON)
						nes.Board.AddressPpu(ppur.get_2007access());
				}
			}

			race_2006 = false;

			if (install_2001 > 0)
			{
				install_2001--;
				if (install_2001 == 0)
				{
					show_bg_new = reg_2001.show_bg;
					show_obj_new = reg_2001.show_obj;
				}
			}

			ppur.status.cycle++;
			is_even_cycle = !is_even_cycle;

			if (ppur.status.cycle >= 257 && ppur.status.cycle <= 320 && ppur.status.sl <= 240 && PPUON)
			{
				reg_2003 = 0;
			}

			// Here we execute a CPU instruction if enough PPU cycles have passed
			// also do other things that happen at instruction level granularity
			cpu_stepcounter++;
			if (cpu_stepcounter == nes.cpu_sequence[cpu_step])
			{
				cpu_step++;
				if (cpu_step == 5) cpu_step = 0;
				cpu_stepcounter = 0;

				// this is where the CPU instruction is called
				nes.RunCpuOne();

				// decay the ppu bus, approximating real behaviour
				PpuOpenBusDecay(DecayType.None);

				// Check for NMIs
				if (NMI_PendingInstructions > 0)
				{
					NMI_PendingInstructions--;
					if (NMI_PendingInstructions <= 0)
					{
						nes.cpu.NMI = true;
					}
				}
			}

			if (HasClockPPU)
			{
				nes.Board.ClockPpu();
			}
			_totalCycles += 1;
		}		
	}
}
