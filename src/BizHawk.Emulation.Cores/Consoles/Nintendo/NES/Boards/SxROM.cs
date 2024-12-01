using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//AKA MMC1
	//http://wiki.nesdev.com/w/index.php/SxROM

	//consult nestopia as well.
	//the initial conditions for MMC1 games are known to be different. this may have to do with which MMC1 rev it is.
	//but how do we know which revision a game is? i don't know which revision is on which board
	//check UNIF for more information.. it may specify board and MMC1 rev independently because boards may have any MMC1 rev
	//in that case, we need to capture MMC1 rev in the game database (maybe add a new `chip` parameter)

	//TODO - this could be refactored to use more recent techniques (bank regs instead of nested if/then)

	//Final Fantasy
	//Mega Man 2
	//Blaster Master
	//Metroid
	//Kid Icarus
	//Zelda
	//Zelda 2
	//Castlevania 2

	// TODO -- different MMC1 revisions handle wram_disable differently; on some it doesn't work at all; on others,
	//         it works, but with different initial states possible.  we only emulate the first case

	internal sealed class MMC1
	{
		public MMC1_SerialController scnt = new MMC1_SerialController();

		public MMC1()
		{
			scnt.WriteRegister = SerialWriteRegister;
			scnt.Reset = SerialReset;

			//collect data about whether this is required here:
			//kid icarus requires it
			//zelda doesnt; nor megaman2; nor blastermaster; nor metroid
			StandardReset();
			//well, lets leave it.

			SyncCHR();
		}

		public void SyncState(Serializer ser)
		{
			scnt.SyncState(ser);
			ser.Sync(nameof(chr_mode), ref chr_mode);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			ser.Sync(nameof(prg_slot), ref prg_slot);
			ser.Sync(nameof(chr_0), ref chr_0);
			ser.Sync(nameof(chr_1), ref chr_1);
			ser.Sync(nameof(wram_disable), ref wram_disable);
			ser.Sync(nameof(prg), ref prg);
			ser.SyncEnum(nameof(mirror), ref mirror);

			SyncCHR();
			SyncPRG();
		}

		public enum Rev
		{
			A, B1, B2, B3
		}

		//register 0:
		public int chr_mode;
		public int prg_mode;
		public int prg_slot; // complicated
		public EMirrorType mirror;

		private static readonly EMirrorType[] _mirrorTypes =
		{
			EMirrorType.OneScreenA,
			EMirrorType.OneScreenB,
			EMirrorType.Vertical,
			EMirrorType.Horizontal
		};

		//register 1,2:
		private int chr_0, chr_1;

		//register 3:
		public bool wram_disable;
		private int prg;

		//regenerable state
		private readonly int[] chr_banks_4k = new int[2];
		private readonly int[] prg_banks_16k = new int[2];

		public class MMC1_SerialController
		{
			//state
			private int shift_count, shift_val;

			public void SyncState(Serializer ser)
			{
				ser.Sync(nameof(shift_count), ref shift_count);
				ser.Sync(nameof(shift_val), ref shift_val);
			}

			public Action Reset;
			public Action<int, int> WriteRegister;

			public void ResetShift()
			{
				shift_count = shift_val = 0;
			}

			public void Write(int addr, byte value)
			{
				int data = value & 1;
				int reset = (value >> 7) & 1;
				if (reset == 1)
				{
					shift_count = 0;
					shift_val = 0;
					Reset?.Invoke();
				}
				else
				{
					shift_val >>= 1;
					shift_val |= (data << 4);
					shift_count++;
					if (shift_count == 5)
					{
						WriteRegister(addr >> 13, shift_val);
						shift_count = 0;
						shift_val = 0;
					}
				}
			}
		}

		private void SerialReset()
		{
			prg_mode = 1;
			prg_slot = 1;
		}

		public void StandardReset()
		{
			prg_mode = 1;
			prg_slot = 1;
			chr_mode = 1;
			scnt.Reset();
			mirror = EMirrorType.Horizontal;
			SyncCHR();
			SyncPRG();
		}

		public void Write(int addr, byte value)
		{
			scnt.Write(addr, value);
			SyncCHR();
			SyncPRG();
		}

		//logical register writes, called from the serial controller
		public void SerialWriteRegister(int addr, int value)
		{
			switch (addr)
			{
				case 0: //8000-9FFF
					mirror = _mirrorTypes[value & 3];
					prg_slot = ((value >> 2) & 1);
					prg_mode = ((value >> 3) & 1);
					chr_mode = ((value >> 4) & 1);
					break;
				case 1: //A000-BFFF
					chr_0 = value & 0x1F;
					break;
				case 2: //C000-DFFF
					chr_1 = value & 0x1F;
					break;
				case 3: //E000-FFFF
					prg = value & 0xF;
					wram_disable = value.Bit(4);
					break;
			}
			//board.NES.LogLine("mapping.. chr_mode={0}, chr={1},{2}", chr_mode, chr_0, chr_1);
			//board.NES.LogLine("mapping.. prg_mode={0}, prg_slot{1}, prg={2}", prg_mode, prg_slot, prg);
		}

		private void SyncCHR()
		{
			if (chr_mode == 0)
			{
				chr_banks_4k[0] = chr_0 & ~1;
				chr_banks_4k[1] = (chr_0 & ~1)+1;
			}
			else
			{
				chr_banks_4k[0] = chr_0;
				chr_banks_4k[1] = chr_1;
			}
		}

		private void SyncPRG()
		{
			if (prg_mode == 0)
			{
				//switch 32kb
				prg_banks_16k[0] = prg & ~1;
				prg_banks_16k[1] = (prg & ~1) + 1;
			}
			else
			{
				//switch 16KB at...
				if (prg_slot == 0)
				{
					//...$C000:
					prg_banks_16k[0] = 0x00;
					prg_banks_16k[1] = prg;
				}
				else
				{
					//...$8000:
					prg_banks_16k[0] = prg;
					prg_banks_16k[1] = 0x0F;
				}
			}
		}

		public int Get_PRGBank(int addr)
		{
			return prg_banks_16k[addr >> 14];
		}

		public int Get_CHRBank_4K(int addr)
		{
			return chr_banks_4k[addr >> 12];
		}
	}

	[NesBoardImplPriority]
	internal class SxROM : NesBoardBase
	{
		//configuration
		protected int prg_mask, chr_mask;
		protected int vram_mask;
		private const int pputimeout = 4; // i don't know if this is right, but anything lower will not boot Bill & Ted
		private bool disablemirror = false; // mapper 171: mmc1 without mirroring control


		//the VS actually does have 2 KB of nametable address space
		//let's make the extra space here, instead of in the main NES to avoid confusion
		private byte[] CIRAM_VS = new byte[0x800];

		//for snrom wram disable
		public bool _is_snrom;
		public bool chr_wram_enable = true;

		//state
		public MMC1 mmc1;
		/// <summary>number of cycles since last WritePRG()</summary>
		private uint ppuclock;

		public override void ClockPpu()
		{
			if (ppuclock < pputimeout)
				ppuclock++;
		}

		public override void WritePrg(int addr, byte value)
		{
			// mmc1 ignores subsequent writes that are very close together
			if (ppuclock >= pputimeout)
			{
				ppuclock = 0;
				mmc1.Write(addr, value);
				if (!disablemirror)
					SetMirrorType(mmc1.mirror); //often redundant, but gets the job done
			}
		}

		public override byte ReadWram(int addr)
		{
			if (_is_snrom)
			{
				if (!mmc1.wram_disable && chr_wram_enable)
					return base.ReadWram(addr);	
				else
					return NES.DB;	
			}
			else
			{
				return base.ReadWram(addr);
			}	
		}

		public override byte ReadPrg(int addr)
		{
			int bank = mmc1.Get_PRGBank(addr) & prg_mask;
			addr = (bank << 14) | (addr & 0x3FFF);
			return Rom[addr];
		}

		private int Gen_CHR_Address(int addr)
		{
			int bank = mmc1.Get_CHRBank_4K(addr);
			addr = ((bank & chr_mask) << 12) | (addr & 0x0FFF);
			return addr;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if (_is_snrom)
				{
					// WRAM enable is tied to ppu a12
					if (Gen_CHR_Address(addr).Bit(16))
						chr_wram_enable = false;
					else
						chr_wram_enable = true;
				}

				addr = Gen_CHR_Address(addr);

				if (Cart.VramSize != 0)
					return Vram[addr & vram_mask];
				else return Vrom[addr];
			}
			else
			{
				if (NES._isVS)
				{
					addr -= 0x2000;
					if (addr < 0x800)
					{
						return NES.CIRAM[addr];
					}
					else
					{
						return CIRAM_VS[addr - 0x800];
					}
				}
				else
					return base.ReadPpu(addr);
					
			}
		}

		public override void WritePpu(int addr, byte value)
		{
			if (NES._isVS)
			{
				if (addr < 0x2000)
				{
					if (Vram != null)
						Vram[Gen_CHR_Address(addr) & vram_mask] = value;
				}
				else
				{
					addr -= 0x2000;
					if (addr < 0x800)
					{
						NES.CIRAM[addr] = value;
					}
					else
					{
						CIRAM_VS[addr - 0x800] = value;
					}
				}
			}
			else if (addr < 0x2000)
			{
				if (_is_snrom)
				{
					// WRAM enable is tied to ppu a12
					if (Gen_CHR_Address(addr).Bit(16))
						chr_wram_enable = false;
					else
						chr_wram_enable = true;
				}

				if (Cart.VramSize != 0)
					Vram[Gen_CHR_Address(addr) & vram_mask] = value;
			}
			else base.WritePpu(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			mmc1.SyncState(ser);
			ser.Sync(nameof(ppuclock), ref ppuclock);
			ser.Sync(nameof(chr_wram_enable), ref chr_wram_enable);
			if (NES._isVS)
				ser.Sync("VS_CIRAM", ref CIRAM_VS, false);
		}
	
		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER116_HACKY":
					break;
				case "MAPPER001_VS":
					// VS mapper MMC1
					NES._isVS = true;
					//update the state of the dip switches
					//this is only done at power on
					NES.VS_dips[0] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_1 ? 1 : 0);
					NES.VS_dips[1] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_2 ? 1 : 0);
					NES.VS_dips[2] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_3 ? 1 : 0);
					NES.VS_dips[3] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_4 ? 1 : 0);
					NES.VS_dips[4] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_5 ? 1 : 0);
					NES.VS_dips[5] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_6 ? 1 : 0);
					NES.VS_dips[6] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_7 ? 1 : 0);
					NES.VS_dips[7] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_8 ? 1 : 0);
					break;
				case "MAPPER001":
				case "NES-SIEPROM":
					// there's no way to define PRG oversize for mapper001 due to how the MMC1 regs work
					// so 512KB must mean SUROM or SXROM.  SUROM is more common, so we try that
					if (Cart.PrgSize > 256)
						return false;
					break;
				case "MAPPER171": // Tui Do Woo Ma Jeung
					AssertPrg(32); AssertChr(32); Cart.WramSize = 0;
					disablemirror = true;
					SetMirrorType(Cart.PadH, Cart.PadV);
					break;
				case "NES-SAROM": //dragon warrior
					AssertPrg(64); AssertChr(16, 32, 64); AssertVram(0); AssertWram(8); 
					break;
				case "NES-SBROM": //dance aerobics
					AssertPrg(64); AssertChr(16, 32, 64);  AssertVram(0); AssertWram(0);
					break;
				case "NES-SCROM": //mechanized attack
				case "NES-SC1ROM": //knight rider
					AssertPrg(64); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				case "NES-SEROM": //lolo
				case "HVC-SEROM": //dr. mario
					AssertPrg(32); AssertChr(16,32); AssertVram(0); AssertWram(0);
					break;
				case "NES-SFROM": //bubble bobble
				case "HVC-SFROM":
				case "NES-SF1ROM":
					AssertPrg(128, 256); AssertChr(16, 32, 64); AssertVram(0); AssertWram(0);
					break;
				case "NES-SGROM": //bionic commando
				case "HVC-SGROM": //Ankoku Shinwa - Yamato Takeru Densetsu (J)
					AssertPrg(128, 256); AssertChr(0); AssertVram(8); AssertWram(0);
					break;
				case "NES-SHROM": //family feud
				case "NES-SH1ROM": //airwolf
					AssertPrg(32); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				case "HVC-SIROM":
					// NTF2 System Cart (U)
					// bootod classifies Igo: Kyuu Roban Taikyoku as HVC-SIROM-02 with 16kb Chr online, but has this board name in the xml
					AssertPrg(32); AssertChr(16, 64); AssertVram(0); AssertWram(8);
					break;
				case "HVC-SJROM": //zombie hunter (wram is missing), artelius.
					AssertPrg(128); AssertChr(32); AssertVram(0); AssertWram(0, 8);
					break;
				case "NES-SJROM": //air fortress
					AssertPrg(128, 256); AssertChr(16, 32, 64); AssertVram(0); AssertWram(8);
					break;
				case "NES-SKROM": //zelda 2
				case "HVC-SKROM": //ad&d dragons of flame (J)
					AssertPrg(128, 256); AssertChr(128); AssertVram(0); AssertWram(8);
					break;
				case "HVC-SKEPROM":
				case "NES-SKEPROM": // chip n dale 2 (proto)
					AssertPrg(128, 256); AssertChr(128); AssertVram(0); AssertWram(0, 8);
					break;
				case "NES-SLROM": //castlevania 2
				case "KONAMI-SLROM": //bayou billy
				case "HVC-SLROM": //Adventures of Lolo 2 (J)
					AssertPrg(128, 256); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				case "HVC-SL1ROM": // untested
				case "NES-SL1ROM": //hoops
					AssertPrg(64, 128, 256); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				case "NES-SL2ROM": //blaster master
					AssertPrg(128); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				case "NES-SL3ROM": //goal!
					AssertPrg(256); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				case "NES-SLRROM": //tecmo bowl
				case "HVC-SLRROM":
					AssertPrg(128); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				case "HVC-SMROM": //Hokkaidou Rensa Satsujin: Okhotsu ni Shoyu  
					AssertPrg(256); AssertChr(0); AssertVram(8); AssertWram(0);
					break;
				case "HVC-SNROM": // Morita Kazuo no Shougi (J)
					AssertPrg(128, 256); AssertChr(0, 8); AssertVram(0, 8); AssertWram(8);
					break;
				case "HVC-SNROM-03": // Dragon Quest III
					AssertPrg(128, 256); AssertChr(0); AssertVram(8); AssertWram(8);
					break;
				case "NES-SNROM": //dragon warrior 2
					_is_snrom = true;
					AssertPrg(128, 256); AssertChr(0); AssertVram(8); AssertWram(8);
					break;
				case "VIRGIN-SNROM":
				case "NES-SNWEPROM": // final fantasy 2 (proto)
					AssertPrg(128, 256); AssertChr(0); AssertVram(8); AssertWram(8);
					break;
				case "SxROM-JUNK":
					break;
				default:
					return false;
			}

			BaseConfigure();

			return true;
		}

		protected void BaseConfigure()
		{
			mmc1 = new MMC1();
			prg_mask = (Cart.PrgSize / 16) - 1;
			vram_mask = (Cart.VramSize*1024) - 1;
			chr_mask = (Cart.ChrSize / 8 * 2) - 1;
			//Chip n Dale (PC10) has a nonstandard chr size, which makes the mask nonsense
			// let's put in a special case to deal with it
			if (Cart.ChrSize==136)
			{
				chr_mask = (128 / 8 * 2) - 1;
			}


			if (!disablemirror)
				SetMirrorType(mmc1.mirror);
			ppuclock = pputimeout;
		}
	} //class SxROM

	internal sealed class SoROM : SuROM
	{
		//this uses a CHR bit to select WRAM banks
		//TODO - only the latter 8KB is supposed to be battery backed
		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "NES-SOROM": //Nobunaga's Ambition
				case "HVC-SOROM": // KHAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAN
					AssertPrg(128, 256); AssertChr(0); AssertVram(8); AssertWram(16);
					break;
				default: return false;
			}

			BaseConfigure();
			return true;
		}

		private int Map_WRAM(int addr)
		{
			//$A000-BFFF:  [...R ...C]
			//  R = PRG-RAM page select
			//  C = CHR reg 0
			//* BUT THIS IS WRONG ??? R IS ONE BIT LOWER !!!??? ?!? *
			int chr_bank = mmc1.Get_CHRBank_4K(0);
			int ofs = addr & ((8 * 1024) - 1);
			int wram_bank_8k = (chr_bank >> 3) & 1;
			return (wram_bank_8k << 13) | ofs;
		}

		public override void WriteWram(int addr, byte value)
		{
			base.WriteWram(Map_WRAM(addr), value);
		}

		public override byte ReadWram(int addr)
		{
			return base.ReadWram(Map_WRAM(addr));
		}
	}

	internal class SXROM : SuROM
	{
		//SXROM's PRG behaves similar to SuROM (and so inherits from it)
		//it also has some WRAM select bits like SoROM
		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER0001-00":
					if (Cart.WramSize < 32)
						return false;
					AssertPrg(128, 256, 512); AssertChr(0); AssertVram(8); AssertWram(32);
					break;
				case "HVC-SXROM": //final fantasy 1& 2
					AssertPrg(128, 256, 512); AssertChr(0); AssertVram(8); AssertWram(32);
					break;
				default: return false;
			}

			BaseConfigure();
			return true;
		}

		private int Map_WRAM(int addr)
		{
			//$A000-BFFF:  [...P RR..]
			//  P = PRG-ROM 256k block select (just like on SUROM)
			//  R = PRG-RAM page select (selects 8k @ $6000-7FFF, just like SOROM)
			int chr_bank = mmc1.Get_CHRBank_4K(0);
			int ofs = addr & ((8 * 1024) - 1);
			int wram_bank_8k = (chr_bank >> 2) & 3;
			return (wram_bank_8k << 13) | ofs;
		}

		public override void WriteWram(int addr, byte value)
		{
			base.WriteWram(Map_WRAM(addr), value);
		}

		public override byte ReadWram(int addr)
		{
			return base.ReadWram(Map_WRAM(addr));
		}
	}

	internal class SuROM : SxROM
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			//SUROM uses CHR A16 to control the upper address line (PRG A18) of its 512KB PRG ROM.

			switch (Cart.BoardType)
			{
				case "MAPPER001":
					// we try to heuristic match to iNES 001 roms with big PRG only
					if (Cart.PrgSize <= 256)
						return false;
					AssertPrg(512); AssertChr(0);
					Cart.VramSize = 8;
					Cart.WramSize = 8;
					Cart.WramBattery = true; // all SUROM boards had batteries
					Console.WriteLine("Guessing SUROM for 512KiB PRG ROM");
					break;
				case "NES-SUROM": //dragon warrior 4
				case "HVC-SUROM":
					AssertPrg(512); AssertChr(0); AssertVram(8); AssertWram(8);
					break;
				default:
					return false;
			}

			BaseConfigure();
			return true;
		}

		public override byte ReadPrg(int addr)
		{
			int bank = mmc1.Get_PRGBank(addr);
			int chr_bank = mmc1.Get_CHRBank_4K(0);
			int bank_bit18 = chr_bank >> 4;
			bank |= (bank_bit18 << 4);
			bank &= prg_mask;
			addr = (bank << 14) | (addr & 0x3FFF);
			return Rom[addr];
		}
	}
}
