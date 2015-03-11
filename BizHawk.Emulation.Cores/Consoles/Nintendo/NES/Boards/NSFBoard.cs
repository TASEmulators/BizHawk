
using System;
using BizHawk.Common;

//NSF ROM and general approaches are taken from FCEUX. however, i've improvised/simplified/broken things so the rom is doing some pointless stuff now.

//check nsfspec.txt for more on why FDS is weird. lets try not following FCEUX too much there.

//TODO - add a sleep mode to the cpu and patch the rom program to use it?
//some NSF players know when a song ends.

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[NES.INESBoardImplCancel]
	public sealed class NSFBoard : NES.NESBoardBase
	{
		//configuration
		internal NSFFormat nsf;
		byte[] InitBankSwitches = new byte[8];
		byte[] FakePRG = new byte[32768];
		bool BankSwitched;

		//------------------------------
		//state
		IntBuffer prg_banks_4k = new IntBuffer(8);

		/// <summary>
		/// whether vectors are currently patched. they should not be patched when running init/play routines because data from the ends of banks might get used
		/// </summary>
		bool Patch_Vectors;

		int CurrentSong;
		bool ResetSignal;
		int ButtonState;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			Cart.wram_size = 8;

			return true;
		}

		public override void Dispose()
		{
			prg_banks_4k.Dispose();
			base.Dispose();
		}

		public void InitNSF(NSFFormat nsf)
		{
			this.nsf = nsf;

			//patch the NSF rom with the init and play addresses
			NSFROM[0x12] = (byte)(nsf.InitAddress);
			NSFROM[0x13] = (byte)(nsf.InitAddress >> 8);
			NSFROM[0x19] = (byte)(nsf.PlayAddress);
			NSFROM[0x1A] = (byte)(nsf.PlayAddress >> 8);

			//complicated anlysis straight from FCEUX
			//apparently, it converts a non-bankswitched configuration into a bankswitched configuration
			//since the non-bankswitched configuration is seemingly almost pointless. 
			//I'm not too sure how we would really get a non-bankswitched file, using the code below. 
			//It would need to be loaded below 0x8000 I think?
			BankSwitched = false;
			for (int i = 0; i < 8; i++)
			{
				InitBankSwitches[i] = nsf.BankswitchInitValues[i];
				if (InitBankSwitches[i] != 0)
					BankSwitched = true;
			}

			if (!BankSwitched)
			{
				if ((nsf.LoadAddress & 0x7000) >= 0x7000)
				{
					//"Ice Climber, and other F000 base address tunes need this"
					BankSwitched = true;
				}
				else
				{
					byte bankCounter = 0;
					for (int x = (nsf.LoadAddress >> 12) & 0x7; x < 8; x++)
					{
						InitBankSwitches[x] = bankCounter;
						bankCounter++;
					}
					BankSwitched = false;
				}
			}

			for (int i = 0; i < 8; i++)
				if (InitBankSwitches[i] != 0)
					BankSwitched = true;

			if (!BankSwitched)
			{
				throw new Exception("Test");
				//setup FakePRG by copying in
			}

			ReplayInit();
			CurrentSong = nsf.StartingSong;
		}

		void ReplayInit()
		{
			ResetSignal = true;
			Patch_Vectors = true;
		}

		public override void NESSoftReset()
		{
			ReplayInit();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
		}

		public override void WriteEXP(int addr, byte value)
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


		public override void WriteReg2xxx(int addr, byte value)
		{
			switch (addr)
			{
				case 0x3FF3: Patch_Vectors = true; break;
				case 0x3FF4: Patch_Vectors = false; break;
				case 0x3FF5: Patch_Vectors = true; break;
				default:
					base.WriteReg2xxx(addr, value);
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
				if (addr == 0x3FF0)
				{
					byte ret = 0;
					if (ResetSignal) ret = 1;
					ResetSignal = false;
					return ret;
				}
				else if (addr == 0x3FF1)
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
					var wram = this.WRAM;
					int wram_size = wram.Length;
					for (int i = 0; i < 0x800; i++)
						ram[i] = 0;
					for (int i = 0; i < wram_size; i++)
						wram[i] = 0;

					//store specified initial bank state
					if (BankSwitched)
						for (int i = 0; i < 8; i++)
							WriteEXP(0x5FF8 + i - 0x4000, InitBankSwitches[i]);

					return (byte)(CurrentSong - 1);
				}
				else if (addr == 0x3FF3) return 0; //always return NTSC I guess
				else return base.ReadReg2xxx(addr);
			}
			else if (addr - 0x3800 < NSFROM.Length) return NSFROM[addr - 0x3800];
			else return base.ReadReg2xxx(addr);
		}

		//; @NMIVector
		//00:XX00:8D F4 3F  STA $3FF4 = #$00 ; clear NMI_2 (the value of A is unimportant)
		//00:XX03:A2 FF     LDX #$FF
		//00:XX05:9A        TXS ; stack pointer is initialized
		//00:XX06:AD F0 3F  LDA $3FF0 = #$00 ; read a flag that says whether we need to run init
		//00:XX09:F0 09     BEQ $8014 ; If we dont need init, go to @PastInit
		//00:XX0B:AD F1 3F  LDA $3FF1 = #$00 ; reading this value causes a reset
		//00:XX0E:AE F3 3F  LDX $3FF3 = #$00 ; reads the PAL flag
		//00:XX11:20 00 00  JSR $0000 ; JSR to INIT routine
		//; @PastInit
		//00:XX14:A9 00     LDA #$00 
		//00:XX16:AA        TAX
		//00:XX17:A8        TAY ; X and Y are cleared
		//00:XX18:20 00 00  JSR $0000 ; JSR to PLAY routine
		//00:XX1B:8D F5 3F  STA $3FF5 = #$FF ; set NMI_2 flag
		//00:XX1E:90 FE     BCC $XX1E ; infinite loop.. when the song is over?
		//; @ResetVector
		//00:XX20:8D F3 3F  STA $3FF3 = #$00 ; set NMI_1 flag (the value of A is unimportant); since the rom boots here, this was needed for the initial NMI. but we also get it from having the reset signal set, so..
		//00:XX23:18        CLC
		//00:XX24:90 FE     BCC $XX24 ;infinite loop to wait for first NMI

		const ushort NMI_VECTOR = 0x3800;
		const ushort RESET_VECTOR = 0x3820;

		//for reasons unknown, this is a little extra long
		byte[] NSFROM = new byte[0x30 + 6]
		{
			//0x00 - NMI
			0x8D,0xF4,0x3F, //Stop play routine NMIs.
			0xA2,0xFF,0x9A, //Initialize the stack pointer. 
			0xAD,0xF0,0x3F, //See if we need to init. 
			0xF0,0x09, //If 0, go to play routine playing. 

			0xAD,0xF1,0x3F, //Confirm and load A      
			0xAE,0xF3,0x3F, //Load X with PAL/NTSC byte 

			//0x11
			0x20,0x00,0x00, //JSR to init routine (WILL BE PATCHED)

			0xA9,0x00,
			0xAA,
			0xA8,

			//0x18
			0x20,0x00,0x00, //JSR to play routine (WILL BE PATCHED)
			0x8D,0xF5,0x3F, //Start play routine NMIs. 
			0x90,0xFE, //Loopie time. 

			// 0x20
			0x8D,0xF3,0x3F, //Init init NMIs 
			0x18,
			0x90,0xFE, //Loopie time. 

			//0x26
			0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
			//0x30
			0x00,0x00,0x00,0x00,0x00,0x00
		};

		public override void AtVsyncNMI()
		{
			if(Patch_Vectors)
				NES.cpu.NMI = true;

			//strobe pad
			NES.WriteMemory(0x4016, 1);
			NES.WriteMemory(0x4016, 0);

			//read pad and create rising edge button signals so we dont trigger events as quickly as we hold the button down
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
				CurrentSong++;
				reset = true;
			}
			if (left)
			{
				CurrentSong--;
				reset = true;
			}

			if (a)
				reset = true;

			if (reset)
			{
				ReplayInit();
			}
		}

		public override byte ReadPPU(int addr)
		{
			return 0;
		}

		public override byte ReadWRAM(int addr)
		{
			return base.ReadWRAM(addr);
		}

		public override byte ReadPRG(int addr)
		{
			//patch in vector reading
			if (Patch_Vectors)
			{
				if (addr == 0x7FFA) return (byte)(NMI_VECTOR & 0xFF);
				else if (addr == 0x7FFB) return (byte)((NMI_VECTOR >> 8) & 0xFF);
				else if (addr == 0x7FFC) return (byte)(RESET_VECTOR & 0xFF);
				else if (addr == 0x7FFD) { return (byte)((RESET_VECTOR >> 8) & 0xFF); }
				return NES.DB;
			}
			else
			{
				int bank_4k = addr >> 12;
				int ofs = addr & ((1 << 12) - 1);
				bank_4k = prg_banks_4k[bank_4k];
				addr = (bank_4k << 12) | ofs;

				if (BankSwitched)
				{
					//rom data began at 0x80 of the NSF file
					addr += 0x80;

					return ROM[addr];
				}
				else return NES.DB;
			}
		}
	}
}
