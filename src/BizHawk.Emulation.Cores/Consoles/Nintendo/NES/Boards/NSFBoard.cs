using BizHawk.Common;

//NSF ROM and general approaches are heavily derived from FCEUX. the general ideas:
//1. Have a hardcoded NSF driver rom loaded to 0x3800
//2. Have fake registers at $3FFx for the NSF driver to use
//3. These addresses are chosen because no known NSF could possibly use them for anything.
//4. Patch the PRG with our own IRQ vectors when the NSF play and init routines aren't running. 
//   That way we can use NMI for overall control and cause our code to be the NMI handler without breaking the NSF data by corrupting the last few bytes

//NSF:
//check nsfspec.txt for more on why FDS is weird. lets try not following FCEUX too much there.

//TODO - add a sleep mode to the cpu and patch the rom program to use it?
//TODO - some NSF players know when a song ends and skip to the next one.. how do they know?

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[NesBoardImplCancel]
	internal sealed class NSFBoard : NesBoardBase
	{
		//------------------------------
		//configuration
		
		internal NSFFormat nsf;

		/// <summary>
		/// Whether the NSF is bankswitched
		/// </summary>
		private bool BankSwitched;

		/// <summary>
		/// the bankswitch values to be used before the INIT routine is called
		/// </summary>
		private readonly byte[] InitBankSwitches = new byte[8];

		/// <summary>
		/// An image of the entire PRG space where the unmapped files are located
		/// </summary>
		private readonly byte[] FakePRG = new byte[32768];

		//------------------------------
		//state

		/// <summary>
		/// PRG bankswitching
		/// </summary>
		private int[] prg_banks_4k = new int[8];

		/// <summary>
		/// whether vectors are currently patched. they should not be patched when running init/play routines because data from the ends of banks might get used
		/// </summary>
		private bool Patch_Vectors;

		/// <summary>
		/// Current 1-indexed song number (1 is the first song)
		/// </summary>
		private int CurrentSong;

		/// <summary>
		/// Whether the INIT routine needs to be called
		/// </summary>
		private bool InitPending;

		/// <summary>
		/// Previous button state for button press handling
		/// </summary>
		private int ButtonState;

		public override bool Configure(EDetectionOrigin origin)
		{
			Cart.WramSize = 8;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_banks_4k), ref prg_banks_4k, false);
			ser.Sync(nameof(Patch_Vectors), ref Patch_Vectors);
			ser.Sync(nameof(CurrentSong), ref CurrentSong);
			ser.Sync(nameof(InitPending), ref InitPending);
			ser.Sync(nameof(ButtonState), ref ButtonState);
		}

		public void InitNSF(NSFFormat nsf)
		{
			this.nsf = nsf;

			//patch the NSF rom with the init and play addresses
			NSFROM[0x12] = (byte)(nsf.InitAddress);
			NSFROM[0x13] = (byte)(nsf.InitAddress >> 8);
			NSFROM[0x19] = (byte)(nsf.PlayAddress);
			NSFROM[0x1A] = (byte)(nsf.PlayAddress >> 8);

			//analyze bankswitch configuration. fix broken configurations
			BankSwitched = false;
			for (int i = 0; i < 8; i++)
			{
				int bank = nsf.BankswitchInitValues[i];
				
				//discard out of range bankswitches.. for example, Balloon Fight is 3120B but has initial bank settings set to 0,0,0,0,0,1,0
				if (bank * 4096 > nsf.NSFData.Length - 0x80)
					bank = 0; 

				InitBankSwitches[i] = (byte)bank;
				if (bank != 0)
					BankSwitched = true;
			}

			//if bit bankswitched, set up the fake PRG with the NSF data at the correct load address
			if (!BankSwitched)
			{
				//copy to load address
				int load_start = nsf.LoadAddress - 0x8000;
				int load_size = nsf.NSFData.Length - 0x80;
				Buffer.BlockCopy(nsf.NSFData, 0x80, FakePRG, load_start, load_size);
			}

			CurrentSong = nsf.StartingSong;
			ReplayInit();
		}

		private void ReplayInit()
		{
			Console.WriteLine("NSF: Playing track {0}/{1}", CurrentSong, nsf.TotalSongs-1);
			InitPending = true;
			Patch_Vectors = true;
		}

		public override void NesSoftReset()
		{
			ReplayInit();
		}

		public override void WriteExp(int addr, byte value)
		{
			switch (addr)
			{
				case 0x1FF6:
				case 0x1FF7:
					//if (!(NSFHeader.SoundChip & 4)) return; //FDS
					break;
				case 0x1FF8:
				case 0x1FF9:
				case 0x1FFA:
				case 0x1FFB:
				case 0x1FFC:
				case 0x1FFD:
				case 0x1FFE:
				case 0x1FFF:
					if (!BankSwitched) break;
					addr -= 0x1FF8;
					prg_banks_4k[addr] = value;
					break;
			}
		}

		public override byte PeekReg2xxx(int addr)
		{
			if (addr < 0x3FF0)
				return NSFROM[addr - 0x3800];
			else return base.PeekReg2xxx(addr);
		}

		public override byte ReadReg2xxx(int addr)
		{
			if (addr < 0x3800)
				return base.ReadReg2xxx(addr);
			else if (addr >= 0x3FF0)
			{
				switch (addr)
				{
					case 0x3FF0:
						{
							byte ret = 0;
							if (InitPending) ret = 1;
							InitPending = false;
							return ret;
						}
					case 0x3FF1:
						{
							//kevtris's reset process seems not to work. dunno what all is going on in there

							//our own innovation, should work OK.. 
							NES.apu.NESSoftReset();

							//mostly fceux's guidance
							NES.WriteMemory(0x4015, 0);
							for (int i = 0; i < 14; i++)
								NES.WriteMemory((ushort)(0x4000 + i), 0);
							NES.WriteMemory(0x4015, 0x0F);

							//clearing APU misc stuff, maybe not needed with soft reset above
							//NES.WriteMemory(0x4017, 0xC0);
							//NES.WriteMemory(0x4017, 0xC0);
							//NES.WriteMemory(0x4017, 0x40);

							//important to NSF standard for ram to be cleared, otherwise replayers are confused on account of not initializing memory themselves
							var ram = NES.ram;
							var wram = this.Wram;
							int wram_size = wram.Length;
							for (int i = 0; i < 0x800; i++)
								ram[i] = 0;
							for (int i = 0; i < wram_size; i++)
								wram[i] = 0;

							//store specified initial bank state
							if (BankSwitched)
								for (int i = 0; i < 8; i++)
									WriteExp(0x5FF8 + i - 0x4000, InitBankSwitches[i]);

							return (byte)(CurrentSong - 1);
						}
					case 0x3FF2:
						return 0; //always return NTSC for now
					case 0x3FF3:
						Patch_Vectors = false;
						return 0;
					case 0x3FF4:
						Patch_Vectors = true;
						return 0;
					default:
						return base.ReadReg2xxx(addr);
				}
			}
			else if (addr - 0x3800 < NSFROM.Length) return NSFROM[addr - 0x3800];
			else return base.ReadReg2xxx(addr);
		}

		private const ushort NMI_VECTOR = 0x3800;
		private const ushort RESET_VECTOR = 0x3820;

		//readable registers
		//3FF0 - InitPending (cleared on read)
		//3FF1 - NextSong (also performs reset process - clears APU, RAM, etc)
		//3FF2 - PAL flag
		//3FF3 - PatchVectors=false
		//3FF4 - PatchVectors=true

		private readonly byte[] NSFROM = new byte[0x23]
		{
			//@NMIVector
			//Suspend vector patching
			//3800:LDA $3FF3
			0xAD,0xF3,0x3F,
			
			//Initialize stack pointer
			//3803:LDX #$FF
			0xA2,0xFF,
			//3805:TXS
			0x9A,

			//Check (and clear) InitPending flag
			//3806:LDA $3FF0
			0xAD,0xF0,0x3F,
			//3809:BEQ $8014
			0xF0,0x09,

			//Read the next song (resetting the player) and PAL flag into A and X and then call the INIT routine
			//380B:LDA $3FF1 
			0xAD,0xF1,0x3F,
			//380E:LDX $3FF2
			0xAE,0xF2,0x3F,
			//3811:JSR INIT
			0x20,0x00,0x00, 

			//Fall through to:
			//@Play - call PLAY routine with X and Y cleared (this is not supposed to be required, but fceux did it)
			//3814:LDA #$00 
			0xA9,0x00,
			//3816:TAX
			0xAA,
			//3817:TAY
			0xA8,
			//3818:JSR PLAY
			0x20,0x00,0x00, 
			
			//Resume vector patching and infinite loop waiting for next NMI
			//381B:LDA $3FF4
			0xAD,0xF4,0x3F,
			//381E:BCC $XX1E
			0x90,0xFE, 

			//@ResetVector - just set up an infinite loop waiting for the first NMI
			//3820:CLC
			0x18,
			//3821:BCC $XX24 
			0x90,0xFE, 
		};

		public override void AtVsyncNmi()
		{
			if(Patch_Vectors)
				NES.cpu.NMI = true;

			//strobe pad
			NES.WriteMemory(0x4016, 1);
			NES.WriteMemory(0x4016, 0);

			//read pad and create rising edge button signals so we don't trigger events as quickly as we hold the button down
			int currButtons = 0;
			for (int i = 0; i < 8; i++)
			{
				currButtons <<= 1;
				currButtons |= (NES.ReadMemory(0x4016) & 1);
			}
			int justDown = (~ButtonState) & currButtons;
			Bit a = (justDown >> 7) & 1;
			Bit b = (justDown >> 6) & 1;
			Bit sel = (justDown >> 5) & 1;
			Bit start = (justDown >> 4) & 1;
			Bit up = (justDown >> 3) & 1;
			Bit down = (justDown >> 2) & 1;
			Bit left = (justDown >> 1) & 1;
			Bit right = (justDown >> 0) & 1;
			ButtonState = currButtons;

			//RIGHT: next song
			//LEFT: prev song
			//A: restart song

			bool reset = false;
			if (right)
			{
				if (CurrentSong < nsf.TotalSongs - 1)
				{
					CurrentSong++;
					reset = true;
				}
			}
			if (left)
			{
				if (CurrentSong > 0)
				{
					CurrentSong--;
					reset = true;
				}
			}

			if (a)
				reset = true;

			if (reset)
			{
				ReplayInit();
			}
		}

		public override byte ReadPpu(int addr)
		{
			return 0;
		}

		public override byte ReadPrg(int addr)
		{
			//patch in vector reading
			if (Patch_Vectors)
			{
				if (addr == 0x7FFA) return NMI_VECTOR & 0xFF;
				else if (addr == 0x7FFB) return (NMI_VECTOR >> 8) & 0xFF;
				else if (addr == 0x7FFC) return RESET_VECTOR & 0xFF;
				else if (addr == 0x7FFD) { return (RESET_VECTOR >> 8) & 0xFF; }
				return NES.DB;
			}
			else
			{
				if (BankSwitched)
				{
					int bank_4k = addr >> 12;
					int ofs = addr & ((1 << 12) - 1);
					bank_4k = prg_banks_4k[bank_4k];
					addr = (bank_4k << 12) | ofs;

					//rom data began at 0x80 of the NSF file
					addr += 0x80;

					return Rom[addr];
				}
				else
				{
					return FakePRG[addr];
				}
			}
		}
	}
}
