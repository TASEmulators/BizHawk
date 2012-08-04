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
		int vram_byte_mask;
		int prg_mask;

		//state
		int prg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER007":
					bus_conflict = false;
					break;

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
					if (Cart.pcb == "NES-AOROM-03") //Battle toads = 03, and needs bus_conflict.  Wheel of Fortune is 01 and does not.  Thus assuming 03 always needs it and 01 and 02 do not.  Until there are test cases that suggest otherwise
					{
						bus_conflict = true; //MAYBE. apparently it varies
					}
					break;

				default:
					return false;
			}

			prg_mask = (Cart.prg_size / 16) - 1;
			vram_byte_mask = 8 * 1024 - 1; //these boards always have 8KB of VRAM

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
			int prg_bank = value & 7;
			prg = (prg_bank * 2) & prg_mask;
			if ((value & 0x10) == 0)
				SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenA);
			else
				SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenB);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VRAM[addr & vram_byte_mask];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				VRAM[addr & vram_byte_mask] = value;
			}
			else base.WritePPU(addr,value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg",ref prg);
		}

	}
}