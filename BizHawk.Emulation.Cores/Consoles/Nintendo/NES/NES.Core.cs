// NOTE: to match Mesen timings, set idleSynch to true at power on, and set start_up_offset to -3

using System;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;

#pragma warning disable 162

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES : IEmulator, ISoundProvider, ICycleTiming
	{
		//hardware/state
		public MOS6502X<CpuLink> cpu;
		public PPU ppu;
		public APU apu;
		public byte[] ram;
		public byte[] CIRAM; //AKA nametables
		string game_name = ""; //friendly name exposed to user and used as filename base
		internal CartInfo cart; //the current cart prototype. should be moved into the board, perhaps
		internal INesBoard Board; //the board hardware that is currently driving things
		EDetectionOrigin origin = EDetectionOrigin.None;
		int sprdma_countdown;

		public bool _irq_apu; //various irq signals that get merged to the cpu irq pin
		
		/// <summary>clock speed of the main cpu in hz</summary>
		public int cpuclockrate { get; private set; }

		//user configuration 
		public int[] palette_compiled = new int[64 * 8];

		//variable set when VS system games are running
		internal bool _isVS = false;
		//some VS games have a ppu that switches 2000 and 2001, so keep trcak of that
		public byte _isVS2c05 = 0;
		//since prg reg for VS System is set in the controller regs, it is convenient to have it here
		//instead of in the board
		public byte VS_chr_reg;
		public byte VS_prg_reg;
		//various VS controls
		public byte[] VS_dips = new byte[8];
		public byte VS_service = 0;
		public byte VS_coin_inserted=0;
		public byte VS_ROM_control;

		// cheat addr index tracker
		// disables all cheats each frame
		public int[] cheat_indexes = new int[0x10000];
		public byte[] cheat_active = new byte[0x10000];
		public int num_cheats;

		// new input system
		NESControlSettings ControllerSettings; // this is stored internally so that a new change of settings won't replace
		IControllerDeck ControllerDeck;
		byte latched4016;

		private DisplayType _display_type = DisplayType.NTSC;

		//Sound config
		public void SetVol1(int v) { apu.m_vol = v; }

		#region Audio

		BlipBuffer blip = new BlipBuffer(4096);
		const int blipbuffsize = 4096;

		public int old_s = 0;

		public bool CanProvideAsync => false;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new NotSupportedException("Only sync mode is supported");
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async not supported");
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void Dispose()
		{
			if (blip != null)
			{
				blip.Dispose();
				blip = null;
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			blip.EndFrame(apu.sampleclock);
			apu.sampleclock = 0;

			nsamp = blip.SamplesAvailable();
			samples = new short[nsamp * 2];

			blip.ReadSamples(samples, nsamp, true);
			// duplicate to stereo
			for (int i = 0; i < nsamp * 2; i += 2)
				samples[i + 1] = samples[i];

			Board.ApplyCustomAudio(samples);
		}

		public void DiscardSamples()
		{
			blip.Clear();
			apu.sampleclock = 0;
		}

		#endregion

		public void HardReset()
		{
			cpu = new MOS6502X<CpuLink>(new CpuLink(this))
			{
				BCD_Enabled = false
			};

			ppu = new PPU(this);
			ram = new byte[0x800];
			CIRAM = new byte[0x800];

			// wire controllers
			// todo: allow changing this
			ControllerDeck = ControllerSettings.Instantiate(ppu.LightGunCallback);
			// set controller definition first time only
			if (ControllerDefinition == null)
			{
				ControllerDefinition = new ControllerDefinition(ControllerDeck.GetDefinition())
				{
					Name = "NES Controller"
				};

				// controls other than the deck
				ControllerDefinition.BoolButtons.Add("Power");
				ControllerDefinition.BoolButtons.Add("Reset");
				if (Board is FDS b)
				{
					ControllerDefinition.BoolButtons.Add("FDS Eject");
					for (int i = 0; i < b.NumSides; i++)
					{
						ControllerDefinition.BoolButtons.Add("FDS Insert " + i);
					}
				}

				if (_isVS)
				{
					ControllerDefinition.BoolButtons.Add("Insert Coin P1");
					ControllerDefinition.BoolButtons.Add("Insert Coin P2");
					ControllerDefinition.BoolButtons.Add("Service Switch");
				}
			}

			// Add in the reset timing float control for subneshawk
			if (using_reset_timing && ControllerDefinition.AxisControls.Count == 0)
			{
				ControllerDefinition.AxisControls.Add("Reset Cycle");
				ControllerDefinition.AxisRanges.Add(new ControllerDefinition.AxisRange(0, 0, 500000));
			}

			// don't replace the magicSoundProvider on reset, as it's not needed
			// if (magicSoundProvider != null) magicSoundProvider.Dispose();

			// set up region
			switch (_display_type)
			{
				case Common.DisplayType.PAL:
					apu = new APU(this, apu, true);
					ppu.region = PPU.Region.PAL;
					VsyncNum = 50;
					VsyncDen = 1;
					cpuclockrate = 1662607;
					cpu_sequence = cpu_sequence_PAL;
					_display_type = DisplayType.PAL;
					ClockRate = 5320342.5;
					break;
				case Common.DisplayType.NTSC:
					apu = new APU(this, apu, false);
					ppu.region = PPU.Region.NTSC;
					VsyncNum = 39375000;
					VsyncDen = 655171;
					cpuclockrate = 1789773;
					cpu_sequence = cpu_sequence_NTSC;
					ClockRate = 5369318.1818181818181818181818182;
					break;
				// this is in bootgod, but not used at all
				case Common.DisplayType.Dendy:
					apu = new APU(this, apu, false);
					ppu.region = PPU.Region.Dendy;
					VsyncNum = 50;
					VsyncDen = 1;
					cpuclockrate = 1773448;
					cpu_sequence = cpu_sequence_NTSC;
					_display_type = DisplayType.Dendy;
					ClockRate = 5320342.5;
					break;
				default:
					throw new Exception("Unknown displaytype!");
			}

			blip.SetRates((uint)cpuclockrate, 44100);

			BoardSystemHardReset();

			// apu has some specific power up bahaviour that we will emulate here
			apu.NESHardReset();

			if (SyncSettings.InitialWRamStatePattern != null && SyncSettings.InitialWRamStatePattern.Any())
			{
				for (int i = 0; i < 0x800; i++)
				{
					ram[i] = SyncSettings.InitialWRamStatePattern[i % SyncSettings.InitialWRamStatePattern.Count];
				}
			}
			else
			{
				// check fceux's PowerNES and FCEU_MemoryRand function for more information:
				// relevant games: Cybernoid; Minna no Taabou no Nakayoshi Daisakusen; Huang Di; and maybe mechanized attack
				for (int i = 0; i < 0x800; i++)
				{
					if ((i & 4) != 0)
					{
						ram[i] = 0xFF;
					}
					else
					{
						ram[i] = 0x00;
					}
				}
			}

			SetupMemoryDomains();

			// some boards cannot have specific values in RAM upon initialization
			// Let's hard code those cases here
			// these will be defined through the gameDB exclusively for now.

			if (cart.GameInfo!=null)
			{
				
				if (cart.GameInfo.Hash == "60FC5FA5B5ACCAF3AEFEBA73FC8BFFD3C4DAE558" // Camerica Golden 5
					|| cart.GameInfo.Hash == "BAD382331C30B22A908DA4BFF2759C25113CC26A" // Camerica Golden 5
					|| cart.GameInfo.Hash == "40409FEC8249EFDB772E6FFB2DCD41860C6CCA23" // Camerica Pegasus 4-in-1
					)
				{
					ram[0x701] = 0xFF;
				}
				
				if (cart.GameInfo.Hash == "68ABE1E49C9E9CCEA978A48232432C252E5912C0") // Dancing Blocks
				{
					ram[0xEC] = 0;
					ram[0xED] = 0;
				}

				if (cart.GameInfo.Hash == "00C50062A2DECE99580063777590F26A253AAB6B") // Silva Saga
				{
					for (int i = 0; i < Board.Wram.Length; i++)
					{
						Board.Wram[i] = 0xFF;
					}
				}
			}
		}

		public long CycleCount => ppu.TotalCycles;
		public double ClockRate { get; private set; }

		private int VsyncNum { get; set; }
		private int VsyncDen { get; set; }

		private IController _controller = NullController.Instance;

		bool resetSignal;
		bool hardResetSignal;
		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_controller = controller;

			if (Tracer.Enabled)
				cpu.TraceCallback = s => Tracer.Put(s);
			else
				cpu.TraceCallback = null;

			lagged = true;
			if (resetSignal)
			{
				Board.NesSoftReset();
				cpu.NESSoftReset();
				apu.NESSoftReset();
				ppu.NESSoftReset();
			}
			else if (hardResetSignal)
			{
				HardReset();
			}

			Frame++;

			//if (resetSignal)
			//Controller.UnpressButton("Reset");   TODO fix this
			resetSignal = controller.IsPressed("Reset");
			hardResetSignal = controller.IsPressed("Power");

			if (Board is FDS)
			{
				var b = Board as FDS;
				if (controller.IsPressed("FDS Eject"))
					b.Eject();
				for (int i = 0; i < b.NumSides; i++)
					if (controller.IsPressed("FDS Insert " + i))
						b.InsertSide(i);
			}

			if (_isVS)
			{
				if (controller.IsPressed("Service Switch"))
					VS_service = 1;
				else
					VS_service = 0;

				if (controller.IsPressed("Insert Coin P1"))
					VS_coin_inserted |= 1;
				else
					VS_coin_inserted &= 2;

				if (controller.IsPressed("Insert Coin P2"))
					VS_coin_inserted |= 2;
				else
					VS_coin_inserted &= 1;
			}

			if (ppu.ppudead > 0)
			{
				while (ppu.ppudead > 0)
				{
					ppu.NewDeadPPU();
				}				
			}
			else
			{
				// do the vbl ticks seperate, that will save us a few checks that don't happen in active region
				while (ppu.do_vbl)
				{
					ppu.TickPPU_VBL();
				}

				// now do the rest of the frame
				while (ppu.do_active_sl)
				{
					ppu.TickPPU_active();
				}

				// now do the pre-NMI lines
				while (ppu.do_pre_vbl)
				{
					ppu.TickPPU_preVBL();
				}
			}
			
			if (lagged)
			{
				_lagcount++;
				islag = true;
			}
			else
				islag = false;

			videoProvider.FillFrameBuffer();

			// turn off all cheats
			// any cheats still active will be re-applied by the buspoke at the start of the next frame
			num_cheats = 0;

			return true;
		}

		// these variables are for subframe input control
		public bool controller_was_latched;
		public bool frame_is_done;
		public bool current_strobe;
		public bool new_strobe;
		public bool alt_lag;
		// variable used with subneshawk to trigger reset at specific cycle after reset
		public bool using_reset_timing = false;
		// this function will run one step of the ppu 
		// it will return whether the controller is read or not.
		public void do_single_step(IController controller, out bool cont_read, out bool frame_done)
		{
			_controller = controller;

			controller_was_latched = false;
			frame_is_done = false;

			current_strobe = new_strobe;
			if (ppu.ppudead > 0)
			{
				ppu.NewDeadPPU();
			}
			else if (ppu.do_vbl)
			{
				ppu.TickPPU_VBL();
			}
			else if (ppu.do_active_sl)
			{
				ppu.TickPPU_active();
			}
			else if (ppu.do_pre_vbl)
			{
				ppu.TickPPU_preVBL();
			}

			cont_read = controller_was_latched;
			frame_done = frame_is_done;
		}

		//PAL:
		//sequence of ppu clocks per cpu clock: 3,3,3,3,4
		//at least it should be, but something is off with that (start up time?) so it is 3,3,3,4,3 for now
		//NTSC:
		//sequence of ppu clocks per cpu clock: 3
		public byte[] cpu_sequence;
		static byte[] cpu_sequence_NTSC = { 3, 3, 3, 3, 3 };
		static byte[] cpu_sequence_PAL = { 3, 3, 3, 4, 3 };
		public int cpu_deadcounter;

		public int oam_dma_index;
		public bool oam_dma_exec = false;
		public ushort oam_dma_addr;
		public byte oam_dma_byte;
		public bool dmc_dma_exec = false;
		public bool dmc_realign;
		public bool IRQ_delay;
		public bool special_case_delay; // very ugly but the only option
		public bool do_the_reread;
		public byte DB; //old data bus values from previous reads

		internal void RunCpuOne()
		{
			///////////////////////////
			// OAM DMA start
			///////////////////////////

			if (sprdma_countdown > 0)
			{
				sprdma_countdown--;
				if (sprdma_countdown == 0)
				{
					if (cpu.TotalExecutedCycles % 2 == 0)
					{
						cpu_deadcounter = 2;
					}
					else
					{
						cpu_deadcounter = 1;
					}
					oam_dma_exec = true;
					cpu.RDY = false;
					oam_dma_index = 0;
					special_case_delay = true;
				}
			}

			if (oam_dma_exec && apu.dmc_dma_countdown != 1 && !dmc_realign)
			{
				if (cpu_deadcounter == 0)
				{

					if (oam_dma_index % 2 == 0)
					{
						oam_dma_byte = ReadMemory(oam_dma_addr);
						oam_dma_addr++;
					}
					else
					{
						WriteMemory(0x2004, oam_dma_byte);
					}
					oam_dma_index++;
					if (oam_dma_index == 512) oam_dma_exec = false;

				}
				else
				{
					cpu_deadcounter--;
				}
			}
			else if (apu.dmc_dma_countdown == 1)
			{
				dmc_realign = true;
			}
			else if (dmc_realign)
			{
				dmc_realign = false;
			}
			/////////////////////////////
			// OAM DMA end
			/////////////////////////////


			/////////////////////////////
			// dmc dma start
			/////////////////////////////

			if (apu.dmc_dma_countdown > 0)
			{
				cpu.RDY = false;
				dmc_dma_exec = true;
				apu.dmc_dma_countdown--;
				if (apu.dmc_dma_countdown == 0)
				{
					apu.RunDMCFetch();
					dmc_dma_exec = false;
					apu.dmc_dma_countdown = -1;
					do_the_reread = true;
				}
			}

			/////////////////////////////
			// dmc dma end
			/////////////////////////////
			apu.RunOneFirst();

			if (cpu.RDY && !IRQ_delay)
			{
				cpu.IRQ = _irq_apu || Board.IrqSignal;
			}
			else if (special_case_delay || apu.dmc_dma_countdown == 3)
			{
				cpu.IRQ = _irq_apu || Board.IrqSignal;
				special_case_delay = false;
			}

			cpu.ExecuteOne();
			Board.ClockCpu();

			int s = apu.EmitSample();

			if (s != old_s)
			{
				blip.AddDelta(apu.sampleclock, s - old_s);
				old_s = s;
			}
			apu.sampleclock++;

			apu.RunOneLast();

			if (ppu.double_2007_read > 0)
				ppu.double_2007_read--;

			if (do_the_reread && cpu.RDY)
				do_the_reread = false;

			if (IRQ_delay)
				IRQ_delay = false;

			if (!dmc_dma_exec && !oam_dma_exec && !cpu.RDY)
			{
				cpu.RDY = true;
				IRQ_delay = true;
			}
		}

		public byte ReadReg(int addr)
		{
			byte ret_spec;
			switch (addr)
			{
				case 0x4000:
				case 0x4001:
				case 0x4002:
				case 0x4003:
				case 0x4004:
				case 0x4005:
				case 0x4006:
				case 0x4007:
				case 0x4008:
				case 0x4009:
				case 0x400A:
				case 0x400B:
				case 0x400C:
				case 0x400D:
				case 0x400E:
				case 0x400F:
				case 0x4010:
				case 0x4011:
				case 0x4012:
				case 0x4013:
					return DB;
				//return apu.ReadReg(addr);
				case 0x4014: /*OAM DMA*/ break;
				case 0x4015: return (byte)((byte)(apu.ReadReg(addr) & 0xDF) + (byte)(DB & 0x20));
				case 0x4016:
					if (_isVS)
					{
						byte ret = 0;
						ret = read_joyport(0x4016);
						ret &= 1;
						ret = (byte)(ret | (VS_service << 2) | (VS_dips[0] << 3) | (VS_dips[1] << 4) | (VS_coin_inserted << 5) | (VS_ROM_control<<7));

						return ret;
					}
					else
					{
						// special hardware glitch case
						ret_spec = read_joyport(addr);
						if (do_the_reread && ppu.region==PPU.Region.NTSC)
						{
							ret_spec = read_joyport(addr);
							do_the_reread = false;
						}
						return ret_spec;

					}
				case 0x4017:
					if (_isVS)
					{
						byte ret = 0;
						ret = read_joyport(0x4017);
						ret &= 1;

						ret = (byte)(ret | (VS_dips[2] << 2) | (VS_dips[3] << 3) | (VS_dips[4] << 4) | (VS_dips[5] << 5) | (VS_dips[6] << 6) | (VS_dips[7] << 7));

						return ret;

					}
					else
					{
						// special hardware glitch case
						ret_spec = read_joyport(addr);
						if (do_the_reread && ppu.region == PPU.Region.NTSC)
						{
							ret_spec = read_joyport(addr);
							do_the_reread = false;
						}
						return ret_spec;
					}
				default:
					//Console.WriteLine("read register: {0:x4}", addr);
					break;

			}
			return DB;
		}

		public byte PeekReg(int addr)
		{
			switch (addr)
			{
				case 0x4000:
				case 0x4001:
				case 0x4002:
				case 0x4003:
				case 0x4004:
				case 0x4005:
				case 0x4006:
				case 0x4007:
				case 0x4008:
				case 0x4009:
				case 0x400A:
				case 0x400B:
				case 0x400C:
				case 0x400D:
				case 0x400E:
				case 0x400F:
				case 0x4010:
				case 0x4011:
				case 0x4012:
				case 0x4013:
					return apu.PeekReg(addr);
				case 0x4014: /*OAM DMA*/ break;
				case 0x4015: return apu.PeekReg(addr);
				case 0x4016:
				case 0x4017:
					return peek_joyport(addr);
				default:
					//Console.WriteLine("read register: {0:x4}", addr);
					break;

			}
			return 0xFF;
		}

		void WriteReg(int addr, byte val)
		{
			switch (addr)
			{
				case 0x4000:
				case 0x4001:
				case 0x4002:
				case 0x4003:
				case 0x4004:
				case 0x4005:
				case 0x4006:
				case 0x4007:
				case 0x4008:
				case 0x4009:
				case 0x400A:
				case 0x400B:
				case 0x400C:
				case 0x400D:
				case 0x400E:
				case 0x400F:
				case 0x4010:
				case 0x4011:
				case 0x4012:
				case 0x4013:
					apu.WriteReg(addr, val);
					break;
				case 0x4014:
					//schedule a sprite dma event for beginning 1 cycle in the future.
					//this receives 2 because that's just the way it works out.
					oam_dma_addr = (ushort)(val << 8);
					sprdma_countdown = 1;
					break;
				case 0x4015: apu.WriteReg(addr, val); break;
				case 0x4016:
					if (_isVS)
					{
						write_joyport(val);
						VS_chr_reg = (byte)((val & 0x4)>>2);

						//TODO: does other stuff for dual system

						//this is actually different then assignment
						VS_prg_reg = (byte)((val & 0x4)>>2);

					}
					else
					{
						write_joyport(val);
					}
					break;
				case 0x4017: apu.WriteReg(addr, val); break;
				default:
					//Console.WriteLine("wrote register: {0:x4} = {1:x2}", addr, val);
					break;
			}
		}

		void write_joyport(byte value)
		{
			var si = new StrobeInfo(latched4016, value);
			ControllerDeck.Strobe(si, _controller);
			latched4016 = value;
			new_strobe = (value & 1) > 0;
			if (current_strobe && !new_strobe)
			{
				controller_was_latched = true;
				alt_lag = false;
			}
		}

		byte read_joyport(int addr)
		{
			InputCallbacks.Call();
			lagged = false;
			byte ret = 0;

			if (_isVS)
			{
				// for whatever reason, in VS left and right controller have swapped regs
				ret = addr == 0x4017 ? ControllerDeck.ReadA(_controller) : ControllerDeck.ReadB(_controller);
			}
			else
			{
				ret = addr == 0x4016 ? ControllerDeck.ReadA(_controller) : ControllerDeck.ReadB(_controller);
			}

			ret &= 0x1f;
			ret |= (byte)(0xe0 & DB);
			return ret;
		}

		byte peek_joyport(int addr)
		{
			// at the moment, the new system doesn't support peeks
			return 0;
		}

		/// <summary>
		/// Sets the provided palette as current.
		/// Applies the current deemph settings if needed to expand a 64-entry palette to 512
		/// </summary>
		public void SetPalette(byte[,] pal)
		{
			int nColors = pal.GetLength(0);
			int nElems = pal.GetLength(1);

			if (nColors == 512)
			{
				//just copy the palette directly
				for (int c = 0; c < 64 * 8; c++)
				{
					int r = pal[c, 0];
					int g = pal[c, 1];
					int b = pal[c, 2];
					palette_compiled[c] = (int)unchecked((int)0xFF000000 | (r << 16) | (g << 8) | b);
				}
			}
			else
			{
				//expand using deemph
				for (int i = 0; i < 64 * 8; i++)
				{
					int d = i >> 6;
					int c = i & 63;
					int r = pal[c, 0];
					int g = pal[c, 1];
					int b = pal[c, 2];
					Palettes.ApplyDeemphasis(ref r, ref g, ref b, d);
					palette_compiled[i] = (int)unchecked((int)0xFF000000 | (r << 16) | (g << 8) | b);
				}
			}
		}

		/// <summary>
		/// looks up an internal NES pixel value to an rgb int (applying the core's current palette and assuming no deemph)
		/// </summary>
		public int LookupColor(int pixel)
		{
			return palette_compiled[pixel];
		}

		public byte DummyReadMemory(ushort addr) { return 0; }

		public void ApplySystemBusPoke(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				ram[(addr & 0x7FF)] = value;
			}
			else if (addr < 0x4000)
			{
				ppu.WriteReg(addr, value);
			}
			else if (addr < 0x4020)
			{
				WriteReg(addr, value);
			}
			else
			{
				// apply a cheat to non-writable memory
				ApplyCheat(addr, value, null);
			}
		}

		public byte PeekMemory(ushort addr)
		{
			byte ret;

			if (addr >= 0x4020)
			{
				//easy optimization, since rom reads are so common, move this up (reordering the rest of these elseifs is not easy)
				ret = Board.PeekCart(addr);
			}
			else if (addr < 0x0800)
			{
				ret = ram[addr];
			}
			else if (addr < 0x2000)
			{
				ret = ram[addr & 0x7FF];
			}
			else if (addr < 0x4000)
			{
				ret = Board.PeekReg2xxx(addr);
			}
			else if (addr < 0x4020)
			{
				ret = PeekReg(addr); //we're not rebasing the register just to keep register names canonical
			}
			else
			{
				throw new Exception("Woopsie-doodle!");
				ret = 0xFF;
			}

			return ret;
		}

		public void ExecFetch(ushort addr)
		{
			uint flags = (uint)(MemoryCallbackFlags.CPUZero | MemoryCallbackFlags.AccessExecute);
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		}

		public byte ReadMemory(ushort addr)
		{
			byte ret;

			if (addr >= 0x8000)
			{
				// easy optimization, since rom reads are so common, move this up (reordering the rest of these else ifs is not easy)
				ret = Board.ReadPrg(addr - 0x8000);
			}
			else if (addr < 0x0800)
			{
				ret = ram[addr];
			}
			else if (addr < 0x2000)
			{
				ret = ram[addr & 0x7FF];
			}
			else if (addr < 0x4000)
			{
				ret = Board.ReadReg2xxx(addr);
			}
			else if (addr < 0x4020)
			{
				ret = ReadReg(addr); // we're not rebasing the register just to keep register names canonical
			}
			else if (addr < 0x6000)
			{
				ret = Board.ReadExp(addr - 0x4000);
			}
			else
			{
				ret = Board.ReadWram(addr - 0x6000);
			}

			// handle cheats (currently cheats can only freeze read only areas)
			// there is no way to distinguish between a memory poke and a memory freeze
			if (num_cheats !=0)
			{
				for (int i = 0; i < num_cheats; i++)
				{
					if(cheat_indexes[i] == addr)
					{
						ret = cheat_active[addr];
					}
				}
			}

			uint flags = (uint)(MemoryCallbackFlags.CPUZero | MemoryCallbackFlags.AccessRead);
			MemoryCallbacks.CallMemoryCallbacks(addr, ret, flags, "System Bus");

			DB = ret;
			return ret;
		}

		public void ApplyCheat(int addr, byte value, byte? compare)
		{
			if (addr <= 0xFFFF)
			{
				cheat_indexes[num_cheats] = addr;
				cheat_active[addr] = value;
				num_cheats++;
			}
		}

		public void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x0800)
			{
				ram[addr] = value;
			}
			else if (addr < 0x2000)
			{
				ram[addr & 0x7FF] = value;
			}
			else if (addr < 0x4000)
			{
				Board.WriteReg2xxx(addr, value);
			}
			else if (addr < 0x4020)
			{
				WriteReg(addr, value);  //we're not rebasing the register just to keep register names canonical
			}
			else if (addr < 0x6000)
			{
				Board.WriteExp(addr - 0x4000, value);
			}
			else if (addr < 0x8000)
			{
				Board.WriteWram(addr - 0x6000, value);
			}
			else
			{
				Board.WritePrg(addr - 0x8000, value);
			}

			uint flags = (uint)(MemoryCallbackFlags.CPUZero | MemoryCallbackFlags.AccessWrite | MemoryCallbackFlags.SizeByte);
			MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");
		}

		// the palette for each VS game needs to be chosen explicitly since there are 6 different ones.
		public void PickVSPalette(CartInfo cart)
		{
			switch (cart.Palette)
			{
				case "2C05": SetPalette(Palettes.palette_2c03_2c05); ppu.CurrentLuma = PPU.PaletteLuma2C03; break;
				case "2C04-1": SetPalette(Palettes.palette_2c04_001); ppu.CurrentLuma = PPU.PaletteLuma2C04_1; break;
				case "2C04-2": SetPalette(Palettes.palette_2c04_002); ppu.CurrentLuma = PPU.PaletteLuma2C04_2; break;
				case "2C04-3": SetPalette(Palettes.palette_2c04_003); ppu.CurrentLuma = PPU.PaletteLuma2C04_3; break;
				case "2C04-4": SetPalette(Palettes.palette_2c04_004); ppu.CurrentLuma = PPU.PaletteLuma2C04_4; break;
			}

			//since this will run for every VS game, let's get security setting too
			//values below 16 are for the 2c05 PPU
			//values 16,32,48 are for Namco games and dealt with in mapper 206
			_isVS2c05 = (byte)(cart.VsSecurity & 15);
		}

	}
}
