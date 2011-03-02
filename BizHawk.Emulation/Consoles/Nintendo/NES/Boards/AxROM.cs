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
		string type;
		bool bus_conflict;
		int cram_mask;
		int prg_mask;

		//state
		byte[] cram;
		int prg;

		public AxROM(string type)
		{
			this.type = type;
			switch (type)
			{
				case "ANROM": bus_conflict = false; break;
				case "AOROM": bus_conflict = true; break;
			}
		}
		public override void Initialize(NES.RomInfo romInfo, NES nes)
		{
			base.Initialize(romInfo, nes);

			Debug.Assert(RomInfo.PRG_Size == 8 || RomInfo.PRG_Size == 16);
			Debug.Assert(RomInfo.CRAM_Size == -1, "don't specify in gamedb, it is redundant");


			if (type == "ANROM")
			{
				Debug.Assert(RomInfo.PRG_Size == 8);
				prg_mask = 7;
			}
			if (type == "AOROM")
			{
				Debug.Assert(RomInfo.PRG_Size == 8 || RomInfo.PRG_Size == 16);
				prg_mask = 15;
			}

			//regardless of what the board is equipped to handle, reduce the mask to how much ROM is actually present
			int rom_prg_mask = (RomInfo.PRG_Size - 1);
			if (rom_prg_mask < prg_mask) prg_mask = rom_prg_mask;

			//these boards always have 8KB of CRAM
			RomInfo.CRAM_Size = 8;
			cram = new byte[RomInfo.CRAM_Size * 1024];
			cram_mask = cram.Length - 1;

			//it is necessary to write during initialization to set the mirroring
			WritePRG(0, 0);
		}

		public override byte ReadPRG(int addr)
		{
			return RomInfo.ROM[addr + (prg << 14)];
		}

		public override void WritePRG(int addr, byte value)
		{
			if (bus_conflict) value = HandleNormalPRGConflict(addr,value);
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
				return cram[addr & cram_mask];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				cram[addr & cram_mask] = value;
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