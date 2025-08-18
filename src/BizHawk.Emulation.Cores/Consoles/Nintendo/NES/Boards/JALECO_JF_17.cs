﻿using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//iNES Mapper 72
	//Example Games:
	//--------------------------
	//Pinball Quest (J)
	//Moero!! Pro Tennis
	//Moero!! Juudou Warriors

	//based on the chips on the pcb (3x 4bit registers and some OR gates) i'm gonna speculate something a little different about how this works.
	//there isnt enough memory for 2 bank registers, a latched bank, and a latched command. so i think the bank isnt latched--the command is latched.
	//when the top 2 bits are 0, then the low 4 bits are written to the register specified by the latch
	//when the top 2 bits arent 0, theyre written to the latch
	//interestingly, this works (for pinball quest) only when bus conflicts are applied, otherwise the game cant get past the title

	internal sealed class JALECO_JF_17 : NesBoardBase
	{
		//configuration
		private int prg_bank_mask_16k;
		private int chr_bank_mask_8k;

		//state
		private int latch;
		private byte[] prg_banks_16k = new byte[2];
		private byte[] chr_banks_8k = new byte[1];

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER072":
					break;
				case "JALECO-JF-17":
					break;
				default:
					return false;
			}

			prg_bank_mask_16k = (Cart.PrgSize / 16) - 1;
			chr_bank_mask_8k = (Cart.ChrSize / 8) - 1;

			SetMirrorType(Cart.PadH, Cart.PadV);

			prg_banks_16k[1] = 0xFF;
			chr_banks_8k[0] = 0;
			SyncMap();

			return true;
		}

		private void SyncMap()
		{
			ApplyMemoryMapMask(prg_bank_mask_16k, prg_banks_16k);
			ApplyMemoryMapMask(chr_bank_mask_8k, chr_banks_8k);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(latch), ref latch);
			ser.Sync(nameof(prg_banks_16k), ref prg_banks_16k, false);
			ser.Sync(nameof(chr_banks_8k), ref chr_banks_8k, false);
		}

		public override void WritePrg(int addr, byte value)
		{
			//Console.WriteLine("MAP {0:X4} = {1:X2}", addr, value);

			value = HandleNormalPRGConflict(addr, value);

			int command = value >> 6;
			switch (command)
			{
				case 0:
					if (latch == 1)
						chr_banks_8k[0] = (byte)(value & 0xF);
					else if (latch == 2)
						prg_banks_16k[0] = (byte)(value & 0xF);
					SyncMap();
					break;
				default:
					latch = command;
					break;
			}
		}

		public override byte ReadPrg(int addr)
		{
			addr = ApplyMemoryMap(14, prg_banks_16k, addr);
			return Rom[addr];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				addr = ApplyMemoryMap(13, chr_banks_8k, addr);
				return base.ReadPPUChr(addr);
			}
			else return base.ReadPpu(addr);
		}
	}
}
