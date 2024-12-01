// NOTE: to match Mesen timings, set idleSynch to true at power on, and set start_up_offset to -3

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;

#pragma warning disable 162

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES : IEmulator, ISoundProvider, ICycleTiming
	{
		internal static class RomChecksums
		{
			public const string CamericaGolden5 = /*sha1:*/"60FC5FA5B5ACCAF3AEFEBA73FC8BFFD3C4DAE558";

			public const string CamericaGolden5Overdump = /*sha1:*/"BAD382331C30B22A908DA4BFF2759C25113CC26A";

			public const string CamericaPegasus4in1 = /*sha1:*/"40409FEC8249EFDB772E6FFB2DCD41860C6CCA23";

			public const string DancingBlocks = /*sha1:*/"68ABE1E49C9E9CCEA978A48232432C252E5912C0";

			public const string SeicrossRev2 = "SHA1:4C9C05FAD6F6F33A92A27C2EDC1E7DE12D7F216D"; // yes this is meant to include the prefix

			public const string SilvaSaga = /*sha1:*/"00C50062A2DECE99580063777590F26A253AAB6B";

			public const string Fam_Jump_II = /*sha1:*/"1D7417D31E19B590AFCEB6A8A6E7B9CAB9F9B475";
		}

		//hardware/state
		public MOS6502X<CpuLink> cpu;
		public PPU ppu;
		public APU apu;
		public byte[] ram;
		public byte[] CIRAM; //AKA nametables
		private string game_name = ""; //friendly name exposed to user and used as filename base
		internal CartInfo cart; //the current cart prototype. should be moved into the board, perhaps
		internal INesBoard Board; //the board hardware that is currently driving things
		private EDetectionOrigin origin = EDetectionOrigin.None;
		private int sprdma_countdown;

		public bool _irq_apu; //various irq signals that get merged to the cpu irq pin
		
		/// <summary>
		/// Clock speed of the main cpu in hz.  Used to time audio synthesis, which runs off the cpu clock.
		/// </summary>
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
		public int[] cheat_addresses = new int[0x1000];
		public byte[] cheat_value = new byte[0x1000];
		public int[] cheat_compare_val = new int[0x1000];
		public int[] cheat_compare_type = new int[0x1000];
		public int num_cheats;

		// new input system
		private readonly NESControlSettings ControllerSettings; // this is stored internally so that a new change of settings won't replace
		private IControllerDeck ControllerDeck;
		private byte latched4016;

		private DisplayType _display_type = DisplayType.NTSC;

		private BlipBuffer blip = new BlipBuffer(4096);
		private const int blipbuffsize = 4096;

		public int old_s = 0;

		public bool CanProvideAsync => false;

		internal void ResetControllerDefinition(bool subframe)
		{
			ControllerDefinition = null;

			ControllerDeck = ControllerSettings.Instantiate(ppu.LightGunCallback);
			ControllerDefinition = ControllerDeck.ControllerDef;

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

			// Add in the reset timing axis for subneshawk
			if (subframe)
			{
				ControllerDefinition.AddAxis("Reset Cycle", 0.RangeTo(500000), 0);
			}

			ControllerDefinition.MakeImmutable();
		}

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

		public void HardReset()
		{
			cpu = new MOS6502X<CpuLink>(new CpuLink(this))
			{
				BCD_Enabled = false
			};

			ppu = new PPU(this);
			ram = new byte[0x800];
			CIRAM = new byte[0x800];

			// don't replace the magicSoundProvider on reset, as it's not needed
			// if (magicSoundProvider != null) magicSoundProvider.Dispose();

			// set up region
			switch (_display_type)
			{
				case DisplayType.PAL:
					apu = new APU(this, apu, true);
					ppu.region = PPU.Region.PAL;
					cpuclockrate = 1662607;
					VsyncNum = cpuclockrate * 2;
					VsyncDen = 66495;
					cpu_sequence = cpu_sequence_PAL;
					_display_type = DisplayType.PAL;
					ClockRate = 5320342.5;
					break;
				case DisplayType.NTSC:
					apu = new APU(this, apu, false);
					ppu.region = PPU.Region.NTSC;
					cpuclockrate = 1789773;
					VsyncNum = cpuclockrate * 2;
					VsyncDen = 59561;
					cpu_sequence = cpu_sequence_NTSC;
					ClockRate = 5369318.1818181818181818181818182;
					break;
				// this is in bootgod, but not used at all
				case DisplayType.Dendy:
					apu = new APU(this, apu, false);
					ppu.region = PPU.Region.Dendy;
					cpuclockrate = 1773448;
					VsyncNum = cpuclockrate;
					VsyncDen = 35464;
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

			var initWRAMPattern = SyncSettings.InitialWRamStatePattern;
			if (initWRAMPattern.Length is not 0)
			{
				for (int i = 0; i < 0x800; i++)
				{
					ram[i] = initWRAMPattern[i % initWRAMPattern.Length];
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
			var hash = cart.GameInfo?.Hash; // SHA1 or MD5 (see NES.IdentifyFromGameDB)
			if (hash is null)
			{
				// short-circuit
			}
			else if (hash is RomChecksums.CamericaGolden5 or RomChecksums.CamericaGolden5Overdump or RomChecksums.CamericaPegasus4in1)
			{
				ram[0x701] = 0xFF;
			}
			else if (hash == RomChecksums.DancingBlocks)
			{
				ram[0xEC] = 0;
				ram[0xED] = 0;
			}
			else if (hash == RomChecksums.SilvaSaga || hash == RomChecksums.Fam_Jump_II)
			{
				for (int i = 0; i < Board.Wram.Length; i++)
				{
					Board.Wram[i] = 0xFF;
				}
			}
		}

		public long CycleCount => ppu.TotalCycles;
		public double ClockRate { get; private set; }

		private int VsyncNum { get; set; }
		private int VsyncDen { get; set; }

		private IController _controller = NullController.Instance;

		private bool resetSignal;
		private bool hardResetSignal;

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_controller = controller;

			if (Tracer.IsEnabled())
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

			Frame++;

			return true;
		}

		// these variables are for subframe input control
		public bool controller_was_latched;
		public bool frame_is_done;
		public bool current_strobe;
		public bool new_strobe;

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
		private static readonly byte[] cpu_sequence_NTSC = { 3, 3, 3, 3, 3 };
		private static readonly byte[] cpu_sequence_PAL = { 3, 3, 3, 4, 3 };
		public int cpu_deadcounter;

		public int oam_dma_index;
		public bool oam_dma_exec = false;
		public ushort oam_dma_addr;
		public byte oam_dma_byte;
		public bool dmc_dma_exec = false;
		public bool dmc_realign;
		public bool reread_trigger;
		public int do_the_reread_2002, do_the_reread_2007, do_the_reread_cont_1, do_the_reread_cont_2;
		public int reread_opp_4016, reread_opp_4017;
		public byte DB; //old data bus values from previous reads

		internal void RunCpuOne()
		{
			///////////////////////////
			// OAM DMA start
			///////////////////////////

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
					if (oam_dma_index == 512) 
					{
						oam_dma_exec = false;
					}
				}
				else
				{
					cpu_deadcounter--;
				}
			}
			
			dmc_realign = false;

			/////////////////////////////
			// OAM DMA end
			/////////////////////////////


			/////////////////////////////
			// dmc dma start
			/////////////////////////////

			if (apu.dmc_dma_countdown > 0)
			{
				if (apu.dmc_dma_countdown == 1)
				{
					dmc_realign = true;
				}
				
				// By this point the cpu should be frozen, if it is not, then we are in a multi-write opcode, add another cycle delay
				if (!cpu.RDY && !cpu.rdy_freeze && (apu.dmc_dma_countdown == apu.DMC_RDY_check))
				{
					//Console.WriteLine("dmc double " + cpu.TotalExecutedCycles + " " + cpu.opcode + " " + cpu.mi);
					apu.dmc_dma_countdown += 2;
				}

				cpu.RDY = false;
				dmc_dma_exec = true;
				apu.dmc_dma_countdown--;
				if (apu.dmc_dma_countdown == 0)
				{
					reread_trigger = true;

					do_the_reread_2002++;

					do_the_reread_2007++;

					// if the DMA address has the same bits set as the re-read address, they don't occur
					// TODO: need to check if also true for ppu regs
					/*
					if ((apu.dmc.sample_address & 0x2007) != 0x2002)
					{
						do_the_reread_2002++;
					}

					if ((apu.dmc.sample_address & 0x2007) != 0x2007)
					{
						do_the_reread_2007++;
					}
					*/
					if ((apu.dmc.sample_address & 0x1F) != 0x16)
					{
						do_the_reread_cont_1++;
					}

					if ((apu.dmc.sample_address & 0x1F) == 0x16)
					{
						reread_opp_4016++;
					}

					if ((apu.dmc.sample_address & 0x1F) != 0x17)
					{
						do_the_reread_cont_2++;
					}

					if ((apu.dmc.sample_address & 0x1F) == 0x17)
					{
						reread_opp_4017++;
					}

					apu.RunDMCFetch();

					dmc_dma_exec = false;
					apu.dmc_dma_countdown = -1;

					if ((apu.dmc.timer == 2) && (apu.dmc.out_bits_remaining == 0))
					{
						//Console.WriteLine("close " + cpu.TotalExecutedCycles + " " + apu.dmc.timer + " " + apu.dmc.sample_length + " " + cpu.opcode + " " + cpu.mi);
						if (apu.dmc.sample_length != 0)
						{
							apu.dmc.fill_glitch = true;
						}					
					}

					if ((apu.dmc.timer == 4) && (apu.dmc.out_bits_remaining == 0) && (apu.dmc.sample_length == 1))
					{
						//Console.WriteLine("close 2 " + cpu.TotalExecutedCycles + " " + apu.dmc.timer + " " + apu.dmc.sample_length + " " + cpu.opcode + " " + cpu.mi);
						apu.dmc.fill_glitch_2 = true;
					}
				}
			}

			/////////////////////////////
			// dmc dma end
			/////////////////////////////
			apu.RunOneFirst();

			cpu.IRQ = _irq_apu || Board.IrqSignal;

			// DMC was started in the APU, but in this case it only lasts 1 cycle and is then aborted, so put this here
			// NOTE: for some famicoms, this will also clock controllers, this will need to be handled if emulating additional models
			if (apu.dmc.fill_glitch_2_end)
			{
				apu.dmc_dma_countdown = -1;
				dmc_dma_exec = false;
				apu.dmc.fill_glitch_2 = false;
				apu.dmc.fill_glitch_2_end = false;
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

			if (reread_trigger && cpu.RDY)
			{
				do_the_reread_2002 = 0;
				do_the_reread_2007 = 0;
				do_the_reread_cont_1 = 0;
				do_the_reread_cont_2 = 0;
				reread_opp_4016 = 0;
				reread_opp_4017 = 0;
				reread_trigger = false;
			}			

			if (!cpu.RDY && !dmc_dma_exec && !oam_dma_exec)
			{
				cpu.RDY = true;
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

						//if (reread_trigger && (do_the_reread_cont_1 == 0)) { Console.WriteLine("same 1 " + (apu.dmc.sample_address - 1)); }

						if ((reread_opp_4017 > 0) && ppu.region == PPU.Region.NTSC)
						{
							read_joyport(0x4017);
							//Console.WriteLine("DMC glitch player 2 opposite " + cpu.TotalExecutedCycles + " addr " + (apu.dmc.sample_address - 1));
						}

						if ((do_the_reread_cont_1 > 0) && ppu.region==PPU.Region.NTSC)
						{
							ret_spec = read_joyport(addr);
							do_the_reread_cont_1--;
							if (do_the_reread_cont_1 > 0) { ret_spec = read_joyport(addr); }
							//Console.WriteLine("DMC glitch player 1 " + cpu.TotalExecutedCycles + " addr " + (apu.dmc.sample_address - 1));
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

						//if (reread_trigger && (do_the_reread_cont_2 == 0)) { Console.WriteLine("same 2 " + (apu.dmc.sample_address - 1)); }

						if ((reread_opp_4016 > 0) && ppu.region == PPU.Region.NTSC)
						{
							read_joyport(0x4016);
							//Console.WriteLine("DMC glitch player 1 opposite " + cpu.TotalExecutedCycles + " addr " + (apu.dmc.sample_address - 1));
						}

						if ((do_the_reread_cont_2 > 0) && ppu.region == PPU.Region.NTSC)
						{
							ret_spec = read_joyport(addr);
							do_the_reread_cont_2--;
							if (do_the_reread_cont_2 > 0) { ret_spec = read_joyport(addr); }
							//Console.WriteLine("DMC glitch player 2 " + cpu.TotalExecutedCycles + " addr " + (apu.dmc.sample_address - 1));
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

		private void WriteReg(int addr, byte val)
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

					if (sprdma_countdown > 0)
					{
						sprdma_countdown--;
						if (sprdma_countdown == 0)
						{
							if (apu.dmc.timer % 2 == 0)
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
						}
					}
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

		private void write_joyport(byte value)
		{
			//Console.WriteLine("cont " + value + " frame " + Frame);
			
			var si = new StrobeInfo(latched4016, value);
			ControllerDeck.Strobe(si, _controller);
			latched4016 = value;
			new_strobe = (value & 1) > 0;
			if (current_strobe && !new_strobe)
			{
				controller_was_latched = true;
				lagged = false;
				InputCallbacks.Call();
			}
			current_strobe = new_strobe;
		}

		private byte read_joyport(int addr)
		{
			byte ret;
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

		private byte peek_joyport(int addr)
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
					palette_compiled[c] = unchecked((int)0xFF000000 | (r << 16) | (g << 8) | b);
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
					palette_compiled[i] = unchecked((int)0xFF000000 | (r << 16) | (g << 8) | b);
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
			else if (addr < 0x6000)
			{
				//let's ignore pokes to EXP until someone asks for it. there's really almost no way that could ever be done without the board having a PokeEXP method
			}
			else if (addr < 0x8000)
			{
				Board.WriteWram(addr - 0x6000, value);
			}
			else
			{
				// apply a cheat to non-writable memory
				ApplyCheat(addr, value);
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
			if (MemoryCallbacks.HasExecutes)
			{
				uint flags = (uint)(MemoryCallbackFlags.CPUZero | MemoryCallbackFlags.AccessExecute);
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		public byte ReadMemory(ushort addr)
		{
			byte ret;

			if (addr >= 0x8000)
			{
				// easy optimization, since rom reads are so common, move this up (reordering the rest of these else ifs is not easy)
				ret = Board.ReadPrg(addr - 0x8000);

				// handle cheats, currently all cheats are of game genie style only
				if (num_cheats != 0)
				{
					for (int i = 0; i < num_cheats; i++)
					{
						if (cheat_addresses[i] == addr)
						{
							if (cheat_compare_type[i] == 0) 
							{ 
								ret = cheat_value[i]; 
							}
							else if ((cheat_compare_type[i] == 1) && (ret == cheat_compare_val[i]))
							{
								ret = cheat_value[i];
							}					
						}
					}
				}
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
				// oam dma access board memory if cpu is not accessing registers
				// this means that OAM DMA can actually access memory that the cpu cannot
				if (oam_dma_exec)
				{
					if ((cpu.PC >= 0x4000) && (cpu.PC < 0x4020))
					{
						ret = ReadReg(addr);
					}
					else
					{
						ret = Board.ReadExp(addr - 0x4000);
					}
				}
				else
				{
					ret = ReadReg(addr);
				}
			}
			else if (addr < 0x6000)
			{
				// oam dma will access registers if cpu is accessing them
				if (oam_dma_exec && ((oam_dma_addr & 0xFF00) == 0x4000) && (cpu.PC >= 0x4000) && (cpu.PC < 0x4020))
				{
					ret = ReadReg(addr & 0x401F);
				}
				else
				{
					ret = Board.ReadExp(addr - 0x4000);
				}
			}
			else
			{
				ret = Board.ReadWram(addr - 0x6000);
			}

			if (MemoryCallbacks.HasReads)
			{
				uint flags = (uint)(MemoryCallbackFlags.CPUZero | MemoryCallbackFlags.AccessRead);
				MemoryCallbacks.CallMemoryCallbacks(addr, ret, flags, "System Bus");
			}

			DB = ret;
			return ret;
		}

		public void ApplyCheat(int addr, byte value)
		{
			if (addr <= 0xFFFF)
			{
				cheat_addresses[num_cheats] = addr;
				cheat_value[num_cheats] = value;

				// there is no compare here
				cheat_compare_val[num_cheats] = -1;
				cheat_compare_type[num_cheats] = 0;

				if (num_cheats < 0x1000) { num_cheats++; }
			}
		}

		public void ApplyCompareCheat(int addr, byte value, int compare, int comparetype)
		{
			if (addr <= 0xFFFF)
			{
				cheat_addresses[num_cheats] = addr;
				cheat_value[num_cheats] = value;

				cheat_compare_val[num_cheats] = compare;
				cheat_compare_type[num_cheats] = comparetype;

				if (num_cheats < 0x1000) { num_cheats++; }
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
				WriteReg(addr, value);
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

			if (MemoryCallbacks.HasWrites)
			{
				uint flags = (uint)(MemoryCallbackFlags.CPUZero | MemoryCallbackFlags.AccessWrite | MemoryCallbackFlags.SizeByte);
				MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");
			}

			DB = value;
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
