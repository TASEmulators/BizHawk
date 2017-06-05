using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//generally mapper7

	[NES.INESBoardImplPriority]
	public sealed class AxROM : NES.NESBoardBase
	{
		//configuration
		bool bus_conflict;
		int prg_mask_32k;

		//state
		int prg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER007":
					bus_conflict = false;
					Cart.vram_size = 8;
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
				case "HVC-AMROM":
					//http://forums.nesdev.com/viewtopic.php?p=95438
					//adding 723 cycles to FrameAdvance_ppudead() does indeed fix it
					AssertPrg(128); AssertChr(0); AssertVram(8); AssertWram(0);
					bus_conflict = false;
					break;

				case "NES-AOROM": //battletoads
				case "HVC-AOROM":
					AssertPrg(128, 256); AssertChr(0); AssertVram(8); AssertWram(0);
					bus_conflict = false; //adelikat:  I could not find an example of a game that needs bus conflicts, please enlightening me of a case where a game fails because of the lack of conflicts!
					break;

				case "ACCLAIM-AOROM": // wizards and warriors 3
					AssertPrg(256); AssertChr(0); AssertVram(8); AssertWram(0);
					bus_conflict = true; // not enough chips on the pcb to disable bus conflicts?
					break;

				default:
					return false;
			}

			prg_mask_32k = Cart.prg_size / 32 - 1;
			SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenA);

			return true;
		}

		public override byte ReadPRG(int addr)
		{
			if (Cart.prg_size ==  16)
			{
				return ROM[(addr & 0x3FFF) | prg << 15];
			}

			return ROM[addr | prg << 15];
		}

		public override void WritePRG(int addr, byte value)
		{
			if (ROM != null && bus_conflict)
				value = HandleNormalPRGConflict(addr,value);
			prg = value & prg_mask_32k;
			if ((value & 0x10) == 0)
				SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenA);
			else
				SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenB);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg",ref prg);
		}

	}
}