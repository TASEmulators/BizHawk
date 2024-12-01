using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES
	{
		private void SyncState(Serializer ser)
		{
			int version = 4;
			ser.BeginSection(nameof(NES));
			ser.Sync(nameof(version), ref version);
			ser.Sync("Frame", ref _frame);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("IsLag", ref islag);
			cpu.SyncState(ser);
			ser.Sync(nameof(ram), ref ram, false);
			ser.Sync(nameof(CIRAM), ref CIRAM, false);
			ser.Sync(nameof(_irq_apu), ref _irq_apu);
			ser.Sync(nameof(sprdma_countdown), ref sprdma_countdown);
			ser.Sync(nameof(cpu_deadcounter), ref cpu_deadcounter);

			ser.Sync(nameof(old_s), ref old_s);

			// OAM related
			ser.Sync(nameof(oam_dma_index), ref oam_dma_index);
			ser.Sync(nameof(oam_dma_exec), ref oam_dma_exec);
			ser.Sync(nameof(oam_dma_addr), ref oam_dma_addr);
			ser.Sync(nameof(oam_dma_byte), ref oam_dma_byte);
			ser.Sync(nameof(dmc_dma_exec), ref dmc_dma_exec);
			ser.Sync(nameof(dmc_realign), ref dmc_realign);
			ser.Sync(nameof(reread_trigger), ref reread_trigger);
			ser.Sync(nameof(do_the_reread_2002), ref do_the_reread_2002);
			ser.Sync(nameof(do_the_reread_2007), ref do_the_reread_2007);
			ser.Sync(nameof(do_the_reread_cont_1), ref do_the_reread_cont_1);
			ser.Sync(nameof(do_the_reread_cont_2), ref do_the_reread_cont_2);
			ser.Sync(nameof(reread_opp_4016), ref reread_opp_4016);
			ser.Sync(nameof(reread_opp_4017), ref reread_opp_4017);

			// VS related
			ser.Sync("VS", ref _isVS);
			ser.Sync("VS_2c05", ref _isVS2c05);
			ser.Sync("VS_CHR", ref VS_chr_reg);
			ser.Sync("VS_PRG", ref VS_prg_reg);
			ser.Sync("VS_DIPS", ref VS_dips, false);
			ser.Sync("VS_Service", ref VS_service);
			ser.Sync("VS_Coin", ref VS_coin_inserted);
			ser.Sync("VS_ROM_Control", ref VS_ROM_control);

			// single cycle execution related
			ser.Sync(nameof(current_strobe), ref current_strobe);
			ser.Sync(nameof(new_strobe), ref new_strobe);

			ser.BeginSection(nameof(Board));
			Board.SyncState(ser);
			if (Board is NesBoardBase board && !board.SyncStateFlag)
			{
				throw new InvalidOperationException($"the current NES mapper didn't call base.{nameof(INesBoard.SyncState)}");
			}

			ser.EndSection();
			ppu.SyncState(ser);
			apu.SyncState(ser);

			if (version >= 2)
			{
				ser.Sync(nameof(DB), ref DB);
			}

			if (version >= 3)
			{
				ser.Sync(nameof(latched4016), ref latched4016);
				ser.BeginSection(nameof(ControllerDeck));
				ControllerDeck.SyncState(ser);
				ser.EndSection();
			}

			if (version >= 4)
			{
				ser.Sync(nameof(resetSignal), ref resetSignal);
				ser.Sync(nameof(hardResetSignal), ref hardResetSignal);
			}

			ser.EndSection();
		}
	}
}
