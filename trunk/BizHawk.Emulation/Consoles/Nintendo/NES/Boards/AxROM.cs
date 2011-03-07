using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	//generally mapper7

	//Battletoads
	//Time Lord
	//Marble Madness

	public class AxROM : NES.NESBoardBase
	{
		//configuration
		bool bus_conflict;
		int cram_byte_mask;
		int prg_mask;

		//state
		byte[] cram;
		int prg;

		public override bool Configure(NES.BootGodDB.Cart cart)
		{
			//configure
			switch (cart.board_type)
			{
				case "NES-ANROM":
					BoardInfo.PRG_Size = 128;
					bus_conflict = false;
					break;

				case "NES-AN1ROM":
					BoardInfo.PRG_Size = 64;
					bus_conflict = false;
					break;

				case "NES-AMROM":
					BoardInfo.PRG_Size = 128;
					bus_conflict = true;
					break;
			
				case "NES-AOROM":
				case "HVC-AOROM":
					Assert(cart.prg_size == 128 || cart.prg_size == 256);
					BoardInfo.PRG_Size = cart.prg_size;
					bus_conflict = true; //MAYBE. apparently it varies
					break;

				default:
					return false;
			}

			//these boards always have 8KB of CRAM
			BoardInfo.CRAM_Size = 8;
			cram = new byte[BoardInfo.CRAM_Size * 1024];
			cram_byte_mask = cram.Length - 1;

			prg_mask = (BoardInfo.PRG_Size / 16) - 1;

			//validate
			Assert(cart.prg_size == BoardInfo.PRG_Size);

			//it is necessary to write during initialization to set the mirroring
			WritePRG(0, 0);

			return true;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr + (prg << 14)];
		}

		public override void WritePRG(int addr, byte value)
		{
			if (ROM != null && bus_conflict) value = HandleNormalPRGConflict(addr,value);
			prg = (value*2) & prg_mask;
			if ((value & 0x10) == 0)
				SetMirrorType(NES.EMirrorType.OneScreenA);
			else
				SetMirrorType(NES.EMirrorType.OneScreenB);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return cram[addr & cram_byte_mask];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				cram[addr & cram_byte_mask] = value;
			}
			else base.WritePPU(addr,value);
		}

		public override void SaveStateBinary(BinaryWriter bw)
		{
			base.SaveStateBinary(bw);
			bw.Write(prg);
			Util.WriteByteBuffer(bw, cram);
		}

		public override void LoadStateBinary(BinaryReader br)
		{
			base.LoadStateBinary(br);
			prg = br.ReadInt32();
			cram = Util.ReadByteBuffer(br, false);
		}

	}
}