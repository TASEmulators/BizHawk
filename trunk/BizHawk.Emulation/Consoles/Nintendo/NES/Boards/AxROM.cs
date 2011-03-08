using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//generally mapper7

	public class AxROM : NES.NESBoardBase
	{
		//configuration
		bool bus_conflict;
		int cram_byte_mask;
		int prg_mask;

		//state
		int prg;

		public override bool Configure()
		{
			//configure
			switch (Cart.board_type)
			{
				case "NES-ANROM": //marble madness
					AssertPrg(128); AssertChr(0); AssertVram(8); AssertWram(0); 
					bus_conflict = false;
					break;

				case "NES-AN1ROM": //R.C. Pro-Am
					AssertPrg(64); AssertChr(0); AssertVram(8); AssertWram(0); 
				    bus_conflict = false;
				    break;

				case "NES-AMROM": //time lord
					AssertPrg(128); AssertChr(0); AssertVram(8); AssertWram(0); 
				    bus_conflict = true;
				    break;
			
				case "NES-AOROM": //battletoads
				case "HVC-AOROM":
					AssertPrg(128,256); AssertChr(0); AssertVram(8); AssertWram(0); 
				    bus_conflict = true; //MAYBE. apparently it varies
				    break;

				default:
					return false;
			}

			prg_mask = (Cart.prg_size / 16) - 1;
			cram_byte_mask = 8 * 1024 - 1; //these boards always have 8KB of CRAM

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
				return VRAM[addr & cram_byte_mask];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				VRAM[addr & cram_byte_mask] = value;
			}
			else base.WritePPU(addr,value);
		}

		public override void SaveStateBinary(BinaryWriter bw)
		{
			base.SaveStateBinary(bw);
			bw.Write(prg);
		}

		public override void LoadStateBinary(BinaryReader br)
		{
			base.LoadStateBinary(br);
			prg = br.ReadInt32();
		}

	}
}